using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FieldShift.Core;

namespace FieldShift.UI
{
    [Serializable]
    public class CriteriaBarRow
    {
        public RectTransform fillRect;
        public UnityEngine.UI.Image fillImage;
        public TMP_Text valueText;
    }

    /// <summary>
    /// Displays side-by-side comparison metrics for a single rotation:
    /// rank badge, overall score, 4 criteria horizontal bars, and irrigation / heat-stress / soil indicators.
    /// </summary>
    public class ResultsComparisonCard : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private string rotationId = "current";

        [Header("Header")]
        [SerializeField] private UnityEngine.UI.Image accentBar;
        [SerializeField] private UnityEngine.UI.Image rankBadgeBackground;
        [SerializeField] private TMP_Text rankBadgeText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text overallScoreText;
        [SerializeField] private TMP_Text deltaText;

        [Header("4 Criteria Bars")]
        [SerializeField] private CriteriaBarRow waterBar = new CriteriaBarRow();
        [SerializeField] private CriteriaBarRow climateBar = new CriteriaBarRow();
        [SerializeField] private CriteriaBarRow soilHealthBar = new CriteriaBarRow();
        [SerializeField] private CriteriaBarRow resilienceBar = new CriteriaBarRow();

        [Header("Field Indicators")]
        [SerializeField] private TMP_Text irrigationText;
        [SerializeField] private TMP_Text heatStressText;
        [SerializeField] private TMP_Text soilHealthText;

        [Header("Interaction")]
        [SerializeField] private UnityEngine.UI.Button cardButton;

        public string RotationId => rotationId;
        public EvaluatedRotationData BoundData { get; private set; }
        public event Action<ResultsComparisonCard> OnCardClicked;

        private static readonly Color Rank1Color = new Color(0.16f, 0.58f, 0.35f, 1f);
        private static readonly Color Rank2Color = new Color(0.22f, 0.48f, 0.72f, 1f);
        private static readonly Color Rank3Color = new Color(0.46f, 0.44f, 0.39f, 1f);
        private static readonly Color PositiveDeltaColor = new Color(0.14f, 0.54f, 0.30f, 1f);
        private static readonly Color NegativeDeltaColor = new Color(0.74f, 0.28f, 0.22f, 1f);

        private void Awake()
        {
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            OnCardClicked?.Invoke(this);
        }

        public void Bind(EvaluatedRotationData data, float baselineOverall)
        {
            BoundData = data;
            if (data == null) return;

            int rank = data.Rank;
            Color rankColor = rank == 1 ? Rank1Color : (rank == 2 ? Rank2Color : Rank3Color);

            if (accentBar != null)
            {
                accentBar.color = rankColor;
            }

            if (rankBadgeBackground != null)
            {
                rankBadgeBackground.color = rankColor;
            }

            if (rankBadgeText != null)
            {
                rankBadgeText.text = rank == 1 ? "#1 BEST" : $"#{rank}";
            }

            if (titleText != null)
            {
                titleText.text = !string.IsNullOrWhiteSpace(data.Name) ? data.Name : data.Id;
            }

            float overall = data.Scores != null ? data.Scores.Overall : 0f;
            if (overallScoreText != null)
            {
                overallScoreText.text = $"{overall:0.0}<size=15><color=#48594F> /100</color></size>";
            }

            if (deltaText != null)
            {
                bool isBaseline = string.Equals(data.Id, "current", StringComparison.OrdinalIgnoreCase);
                if (isBaseline)
                {
                    deltaText.text = "Baseline";
                    deltaText.color = new Color(0.28f, 0.36f, 0.32f, 1f);
                }
                else
                {
                    float diff = overall - baselineOverall;
                    string sign = diff >= 0f ? "+" : "";
                    deltaText.text = $"{sign}{diff:0.0} vs base";
                    deltaText.color = diff >= 0f ? PositiveDeltaColor : NegativeDeltaColor;
                }
            }

            if (data.Scores != null)
            {
                ApplyBar(waterBar, data.Scores.Water);
                ApplyBar(climateBar, data.Scores.Climate);
                ApplyBar(soilHealthBar, data.Scores.SoilHealth);
                float fourthValue = data.StressTest != null ? data.StressTest.RetentionPct : data.Scores.Resilience;
                string fourthSuffix = data.StressTest != null ? "%" : string.Empty;
                ApplyBar(resilienceBar, fourthValue, fourthSuffix);
            }

            if (data.Details != null)
            {
                if (irrigationText != null)
                {
                    float irrig = data.Details.EffectiveIrrigationMm;
                    if (data.Details.YearlyRainMm > 0f)
                    {
                        irrigationText.text = $"Irrig: <b>{irrig:0} mm/yr</b> (Rain: {data.Details.YearlyRainMm:0} mm)";
                    }
                    else
                    {
                        string lvl = string.IsNullOrWhiteSpace(data.Details.IrrigationLevel)
                            ? string.Empty
                            : $" ({data.Details.IrrigationLevel})";
                        irrigationText.text = $"Irrig: <b>{irrig:0} mm</b>{lvl}";
                    }
                }

                if (heatStressText != null)
                {
                    heatStressText.text = $"Heat: <b>{data.Details.EffectiveHeatDays:0.#} d/yr</b>";
                }

                if (soilHealthText != null)
                {
                    if (!string.IsNullOrWhiteSpace(data.Details.SoilHealthIndicator))
                    {
                        string soil = !string.IsNullOrWhiteSpace(data.Details.SoilOrganicMatterTrend)
                            ? $"{data.Details.SoilHealthIndicator} • {data.Details.SoilOrganicMatterTrend}"
                            : data.Details.SoilHealthIndicator;
                        soilHealthText.text = $"Soil: <b>{soil}</b>";
                    }
                    else if (data.StressTest != null)
                    {
                        soilHealthText.text = $"Bare: <b>{data.Details.BareMonthsPerYear:0.0} mo/yr</b>  •  Stress: <b>{data.StressTest.OverallUnderStress:0.0}</b> ({data.StressTest.RetentionPct:0.#}%)";
                    }
                    else
                    {
                        soilHealthText.text = $"Bare Soil: <b>{data.Details.BareMonthsPerYear:0.0} mo/yr</b>";
                    }
                }
            }
        }

        private static void ApplyBar(CriteriaBarRow row, float score, string suffix = "")
        {
            if (row == null) return;

            float clamped = Mathf.Clamp(score, 0f, 100f);
            if (row.fillRect != null)
            {
                row.fillRect.anchorMin = Vector2.zero;
                row.fillRect.anchorMax = new Vector2(clamped / 100f, 1f);
                row.fillRect.offsetMin = Vector2.zero;
                row.fillRect.offsetMax = Vector2.zero;
            }

            if (row.valueText != null)
            {
                row.valueText.text = string.IsNullOrEmpty(suffix)
                    ? $"{clamped:0.#}"
                    : $"{clamped:0.#}{suffix}";
            }
        }
    }
}
