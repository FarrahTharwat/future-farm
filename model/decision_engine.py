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

def load_climate():
    """Monthly climate table. ET0 comes from, in order: an et0_mm_mean column in environmental_features.csv
    (DS1's own series), data/processed/et0_monthly.csv (Fallon AgriMet station, converted), or a TEMPORARY list."""
    clim = pd.read_csv("data/processed/environmental_features.csv", index_col="month")
    source = "environmental_features.csv"
    if "et0_mm_mean" not in clim.columns:
        if os.path.exists("data/processed/et0_monthly.csv"):
            clim["et0_mm_mean"] = pd.read_csv("data/processed/et0_monthly.csv", index_col="month")["et0_mm_mean"]
            source = "USBR AgriMet Fallon station (2001-2010), converted to grass reference"
        else:
            clim["et0_mm_mean"] = TEMP_ET0
            source = "TEMPORARY"
    return clim, source

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


ET0_FACTORS = [0.9, 1.0, 1.1]    # DS2's water min / central / max are -10% / 0 / +10% of the central value
HEAT_SHIFTS = [-2, 0, 2]         # heat limits are estimates: test +-2 C

def uncertainty_ranges(rots, clim, daily, db, cal, all_scores):
    """Rerun overall scores across DS2's water range and the heat-limit range. Reports whether the ranking holds."""
    cases = []
    for f in ET0_FACTORS:
        c2 = clim.copy()
        c2["et0_mm_mean"] = c2["et0_mm_mean"] * f
        for dt in HEAT_SHIFTS:
            ov = {}
            for rid in ROTATIONS:
                wsc = rotation_water(rid, rots, c2, db, cal)["water_score"]
                csc = rotation_climate(rid, rots, daily, db, cal, threshold_shift_c=dt)["climate_score"]
                ov[rid] = score_rotation({"water": wsc, "climate": csc,
                                          "soil_health": all_scores[rid]["soil_health"]}, WEIGHTS)["overall"]
            cases.append({"et0_factor": f, "heat_limit_shift_c": dt, "overall": ov,
                          "ranking": sorted(ov, key=lambda r: -ov[r])})
    first = {rid: sum(c["ranking"][0] == rid for c in cases) for rid in ROTATIONS}
    return {
        "note": "Water range = DS2's -10%/+10% around the central value; heat limits shifted +-2 C; soil score held constant",
        "overall_range": {rid: {"min": min(c["overall"][rid] for c in cases),
                                "max": max(c["overall"][rid] for c in cases)} for rid in ROTATIONS},
        "cases_ranked_first": first,
        "cases_total": len(cases),
        "ranking_ever_changes": len({tuple(c["ranking"]) for c in cases}) > 1,
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
                    + " ".join(f"The top rotation changes from {names[f['from']]} to {names[f['to']]} "
                               f"at about {f['at_weight_pct']}% {label}." for f in flips))
        else:
            text = f"{names[first]} ranks first at every {label} weight from 0% to 100%."
        out[k] = {"current_weight_pct": current_pct, "leader_at_0_pct": first, "leader_at_100_pct": prev,
                  "flip_points": flips, "top_never_changes": not flips, "sentence": text}
    return out
    
    
def build():
    db, cal = load_crops()
    rots = load_rotations()
    clim, et0_source = load_climate()
    temp_et0 = et0_source == "TEMPORARY"
    daily = load_daily()
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
        reasons = ["Crop coefficients, heat limits and soil ratings are not yet verified by a human",
                   "Soil properties (SoilGrids) are not used in the scores yet",
                   "Water supply (1,220 mm) and effective-rain method are unverified assumptions"]
        if temp_et0:
            reasons.insert(0, "Evapotranspiration (ET0) is a TEMPORARY estimate, not NASA-derived")
        rotation_results.append({
            "id": rid, "name": names[rid], "crops": list(dict.fromkeys(x["crop_id"] for x in w["steps"])),
            "cycle_years": w["cycle_years"], "rank": rank_base[rid],
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
        "farm": {"name": "Fallon, Nevada (pilot)", "latitude": 39.47, "longitude": -118.78, "analysis_period": "2001-2025"},
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
                        "Warming raises water demand by an approximate temperature scaling, not a full evapotranspiration model",]}

if __name__ == "__main__":
    out = build()
    os.makedirs("outputs", exist_ok=True)
    json.dump(out, open("outputs/results.json", "w"), indent=2)
    print(out["data_status"])
    for r in out["rotations"]:
        print(f"{r['id']:8s}", r["scores"], "| stress:", r["stress_test"]["overall_under_stress"],
              "| rank", r["stress_test"]["rank_baseline"], "->", r["stress_test"]["rank_under_stress"])
    print("ET0 source:", out["et0_source"])
    print("share of priority settings each rotation wins:", out["ranking_stability"])
    for k, v in out["sensitivity"].items():
        print(k, v["overall"], "->", v["ranking"])
        
    u = out["uncertainty_ranges"]
    print("overall range:", u["overall_range"])
    print("ranked first in", u["cases_ranked_first"], "of", u["cases_total"], "cases | ranking ever changes:", u["ranking_ever_changes"])
    for k, v in out["flip_points"].items():
        print("flip:", v["sentence"])