
# import json
# from pathlib import Path
# from urllib.parse import urlencode
# from urllib.request import urlopen

# import pandas as pd  # type: ignore[reportMissingModuleSource]

# # ==========================================
# # 1. Project location
# # ==========================================

# # Lattin Farms - Fallon, Nevada
# LATITUDE = 39.461142
# LONGITUDE = -118.830528

# START_DATE = "20010101"
# END_DATE = "20251231"

# BASE_URL = "https://power.larc.nasa.gov/api/temporal/daily/point"

# # ==========================================
# # 2. NASA POWER parameters
# # ==========================================

# PARAMETERS = [
#     "T2M",
#     "T2M_MAX",
#     "T2M_MIN",
#     "PRECTOTCORR",
#     "RH2M",
#     "WS2M",
#     "ALLSKY_SFC_SW_DWN"
# ]

# params = {
#     "parameters": ",".join(PARAMETERS),
#     "community": "AG",
#     "longitude": LONGITUDE,
#     "latitude": LATITUDE,
#     "start": START_DATE,
#     "end": END_DATE,
#     "format": "JSON",
#     "time-standard": "UTC"
# }

# # ==========================================
# # 3. Download NASA POWER data
# # ==========================================

# print("Downloading NASA POWER data...")

# url = f"{BASE_URL}?{urlencode(params)}"
# with urlopen(url, timeout=60) as response:
#     data = json.load(response)

# weather_data = data["properties"]["parameter"]

# df = pd.DataFrame(weather_data)

# # Convert date index into a column
# df.index = pd.to_datetime(df.index, format="%Y%m%d")
# df.index.name = "date"
# df = df.reset_index()

# # ==========================================
# # 4. Clean missing values
# # ==========================================

# df[PARAMETERS] = df[PARAMETERS].replace(-999, float("nan"))

# # ==========================================
# # 5. Rename columns
# # ==========================================

# df = df.rename(columns={
#     "T2M": "temperature_avg",
#     "T2M_MAX": "temperature_max",
#     "T2M_MIN": "temperature_min",
#     "PRECTOTCORR": "rainfall",
#     "RH2M": "humidity",
#     "WS2M": "wind_speed",
#     "ALLSKY_SFC_SW_DWN": "solar_radiation"
# })

# # Add location information
# df["latitude"] = LATITUDE
# df["longitude"] = LONGITUDE

# # ==========================================
# # 6. Define output folders
# # ==========================================

# # Project root (works when this file is inside src/data_collection)
# PROJECT_ROOT = Path(__file__).resolve().parents[1]
# raw_dir = PROJECT_ROOT / "data" / "raw" / "nasa_power"
# processed_dir = PROJECT_ROOT / "data" / "processed"

# raw_dir.mkdir(parents=True, exist_ok=True)
# processed_dir.mkdir(parents=True, exist_ok=True)

# # ==========================================
# # 7. Save raw and processed data
# # ==========================================

# raw_file = raw_dir / "nasa_power_raw.json"
# output_file = processed_dir / "weather_daily.csv"

# with open(raw_file, "w", encoding="utf-8") as file:
#     json.dump(data, file, indent=4)

# df.to_csv(output_file, index=False)

# # ==========================================
# # 8. Display results
# # ==========================================

# print("\nNASA POWER data downloaded successfully!")

# print("\nFirst 5 rows:")
# print(df.head())

# print("\nDataset information:")
# df.info()

# print("\nDataset shape:", df.shape)

# print("\nMissing values:")
# print(df.isna().sum())

# print("\nSaved raw file:", raw_file)
# print("Saved processed file:", output_file)



import json
from pathlib import Path
from urllib.parse import urlencode
from urllib.request import urlopen

import pandas as pd  # type: ignore[reportMissingModuleSource]

# ==========================================
# 1. Project location
# ==========================================

# Lattin Farms - Fallon, Nevada
LATITUDE = 39.461142
LONGITUDE = -118.830528

START_DATE = "20010101"
END_DATE = "20251231"

BASE_URL = "https://power.larc.nasa.gov/api/temporal/daily/point"

# ==========================================
# 2. NASA POWER parameters
# ==========================================

PARAMETERS = [
    "T2M",
    "T2M_MAX",
    "T2M_MIN",
    "PRECTOTCORR",
    "RH2M",
    "WS2M",
    "ALLSKY_SFC_SW_DWN"
]

params = {
    "parameters": ",".join(PARAMETERS),
    "community": "AG",
    "longitude": LONGITUDE,
    "latitude": LATITUDE,
    "start": START_DATE,
    "end": END_DATE,
    "format": "JSON",
    "time-standard": "UTC"
}

# ==========================================
# 3. Download NASA POWER data
# ==========================================

print("Downloading NASA POWER data...")

url = f"{BASE_URL}?{urlencode(params)}"
with urlopen(url, timeout=60) as response:
    data = json.load(response)

weather_data = data["properties"]["parameter"]

df = pd.DataFrame(weather_data)

# Convert date index into a column
df.index = pd.to_datetime(df.index, format="%Y%m%d")
df.index.name = "date"
df = df.reset_index()

# ==========================================
# 4. Clean missing values
# ==========================================

df[PARAMETERS] = df[PARAMETERS].replace(-999, float("nan"))

# ==========================================
# 5. Rename columns
# ==========================================

df = df.rename(columns={
    "T2M": "temperature_avg",
    "T2M_MAX": "temperature_max",
    "T2M_MIN": "temperature_min",
    "PRECTOTCORR": "rainfall",
    "RH2M": "humidity",
    "WS2M": "wind_speed",
    "ALLSKY_SFC_SW_DWN": "solar_radiation"
})

# Add location information
df["latitude"] = LATITUDE
df["longitude"] = LONGITUDE

# ==========================================
# 6. Define output folders
# ==========================================

# Project root (works when this file is inside src/data_collection)
PROJECT_ROOT = Path(__file__).resolve().parents[1]
raw_dir = PROJECT_ROOT / "data" / "raw" / "nasa_power"
processed_dir = PROJECT_ROOT / "data" / "processed"

raw_dir.mkdir(parents=True, exist_ok=True)
processed_dir.mkdir(parents=True, exist_ok=True)

# ==========================================
# 7. Save raw and processed data
# ==========================================

raw_file = raw_dir / "nasa_power_raw.json"
output_file = processed_dir / "weather_daily.csv"

with open(raw_file, "w", encoding="utf-8") as file:
    json.dump(data, file, indent=4)

df.to_csv(output_file, index=False)

# ==========================================
# 8. Display results
# ==========================================

print("\nNASA POWER data downloaded successfully!")

print("\nFirst 5 rows:")
print(df.head())

print("\nDataset information:")
df.info()

print("\nDataset shape:", df.shape)

print("\nMissing values:")
print(df.isna().sum())

print("\nSaved raw file:", raw_file)
print("Saved processed file:", output_file)

