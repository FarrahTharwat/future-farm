using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using FieldShift.Core;
using FieldShift.Farm;

namespace FieldShift.UI
{
    /// <summary>
    /// Opens when the user clicks "Inspect" on one of the 3 side-by-side farm plots.
    /// Shows crops, rotation length, current scenario scores, irrigation/heat/soil details, and uncertainty.
    /// </summary>
    public class PlotInspectModalController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button backdropButton;

        [Header("Plots")]
        [SerializeField] private List<FarmPlotView> plotViews = new List<FarmPlotView>();

        private FarmPlotView _inspectedPlot;

        private void Awake()
        {
            if (plotViews == null || plotViews.Count == 0)
            {
                plotViews = new List<FarmPlotView>(FindObjectsByType<FarmPlotView>());
            }

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);

            if (modalRoot != null) modalRoot.SetActive(false);
        }

        private void OnEnable()
        {
            foreach (FarmPlotView plot in plotViews)
            {
                if (plot != null)
                {
                    plot.OnInspectRequested += HandleInspectRequested;
                }
            }

            if (ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated += HandleScenarioEvaluated;
            }
        }

        private void OnDisable()
        {
            foreach (FarmPlotView plot in plotViews)
            {
                if (plot != null)
                {
                    plot.OnInspectRequested -= HandleInspectRequested;
                }
            }

            if (ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated -= HandleScenarioEvaluated;
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (backdropButton != null) backdropButton.onClick.RemoveListener(Close);
        }

        public void Close()
        {
            if (_inspectedPlot != null)
            {
                _inspectedPlot.SetSelected(false);
                _inspectedPlot = null;
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        public void Show(FarmPlotView plot)
        {
            HandleInspectRequested(plot);
        }

        private void HandleInspectRequested(FarmPlotView plot)
        {
            if (plot == null) return;

            if (_inspectedPlot != null && _inspectedPlot != plot)
            {
                _inspectedPlot.SetSelected(false);
            }

            _inspectedPlot = plot;
            _inspectedPlot.SetSelected(true);
            RefreshContent();

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }
        }

        private void HandleScenarioEvaluated(EvaluatedScenarioState state)
        {
            if (modalRoot != null && modalRoot.activeSelf && _inspectedPlot != null)
            {
                RefreshContent();
            }
        }

        private void RefreshContent()
        {
            if (_inspectedPlot == null || _inspectedPlot.CurrentRotation == null) return;

            EvaluatedRotationData rot = _inspectedPlot.CurrentRotation;

            if (titleText != null)
            {
                titleText.text = $"{rot.Name}  (Rank #{rot.Rank} • {rot.Scores?.Overall ?? 0f:0.0}/100)";
            }

            if (subtitleText != null)
            {
                string crops = rot.Crops != null && rot.Crops.Count > 0 ? string.Join(", ", rot.Crops) : "N/A";
                subtitleText.text = $"Crops: {crops}   •   Rotation Length: {rot.RotationLengthYears} Year(s)";
            }

            if (summaryText != null)
            {
                var sb = new StringBuilder();

                if (rot.Scores != null)
                {
                    sb.AppendLine("<b>Scores (0-100)</b>");
                    if (rot.Scores.Resilience > 0.01f)
                    {
                        sb.AppendLine($"  Water: {rot.Scores.Water:0.#}   |   Climate: {rot.Scores.Climate:0.#}   |   Soil Health: {rot.Scores.SoilHealth:0.#}   |   Resilience: {rot.Scores.Resilience:0.#}");
                    }
                    else
                    {
                        sb.AppendLine($"  Water (40%): {rot.Scores.Water:0.0}   |   Climate (25%): {rot.Scores.Climate:0.0}   |   Soil Health (35%): {rot.Scores.SoilHealth:0.0}");
                    }
                    sb.AppendLine();
                }

                if (rot.StressTest != null)
                {
                    string scenText = rot.StressTest.Scenario != null
                        ? $"{rot.StressTest.Scenario.RainPct:+0;-0;0}% rain, +{rot.StressTest.Scenario.TempC:0.#}C"
                        : "-20% rain, +1.5C";
                    sb.AppendLine($"<b>Stress Test ({scenText})</b>");
                    sb.AppendLine($"  • Score under stress: {rot.StressTest.OverallUnderStress:0.0}/100 ({rot.StressTest.RetentionPct:0.0}% retained, rank #{rot.StressTest.RankUnderStress})");
                    sb.AppendLine();
                }

                if (rot.Details != null && (rot.Details.EffectiveIrrigationMm > 0 || !string.IsNullOrWhiteSpace(rot.Details.SoilHealthIndicator)))
                {
                    sb.AppendLine("<b>Agronomic Indicators</b>");
                    if (rot.Details.YearlyRainMm > 0f)
                    {
                        sb.AppendLine($"  • Irrigation Need: {rot.Details.EffectiveIrrigationMm:0} mm/yr   |   Usable Rain: {rot.Details.YearlyRainMm:0} mm/yr   |   Deficit: {rot.Details.WaterDeficitMm:0} mm");
                    }
                    else
                    {
                        sb.AppendLine($"  • Irrigation Requirement: {rot.Details.EffectiveIrrigationMm:0} mm/yr ({rot.Details.IrrigationLevel})");
                    }

                    string hddPart = rot.HeatDegreeDaysPerYear > 0f ? $" ({rot.HeatDegreeDaysPerYear:0.0} degree-days/yr)" : string.Empty;
                    sb.AppendLine($"  • Heat Stress: {rot.Details.EffectiveHeatDays:0.0} days/yr{hddPart}");

                    if (!string.IsNullOrWhiteSpace(rot.Details.SoilHealthIndicator))
                    {
                        sb.AppendLine($"  • Soil Health: {rot.Details.SoilHealthIndicator} ({rot.Details.SoilOrganicMatterTrend})");
                    }
                    else
                    {
                        sb.AppendLine($"  • Bare Soil Exposure: {rot.Details.BareMonthsPerYear:0.0} months/yr without green cover");
                    }
                    sb.AppendLine();
                }

                if (rot.FeasibilityWarnings != null && rot.FeasibilityWarnings.Count > 0)
                {
                    sb.AppendLine("<b>Feasibility Warnings</b>");
                    foreach (string w in rot.FeasibilityWarnings)
                    {
                        sb.AppendLine($"  • {w}");
                    }
                    sb.AppendLine();
                }

                if (rot.Explanations != null && rot.Explanations.Count > 0)
                {
                    sb.AppendLine("<b>Key Takeaways</b>");
                    foreach (string exp in rot.Explanations)
                    {
                        sb.AppendLine($"  • {exp}");
                    }
                    sb.AppendLine();
                }

                if (rot.Uncertainty?.Reasons != null && rot.Uncertainty.Reasons.Count > 0)
                {
                    sb.AppendLine($"<b>Uncertainty ({rot.Uncertainty.Level})</b>");
                    foreach (string r in rot.Uncertainty.Reasons)
                    {
                        sb.AppendLine($"  • {r}");
                    }
                }

                summaryText.text = sb.ToString().TrimEnd();
            }
        }
    }
}
