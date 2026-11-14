"""Builds outputs/results.json, the file Unity reads.   Run from the repo root:  python model/decision_engine.py

Pipeline: climate + crop files -> four 0-100 sub-scores per rotation (water, climate, soil_health, resilience)
-> weighted by farmer priorities -> explanations, ranking stability, and precomputed what-if scenarios.
EVERYTHING here is prototype-grade until DS1's real ET0 arrives and DS2's values are verified.
"""
import json, os, sys
import pandas as pd

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from water_need import load_crops, load_rotations, rotation_water
from soil_health import soil_health
from climate_score import load_daily, rotation_climate
from scoring import score_rotation
from scenario_analysis import weight_sweep
from explanations import explain

ROTATIONS = ["current", "alt_a", "alt_b"]
CRITERIA = ["water", "climate", "soil_health", "resilience"]
WEIGHTS = {"water": 40, "climate": 20, "soil_health": 30, "resilience": 10}   # default farmer priorities
HARSH = {"rain_pct": -20, "temp_c": 1.5}                                      # used for the resilience score
RAIN_STEPS, TEMP_STEPS = [-20, 0, 20], [0, 1.5, 3]                            # what-if grid for the sliders
TEMP_ET0 = [20, 35, 70, 110, 150, 180, 200, 175, 125, 80, 40, 20]             # TEMPORARY, mm per month

def load_climate():
    clim = pd.read_csv("data/processed/environmental_features.csv", index_col="month")
    temporary = "et0_mm_mean" not in clim.columns
    if temporary:
        clim["et0_mm_mean"] = TEMP_ET0
    return clim, temporary

def what_if(rid, rain_pct, temp_c, clim, daily, rots, db, cal):
    c2 = clim.copy()
    c2["rain_mm_mean"] = c2["rain_mm_mean"] * (1 + rain_pct / 100)
    water = rotation_water(rid, rots, c2, db, cal)["water_score"]
    climate = rotation_climate(rid, rots, daily, db, cal, temp_shift_c=temp_c)["climate_score"]
    return water, climate

def build():
    db, cal = load_crops()
    rots = load_rotations()
    clim, temp_et0 = load_climate()
    daily = load_daily()
    names = rots.drop_duplicates("rotation_id").set_index("rotation_id")["rotation_name"].to_dict()

    rotation_results, all_scores = [], {}
    for rid in ROTATIONS:
        w = rotation_water(rid, rots, clim, db, cal)
        c = rotation_climate(rid, rots, daily, db, cal)
        s = soil_health(rid, rots, db, cal)
        hw, hc = what_if(rid, HARSH["rain_pct"], HARSH["temp_c"], clim, daily, rots, db, cal)
        base_avg, harsh_avg = (w["water_score"] + c["climate_score"]) / 2, (hw + hc) / 2
        resilience = round(min(100.0, 100 * harsh_avg / base_avg), 1) if base_avg else 0.0

        subs = {"water": w["water_score"], "climate": c["climate_score"],
                "soil_health": s["soil_health_score"], "resilience": resilience}
        res = score_rotation(subs, WEIGHTS)
        all_scores[rid] = subs
        explanations = explain(names[rid], subs, res["contributions"]) + [
            f"Needs about {w['yearly_irrigation_mm']} mm of irrigation per year; rain covers about {w['yearly_rain_mm']} mm.",
            f"About {c['heat_days_per_year']} heat-stress days per year across its growing seasons.",
            f"Roughly {s['bare_months_per_year']} months per year without living green cover.",
            f"Under {HARSH['rain_pct']}% rain and +{HARSH['temp_c']} C it keeps {resilience}% of its water and climate suitability."]
        reasons = ["Crop coefficients, heat limits and soil ratings are unverified placeholders",
                   "Soil properties (SoilGrids) are not used yet", "Satellite soil moisture is not used yet"]
        if temp_et0:
            reasons.insert(0, "Evapotranspiration (ET0) is a TEMPORARY estimate, not NASA-derived")
        rotation_results.append({
            "id": rid, "name": names[rid], "crops": list(dict.fromkeys(x["crop_id"] for x in w["steps"])),
            "cycle_years": w["cycle_years"],
            "scores": {**subs, "overall": res["overall"]}, "contributions": res["contributions"],
            "details": {"yearly_irrigation_mm": w["yearly_irrigation_mm"], "yearly_rain_mm": w["yearly_rain_mm"],
                        "heat_days_per_year": c["heat_days_per_year"], "bare_months_per_year": s["bare_months_per_year"]},
            "explanations": explanations,
            "uncertainty": {"level": "high", "reasons": reasons}})

    variants = []
    for rp in RAIN_STEPS:
        for tc in TEMP_STEPS:
            rs = {}
            for rid in ROTATIONS:
                w_, c_ = what_if(rid, rp, tc, clim, daily, rots, db, cal)
                full = score_rotation({"water": w_, "climate": c_,
                                       "soil_health": all_scores[rid]["soil_health"],
                                       "resilience": all_scores[rid]["resilience"]}, WEIGHTS)
                rs[rid] = {"water": w_, "climate": c_, "overall": full["overall"]}
            variants.append({"rainfall_change_percent": rp, "temperature_change_c": tc, "rotation_scores": rs})

    return {
        "data_status": "PROTOTYPE - placeholder inputs, not validated results" + (" (temporary ET0)" if temp_et0 else ""),
        "farm": {"name": "Fallon, Nevada (pilot)", "latitude": 39.47, "longitude": -118.78, "analysis_period": "2001-2025"},
        "farmer_priorities": WEIGHTS,
        "rotations": rotation_results,
        "ranking_stability": weight_sweep(all_scores, CRITERIA),
        "scenario_variants": variants,
        "scenario_note": "In the scenario grid, water and climate are recalculated; soil health and resilience are held at their baseline values. Warming changes heat days but not ET0 yet.",
        "limitations": ["Satellite soil moisture is not root-zone moisture", "Crop suitability requires local validation",
                        "Soil-health score is a checklist of potential benefits, not a measurement",
                        "Frost risk and soil salinity are not modelled"]}

if __name__ == "__main__":
    out = build()
    os.makedirs("outputs", exist_ok=True)
    json.dump(out, open("outputs/results.json", "w"), indent=2)
    print(out["data_status"])
    for r in out["rotations"]:
        print(f"{r['id']:8s}", r["scores"])
    print("share of priority settings each rotation wins:", out["ranking_stability"])