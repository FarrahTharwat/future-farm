# Data sources

Every dataset the project touches, with what is used, what is not, and its known limits. The agronomy references
(S01 to S22) are listed in `data/agricultural/agronomy_sources.md`. Access dates marked `[fill in]` must be completed
before submitting.

**Farm point:** Lattin Farms, Fallon, Nevada (39.461142 N, 118.830528 W). All DS1 downloads use this coordinate.

## What the scores actually read

| Model input | File | Made by | Used for |
|---|---|---|---|
| Daily weather 2001-2025 | `data/processed/weather_daily.csv` | `pipeline/fetch_power.py` (DS1) | Heat-stress days (`temperature_max`), rain and mean temperature by calendar month |
| Monthly reference ET0 (12 values) | `data/processed/environmental_features.csv`, column `et0_mm_mean` | `pipeline/feature_engineering.py` (DS1) | Crop water need (Kc x ET0) |
| Crop values, calendars, rotations | `data/agricultural/*.csv` | DS2 | All three scores |
| Water supply (1,220 mm per year) | typed into `model/water_need.py` | assumption | Water score |

Two constants at the top of `model/decision_engine.py` choose these inputs: `WEATHER_FILE` and `ET0_SOURCE`
(`"nasa"` or `"agrimet"`). SoilGrids, SMAP and MODIS are **not** used in any score.

## Used in the scores

### NASA POWER (daily weather)
| | |
|---|---|
| Provider | NASA Langley Research Center, POWER Project, Daily Point API, Agroclimatology (AG) community, UTC time standard |
| Location | One point: 39.461142 N, 118.830528 W (Lattin Farms, Fallon, Nevada) |
| Period | 2001-01-01 to 2025-12-31 (9,131 daily values). NASA's missing-value code (-999) is converted to blank; none occur in this download |
| Variables downloaded | `T2M`, `T2M_MAX`, `T2M_MIN` (2 m air temperature, C), `PRECTOTCORR` (corrected precipitation, mm/day), `RH2M` (relative humidity, %), `WS2M` (wind speed at 2 m, m/s), `ALLSKY_SFC_SW_DWN` (solar radiation, MJ/m2/day) |
| Variables used by the scores | `T2M_MAX` (heat stress), `PRECTOTCORR` (rain), `T2M` (monthly mean, used to scale ET0 under warming). The other variables are used only inside the ET0 calculation |
| Resolution | Meteorological fields about 0.5 x 0.625 degrees; solar fields about 1 x 1 degree. Gridded estimates, not farm-station observations |
| Files | `data/raw/nasa_power/nasa_power_raw.json` (original response), `data/processed/weather_daily.csv` (columns renamed to `temperature_avg`, `temperature_max`, `temperature_min`, `rainfall`, `humidity`, `wind_speed`, `solar_radiation`; the model maps them back to the POWER names) |
| Accessed | [fill in] |
| Known limits | One grid cell, tens of km wide. Daily values follow UTC days, not local days. An earlier download for 39.47 N, 118.78 W (`data/raw/nasa_power/power_daily.csv`, kept only as a fallback) falls in the same cell (same annual rain and mean temperature) but its daily maximum temperature differs on about a fifth of days; the new series is about 0.5 C hotter on average (about 47 against 43 days per year above 35 C). The cause is probably the time standard, which is not confirmed |

### Reference evapotranspiration (ET0), FAO-56 Penman-Monteith
| | |
|---|---|
| Provider | DS1, computed from the NASA POWER daily weather above |
| Method | FAO-56 Penman-Monteith (grass reference) per day, summed by calendar month for each year, then averaged over 2001-2025 |
| File | `data/processed/environmental_features.csv`: 12 rows, columns `month` and `et0_mm_mean` (mm per month) |
| Script | `pipeline/feature_engineering.py` |
| Known limits | Assumes an elevation of 1,200 m for air pressure and clear-sky radiation, while the POWER grid cell reports about 1,269 m. Wind comes from a coarse gridded product and may differ from local wind at the field. Not yet checked against a local reference other than the comparison below |

**Cross-check against the Fallon station.** DS2 also derived a monthly ET0 from the USBR AgriMet station FALN
(2001-2010 mean ETr, Kimberly-Penman 1982, alfalfa reference, divided by 1.2 to give grass reference, S17 and S20;
file `data/processed/et0_monthly.csv`, accessed 2026-11-14). The two series agree closely in the peak season:

| Month | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | Year |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AgriMet (mm) | 23 | 40 | 84 | 116 | 167 | 210 | 228 | 186 | 130 | 75 | 37 | 22 | 1,320 |
| NASA-derived (mm) | 31 | 48 | 86 | 123 | 172 | 211 | 225 | 195 | 146 | 90 | 46 | 29 | 1,400 |
| Difference | +34% | +20% | +2% | +6% | +3% | 0% | -2% | +5% | +12% | +20% | +24% | +29% | +6% |

The AgriMet series is a 10-year station average from irrigated cropland, and its conversion to grass reference by a
fixed factor of 1.2 is approximate (the true ratio varies by month). It is kept as a selectable alternative
(`ET0_SOURCE = "agrimet"`), not as the default.

**Effect on the result.** Re-running the model with each combination of inputs (old and new weather, AgriMet and
NASA-derived ET0) changes each overall score by at most about 2 points. The current rotation ranks first in all
27 uncertainty cases every time. The full comparison is in `docs/methodology.md`.

### Crop and rotation files (DS2)
| | |
|---|---|
| Files | `crop_database.csv` (10 crops, including alfalfa establishment), `crop_calendars.csv` (10 crops), `rotation_strategies.csv` (3 rotations, 12 steps), `rotation_strategies.json`, `local_irrigation_observations.csv` (5 rows, not read by any script), `verification_checklist.csv`, `agronomy_sources.md` |
| Main sources | FAO Irrigation and Drainage Paper 56 (Kc, rooting depth, stage lengths for six main crops); FAO ECOCROP (temperature ranges); UNR Extension fact sheets and trials; USDA-NRCS Great Basin Plant Materials Center (cover crops); USBR AgriMet (ET0 cross-check) |
| Status | Kc and rooting depth for six crops come from FAO-56. Heat limits, qualitative ratings, planting months, and the rye, vetch and teff values are estimates or analogs. Winter wheat's season was shortened by DS2 to end in July (not source-verified). Alfalfa seeding in August-September after the wheat harvest is an unsourced assumption. Most "Verified by" entries are still empty |
| Known limits | See `docs/limitations.md`; local validation needed |

### Water supply assumption
| | |
|---|---|
| Value | 1,220 mm per year (midpoint of 1,067 to 1,372 mm) |
| Source | Newlands Project allocation per acre, from a Truckee-Carson Irrigation District exhibit (S16), recorded as observation O05 |
| Known limits | Typed into `model/water_need.py` by hand, not read from the file. Unverified. A supply limit, not a crop requirement |

## Downloaded, not used in any score

| Dataset | Content | Status | Limits |
|---|---|---|---|
| ISRIC SoilGrids 2.0 | Bulk density, clay, sand, pH in water, organic carbon and nitrogen at six depths (0-5, 5-15, 15-30, 30-60, 60-100, 100-200 cm): 36 values at the farm point. Raw GeoTIFFs in `data/raw/soilgrids/`, values in `data/processed/soil_properties.csv`. Script: `pipeline/fetch_soilgrids.py` | Downloaded; not used | Model predictions at 250 m, not field measurements. Accessed: [fill in] |
| NASA SMAP `SPL3SMP_E` v006 | Surface soil moisture (about the top 5 cm), daily composite, 9 km grid posting, about 36 km native footprint. Product period 2015-03-31 to present; the project's intended analysis window is 2016-2025. File: `data/processed/smap_soil_moisture.csv`. Script: `pipeline/fetch_smap.py` (Harmony spatial subset around the farm; recommended-quality flags only) | Only 2025-01-01 to 2025-01-07 downloaded (7 days, 5 within 15 km of the farm). Too short for any comparison with the water model | Regional surface indicator, not root-zone moisture and not a field measurement. Accessed: [fill in] |
| MODIS MOD13Q1 v061 NDVI | 250 m, 16-day composites (product period 2000 onward; 2001-2025 requested for the project) sampled at the farm point through NASA AppEEARS, which returns a small point series instead of full granules. The script rescales NDVI by 0.0001 and keeps only pixel-reliability flag 0. File: `data/processed/ndvi.csv`. Script: `pipeline/fetch_modis_ndvi.py` | Test request only (January 2025): 2 rows, 1 valid value. The script's default is a one-month test; longer requests need `--allow-long-range`. Not used | Cloud and snow screening leave gaps. Accessed: [fill in] |

## Derived files
| File | Made by | What it holds |
|---|---|---|
| `data/processed/farm_daily_dataset.csv` | `pipeline/merge_datasets.py` (DS1) | 3,653 daily rows (2016-2025) and 55 columns: weather, SMAP values (blank when missing, never interpolated) and the 36 static SoilGrids values repeated on every row. Not read by the model |
| `outputs/results.json` | `model/decision_engine.py` (DS3) | Scores, ranking, stress test, sensitivity, flip points, uncertainty ranges, scenario grid and field timeline, read by the game |

## Next steps for data
- Compare the NASA-derived ET0 with a local reference (for example more AgriMet years) and use the POWER grid elevation
  (about 1,269 m) in the pressure and radiation terms.
- Confirm why the two POWER downloads differ in daily maximum temperature (time standard against location).
- Download a full SMAP series (2016 onward, or at least one year) and run the seasonal comparison with the water model.
- Use SoilGrids for a soil indicator, and MODIS NDVI to check crop calendars against observed greening.
- Have DS2 fill the "Verified by" column and replace estimated crop values with sourced ones.