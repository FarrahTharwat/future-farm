# Data sources

Every dataset the project touches, with what is used, what is not, and its known limits. The full list of agronomy
references (S01 to S21) is in `data/agricultural/agronomy_sources.md`.

Fill in the access dates marked `[fill in]` before submitting.

## Used in the scores

### NASA POWER (daily weather)
| | |
|---|---|
| Provider | NASA Langley Research Center, POWER Project, daily point API, community AG |
| Location | One point: 39.47 N, 118.78 W (Fallon, Nevada) |
| Period | 2001-01-01 to 2025-12-31 (9,131 daily values) |
| Variables downloaded | T2M (mean temperature), T2M_MAX (maximum temperature), PRECTOTCORR (corrected precipitation), RH2M (relative humidity), ALLSKY_SFC_SW_DWN (solar radiation) |
| Variables used | T2M_MAX (heat stress), PRECTOTCORR (rain), T2M (monthly mean, used to scale ET0 under warming). RH2M and solar radiation are downloaded but not used |
| Resolution | A coarse grid cell, tens of km (confirm the exact resolution of each parameter in the POWER documentation) |
| Files | `data/raw/nasa_power/power_daily.csv`, `data/processed/weather.csv` (monthly), `data/processed/environmental_features.csv` (typical year, 12 rows) |
| Accessed | [fill in] |
| Known limits | Gridded model and satellite-derived values, not field measurements; one point only; no daily minimum temperature or wind downloaded |

### USBR AgriMet, station FALN (monthly reference evapotranspiration)
| | |
|---|---|
| Provider | US Bureau of Reclamation, AgriMet, Fallon NV station, monthly average reference evapotranspiration |
| Period | 2001-2010 average |
| Variable | Alfalfa-reference ET (Kimberly-Penman 1982), inches per month |
| Processing | Converted to mm, then divided by 1.2 to give grass-reference ET0 (UC Davis Alfalfa Symposium 2002) |
| File | `data/processed/et0_monthly.csv` (12 rows) |
| Accessed | 2026-11-14 |
| Known limits | A 10-year average from a station in irrigated cropland, not NASA-derived, and may differ from a POWER-based estimate; the 1.2 conversion is approximate |

### Crop and rotation files (DS2)
| | |
|---|---|
| Files | `crop_database.csv` (9 crops), `crop_calendars.csv` (9 crops), `rotation_strategies.csv` (3 rotations, 12 steps), `local_irrigation_observations.csv` (5 rows, not read by any script) |
| Main sources | FAO Irrigation and Drainage Paper 56 (Kc, rooting depth, stage lengths, six main crops); FAO ECOCROP (temperature ranges); UNR Extension fact sheets and trials; USDA-NRCS Great Basin Plant Materials Center (cover crops) |
| Status | Kc and rooting depth for six crops are sourced from FAO-56; heat limits, qualitative ratings, planting months and rye, vetch and teff values are estimates or analogs. Most "Verified by" entries are still empty |
| Known limits | See `limitations.md`; local validation needed |

### Water supply assumption
| | |
|---|---|
| Value | 1,220 mm per year (midpoint of 1,067 to 1,372 mm) |
| Source | Newlands Project allocation per acre, from a Truckee-Carson Irrigation District exhibit (S16), recorded as observation O05 |
| Known limits | Typed into `model/water_need.py` by hand, not read from the file; unverified; a supply limit, not a crop requirement |

## Downloaded or planned, not used in any score

| Dataset | Intended use | Status | Notes |
|---|---|---|---|
| ISRIC SoilGrids (soil properties) | Soil carbon, pH, texture at the farm point | Script written (`pipeline/fetch_soilgrids.py`); not used | Model predictions at 250 m, not measurements |
| NASA SMAP (surface soil moisture) | Optional validation panel | Not used. `data/processed/soil_moisture.csv` holds only 7 daily values (1 to 6 January 2025), too few to compare with the model | About 9 km pixels, surface layer only, not root zone; dropped to "next steps" |
| MODIS NDVI / EVI | Check crop calendars against observed greening | Not delivered | Dropped to "next steps" |

## Derived files
| File | Made by | What it holds |
|---|---|---|
| `outputs/results.json` | `model/decision_engine.py` | Scores, ranking, stress test, sensitivity, flip points, uncertainty ranges, scenario grid and field timeline, read by the game |

## Next steps for data
- Replace the Fallon station ET0 with a NASA-derived series (for example from POWER temperature and radiation).
- Add daily minimum temperature and wind to the POWER download (frost and a fuller ET0).
- Use SoilGrids for a soil indicator and SMAP as a regional validation of modelled water shortfall.
- Have DS2 fill the "Verified by" column and replace estimated crop values with sourced ones.# Data sources

Every dataset the project touches, with what is used, what is not, and its known limits. The full list of agronomy
references (S01 to S21) is in `data/agricultural/agronomy_sources.md`.

Fill in the access dates marked `[fill in]` before submitting.

## Used in the scores

### NASA POWER (daily weather)
| | |
|---|---|
| Provider | NASA Langley Research Center, POWER Project, daily point API, community AG |
| Location | One point: 39.47 N, 118.78 W (Fallon, Nevada) |
| Period | 2001-01-01 to 2025-12-31 (9,131 daily values) |
| Variables downloaded | T2M (mean temperature), T2M_MAX (maximum temperature), PRECTOTCORR (corrected precipitation), RH2M (relative humidity), ALLSKY_SFC_SW_DWN (solar radiation) |
| Variables used | T2M_MAX (heat stress), PRECTOTCORR (rain), T2M (monthly mean, used to scale ET0 under warming). RH2M and solar radiation are downloaded but not used |
| Resolution | A coarse grid cell, tens of km (confirm the exact resolution of each parameter in the POWER documentation) |
| Files | `data/raw/nasa_power/power_daily.csv`, `data/processed/weather.csv` (monthly), `data/processed/environmental_features.csv` (typical year, 12 rows) |
| Accessed | [fill in] |
| Known limits | Gridded model and satellite-derived values, not field measurements; one point only; no daily minimum temperature or wind downloaded |

### USBR AgriMet, station FALN (monthly reference evapotranspiration)
| | |
|---|---|
| Provider | US Bureau of Reclamation, AgriMet, Fallon NV station, monthly average reference evapotranspiration |
| Period | 2001-2010 average |
| Variable | Alfalfa-reference ET (Kimberly-Penman 1982), inches per month |
| Processing | Converted to mm, then divided by 1.2 to give grass-reference ET0 (UC Davis Alfalfa Symposium 2002) |
| File | `data/processed/et0_monthly.csv` (12 rows) |
| Accessed | 2026-11-14 |
| Known limits | A 10-year average from a station in irrigated cropland, not NASA-derived, and may differ from a POWER-based estimate; the 1.2 conversion is approximate |

### Crop and rotation files (DS2)
| | |
|---|---|
| Files | `crop_database.csv` (9 crops), `crop_calendars.csv` (9 crops), `rotation_strategies.csv` (3 rotations, 12 steps), `local_irrigation_observations.csv` (5 rows, not read by any script) |
| Main sources | FAO Irrigation and Drainage Paper 56 (Kc, rooting depth, stage lengths, six main crops); FAO ECOCROP (temperature ranges); UNR Extension fact sheets and trials; USDA-NRCS Great Basin Plant Materials Center (cover crops) |
| Status | Kc and rooting depth for six crops are sourced from FAO-56; heat limits, qualitative ratings, planting months and rye, vetch and teff values are estimates or analogs. Most "Verified by" entries are still empty |
| Known limits | See `limitations.md`; local validation needed |

### Water supply assumption
| | |
|---|---|
| Value | 1,220 mm per year (midpoint of 1,067 to 1,372 mm) |
| Source | Newlands Project allocation per acre, from a Truckee-Carson Irrigation District exhibit (S16), recorded as observation O05 |
| Known limits | Typed into `model/water_need.py` by hand, not read from the file; unverified; a supply limit, not a crop requirement |

## Downloaded or planned, not used in any score

| Dataset | Intended use | Status | Notes |
|---|---|---|---|
| ISRIC SoilGrids (soil properties) | Soil carbon, pH, texture at the farm point | Script written (`pipeline/fetch_soilgrids.py`); not used | Model predictions at 250 m, not measurements |
| NASA SMAP (surface soil moisture) | Optional validation panel | Not used. `data/processed/soil_moisture.csv` holds only 7 daily values (1 to 6 January 2025), too few to compare with the model | About 9 km pixels, surface layer only, not root zone; dropped to "next steps" |
| MODIS NDVI / EVI | Check crop calendars against observed greening | Not delivered | Dropped to "next steps" |

## Derived files
| File | Made by | What it holds |
|---|---|---|
| `outputs/results.json` | `model/decision_engine.py` | Scores, ranking, stress test, sensitivity, flip points, uncertainty ranges, scenario grid and field timeline, read by the game |

## Next steps for data
- Replace the Fallon station ET0 with a NASA-derived series (for example from POWER temperature and radiation).
- Add daily minimum temperature and wind to the POWER download (frost and a fuller ET0).
- Use SoilGrids for a soil indicator and SMAP as a regional validation of modelled water shortfall.
- Have DS2 fill the "Verified by" column and replace estimated crop values with sourced ones.# Data sources

Every dataset the project touches, with what is used, what is not, and its known limits. The full list of agronomy
references (S01 to S21) is in `data/agricultural/agronomy_sources.md`.

Fill in the access dates marked `[fill in]` before submitting.

## Used in the scores

### NASA POWER (daily weather)
| | |
|---|---|
| Provider | NASA Langley Research Center, POWER Project, daily point API, community AG |
| Location | One point: 39.47 N, 118.78 W (Fallon, Nevada) |
| Period | 2001-01-01 to 2025-12-31 (9,131 daily values) |
| Variables downloaded | T2M (mean temperature), T2M_MAX (maximum temperature), PRECTOTCORR (corrected precipitation), RH2M (relative humidity), ALLSKY_SFC_SW_DWN (solar radiation) |
| Variables used | T2M_MAX (heat stress), PRECTOTCORR (rain), T2M (monthly mean, used to scale ET0 under warming). RH2M and solar radiation are downloaded but not used |
| Resolution | A coarse grid cell, tens of km (confirm the exact resolution of each parameter in the POWER documentation) |
| Files | `data/raw/nasa_power/power_daily.csv`, `data/processed/weather.csv` (monthly), `data/processed/environmental_features.csv` (typical year, 12 rows) |
| Accessed | [fill in] |
| Known limits | Gridded model and satellite-derived values, not field measurements; one point only; no daily minimum temperature or wind downloaded |

### USBR AgriMet, station FALN (monthly reference evapotranspiration)
| | |
|---|---|
| Provider | US Bureau of Reclamation, AgriMet, Fallon NV station, monthly average reference evapotranspiration |
| Period | 2001-2010 average |
| Variable | Alfalfa-reference ET (Kimberly-Penman 1982), inches per month |
| Processing | Converted to mm, then divided by 1.2 to give grass-reference ET0 (UC Davis Alfalfa Symposium 2002) |
| File | `data/processed/et0_monthly.csv` (12 rows) |
| Accessed | 2026-11-14 |
| Known limits | A 10-year average from a station in irrigated cropland, not NASA-derived, and may differ from a POWER-based estimate; the 1.2 conversion is approximate |

### Crop and rotation files (DS2)
| | |
|---|---|
| Files | `crop_database.csv` (9 crops), `crop_calendars.csv` (9 crops), `rotation_strategies.csv` (3 rotations, 12 steps), `local_irrigation_observations.csv` (5 rows, not read by any script) |
| Main sources | FAO Irrigation and Drainage Paper 56 (Kc, rooting depth, stage lengths, six main crops); FAO ECOCROP (temperature ranges); UNR Extension fact sheets and trials; USDA-NRCS Great Basin Plant Materials Center (cover crops) |
| Status | Kc and rooting depth for six crops are sourced from FAO-56; heat limits, qualitative ratings, planting months and rye, vetch and teff values are estimates or analogs. Most "Verified by" entries are still empty |
| Known limits | See `limitations.md`; local validation needed |

### Water supply assumption
| | |
|---|---|
| Value | 1,220 mm per year (midpoint of 1,067 to 1,372 mm) |
| Source | Newlands Project allocation per acre, from a Truckee-Carson Irrigation District exhibit (S16), recorded as observation O05 |
| Known limits | Typed into `model/water_need.py` by hand, not read from the file; unverified; a supply limit, not a crop requirement |

## Downloaded or planned, not used in any score

| Dataset | Intended use | Status | Notes |
|---|---|---|---|
| ISRIC SoilGrids (soil properties) | Soil carbon, pH, texture at the farm point | Script written (`pipeline/fetch_soilgrids.py`); not used | Model predictions at 250 m, not measurements |
| NASA SMAP (surface soil moisture) | Optional validation panel | Not used. `data/processed/soil_moisture.csv` holds only 7 daily values (1 to 6 January 2025), too few to compare with the model | About 9 km pixels, surface layer only, not root zone; dropped to "next steps" |
| MODIS NDVI / EVI | Check crop calendars against observed greening | Not delivered | Dropped to "next steps" |

## Derived files
| File | Made by | What it holds |
|---|---|---|
| `outputs/results.json` | `model/decision_engine.py` | Scores, ranking, stress test, sensitivity, flip points, uncertainty ranges, scenario grid and field timeline, read by the game |

## Next steps for data
- Replace the Fallon station ET0 with a NASA-derived series (for example from POWER temperature and radiation).
- Add daily minimum temperature and wind to the POWER download (frost and a fuller ET0).
- Use SoilGrids for a soil indicator and SMAP as a regional validation of modelled water shortfall.
- Have DS2 fill the "Verified by" column and replace estimated crop values with sourced ones.