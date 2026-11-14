def water_score(need_mm, season_months, clim):
    """0-100: how much of the crop's water need typical rainfall covers."""
    rain = clim.loc[season_months, "rain_mm_mean"].sum()
    shortfall = max(need_mm - rain, 0)
    return round(100 * (1 - shortfall / need_mm), 1), rain, shortfall