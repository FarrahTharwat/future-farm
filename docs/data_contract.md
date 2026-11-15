# Data contract (DS -> Unity)

Source of truth: `outputs/results.json`, written by `python model/decision_engine.py`.
The game reads only this file. It never calculates scores, calls an API, or reads DS2's CSVs.
Change this file only with everyone's agreement, and update this document first.

## Changes since the first version
- `scores.resilience` is **removed**. Resilience is now `stress_test`, shown beside the ranking, not part of the overall.
- `farmer_priorities` has **three** keys: `water`, `climate`, `soil_health`.
- New per rotation: `stress_test`, `feasibility_warnings`, `field_timeline`, `details.water_deficit_mm`.
- New top level: `sensitivity`, `uncertainty_ranges`, `flip_points`, `et0_source`, `water_supply_assumption_mm`.
- Warming now also raises ET0, so the warming slider moves the water score as well as the climate score.
- New per rotation: `tied_with`. New in `uncertainty_ranges`: `leader_ever_changes`, `smallest_lead_points`, and `severity_cap_c` in each case.
- Flip-point sentences now read "As the weight on X rises, the top rotation changes from A to B at about N%."

## Top level
| Key | Type | Meaning |
|---|---|---|
| `data_status` | string | Banner text. Show it on every screen while it says PROTOTYPE |
| `farm` | object | name, latitude, longitude, analysis_period |
| `farmer_priorities` | object | weights for water, climate, soil_health (sum 100). Fixed in the file |
| `water_supply_assumption_mm` | number | Assumed yearly irrigation supply used for the water score |
| `rotations` | array (3) | One entry per rotation, see below |
| `ranking_stability` | object | Share of priority settings (weights swept in steps of 10) under which each rotation ranks first |
| `sensitivity` | object | Overall scores and ranking when every heat limit is shifted -2 / 0 / +2 C. Keys `heat_limit_shift_-2C`, `heat_limit_shift_+0C`, `heat_limit_shift_+2C`, each with `overall` and `ranking` |
| `uncertainty_ranges` | object | Reruns across water range (ET0 x0.9 / 1.0 / x1.1), heat limits (-2 / 0 / +2 C) and heat severity cap (5 / 10 / 15 C), see below |
| `flip_points` | object | For each criterion (`water`, `climate`, `soil_health`), where the top-ranked rotation changes as that weight is swept 0-100%, see below |
| `et0_source` | string | Where ET0 came from. If it says TEMPORARY, ET0 is a placeholder |
| `scenario_variants` | array (9) | Precomputed rain x warming grid for the sliders |
| `scenario_note` | string | How to read the grid |
| `limitations` | array of strings | Show on the "what we don't know" screen |

## Each entry in `rotations`
| Key | Type | Meaning |
|---|---|---|
| `id` | string | `current`, `alt_a`, `alt_b` |
| `name` | string | Display label |
| `crops` | array | Crop ids in the rotation |
| `cycle_years` | integer | Length of the rotation cycle |
| `rank` | integer | 1 to 3 by overall score. Strict order, even for ties |
| `tied_with` | array of rotation ids | Other rotations whose overall score is within 1 point. If not empty, show "tied" instead of a strict rank between them |
| `scores` | object | `water`, `climate`, `soil_health`, `overall`, each 0-100 (higher is better) |
| `contributions` | object | weight x score for each criterion. They add up to `overall`. Use as a stacked bar |
| `stress_test` | object | `overall_under_stress`, `retention_pct`, `rank_baseline`, `rank_under_stress`, `scenario` (rain_pct, temp_c), `note` |
| `details` | object | `yearly_irrigation_mm`, `yearly_rain_mm`, `water_deficit_mm`, `heat_days_per_year`, `bare_months_per_year` |
| `feasibility_warnings` | array of strings | Empty if the rotation fits within the assumed water supply |
| `explanations` | array of strings | Ready-made sentences to display |
| `field_timeline` | array | One entry per cycle year and month, see below |
| `heat_degree_days_per_year` | number | Total degrees above the heat limits per year across the growing seasons. Sits at rotation level, next to `details`, not inside it |
| `uncertainty` | object | `level` and `reasons` |

## `field_timeline` entries
`{"cycle_year": 1, "month": 3, "crop": "barley", "green_cover": true}`
- One entry for every month of every year of the cycle (cycle_years x 12 entries).
- `crop` is `null` when nothing is growing, and `green_cover` is then false.
- It shows what is in the ground, not growth stage. It is a typical-year calendar and does not change with the sliders.
- If two crops overlap in one month, the later one in the sequence wins.

## `scenario_variants` entries
`{"rainfall_change_percent": -20, "temperature_change_c": 1.5, "rotation_scores": {"current": {"water", "climate", "overall"}, "alt_a": {...}, "alt_b": {...}}}`
- Rain steps: -20, 0, +20. Warming steps: 0, 1.5, 3. Nine entries.
- Match the sliders to an entry on both values and read it. Do not interpolate or calculate.
- The 0 / 0 entry equals each rotation's headline scores.
- Soil health is held constant in the grid.
- When you show an order for a slider setting, treat rotations whose `overall` scores are within 1 point as tied (the same rule as `tied_with`). Alt A and Alt B swap places inside the grid.

## `uncertainty_ranges`
- `overall_range`: `{rotation_id: {"min", "max"}}` of the overall score across all cases. Draw as error bars.
- `cases_ranked_first`: how many of the cases each rotation ranks first in. `cases_total`: number of cases (27: three water factors x three heat-limit shifts x three severity caps).
- `leader_ever_changes`: true if the top rotation differs between any two cases. This is the flag to show as "is the winner stable?".
- `ranking_ever_changes`: true if the full order differs between any two cases. It can be true only because second and third place swap, so do not read it as the leader being unstable.
- `smallest_lead_points`: the smallest margin of first place over second place across all cases.
- `cases`: the individual cases, each with `et0_factor`, `heat_limit_shift_c`, `severity_cap_c`, `overall`, `ranking`.
- `note`: what was varied. Soil health is held constant.

## `flip_points`
One entry per criterion, keyed `water`, `climate`, `soil_health`. Each entry is:
| Key | Type | Meaning |
|---|---|---|
| `current_weight_pct` | number | That criterion's weight in the headline score, as a percentage |
| `leader_at_0_pct` | string | Rotation id ranked first when the criterion has 0% weight |
| `leader_at_100_pct` | string | Rotation id ranked first when the criterion has 100% weight |
| `flip_points` | array | Each change of leader: `{"at_weight_pct", "from", "to"}` (rotation ids, accurate to 0.5 points). Empty if the leader never changes |
| `top_never_changes` | boolean | True if the same rotation ranks first at every weight |
| `sentence` | string | Ready-made sentence to display. It uses full rotation names and can be long, so shorten it for small screens if needed |

- The sweep changes one weight from 0 to 100%. The other two keep their current ratio to each other.
- A slider marker can be drawn at `current_weight_pct` and at each `at_weight_pct`.
- The values depend on the current inputs and will change if DS2 updates a crop file.

## Rules for the game
1. Do not hardcode any number. Read everything from the file.
2. Do not compute scores. If you need a value that is not in the file, ask DS3 to add it.
3. Always show `data_status` and the `limitations` list.
4. Treat missing optional keys gracefully, so an older file does not crash the game.
5. After a structure change, DS3 announces it here and in the team channel before regenerating the file.

## Open decisions
- Farmer priorities are fixed in the file. If players should change them, the game would have to recompute `overall` (weight x score, normalised to sum 1). Not implemented.