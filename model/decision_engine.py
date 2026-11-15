"""Builds outputs/results.json, the file Unity reads.   Run from the repo root:  python model/decision_engine.py

Pipeline: climate + crop files -> three 0-100 criterion scores per rotation (water, climate, soil_health)
-> weighted by farmer priorities -> overall. Climate resilience is a SEPARATE stress test shown beside the
ranking, NOT part of the overall (it is built from the water and climate scores, so counting it again would
count the same information twice).
EVERYTHING here is prototype-grade until ET0 is real and DS2's values are verified by a human.
"""
import json, os, sys
import pandas as pd

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from water_need import load_crops, load_rotations, rotation_water, WATER_SUPPLY_MM
from soil_health import soil_health
from climate_score import load_daily, rotation_climate
from scoring import score_rotation, rank
from scenario_analysis import weight_sweep
from explanations import explain
from field_timeline import field_timeline

ROTATIONS = ["current", "alt_a", "alt_b"]
CRITERIA = ["water", "climate", "soil_health"]
WEIGHTS = {"water": 40, "climate": 25, "soil_health": 35}      # default farmer priorities (illustrative)
HARSH = {"rain_pct": -20, "temp_c": 1.5}                        # the stress test
RAIN_STEPS, TEMP_STEPS = [-20, 0, 20], [0, 1.5, 3]              # what-if grid for the sliders
TEMP_ET0 = [20, 35, 70, 110, 150, 180, 200, 175, 125, 80, 40, 20]   # TEMPORARY, mm per month
TIE_MARGIN = 1.0                  # overall scores within this many points are shown as a tie, not a strict rank

# ---- Model inputs (the only two switches that decide where the data comes from) -------------------------------
WEATHER_FILE = "data/processed/weather_daily.csv"   # DS1: daily NASA POWER at the farm point (2001-2025)
ET0_SOURCE = "nasa"        # "nasa": DS1's FAO-56 Penman-Monteith ET0 (column et0_mm_mean in environmental_features.csv)
                           # "agrimet": DS2's Fallon station series (et0_monthly.csv, alfalfa reference / 1.2)
NASA_ET0_FILE = "data/processed/environmental_features.csv"
AGRIMET_ET0_FILE = "data/processed/et0_monthly.csv"

def climate_from_daily(daily):
    """Typical-year table (index = calendar month 1-12) built from the daily weather: mean monthly rain total,
    mean monthly temperature, etc. The model computes this itself, so it never depends on which columns
    environmental_features.csv happens to hold."""
    m = daily.resample("MS").agg({"T2M": "mean", "T2M_MAX": "mean", "PRECTOTCORR": "sum",
                                  "RH2M": "mean", "ALLSKY_SFC_SW_DWN": "mean"})
    g = m.groupby(m.index.month)
    out = pd.DataFrame({"rain_mm_mean": g["PRECTOTCORR"].mean(), "rain_mm_std": g["PRECTOTCORR"].std(),
                        "temp_c_mean": g["T2M"].mean(), "temp_max_c_mean": g["T2M_MAX"].mean(),
                        "humidity_pct_mean": g["RH2M"].mean(), "solar_mean": g["ALLSKY_SFC_SW_DWN"].mean()})
    out.index.name = "month"
    return out

def load_climate(daily):
    """Monthly climate table: rain and temperature from the daily weather, ET0 from the source chosen above.
    Returns (table, text describing the ET0 source)."""
    clim = climate_from_daily(daily)
    if ET0_SOURCE == "nasa":
        f = pd.read_csv(NASA_ET0_FILE, index_col="month")
        if "et0_mm_mean" not in f.columns or len(f) != 12 or f["et0_mm_mean"].isna().any():
            raise ValueError(f"{NASA_ET0_FILE} needs 12 monthly values in a column called et0_mm_mean "
                             f"(run pipeline/feature_engineering.py), or set ET0_SOURCE = 'agrimet'")
        clim["et0_mm_mean"] = f["et0_mm_mean"]
        return clim, "NASA POWER daily weather, FAO-56 Penman-Monteith (DS1), monthly mean 2001-2025"
    if ET0_SOURCE != "agrimet":
        raise ValueError("ET0_SOURCE must be 'nasa' or 'agrimet'")
    if os.path.exists(AGRIMET_ET0_FILE):
        clim["et0_mm_mean"] = pd.read_csv(AGRIMET_ET0_FILE, index_col="month")["et0_mm_mean"]
        return clim, "USBR AgriMet Fallon station (2001-2010), converted to grass reference"
    clim["et0_mm_mean"] = TEMP_ET0
    return clim, "TEMPORARY"

def warm_et0(clim, temp_c):
    """ET0 under warming. Hargreaves shape: ET0 scales with (mean temp + 17.8), so +dT raises a month's ET0 by
    dT / (T_mean + 17.8). Approximation [UNVERIFIED]: ignores changes in temperature range, humidity, wind."""
    c2 = clim.copy()
    c2["et0_mm_mean"] = c2["et0_mm_mean"] * (1 + temp_c / (c2["temp_c_mean"] + 17.8))
    return c2

def what_if(rid, rain_pct, temp_c, clim, daily, rots, db, cal):
    c2 = clim.copy()
    c2["rain_mm_mean"] = c2["rain_mm_mean"] * (1 + rain_pct / 100)
    c2 = warm_et0(c2, temp_c)
    water = rotation_water(rid, rots, c2, db, cal)["water_score"]
    climate = rotation_climate(rid, rots, daily, db, cal, temp_shift_c=temp_c)["climate_score"]
    return water, climate


ET0_FACTORS = [0.9, 1.0, 1.1]        # DS2's water min / central / max are -10% / 0 / +10% of the central value
HEAT_SHIFTS = [-2, 0, 2]             # heat limits are estimates: test +-2 C
SEVERITY_CAPS = [5.0, 10.0, 15.0]    # degrees above the limit at which a day counts as fully stressed (judgement)

def _lead(ov):
    top = sorted(ov.values(), reverse=True)
    return round(top[0] - top[1], 1)

def uncertainty_ranges(rots, clim, daily, db, cal, all_scores):
    """Rerun overall scores across DS2's water range, the heat-limit range and the heat-severity cap (27 cases).
    Water depends only on the ET0 factor and climate only on (heat shift, cap), so each is computed once."""
    water = {}
    for f in ET0_FACTORS:
        c2 = clim.copy()
        c2["et0_mm_mean"] = c2["et0_mm_mean"] * f
        water[f] = {rid: rotation_water(rid, rots, c2, db, cal)["water_score"] for rid in ROTATIONS}
    climate = {(dt, cap): {rid: rotation_climate(rid, rots, daily, db, cal, threshold_shift_c=dt, cap=cap)["climate_score"]
                           for rid in ROTATIONS}
               for dt in HEAT_SHIFTS for cap in SEVERITY_CAPS}
    cases = []
    for f in ET0_FACTORS:
        for dt in HEAT_SHIFTS:
            for cap in SEVERITY_CAPS:
                ov = {rid: score_rotation({"water": water[f][rid], "climate": climate[(dt, cap)][rid],
                                           "soil_health": all_scores[rid]["soil_health"]}, WEIGHTS)["overall"]
                      for rid in ROTATIONS}
                cases.append({"et0_factor": f, "heat_limit_shift_c": dt, "severity_cap_c": cap, "overall": ov,
                              "ranking": sorted(ov, key=lambda r: -ov[r])})
    first = {rid: sum(c["ranking"][0] == rid for c in cases) for rid in ROTATIONS}
    return {
        "note": "Water range = DS2's -10%/+10% around the central value; heat limits shifted +-2 C; heat severity cap 5/10/15 C; soil score held constant",
        "overall_range": {rid: {"min": min(c["overall"][rid] for c in cases),
                                "max": max(c["overall"][rid] for c in cases)} for rid in ROTATIONS},
        "cases_ranked_first": first,
        "cases_total": len(cases),
        "leader_ever_changes": len({c["ranking"][0] for c in cases}) > 1,
        "ranking_ever_changes": len({tuple(c["ranking"]) for c in cases}) > 1,   # any change of order, incl. 2nd/3rd swapping
        "smallest_lead_points": min(_lead(c["overall"]) for c in cases),          # first place's smallest margin over second
        "cases": cases}

def flip_points(all_scores, criteria, weights, names, step=0.5):
    """For each criterion, sweep its weight from 0 to 100%. The other criteria keep their current ratio.
    Report where the top-ranked rotation changes (accurate to one step)."""
    total = sum(weights.values())
    out = {}
    for k in criteria:
        others = [c for c in criteria if c != k]
        rest = sum(weights[c] for c in others)
        def leader(w):
            wt = {k: w, **{c: (100 - w) * weights[c] / rest for c in others}}
            return rank(all_scores, wt)[0][0]
        grid = [i * step for i in range(int(100 / step) + 1)]
        flips, prev = [], leader(0)
        first = prev
        for w in grid[1:]:
            now = leader(w)
            if now != prev:
                flips.append({"at_weight_pct": w, "from": prev, "to": now})
                prev = now
        label = k.replace("_", " ")
        current_pct = round(100 * weights[k] / total, 1)
        if flips:
            text = (f"With {label} weighted {current_pct}%, {names[leader(current_pct)]} ranks first. "
                    + " ".join(f"As the weight on {label} rises, the top rotation changes from {names[f['from']]} "
                               f"to {names[f['to']]} at about {f['at_weight_pct']}%." for f in flips))
        else:
            text = f"{names[first]} ranks first at every {label} weight from 0% to 100%."
        out[k] = {"current_weight_pct": current_pct, "leader_at_0_pct": first, "leader_at_100_pct": prev,
                  "flip_points": flips, "top_never_changes": not flips, "sentence": text}
    return out


def build():
    db, cal = load_crops()
    rots = load_rotations()
    daily = load_daily(WEATHER_FILE)
    clim, et0_source = load_climate(daily)
    temp_et0 = et0_source == "TEMPORARY"
    names = rots.drop_duplicates("rotation_id").set_index("rotation_id")["rotation_name"].to_dict()

    base, stress, parts = {}, {}, {}
    for rid in ROTATIONS:
        w = rotation_water(rid, rots, clim, db, cal)
        c = rotation_climate(rid, rots, daily, db, cal)
        s = soil_health(rid, rots, db, cal)
        hw, hc = what_if(rid, HARSH["rain_pct"], HARSH["temp_c"], clim, daily, rots, db, cal)
        subs = {"water": w["water_score"], "climate": c["climate_score"], "soil_health": s["soil_health_score"]}
        base[rid] = score_rotation(subs, WEIGHTS)
        stress[rid] = score_rotation({**subs, "water": hw, "climate": hc}, WEIGHTS)["overall"]
        parts[rid] = (w, c, s, subs)

    rank_base = {r: i + 1 for i, (r, _) in enumerate(sorted(base.items(), key=lambda x: -x[1]["overall"]))}
    rank_stress = {r: i + 1 for i, (r, _) in enumerate(sorted(stress.items(), key=lambda x: -x[1]))}
    tied = {r: [o for o in ROTATIONS if o != r and round(abs(base[o]["overall"] - base[r]["overall"]), 1) <= TIE_MARGIN]
            for r in ROTATIONS}

    rotation_results, all_scores = [], {}
    for rid in ROTATIONS:
        w, c, s, subs = parts[rid]
        res = base[rid]
        retention = round(min(100.0, 100 * stress[rid] / res["overall"]), 1) if res["overall"] else 0.0
        all_scores[rid] = subs
        warnings = []
        if not w["feasible_within_supply"]:
            warnings.append(f"Needs {w['yearly_irrigation_mm']} mm irrigation per year, "
                            f"{w['water_deficit_mm']} mm more than the assumed supply of {WATER_SUPPLY_MM} mm")
        explanations = explain(names[rid], subs, res["contributions"]) + [
            f"Needs about {w['yearly_irrigation_mm']} mm of irrigation per year; usable rain covers about {w['yearly_rain_mm']} mm.",
            f"About {c['heat_days_per_year']} heat-stress days per year across its growing seasons.",
            f"Roughly {s['bare_months_per_year']} months per year without living green cover.",
            f"Stress test ({HARSH['rain_pct']}% rain, +{HARSH['temp_c']} C): its overall score is "
            f"{stress[rid]} (rank {rank_stress[rid]}, baseline rank {rank_base[rid]})."]
        if tied[rid]:
            explanations.append(f"Its overall score is within {TIE_MARGIN:g} point of "
                                f"{', '.join(names[o] for o in tied[rid])}, so the order between them is effectively a tie.")
        reasons = ["Crop coefficients, heat limits and soil ratings are not yet verified by a human",
                   "Soil properties (SoilGrids) are not used in the scores yet",
                   "Water supply (1,220 mm) and effective-rain method are unverified assumptions"]
        if temp_et0:
            reasons.insert(0, "Evapotranspiration (ET0) is a TEMPORARY estimate, not NASA-derived")
        rotation_results.append({
            "id": rid, "name": names[rid], "crops": list(dict.fromkeys(x["crop_id"] for x in w["steps"])),
            "cycle_years": w["cycle_years"], "rank": rank_base[rid], "tied_with": tied[rid],
            "scores": {**subs, "overall": res["overall"]}, "contributions": res["contributions"],
            "stress_test": {"overall_under_stress": stress[rid], "retention_pct": retention,
                            "rank_baseline": rank_base[rid], "rank_under_stress": rank_stress[rid],
                            "scenario": HARSH, "note": "Shown beside the ranking; not part of the overall score"},
            "details": {"yearly_irrigation_mm": w["yearly_irrigation_mm"], "yearly_rain_mm": w["yearly_rain_mm"],
                        "water_deficit_mm": w["water_deficit_mm"],
                        "heat_days_per_year": c["heat_days_per_year"], "bare_months_per_year": s["bare_months_per_year"]},
            "feasibility_warnings": warnings,
            "explanations": explanations,
            "field_timeline": field_timeline(rid, rots, cal),
            "heat_degree_days_per_year": c["degree_days_per_year"],
            "uncertainty": {"level": "high", "reasons": reasons}})

    # How much does the result depend on DS2's heat limits (all placeholders or estimates)?  Shift every limit by -2/0/+2 C.
    sensitivity = {}
    for dt in (-2, 0, 2):
        ov = {}
        for rid in ROTATIONS:
            cl = rotation_climate(rid, rots, daily, db, cal, threshold_shift_c=dt)["climate_score"]
            ov[rid] = score_rotation({**all_scores[rid], "climate": cl}, WEIGHTS)["overall"]
        order = sorted(ov, key=lambda r: -ov[r])
        sensitivity[f"heat_limit_shift_{dt:+d}C"] = {"overall": ov, "ranking": order}
    ranges = uncertainty_ranges(rots, clim, daily, db, cal, all_scores)
    flips = flip_points(all_scores, CRITERIA, WEIGHTS, names)
    variants = []
    for rp in RAIN_STEPS:
        for tc in TEMP_STEPS:
            rs = {}
            for rid in ROTATIONS:
                w_, c_ = what_if(rid, rp, tc, clim, daily, rots, db, cal)
                full = score_rotation({"water": w_, "climate": c_, "soil_health": all_scores[rid]["soil_health"]}, WEIGHTS)
                rs[rid] = {"water": w_, "climate": c_, "overall": full["overall"]}
            variants.append({"rainfall_change_percent": rp, "temperature_change_c": tc, "rotation_scores": rs})

    return {
        "data_status": "PROTOTYPE - inputs not yet verified by a human, not validated results" + (" (temporary ET0)" if temp_et0 else ""),
        "farm": {"name": "Fallon, Nevada (pilot)", "latitude": 39.461142, "longitude": -118.830528, "analysis_period": "2001-2025"},
        "farmer_priorities": WEIGHTS,
        "water_supply_assumption_mm": WATER_SUPPLY_MM,
        "rotations": rotation_results,
        "ranking_stability": weight_sweep(all_scores, CRITERIA),
        "sensitivity": sensitivity,
        "uncertainty_ranges": ranges,
        "flip_points": flips,
        "et0_source": et0_source,
        "scenario_variants": variants,
        "scenario_note": "In the grid, water and climate are recalculated for each rain and warming setting; soil health is held constant; overall uses the same weights as the headline score. Warming raises ET0 using an approximate Hargreaves-style scaling.",
        "limitations": ["Satellite soil moisture is not root-zone moisture", "Crop suitability requires local validation",
                        "Soil-health score is a checklist of potential benefits, not a measurement",
                        "Frost risk and soil salinity are not modelled",
                        "Warming raises water demand by an approximate temperature scaling, not a full evapotranspiration model"]}

if __name__ == "__main__":
    out = build()
    os.makedirs("outputs", exist_ok=True)
    json.dump(out, open("outputs/results.json", "w"), indent=2)
    print(out["data_status"])
    for r in out["rotations"]:
        print(f"{r['id']:8s}", r["scores"], "| stress:", r["stress_test"]["overall_under_stress"],
              "| rank", r["stress_test"]["rank_baseline"], "->", r["stress_test"]["rank_under_stress"],
              "| tied with:", r["tied_with"] or "none")
    print("ET0 source:", out["et0_source"])
    print("share of priority settings each rotation wins:", out["ranking_stability"])
    for k, v in out["sensitivity"].items():
        print(k, v["overall"], "->", v["ranking"])

    u = out["uncertainty_ranges"]
    print("overall range:", u["overall_range"])
    print("ranked first in", u["cases_ranked_first"], "of", u["cases_total"], "cases")
    print("leader ever changes:", u["leader_ever_changes"], "| any change of order:", u["ranking_ever_changes"],
          "| smallest lead of first place (points):", u["smallest_lead_points"])
    for k, v in out["flip_points"].items():
        print("flip:", v["sentence"])