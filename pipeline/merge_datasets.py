# # 

# """Merge daily weather, SMAP observations, and static SoilGrids properties."""
# from __future__ import annotations

# from pathlib import Path

# import pandas as pd

# ROOT = Path(__file__).resolve().parents[1]
# PROCESSED = ROOT / "data" / "processed"
# WEATHER_FILE = PROCESSED / "weather_daily.csv"
# SMAP_FILE = PROCESSED / "soil_moisture.csv"
# SOIL_FILE = PROCESSED / "soil_properties.csv"
# OUTPUT_FILE = PROCESSED / "farm_daily_dataset.csv"

# UNIT_SUFFIX = {"kg/dm3": "kg_dm3", "%": "pct", "pH": "pH", "g/kg": "g_kg"}


# def main() -> None:
#     for path in (WEATHER_FILE, SMAP_FILE, SOIL_FILE):
#         if not path.exists():
#             raise FileNotFoundError(f"Required input file is missing: {path}")

#     weather = pd.read_csv(WEATHER_FILE, parse_dates=["date"])
#     # The long POWER series starts in 2001; keep this merged file in the
#     # 2016-2025 interval shared with the project's SMAP data.
#     weather = weather[weather["date"].between("2016-01-01", "2025-12-31")].copy()
#     smap = pd.read_csv(SMAP_FILE, parse_dates=["date"])
#     soil = pd.read_csv(SOIL_FILE)

#     if weather["date"].duplicated().any():
#         raise ValueError("weather_daily.csv contains duplicate dates")
#     if smap["date"].duplicated().any():
#         raise ValueError("soil_moisture.csv contains duplicate dates")
#     if soil[["property", "depth"]].duplicated().any():
#         raise ValueError("soil_properties.csv contains duplicate property/depth rows")
#     if soil["value"].isna().any():
#         raise ValueError("soil_properties.csv has missing static soil values")

#     smap_columns = [
#         "date", "soil_moisture_m3_m3", "pass", "grid_latitude", "grid_longitude",
#         "distance_km", "retrieval_quality_flag", "quality_label", "source_file",
#         "product_version",
#     ]
#     smap_daily = smap[smap_columns].rename(
#         columns={
#             "pass": "smap_pass",
#             "grid_latitude": "smap_grid_latitude",
#             "grid_longitude": "smap_grid_longitude",
#             "distance_km": "smap_distance_km",
#             "retrieval_quality_flag": "smap_retrieval_quality_flag",
#             "quality_label": "smap_quality_label",
#             "source_file": "smap_source_file",
#             "product_version": "smap_product_version",
#         }
#     )

#     soil_features: dict[str, float] = {}
#     for row in soil.itertuples(index=False):
#         depth = str(row.depth).replace("cm", "").replace("-", "_")
#         unit = UNIT_SUFFIX.get(str(row.unit), "value")
#         soil_features[f"soil_{row.property}_{depth}_{unit}"] = float(row.value)
#     if len(soil_features) != len(soil):
#         raise ValueError("SoilGrids feature names are not unique")

#     daily = weather.merge(smap_daily, on="date", how="left", validate="one_to_one")
#     for name, value in soil_features.items():
#         daily[name] = value

#     daily = daily.sort_values("date").reset_index(drop=True)
#     daily.to_csv(OUTPUT_FILE, index=False, na_rep="")

#     print(f"Saved {len(daily)} daily rows and {len(daily.columns)} columns to {OUTPUT_FILE}")
#     print(f"SMAP observations joined: {daily['soil_moisture_m3_m3'].notna().sum()} of {len(daily)} days")
#     print(f"Static soil properties included: {len(soil_features)} columns")


# if __name__ == "__main__":
#     main()


# 

"""Merge daily weather, SMAP observations, and static SoilGrids properties."""
from __future__ import annotations

from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
PROCESSED = ROOT / "data" / "processed"
WEATHER_FILE = PROCESSED / "weather_daily.csv"
SMAP_FILE = PROCESSED / "soil_moisture.csv"
SOIL_FILE = PROCESSED / "soil_properties.csv"
OUTPUT_FILE = PROCESSED / "farm_daily_dataset.csv"

UNIT_SUFFIX = {"kg/dm3": "kg_dm3", "%": "pct", "pH": "pH", "g/kg": "g_kg"}


def main() -> None:
    for path in (WEATHER_FILE, SMAP_FILE, SOIL_FILE):
        if not path.exists():
            raise FileNotFoundError(f"Required input file is missing: {path}")

    weather = pd.read_csv(WEATHER_FILE, parse_dates=["date"])
    # The long POWER series starts in 2001; keep this merged file in the
    # 2016-2025 interval shared with the project's SMAP data.
    weather = weather[weather["date"].between("2016-01-01", "2025-12-31")].copy()
    smap = pd.read_csv(SMAP_FILE, parse_dates=["date"])
    soil = pd.read_csv(SOIL_FILE)

    if weather["date"].duplicated().any():
        raise ValueError("weather_daily.csv contains duplicate dates")
    if smap["date"].duplicated().any():
        raise ValueError("soil_moisture.csv contains duplicate dates")
    if soil[["property", "depth"]].duplicated().any():
        raise ValueError("soil_properties.csv contains duplicate property/depth rows")
    if soil["value"].isna().any():
        raise ValueError("soil_properties.csv has missing static soil values")

    smap_columns = [
        "date", "soil_moisture_m3_m3", "pass", "grid_latitude", "grid_longitude",
        "distance_km", "retrieval_quality_flag", "quality_label", "source_file",
        "product_version",
    ]
    smap_daily = smap[smap_columns].rename(
        columns={
            "pass": "smap_pass",
            "grid_latitude": "smap_grid_latitude",
            "grid_longitude": "smap_grid_longitude",
            "distance_km": "smap_distance_km",
            "retrieval_quality_flag": "smap_retrieval_quality_flag",
            "quality_label": "smap_quality_label",
            "source_file": "smap_source_file",
            "product_version": "smap_product_version",
        }
    )

    soil_features: dict[str, float] = {}
    for row in soil.itertuples(index=False):
        depth = str(row.depth).replace("cm", "").replace("-", "_")
        unit = UNIT_SUFFIX.get(str(row.unit), "value")
        soil_features[f"soil_{row.property}_{depth}_{unit}"] = float(row.value)
    if len(soil_features) != len(soil):
        raise ValueError("SoilGrids feature names are not unique")

    daily = weather.merge(smap_daily, on="date", how="left", validate="one_to_one")
    for name, value in soil_features.items():
        daily[name] = value

    daily = daily.sort_values("date").reset_index(drop=True)
    daily.to_csv(OUTPUT_FILE, index=False, na_rep="")

    print(f"Saved {len(daily)} daily rows and {len(daily.columns)} columns to {OUTPUT_FILE}")
    print(f"SMAP observations joined: {daily['soil_moisture_m3_m3'].notna().sum()} of {len(daily)} days")
    print(f"Static soil properties included: {len(soil_features)} columns")


if __name__ == "__main__":
    main()
