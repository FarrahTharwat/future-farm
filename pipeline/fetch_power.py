"""Download daily NASA POWER data for one point and save raw + monthly CSVs.
Usage: python pipeline/fetch_power.py --lat 39.47 --lon -118.78 --start 2001 --end 2025
NOTE: written from the POWER API docs; run it once and check the output.
"""
import argparse, requests, pandas as pd

PARAMS = ["T2M", "T2M_MAX", "PRECTOTCORR", "RH2M", "ALLSKY_SFC_SW_DWN"]
URL = "https://power.larc.nasa.gov/api/temporal/daily/point"

def fetch(lat, lon, start, end):
    r = requests.get(URL, params={
        "parameters": ",".join(PARAMS), "community": "AG",
        "longitude": lon, "latitude": lat,
        "start": f"{start}0101", "end": f"{end}1231", "format": "JSON"}, timeout=180)
    r.raise_for_status()
    data = r.json()["properties"]["parameter"]
    df = pd.DataFrame(data)
    df.index = pd.to_datetime(df.index, format="%Y%m%d")
    return df.replace(-999, pd.NA).astype(float)   # -999 = POWER's missing value

def monthly(df):
    agg = {"T2M": "mean", "T2M_MAX": "mean", "PRECTOTCORR": "sum",
           "RH2M": "mean", "ALLSKY_SFC_SW_DWN": "mean"}
    return df.resample("MS").agg(agg)

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--lat", type=float, required=True)
    ap.add_argument("--lon", type=float, required=True)
    ap.add_argument("--start", type=int, default=2001)
    ap.add_argument("--end", type=int, default=2025)
    a = ap.parse_args()
    daily = fetch(a.lat, a.lon, a.start, a.end)
    daily.to_csv("data/raw/nasa_power/power_daily.csv")
    monthly(daily).to_csv("data/processed/weather.csv")
    print("saved", len(daily), "daily rows")
