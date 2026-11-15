# 

"""Fetch SMAP SPL3SMP_E v006 soil moisture for a date range.

Requests an NSIDC Harmony spatial subset around Lattin Farms instead of downloading
full global granules. Start with a short 2025 test period before requesting years.
"""
from __future__ import annotations

import argparse
import re
import sys
from datetime import date, timedelta
from pathlib import Path
from typing import Any
from urllib.parse import urlparse

import numpy as np
import pandas as pd

PRODUCT_SHORT_NAME = "SPL3SMP_E"
PRODUCT_VERSION = "006"
FARM_LATITUDE = 39.461142
FARM_LONGITUDE = -118.830528
FILL_VALUE = -9999.0
QUALITY_FILL_VALUE = 65534
RECOMMENDED_FLAGS = {0, 8}
ROOT = Path(__file__).resolve().parents[1]
RAW_DIR = ROOT / "data" / "raw" / "smap"
OUTPUT_CSV = ROOT / "data" / "processed" / "soil_moisture.csv"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--start", default="2025-01-01", help="Start date, YYYY-MM-DD (default: test week)")
    parser.add_argument("--end", default="2025-01-07", help="End date inclusive, YYYY-MM-DD (default: test week)")
    parser.add_argument("--estimate-only", action="store_true", help="Count granules without downloading")
    parser.add_argument("--allow-long-range", action="store_true", help="Allow requests longer than 31 days")
    parser.add_argument("--buffer-degrees", type=float, default=0.25, help="Half-width of farm bounding box in degrees (default: 0.25)")
    return parser.parse_args()


def month_ranges(start: date, end: date):
    month = date(start.year, start.month, 1)
    while month <= end:
        next_month = date(month.year + (month.month == 12), month.month % 12 + 1, 1)
        yield max(start, month), min(end, next_month - timedelta(days=1))
        month = next_month


def granule_date(path: Path) -> date:
    match = re.search(r"_(\d{8})(?:T|_|\.)", path.name)
    if not match:
        raise ValueError(f"Cannot find date in granule filename: {path.name}")
    value = match.group(1)
    return date(int(value[:4]), int(value[4:6]), int(value[6:8]))


def granule_filename(granule: Any) -> str | None:
    for link in granule.data_links(access="external"):
        name = Path(urlparse(link).path).name
        if name.lower().endswith((".h5", ".hdf5")):
            return name
    return None


def haversine_km(latitude: np.ndarray, longitude: np.ndarray) -> np.ndarray:
    lat1 = np.radians(FARM_LATITUDE)
    lat2 = np.radians(latitude)
    dlat = lat2 - lat1
    dlon = np.radians(longitude - FARM_LONGITUDE)
    a = np.sin(dlat / 2) ** 2 + np.cos(lat1) * np.cos(lat2) * np.sin(dlon / 2) ** 2
    return 6371.0088 * 2 * np.arcsin(np.sqrt(np.clip(a, 0, 1)))


def read_var(group: Any, name: str) -> np.ndarray:
    if name not in group:
        raise KeyError(f"Required variable {group.name}/{name} is missing")
    return np.asarray(group[name][...])


def best_cell(h5: Any) -> dict[str, object] | None:
    candidates = []
    for pass_name, group_name, suffix in (
        ("AM", "Soil_Moisture_Retrieval_Data_AM", ""),
        ("PM", "Soil_Moisture_Retrieval_Data_PM", "_pm"),
    ):
        if group_name not in h5:
            continue
        group = h5[group_name]
        moisture = read_var(group, f"soil_moisture_dca{suffix}").astype(float)
        quality = read_var(group, f"retrieval_qual_flag_dca{suffix}")
        lat = read_var(group, f"latitude{suffix}").astype(float)
        lon = read_var(group, f"longitude{suffix}").astype(float)
        if not (moisture.shape == quality.shape == lat.shape == lon.shape):
            raise ValueError(f"Unexpected variable dimensions in {group.name}")
        valid = (
            np.isfinite(moisture) & (moisture != FILL_VALUE)
            & (moisture >= 0) & (moisture <= 1)
            & np.isfinite(lat) & np.isfinite(lon)
            & (lat >= -90) & (lat <= 90) & (lon >= -180) & (lon <= 180)
            & (quality != QUALITY_FILL_VALUE) & np.isin(quality, list(RECOMMENDED_FLAGS))
        )
        if not np.any(valid):
            continue
        distances = haversine_km(lat[valid], lon[valid])
        flat = np.flatnonzero(valid)[int(np.argmin(distances))]
        row, col = np.unravel_index(flat, moisture.shape)
        candidates.append({
            "pass": pass_name, "grid_latitude": float(lat[row, col]),
            "grid_longitude": float(lon[row, col]),
            "distance_km": float(haversine_km(lat[row, col], lon[row, col])),
            "soil_moisture_m3_m3": float(moisture[row, col]),
            "retrieval_quality_flag": int(quality[row, col]),
            "quality_label": "recommended",
        })
    return min(candidates, key=lambda row: row["distance_km"]) if candidates else None


def extract_granule(path: Path) -> dict[str, object]:
    try:
        import h5py
    except ImportError as exc:
        raise ImportError("Install HDF5 support with: python -m pip install h5py") from exc
    with h5py.File(path, "r") as h5:
        cell = best_cell(h5)
    result: dict[str, object] = {
        "date": granule_date(path).isoformat(),
        "farm_latitude": FARM_LATITUDE, "farm_longitude": FARM_LONGITUDE,
        "product": PRODUCT_SHORT_NAME, "product_version": PRODUCT_VERSION,
        "soil_moisture_units": "m3/m3 (volumetric water content)",
        "source_file": path.name,
        "spatial_note": "9 km grid posting; ~36 km native radiometer footprint; regional indicator",
    }
    if cell is None:
        result.update({
            "pass": None, "grid_latitude": np.nan, "grid_longitude": np.nan,
            "distance_km": np.nan, "soil_moisture_m3_m3": np.nan,
            "retrieval_quality_flag": np.nan,
            "quality_label": "no recommended-quality retrieval",
        })
    else:
        result.update(cell)
    return result


def main() -> int:
    args = parse_args()
    start, end = date.fromisoformat(args.start), date.fromisoformat(args.end)
    if end < start:
        raise ValueError("--end must be on or after --start")
    if args.buffer_degrees <= 0 or args.buffer_degrees > 2:
        raise ValueError("--buffer-degrees must be greater than 0 and at most 2")
    if (end - start).days > 30 and not args.allow_long_range:
        raise ValueError("Requests over 31 days require --allow-long-range; test a short period first")

    import earthaccess
    from harmony import BBox, CapabilitiesRequest, Client, Collection, Request

    RAW_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    print(f"Searching {PRODUCT_SHORT_NAME} v{PRODUCT_VERSION}: {start} to {end}...")
    granules = earthaccess.search_data(
        short_name=PRODUCT_SHORT_NAME, version=PRODUCT_VERSION,
        temporal=(start.isoformat(), f"{end.isoformat()}T23:59:59"),
    )
    if not granules:
        raise RuntimeError("No SMAP granules found for the requested dates")

    by_month = {}
    for granule in granules:
        name = granule_filename(granule)
        if name:
            day = granule_date(Path(name))
            if start <= day <= end:
                by_month.setdefault((day.year, day.month), {})[name] = granule
    existing_dates = set()
    subset_files = list(RAW_DIR.glob("*subsetted*.nc4")) + list(RAW_DIR.glob("*subsetted*.h5")) + list(RAW_DIR.glob("*subsetted*.hdf5"))
    for path in subset_files:
        try:
            existing_dates.add(granule_date(path))
        except ValueError:
            pass
    pending_by_month = {}
    for ym, items in by_month.items():
        for name, granule in items.items():
            if granule_date(Path(name)) not in existing_dates:
                pending_by_month.setdefault(ym, []).append(name)

    count = sum(map(len, pending_by_month.values()))
    print(f"Granules in date range: {sum(map(len, by_month.values()))}; subset files already present: {len(existing_dates)}; to request: {count}.")
    print(f"Requested area: {args.buffer_degrees:g} degree around the farm ({FARM_LATITUDE}, {FARM_LONGITUDE}).")
    if args.estimate_only:
        print("This is a subset request; full-granule transfer sizes do not apply.")
        return 0

    if count:
        if input("Type DOWNLOAD to request spatially subsetted granules: ").strip() != "DOWNLOAD":
            print("Cancelled; nothing downloaded.")
            return 0
        earthaccess.login()
        # earthaccess currently returns a token response dictionary; Harmony expects
        # the bearer-token string itself.
        token_data = earthaccess.get_edl_token()
        edl_token = token_data.get("access_token") if isinstance(token_data, dict) else token_data
        if not isinstance(edl_token, str) or not edl_token:
            raise RuntimeError("earthaccess did not provide a valid Earthdata access token")
        client = Client(token=edl_token)
        capabilities = client.submit(CapabilitiesRequest(short_name=PRODUCT_SHORT_NAME))
        collection_id = capabilities["conceptId"]
        west = FARM_LONGITUDE - args.buffer_degrees
        east = FARM_LONGITUDE + args.buffer_degrees
        south = FARM_LATITUDE - args.buffer_degrees
        north = FARM_LATITUDE + args.buffer_degrees
        for (year, month), names in sorted(pending_by_month.items()):
            print(f"Requesting {len(names)} cropped granules for {year}-{month:02d}...")
            request = Request(
                collection=Collection(id=collection_id),
                granule_name=names,
                spatial=BBox(west, south, east, north),
            )
            if not request.is_valid():
                raise RuntimeError(f"Harmony rejected the SMAP subset request for {year}-{month:02d}")
            job_id = client.submit(request)
            client.wait_for_processing(job_id, show_progress=True)
            futures = client.download_all(job_id, directory=str(RAW_DIR), overwrite=False)
            downloaded = [future.result() for future in futures]
            print(f"Saved {len(downloaded)} spatial subsets in {RAW_DIR}.")

    paths = list(RAW_DIR.glob("*subsetted*.nc4")) + list(RAW_DIR.glob("*subsetted*.h5")) + list(RAW_DIR.glob("*subsetted*.hdf5"))
    files = []
    for path in sorted(paths):
        try:
            day = granule_date(path)
        except ValueError:
            continue
        if start <= day <= end:
            files.append(path)
    if not files:
        raise RuntimeError(f"No Harmony subset files found in {RAW_DIR} for these dates")

    rows = [extract_granule(path) for path in files]
    frame = pd.DataFrame(rows).drop_duplicates(subset=["date"], keep="last").sort_values("date")
    frame.to_csv(OUTPUT_CSV, index=False, na_rep="")
    print(f"Processed {len(frame)} subset granules into {OUTPUT_CSV}")
    print(f"Cropped raw files are in {RAW_DIR}; only recommended quality flags 0 and 8 are used.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"SMAP pipeline error: {error}", file=sys.stderr)
        raise SystemExit(1)
