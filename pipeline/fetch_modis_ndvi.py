"""Download MODIS MOD13Q1 NDVI at the Lattin Farms point."""
from __future__ import annotations

import argparse
import re
import sys
from datetime import date
from pathlib import Path
from typing import Any
from urllib.parse import urlparse

import numpy as np
import pandas as pd
import rasterio
from rasterio.warp import transform

ROOT = Path(__file__).resolve().parents[1]
RAW_DIR = ROOT / "data" / "raw" / "modis"
OUTPUT_CSV = ROOT / "data" / "processed" / "ndvi.csv"
LATITUDE = 39.461142
LONGITUDE = -118.830528
PRODUCT = "MOD13Q1"
VERSION = "061"


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--start", default="2001-01-01")
    parser.add_argument("--end", default="2025-12-31")
    parser.add_argument("--estimate-only", action="store_true")
    return parser.parse_args()


def granule_date(path: Path) -> date:
    match = re.search(r"\.A(\d{4})(\d{3})\.", path.name)
    if not match:
        raise ValueError(f"Cannot read MODIS year/day from {path.name}")
    return date(int(match.group(1)), 1, 1) + pd.Timedelta(days=int(match.group(2)) - 1)


def hdf_name(granule: Any) -> str | None:
    for link in granule.data_links(access="external"):
        name = Path(urlparse(link).path).name
        if name.lower().endswith((".hdf", ".h5", ".hdf5")):
            return name
    return None


def subdataset(container: Any, keyword: str, exclude: str | None = None) -> str:
    matches = [
        item for item in container.subdatasets
        if keyword.lower() in item.lower()
        and (exclude is None or exclude.lower() not in item.lower())
    ]
    if not matches:
        raise ValueError(f"Could not find MODIS subdataset containing {keyword!r}")
    return matches[0]


def sample_point(dataset_path: str, lon: float = LONGITUDE, lat: float = LATITUDE) -> int:
    with rasterio.open(dataset_path) as src:
        xs, ys = transform("EPSG:4326", src.crs, [lon], [lat])
        return int(next(src.sample([(xs[0], ys[0])]))[0])


def extract(path: Path) -> dict[str, object]:
    with rasterio.open(path) as container:
        ndvi_ds = subdataset(container, "NDVI", "quality")
        qa_ds = subdataset(container, "pixel reliability")
    raw_ndvi = sample_point(ndvi_ds)
    reliability = sample_point(qa_ds)
    valid = -2000 <= raw_ndvi <= 10000 and reliability == 0
    return {
        "date": granule_date(path).isoformat(),
        "ndvi": raw_ndvi * 0.0001 if valid else np.nan,
        "ndvi_units": "unitless",
        "pixel_reliability": reliability,
        "quality_label": "good" if valid else "not recommended or missing",
        "product": PRODUCT,
        "product_version": VERSION,
        "latitude": LATITUDE,
        "longitude": LONGITUDE,
        "source_file": path.name,
        "spatial_resolution_m": 250,
        "composite_days": 16,
    }


def main() -> int:
    args = parse_args()
    start, end = date.fromisoformat(args.start), date.fromisoformat(args.end)
    if end < start:
        raise ValueError("--end must be on or after --start")
    import earthaccess

    RAW_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    found = earthaccess.search_data(
        short_name=PRODUCT,
        version=VERSION,
        temporal=(start.isoformat(), end.isoformat()),
        bounding_box=(LONGITUDE - 0.002, LATITUDE - 0.002, LONGITUDE + 0.002, LATITUDE + 0.002),
    )
    missing = []
    size_mb = 0.0
    for granule in found:
        filename = hdf_name(granule)
        if filename is None or not (RAW_DIR / filename).exists():
            missing.append(granule)
            size_mb += float(getattr(granule, "size", 0) or 0)
    print(f"MODIS granules found: {len(found)}; new granules: {len(missing)}; estimated download: {size_mb / 1024:.1f} GB")
    if args.estimate_only:
        return 0
    if missing:
        if input("Type DOWNLOAD to begin: ").strip() != "DOWNLOAD":
            print("Cancelled; no new files downloaded.")
            return 0
        earthaccess.login()
        earthaccess.download(missing, local_path=str(RAW_DIR))

    files = []
    for pattern in ("*.hdf", "*.h5", "*.hdf5"):
        for path in sorted(RAW_DIR.glob(pattern)):
            day = granule_date(path)
            if start <= day <= end:
                files.append(path)
    if not files:
        raise RuntimeError(f"No MODIS granules found under {RAW_DIR} for the requested period")
    frame = pd.DataFrame([extract(path) for path in files])
    frame = frame.drop_duplicates(subset=["date"], keep="last").sort_values("date")
    frame.to_csv(OUTPUT_CSV, index=False, na_rep="")
    print(f"Saved {len(frame)} MODIS composites to {OUTPUT_CSV}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"MODIS NDVI error: {error}", file=sys.stderr)
        raise SystemExit(1)
