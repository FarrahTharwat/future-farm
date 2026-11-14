"""Climate suitability (0-100): how much of a rotation's growing time is NOT heat-stressed.
A heat-stress day = a day inside the crop's growing months whose daily maximum temperature is above
that crop's heat_threshold_c. Score = 100 * (1 - share of growing days that are heat-stress days).
Inputs : daily NASA POWER file (T2M_MAX), crop_database.csv (heat_threshold_c), crop_calendars.csv.
WARNING: heat_threshold_c is a PLACEHOLDER (random plausible numbers) in DS2's current files. Test only.
Not covered yet: frost risk (needs daily minimum temperature, T2M_MIN, which we have not downloaded).
temp_shift_c lets the scenario code ask "what if it were 1.5 C warmer?" without touching the data.
"""
import pandas as pd

def load_daily(path="data/raw/nasa_power/power_daily.csv"):
    return pd.read_csv(path, index_col=0, parse_dates=True)

def crop_heat(crop_id, daily, db, cal, temp_shift_c=0.0):
    months = [int(m) for m in str(cal.loc[crop_id, "green_cover_months"]).split(";")]
    threshold = db.loc[crop_id, "heat_threshold_c"]
    tmax = daily.loc[daily.index.month.isin(months), "T2M_MAX"].dropna() + temp_shift_c
    years = daily.index.year.nunique()
    return {"crop_id": crop_id,
            "season_days_per_year": len(tmax) / years,
            "heat_days_per_year": float((tmax > threshold).sum()) / years}

def rotation_climate(rotation_id, rotations, daily, db, cal, temp_shift_c=0.0):
    steps = rotations[rotations["rotation_id"] == rotation_id].sort_values("step")
    cycle = int(steps["cycle_years"].iloc[0])
    per_step = [crop_heat(c, daily, db, cal, temp_shift_c) for c in steps["crop_id"]]
    season = sum(s["season_days_per_year"] for s in per_step)
    heat = sum(s["heat_days_per_year"] for s in per_step)
    share = heat / season if season else 0.0
    return {
        "rotation_id": rotation_id,
        "climate_score": round(100 * (1 - share), 1),
        "heat_days_per_year": round(heat / cycle, 1),
        "share_of_growing_days_hot": round(share, 3),
        "steps": per_step,
    }