# Who owns what
| Folder | Owner | Reads | Writes |
|---|---|---|---|
| pipeline/ | DS1, Environmental Data Engineer | NASA POWER, SoilGrids, SMAP, MODIS | data/processed/*.csv incl. environmental_features.csv |
| agriculture/ + data/agricultural/ | DS2, Agricultural Data Scientist | FAO + research sources | crop_database.csv, crop_calendars.csv, rotation_strategies.json, agronomy_sources.md |
| model/ | DS3, Decision Engine & Explainability | processed CSVs + agricultural files | outputs/results.json |
| game/ | Unity Developer | outputs/results.json | WebGL build, demo video |
| docs/ | everyone | | data_sources.md, methodology.md, limitations.md |

Rule: only edit your own folder. If you need a change in someone else's output, ask them (or update docs/data_contract.md first).
