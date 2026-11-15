"""Climate suitability (0-100): how much of a rotation's growing time is NOT heat-stressed, weighted by severity.
A day inside the crop's growing months whose daily maximum temperature is above that crop's heat_threshold_c is a
heat-stress day. How bad it is depends on how far above the limit it is: stress for the day = excess / SEVERITY_CAP_C,
capped at 1. So a day 2 C over the limit counts 0.2, a day 10 C or more over counts 1.
Score = 100 * (1 - severity-weighted stress days / growing days).
Inputs : daily NASA POWER file (T2M_MAX), crop_database.csv (heat_threshold_c), crop_calendars.csv.
WARNING: heat_threshold_c values are DS2 estimates (not sourced), and SEVERITY_CAP_C is a judgement value. Test both.
Not covered yet: frost risk (needs daily minimum temperature, T2M_MIN, which we have not downloaded) and
stage-specific heat thresholds (flowering is far more heat-sensitive than other stages; no data for this yet).
temp_shift_c lets the scenario code ask "what if it were 1.5 C warmer?" without touching the data.
"""
import pandas as pd

SEVERITY_CAP_C = 10.0   # degrees above the limit at which a day counts as fully stressed [UNVERIFIED judgement]

# DS1's weather_daily.csv uses descriptive column names; the model uses the NASA POWER names. Both schemas work.
POWER_NAMES = {"temperature_avg": "T2M", "temperature_max": "T2M_MAX", "temperature_min": "T2M_MIN",
               "rainfall": "PRECTOTCORR", "humidity": "RH2M", "wind_speed": "WS2M",
               "solar_radiation": "ALLSKY_SFC_SW_DWN"}

def load_daily(path="data/processed/weather_daily.csv"):
    """Daily weather indexed by date, with NASA POWER column names (T2M_MAX, PRECTOTCORR, ...)."""
    d = pd.read_csv(path, index_col=0, parse_dates=True).rename(columns=POWER_NAMES)
    missing = [c for c in ("T2M", "T2M_MAX", "PRECTOTCORR", "RH2M", "ALLSKY_SFC_SW_DWN") if c not in d.columns]
    if missing:
        raise ValueError(f"{path} is missing weather columns {missing}")
    return d

def crop_heat(crop_id, daily, db, cal, temp_shift_c=0.0, threshold_shift_c=0.0, cap=None):
    cap = SEVERITY_CAP_C if cap is None else cap
    months = [int(m) for m in str(cal.loc[crop_id, "green_cover_months"]).split(";")]
    threshold = db.loc[crop_id, "heat_threshold_c"] + threshold_shift_c   # shift tests how sure we are of the limit
    tmax = daily.loc[daily.index.month.isin(months), "T2M_MAX"].dropna() + temp_shift_c
    years = daily.index.year.nunique()
    excess = (tmax - threshold).clip(lower=0)                             # degrees above the limit, 0 if below
    return {"crop_id": crop_id,
            "season_days_per_year": len(tmax) / years,
            "heat_days_per_year": float((excess > 0).sum()) / years,
            "degree_days_per_year": float(excess.sum()) / years,
            "stress_days_per_year": float((excess / cap).clip(upper=1.0).sum()) / years}

def rotation_climate(rotation_id, rotations, daily, db, cal, temp_shift_c=0.0, threshold_shift_c=0.0, cap=None):
    steps = rotations[rotations["rotation_id"] == rotation_id].sort_values("step")
    cycle = int(steps["cycle_years"].iloc[0])
    per_step = [crop_heat(c, daily, db, cal, temp_shift_c, threshold_shift_c, cap) for c in steps["crop_id"]]
    season = sum(s["season_days_per_year"] for s in per_step)
    heat = sum(s["heat_days_per_year"] for s in per_step)
    degree = sum(s["degree_days_per_year"] for s in per_step)
    stress = sum(s["stress_days_per_year"] for s in per_step)
    share = stress / season if season else 0.0
    return {
        "rotation_id": rotation_id,
        "climate_score": round(100 * (1 - share), 1),
        "heat_days_per_year": round(heat / cycle, 1),
        "degree_days_per_year": round(degree / cycle, 1),
        "share_of_growing_days_hot": round(heat / season, 3) if season else 0.0,
        "severity_weighted_share": round(share, 3),
        "steps": per_step,
    }