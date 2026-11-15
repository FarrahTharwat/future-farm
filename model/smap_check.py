"""Does satellite soil moisture (SMAP) agree with what our water model assumes?   Run from the repo root:
    python model/smap_check.py

Two honest, simple checks (no fitting, no claims of validation):
 1. Seasonal shape: average SMAP soil moisture per calendar month vs. the atmospheric water gap that month
    (ET0 minus effective rain, in mm). If the model's picture of "dry months" is right, soil should be driest when the gap is biggest.
 2. Year-to-year: after removing the normal seasonal cycle, are wetter-than-usual months (more rain than usual) also
    wetter-than-usual in SMAP?
IMPORTANT: SMAP is ~36 km, regional, SURFACE moisture (top ~5 cm). Fallon is irrigated land inside a desert, so irrigation
can keep soil wet when rain is near zero. A weak match is an expected result, not an error: it supports the idea that
irrigation, not rain, controls water supply here. Report whatever comes out.
"""
import json, os, sys
import pandas as pd

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from water_need import effective_rain

SMAP_FILE = "data/processed/soil_moisture.csv"
MAX_DISTANCE_KM = 15      # the pipeline picks the nearest valid cell per day; far-away cells mix in a different footprint
MIN_DAYS_PER_MONTH = 5

def spearman(a, b):
    """Spearman correlation = Pearson correlation of the ranks (so no scipy is needed)."""
    return a.rank().corr(b.rank())

def load_smap():
    d = pd.read_csv(SMAP_FILE, parse_dates=["date"])
    n0 = len(d)
    d = d[(d["quality_label"] == "recommended") & (d["soil_moisture_m3_m3"].notna())]
    n1 = len(d)
    d = d[d["distance_km"] <= MAX_DISTANCE_KM]
    info = {"rows_in_file": n0, "rows_recommended_quality": n1, "rows_used_near_cell": len(d),
            "date_min": str(d["date"].min().date()), "date_max": str(d["date"].max().date())}
    return d, info

def monthly_smap(d):
    g = d.groupby([d["date"].dt.year.rename("year"), d["date"].dt.month.rename("month")])["soil_moisture_m3_m3"]
    m = pd.DataFrame({"smap": g.mean(), "n_days": g.count()})
    return m[m["n_days"] >= MIN_DAYS_PER_MONTH].reset_index()

def monthly_rain(years):
    p = pd.read_csv("data/raw/nasa_power/power_daily.csv", index_col=0, parse_dates=True)
    p = p[p.index.year.isin(years)]
    r = p["PRECTOTCORR"].groupby([p.index.year.rename("year"), p.index.month.rename("month")]).sum()
    return r.rename("rain_mm").reset_index()

def main():
    d, info = load_smap()
    m = monthly_smap(d)
    et0 = pd.read_csv("data/processed/et0_monthly.csv", index_col="month")["et0_mm_mean"]
    clim = pd.read_csv("data/processed/environmental_features.csv", index_col="month")["rain_mm_mean"]

    # 1. seasonal shape
    seas = m.groupby("month")["smap"].mean().to_frame("smap_mean")
    seas["water_gap_mm"] = [et0[k] - effective_rain(clim[k]) for k in seas.index]
    shape_corr = spearman(seas["smap_mean"], seas["water_gap_mm"])

    # 2. year-to-year anomalies
    r = monthly_rain(m["year"].unique())
    j = m.merge(r, on=["year", "month"])
    for c in ("smap", "rain_mm"):
        j[c + "_anom"] = j[c] - j.groupby("month")[c].transform("mean")
    anom_corr = spearman(j["smap_anom"], j["rain_mm_anom"])
    j = j.sort_values(["year", "month"])
    j["rain_prev"] = j["rain_mm_anom"].shift(1)           # does last month's rain matter (soil dries slowly)?
    lag_corr = spearman(j["smap_anom"], j["rain_prev"])

    out = {"data": info, "months_used": int(len(j)),
           "seasonal_shape_spearman_smap_vs_water_gap": round(float(shape_corr), 2),
           "anomaly_spearman_smap_vs_rain_same_month": round(float(anom_corr), 2),
           "anomaly_spearman_smap_vs_rain_previous_month": round(float(lag_corr), 2),
           "seasonal_table": seas.round(3).reset_index().to_dict("records"),
           "caveats": ["SMAP is ~36 km, regional, surface (about 5 cm) moisture, not root-zone",
                       "Fallon is irrigated: irrigation can keep soil moist when rain is near zero",
                       "Rain comes from NASA POWER, a coarse grid that looks wetter than local stations",
                       f"Months with fewer than {MIN_DAYS_PER_MONTH} valid SMAP days are dropped",
                       "A correlation is not validation of the water model"]}
    os.makedirs("outputs", exist_ok=True)
    json.dump(out, open("outputs/smap_check.json", "w"), indent=2)

    print("SMAP data:", info)
    print(f"months compared: {len(j)}")
    print("\nSeasonal shape (average by calendar month):")
    print(seas.round(3).to_string())
    print(f"\nSpearman, SMAP vs water gap across the 12 months: {shape_corr:.2f}  (expect negative if dry months have a big gap)")
    print(f"Spearman, SMAP anomaly vs rain anomaly, same month: {anom_corr:.2f}")
    print(f"Spearman, SMAP anomaly vs rain anomaly, previous month: {lag_corr:.2f}")
    print("Saved outputs/smap_check.json")

if __name__ == "__main__":
    main()