# Methodology (DRAFT, written by DS3)

> **Status:** prototype. Inputs marked [UNVERIFIED] are estimates, placeholders or recalled values that must be
> checked before any result is presented as a finding. Scores compare rotations on the same footing. They do not
> predict yields.

## 1. What the tool does
For a pilot farm (Fallon, Nevada), the tool compares three crop rotations (the current alfalfa stand, a water-conserving
alternative A, and a soil-health alternative B) on three criteria, and combines them using farmer priorities.
It also reports a separate stress test, how stable the ranking is, and how much the result depends on uncertain inputs.

It is a **transparent rule-based calculation (multi-criteria decision analysis)**, not a trained machine-learning model.
No labelled data says which rotation improved which field, so a trained model could not be validated. Every number can
be traced to an input and a formula.

## 2. Inputs
| Input | Source | Status |
|---|---|---|
| Daily and monthly temperature, rainfall, humidity, solar radiation, 2001-2025 | NASA POWER, point at 39.47 N, 118.78 W (DS1) | Real |
| Monthly reference evapotranspiration (ET0) | USBR AgriMet station FALN (Fallon), 2001-2010, alfalfa-reference ET divided by 1.2 to give grass reference (DS2) | Real, but a station average from irrigated cropland, not NASA-derived |
| Crop coefficients (Kc), rooting depth, stage lengths | FAO-56 tables for six main crops; analog rows for rye, vetch and teff (DS2) | Sourced for six crops; [UNVERIFIED] until a human re-checks |
| Heat limits, qualitative ratings (nitrogen fixing, residue, roots) | DS2 estimates and judgement | [UNVERIFIED] |
| Planting and harvest months | DS2 assumptions | [UNVERIFIED] |
| Rotation sequences | DS2 rotation file | Draft |
| Water supply (1,220 mm per year) | Midpoint of a Newlands Project allocation range | [UNVERIFIED] |
| Soil properties (SoilGrids), satellite soil moisture (SMAP), vegetation (MODIS) | Not used in any score | Planned |

Datasets, resolution and periods are listed in `docs/data_sources.md`.

## 3. The three scores (each 0 to 100, higher is better)

**Water suitability.** For each crop, water need is the sum over its growing days of Kc x ET0 (FAO-56 crop coefficient
method, with ET0 spread by month). Annual crops and cover crops are walked day by day through the stage lengths
(initial, development, mid, late). Alfalfa is perennial, so its stages are spread across its growing months
(cutting-averaged Kc). Rain in the growing months is reduced to **effective rain** using a USDA-SCS style approximation
[UNVERIFIED], and the remainder is the irrigation the crop needs. A rotation's yearly irrigation is the cycle total
divided by the cycle length. Score = 100 x (1 - yearly irrigation / assumed supply), floored at 0. Because the floor
hides how large a shortage is, the real shortfall is reported separately as `water_deficit_mm`, with a
`feasibility_warnings` list.

**Climate suitability.** A heat-stress day is a day inside the crop's growing months whose daily maximum temperature
exceeds that crop's heat limit [UNVERIFIED]. Score = 100 x (1 - share of growing days that are heat-stress days), pooled
across the crops in the rotation. Frost is not modelled (daily minimum temperature has not been downloaded).

**Soil-health potential.** A checklist of agronomic benefits, **not a measurement of soil improvement** (satellites
cannot observe organic matter building up). Four indicators, weighted: nitrogen fixation (share of years with a legume
in the ground, 30%), living cover (share of the year with green cover, 30%), organic inputs (root and residue ratings,
25%), crop diversity (four or more crops is full marks, 15%). The weights and the low / medium / high ratings are
judgement [UNVERIFIED].

## 4. Combining the scores
Overall = sum of (weight x score), with weights normalised to add to 1. Default priorities are water 40%, climate 25%,
soil health 35%. These are illustrative, not validated agronomic weights. Each criterion's contribution
(weight x score) is stored in `contributions` and is the explanation of why a rotation ranks where it does.

## 5. Stress test (separate from the overall)
The water and climate scores are recalculated with 20% less rain and 1.5 C warmer, and the overall score and rank are
recomputed with the same weights and the unchanged soil score. This is shown beside the ranking as `stress_test`. It is
**not** part of the overall score: it is built from the water and climate scores, so counting it again would count the
same information twice. `retention_pct` is the stress score as a percentage of the baseline overall.

## 6. Scenarios and sensitivity
- **What-if grid:** rain (-20, 0, +20%) by warming (0, +1.5, +3 C), precomputed so the interface only looks values up.
  Soil health is held constant. The 0 / 0 entry equals each rotation's headline scores.
- **Warming and water demand:** in every warming scenario, each month's ET0 is raised by dT / (T_mean + 17.8), where
  T_mean is that month's mean temperature. This follows the temperature term of the Hargreaves equation. It is an
  approximation [UNVERIFIED] that holds temperature range, humidity, wind and sunlight constant.
- **Ranking stability:** the criterion weights are swept across all combinations in steps of 10 points, and the tool
  reports the share of settings under which each rotation ranks first.
- **Heat-limit sensitivity:** every crop's heat limit is shifted by -2, 0 and +2 C, and the overall scores and ranking
  are recomputed.
- **Uncertainty ranges:** the model is rerun for every combination of ET0 x 0.9, x 1.0, x 1.1 (matching DS2's water
  minimum and maximum of -10% and +10% around the central value) and heat limits shifted -2, 0, +2 C: nine cases. The
  tool reports the minimum and maximum overall score of each rotation, how many cases each ranks first in, and whether
  the ranking ever changes. Soil health is held constant. Headline results use the central values.

## 7. Uncertainty
Output uncertainty is reported qualitatively (currently "high") with the reasons listed, plus the ranges above, rather
than as invented confidence intervals. Main sources: unverified crop values, an ET0 series from a station average, an
assumed water supply, a rough effective-rain formula, soil information that is not yet used, coarse satellite
resolution, and uncertainty in future climate.

## 8. Decisions agreed with DS2
1. Rank on the central value of every range; use the minimum and maximum for sensitivity only.
2. Soybean and teff stay as optional swaps and are not scored as main rotations. Soybean is still in local trials, and
   the teff evidence is a single 2004 trial.
3. Soil salinity is a flag, not a score. FAO tolerance thresholds would be applied only if a farmer enters a soil
   salinity value, and the tool has no such input.

## 9. Known weak points
- **Winter wheat season length.** The FAO stage lengths come from Idaho (335 days). DS2 estimates that a season cut at
  15 July gives about 820 mm instead of about 1000 mm, and no Nevada harvest date has been found yet. Winter wheat is in
  both the current and Alt A rotations, so this affects two of the three water scores. Treat the lower value as more
  likely until DS2 updates the calendar.
- **Arid-climate Kc adjustment.** Tabulated Kc values assume a sub-humid climate. Fallon is arid, so mid and late Kc
  may be too low (FAO-56 Eq. 62 and 65). This has not been applied.
- **Heat limits** are general stress thresholds, not stage-specific. Heat severity (degrees above the limit) is not
  counted, only whether a day crosses the limit.
- **Water supply and effective rain** are unverified assumptions, and the supply is the farm-level limit, not a
  per-crop value.
- **Irrigated land.** Rain contributes little at Fallon, so the rain slider moves results only slightly.

## 10. Where local expertise is required
Crop suitability, realistic irrigation volumes and water rights, soil salinity, variety choice, harvest dates and
market considerations all need review by local farmers or agricultural advisers (for example UNR Extension, Churchill
County). This tool supports that judgement and does not replace it.