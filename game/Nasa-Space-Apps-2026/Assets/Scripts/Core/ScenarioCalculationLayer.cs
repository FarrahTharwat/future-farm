using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FieldShift.Data;

namespace FieldShift.Core
{
    [Serializable]
    public struct ClimateInputs
    {
        public float RainfallDeltaPct;
        public float TemperatureDeltaC;

        public ClimateInputs(float rainfallDeltaPct, float temperatureDeltaC)
        {
            RainfallDeltaPct = rainfallDeltaPct;
            TemperatureDeltaC = temperatureDeltaC;
        }
    }

    /// <summary>
    /// Evaluated rotation view consumed by UI and FarmPlotViews.
    /// Produced by ScenarioCalculationLayer so UI never reads raw scenario overrides directly.
    /// </summary>
    [Serializable]
    public class EvaluatedRotationData
    {
        public string Id;
        public string Name;
        public string FullName;
        public string Subtitle;
        public List<string> Crops;
        public List<string> RawCrops;
        public int RotationLengthYears;
        public List<CropSeasonEntry> Seasons;
        public List<FieldTimelineEntry> FieldTimeline;
        public CriteriaScores Scores;
        public CriteriaContributions Contributions;
        public StressTestInfo StressTest;
        public RotationDetails Details;
        public float HeatDegreeDaysPerYear;
        public List<string> FeasibilityWarnings;
        public List<string> Explanations;
        public UncertaintyInfo Uncertainty;
        public int Rank;
        public List<string> TiedWith;
        public float RankingStabilityShare;
    }

    [Serializable]
    public class EvaluatedScenarioState
    {
        public ClimateInputs Inputs;
        public ClimateInputs MatchedVariantInputs;
        public bool IsExactScenarioMatch;
        public string ScenarioId;
        public string ScenarioLabel;
        public bool IsUsingScenarioFallback;
        public float WaterSupplyAssumptionMm;
        public string Et0Source;
        public string ScenarioNote;
        public FarmerPriorities FarmerPriorities;
        public List<EvaluatedRotationData> Rotations = new List<EvaluatedRotationData>();
        [NonSerialized]
        public Dictionary<string, float> RankingStability = new Dictionary<string, float>();

        public EvaluatedRotationData GetRotationById(string id)
        {
            return Rotations.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public EvaluatedRotationData GetTopRankedRotation()
        {
            return Rotations.OrderBy(r => r.Rank).FirstOrDefault();
        }
    }

    /// <summary>
    /// Pluggable calculation/modifier contract. Implement this later to add continuous slider
    /// interpolation or custom delta adjustments without changing DataManager or UI code.
    /// </summary>
    public interface IScenarioValueModifier
    {
        EvaluatedScenarioState Modify(EvaluatedScenarioState baseState, ResultsRoot rawData, ClimateInputs inputs);
    }

    /// <summary>
    /// Default pass-through modifier: returns the exact values loaded from results.json without altering them.
    /// </summary>
    public sealed class IdentityScenarioModifier : IScenarioValueModifier
    {
        public EvaluatedScenarioState Modify(EvaluatedScenarioState baseState, ResultsRoot rawData, ClimateInputs inputs)
        {
            return baseState;
        }
    }

    /// <summary>
    /// Calculation layer sitting between DataManager (raw JSON) and UI/Gameplay consumers.
    /// Evaluates precomputed scenario_variants grids from future-farm results.json cleanly.
    /// </summary>
    public class ScenarioCalculationLayer : MonoBehaviour
    {
        public static ScenarioCalculationLayer Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private DataManager dataManager;

        [Header("Initial Climate Inputs")]
        [SerializeField] private float initialRainfallDeltaPct = 0f;
        [SerializeField] private float initialTemperatureDeltaC = 0f;

        public ClimateInputs CurrentInputs { get; private set; }
        public EvaluatedScenarioState CurrentState { get; private set; }

        public event Action<EvaluatedScenarioState> OnScenarioEvaluated;

        private IScenarioValueModifier _modifier = new IdentityScenarioModifier();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentInputs = new ClimateInputs(initialRainfallDeltaPct, initialTemperatureDeltaC);

            if (dataManager == null)
            {
                dataManager = DataManager.Instance != null ? DataManager.Instance : FindAnyObjectByType<DataManager>();
            }
        }

        private void OnEnable()
        {
            if (dataManager == null)
            {
                dataManager = DataManager.Instance != null ? DataManager.Instance : FindAnyObjectByType<DataManager>();
            }

            if (dataManager != null)
            {
                dataManager.OnDataLoaded += HandleDataLoaded;
            }
        }

        private void OnDisable()
        {
            if (dataManager != null)
            {
                dataManager.OnDataLoaded -= HandleDataLoaded;
            }
        }

        private void Start()
        {
            if (dataManager != null && dataManager.IsLoaded && CurrentState == null)
            {
                SyncFromLoadedData(dataManager.RawResults);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetModifier(IScenarioValueModifier modifier)
        {
            _modifier = modifier ?? new IdentityScenarioModifier();
            if (dataManager != null && dataManager.IsLoaded)
            {
                Evaluate();
            }
        }

        public void SetClimateInputs(float rainfallDeltaPct, float temperatureDeltaC)
        {
            CurrentInputs = new ClimateInputs(rainfallDeltaPct, temperatureDeltaC);
            Evaluate();
        }

        public void SetRainfallDelta(float rainfallDeltaPct)
        {
            SetClimateInputs(rainfallDeltaPct, CurrentInputs.TemperatureDeltaC);
        }

        public void SetTemperatureDelta(float temperatureDeltaC)
        {
            SetClimateInputs(CurrentInputs.RainfallDeltaPct, temperatureDeltaC);
        }

        public EvaluatedScenarioState Evaluate()
        {
            if (dataManager == null || !dataManager.IsLoaded || dataManager.RawResults == null)
            {
                return null;
            }

            EvaluatedScenarioState baseline = BuildBaselineState(dataManager.RawResults, CurrentInputs);
            CurrentState = _modifier.Modify(baseline, dataManager.RawResults, CurrentInputs);
            RecalculateRanks(CurrentState);

            OnScenarioEvaluated?.Invoke(CurrentState);
            return CurrentState;
        }

        private EvaluatedScenarioState BuildBaselineState(ResultsRoot raw, ClimateInputs inputs)
        {
            ScenarioVariant variant = dataManager.FindClosestScenarioVariant(inputs.RainfallDeltaPct, inputs.TemperatureDeltaC);
            bool usingFallback = variant == null;

            float matchedRain = variant != null ? variant.EffectiveRainfallDeltaPct : inputs.RainfallDeltaPct;
            float matchedTemp = variant != null ? variant.EffectiveTemperatureDeltaC : inputs.TemperatureDeltaC;

            bool exactMatch = variant != null
                && Mathf.Abs(matchedRain - inputs.RainfallDeltaPct) < 0.01f
                && Mathf.Abs(matchedTemp - inputs.TemperatureDeltaC) < 0.01f;

            FarmerPriorities priorities = (raw.FarmerPriorities ?? new FarmerPriorities()).Clone();

            var state = new EvaluatedScenarioState
            {
                Inputs = inputs,
                MatchedVariantInputs = new ClimateInputs(matchedRain, matchedTemp),
                IsExactScenarioMatch = exactMatch,
                ScenarioId = variant != null && !string.IsNullOrWhiteSpace(variant.Id)
                    ? variant.Id
                    : $"rain_{matchedRain:0}_temp_{matchedTemp:0.#}",
                ScenarioLabel = variant != null && !string.IsNullOrWhiteSpace(variant.Label)
                    ? variant.Label
                    : BuildScenarioLabel(matchedRain, matchedTemp),
                IsUsingScenarioFallback = usingFallback,
                WaterSupplyAssumptionMm = raw.WaterSupplyAssumptionMm,
                Et0Source = raw.Et0Source,
                ScenarioNote = raw.ScenarioNote,
                FarmerPriorities = priorities,
                RankingStability = variant?.RankingStability != null && variant.RankingStability.Count > 0
                    ? new Dictionary<string, float>(variant.RankingStability)
                    : new Dictionary<string, float>(raw.RankingStability ?? new Dictionary<string, float>())
            };

            foreach (RotationData baseRot in raw.Rotations)
            {
                ScenarioRotationOverride rotOverride = variant?.Rotations?
                    .FirstOrDefault(r => string.Equals(r.Id, baseRot.Id, StringComparison.OrdinalIgnoreCase));

                ScenarioRotationScore gridScore = null;
                variant?.RotationScores?.TryGetValue(baseRot.Id, out gridScore);

                CriteriaScores scores = (rotOverride?.Scores ?? baseRot.Scores ?? new CriteriaScores()).Clone();
                CriteriaContributions contrib = (rotOverride?.Contributions ?? baseRot.Contributions ?? new CriteriaContributions()).Clone();
                RotationDetails details = (rotOverride?.Details ?? baseRot.Details ?? new RotationDetails()).Clone();

                if (gridScore != null)
                {
                    scores.Water = gridScore.Water;
                    scores.Climate = gridScore.Climate;
                    scores.Overall = gridScore.Overall;

                    float totalW = priorities.Water + priorities.Climate + priorities.SoilHealth + priorities.Resilience;
                    if (totalW > 0.0001f)
                    {
                        contrib.Water = (float)Math.Round(scores.Water * (priorities.Water / totalW), 2);
                        contrib.Climate = (float)Math.Round(scores.Climate * (priorities.Climate / totalW), 2);
                        contrib.SoilHealth = (float)Math.Round(scores.SoilHealth * (priorities.SoilHealth / totalW), 2);
                    }
                }

                List<string> explanations = rotOverride?.Explanations != null && rotOverride.Explanations.Count > 0
                    ? new List<string>(rotOverride.Explanations)
                    : new List<string>(baseRot.Explanations ?? new List<string>());

                state.RankingStability.TryGetValue(baseRot.Id, out float stabilityShare);

                List<CropSeasonEntry> resolvedSeasons = baseRot.Seasons != null && baseRot.Seasons.Count > 0
                    ? baseRot.Seasons.Select(s => s?.Clone()).Where(s => s != null).ToList()
                    : ResultsSchemaHelpers.BuildSeasonsFromTimeline(baseRot.FieldTimeline);

                List<FieldTimelineEntry> clonedTimeline = baseRot.FieldTimeline != null
                    ? baseRot.FieldTimeline.Select(t => t?.Clone()).Where(t => t != null).ToList()
                    : new List<FieldTimelineEntry>();

                ResultsSchemaHelpers.ParseRotationDisplayName(baseRot.Name, baseRot.Id, out string shortTitle, out string parsedSubtitle);
                string resolvedSubtitle = !string.IsNullOrWhiteSpace(baseRot.Subtitle) ? baseRot.Subtitle : parsedSubtitle;

                List<string> rawCrops = new List<string>(baseRot.Crops ?? new List<string>());
                List<string> formattedCrops = rawCrops
                    .Select(ResultsSchemaHelpers.FormatCropDisplayName)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();

                state.Rotations.Add(new EvaluatedRotationData
                {
                    Id = baseRot.Id,
                    Name = shortTitle,
                    FullName = !string.IsNullOrWhiteSpace(baseRot.Name) ? baseRot.Name : shortTitle,
                    Subtitle = resolvedSubtitle,
                    Crops = formattedCrops,
                    RawCrops = rawCrops,
                    RotationLengthYears = baseRot.GetEffectiveCycleYears(),
                    Seasons = resolvedSeasons,
                    FieldTimeline = clonedTimeline,
                    Scores = scores,
                    Contributions = contrib,
                    StressTest = baseRot.StressTest?.Clone(),
                    Details = details,
                    HeatDegreeDaysPerYear = baseRot.HeatDegreeDaysPerYear,
                    FeasibilityWarnings = new List<string>(baseRot.FeasibilityWarnings ?? new List<string>()),
                    Explanations = explanations,
                    Uncertainty = (baseRot.Uncertainty ?? new UncertaintyInfo()).Clone(),
                    Rank = baseRot.Rank,
                    TiedWith = new List<string>(baseRot.TiedWith ?? new List<string>()),
                    RankingStabilityShare = stabilityShare
                });
            }

            return state;
        }

        private static string BuildScenarioLabel(float rainPct, float tempC)
        {
            if (Mathf.Abs(rainPct) < 0.05f && Mathf.Abs(tempC) < 0.05f)
            {
                return "Historical Baseline (0% Rain, +0°C)";
            }

            string prefix = rainPct < -0.5f && tempC >= 2.5f
                ? "Severe Hot & Dry"
                : rainPct < -0.5f && tempC > 0.5f
                    ? "Dry & Warmer"
                    : rainPct < -0.5f
                        ? "Dry Baseline"
                        : rainPct > 0.5f && tempC > 0.5f
                            ? "Wetter & Warmer"
                            : rainPct > 0.5f
                                ? "Wetter Baseline"
                                : tempC >= 2.5f
                                    ? "High Warming"
                                    : "Moderate Warming";

            string rainText = rainPct > 0f ? $"+{rainPct:0}%" : $"{rainPct:0}%";
            return $"{prefix} ({rainText} Rain, +{tempC:0.#}°C)";
        }

        private static void RecalculateRanks(EvaluatedScenarioState state)
        {
            if (state?.Rotations == null || state.Rotations.Count == 0) return;

            List<EvaluatedRotationData> ordered = state.Rotations
                .OrderByDescending(r => r.Scores != null ? r.Scores.Overall : 0f)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Rank = i + 1;
            }
        }

        /// <summary>
        /// Helper for future continuous IScenarioValueModifier implementations: recomputes weighted
        /// contributions and overall score from sub-scores and normalized farmer priorities.
        /// </summary>
        public static void RecalculateOverallFromPriorities(EvaluatedRotationData rotation, FarmerPriorities priorities)
        {
            if (rotation?.Scores == null || priorities == null) return;

            float totalWeight = priorities.Water + priorities.Climate + priorities.SoilHealth + priorities.Resilience;
            if (totalWeight <= 0.0001f) return;

            float wWater = priorities.Water / totalWeight;
            float wClimate = priorities.Climate / totalWeight;
            float wSoil = priorities.SoilHealth / totalWeight;
            float wResilience = priorities.Resilience / totalWeight;

            rotation.Contributions ??= new CriteriaContributions();
            rotation.Contributions.Water = (float)Math.Round(wWater * rotation.Scores.Water, 2);
            rotation.Contributions.Climate = (float)Math.Round(wClimate * rotation.Scores.Climate, 2);
            rotation.Contributions.SoilHealth = (float)Math.Round(wSoil * rotation.Scores.SoilHealth, 2);
            rotation.Contributions.Resilience = (float)Math.Round(wResilience * rotation.Scores.Resilience, 2);
            rotation.Scores.Overall = (float)Math.Round(
                rotation.Contributions.Water +
                rotation.Contributions.Climate +
                rotation.Contributions.SoilHealth +
                rotation.Contributions.Resilience, 1);
        }

        private void HandleDataLoaded(ResultsRoot raw)
        {
            SyncFromLoadedData(raw);
        }

        private void SyncFromLoadedData(ResultsRoot raw)
        {
            if (TimeManager.Instance != null && raw?.Rotations != null && raw.Rotations.Count > 0)
            {
                int maxYears = raw.Rotations.Max(r => r.GetEffectiveCycleYears());
                TimeManager.Instance.SetTotalYears(maxYears);
            }

            Evaluate();
        }
    }
}
