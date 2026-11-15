"""Month-by-month calendar of what is in each field. Same inputs as soil_health.py (DS2's calendars + rotations)."""

def field_timeline(rotation_id, rotations, cal):
    steps = rotations[rotations["rotation_id"] == rotation_id].sort_values("step")
    cycle = int(steps["cycle_years"].iloc[0])
    grid = {(y, m): None for y in range(1, cycle + 1) for m in range(1, 13)}
    for _, s in steps.iterrows():
        k = cal.loc[s["crop_id"]]
        months = [int(m) for m in str(k["green_cover_months"]).split(";")]
        crosses = str(k["crosses_year"]).strip().lower() == "yes"
        y = int(s["year"])
        nxt = y % cycle + 1
        for m in months:
            later = crosses and m < int(k["plant_start_month"])
            grid[(nxt if later else y, m)] = s["crop_id"]
    return [{"cycle_year": y, "month": m, "crop": c, "green_cover": c is not None}
            for (y, m), c in sorted(grid.items())]