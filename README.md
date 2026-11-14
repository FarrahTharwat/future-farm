# Future Farm
Crop-rotation decision support using NASA Earth observations, shown through a Unity farm simulator.
NASA Space Apps 2026 challenge: Field Shift.

## Quick start
```
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
python pipeline/fetch_power.py --lat 39.47 --lon -118.78
python pipeline/fetch_soilgrids.py --lat 39.47 --lon -118.78
PYTHONPATH=model python model/decision_engine.py
```
Output: outputs/results.json (currently MOCK data).

## Layout
data/ (raw, processed, agricultural) | pipeline/ (downloaders) | model/ (scoring) | game/ (Unity) | outputs/ | docs/
