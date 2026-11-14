"""Turn data/processed/weather.csv (monthly, from fetch_power.py) into environmental_features.csv.
Output: one row per calendar month (the 'typical year') plus annual totals, so DS3 can match crop seasons.
Usage: python pipeline/build_environmental_features.py
"""
import pandas as pd

def monthly_climatology(weather):
    w = weather.copy()
    w.index = pd.to_datetime(w.index)
    g = w.groupby(w.index.month)
    out = pd.DataFrame({
        "rain_mm_mean": g["PRECTOTCORR"].mean(),
        "rain_mm_std": g["PRECTOTCORR"].std(),
        "temp_c_mean": g["T2M"].mean(),
        "temp_max_c_mean": g["T2M_MAX"].mean(),
        "humidity_pct_mean": g["RH2M"].mean(),
        "solar_mean": g["ALLSKY_SFC_SW_DWN"].mean(),
    })
    out.index.name = "month"
    return out

def annual_summary(weather):
    w = weather.copy(); w.index = pd.to_datetime(w.index)
    yearly = w.groupby(w.index.year)
    return {"annual_rainfall_mm": float(yearly["PRECTOTCORR"].sum().mean()),
            "mean_temperature_c": float(w["T2M"].mean())}

def heat_days(daily, threshold_c, months):
    """Average number of days per year above threshold within the given months (needs power_daily.csv)."""
    d = daily.copy(); d.index = pd.to_datetime(d.index)
    d = d[d.index.month.isin(months)]
    return float((d["T2M_MAX"] > threshold_c).groupby(d.index.year).sum().mean())

if __name__ == "__main__":
    weather = pd.read_csv("data/processed/weather.csv", index_col=0)
    monthly_climatology(weather).to_csv("data/processed/environmental_features.csv")
    print(annual_summary(weather))
