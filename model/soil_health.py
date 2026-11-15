"""Soil-health POTENTIAL score (0-100) for a rotation. It is a checklist of agronomic benefits, NOT a
measurement of soil improvement. Satellites cannot see organic matter building up.
Inputs : crop_database.csv, crop_calendars.csv, rotation_strategies.csv (all from DS2).
WARNING: the low/medium/high ratings and the weights below are judgement values, not sourced. Test only.
"""
import pandas as pd

LEVEL = {"low": 0.0, "medium": 0.5, "high": 1.0}

# How much each indicator counts. Judgement. Agree these as a team and document them in docs/methodology.md
WEIGHTS = {"nitrogen": 0.30, "cover": 0.30, "organic_inputs": 0.25, "diversity": 0.15}

def _covered(steps, cal):
    """{cycle_year: set of calendar months with living green cover}. Crops that cross the new year
    (planted in autumn, finished next summer) put their later months into the NEXT year of the cycle."""
    cycle = int(steps["cycle_years"].iloc[0])
    covered = {y: set() for y in range(1, cycle + 1)}
    for _, s in steps.iterrows():
        k = cal.loc[s["crop_id"]]
        months = [int(m) for m in str(k["green_cover_months"]).split(";")]
        crosses = str(k["crosses_year"]).strip().lower() == "yes"
        y, nxt = int(s["year"]), (int(s["year"]) % cycle) + 1
        for m in months:
            later = crosses and m < int(k["plant_start_month"])
            covered[nxt if later else y].add(m)
    return covered

def soil_health(rotation_id, rotations, db, cal, weights=WEIGHTS):
    steps = rotations[rotations["rotation_id"] == rotation_id].sort_values("step")
    cycle = int(steps["cycle_years"].iloc[0])
    crops = list(steps["crop_id"])

    # 1. share of years with a nitrogen-fixing crop in the ground
    n_years = sum(
        any(db.loc[c, "nitrogen_fixing"] == "yes" for c in steps[steps["year"] == y]["crop_id"])
        for y in range(1, cycle + 1))
    nitrogen = n_years / cycle

    # 2. share of the year with living green cover (the rest counts as bare or dormant)
    covered = _covered(steps, cal)
    cover = sum(len(m) for m in covered.values()) / (12 * cycle)

    # 3. organic inputs: average of root and residue ratings across the crops
    organic = sum((LEVEL[db.loc[c, "root_biomass_contribution"]] + LEVEL[db.loc[c, "residue_level"]]) / 2
                  for c in crops) / len(crops)

    # 4. diversity: different crops in the cycle (4 or more counts as full marks)
    species = {c.replace("_establishment", "") for c in crops}   # establishment year is the same species as alfalfa
    diversity = min(len(species) / 4, 1.0)

    indicators = {"nitrogen": nitrogen, "cover": cover, "organic_inputs": organic, "diversity": diversity}
    contributions = {k: round(100 * weights[k] * v, 1) for k, v in indicators.items()}
    return {
        "rotation_id": rotation_id,
        "soil_health_score": round(sum(contributions.values()), 1),
        "indicators": {k: round(v, 2) for k, v in indicators.items()},
        "contributions": contributions,
        "bare_months_per_year": round(12 - 12 * cover, 1),
    }