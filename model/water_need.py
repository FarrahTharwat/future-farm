"""Crop water need = sum over the growing months of Kc x monthly ET0 (FAO-56 idea, monthly simplification).
Inputs : crop_database.csv (Kc), crop_calendars.csv (growing months + stage lengths), a monthly climate table
         with columns rain_mm_mean and et0_mm_mean (mm per month), index = calendar month 1-12.
NOTE   : Kc values and calendars are UNVERIFIED placeholders from DS2. Use for testing, not for claims.
"""
import pandas as pd

def load_crops(db_path="data/agricultural/crop_database.csv",
               cal_path="data/agricultural/crop_calendars.csv"):
    db = pd.read_csv(db_path).set_index("crop_id")
    cal = pd.read_csv(cal_path).set_index("crop_id")
    return db, cal

def growing_months(cal_row):
    """'10;11;3;4' -> [10, 11, 3, 4] (kept in the order the crop actually grows)."""
    return [int(m) for m in str(cal_row["green_cover_months"]).split(";")]

def kc_at(frac, kc_ini, kc_mid, kc_late, stage_fracs):
    """FAO-56 Kc curve: flat initial, ramp up, flat mid, ramp down. frac = position in season (0-1)."""
    f_ini, f_dev, f_mid, f_late = stage_fracs
    if frac < f_ini:
        return kc_ini
    if frac < f_ini + f_dev:
        return kc_ini + (kc_mid - kc_ini) * (frac - f_ini) / f_dev
    if frac < f_ini + f_dev + f_mid:
        return kc_mid
    return kc_mid + (kc_late - kc_mid) * (frac - f_ini - f_dev - f_mid) / f_late

def water_need(crop_id, clim, db, cal):
    c, k = db.loc[crop_id], cal.loc[crop_id]
    months = growing_months(k)
    total = k["initial_days"] + k["development_days"] + k["mid_days"] + k["late_days"]
    fracs = [k["initial_days"] / total, k["development_days"] / total,
             k["mid_days"] / total, k["late_days"] / total]
    need = 0.0
    for i, m in enumerate(months):
        frac = (i + 0.5) / len(months)          # middle of this month within the season
        need += kc_at(frac, c["kc_initial"], c["kc_mid"], c["kc_late"], fracs) * clim.loc[m, "et0_mm_mean"]
    rain = clim.loc[months, "rain_mm_mean"].sum()
    irrigation = max(need - rain, 0.0)
    return {"need_mm": round(need), "rain_mm": round(rain), "irrigation_needed_mm": round(irrigation),
            "rain_cover_score": round(100 * min(rain / need, 1.0), 1)}


# ---------------------------------------------------------------------------
# Rotation level
# ---------------------------------------------------------------------------
# UNCONFIRMED placeholder: midpoint of the Newlands Project allocation range in
# local_irrigation_observations.csv (row O05: 1067-1372 mm). Verify before using in any claim.
WATER_SUPPLY_MM = 1220

def load_rotations(path="data/agricultural/rotation_strategies.csv"):
    return pd.read_csv(path)

def rotation_water(rotation_id, rotations, clim, db, cal, supply_mm=WATER_SUPPLY_MM):
    """Water picture for one rotation, averaged per year over its cycle.

    Every step (one crop in one season) is costed with water_need(); the rotation's yearly irrigation
    need is the cycle total divided by cycle_years. The 0-100 score is the share of the assumed water
    supply that is LEFT after the rotation takes what it needs:  100 * (1 - yearly irrigation / supply).
    Cover crops count: they use water too.
    """
    steps = rotations[rotations["rotation_id"] == rotation_id].sort_values("step")
    cycle = int(steps["cycle_years"].iloc[0])
    per_step = []
    for crop in steps["crop_id"]:
        per_step.append({"crop_id": crop, **water_need(crop, clim, db, cal)})
    need = sum(s["need_mm"] for s in per_step)
    rain = sum(s["rain_mm"] for s in per_step)
    irrigation = sum(s["irrigation_needed_mm"] for s in per_step)
    yearly_irrigation = irrigation / cycle
    return {
        "rotation_id": rotation_id,
        "cycle_years": cycle,
        "yearly_need_mm": round(need / cycle),
        "yearly_rain_mm": round(rain / cycle),
        "yearly_irrigation_mm": round(yearly_irrigation),
        "water_score": round(100 * max(0.0, 1 - yearly_irrigation / supply_mm), 1),
        "steps": per_step,
    }