using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FieldShift.Core;
using FieldShift.Farm;

namespace FieldShift.UI
{
    /// <summary>
    /// Coordinates the 3 side-by-side comparison score cards from ScenarioCalculationLayer.
    /// Updates automatically whenever scenario or calculation results change.
    /// </summary>
    public class ResultsComparisonDashboard : MonoBehaviour
    {
        [Header("Comparison Cards")]
        [SerializeField] private List<ResultsComparisonCard> cards = new List<ResultsComparisonCard>();

        [Header("Collapse Toggle (Optional)")]
        [SerializeField] private GameObject cardsContainer;
        [SerializeField] private UnityEngine.UI.Button toggleButton;
        [SerializeField] private TMP_Text toggleButtonLabel;

        [Header("Inspect Integration (Optional)")]
        [SerializeField] private PlotInspectModalController inspectModal;

        private bool _subscribed;
        private bool _isExpanded = true;
        private FarmPlotView[] _plotViews;

        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleExpanded);
            }

            foreach (ResultsComparisonCard card in cards)
            {
                if (card != null)
                {
                    card.OnCardClicked += HandleCardClicked;
                }
            }
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            RefreshFromCurrentScenario();
        }

        private void Start()
        {
            EnsureSubscribed();
            _plotViews = FindObjectsByType<FarmPlotView>();
            RefreshFromCurrentScenario();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(ToggleExpanded);
            }

            foreach (ResultsComparisonCard card in cards)
            {
                if (card != null)
                {
                    card.OnCardClicked -= HandleCardClicked;
                }
            }
        }

        private void EnsureSubscribed()
        {
            if (_subscribed || ScenarioCalculationLayer.Instance == null) return;

            ScenarioCalculationLayer.Instance.OnScenarioEvaluated += HandleScenarioEvaluated;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            if (ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated -= HandleScenarioEvaluated;
            }
            _subscribed = false;
        }

        private void HandleScenarioEvaluated(EvaluatedScenarioState state)
        {
            BindState(state);
        }

        public void RefreshFromCurrentScenario()
        {
            if (ScenarioCalculationLayer.Instance != null && ScenarioCalculationLayer.Instance.CurrentState != null)
            {
                BindState(ScenarioCalculationLayer.Instance.CurrentState);
            }
        }

        public void BindState(EvaluatedScenarioState state)
        {
            if (state == null) return;

            EvaluatedRotationData baseline = state.GetRotationById("current");
            float baselineOverall = baseline != null && baseline.Scores != null ? baseline.Scores.Overall : 0f;

            foreach (ResultsComparisonCard card in cards)
            {
                if (card == null) continue;
                EvaluatedRotationData data = state.GetRotationById(card.RotationId);
                card.Bind(data, baselineOverall);
            }
        }

        public void ToggleExpanded()
        {
            SetExpanded(!_isExpanded);
        }

        public void SetExpanded(bool expanded)
        {
            _isExpanded = expanded;
            if (cardsContainer != null)
            {
                cardsContainer.SetActive(expanded);
            }

            if (toggleButtonLabel != null)
            {
                toggleButtonLabel.text = expanded ? "Hide Comparison" : "Show Comparison";
            }
        }

        private void HandleCardClicked(ResultsComparisonCard card)
        {
            if (card == null || card.BoundData == null) return;

            if (_plotViews == null || _plotViews.Length == 0)
            {
                _plotViews = FindObjectsByType<FarmPlotView>();
            }

            foreach (FarmPlotView plot in _plotViews)
            {
                if (plot == null) continue;
                bool match = string.Equals(plot.RotationId, card.RotationId, System.StringComparison.OrdinalIgnoreCase);
                plot.SetSelected(match);
                if (match && inspectModal != null)
                {
                    inspectModal.Show(plot);
                }
            }
        }
    }
}
