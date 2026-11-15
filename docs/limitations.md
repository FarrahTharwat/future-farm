# Limitations

This tool compares three rotations on the same footing and shows how stable the comparison is. It is a prototype for
discussion, not a recommendation engine, and nothing here has been validated on a real farm.

## What the scores are not
- They are **not yield or profit predictions**. They compare rotations against each other using simple rules.
- The soil-health score is a **checklist of potential benefits** (legumes, green cover, roots and residue, crop variety), not a
  measurement of soil improvement. Satellites cannot see organic matter building up.
- The overall score depends on **illustrative farmer priorities** (water 40%, climate 25%, soil health 35%). The tool shows where
  the leader changes as those weights move (for water, only above about 74%).
- The gaps between rotations are small compared with the uncertainty in the inputs. The current rotation leads in all 27
  uncertainty cases, but Alt A and Alt B are only about 1 point apart and swap places in many of them, so their order means little.

## Data
- NASA POWER describes a coarse grid cell (about 0.5 x 0.625 degrees) around one point, not a single field, and its days follow UTC.
- Reference evapotranspiration (ET0) is computed from that weather with FAO-56 Penman-Monteith, assuming 1,200 m elevation. It
  agrees with the Fallon AgriMet station within about 6% over the year (more in winter), but wind comes from a coarse product.
- Satellite surface soil moisture (SMAP) is not root-zone moisture and is **not used in any score**. Only 7 days (1-7 January
  2025) were downloaded, so the planned cross-check with the water model could not be done.
- SoilGrids values are model predictions at 250 m, not field measurements, and are **not used in any score**.
- MODIS NDVI was only test-requested (2 rows) and is **not used**. Satellite pixels (250 m to 9 km) cannot resolve small fields.

## Crop values and sources
- Crop values come from public sources (FAO-56, UNR Extension, NRCS, FAO ECOCROP) and **need local validation**.
- Kc, rooting depth and stage lengths for six crops come from FAO-56 tables. FAO-56 disagrees with itself on alfalfa mid-season Kc
  (0.95 in the table, 1.05 in the text). Its web edition is an OCR scan.
- Heat limits, qualitative ratings (nitrogen fixing, residue, roots), planting months, and all rye, vetch and teff values are
  estimates or analogs, not directly sourced. Field-pea temperatures are among the lowest-confidence numbers.
- Most UNR Extension and other source links were located by search and **have not been read in full by a person**. Many "Verified
  by" entries are empty or are the DS2 owner's self-report.
- Kc values are not adjusted for Fallon's arid climate, which would raise mid-season values slightly (about 0.05).
- Winter wheat's season was shortened from the Idaho-based 335 days to 303 days ending in July. This is an adjustment, not a
  source-verified value, and no Nevada harvest date has been found. Winter wheat is in two of the three rotations.
- Alfalfa is assumed to be seeded in August-September after the wheat harvest. This is not sourced for Fallon.
- In the current rotation, spring-summer of year 5 has no crop, so the field is bare for part of the cycle.
- Soybean and teff appear only as optional swaps and are not scored. Teff evidence is a single 2004 trial.

## Water
- The assumed water supply (1,220 mm per year) is the midpoint of an allocation range and is unverified. It is a supply limit, not a
  crop requirement, and is typed into the code by hand.
- The effective-rainfall formula is an approximation that has not been checked against the FAO source.
- Warming raises water demand with an approximate temperature scaling, not a full evapotranspiration model; humidity, wind and
  sunlight are held constant.
- Rain contributes little at an irrigated site such as Fallon, so the rain slider changes results only slightly.

## Heat and climate
- Heat stress uses one limit per crop for the whole season, not stage-specific limits (flowering is far more sensitive).
- Severity is capped at 10 C above the limit, which is a judgement. The leader is unchanged with caps of 5 and 15 C, but the order of
  Alt A and Alt B changes.
- Frost risk is not modelled (no daily minimum temperature is used in any score).
- Future climate is represented by simple scenarios (up to 3 C warmer, 20% less or more rain), not climate-model projections.

## Not modelled
- Soil salinity and alkali problems, which are documented in the Lahontan Valley. This is a flag only: FAO tolerance thresholds
  would apply if a farmer entered a soil salinity value, and the tool has no such input.
- Market prices, labour, equipment, water rights and policy.
- Pests, disease, variety choice and management.
- Nitrogen credit from legumes (it depends on management and is not given as a number).

## Pilot scope
- One location (Fallon, Nevada) and three fixed rotations. The tool accepts other coordinates, but the crops, calendars and water
  rules would all need replacing.

## Needs local expertise
Crop suitability, realistic irrigation volumes, water rights, soil salinity, harvest dates and variety choice should be reviewed by
local farmers and agricultural advisers (for example UNR Extension, Churchill County). This tool supports their judgement and does
not replace it.