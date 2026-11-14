"""TODO (DS3, you): build per-rotation features.
Inputs : data/processed/environmental_features.csv (DS1), data/agricultural/*.csv and rotation_strategies.json (DS2)
Work   : for each crop's growing months compare crop water need vs seasonal rain, count heat exposure,
         find bare-soil months from the calendars, credit legumes / cover crops / deep roots.
Output : the 0-100 sub-scores (water, climate, soil_health, resilience) fed into decision_engine.py
"""
