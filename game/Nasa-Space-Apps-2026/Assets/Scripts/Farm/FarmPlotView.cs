using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FieldShift.Core;
using FieldShift.Data;

namespace FieldShift.Farm
{
    /// <summary>
    /// Controls a single side-by-side isometric farm plot and its attached uGUI header card.
    /// Reads rotation scores/details from ScenarioCalculationLayer and calendar state from TimeManager.
    /// </summary>
    public class FarmPlotView : MonoBehaviour
    {
        [Header("Rotation Binding")]
        [SerializeField] private string rotationId = "current";

        [Header("Isometric World Visuals")]
        [SerializeField] private List<SpriteRenderer> cropRenderers = new List<SpriteRenderer>();
        [SerializeField] private List<SpriteRenderer> soilRenderers = new List<SpriteRenderer>();
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private float cropBaseScale = 1.0f;

        [Header("Plot Header Card (uGUI)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text rankBadgeText;
        [SerializeField] private TMP_Text scoreBadgeText;
        [SerializeField] private TMP_Text activeCropText;
        [SerializeField] private UnityEngine.UI.Image cropPillBackground;
        [SerializeField] private UnityEngine.UI.Image cardAccentBar;
        [SerializeField] private UnityEngine.UI.Button inspectButton;

        [Header("References")]
        [SerializeField] private CropVisualLibrary visualLibrary;

        public string RotationId => rotationId;
        public IReadOnlyList<SpriteRenderer> CropRenderers => cropRenderers;
        private float _climateDryFactor;
        private float _climateWetFactor;
        private float _climateHeatFactor;
        private float _rotationCropStress;

        public EvaluatedRotationData CurrentRotation { get; private set; }
        public string ActiveCropName { get; private set; }
        public CropGrowthStage CurrentStage { get; private set; }

        public event Action<FarmPlotView> OnInspectRequested;

        private void Awake()
        {
            if (visualLibrary == null)
            {
                visualLibrary = CropVisualLibrary.Instance != null
                    ? CropVisualLibrary.Instance
                    : FindAnyObjectByType<CropVisualLibrary>();
            }

            if (inspectButton != null)
            {
                inspectButton.onClick.AddListener(HandleInspectClicked);
            }

            SetSelected(false);
        }

        private void OnEnable()
        {
            if (ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated += HandleScenarioEvaluated;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated += HandleTimeUpdated;
            }
        }

        private void OnDisable()
        {
            if (ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated -= HandleScenarioEvaluated;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
            }
        }

        private void Start()
        {
            // Retry subscription in case singletons initialized in a different Awake order.
            if (ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated -= HandleScenarioEvaluated;
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated += HandleScenarioEvaluated;

                if (ScenarioCalculationLayer.Instance.CurrentState != null)
                {
                    HandleScenarioEvaluated(ScenarioCalculationLayer.Instance.CurrentState);
                }
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                TimeManager.Instance.OnTimeUpdated += HandleTimeUpdated;
            }

            RefreshSeasonalVisuals();
        }

        private void OnDestroy()
        {
            if (inspectButton != null)
            {
                inspectButton.onClick.RemoveListener(HandleInspectClicked);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(selected);
            }
        }

        public void RequestInspect()
        {
            OnInspectRequested?.Invoke(this);
        }

        private void HandleInspectClicked()
        {
            RequestInspect();
        }

        private void HandleScenarioEvaluated(EvaluatedScenarioState state)
        {
            if (state == null) return;

            CurrentRotation = state.GetRotationById(rotationId);
            if (CurrentRotation == null) return;

            UpdateClimateStressFactors(state, CurrentRotation);
            RefreshHeaderCard();
            RefreshSeasonalVisuals();
        }

        private void UpdateClimateStressFactors(EvaluatedScenarioState state, EvaluatedRotationData rot)
        {
            float rainDelta = state.Inputs.RainfallDeltaPct;
            float tempDelta = state.Inputs.TemperatureDeltaC;

            _climateDryFactor = rainDelta < -0.5f ? Mathf.Clamp01(-rainDelta / 20f) : 0f;
            _climateWetFactor = rainDelta > 0.5f ? Mathf.Clamp01(rainDelta / 20f) : 0f;
            _climateHeatFactor = tempDelta > 0.05f ? Mathf.Clamp01(tempDelta / 3f) : 0f;

            if (_climateDryFactor <= 0.01f && _climateHeatFactor <= 0.01f)
            {
                _rotationCropStress = 0f;
                return;
            }

            // Combine external climate forcing with how vulnerable this rotation's scenario scores are.
            float waterScore = rot.Scores != null ? rot.Scores.Water : 50f;
            float climateScore = rot.Scores != null ? rot.Scores.Climate : 75f;
            float waterVulnerability = Mathf.Clamp01((52f - waterScore) / 36f);
            float heatVulnerability = Mathf.Clamp01((90f - climateScore) / 32f);

            float combined = (_climateDryFactor * 0.40f)
                + (_climateHeatFactor * 0.35f)
                + (_climateDryFactor * waterVulnerability * 0.25f)
                + (_climateHeatFactor * heatVulnerability * 0.20f);
            _rotationCropStress = Mathf.Clamp01(combined);
        }

        private void HandleTimeUpdated()
        {
            RefreshSeasonalVisuals();
        }

        private void RefreshHeaderCard()
        {
            if (CurrentRotation == null) return;

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(CurrentRotation.Name) ? rotationId : CurrentRotation.Name;
            }

            if (subtitleText != null)
            {
                string sub = !string.IsNullOrWhiteSpace(CurrentRotation.Subtitle)
                    ? CurrentRotation.Subtitle
                    : string.Join(" • ", CurrentRotation.Crops ?? new List<string>());
                subtitleText.text = sub;
            }

            if (rankBadgeText != null)
            {
                rankBadgeText.text = CurrentRotation.Rank > 0 ? $"#{CurrentRotation.Rank}" : "-";
            }

            if (scoreBadgeText != null && CurrentRotation.Scores != null)
            {
                scoreBadgeText.text = $"{CurrentRotation.Scores.Overall:0.0}";
            }

            if (cardAccentBar != null)
            {
                cardAccentBar.color = CurrentRotation.Rank == 1
                    ? new Color(0.18f, 0.62f, 0.36f, 1f)
                    : CurrentRotation.Rank == 2
                        ? new Color(0.24f, 0.50f, 0.72f, 1f)
                        : new Color(0.55f, 0.52f, 0.44f, 1f);
            }
        }

        private void RefreshSeasonalVisuals()
        {
            if (CurrentRotation == null) return;

            int year = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 1;
            int month = TimeManager.Instance != null ? TimeManager.Instance.CurrentMonth : 6;
            float monthProgress = TimeManager.Instance != null ? TimeManager.Instance.MonthProgress : 0.5f;

            ResolveActiveCropAndStage(year, month, monthProgress, out string cropName, out CropGrowthStage stage);
            ActiveCropName = ResultsSchemaHelpers.FormatCropDisplayName(cropName);
            CurrentStage = stage;

            CropVisualStyle style = visualLibrary != null ? visualLibrary.GetStyleForCrop(cropName) : null;
            Sprite stageSprite = visualLibrary != null ? visualLibrary.GetSpriteForStage(style, stage) : null;

            string displayCrop = !string.IsNullOrEmpty(ActiveCropName)
                ? ActiveCropName
                : (style != null && !string.IsNullOrWhiteSpace(style.displayName) ? style.displayName : cropName);

            if (activeCropText != null)
            {
                activeCropText.text = stage == CropGrowthStage.Fallow
                    ? "Fallow / Soil Rest"
                    : $"{displayCrop} ({stage})";
            }

            if (cropPillBackground != null)
            {
                cropPillBackground.color = GetStageBadgeColor(stage);
            }

            ApplyCropRenderers(stage, stageSprite, style);
            ApplySoilTint(stage);
        }

        private static Color GetStageBadgeColor(CropGrowthStage stage)
        {
            return stage switch
            {
                CropGrowthStage.Planting => new Color(0.24f, 0.64f, 0.44f, 1f),
                CropGrowthStage.Growing => new Color(0.18f, 0.56f, 0.30f, 1f),
                CropGrowthStage.Flowering => new Color(0.35f, 0.44f, 0.78f, 1f),
                CropGrowthStage.Harvest => new Color(0.80f, 0.55f, 0.14f, 1f),
                _ => new Color(0.45f, 0.42f, 0.38f, 1f)
            };
        }

        private void ResolveActiveCropAndStage(
            int year,
            int month,
            float monthProgress,
            out string cropName,
            out CropGrowthStage stage)
        {
            cropName = CurrentRotation.Crops != null && CurrentRotation.Crops.Count > 0
                ? CurrentRotation.Crops[0]
                : "Crop";
            stage = CropGrowthStage.Growing;

            if (CurrentRotation.Seasons == null || CurrentRotation.Seasons.Count == 0)
            {
                if (CurrentRotation.Crops != null && CurrentRotation.Crops.Count > 0)
                {
                    int idx = (year - 1) % CurrentRotation.Crops.Count;
                    cropName = CurrentRotation.Crops[idx];
                }
                return;
            }

            int rotYear = ((year - 1) % Mathf.Max(1, CurrentRotation.RotationLengthYears)) + 1;
            CropSeasonEntry activeEntry = null;

            foreach (CropSeasonEntry entry in CurrentRotation.Seasons)
            {
                if (entry.Year == rotYear && IsMonthInRange(month, entry.PlantMonth, entry.HarvestMonth))
                {
                    activeEntry = entry;
                    break;
                }
            }

            if (activeEntry == null)
            {
                stage = CropGrowthStage.Fallow;
                CropSeasonEntry yearFallback = CurrentRotation.Seasons.Find(s => s.Year == rotYear);
                if (yearFallback != null)
                {
                    cropName = yearFallback.Crop;
                }
                return;
            }

            cropName = activeEntry.Crop;
            float t = ComputeNormalizedSeasonProgress(month, monthProgress, activeEntry.PlantMonth, activeEntry.HarvestMonth);

            // Detect overwintering crops (autumn sowing Oct-Dec that resume the next cycle year Jan-Mar)
            // so autumn doesn't rush into a fake Harvest and spring resumes from vegetative growth.
            int totalCycleYears = Mathf.Max(1, CurrentRotation.RotationLengthYears);
            int nextRotYear = (rotYear % totalCycleYears) + 1;
            int prevRotYear = ((rotYear - 2 + totalCycleYears) % totalCycleYears) + 1;
            string activeId = !string.IsNullOrEmpty(activeEntry.RawCropId) ? activeEntry.RawCropId : activeEntry.Crop;

            bool continuesNextSpring = activeEntry.PlantMonth >= 9 && activeEntry.HarvestMonth >= 10
                && CurrentRotation.Seasons.Exists(s =>
                    s != null && s.Year == nextRotYear && s.PlantMonth <= 4 &&
                    string.Equals(!string.IsNullOrEmpty(s.RawCropId) ? s.RawCropId : s.Crop, activeId, StringComparison.OrdinalIgnoreCase));

            bool continuedFromLastAutumn = activeEntry.PlantMonth <= 4
                && CurrentRotation.Seasons.Exists(s =>
                    s != null && s.Year == prevRotYear && s.PlantMonth >= 9 && s.HarvestMonth >= 10 &&
                    string.Equals(!string.IsNullOrEmpty(s.RawCropId) ? s.RawCropId : s.Crop, activeId, StringComparison.OrdinalIgnoreCase));

            if (continuesNextSpring)
            {
                // Autumn leg: Planting -> vegetative Growing (0.0 .. 0.42)
                t *= 0.42f;
            }
            else if (continuedFromLastAutumn)
            {
                // Spring resumption leg: Growing -> Flowering -> Harvest (0.35 .. 1.0)
                t = Mathf.Lerp(0.35f, 1.0f, t);
            }

            if (t < 0.22f) stage = CropGrowthStage.Planting;
            else if (t < 0.58f) stage = CropGrowthStage.Growing;
            else if (t < 0.85f) stage = CropGrowthStage.Flowering;
            else stage = CropGrowthStage.Harvest;
        }

        private static bool IsMonthInRange(int month, int startMonth, int endMonth)
        {
            if (startMonth <= endMonth)
            {
                return month >= startMonth && month <= endMonth;
            }
            return month >= startMonth || month <= endMonth;
        }

        private static float ComputeNormalizedSeasonProgress(int month, float monthProgress, int startMonth, int endMonth)
        {
            int span = startMonth <= endMonth
                ? (endMonth - startMonth + 1)
                : ((12 - startMonth + 1) + endMonth);
            if (span <= 1) return monthProgress;

            int offset = startMonth <= endMonth
                ? (month - startMonth)
                : (month >= startMonth ? month - startMonth : (12 - startMonth) + month);

            return Mathf.Clamp01((offset + monthProgress) / span);
        }

        private void ApplyCropRenderers(CropGrowthStage stage, Sprite stageSprite, CropVisualStyle style)
        {
            if (cropRenderers == null) return;

            float baseScale = (style != null ? style.scaleMultiplier : 1f) * Mathf.Max(0.1f, cropBaseScale);
            float stageScale = stage switch
            {
                CropGrowthStage.Planting => 0.85f,
                CropGrowthStage.Growing => 0.94f,
                CropGrowthStage.Flowering => 1.04f,
                CropGrowthStage.Harvest => 1.00f,
                _ => 0.0f
            };

            Color tint = style != null ? style.tintColor : Color.white;
            if (stage == CropGrowthStage.Harvest && style != null)
            {
                tint = style.harvestTint;
            }

            if (_rotationCropStress > 0.01f)
            {
                // Parched olive-straw tint proportional to drought/heat stress & low scenario suitability.
                Color parchedTint = new Color(0.95f, 0.80f, 0.44f, 1f);
                tint = Color.Lerp(tint, parchedTint, _rotationCropStress * 0.55f);
            }
            else if (_climateWetFactor > 0.01f)
            {
                Color lushTint = new Color(0.88f, 1.00f, 0.90f, 1f);
                tint = Color.Lerp(tint, lushTint, _climateWetFactor * 0.25f);
            }

            float stressScaleX = 1f - (0.05f * _rotationCropStress) + (0.03f * _climateWetFactor);
            float stressScaleY = 1f - (0.14f * _rotationCropStress) + (0.04f * _climateWetFactor);

            foreach (SpriteRenderer sr in cropRenderers)
            {
                if (sr == null) continue;

                bool show = stage != CropGrowthStage.Fallow && stageSprite != null;
                sr.enabled = show;
                if (!show) continue;

                sr.sprite = stageSprite;
                sr.color = tint;
                float s = baseScale * stageScale;
                sr.transform.localScale = new Vector3(s * stressScaleX, s * stressScaleY, 1f);
            }
        }

        private void ApplySoilTint(CropGrowthStage stage)
        {
            if (soilRenderers == null) return;

            Color soilColor = stage == CropGrowthStage.Fallow
                ? new Color(0.84f, 0.78f, 0.72f, 1f)
                : Color.white;

            if (_climateWetFactor > 0.01f && _climateHeatFactor < 0.75f)
            {
                Color moistSoil = new Color(0.70f, 0.76f, 0.70f, 1f);
                soilColor = Color.Lerp(soilColor, moistSoil, _climateWetFactor * (0.34f - 0.14f * _climateHeatFactor));
            }
            else if (_climateDryFactor > 0.01f || _climateHeatFactor > 0.01f)
            {
                Color bakedSoil = new Color(1.00f, 0.89f, 0.68f, 1f);
                float dryAmt = Mathf.Clamp01((_climateDryFactor * 0.40f) + (_climateHeatFactor * 0.28f));
                soilColor = Color.Lerp(soilColor, bakedSoil, dryAmt);
            }

            foreach (SpriteRenderer sr in soilRenderers)
            {
                if (sr != null)
                {
                    sr.color = soilColor;
                }
            }
        }
    }
}
