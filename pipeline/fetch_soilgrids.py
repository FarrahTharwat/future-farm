
from pathlib import Path
import time
import os

import numpy as np
import pandas as pd
import rasterio
from owslib.wcs import WebCoverageService

# ==========================================
# 1. Location
# ==========================================

LATITUDE = 39.461142
LONGITUDE = -118.830528

# ==========================================
# 2. Project directories
# ==========================================

PROJECT_ROOT = Path(__file__).resolve().parents[1]

RAW_DIR = PROJECT_ROOT / "data" / "raw" / "soilgrids"
PROCESSED_DIR = PROJECT_ROOT / "data" / "processed"

RAW_DIR.mkdir(parents=True, exist_ok=True)
PROCESSED_DIR.mkdir(parents=True, exist_ok=True)

# ==========================================
# 3. SoilGrids configuration
# ==========================================

PROPERTIES = [
    "bdod",
    "clay",
    "sand",
    "phh2o",
    "soc",
    "nitrogen"
]

DEPTHS = [
    "0-5cm",
    "5-15cm",
    "15-30cm",
    "30-60cm",
    "60-100cm",
    "100-200cm"
]

SCALE_FACTORS = {
    "bdod": 100,
    "clay": 10,
    "sand": 10,
    "phh2o": 10,
    "soc": 10,
    "nitrogen": 100
}

UNITS = {
    "bdod": "kg/dm3",
    "clay": "%",
    "sand": "%",
    "phh2o": "pH",
    "soc": "g/kg",
    "nitrogen": "g/kg"
}

BASE_URL = "https://maps.isric.org/mapserv?map=/map/{}.map"

# Small bounding box around study location
OFFSET = 0.005
REQUEST_TIMEOUT_SECONDS = 120
MAX_ATTEMPTS = 4
RETRY_DELAY_SECONDS = 5

BBOX = (
    LONGITUDE - OFFSET,
    LATITUDE - OFFSET,
    LONGITUDE + OFFSET,
    LATITUDE + OFFSET
)

# ==========================================
# 4. Download, retry, and extract
# ==========================================

def extract_pixel(raster_file: Path):
    with rasterio.open(raster_file) as src:
        raw_value = list(src.sample([(LONGITUDE, LATITUDE)]))[0][0]
        if src.nodata is not None and raw_value == src.nodata:
            return np.nan
        if raw_value == -32768:
            return np.nan
        return float(raw_value)


output_file = PROCESSED_DIR / "soil_properties.csv"
results_by_key = {}

# Keep successful values from a previous run so rerunning only retries missing layers.
if output_file.exists():
    previous = pd.read_csv(output_file)
    for row in previous.itertuples(index=False):
        if pd.notna(row.value):
            results_by_key[(row.property, row.depth)] = {
                "property": row.property,
                "depth": row.depth,
                "raw_value": row.raw_value,
                "value": row.value,
                "unit": row.unit,
                "latitude": row.latitude,
                "longitude": row.longitude,
            }

failed_layers = []

for prop in PROPERTIES:
    print(f"\nProcessing property: {prop}")
    pending_depths = [depth for depth in DEPTHS if (prop, depth) not in results_by_key]
    if not pending_depths:
        print("All depth values already exist; skipping this property.")
        continue

    url = BASE_URL.format(prop)
    depths_to_download = [
        depth for depth in pending_depths
        if not (RAW_DIR / f"{prop}_{depth}_mean.tif").exists()
    ]
    wcs = None
    if depths_to_download:
        for connection_attempt in range(1, MAX_ATTEMPTS + 1):
            try:
                wcs = WebCoverageService(
                    url,
                    version="1.0.0",
                    timeout=REQUEST_TIMEOUT_SECONDS,
                )
                break
            except Exception as error:
                if connection_attempt < MAX_ATTEMPTS:
                    delay = RETRY_DELAY_SECONDS * connection_attempt
                    print(
                        f"WCS connection attempt {connection_attempt}/{MAX_ATTEMPTS} "
                        f"failed for {prop}: {error}. Retrying in {delay} seconds..."
                    )
                    time.sleep(delay)
                else:
                    print(f"Could not connect to SoilGrids for {prop}: {error}")
                    failed_layers.extend(f"{prop}_{depth}_mean" for depth in depths_to_download)
        if wcs is None:
            continue
    else:
        print("All GeoTIFFs are already downloaded; extracting from local files.")

    for depth in DEPTHS:
        key = (prop, depth)
        if key in results_by_key:
            print(f"Already available, keeping previous value: {prop}_{depth}_mean")
            continue

        layer_name = f"{prop}_{depth}_mean"
        raster_file = RAW_DIR / f"{layer_name}.tif"
        print(f"Downloading {layer_name}...")

        for attempt in range(1, MAX_ATTEMPTS + 1):
            try:
                if not raster_file.exists():
                    response = wcs.getCoverage(
                        identifier=layer_name,
                        crs="urn:ogc:def:crs:EPSG::4326",
                        bbox=BBOX,
                        resx=0.00225,
                        resy=0.00225,
                        format="GEOTIFF_INT16",
                    )
                    raster_bytes = response.read()
                    if not raster_bytes:
                        raise ValueError("The WCS response was empty")
                    temporary_file = raster_file.with_name(raster_file.stem + ".download.tif")
                    temporary_file.write_bytes(raster_bytes)
                    # Validate the download before replacing a previous file.
                    with rasterio.open(temporary_file) as src:
                        if src.count < 1 or src.width < 1 or src.height < 1:
                            raise ValueError("The downloaded GeoTIFF is empty")
                    os.replace(temporary_file, raster_file)

                raw_value = extract_pixel(raster_file)
                value = np.nan if pd.isna(raw_value) else raw_value / SCALE_FACTORS[prop]
                results_by_key[key] = {
                    "property": prop,
                    "depth": depth,
                    "raw_value": raw_value,
                    "value": value,
                    "unit": UNITS[prop],
                    "latitude": LATITUDE,
                    "longitude": LONGITUDE,
                }
                print(f"Extracted value: {value}")
                break
            except Exception as error:
                if attempt < MAX_ATTEMPTS:
                    delay = RETRY_DELAY_SECONDS * attempt
                    print(
                        f"Attempt {attempt}/{MAX_ATTEMPTS} failed for {layer_name}: {error} "
                        f"Retrying in {delay} seconds..."
                    )
                    time.sleep(delay)
                else:
                    print(f"Failed after {MAX_ATTEMPTS} attempts for {layer_name}: {error}")
                    failed_layers.append(layer_name)

# Write all successful values, retaining cached values from earlier runs.
ordered_results = [
    results_by_key[(prop, depth)]
    for prop in PROPERTIES
    for depth in DEPTHS
    if (prop, depth) in results_by_key
]
df = pd.DataFrame(ordered_results)
df.to_csv(output_file, index=False)

expected = len(PROPERTIES) * len(DEPTHS)
print("\n===================================")
if len(df) == expected and df["value"].notna().all():
    print("SOILGRIDS PROCESSING COMPLETED")
else:
    print("SOILGRIDS PROCESSING INCOMPLETE")
print("===================================")
print(f"Saved {df['value'].notna().sum()} valid values out of {expected} to {output_file}")
if failed_layers:
    print("Rerun the script to retry these layers:")
    for layer in failed_layers:
        print(f" - {layer}")
print(f"Raw rasters folder: {RAW_DIR}")
