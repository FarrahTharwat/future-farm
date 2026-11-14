
from pathlib import Path

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

BBOX = (
    LONGITUDE - OFFSET,
    LATITUDE - OFFSET,
    LONGITUDE + OFFSET,
    LATITUDE + OFFSET
)

# ==========================================
# 4. Download and extract
# ==========================================

results = []

for prop in PROPERTIES:

    print(f"\nProcessing property: {prop}")

    url = BASE_URL.format(prop)

    wcs = WebCoverageService(
        url,
        version="1.0.0"
    )

    for depth in DEPTHS:

        layer_name = f"{prop}_{depth}_mean"

        print(f"Downloading {layer_name}...")

        try:
            response = wcs.getCoverage(
                identifier=layer_name,
                crs="urn:ogc:def:crs:EPSG::4326",
                bbox=BBOX,
                resx=0.00225,
                resy=0.00225,
                format="GEOTIFF_INT16"
            )

            # Save original raster
            raster_file = RAW_DIR / f"{layer_name}.tif"

            with open(raster_file, "wb") as file:
                file.write(response.read())

            # Extract pixel at study coordinates
            with rasterio.open(raster_file) as src:

                raw_value = list(
                    src.sample([(LONGITUDE, LATITUDE)])
                )[0][0]

                if src.nodata is not None and raw_value == src.nodata:
                    raw_value = np.nan

                if raw_value == -32768:
                    raw_value = np.nan

            # Convert to physical units
            if pd.isna(raw_value):
                value = np.nan
            else:
                value = raw_value / SCALE_FACTORS[prop]

            results.append({
                "property": prop,
                "depth": depth,
                "raw_value": raw_value,
                "value": value,
                "unit": UNITS[prop],
                "latitude": LATITUDE,
                "longitude": LONGITUDE
            })

            print(f"Extracted value: {value}")

        except Exception as error:

            print(f"Error processing {layer_name}: {error}")

# ==========================================
# 5. Save final dataset
# ==========================================

df = pd.DataFrame(results)

output_file = PROCESSED_DIR / "soil_properties.csv"

df.to_csv(output_file, index=False)

print("\n===================================")
print("SOILGRIDS PROCESSING COMPLETED")
print("===================================")

print("\nDataset:")
print(df)

print("\nDataset shape:", df.shape)

print("\nMissing values:")
print(df.isna().sum())

print("\nSaved CSV:", output_file)
print("Raw rasters folder:", RAW_DIR)
