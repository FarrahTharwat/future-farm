"""End-to-end smoke test for the decision engine.    Run from anywhere:   python model/check_run.py

It checks that every input file exists and is complete, that the model runs, that the output is internally
consistent, and that outputs/results.json and docs/methodology.md match what the code produces right now.
Exit code 0 = everything passed, 1 = at least one problem (each one is printed).
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(ROOT)                                   # every path in the model is relative to the repo root
sys.path.insert(0, os.path.join(ROOT, "model"))

REQUIRED_FILES = [
    "data/processed/weather_daily.csv",
    "data/processed/environmental_features.csv",
    "data/agricultural/crop_database.csv",
    "data/agricultural/crop_calendars.csv",
    "data/agricultural/rotation_strategies.csv",
]
DB_COLS = ["type", "kc_initial", "kc_mid", "kc_late", "heat_threshold_c", "nitrogen_fixing",
           "residue_level", "root_biomass_contribution"]
CAL_COLS = ["plant_start_month", "plant_end_month", "harvest_end_month", "crosses_year", "green_cover_months",
            "initial_days", "development_days", "mid_days", "late_days"]
LEVELS = {"low", "medium", "high"}

errors, warnings = [], []


def need(condition, message):
    if not condition:
        errors.append(message)


def finish():
    for w in warnings:
        print("WARNING:", w)
    for e in errors:
        print("FAIL:   ", e)
    if errors:
        print(f"\n{len(errors)} problem(s) found.")
        sys.exit(1)
    print("\nOK: inputs complete, model runs, output consistent, results.json and methodology.md up to date.")
    sys.exit(0)


# 1. Files exist ------------------------------------------------------------------------------------------------
for f in REQUIRED_FILES:
    need(os.path.exists(f), f"missing input file: {f}")
if errors:
    finish()

import pandas as pd                               # noqa: E402  (imported after the chdir on purpose)
import decision_engine as de                      # noqa: E402
from water_need import load_crops, load_rotations  # noqa: E402
from climate_score import load_daily               # noqa: E402

# 2. Crop files are complete for every crop a rotation uses ------------------------------------------------------
db, cal = load_crops()
rots = load_rotations()
used = set(rots["crop_id"])
need(used <= set(db.index), f"crops in rotation_strategies.csv but not in crop_database.csv: {sorted(used - set(db.index))}")
need(used <= set(cal.index), f"crops in rotation_strategies.csv but not in crop_calendars.csv: {sorted(used - set(cal.index))}")
need(set(rots["rotation_id"]) == set(de.ROTATIONS), f"rotation ids are {sorted(set(rots['rotation_id']))}, expected {de.ROTATIONS}")
if errors:
    finish()

for crop in sorted(used):
    for c in DB_COLS:
        need(c in db.columns and pd.notna(db.loc[crop, c]), f"crop_database.csv: '{crop}' has no value for {c}")
    for c in CAL_COLS:
        need(c in cal.columns and pd.notna(cal.loc[crop, c]), f"crop_calendars.csv: '{crop}' has no value for {c}")
if errors:
    finish()
for crop in sorted(used):
    need(db.loc[crop, "residue_level"] in LEVELS, f"'{crop}': residue_level must be low/medium/high")
    need(db.loc[crop, "root_biomass_contribution"] in LEVELS, f"'{crop}': root_biomass_contribution must be low/medium/high")
    need(db.loc[crop, "nitrogen_fixing"] in {"yes", "no"}, f"'{crop}': nitrogen_fixing must be yes/no")

# 3. Weather and ET0 ---------------------------------------------------------------------------------------------
try:
    daily = load_daily(de.WEATHER_FILE)
    need(len(daily) > 9000, f"{de.WEATHER_FILE} has only {len(daily)} rows (expected about 9,131 for 2001-2025)")
    need(int(daily["T2M_MAX"].isna().sum()) == 0, "weather file has blank daily maximum temperatures")
except Exception as exc:
    errors.append(f"could not read {de.WEATHER_FILE}: {exc}")
if errors:
    finish()

# 4. The model runs ----------------------------------------------------------------------------------------------
try:
    out = de.build()
except Exception as exc:
    errors.append(f"decision_engine.build() raised {type(exc).__name__}: {exc}")
    finish()
try:
    text = json.dumps(out, allow_nan=False)
except (ValueError, TypeError) as exc:
    errors.append(f"results cannot be written as clean JSON (NaN or unsupported type): {exc}")
    finish()

# 5. Output is internally consistent -----------------------------------------------------------------------------
need(not out["et0_source"].startswith("TEMPORARY"), "ET0 is the TEMPORARY placeholder")
need(len(out["rotations"]) == 3, "results must hold exactly 3 rotations")
need(sorted(r["rank"] for r in out["rotations"]) == [1, 2, 3], "ranks must be 1, 2, 3")
for r in out["rotations"]:
    s, rid = r["scores"], r["id"]
    need("resilience" not in s, f"{rid}: scores.resilience should no longer exist")
    need(all(0 <= s[k] <= 100 for k in ("water", "climate", "soil_health", "overall")), f"{rid}: a score is outside 0-100")
    need(abs(sum(r["contributions"].values()) - s["overall"]) <= 0.1, f"{rid}: contributions do not add up to overall")
    need(len(r["field_timeline"]) == r["cycle_years"] * 12, f"{rid}: field_timeline should have cycle_years x 12 entries")
    need({"overall_under_stress", "retention_pct", "rank_baseline", "rank_under_stress"} <= set(r["stress_test"]),
         f"{rid}: stress_test is incomplete")
    if r["feasibility_warnings"]:
        warnings.append(f"{rid}: {r['feasibility_warnings'][0]}")

need(len(out["scenario_variants"]) == 9, "scenario_variants must have 9 entries")
base = next((v for v in out["scenario_variants"]
             if v["rainfall_change_percent"] == 0 and v["temperature_change_c"] == 0), None)
need(base is not None, "scenario grid has no 0% rain / 0 C entry")
if base:
    for r in out["rotations"]:
        g = base["rotation_scores"][r["id"]]
        for k in ("water", "climate", "overall"):
            need(abs(g[k] - r["scores"][k]) <= 0.05, f"{r['id']}: grid 0/0 {k} ({g[k]}) differs from headline ({r['scores'][k]})")
need(out["uncertainty_ranges"]["cases_total"] == 27, "uncertainty_ranges should hold 27 cases")
need(set(out["flip_points"]) == {"water", "climate", "soil_health"}, "flip_points must have water, climate, soil_health")
need(set(out["farmer_priorities"]) == {"water", "climate", "soil_health"}, "farmer_priorities must have 3 keys")

# 6. results.json on disk is the file this code produces ---------------------------------------------------------
if os.path.exists("outputs/results.json"):
    if json.load(open("outputs/results.json")) != json.loads(text):
        errors.append("outputs/results.json is STALE: run  python model/decision_engine.py  and commit the new file")
else:
    errors.append("outputs/results.json does not exist: run  python model/decision_engine.py")

# 7. methodology.md quotes the same numbers ----------------------------------------------------------------------
if os.path.exists("docs/methodology.md"):
    doc = open("docs/methodology.md", encoding="utf-8").read()
    for r in out["rotations"]:
        for label, value in (("overall score", r["scores"]["overall"]), ("yearly irrigation", r["details"]["yearly_irrigation_mm"])):
            need(str(value) in doc, f"docs/methodology.md does not contain {r['id']}'s {label} ({value}): update section 7")
    water_flip = out["flip_points"]["water"]["flip_points"]
    if water_flip:
        need(f"{water_flip[0]['at_weight_pct']:g}%" in doc, f"docs/methodology.md does not mention the water flip point ({water_flip[0]['at_weight_pct']:g}%)")
else:
    warnings.append("docs/methodology.md not found")

finish()