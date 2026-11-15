using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace FieldShift.Data
{
    /// <summary>
    /// Root payload deserialized from StreamingAssets/results.json.
    /// Supports the full future-farm output contract (field_timeline, stress_test,
    /// scenario_variants grid, sensitivity, uncertainty_ranges, flip_points) as well as legacy fields.
    /// </summary>
    [Serializable]
    public class ResultsRoot
    {
        [JsonProperty("mock")]
        public bool Mock { get; set; }

        [JsonProperty("data_status")]
        public string DataStatus { get; set; }

        [JsonProperty("farm")]
        public FarmInfo Farm { get; set; } = new FarmInfo();

        [JsonProperty("farmer_priorities")]
        public FarmerPriorities FarmerPriorities { get; set; } = new FarmerPriorities();

        [JsonProperty("water_supply_assumption_mm")]
        public float WaterSupplyAssumptionMm { get; set; }

        [JsonProperty("et0_source")]
        public string Et0Source { get; set; }

        [JsonProperty("scenario_note")]
        public string ScenarioNote { get; set; }

        [JsonProperty("rotations")]
        public List<RotationData> Rotations { get; set; } = new List<RotationData>();

        [JsonProperty("ranking_stability")]
        public Dictionary<string, float> RankingStability { get; set; } = new Dictionary<string, float>();

        [JsonProperty("sensitivity")]
        public Dictionary<string, SensitivityEntry> Sensitivity { get; set; } = new Dictionary<string, SensitivityEntry>();

        [JsonProperty("uncertainty_ranges")]
        public UncertaintyRangesInfo UncertaintyRanges { get; set; }

        [JsonProperty("flip_points")]
        public Dictionary<string, FlipPointCriterion> FlipPoints { get; set; } = new Dictionary<string, FlipPointCriterion>();

        [JsonProperty("scenario_variants")]
        public List<ScenarioVariant> ScenarioVariants { get; set; } = new List<ScenarioVariant>();

        [JsonProperty("limitations")]
        public List<string> Limitations { get; set; } = new List<string>();
    }

    [Serializable]
    public class FarmInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("latitude")]
        public float Latitude { get; set; }

        [JsonProperty("longitude")]
        public float Longitude { get; set; }

        [JsonProperty("soil_source")]
        public string SoilSource { get; set; }

        [JsonProperty("analysis_period")]
        public string AnalysisPeriod { get; set; }
    }

    [Serializable]
    public class FarmerPriorities
    {
        [JsonProperty("water")]
        public float Water { get; set; }

        [JsonProperty("climate")]
        public float Climate { get; set; }

        [JsonProperty("soil_health")]
        public float SoilHealth { get; set; }

        [JsonProperty("resilience")]
        public float Resilience { get; set; }

        public FarmerPriorities Clone()
        {
            return new FarmerPriorities
            {
                Water = Water,
                Climate = Climate,
                SoilHealth = SoilHealth,
                Resilience = Resilience
            };
        }
    }

    [Serializable]
    public class RotationData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        [JsonProperty("crops")]
        public List<string> Crops { get; set; } = new List<string>();

        [JsonProperty("cycle_years")]
        public int CycleYears { get; set; }

        [JsonProperty("rotation_length_years")]
        public int RotationLengthYears { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("tied_with")]
        public List<string> TiedWith { get; set; } = new List<string>();

        [JsonProperty("seasons")]
        public List<CropSeasonEntry> Seasons { get; set; } = new List<CropSeasonEntry>();

        [JsonProperty("field_timeline")]
        public List<FieldTimelineEntry> FieldTimeline { get; set; } = new List<FieldTimelineEntry>();

        [JsonProperty("scores")]
        public CriteriaScores Scores { get; set; } = new CriteriaScores();

        [JsonProperty("contributions")]
        public CriteriaContributions Contributions { get; set; } = new CriteriaContributions();

        [JsonProperty("stress_test")]
        public StressTestInfo StressTest { get; set; }

        [JsonProperty("details")]
        public RotationDetails Details { get; set; } = new RotationDetails();

        [JsonProperty("heat_degree_days_per_year")]
        public float HeatDegreeDaysPerYear { get; set; }

        [JsonProperty("feasibility_warnings")]
        public List<string> FeasibilityWarnings { get; set; } = new List<string>();

        [JsonProperty("explanations")]
        public List<string> Explanations { get; set; } = new List<string>();

        [JsonProperty("uncertainty")]
        public UncertaintyInfo Uncertainty { get; set; } = new UncertaintyInfo();

        public int GetEffectiveCycleYears()
        {
            if (CycleYears > 0) return CycleYears;
            if (RotationLengthYears > 0) return RotationLengthYears;

            int maxFromTimeline = 1;
            if (FieldTimeline != null)
            {
                foreach (FieldTimelineEntry e in FieldTimeline)
                {
                    if (e != null && e.CycleYear > maxFromTimeline)
                    {
                        maxFromTimeline = e.CycleYear;
                    }
                }
            }
            return maxFromTimeline;
        }
    }

    [Serializable]
    public class FieldTimelineEntry
    {
        [JsonProperty("cycle_year")]
        public int CycleYear { get; set; }

        [JsonProperty("month")]
        public int Month { get; set; }

        [JsonProperty("crop")]
        public string Crop { get; set; }

        [JsonProperty("green_cover")]
        public bool GreenCover { get; set; }

        public FieldTimelineEntry Clone()
        {
            return new FieldTimelineEntry
            {
                CycleYear = CycleYear,
                Month = Month,
                Crop = Crop,
                GreenCover = GreenCover
            };
        }
    }

    [Serializable]
    public class CropSeasonEntry
    {
        [JsonProperty("year")]
        public int Year { get; set; } = 1;

        [JsonProperty("crop")]
        public string Crop { get; set; }

        [JsonIgnore]
        public string RawCropId { get; set; }

        [JsonProperty("plant_month")]
        public int PlantMonth { get; set; } = 3;

        [JsonProperty("harvest_month")]
        public int HarvestMonth { get; set; } = 9;

        public CropSeasonEntry Clone()
        {
            return new CropSeasonEntry
            {
                Year = Year,
                Crop = Crop,
                RawCropId = RawCropId,
                PlantMonth = PlantMonth,
                HarvestMonth = HarvestMonth
            };
        }
    }

    [Serializable]
    public class CriteriaScores
    {
        [JsonProperty("water")]
        public float Water { get; set; }

        [JsonProperty("climate")]
        public float Climate { get; set; }

        [JsonProperty("soil_health")]
        public float SoilHealth { get; set; }

        [JsonProperty("resilience")]
        public float Resilience { get; set; }

        [JsonProperty("overall")]
        public float Overall { get; set; }

        public CriteriaScores Clone()
        {
            return new CriteriaScores
            {
                Water = Water,
                Climate = Climate,
                SoilHealth = SoilHealth,
                Resilience = Resilience,
                Overall = Overall
            };
        }
    }

    [Serializable]
    public class CriteriaContributions
    {
        [JsonProperty("water")]
        public float Water { get; set; }

        [JsonProperty("climate")]
        public float Climate { get; set; }

        [JsonProperty("soil_health")]
        public float SoilHealth { get; set; }

        [JsonProperty("resilience")]
        public float Resilience { get; set; }

        public CriteriaContributions Clone()
        {
            return new CriteriaContributions
            {
                Water = Water,
                Climate = Climate,
                SoilHealth = SoilHealth,
                Resilience = Resilience
            };
        }
    }

    [Serializable]
    public class StressTestScenarioRef
    {
        [JsonProperty("rain_pct")]
        public float RainPct { get; set; }

        [JsonProperty("temp_c")]
        public float TempC { get; set; }

        public StressTestScenarioRef Clone()
        {
            return new StressTestScenarioRef { RainPct = RainPct, TempC = TempC };
        }
    }

    [Serializable]
    public class StressTestInfo
    {
        [JsonProperty("overall_under_stress")]
        public float OverallUnderStress { get; set; }

        [JsonProperty("retention_pct")]
        public float RetentionPct { get; set; }

        [JsonProperty("rank_baseline")]
        public int RankBaseline { get; set; }

        [JsonProperty("rank_under_stress")]
        public int RankUnderStress { get; set; }

        [JsonProperty("scenario")]
        public StressTestScenarioRef Scenario { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        public StressTestInfo Clone()
        {
            return new StressTestInfo
            {
                OverallUnderStress = OverallUnderStress,
                RetentionPct = RetentionPct,
                RankBaseline = RankBaseline,
                RankUnderStress = RankUnderStress,
                Scenario = Scenario?.Clone(),
                Note = Note
            };
        }
    }

    [Serializable]
    public class RotationDetails
    {
        // Current future-farm schema fields
        [JsonProperty("yearly_irrigation_mm")]
        public float YearlyIrrigationMm { get; set; }

        [JsonProperty("yearly_rain_mm")]
        public float YearlyRainMm { get; set; }

        [JsonProperty("water_deficit_mm")]
        public float WaterDeficitMm { get; set; }

        [JsonProperty("heat_days_per_year")]
        public float HeatDaysPerYear { get; set; }

        [JsonProperty("bare_months_per_year")]
        public float BareMonthsPerYear { get; set; }

        // Legacy fallback fields (kept for compatibility with earlier prototype JSONs)
        [JsonProperty("irrigation_requirement_mm")]
        public float LegacyIrrigationRequirementMm { get; set; }

        [JsonProperty("irrigation_level")]
        public string IrrigationLevel { get; set; }

        [JsonProperty("heat_stress_days")]
        public float LegacyHeatStressDays { get; set; }

        [JsonProperty("soil_health_indicator")]
        public string SoilHealthIndicator { get; set; }

        [JsonProperty("soil_organic_matter_trend")]
        public string SoilOrganicMatterTrend { get; set; }

        [JsonIgnore]
        public float EffectiveIrrigationMm => YearlyIrrigationMm > 0f ? YearlyIrrigationMm : LegacyIrrigationRequirementMm;

        [JsonIgnore]
        public float EffectiveHeatDays => HeatDaysPerYear > 0f ? HeatDaysPerYear : LegacyHeatStressDays;

        public RotationDetails Clone()
        {
            return new RotationDetails
            {
                YearlyIrrigationMm = YearlyIrrigationMm,
                YearlyRainMm = YearlyRainMm,
                WaterDeficitMm = WaterDeficitMm,
                HeatDaysPerYear = HeatDaysPerYear,
                BareMonthsPerYear = BareMonthsPerYear,
                LegacyIrrigationRequirementMm = LegacyIrrigationRequirementMm,
                IrrigationLevel = IrrigationLevel,
                LegacyHeatStressDays = LegacyHeatStressDays,
                SoilHealthIndicator = SoilHealthIndicator,
                SoilOrganicMatterTrend = SoilOrganicMatterTrend
            };
        }
    }

    [Serializable]
    public class UncertaintyInfo
    {
        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("reasons")]
        public List<string> Reasons { get; set; } = new List<string>();

        public UncertaintyInfo Clone()
        {
            return new UncertaintyInfo
            {
                Level = Level,
                Reasons = Reasons != null ? new List<string>(Reasons) : new List<string>()
            };
        }
    }

    [Serializable]
    public class ScenarioRotationScore
    {
        [JsonProperty("water")]
        public float Water { get; set; }

        [JsonProperty("climate")]
        public float Climate { get; set; }

        [JsonProperty("overall")]
        public float Overall { get; set; }
    }

    [Serializable]
    public class ScenarioVariant
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        // Real future-farm field names
        [JsonProperty("rainfall_change_percent")]
        public float? RainfallChangePercent { get; set; }

        [JsonProperty("temperature_change_c")]
        public float? TemperatureChangeC { get; set; }

        [JsonProperty("rotation_scores")]
        public Dictionary<string, ScenarioRotationScore> RotationScores { get; set; } = new Dictionary<string, ScenarioRotationScore>();

        // Legacy field names
        [JsonProperty("rainfall_delta_pct")]
        public float LegacyRainfallDeltaPct { get; set; }

        [JsonProperty("temperature_delta_c")]
        public float LegacyTemperatureDeltaC { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("rotations")]
        public List<ScenarioRotationOverride> Rotations { get; set; } = new List<ScenarioRotationOverride>();

        [JsonProperty("ranking_stability")]
        public Dictionary<string, float> RankingStability { get; set; } = new Dictionary<string, float>();

        [JsonIgnore]
        public float EffectiveRainfallDeltaPct => RainfallChangePercent ?? LegacyRainfallDeltaPct;

        [JsonIgnore]
        public float EffectiveTemperatureDeltaC => TemperatureChangeC ?? LegacyTemperatureDeltaC;
    }

    [Serializable]
    public class ScenarioRotationOverride
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("scores")]
        public CriteriaScores Scores { get; set; }

        [JsonProperty("contributions")]
        public CriteriaContributions Contributions { get; set; }

        [JsonProperty("details")]
        public RotationDetails Details { get; set; }

        [JsonProperty("explanations")]
        public List<string> Explanations { get; set; }
    }

    [Serializable]
    public class SensitivityEntry
    {
        [JsonProperty("overall")]
        public Dictionary<string, float> Overall { get; set; } = new Dictionary<string, float>();

        [JsonProperty("ranking")]
        public List<string> Ranking { get; set; } = new List<string>();
    }

    [Serializable]
    public class UncertaintyScoreRange
    {
        [JsonProperty("min")]
        public float Min { get; set; }

        [JsonProperty("max")]
        public float Max { get; set; }
    }

    [Serializable]
    public class UncertaintyMatrixCase
    {
        [JsonProperty("et0_factor")]
        public float Et0Factor { get; set; }

        [JsonProperty("heat_limit_shift_c")]
        public float HeatLimitShiftC { get; set; }

        [JsonProperty("severity_cap_c")]
        public float SeverityCapC { get; set; }

        [JsonProperty("overall")]
        public Dictionary<string, float> Overall { get; set; } = new Dictionary<string, float>();

        [JsonProperty("ranking")]
        public List<string> Ranking { get; set; } = new List<string>();
    }

    [Serializable]
    public class UncertaintyRangesInfo
    {
        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("overall_range")]
        public Dictionary<string, UncertaintyScoreRange> OverallRange { get; set; } = new Dictionary<string, UncertaintyScoreRange>();

        [JsonProperty("cases_ranked_first")]
        public Dictionary<string, int> CasesRankedFirst { get; set; } = new Dictionary<string, int>();

        [JsonProperty("cases_total")]
        public int CasesTotal { get; set; }

        [JsonProperty("leader_ever_changes")]
        public bool LeaderEverChanges { get; set; }

        [JsonProperty("ranking_ever_changes")]
        public bool RankingEverChanges { get; set; }

        [JsonProperty("smallest_lead_points")]
        public float SmallestLeadPoints { get; set; }

        [JsonProperty("cases")]
        public List<UncertaintyMatrixCase> Cases { get; set; } = new List<UncertaintyMatrixCase>();
    }

    [Serializable]
    public class FlipPointStep
    {
        [JsonProperty("at_weight_pct")]
        public float AtWeightPct { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }

    [Serializable]
    public class FlipPointCriterion
    {
        [JsonProperty("current_weight_pct")]
        public float CurrentWeightPct { get; set; }

        [JsonProperty("leader_at_0_pct")]
        public string LeaderAt0Pct { get; set; }

        [JsonProperty("leader_at_100_pct")]
        public string LeaderAt100Pct { get; set; }

        [JsonProperty("flip_points")]
        public List<FlipPointStep> FlipPoints { get; set; } = new List<FlipPointStep>();

        [JsonProperty("top_never_changes")]
        public bool TopNeverChanges { get; set; }

        [JsonProperty("sentence")]
        public string Sentence { get; set; }
    }

    public static class ResultsSchemaHelpers
    {
        public static string FormatCropDisplayName(string rawCrop)
        {
            if (string.IsNullOrWhiteSpace(rawCrop))
            {
                return string.Empty;
            }

            switch (rawCrop.Trim().ToLowerInvariant())
            {
                case "alfalfa_establishment":
                    return "Alfalfa (Est.)";
                case "alfalfa":
                    return "Alfalfa";
                case "winter_wheat":
                    return "Winter Wheat";
                case "barley":
                    return "Barley";
                case "field_pea":
                    return "Field Pea";
                case "corn_silage":
                    return "Corn Silage";
                case "cereal_rye":
                    return "Cereal Rye";
                case "hairy_vetch":
                    return "Hairy Vetch";
            }

            string spaced = rawCrop.Replace('_', ' ').Trim();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.ToLowerInvariant());
        }

        /// <summary>
        /// Splits a verbose rotation title such as "Alternative A: Water-conserving small-grain and pea rotation"
        /// or "Current rotation (baseline: alfalfa stand)" into a compact card title + descriptive subtitle.
        /// </summary>
        public static void ParseRotationDisplayName(string rawName, string id, out string shortTitle, out string subtitle)
        {
            shortTitle = string.IsNullOrWhiteSpace(rawName) ? (id ?? "Rotation") : rawName.Trim();
            subtitle = string.Empty;

            if (string.IsNullOrWhiteSpace(rawName))
            {
                return;
            }

            int colonIdx = rawName.IndexOf(':');
            int parenIdx = rawName.IndexOf('(');

            if (colonIdx > 0 && (parenIdx < 0 || colonIdx < parenIdx))
            {
                shortTitle = rawName.Substring(0, colonIdx).Trim();
                subtitle = CapitalizeFirst(rawName.Substring(colonIdx + 1).Trim());
                return;
            }

            if (parenIdx > 0)
            {
                shortTitle = rawName.Substring(0, parenIdx).Trim();
                string inside = rawName.Substring(parenIdx + 1).Trim().TrimEnd(')').Trim();
                if (inside.StartsWith("baseline:", StringComparison.OrdinalIgnoreCase))
                {
                    inside = inside.Substring("baseline:".Length).Trim();
                }
                subtitle = CapitalizeFirst(inside);
            }
        }

        /// <summary>
        /// Converts month-by-month field_timeline entries into grouped per-year planting-to-harvest segments.
        /// </summary>
        public static List<CropSeasonEntry> BuildSeasonsFromTimeline(List<FieldTimelineEntry> timeline)
        {
            var result = new List<CropSeasonEntry>();
            if (timeline == null || timeline.Count == 0)
            {
                return result;
            }

            var sorted = new List<FieldTimelineEntry>(timeline);
            sorted.Sort((a, b) =>
            {
                int cmp = a.CycleYear.CompareTo(b.CycleYear);
                return cmp != 0 ? cmp : a.Month.CompareTo(b.Month);
            });

            CropSeasonEntry active = null;
            int lastMonth = -1;

            foreach (FieldTimelineEntry entry in sorted)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Crop))
                {
                    if (active != null)
                    {
                        result.Add(active);
                        active = null;
                    }
                    lastMonth = -1;
                    continue;
                }

                string rawId = entry.Crop.Trim();
                if (active != null
                    && active.Year == entry.CycleYear
                    && string.Equals(active.RawCropId, rawId, StringComparison.OrdinalIgnoreCase)
                    && entry.Month == lastMonth + 1)
                {
                    active.HarvestMonth = entry.Month;
                    lastMonth = entry.Month;
                }
                else
                {
                    if (active != null)
                    {
                        result.Add(active);
                    }

                    active = new CropSeasonEntry
                    {
                        Year = Mathf.Max(1, entry.CycleYear),
                        RawCropId = rawId,
                        Crop = FormatCropDisplayName(rawId),
                        PlantMonth = Mathf.Clamp(entry.Month, 1, 12),
                        HarvestMonth = Mathf.Clamp(entry.Month, 1, 12)
                    };
                    lastMonth = entry.Month;
                }
            }

            if (active != null)
            {
                result.Add(active);
            }

            return result;
        }

        private static string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length == 1) return s.ToUpperInvariant();
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
