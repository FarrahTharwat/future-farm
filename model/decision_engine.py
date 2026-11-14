"""Builds outputs/results.json (the data contract Unity reads).
Right now it uses PLACEHOLDER sub-scores so Unity can start. Replace MOCK_SUB_SCORES with
scores computed in feature_engineering.py once real data flows.
"""
import json, os
from scoring import score_rotation
from scenario_analysis import weight_sweep
from explanations import explain

CRITERIA = ["water", "climate", "soil_health", "resilience"]
WEIGHTS = {"water": 40, "climate": 20, "soil_health": 30, "resilience": 10}  # farmer priorities

# PLACEHOLDER numbers, not real results
MOCK_SUB_SCORES = {
    "current": {"name": "Current rotation", "crops": ["Crop A", "Crop B"],
                "scores": {"water": 45, "climate": 60, "soil_health": 50, "resilience": 40}},
    "alt_a":   {"name": "Alternative A", "crops": ["Crop A", "Crop C", "Cover crop"],
                "scores": {"water": 60, "climate": 62, "soil_health": 70, "resilience": 58}},
    "alt_b":   {"name": "Alternative B", "crops": ["Crop D", "Crop E"],
                "scores": {"water": 75, "climate": 55, "soil_health": 55, "resilience": 68}},
}

def build():
    rotations = []
    for rid, r in MOCK_SUB_SCORES.items():
        res = score_rotation(r["scores"], WEIGHTS)
        rotations.append({
            "id": rid, "name": r["name"], "crops": r["crops"],
            "scores": {**r["scores"], "overall": res["overall"]},
            "contributions": res["contributions"],
            "explanations": explain(r["name"], r["scores"], res["contributions"]),
            "uncertainty": {"level": "moderate", "reasons": ["Soil information is estimated"]},
        })
    stability = weight_sweep({k: v["scores"] for k, v in MOCK_SUB_SCORES.items()}, CRITERIA)
    return {
        "mock": True,
        "farm": {"latitude": 39.47, "longitude": -118.78, "soil_source": "SoilGrids",
                 "analysis_period": "2001-2025"},
        "farmer_priorities": WEIGHTS,
        "rotations": rotations,
        "ranking_stability": stability,
        "scenario_variants": [],
        "limitations": ["Satellite soil moisture is not root-zone moisture",
                        "Crop suitability requires local validation"],
    }

if __name__ == "__main__":
    os.makedirs("outputs", exist_ok=True)
    json.dump(build(), open("outputs/results.json", "w"), indent=2)
    print("wrote outputs/results.json")
