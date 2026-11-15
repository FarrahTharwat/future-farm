# Future Farm

Crop-rotation decision support using NASA Earth observations, shown through a Unity farm simulator.
NASA Space Apps 2026 challenge: **Field Shift**.

Three versions of the same farm in Fallon, Nevada play out side by side: the current alfalfa rotation, a water-conserving
alternative (barley, field pea, winter wheat) and a soil-health alternative (corn silage, legume and winter cover crops).
A transparent, rule-based model scores each on water, climate and soil health, explains why, and shows how stable the
ranking is and how sure we are.

> **Prototype.** Crop values are drawn from public sources but have not been validated locally. Scores compare rotations;
> they do not predict yields. Read `docs/limitations.md` before using any number.

## What it finds (prototype inputs)
| Rotation | Water | Climate | Soil health | Overall |
|---|---|---|---|---|
| Current (alfalfa stand) | 42.6 | 94.4 | 65.8 | 63.7 |
| Alt A (water-conserving) | 48.3 | 75.9 | 52.1 | 56.5 |
| Alt B (soil health) | 28.8 | 85.9 | 64.1 | 55.4 |

The alfalfa baseline ranks first in all 27 uncertainty cases, and the leader changes only if water is weighted above about 74%.
Alt A and Alt B are close and swap places under some assumptions. Full details and caveats: `docs/methodology.md`.

## Run it
The processed data files are committed, so you only need pandas and numpy to reproduce the results. Run everything from the
repo root.

```bash
python -m venv .venv && source .venv/bin/activate
pip install pandas numpy

python model/decision_engine.py     # writes outputs/results.json (the only file the game reads)
python model/check_run.py           # smoke test: inputs, consistency, results.json and docs up to date
python model/summarize_results.py   # prints the "Results at a glance" figures for docs/methodology.md
```

`check_run.py` prints `OK` when everything passes and exits with an error listing each problem otherwise.

### Optional: rebuild the data (needs `pip install -r requirements.txt`)
```bash
python pipeline/fetch_power.py        # NASA POWER daily weather 2001-2025 -> data/processed/weather_daily.csv
python pipeline/feature_engineering.py # FAO-56 ET0 by month -> data/processed/environmental_features.csv
python pipeline/fetch_soilgrids.py    # SoilGrids at the farm point -> data/processed/soil_properties.csv
```
- The farm coordinate (39.461142 N, 118.830528 W) is set at the top of each script; the scripts take no arguments.
- `fetch_smap.py` and `fetch_modis_ndvi.py` need a free NASA Earthdata account and ask you to type a confirmation. They fetch short
  test periods by default and their output is **not used in any score**. `fetch_modis_ndvi.py` reads credentials from `~/_netrc`.
- Do **not** run `pipeline/build_environmental_features.py`: it is the legacy version and writes a different file layout that the
  model cannot read.
- `model/smap_check.py` is not part of the submission run: the SMAP file is too short for it to produce a meaningful result.

## Layout
| Folder | Contents | Owner |
|---|---|---|
| `data/` | `raw/`, `processed/` (DS1) and `agricultural/` (DS2) | DS1, DS2 |
| `pipeline/` | downloaders and ET0 calculation | DS1 |
| `agriculture/` | crop-data notes | DS2 |
| `model/` | scoring, scenarios, flip points, `decision_engine.py` | DS3 |
| `outputs/` | `results.json`, read by the game | DS3 |
| `game/` | Unity project and WebGL build | Unity developer |
| `docs/` | `methodology.md`, `limitations.md`, `data_sources.md`, `data_contract.md` | everyone |

The game reads only `outputs/results.json` and never calculates scores. Its structure is documented in `docs/data_contract.md`.

## Data and sources
NASA POWER (weather), FAO-56 and ECOCROP (crop coefficients and temperatures), USBR AgriMet (ET0 cross-check), UNR Extension and
USDA-NRCS (local trials and cover crops), Newlands Project allocation (water supply). SoilGrids, SMAP and MODIS were downloaded or
tested but are not used in any score. See `docs/data_sources.md` and `data/agricultural/agronomy_sources.md`.

## Limits in one paragraph
One location, three fixed rotations, no yield or profit prediction, no frost or salinity modelling, unverified heat limits and
water supply, simple climate scenarios. This tool supports local farmers and advisers (for example UNR Extension, Churchill
County); it does not replace them. More in `docs/limitations.md`.
