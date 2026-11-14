# pipeline/  (owner: DS1, Environmental Data Engineer)
Deliverables: weather.csv, soil_properties.csv, soil_moisture.csv, environmental_features.csv (all in data/processed/)
Scripts:
- fetch_power.py  -> weather.csv (+ raw daily in data/raw/nasa_power/)   [starter, untested]
- fetch_soilgrids.py -> soil_properties.csv                               [starter, untested]
- fetch_smap.py -> soil_moisture.csv                                      [TODO, needs free NASA Earthdata login]
- fetch_modis.py -> ndvi.csv                                              [TODO, optional]
- build_environmental_features.py -> environmental_features.csv           [starter, works on weather.csv]
Document resolution and period of each dataset in docs/data_sources.md.
