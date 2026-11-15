"""Fetch a point-sampled MODIS NDVI time series for Lattin Farms via NASA AppEEARS.

AppEEARS samples the requested product at the farm coordinate and returns a small
tabular result, avoiding downloads of full MODIS granule tiles.
"""
from __future__ import annotations

import argparse
import re
import sys
import time
from datetime import date
from netrc import NetrcParseError, netrc
from pathlib import Path
from typing import Any

import numpy as np
import pandas as pd
import requests

ROOT = Path(__file__).resolve().parents[1]
RAW_DIR = ROOT / "data" / "raw" / "modis"
OUTPUT_CSV = ROOT / "data" / "processed" / "ndvi.csv"
RAW_CSV = RAW_DIR / "appeears_mod13q1_point_results.csv"
LATITUDE = 39.461142
LONGITUDE = -118.830528
PRODUCT = "MOD13Q1.061"
NDVI_LAYER = "_250m_16_days_NDVI"
QUALITY_LAYER = "_250m_16_days_pixel_reliability"
API = "https://appeears.earthdatacloud.nasa.gov/api/"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--start", default="2025-01-01", help="Start date, YYYY-MM-DD (default: one-month test)")
    parser.add_argument("--end", default="2025-01-31", help="End date inclusive, YYYY-MM-DD (default: one-month test)")
    parser.add_argument("--estimate-only", action="store_true", help="Show the point request size without submitting it")
    parser.add_argument("--allow-long-range", action="store_true", help="Allow requests longer than 31 days")
    return parser.parse_args()


def read_earthdata_credentials() -> tuple[str, str]:
    """Read credentials from the private Windows _netrc file without displaying them."""
    credentials_file = Path.home() / "_netrc"
    if not credentials_file.exists():
        raise RuntimeError(f"Earthdata credentials file not found at {credentials_file}. Create _netrc in your Windows user folder.")
    try:
        credentials = netrc(str(credentials_file)).authenticators("urs.earthdata.nasa.gov")
    except (NetrcParseError, OSError) as exc:
        raise RuntimeError(f"Could not read Earthdata credentials from {credentials_file}") from exc
    if not credentials or not credentials[0] or not credentials[2]:
        raise RuntimeError(f"The file {credentials_file} needs a machine urs.earthdata.nasa.gov entry with login and password.")
    return credentials[0], credentials[2]


def api_json(response: requests.Response, label: str) -> Any:
    try:
        response.raise_for_status()
    except requests.HTTPError as exc:
        detail = response.text[:500].replace("\n", " ")
        raise RuntimeError(f"AppEEARS {label} failed (HTTP {response.status_code}): {detail}") from exc
    try:
        return response.json()
    except ValueError as exc:
        raise RuntimeError(f"AppEEARS {label} returned a non-JSON response (HTTP {response.status_code}). Check network access and Earthdata credentials.") from exc


def login(session: requests.Session) -> str:
    username, password = read_earthdata_credentials()
    response = session.post(f"{API}login", auth=(username, password), timeout=60)
    result = api_json(response, "login")
    token = result.get("token")
    if not token:
        raise RuntimeError("AppEEARS login succeeded but did not return an access token")
    return str(token)


def date_from_value(value: Any) -> date:
    text = str(value).strip()
    if re.fullmatch(r"A?\d{7}", text):
        digits = text[1:] if text.startswith("A") else text
        return date(int(digits[:4]), 1, 1) + pd.Timedelta(days=int(digits[4:]) - 1)
    parsed = pd.to_datetime(text, errors="coerce")
    if pd.isna(parsed):
        raise ValueError(f"Cannot interpret AppEEARS date value {value!r}")
    return parsed.date()


def normalize_result(path: Path, start: date, end: date) -> pd.DataFrame:
    frame = pd.read_csv(path)
    if frame.empty:
        raise RuntimeError("AppEEARS returned an empty point-sample CSV")
    columns = {str(column).strip().casefold(): column for column in frame.columns}
    date_col = next((original for key, original in columns.items() if key == "date" or key.startswith("date[")), None)
    ndvi_col = next((original for key, original in columns.items() if "ndvi" in key and "pixel" not in key and "quality" not in key), None)
    quality_col = next((original for key, original in columns.items() if "pixel_reliability" in key), None)
    if date_col is None or ndvi_col is None:
        raise RuntimeError(f"Could not find date and NDVI columns in AppEEARS CSV. Columns: {list(frame.columns)}")
    if quality_col is None:
        raise RuntimeError(f"Could not find the pixel-reliability quality column in AppEEARS CSV. Columns: {list(frame.columns)}")

    result = pd.DataFrame()
    result["date"] = frame[date_col].map(date_from_value).map(lambda day: day.isoformat())
    raw_ndvi = pd.to_numeric(frame[ndvi_col], errors="coerce")
    # AppEEARS may return either scaled values (0.5) or source integers (5000).
    scaled_ndvi = raw_ndvi.where(raw_ndvi.abs() <= 1.0, raw_ndvi * 0.0001)
    quality = pd.to_numeric(frame[quality_col], errors="coerce") if quality_col else pd.Series(np.nan, index=frame.index)
    valid = scaled_ndvi.between(-0.2, 1.0)
    if quality_col:
        valid &= quality.eq(0)

    result["ndvi"] = scaled_ndvi.where(valid)
    result["ndvi_units"] = "unitless"
    result["pixel_reliability"] = quality
    result["quality_label"] = np.where(valid, "good", "not recommended or missing")
    result["product"] = "MOD13Q1"
    result["product_version"] = "061"
    result["latitude"] = LATITUDE
    result["longitude"] = LONGITUDE
    result["spatial_resolution_m"] = 250
    result["composite_days"] = 16
    result["source_file"] = path.name
    result["_date"] = result["date"].map(date.fromisoformat)
    result = result[result["_date"].between(start, end)].drop(columns="_date")
    return result.drop_duplicates(subset=["date"], keep="last").sort_values("date")


def main() -> int:
    args = parse_args()
    start, end = date.fromisoformat(args.start), date.fromisoformat(args.end)
    if end < start:
        raise ValueError("--end must be on or after --start")
    days = (end - start).days + 1
    if days > 31 and not args.allow_long_range:
        raise ValueError("Requests over 31 days require --allow-long-range; test a short period first")
    estimated_observations = (days + 7) // 16 + 1
    print(f"MODIS point sample: {PRODUCT}, layer {NDVI_LAYER}")
    print(f"Farm coordinate: {LATITUDE}, {LONGITUDE}")
    print(f"Date range: {start} through {end} (about {estimated_observations} composites)")
    print("This requests point values and quality data; it does not download global HDF granules.")
    if args.estimate_only:
        print("Estimated output is a small CSV point series, not a multi-gigabyte tile download.")
        return 0
    if input("Type REQUEST to submit this point-sample request: ").strip() != "REQUEST":
        print("Cancelled; no AppEEARS request submitted.")
        return 0

    RAW_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    session = requests.Session()
    token = login(session)
    headers = {"Authorization": f"Bearer {token}"}

    product_response = session.get(f"{API}product/{PRODUCT}", timeout=60)
    layer_info = api_json(product_response, "product/layer lookup")
    available_layers = set(layer_info)
    for required_layer in (NDVI_LAYER, QUALITY_LAYER):
        if required_layer not in available_layers:
            raise RuntimeError(f"AppEEARS layer {required_layer!r} is unavailable for {PRODUCT}")

    task = {
        "task_type": "point",
        "task_name": f"future-farm-modis-ndvi-{start:%Y%m%d}-{end:%Y%m%d}",
        "params": {
            "dates": [{"startDate": start.strftime("%m-%d-%Y"), "endDate": end.strftime("%m-%d-%Y")}],
            "layers": [
                {"product": PRODUCT, "layer": NDVI_LAYER},
                {"product": PRODUCT, "layer": QUALITY_LAYER},
            ],
            "coordinates": [{"id": "Lattin_Farms", "latitude": LATITUDE, "longitude": LONGITUDE}],
        },
    }
    submitted = session.post(f"{API}task", json=task, headers=headers, timeout=60)
    task_response = api_json(submitted, "task submission")
    task_id = task_response.get("task_id")
    if not task_id:
        location = submitted.headers.get("Location", "")
        task_id = location.rstrip("/").split("/")[-1] if location else None
    if not task_id:
        raise RuntimeError(f"AppEEARS did not return a task ID: {task_response}")
    print(f"AppEEARS task submitted: {task_id}")

    while True:
        status = api_json(session.get(f"{API}task/{task_id}", headers=headers, timeout=60), "task status")
        state = status.get("status")
        print(f"AppEEARS status: {state}")
        if state == "done":
            if status.get("summary", {}).get("empty"):
                raise RuntimeError("AppEEARS completed the task but returned no samples for this point/date range")
            break
        if state == "error":
            raise RuntimeError(f"AppEEARS task failed: {status.get('error') or status.get('summary')}")
        time.sleep(20)

    bundle = api_json(session.get(f"{API}bundle/{task_id}", headers=headers, timeout=60), "result bundle")
    result_file = next((item for item in bundle.get("files", []) if item.get("file_type") == "csv" and "results" in item.get("file_name", "").casefold()), None)
    if result_file is None:
        raise RuntimeError(f"No AppEEARS results CSV was found in the task bundle: {bundle.get('files', [])}")
    download = session.get(f"{API}bundle/{task_id}/{result_file['file_id']}", headers=headers, allow_redirects=True, timeout=120)
    download.raise_for_status()
    RAW_CSV.write_bytes(download.content)
    result = normalize_result(RAW_CSV, start, end)
    if result.empty:
        raise RuntimeError("AppEEARS result had no rows within the requested dates")
    result.to_csv(OUTPUT_CSV, index=False, na_rep="")
    print(f"Saved raw point sample to {RAW_CSV}")
    print(f"Saved {len(result)} MODIS NDVI observations to {OUTPUT_CSV}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"MODIS NDVI error: {error}", file=sys.stderr)
        raise SystemExit(1)
