"""Query ISRIC SoilGrids for one point. Saves data/processed/soil_properties.csv
Usage: python pipeline/fetch_soilgrids.py --lat 39.47 --lon -118.78
NOTE: written from the SoilGrids REST docs; the API can be slow/rate-limited, so cache the result.
"""
import argparse, requests, pandas as pd

URL = "https://rest.isric.org/soilgrids/v2.0/properties/query"
PROPS = ["soc", "phh2o", "clay", "sand", "silt", "nitrogen"]
DEPTHS = ["0-5cm", "5-15cm", "15-30cm"]

def fetch(lat, lon):
    r = requests.get(URL, params={"lat": lat, "lon": lon, "property": PROPS,
                                  "depth": DEPTHS, "value": "mean"}, timeout=120)
    r.raise_for_status()
    rows = []
    for layer in r.json()["properties"]["layers"]:
        d = layer["unit_measure"].get("d_factor", 1) or 1
        for dep in layer["depths"]:
            v = dep["values"].get("mean")
            rows.append({"property": layer["name"], "depth": dep["label"],
                         "value": None if v is None else v / d,
                         "unit": layer["unit_measure"].get("target_units", "")})
    return pd.DataFrame(rows)

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--lat", type=float, required=True)
    ap.add_argument("--lon", type=float, required=True)
    a = ap.parse_args()
    fetch(a.lat, a.lon).to_csv("data/processed/soil_properties.csv", index=False)
    print("saved soil_properties.csv")
