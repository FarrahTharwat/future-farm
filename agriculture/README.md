# agriculture/  (owner: DS2, Agricultural Data Scientist)
Deliverables (in data/agricultural/): crop_database.csv, crop_calendars.csv, rotation_strategies.json, agronomy_sources.md
Rules:
- Every important value needs a source (the `source` column / agronomy_sources.md). No source, no value.
- Define exactly 3 rotations for the pilot region: the current one (baseline) plus two alternatives, each with a short agronomic justification.
- Run `python agriculture/validate_crop_data.py` before telling the team you are done. It checks headers, missing sources, and that rotations only use crops in the database.
