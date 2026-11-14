# Agronomy Sources and Assumptions (Fallon, NV pilot)

**Status:** DRAFT. Links were located through web search on 2026-11-14. Open each one, confirm it says what is claimed, and fill in the "Verified by / date" column. No value in `crop_database.csv` should be treated as final until its source has been checked.

## Source list

| ID | Source | Used for | Link | Verified by / date |
|---|---|---|---|---|
| S01 | Allen, R.G., Pereira, L.S., Raes, D., Smith, M. (1998). *Crop evapotranspiration - Guidelines for computing crop water requirements*. FAO Irrigation and Drainage Paper 56. | Kc values, growth-stage lengths, rooting depths | https://www.fao.org/3/X0490E/X0490E00.htm (chapter pages: Ch. 6 https://www.fao.org/4/X0490E/x0490e0b.htm ; Ch. 8 https://www.fao.org/4/X0490E/x0490e0e.htm) | Tables 11, 12, 22, 23, 24 read 2026-11-14 by the DS2 assistant; a human should re-check |
| S02 | FAO ECOCROP database (now hosted through GAEZ; discontinued around 2015) | Temperature and rainfall ranges (not yet filled in) | https://www.fao.org/geospatial/data-and-tools/data-portals/ecocrop/ | |
| S03 | UNR Extension. Sample costs and returns, alfalfa hay under flood irrigation, Churchill County | Alfalfa baseline practices | https://extension.unr.edu/publication.aspx?PubID=2380 | |
| S04 | Davison, J., Solomon, J., Lawry, T. (2016). Alfalfa Variety Trial in Western Nevada. UNR Extension | Alfalfa yield and water use in western Nevada | https://extension.unr.edu/publication.aspx?PubID=2162 | |
| S05 | UNR Extension. Tef in the Lahontan Valley (fact sheet and 2004 trial) | Teff water use; typical alfalfa irrigation (3.5-4.0 acre-ft) | https://extension.unr.edu/publication.aspx?PubID=2363 | |
| S06 | McCuin, G., Solomon, J.K., Frey, J. (2025). Can We Grow Cover Crops in Nevada? Fact Sheet FS-25-10. UNR Extension | Cover crop options, water management in arid Nevada | https://extension.unr.edu/publication.aspx?PubID=7254 | |
| S07 | Walia, M.K. (2019). Benefits of Cover Crops. UNR Extension | Cover crop soil-health benefits | https://extension.unr.edu/4h/pub.aspx?PubID=2850 | |
| S08 | Bernau, C., Humphrey, M. (2020). Evaluation of Cool Season Cover Crops in the Great Basin. USDA-NRCS Great Basin Plant Materials Center, Fallon, NV | Cover crop winter hardiness, emergence, weed suppression at Fallon | https://www.nrcs.usda.gov/plantmaterials/nvpmcsr13578.pdf | |
| S09 | Clark, A. (ed.) (2007). *Managing Cover Crops Profitably*, 3rd ed. SARE | Cover crop species traits, nitrogen contributions | https://www.sare.org/wp-content/uploads/Managing-Cover-Crops-Profitably.pdf | |
| S10 | USDA NASS (1997). Usual Planting and Harvesting Dates for U.S. Field Crops, Agricultural Handbook 628 | Cross-check of calendars (old; secondary source only) | https://esmis.nal.usda.gov/concern/publications/vm40xr56k | |
| S11 | Evapotranspiration and Net Irrigation Water Requirements for Nevada. ASCE World Environmental and Water Resources Congress 2009 | Sanity check for Nevada crop water demand | https://ascelibrary.com/doi/10.1061/41036%28342%29420 | |
| S12 | USGS Scientific Investigations Report 2009-5079 (Walker River basin evapotranspiration, includes a Fallon weather station) | Local ET context | https://pubs.usgs.gov/sir/2009/5079/pdf/sir20095079.pdf | |
| S13 | UNR Experiment Station. Fallon Research Center | Crops currently under local trial (soybean, forage soybean, teff, chickpeas, dry beans, sorghum, butternut squash) | https://naes.unr.edu/facilities/fallon.aspx | Opened 2026-11-14 |
| S14 | NASA POWER Parameter Dictionary | Climate variable names to align with DS1 | https://power.larc.nasa.gov/docs/tutorials/parameters/ | |
| S15 | UNR thesis: Remotely Sensed Estimates of Evapotranspiration in Agricultural Areas of Northwestern Nevada (includes Fallon) | Optional ET cross-check | https://core.ac.uk/download/304650777.pdf | |
| S16 | Truckee-Carson Irrigation District testimony exhibit (Newlands Project allocations), California State Water Board hearing | Water-right allocation per acre (supply limit, not crop use) | https://water.waterboards.ca.gov/waterrights/water_issues/programs/hearings/truckee_river/exhibits/tcid_281.pdf | |
| S17 | USBR AgriMet, Monthly Average Reference Evapotranspiration, station FALN (Fallon, NV), 2001-2010, alfalfa reference (Kimberly-Penman 1982), inches per month | Fallon ET driver for season water and `et0_mm_mean` | https://www.usbr.gov/pn/agrimet/monthlyet.html | Read 2026-11-14 |
| S18 | FAO ECOCROP data sheets: Medicago sativa (id 1428), Zea mays (id 2175), Secale cereale (id 1929), Hordeum vulgare note (id 1232) | Temperature ranges | https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1428 (also id=2175, id=1929; barley: https://ecocrop.apps.fao.org/ecocrop/srv/en/cropView?id=1232) | Found via search 2026-11-14 |
| S19 | Illinois State Water Survey, crop requirements for Triticum aestivum (ECOCROP-derived temperature table) | Winter wheat temperatures | https://isws.illinois.edu/data/altcrops/cropreq.asp?crop=886 | Found via search 2026-11-14 |
| S20 | UC Davis Alfalfa Symposium (2002), crop coefficients for alfalfa vs grass reference ET (factor 1.2) | ETr to ETo conversion | https://alfalfasymposium.ucdavis.edu/+symposium/proceedings/2002/02-065.pdf | Found via search 2026-11-14 |
| S21 | Frontiers in Plant Science (2020), soybean phenology and cardinal temperatures | Soybean temperature context | https://www.frontiersin.org/journals/plant-science/articles/10.3389/fpls.2019.01755/pdf | Found via search 2026-11-14 |

## Values that need checking first

| Item | Where to check | Notes |
|---|---|---|
| Kc initial / mid / late for alfalfa, winter wheat, barley, corn, pea, soybean | S01, Chapter 6 (Kc table; confirm the table number) | Values in the CSV were recalled, not copied from the paper |
| Rooting depths (min and max) | S01 (maximum rooting depth table; confirm number) | Ranges for the six FAO crops were checked against Table 22 on 2026-11-14; rye, vetch and teff ranges are estimates |
| Stage lengths (initial, development, mid, late) | S01 (stage length table; confirm number) | Filled from the Table 11 rows named in the calendar notes; rye and vetch are fitted to their plant-to-termination windows |
| Kc adjustment for arid climate | S01, Kc mid/late adjustment equation for humidity and wind | Tabulated Kc values assume a sub-humid reference climate; Fallon is arid, so mid and late values may need an upward adjustment. Confirm with DS3 |
| Planting and harvest months | S03, S04, S05, S13, then S10 | Planting months are assumptions; harvest months follow from the stage lengths |
| Teff Kc and rooting depth | Search for a teff Kc source; none confirmed | Currently an analogy estimate |
| Temperature thresholds | S02 and UNR fact sheets | Filled from ECOCROP data sheets where found, otherwise estimates (see the fill-in section) |
| `season_water_min_mm` / `season_water_max_mm` | Compute from sum of Kc x ET0 over the calendar window using DS1 data (min and max from the Kc and ET0 ranges) | Filled by calculation (see the fill-in section); compare against S11. Do not use irrigation observations to fill these |
| Irrigation observations (O01-O05) | S04, S05, S16 | All unconfirmed snippets; two rows are from Reno, not Fallon |

## Conventions

- Water in **mm**, temperature in **degrees C**, rooting depth in **meters**.
- Months are calendar months 1-12. `crosses_year = yes` means the crop is planted in one year and harvested or terminated in the next.
- For cover crops, the "harvest" columns mean **termination** month.
- **Three different water quantities are kept apart on purpose:**
  1. **Crop water requirement** (`season_water_min_mm`, `season_water_max_mm` in `crop_database.csv`): what the crop needs, derived from Kc x ET0. Stored as a min and a max, never as a range in one cell.
  2. **Local irrigation observations** (`local_irrigation_observations.csv`): what was applied in trials or described as typical practice, one row per observation, with location, year, and evidence type. These depend on management, soil, and water supply, so they are not requirements.
  3. **Water-right allocations** (also in `local_irrigation_observations.csv`, evidence type `water_right_allocation`): the supply limit per acre.
- Conversion used for the observations file: 1 acre-ft per acre = 304.8 mm.
- Rooting depth is stored as `rooting_depth_min_m` and `rooting_depth_max_m` because sources report ranges. Cover-crop and teff ranges are estimates. DS3 should state whether the model uses the minimum, maximum, or midpoint.
- **Estimated values:** cells listed in each row's `estimated_fields` column are estimates or derived values, not directly sourced. Replace them with sourced values when found. Empty `none` entries in `rotation_strategies.csv` mean not applicable. Code must handle `none` as missing.

## Assumptions

1. Fallon is treated as irrigated cropland (Newlands Project water), with rainfall contributing little.
2. The alfalfa baseline cycle (4 years of stand, then a small-grain transition) is an assumption to validate locally.
3. Qualitative attributes (drought tolerance, residue level, root contribution) are judgement calls and are not yet sourced.
4. Soybean and teff are candidates because they are under local trial, not because their performance is proven.

## Known limitations

- Soil salinity and alkali issues in the Lahontan Valley are documented in historical research at the Fallon site but are not represented in the data.
- Nitrogen credit from legumes depends on management and is not given as a number, on purpose.
- Several evidence ratings are "limited" or "moderate"; DS3 should carry these into the uncertainty display.
- S10 is a 1997 publication and should not be the primary calendar source.

## FAO-56 values checked on 2026-11-14

Read from the FAO-56 web edition (Chapter 6: Tables 11 and 12; Chapter 8: Tables 22, 23, 24). The FAO notes that its web edition is an OCR scan and declines responsibility for discrepancies with the print version, so a human should still compare against the printed table for any value that drives a headline result.

| Crop in our table | FAO-56 row used | Status |
|---|---|---|
| alfalfa | Table 12 "Alfalfa Hay - averaged cutting effects"; Table 22 "Alfalfa - for hay" | Verified. The table gives Kc mid 0.95 but the chapter text says 1.05: FAO-56 disagrees with itself |
| winter_wheat | Table 12 "Winter Wheat - with frozen soils"; Table 22 "Winter Wheat" | Verified; Kc end 0.25 is the machine-harvest end of a 0.25-0.40 range |
| barley | Table 12 "Barley"; Table 22 "Barley" | Verified |
| corn_silage | Table 12 "Maize, Field (grain)"; Table 22 "Maize, Field (grain)" | Nearest match, not silage-specific; Kc end 0.60 is the high-moisture harvest value |
| field_pea | Table 12 "Peas - Dry/Seed"; Table 22 "Peas - Dry/Seed" | Verified |
| soybean | Table 12 "Soybeans"; Table 22 "Soybeans" | Verified |
| cereal_rye | Table 12 "Winter Wheat - with frozen soils" | Analog only |
| hairy_vetch | Table 12 "Clover hay, Berseem - averaged cutting effects" | Analog only |
| teff | Table 12 "Millet" | Analog only |

Still not directly sourced: heat thresholds, most qualitative ratings, planting months, and the teff, vetch and field-pea temperatures (see the fill-in section at the end).

**Arid-climate adjustment:** Table 12 values are for a sub-humid climate (minimum relative humidity about 45%, wind about 2 m/s). FAO-56 Eq. 62 adjusts Kc mid, and Eq. 65 adjusts Kc end when the table value exceeds 0.45, for other humidity, wind and crop height. Fallon is arid, so DS3 should apply it using DS1's humidity and wind data. FAO also says Kc ini should be refined for the wetting interval (Fig. 29, 30), which matters for flood irrigation.

**Optional columns the same chapters make possible (not added yet):**
- Salinity tolerance (Table 23): alfalfa ECe threshold 2.0 dS/m (moderately sensitive), barley 8.0 (tolerant), wheat 6.0, soybean 5.0, peas 1.5 (sensitive), maize 1.7, common vetch 3.0. This could address the salinity gap.
- Depletion fraction p (Table 22) and seasonal yield response Ky (Table 24, for example alfalfa 1.1, maize 1.25, soybean 0.85, winter wheat 1.05): sourced numbers that could replace the judgement-based drought tolerance rating.

## Fill-in pass (all numeric cells filled) - what each number is

Every numeric cell is now filled. The cells are not all the same quality, so `estimated_fields` lists the fields in each row that are ESTIMATES or DERIVED, not directly sourced.

| Field | How it was filled | Quality |
|---|---|---|
| `kc_*`, rooting depth (six main crops) | FAO-56 Tables 12 and 22, read 2026-11-14 | Sourced |
| `kc_*` for rye, vetch, teff | Copied from a named analog row (winter wheat, berseem clover hay, millet) | Estimate |
| Rooting depth for rye, vetch, teff | Analog rows (barley, berseem clover); teff is a low-confidence estimate | Estimate |
| `temp_min_c`, `temp_optimal_min_c`, `temp_optimal_max_c` | ECOCROP data sheets for alfalfa, maize and rye (S18); ECOCROP-derived table for wheat (S19); other crops are estimates (see `value_status`) | Mixed |
| `heat_threshold_c` | General agronomic stress threshold for daily maximum temperature. ECOCROP's temperatures are mean-temperature ranges, so its absolute maximum is not used here | Estimate |
| `season_water_min_mm`, `season_water_max_mm` | Sum of daily Kc x ETo over the stage lengths. ETo = Fallon AgriMet ETr (S17) divided by 1.2 (S20). Min and max are -10% and +10% of the central value; winter wheat min uses a season cut at 15 July | Derived |
| `et0_mm_mean` | Mean daily ETo (mm per day) over the crop's season window, same ETo source. **Unit assumption:** the column was added by a teammate with no definition, so this assumes mean daily mm | Derived; confirm unit |
| Stage lengths | FAO-56 Table 11 rows named in the calendar notes. Rye and vetch are fitted to their plant-to-termination windows | Sourced / estimate |
| Planting and harvest months | Planting months are assumptions. Harvest months are recomputed from planting plus stage length so months and stage lengths agree | Assumption |
| `none` in `rotation_strategies.csv` | Means "not applicable" (no planting that year, not a cover crop, no swap offered) | n/a |

**Cross-check:** the computed alfalfa season water (about 1000 mm, range 900-1100) is close to the typical local irrigation statement of 3.5-4.0 acre-ft (1067-1219 mm, observation O01), which is expected because irrigation also covers losses and some rainfall.

**Known weak points:**
- Winter wheat's central water use (about 1000 mm) is as high as alfalfa's because the FAO stage lengths come from Idaho (335 days). The Fallon season is probably shorter, so treat the lower end of its range (about 820 mm) as more likely and have DS3 check the Alt A water score.
- Fallon ETr is a 2001-2010 average from a station in irrigated cropland; POWER-based ET0 from DS1 may differ.
- Kc values have not had the arid-climate adjustment (FAO-56 Eq. 62), which would raise mid-season Kc by roughly 0.05.
- Teff, hairy vetch and field pea temperatures and teff rooting depth are the lowest-confidence numbers in the files.
