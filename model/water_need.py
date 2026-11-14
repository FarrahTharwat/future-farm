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
