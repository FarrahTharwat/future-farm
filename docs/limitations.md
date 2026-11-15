# Limitations

This tool compares three rotations on the same footing and shows how stable the comparison is. It is a prototype for
discussion, not a recommendation engine, and nothing here has been validated on a real farm.

## What the scores are not
- They are **not yield or profit predictions**. They compare rotations against each other using simple rules.
- The soil-health score is a **checklist of potential benefits** (legumes, green cover, roots and residue, crop
  variety), not a measurement of soil improvement. Satellites cannot see organic matter building up.
- The overall score depends on **farmer priorities that are illustrative** (water 40%, climate 25%, soil health 35%).
  The tool shows where the leader changes as those weights move.
- The gaps between rotations are small compared with the uncertainty in the inputs.

## Data
- Satellite surface soil moisture (SMAP) is not root-zone moisture, and it is not used in any score.
- SoilGrids values are model predictions, not field measurements, and they are not used in any score yet.
- NASA POWER values describe a coarse grid cell around one point, not a single field.
- Satellite pixels (250 m to 9 km) cannot resolve small individual fields.
- Reference evapotranspiration (ET0) comes from a 2001-2010 station average at Fallon, not from NASA data. It is
  converted from alfalfa-reference to grass-reference ET.

## Crop values
- Crop values come from public sources (FAO-56, UNR Extension, NRCS) and **need local validation**.
- Heat limits, qualitative ratings (nitrogen fixing, residue, roots), planting months and the rye, vetch and teff
  values are estimates or analogs, not directly sourced.
- Kc values are not adjusted for Fallon's arid climate, which would raise mid-season values slightly.
- Winter wheat's season was shortened from the Idaho-based 335 days to 303 days ending in July. This is an adjustment,
  not a source-verified value, and no Nevada harvest date has been found. Winter wheat is in two of the three rotations.
- In the current rotation, the alfalfa establishment months overlap with the end of the previous cycle's winter wheat,
  and both crops are counted in the overlapping months. An outside review estimates this overstates the baseline's
  irrigation by roughly 90 mm per year (unverified). The intended sequence needs to be confirmed with DS2. Fixing it
  would widen the current rotation's lead, not narrow it.
- The two alternatives are currently tied on the overall score, and their order changes with the heat-severity cap and
  water demand. The tool flags this with `tied_with` rather than showing a false ranking.

## Water
- The assumed water supply (1,220 mm per year) is the midpoint of an allocation range and is unverified.
- The effective-rainfall formula is an approximation that has not been checked against the FAO source.
- Warming raises water demand using an approximate temperature scaling, not a full evapotranspiration model. It holds
  humidity, wind and sunlight constant.
- Rain contributes little at an irrigated site such as Fallon, so the rain slider changes results only slightly.

## Heat and climate
- Heat stress uses one limit per crop for the whole season, not stage-specific limits (flowering is far more sensitive).
- Severity is capped at 10 C above the limit, which is a judgement. The ranking is unchanged with caps of 5 and 15 C.
- Frost risk is not modelled (no daily minimum temperature was downloaded).
- Future climate is represented by simple scenarios (up to 3 C warmer, 20% less or more rain), not climate-model
  projections.

## Not modelled
- Soil salinity and alkali problems, which are documented in the Lahontan Valley. A flag only: FAO tolerance
  thresholds would apply only if a farmer entered a soil salinity value, and the tool has no such input.
- Market prices, labour, equipment, water rights and policy.
- Pests, disease, variety choice and management.
- Nitrogen credit from legumes (it depends on management and is not given as a number).

## Pilot scope
- One location (Fallon, Nevada) and three fixed rotations. Soybean and teff appear only as optional swaps and are not
  scored. The tool accepts other coordinates, but the crops, calendars and water rules would all need replacing.

## Needs local expertise
Crop suitability, realistic irrigation volumes, water rights, soil salinity, harvest dates and variety choice should be
reviewed by local farmers and agricultural advisers (for example UNR Extension, Churchill County). This tool supports
their judgement and does not replace it.