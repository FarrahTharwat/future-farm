"""Sanity-check DS2's files so the model never receives broken inputs."""
import csv, json, sys

DB_COLS = ["crop","water_requirement_mm_per_season","rooting_depth_cm","nitrogen_fixing",
           "drought_tolerance","heat_threshold_c","soil_cover_value","notes","source"]
problems = []

with open("data/agricultural/crop_database.csv") as f:
    rows = list(csv.DictReader(f))
    if list(rows[0].keys() if rows else DB_COLS) != DB_COLS and rows:
        problems.append("crop_database.csv columns differ from the agreed header")
crops = {r["crop"] for r in rows}
for r in rows:
    if not r["source"].strip():
        problems.append(f"crop_database.csv: '{r['crop']}' has no source")

rot = json.load(open("data/agricultural/rotation_strategies.json"))
if len(rot["rotations"]) != 3:
    problems.append("need exactly 3 rotations (current + 2 alternatives)")
for r in rot["rotations"]:
    for s in r["seasons"]:
        if s["crop"] not in crops:
            problems.append(f"rotation '{r['id']}' uses crop '{s['crop']}' not in crop_database.csv")

print("OK" if not problems else "\n".join(problems))
sys.exit(1 if problems else 0)
