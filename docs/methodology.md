# Methodology (DRAFT, written by DS3)

> **Status:** prototype. Inputs marked [UNVERIFIED] are placeholders or recalled values that must be checked
> before any result is presented as a finding. Scores are for comparing rotations on the same footing,
> not for predicting yields.

## 1. What the tool does
For a pilot farm (Fallon, Nevada), the tool compares three crop rotations (the current one, a water-conserving
alternative, and a soil-health alternative) on four questions, and combines them using the farmer's priorities.
It is a **transparent rule-based calculation (multi-criteria decision analysis)**, not a trained machine-learning
model. No labelled data exists that says which rotation improved which field, so a trained model would be
unvalidated. Every number can be traced to an input and a formula.

## 2. Inputs
| Input | Source | Status |
|---|---|---|
| Daily and monthly temperature, rainfall, humidity, solar radiation, 2001-2025 | NASA POWER, point at 39.47 N, 118.78 W | Real |
| Monthly reference evapotranspiration (ET0) | Temporary estimate; to be replaced by DS1's NASA-derived series | [UNVERIFIED] |
| Crop coefficients (Kc), rooting depth, heat limits, qualitative ratings | DS2 crop database | [UNVERIFIED] |
| Growing months and stage lengths | DS2 crop calendars | [UNVERIFIED] |
| Rotation sequences | DS2 rotation file | Draft |
| Soil properties (SoilGrids), satellite soil moisture (SMAP), vegetation (MODIS) | Not used yet | Planned |

## 3. The four scores (each 0 to 100, higher is better)

**Water suitability.** For each crop, water use = sum over its growing months of Kc x monthly ET0 (FAO-56 crop
coefficient method, monthly simplification). Kc follows the standard curve: low early, high mid-season, lower late;
stage lengths are spread evenly across the growing months. Rain falling in those months is subtracted, leaving the
irrigation the crop would need. A rotation's yearly irrigation need is the cycle total divided by the cycle length.
Score = 100 x (1 - yearly irrigation / assumed water supply). The supply is 1,220 mm per year, the midpoint of a
reported local allocation range [UNVERIFIED]. All rain is counted as usable (no runoff or losses modelled).

**Climate suitability.** A heat-stress day is a day inside the crop's growing months whose daily maximum
temperature exceeds that crop's heat threshold [UNVERIFIED]. Score = 100 x (1 - share of growing days that are
heat-stress days), pooled across the crops in the rotation. Frost is not modelled yet.

**Soil-health potential.** A checklist of agronomic benefits, **not a measurement of soil improvement**
(satellites cannot observe organic matter building up). Four indicators, weighted: nitrogen fixation (share of
years with a legume in the ground, 30%), living cover (share of the year with green cover, 30%), organic inputs
(root and residue ratings, 25%), crop diversity (4 or more crops is full marks, 15%). The weights and the
low/medium/high ratings are judgement [UNVERIFIED] and will be agreed and sourced by the team.

**Climate resilience.** The water and climate scores are recalculated with 20% less rain and 1.5 C warmer. The score
is the share of the original suitability the rotation keeps (capped at 100). Warming currently changes heat days
but not ET0, so water need is understated in hot scenarios.

## 4. Combining the scores
Overall = sum of (farmer weight x score), with weights normalised to add to 1. Default priorities are water 40%,
soil health 30%, climate 20%, resilience 10%. These are illustrative, not validated agronomic weights; the farmer
changes them. Because the overall is a plain weighted sum, each criterion's contribution (weight x score) is shown
directly and is the explanation of why a rotation ranks where it does.

## 5. Scenarios and sensitivity
- **What-if grid:** rain (-20, 0, +20%) by warming (0, +1.5, +3 C), precomputed so the interface only looks values up.
  In the grid, soil health and resilience are held at their baseline values.
- **Ranking stability:** the weights are swept across all combinations in steps of 10 points, and the tool reports
  the share of priority settings under which each rotation ranks first.

## 6. Uncertainty
Output uncertainty is reported qualitatively (currently "high") with the reasons listed, rather than as invented
confidence intervals. Main sources: unverified crop values, temporary ET0, estimated rather than measured soil
information, coarse satellite resolution, and uncertainty in future climate.

## 7. Where local expertise is required
Crop suitability, realistic irrigation volumes and water rights, soil salinity, variety choice, and market
considerations all need review by local farmers or agricultural advisers. This tool supports that judgement and
does not replace it.