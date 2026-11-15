# Data sources and temporal coverage

## NASA POWER daily weather
- Variables: mean, maximum, and minimum 2 m air temperature (`T2M`, `T2M_MAX`, `T2M_MIN`), corrected precipitation (`PRECTOTCORR`), relative humidity at 2 m (`RH2M`), wind speed at 2 m (`WS2M`), and downward shortwave solar radiation (`ALLSKY_SFC_SW_DWN`).
- Spatial resolution: meteorological fields are approximately 0.5° × 0.625°; solar fields are approximately 1° × 1°. These are gridded estimates, not farm-station observations.
- Temporal resolution and period: daily, 2001-01-01 through 2025-12-31 in the current project download.
- Access: NASA POWER Daily Point API, Agroclimatology community, UTC.
- Files: `data/raw/nasa_power/nasa_power_raw.json`; processed as `data/processed/weather_daily.csv`.
- Limits: spatial grid cells are much larger than an individual farm. NASA missing values (-999) are converted to blank values.

## FAO-56 reference evapotranspiration (ET0)
- Variables used: NASA POWER daily temperature, humidity, 2 m wind, and solar radiation.
- Spatial resolution and period: inherits NASA POWER resolution and the 2001-2025 daily weather record.
- Temporal resolution: daily ET0 is estimated using FAO-56 Penman-Monteith; monthly totals are calculated per year and averaged by calendar month.
- Output: `data/processed/environmental_features.csv`, 12 rows for calendar months 1-12. `et0_mm_mean` is the average monthly total in mm.
- Assumption: approximate site elevation of 1,200 m is used for atmospheric pressure and clear-sky radiation calculations.

## SoilGrids 2.0 soil properties
- Variables: bulk density, clay, sand, pH in water, soil organic carbon, and nitrogen at six depth intervals (0-5, 5-15, 15-30, 30-60, 60-100, and 100-200 cm).
- Spatial resolution: 250 m gridded predictions; the project samples the grid at the farm coordinates.
- Temporal coverage: static modelled soil property maps, not a daily or annual time series.
- Files: original GeoTIFFs under `data/raw/soilgrids/`; extracted values in `data/processed/soil_properties.csv`.
- Limits: SoilGrids values are model predictions and should not be treated as field measurements.

## SMAP Enhanced L3 Radiometer Soil Moisture
- Product: `SPL3SMP_E`, Version 6; surface volumetric soil moisture, approximately top 5 cm.
- Spatial resolution: 9 km grid posting; native radiometer footprint is approximately 36 km.
- Product period: 2015-03-31 to present. The project's merged weather/SMAP analysis is limited to 2016-2025.
- Temporal resolution: daily composite.
- Files: source HDF5 granules under `data/raw/smap/`; extracted series in `data/processed/soil_moisture.csv`.
- Current local coverage: 2025-01-01 through 2025-01-07 (test period). The full interval is not downloaded yet.
- Limits: regional surface-moisture indicator, not a field-scale or root-zone measurement. Only recommended quality retrievals are retained.

## Combined daily dataset
- File: `data/processed/farm_daily_dataset.csv`.
- Period: 2016-01-01 through 2025-12-31, matching the project's SMAP analysis window and weather record.
- Temporal resolution: daily. Weather and SMAP are joined by date; static SoilGrids properties are repeated on each daily row.
- Missing SMAP dates remain blank and are not interpolated.

## MODIS vegetation index (optional)
- Product: MOD13Q1 Version 061, 16-day NDVI composite.
- Spatial resolution: 250 m. NASA AppEEARS samples the pixel at the farm coordinate and returns a point time series instead of full global granules.
- Product period: 2000 onward; the project's requested period is 2001-2025.
- NDVI scale: unitless; the pipeline normalizes the source scale factor (0.0001) and keeps observations with pixel-reliability flag 0.
- Files: the raw AppEEARS point response is saved under `data/raw/modis/`; the normalized time series is `data/processed/ndvi.csv`.
- Limits: a 250 m satellite pixel is not a field survey; cloud, snow, and other quality screening can leave gaps. The default request is a one-month test; requests longer than 31 days require `--allow-long-range`.

