# """Build monthly environmental features from the daily NASA POWER weather file.

# ET0 is estimated with the FAO-56 Penman-Monteith method. The output is a
# 12-row calendar-month climatology: mean monthly total across available years.
# """
# from __future__ import annotations

# from pathlib import Path

# import numpy as np
# import pandas as pd

# ROOT = Path(__file__).resolve().parents[1]
# WEATHER_FILE = ROOT / "data" / "processed" / "weather_daily.csv"
# OUTPUT_FILE = ROOT / "data" / "processed" / "environmental_features.csv"
# LATITUDE_DEGREES = 39.461142
# ELEVATION_METERS = 1200.0  # Approximate elevation near Lattin Farms, Fallon, Nevada.
# STEFAN_BOLTZMANN = 4.903e-9  # MJ K^-4 m^-2 day^-1


# def saturation_vapor_pressure(temp_c: pd.Series | np.ndarray) -> np.ndarray:
#     temp = np.asarray(temp_c, dtype=float)
#     return 0.6108 * np.exp((17.27 * temp) / (temp + 237.3))


# def extraterrestrial_radiation(day_of_year: np.ndarray) -> np.ndarray:
#     """FAO-56 daily extraterrestrial radiation Ra, in MJ m-2 day-1."""
#     lat = np.radians(LATITUDE_DEGREES)
#     dr = 1 + 0.033 * np.cos(2 * np.pi * day_of_year / 365)
#     solar_declination = 0.409 * np.sin(2 * np.pi * day_of_year / 365 - 1.39)
#     sunset_angle = np.arccos(np.clip(-np.tan(lat) * np.tan(solar_declination), -1, 1))
#     return (24 * 60 / np.pi) * 0.0820 * dr * (
#         sunset_angle * np.sin(lat) * np.sin(solar_declination)
#         + np.cos(lat) * np.cos(solar_declination) * np.sin(sunset_angle)
#     )


# def calculate_daily_et0(weather: pd.DataFrame) -> pd.Series:
#     required = [
#         "date", "temperature_avg", "temperature_max", "temperature_min",
#         "humidity", "wind_speed", "solar_radiation",
#     ]
#     missing = [column for column in required if column not in weather.columns]
#     if missing:
#         raise ValueError(
#             "weather_daily.csv is missing required columns: " + ", ".join(missing)
#             + ". Re-run pipeline/fetch_power.py to refresh the POWER download."
#         )

#     tmean = weather["temperature_avg"].astype(float).to_numpy()
#     tmax = weather["temperature_max"].astype(float).to_numpy()
#     tmin = weather["temperature_min"].astype(float).to_numpy()
#     rh = weather["humidity"].astype(float).to_numpy()
#     wind = weather["wind_speed"].astype(float).to_numpy()  # POWER WS2M is already at 2 m.
#     solar = weather["solar_radiation"].astype(float).to_numpy()
#     dates = pd.to_datetime(weather["date"])
#     doy = dates.dt.dayofyear.to_numpy()

#     es = (saturation_vapor_pressure(tmax) + saturation_vapor_pressure(tmin)) / 2
#     ea = np.clip(rh, 0, 100) / 100 * es
#     delta = 4098 * saturation_vapor_pressure(tmean) / (tmean + 237.3) ** 2
#     pressure = 101.3 * ((293 - 0.0065 * ELEVATION_METERS) / 293) ** 5.26
#     psychrometric = 0.000665 * pressure

#     ra = extraterrestrial_radiation(doy)
#     rso = (0.75 + 2e-5 * ELEVATION_METERS) * ra
#     rs_rso = np.divide(solar, rso, out=np.zeros_like(solar), where=rso > 0)
#     rs_rso = np.clip(rs_rso, 0, 1)
#     net_shortwave = 0.77 * solar
#     net_longwave = (
#         STEFAN_BOLTZMANN
#         * ((tmax + 273.16) ** 4 + (tmin + 273.16) ** 4) / 2
#         * (0.34 - 0.14 * np.sqrt(np.maximum(ea, 0)))
#         * (1.35 * rs_rso - 0.35)
#     )
#     net_radiation = net_shortwave - net_longwave

#     numerator = (
#         0.408 * delta * net_radiation
#         + psychrometric * (900 / (tmean + 273)) * wind * (es - ea)
#     )
#     denominator = delta + psychrometric * (1 + 0.34 * wind)
#     et0 = numerator / denominator
#     et0 = np.maximum(et0, 0)

#     invalid = (
#         ~np.isfinite(tmean) | ~np.isfinite(tmax) | ~np.isfinite(tmin)
#         | ~np.isfinite(rh) | ~np.isfinite(wind) | ~np.isfinite(solar)
#         | (weather["temperature_avg"].to_numpy() == -999)
#         | (weather["temperature_max"].to_numpy() == -999)
#         | (weather["temperature_min"].to_numpy() == -999)
#         | (weather["humidity"].to_numpy() == -999)
#         | (weather["wind_speed"].to_numpy() == -999)
#         | (weather["solar_radiation"].to_numpy() == -999)
#     )
#     et0[invalid] = np.nan
#     return pd.Series(et0, index=weather.index, name="et0_mm_day")


# def main() -> None:
#     if not WEATHER_FILE.exists():
#         raise FileNotFoundError(f"Weather input not found: {WEATHER_FILE}")
#     weather = pd.read_csv(WEATHER_FILE, parse_dates=["date"])
#     weather["et0_mm_day"] = calculate_daily_et0(weather)
#     weather["year"] = weather["date"].dt.year
#     weather["month"] = weather["date"].dt.month

#     monthly_totals = weather.groupby(["year", "month"], as_index=False)["et0_mm_day"].sum(min_count=1)
#     features = (
#         monthly_totals.groupby("month", as_index=False)["et0_mm_day"]
#         .mean()
#         .rename(columns={"et0_mm_day": "et0_mm_mean"})
#     )
#     features = (
#         pd.DataFrame({"month": range(1, 13)})
#         .merge(features, on="month", how="left", validate="one_to_one")
#     )
#     OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
#     features.to_csv(OUTPUT_FILE, index=False, float_format="%.3f", na_rep="")
#     print(f"Saved {len(features)} calendar-month ET0 features to {OUTPUT_FILE}")
#     print(features.to_string(index=False))


# if __name__ == "__main__":
#     main()


"""Build monthly environmental features from the daily NASA POWER weather file.

ET0 is estimated with the FAO-56 Penman-Monteith method. The output is a
12-row calendar-month climatology: mean monthly total across available years.
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
WEATHER_FILE = ROOT / "data" / "processed" / "weather_daily.csv"
OUTPUT_FILE = ROOT / "data" / "processed" / "environmental_features.csv"
LATITUDE_DEGREES = 39.461142
ELEVATION_METERS = 1200.0  # Approximate elevation near Lattin Farms, Fallon, Nevada.
STEFAN_BOLTZMANN = 4.903e-9  # MJ K^-4 m^-2 day^-1


def saturation_vapor_pressure(temp_c: pd.Series | np.ndarray) -> np.ndarray:
    temp = np.asarray(temp_c, dtype=float)
    return 0.6108 * np.exp((17.27 * temp) / (temp + 237.3))


def extraterrestrial_radiation(day_of_year: np.ndarray) -> np.ndarray:
    """FAO-56 daily extraterrestrial radiation Ra, in MJ m-2 day-1."""
    lat = np.radians(LATITUDE_DEGREES)
    dr = 1 + 0.033 * np.cos(2 * np.pi * day_of_year / 365)
    solar_declination = 0.409 * np.sin(2 * np.pi * day_of_year / 365 - 1.39)
    sunset_angle = np.arccos(np.clip(-np.tan(lat) * np.tan(solar_declination), -1, 1))
    return (24 * 60 / np.pi) * 0.0820 * dr * (
        sunset_angle * np.sin(lat) * np.sin(solar_declination)
        + np.cos(lat) * np.cos(solar_declination) * np.sin(sunset_angle)
    )


def calculate_daily_et0(weather: pd.DataFrame) -> pd.Series:
    required = [
        "date", "temperature_avg", "temperature_max", "temperature_min",
        "humidity", "wind_speed", "solar_radiation",
    ]
    missing = [column for column in required if column not in weather.columns]
    if missing:
        raise ValueError(
            "weather_daily.csv is missing required columns: " + ", ".join(missing)
            + ". Re-run pipeline/fetch_power.py to refresh the POWER download."
        )

    tmean = weather["temperature_avg"].astype(float).to_numpy()
    tmax = weather["temperature_max"].astype(float).to_numpy()
    tmin = weather["temperature_min"].astype(float).to_numpy()
    rh = weather["humidity"].astype(float).to_numpy()
    wind = weather["wind_speed"].astype(float).to_numpy()  # POWER WS2M is already at 2 m.
    solar = weather["solar_radiation"].astype(float).to_numpy()
    dates = pd.to_datetime(weather["date"])
    doy = dates.dt.dayofyear.to_numpy()

    es = (saturation_vapor_pressure(tmax) + saturation_vapor_pressure(tmin)) / 2
    ea = np.clip(rh, 0, 100) / 100 * es
    delta = 4098 * saturation_vapor_pressure(tmean) / (tmean + 237.3) ** 2
    pressure = 101.3 * ((293 - 0.0065 * ELEVATION_METERS) / 293) ** 5.26
    psychrometric = 0.000665 * pressure

    ra = extraterrestrial_radiation(doy)
    rso = (0.75 + 2e-5 * ELEVATION_METERS) * ra
    rs_rso = np.divide(solar, rso, out=np.zeros_like(solar), where=rso > 0)
    rs_rso = np.clip(rs_rso, 0, 1)
    net_shortwave = 0.77 * solar
    net_longwave = (
        STEFAN_BOLTZMANN
        * ((tmax + 273.16) ** 4 + (tmin + 273.16) ** 4) / 2
        * (0.34 - 0.14 * np.sqrt(np.maximum(ea, 0)))
        * (1.35 * rs_rso - 0.35)
    )
    net_radiation = net_shortwave - net_longwave

    numerator = (
        0.408 * delta * net_radiation
        + psychrometric * (900 / (tmean + 273)) * wind * (es - ea)
    )
    denominator = delta + psychrometric * (1 + 0.34 * wind)
    et0 = numerator / denominator
    et0 = np.maximum(et0, 0)

    invalid = (
        ~np.isfinite(tmean) | ~np.isfinite(tmax) | ~np.isfinite(tmin)
        | ~np.isfinite(rh) | ~np.isfinite(wind) | ~np.isfinite(solar)
        | (weather["temperature_avg"].to_numpy() == -999)
        | (weather["temperature_max"].to_numpy() == -999)
        | (weather["temperature_min"].to_numpy() == -999)
        | (weather["humidity"].to_numpy() == -999)
        | (weather["wind_speed"].to_numpy() == -999)
        | (weather["solar_radiation"].to_numpy() == -999)
    )
    et0[invalid] = np.nan
    return pd.Series(et0, index=weather.index, name="et0_mm_day")


def main() -> None:
    if not WEATHER_FILE.exists():
        raise FileNotFoundError(f"Weather input not found: {WEATHER_FILE}")
    weather = pd.read_csv(WEATHER_FILE, parse_dates=["date"])
    weather["et0_mm_day"] = calculate_daily_et0(weather)
    weather["year"] = weather["date"].dt.year
    weather["month"] = weather["date"].dt.month

    monthly_totals = weather.groupby(["year", "month"], as_index=False)["et0_mm_day"].sum(min_count=1)
    features = (
        monthly_totals.groupby("month", as_index=False)["et0_mm_day"]
        .mean()
        .rename(columns={"et0_mm_day": "et0_mm_mean"})
    )
    features = (
        pd.DataFrame({"month": range(1, 13)})
        .merge(features, on="month", how="left", validate="one_to_one")
    )
    OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
    features.to_csv(OUTPUT_FILE, index=False, float_format="%.3f", na_rep="")
    print(f"Saved {len(features)} calendar-month ET0 features to {OUTPUT_FILE}")
    print(features.to_string(index=False))


if __name__ == "__main__":
    main()
