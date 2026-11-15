# Methodology

> **Status:** prototype for discussion. Scores compare three rotations on the same footing; they do not predict yields,
> profit or soil change. Inputs marked **[UNVERIFIED]** are estimates, analogs or assumptions that no human has yet checked
> against a source. Section 7 is generated from `outputs/results.json`; `python model/check_run.py` fails if it drifts.

## 1. What the tool does
For a pilot farm (Lattin Farms, Fallon, Nevada) the tool compares three crop rotations:

| ID | Rotation | Cycle |
|---|---|---|
| `current` | Baseline: alfalfa stand (establishment, three production years, winter wheat transition) | 5 years |
| `alt_a` | Water-conserving: barley, field pea, winter wheat | 3 years |
| `alt_b` | Soil health: corn silage, cereal rye cover, field pea, hairy vetch cover | 2 years |

Each is scored on water, climate and soil health, combined with illustrative farmer priorities. The tool also reports a
separate stress test, how stable the ranking is, where the top rotation would change, and how much the result depends on
uncertain inputs.

It is a **transparent rule-based calculation (multi-criteria decision analysis)**, not a trained model. No labelled data says
which rotation improved which field, so a trained model could not be validated. Every number traces to an input and a
formula, and each criterion's contribution (weight x score) is the explanation, so no SHAP or LLM is used in the scoring.

## 2. Inputs
| Input | Source | Status |
|---|---|---|
| Daily weather 2001-2025 (T2M, T2M_MAX, T2M_MIN, rain, humidity, wind, solar) | NASA POWER, one point at 39.461142 N, 118.830528 W (DS1) | Real; gridded estimate, not a farm station |
| Monthly reference ET0 | DS1: FAO-56 Penman-Monteith from the daily weather, monthly mean 2001-2025 (`environmental_features.csv`) | Real; assumes 1,200 m elevation |
| ET0 cross-check | USBR AgriMet station FALN, 2001-2010, alfalfa reference / 1.2 (DS2); selectable with `ET0_SOURCE = "agrimet"` | Real; annual total 1,320 mm vs 1,400 mm NASA-derived (+6%) |
| Kc, rooting depth, stage lengths | FAO-56 tables for six crops; analog rows for rye, vetch, teff (DS2) | Six crops sourced, **[UNVERIFIED]** by a second person; rye/vetch/teff are analogs |
| Winter wheat season | FAO-56 Idaho row (335 days) shortened by DS2 to 303 days ending 15 July | Adjusted, not source-verified |
| Heat limits, qualitative ratings, planting months | DS2 estimates and judgement | **[UNVERIFIED]** |
| Rotation sequences | DS2 (`rotation_strategies.csv`) | Draft; not confirmed with UNR Extension |
| Water supply 1,220 mm per year | Midpoint of the Newlands Project allocation range 1,067-1,372 mm (S16) | **[UNVERIFIED]**; typed into `model/water_need.py` |
| SoilGrids, SMAP, MODIS | Downloaded or tested only | **Not used in any score** |

Datasets, resolutions and periods are in `docs/data_sources.md`; agronomy sources S01-S22 in `data/agricultural/agronomy_sources.md`.

## 3. The three scores (each 0 to 100, higher is better)

**Water suitability.** A crop's need is the sum over its growing days of Kc x ET0 (FAO-56 crop coefficient method with monthly
ET0). Annual and cover crops are walked day by day through the four stages (initial, development, mid, late) from the planting
month. Alfalfa is perennial, so its stages are spread over its growing months (cutting-averaged Kc). Rain is reduced to
**effective rain** with a USDA-SCS-style formula **[UNVERIFIED]**, and the remainder is irrigation. A rotation's yearly
irrigation is the cycle total divided by the cycle length; each step is costed separately and cover crops count.
`Score = 100 x (1 - yearly irrigation / 1,220 mm)`, floored at 0. Because the floor hides shortages, the real shortfall is
reported as `water_deficit_mm` with a `feasibility_warnings` list.

**Climate suitability.** A heat-stress day is a day in the crop's growing months whose daily maximum exceeds the crop's heat
limit **[UNVERIFIED]**. Each such day counts in proportion to its excess over the limit divided by a 10 C cap (a day 2 C over
counts 0.2; 10 C or more counts 1). `Score = 100 x (1 - severity-weighted stress days / growing days)`, pooled over the crops
of the rotation. The cap is a judgement, so the uncertainty cases also use 5 and 15 C. Frost and stage-specific heat
sensitivity (flowering) are not modelled. `heat_days_per_year` and `heat_degree_days_per_year` are reported alongside.

**Soil-health potential.** A checklist of agronomic benefits, **not a measurement of soil improvement**: nitrogen fixation
(share of cycle years with a legume in the ground, 30%), living cover (share of the year with green cover, 30%), organic inputs
(root and residue ratings, 25%) and crop diversity (four or more crops = full marks, 15%). The weights and the
low/medium/high ratings are judgement **[UNVERIFIED]**.

## 4. Combining the scores
`Overall = sum(weight x score)` with weights normalised to 1. Default priorities are water 40%, climate 25%, soil health 35%.
They are illustrative, not agronomic findings. Contributions are stored in `contributions` and drawn as the explanation.

**Ties.** Scores are reported to one decimal. If two rotations are within 1 point, `tied_with` lists the other and the order
should be read as a tie. At the default settings no pair is within 1 point, but Alt A and Alt B are only 1.1 apart and swap
places in many sensitivity cases (section 6), so treat their order as weak.

## 5. Stress test (separate from the overall)
Water and climate are recalculated with 20% less rain and +1.5 C, and the overall and rank are recomputed with the unchanged
soil score. It is shown beside the ranking as `stress_test` and is **not** part of the overall, because it is built from the
water and climate scores and counting it again would double-count them. `retention_pct` is the stressed overall as a
percentage of the baseline overall.

## 6. Scenarios and sensitivity
- **What-if grid:** rain (-20, 0, +20%) by warming (0, +1.5, +3 C), precomputed; soil health held constant; the 0/0 entry equals
  the headline scores. Warming raises each month's ET0 by `dT / (T_mean + 17.8)` (Hargreaves temperature term) **[UNVERIFIED]**,
  holding humidity, wind and sunlight constant; about +4% annual ET0 at +1.5 C.
- **Ranking stability:** weights swept in steps of 10 points; share of settings in which each rotation ranks first.
- **Flip points:** one weight swept 0-100% in 0.5-point steps, the other two keeping their ratio; reports where the leader changes.
- **Heat-limit sensitivity:** every crop's limit shifted -2 / 0 / +2 C.
- **Uncertainty ranges:** 27 cases = ET0 x0.9 / 1.0 / x1.1 (DS2's -10%/+10% water range) x heat limits -2 / 0 / +2 C x severity
  caps 5 / 10 / 15 C. Reports each rotation's min and max overall, how many cases it ranks first in, the smallest lead, and two
  flags: `leader_ever_changes` and `ranking_ever_changes` (the latter can be true only because 2nd and 3rd swap). Each case
  moves all rotations together, so compare rotations within a case, not across min-max ranges.
- **Field timeline:** typical-year calendar of what is in each field, from DS2's calendars. It feeds the game animation only,
  not any score.

## 7. Results at a glance (from `outputs/results.json`)

| Rotation | Water | Climate | Soil health | Overall | Under stress | Irrigation (mm/yr) | Heat-stress days/yr | Bare months/yr |
|---|---|---|---|---|---|---|---|---|
| Current (alfalfa stand) | 42.6 | 94.4 | 65.8 | 63.7 | 61.2 | 701 | 41.3 | 5.8 |
| Alt A (water-conserving) | 48.3 | 75.9 | 52.1 | 56.5 | 53.5 | 630 | 77.1 | 6.3 |
| Alt B (soil health) | 28.8 | 85.9 | 64.1 | 55.4 | 51.8 | 868 | 75.7 | 4.0 |

- **Ranking:** Current 1st, Alt A 2nd, Alt B 3rd, and the same order under the stress test. No rotation exceeds the assumed
  1,220 mm supply (all water deficits are 0).
- **Water:** Alt A needs about 71 mm per year less irrigation than Current; Alt B needs about 167 mm more. Alt A pays for that
  with the most heat-stress days and the lowest soil-health score.
- **Priorities:** Current ranks first in 91% of weight settings and Alt A in 9%; Alt B never does. The only flip is on water:
  Alt A takes the lead if water is weighted above about 74% (default 40%). Current stays first at every climate and every
  soil-health weight.
- **Uncertainty (27 cases):** Current ranks first in all 27, so the leader never changes. Its smallest lead is 4.6 points.
  Overall ranges: Current 57.6-67.2, Alt A 49.5-62.2, Alt B 48.7-60.8. The order of Alt A and Alt B does change between cases
  (Alt B is ahead in most cases at a 5 C cap), which is why their order is not meaningful.
- **Heat limits -2 / 0 / +2 C:** Current leads every time (62.3 / 63.7 / 64.5); Alt A 54.5 / 56.5 / 58.5; Alt B 54.0 / 55.4 / 56.6.
- **Input swaps:** changing weather file or ET0 source (NASA-derived vs AgriMet) moved each overall score by at most about 2 points.

**Honest reading:** under these inputs the alfalfa baseline scores highest, mainly because it keeps green cover, fixes
nitrogen, and has fewer heat-stress days, even though it uses a lot of water. The tool shows the trade-offs and how stable they
are. It is not evidence that "alfalfa is best", and the gaps are small next to the uncertainty in the inputs.

## 8. Checks
- **ET0 cross-check:** NASA-derived and AgriMet monthly ET0 agree within 0-6% from March to August, differ by 12% in September and by
  20-34% from October to February, when little is growing; annual totals differ by 6%.
- **Internal consistency** (`python model/check_run.py`): inputs complete, 0/0 scenario equals the headline scores, contributions
  sum to the overall, timelines cover every month, `results.json` is current, and section 7 quotes the current numbers.
- **SMAP cross-check: not completed.** `model/smap_check.py` needs a multi-year SMAP series; the file holds only 1-7 January
  2025 (5 days near the farm cell). SMAP is a ~36 km surface measurement over irrigated desert land, so even a full series
  would be a weak check. It feeds no score and does not validate the water model.

## 9. Uncertainty
Reported qualitatively (level "high", with reasons) plus the ranges above, not as invented confidence intervals. Main sources:
unverified crop values, analog rows, an assumed water supply, a rough effective-rain formula, a judgement heat-severity cap,
unused soil data, coarse satellite resolution, and simple future-climate scenarios.

## 10. Decisions agreed with DS2
1. Rank on the central value of every range; use min and max for sensitivity only.
2. Soybean and teff stay as optional swaps and are not scored (soybean is in local trials; teff evidence is one 2004 trial).
3. Soil salinity is a flag, not a score; FAO thresholds would apply only if a farmer entered a salinity value.

## 11. Known weak points
- **Winter wheat season** was shortened by DS2 from 335 to 303 days ending in July; no Nevada harvest date has been found. Wheat is
  in two of the three rotations.
- **Alfalfa seeding in August-September** after the wheat harvest is an unsourced assumption (needs UNR Extension). The earlier
  overlap between wheat and alfalfa establishment no longer appears in the field timeline.
- **Year 5 of the current rotation** has no spring-summer crop between the last alfalfa year and fall wheat, so the field is bare.
- **Arid-climate Kc adjustment** (FAO-56 Eq. 62/65) is not applied; mid-season Kc may be about 0.05 too low.
- **Heat limits** are general thresholds, not stage-specific, and the severity cap is a judgement.
- **Water supply and effective rain** are unverified; supply is a farm-level limit, not a per-crop value.
- **Irrigated land:** rain contributes little, so the rain slider moves results only slightly.
- **Source verification:** most sources (S03-S09, S11, S12, S14-S16) were located by search and not read in full by a person;
  several "Verified by" entries are the DS2 owner's self-report.

## 12. Where local expertise is required
Crop suitability, realistic irrigation volumes and water rights, soil salinity, harvest dates, variety choice and markets need
review by local farmers or advisers (for example UNR Extension, Churchill County). This tool supports their judgement and does
not replace it.

## 13. Reproducing the results
From the repo root: `python model/decision_engine.py` writes `outputs/results.json`; `python model/check_run.py` verifies it;
`python model/summarize_results.py` prints the section 7 figures. See `README.md` for setup.