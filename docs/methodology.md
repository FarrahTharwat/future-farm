# Methodology (DRAFT, written by DS3)

> **Status:** prototype. Inputs marked [UNVERIFIED] are estimates, placeholders or recalled values that must be
> checked before any result is presented as a finding. Scores compare rotations on the same footing. They do not
> predict yields.

## 1. What the tool does
For a pilot farm (Fallon, Nevada), the tool compares three crop rotations (the current alfalfa stand, a water-conserving
alternative A, and a soil-health alternative B) on three criteria, and combines them using farmer priorities.
It also reports a separate stress test, how stable the ranking is, where the top rotation would change, and how much
the result depends on uncertain inputs.

It is a **transparent rule-based calculation (multi-criteria decision analysis)**, not a trained machine-learning model.
No labelled data says which rotation improved which field, so a trained model could not be validated. Every number can
be traced to an input and a formula.

## 2. Inputs
| Input | Source | Status |
|---|---|---|
| Daily temperature, rainfall, humidity, solar radiation, 2001-2025 | NASA POWER, point at 39.47 N, 118.78 W (DS1) | Real |
| Monthly reference evapotranspiration (ET0) | USBR AgriMet station FALN (Fallon), 2001-2010, alfalfa-reference ET divided by 1.2 to give grass reference (DS2) | Real, but a station average from irrigated cropland, not NASA-derived |
| Crop coefficients (Kc), rooting depth, stage lengths | FAO-56 tables for six main crops; analog rows for rye, vetch and teff (DS2) | Sourced for six crops; [UNVERIFIED] until a human re-checks |
| Winter wheat season | FAO-56 stage lengths come from Idaho (335 days). DS2 shortened the season to 303 days ending in July | Adjusted, not source-verified |
| Heat limits, qualitative ratings (nitrogen fixing, residue, roots) | DS2 estimates and judgement | [UNVERIFIED] |
| Planting and harvest months | DS2 assumptions | [UNVERIFIED] |
| Rotation sequences | DS2 rotation file | Draft |
| Water supply (1,220 mm per year) | Midpoint of a Newlands Project allocation range (1,067 to 1,372 mm) | [UNVERIFIED] |
| SMAP surface soil moisture | Not used. The file holds only 7 daily values (section 8) | Incomplete |
| SoilGrids soil properties, MODIS vegetation | Not used | Planned |

Datasets, resolution and periods are listed in `docs/data_sources.md`.

## 3. The three scores (each 0 to 100, higher is better)

**Water suitability.** For each crop, water need is the sum over its growing days of Kc x ET0 (FAO-56 crop coefficient
method, with ET0 given by month). Annual crops and cover crops are walked day by day through the stage lengths
(initial, development, mid, late). Alfalfa is perennial, so its stages are spread across its growing months
(cutting-averaged Kc). Rain in the growing months is reduced to **effective rain** using a USDA-SCS style approximation
[UNVERIFIED], and the remainder is the irrigation the crop needs. A rotation's yearly irrigation is the cycle total
divided by the cycle length. Each step is costed separately and the costs are added, so two crops that claim the same
field in the same month are both counted (see section 11). Score = 100 x (1 - yearly irrigation / assumed supply),
floored at 0. Because the floor hides how large a shortage is, the real shortfall is reported separately as
`water_deficit_mm`, with a `feasibility_warnings` list.

**Climate suitability.** A heat-stress day is a day inside the crop's growing months whose daily maximum temperature
exceeds that crop's heat limit [UNVERIFIED]. Each such day counts in proportion to how far it is above the limit:
excess divided by a cap, with a maximum of 1. With the default cap of 10 C, a day 2 C over counts 0.2 and a day 10 C
or more over counts 1. Score = 100 x (1 - severity-weighted stress days / growing days), pooled across the crops in the
rotation. The cap is a judgement [UNVERIFIED], so every uncertainty case (section 6) is run with caps of 5, 10 and
15 C. The cap changes how high the climate scores are and the order of Alt A and Alt B, but not which rotation leads.
`heat_days_per_year` reports the days over the limit, and `heat_degree_days_per_year` the total degrees above the limit.
Frost and stage-specific heat sensitivity are not modelled.

**Soil-health potential.** A checklist of agronomic benefits, **not a measurement of soil improvement** (satellites
cannot observe organic matter building up). Four indicators, weighted: nitrogen fixation (share of years with a legume
in the ground, 30%), living cover (share of the year with green cover, 30%), organic inputs (root and residue ratings,
25%), crop diversity (four or more crops is full marks, 15%). The weights and the low / medium / high ratings are
judgement [UNVERIFIED].

## 4. Combining the scores
Overall = sum of (weight x score), with weights normalised to add to 1. Default priorities are water 40%, climate 25%,
soil health 35%. These are illustrative, not validated agronomic weights. Each criterion's contribution
(weight x score) is stored in `contributions` and is the explanation of why a rotation ranks where it does.

**Ties.** Overall scores are reported to one decimal. If two rotations are within 1 point, `tied_with` lists the other
rotation, and the displayed order between them should be read as a tie, not a ranking.

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
  approximation [UNVERIFIED] that holds temperature range, humidity, wind and sunlight constant. At +1.5 C it raises
  annual ET0 by about 4%.
- **Ranking stability:** the criterion weights are swept across all combinations in steps of 10 points, and the tool
  reports the share of settings under which each rotation ranks first.
- **Flip points:** for each criterion, its weight is swept from 0 to 100% in 0.5-point steps while the other two keep
  their current ratio. The tool reports the weight at which the top-ranked rotation changes as that weight rises. A flip
  at a very low or very high weight means that criterion effectively never changes the leader.
- **Heat-limit sensitivity:** every crop's heat limit is shifted by -2, 0 and +2 C, and the overall scores and ranking
  are recomputed.
- **Uncertainty ranges:** the model is rerun for every combination of ET0 x 0.9, x 1.0, x 1.1 (matching DS2's water
  minimum and maximum of -10% and +10% around the central value), heat limits shifted -2, 0, +2 C, and severity caps of
  5, 10 and 15 C: 27 cases. The tool reports the minimum and maximum overall score of each rotation, how many cases each
  ranks first in, the smallest lead of first place over second, and two separate flags: whether the **leader** ever
  changes, and whether the **order** ever changes (which can happen only because second and third place swap). Soil
  health is held constant. Headline results use the central values. Each case moves all three rotations together, so
  compare rotations within a case, not across the min-max ranges.
- **Field timeline:** a typical-year calendar of what is in each field each month, built from DS2's calendars
  (the months a crop has green cover and whether it crosses the new year). If two crops overlap in a month, the later
  one in the sequence is shown. It is for the game's animation and does not feed any score.

## 7. Results at a glance (pilot inputs, prototype)
These come from the latest run, with winter wheat at 303 days ending in July. Regenerate this section from `results.json`
whenever the results change, so the numbers cannot drift out of date.

| Rotation | Water | Climate | Soil health | Overall | Under stress | Irrigation (mm per year) | Heat-stress days per year |
|---|---|---|---|---|---|---|---|
| Current (alfalfa stand) | 36.6 | 94.9 | 65.8 | 61.4 | 58.8 | 774 | 43.7 |
| Alt A (water-conserving) | 49.5 | 77.4 | 52.1 | 57.4 | 54.4 | 616 | 74.1 |
| Alt B (soil health) | 33.2 | 86.8 | 64.1 | 57.4 | 53.9 | 814 | 71.7 |

- The current rotation leads. Alternatives A and B are tied at 57.4, so `tied_with` is set on both and their order is
  not meaningful. Under the stress test they score 54.4 and 53.9, still within 1 point.
- Alt A needs about 158 mm per year less irrigation than the current rotation (616 against 774) but has the most
  heat-stress days and the lowest soil-health score. Alt B needs the most irrigation.
- Across farmer priorities, the current rotation ranks first in 77% of weight settings and Alt A in 23%. Alt B never
  ranks first.
- Flip points: Alt A takes the lead if water is weighted above about 54.5% (the default is 40%), if climate is weighted
  below about 3%, or if soil health is weighted below about 8%. The climate and soil flips are close to the edge of the
  range, so they say little about realistic priorities. The water flip is the one that matters.
- Uncertainty ranges (27 cases): the current rotation ranks first in all 27, so the leader never changes. Its lead over
  second place ranges from 1.2 to about 4.7 points. The smallest lead occurs with water demand +10%, heat limits +2 C
  and a severity cap of 15 C, which is the combination most favourable to Alt A. The order of Alt A and Alt B does change
  between cases: Alt B is ahead in most cases at a 5 C cap, Alt A in most cases at a 15 C cap, and at 10 C it depends
  on water demand. This is why the two are reported as tied.
- The heat-limit sensitivity gives the same picture: the current rotation leads at every shift, and the order of A and B
  changes.
- The gaps are small relative to the uncertainty in the inputs, and the current rotation's lead can shrink to about
  1 point under favourable assumptions for Alt A. The honest reading is "here are the tradeoffs and how stable they
  are", not "alfalfa is best".

## 8. Satellite cross-check (SMAP): not completed
`model/smap_check.py` is written to compare SMAP surface soil moisture with the model's water picture: whether soil is
driest in the months when the modelled gap between ET0 and effective rain is largest, and whether wetter-than-usual
months in the rainfall data are also wetter in SMAP. It could not be used here. The SMAP file
(`data/processed/soil_moisture.csv`) holds only 7 daily values, from 1 to 6 January 2025 (5 of them near the farm cell),
so there is no seasonal or year-to-year comparison to make and the script returns no result. The check is moved to next
steps, to be run once a full SMAP series (2015 onward) is available. SMAP is a regional (about 36 km) surface
measurement, and Fallon is irrigated land in a desert, so a weak match would be expected even then. The check feeds no
score and does not validate the water model.

## 9. Uncertainty
Output uncertainty is reported qualitatively (currently "high") with the reasons listed, plus the ranges above, rather
than as invented confidence intervals. Main sources: unverified crop values, an ET0 series from a station average, an
assumed water supply, a rough effective-rain formula, a judgement heat-severity cap, soil information that is not yet
used, coarse satellite resolution, and uncertainty in future climate.

## 10. Decisions agreed with DS2
1. Rank on the central value of every range; use the minimum and maximum for sensitivity only.
2. Soybean and teff stay as optional swaps and are not scored as main rotations. Soybean is still in local trials, and
   the teff evidence is a single 2004 trial.
3. Soil salinity is a flag, not a score. FAO tolerance thresholds would be applied only if a farmer enters a soil
   salinity value, and the tool has no such input.

## 11. Known weak points
- **Winter wheat season length.** DS2 shortened the season from the Idaho-based 335 days to 303 days, ending in July.
  DS2's checklist labels this "adjusted, not source-verified", and no Nevada harvest date has been found. Winter wheat is
  in both the current and Alt A rotations, so it affects two of the three water scores.
- **Alfalfa establishment overlap (open, owner DS2).** In the current rotation, the winter wheat that ends the cycle is
  harvested in July of the next cycle's year 1, but alfalfa establishment is listed for April to May of the same year.
  Because each step is costed separately, both crops are counted in the overlapping months. An outside review estimates
  this overstates the baseline's irrigation by roughly 90 mm per year (about 774 against about 685) and understates its
  water score by several points (about 36.6 against about 44). This is an estimate and has not been verified. The
  overlap affects the water score, and to a lesser degree the climate score, even though the field timeline feeds no
  score. The direction is conservative for the findings: fixing it would widen the current rotation's lead. A
  "one crop per field-month" rule was tried in code and rejected, because at monthly resolution it also removed the rye
  cover crop's spring water use in Alt B. The fix belongs in DS2's data (for example, alfalfa seeded in August to
  September after the wheat harvest). The game's timeline shows wheat in those months until then.
- **Arid-climate Kc adjustment.** Tabulated Kc values assume a sub-humid climate. Fallon is arid, so mid and late Kc
  may be too low (FAO-56 Eq. 62 and 65). This has not been applied.
- **Heat limits** are general stress thresholds, not stage-specific, and the severity cap is a judgement.
- **Water supply and effective rain** are unverified assumptions, and the supply is a farm-level limit, not a
  per-crop value.
- **Irrigated land.** Rain contributes little at Fallon, so the rain slider moves results only slightly.

## 12. Where local expertise is required
Crop suitability, realistic irrigation volumes and water rights, soil salinity, harvest dates, variety choice and
market considerations all need review by local farmers or agricultural advisers (for example UNR Extension, Churchill
County). This tool supports that judgement and does not replace it.