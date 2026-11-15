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

def effective_rain(p_mm):
    """Share of a month's rain a crop can actually use (USDA-SCS style approximation, monthly mm).
    [UNVERIFIED] Written from memory of the FAO irrigation guidance: check against the FAO source before claiming it.
    In an arid month with little rain it removes only a few percent; in wet months it matters more."""
    if p_mm <= 250:
        return p_mm * (125 - 0.2 * p_mm) / 125
    return 125 + 0.1 * p_mm

DIM = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
CUM = [sum(DIM[:i]) for i in range(12)]            # day-of-year (0-based) on which each month starts

def _month_of(doy):
    doy %= 365
    return max(i for i in range(12) if CUM[i] <= doy) + 1

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
    return kc_mid + (kc_late - kc_mid) * (frac - f_ini - f_dev - f_mid) / f_late if f_late else kc_mid

def season_profile(crop_id, db, cal):
    """[(calendar month, days of the season falling in that month, average Kc in those days)].

    Annual crops and cover crops: walk the season DAY BY DAY using DS2's stage lengths (initial, development,
    mid, late), starting at the planting month (the middle of the planting window if it spans two months),
    and stop at the end of the harvest/termination month.
    Perennial forage (alfalfa): its stage days describe one cutting, so the growth stages are spread evenly
    across the growing months in the calendar instead (cutting-averaged Kc).
    """
    c, k = db.loc[crop_id], cal.loc[crop_id]
    kc_i, kc_m, kc_l = c["kc_initial"], c["kc_mid"], c["kc_late"]
    stages = [k["initial_days"], k["development_days"], k["mid_days"], k["late_days"]]
    total = sum(stages)

    if c["type"] == "perennial_forage":
        months = growing_months(k)
        fr = [x / total for x in stages]
        return [(m, DIM[m - 1], kc_at((i + 0.5) / len(months), kc_i, kc_m, kc_l, fr)) for i, m in enumerate(months)]

    p1, p2 = int(k["plant_start_month"]), int(k["plant_end_month"])
    start = CUM[p1 - 1] + (0 if p1 == p2 else sum(DIM[p1 - 1:p2]) // 2)
    end_doy = CUM[int(k["harvest_end_month"]) - 1] + DIM[int(k["harvest_end_month"]) - 1] - 1
    length = min(int(total), (end_doy - start) % 365 + 1)
    ini, dev, mid, late = stages
    days = {}
    for d in range(length):
        if d < ini:
            kc = kc_i
        elif d < ini + dev:
            kc = kc_i + (kc_m - kc_i) * (d - ini) / dev
        elif d < ini + dev + mid:
            kc = kc_m
        else:
            kc = kc_m + (kc_l - kc_m) * (d - ini - dev - mid) / late if late else kc_m
        m = _month_of(start + d)
        n, tot = days.get(m, (0, 0.0))
        days[m] = (n + 1, tot + kc)
    return [(m, n, tot / n) for m, (n, tot) in days.items()]

def water_need(crop_id, clim, db, cal):
    need = rain = total_rain = 0.0
    for m, n, kc in season_profile(crop_id, db, cal):
        share = n / DIM[m - 1]                              # part of the month the crop is in the field
        need += kc * clim.loc[m, "et0_mm_mean"] * share
        total_rain += clim.loc[m, "rain_mm_mean"] * share
        rain += effective_rain(clim.loc[m, "rain_mm_mean"]) * share   # effective, not total
    irrigation = max(need - rain, 0.0)
    return {"need_mm": round(need), "rain_mm": round(rain), "total_rain_mm": round(total_rain),
            "irrigation_needed_mm": round(irrigation),
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
        # The score is clamped at 0, so report the real shortage separately (a score of 0 alone hides severity)
        "water_deficit_mm": round(max(yearly_irrigation - supply_mm, 0.0)),
        "feasible_within_supply": yearly_irrigation <= supply_mm,
        "steps": per_step,
    }