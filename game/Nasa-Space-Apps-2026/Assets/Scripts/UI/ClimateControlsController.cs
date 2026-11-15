using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FieldShift.Core;

namespace FieldShift.UI
{
    [Serializable]
    public class ClimateOptionButton
    {
        public float value;
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image background;
        public TMP_Text label;
    }

    /// <summary>
    /// Interactive Climate Scenario Controls for Rainfall (-20%, 0%, +20%)
    /// and Temperature (0°C, +1.5°C, +3°C). Updates ScenarioCalculationLayer,
    /// which handles scenario lookup and downstream notifications.
    /// </summary>
    public class ClimateControlsController : MonoBehaviour
    {
        [Header("Rainfall Options (-20, 0, +20)")]
        [SerializeField] private List<ClimateOptionButton> rainfallOptions = new List<ClimateOptionButton>();

        [Header("Temperature Options (0, +1.5, +3.0)")]
        [SerializeField] private List<ClimateOptionButton> temperatureOptions = new List<ClimateOptionButton>();

        [Header("Scenario Status & Reset")]
        [SerializeField] private UnityEngine.UI.Image scenarioBadgeBackground;
        [SerializeField] private TMP_Text scenarioBadgeText;
        [SerializeField] private UnityEngine.UI.Button resetButton;

        private bool _subscribed;

        private static readonly Color InactiveBg = new Color(0.18f, 0.25f, 0.22f, 1f);
        private static readonly Color InactiveText = new Color(0.84f, 0.90f, 0.86f, 1f);
        private static readonly Color ActiveRainBg = new Color(0.16f, 0.50f, 0.78f, 1f);
        private static readonly Color ActiveTempBg = new Color(0.82f, 0.43f, 0.15f, 1f);

        private static readonly Color BadgeBaseline = new Color(0.16f, 0.34f, 0.25f, 1f);
        private static readonly Color BadgeWet = new Color(0.14f, 0.36f, 0.52f, 1f);
        private static readonly Color BadgeWarm = new Color(0.46f, 0.36f, 0.14f, 1f);
        private static readonly Color BadgeHotDry = new Color(0.62f, 0.24f, 0.14f, 1f);

        private void Awake()
        {
            BindOptionButtons(rainfallOptions, val =>
            {
                if (ScenarioCalculationLayer.Instance != null)
                {
                    ScenarioCalculationLayer.Instance.SetRainfallDelta(val);
                }
            });

            BindOptionButtons(temperatureOptions, val =>
            {
                if (ScenarioCalculationLayer.Instance != null)
                {
                    ScenarioCalculationLayer.Instance.SetTemperatureDelta(val);
                }
            });

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(HandleResetClicked);
            }
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            RefreshDisplay();
        }

        private void Start()
        {
            EnsureSubscribed();
            RefreshDisplay();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(HandleResetClicked);
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

        private void HandleResetClicked()
        {
            if (ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.SetClimateInputs(0f, 0f);
            }
        }

        private void HandleScenarioEvaluated(EvaluatedScenarioState state)
        {
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            ScenarioCalculationLayer calc = ScenarioCalculationLayer.Instance;
            if (calc == null) return;

            ClimateInputs inputs = calc.CurrentInputs;
            UpdateOptionVisuals(rainfallOptions, inputs.RainfallDeltaPct, ActiveRainBg);
            UpdateOptionVisuals(temperatureOptions, inputs.TemperatureDeltaC, ActiveTempBg);

            EvaluatedScenarioState state = calc.CurrentState;
            if (scenarioBadgeText != null)
            {
                scenarioBadgeText.text = state != null && !string.IsNullOrWhiteSpace(state.ScenarioLabel)
                    ? state.ScenarioLabel
                    : $"({inputs.RainfallDeltaPct:+0;-0;0}% Rain, +{inputs.TemperatureDeltaC:0.#}°C)";
            }

            if (scenarioBadgeBackground != null)
            {
                scenarioBadgeBackground.color = ResolveBadgeColor(inputs);
            }
        }

        private static void BindOptionButtons(List<ClimateOptionButton> options, Action<float> onSelected)
        {
            if (options == null) return;

            foreach (ClimateOptionButton opt in options)
            {
                if (opt?.button == null) continue;
                float capturedValue = opt.value;
                opt.button.onClick.AddListener(() => onSelected?.Invoke(capturedValue));
            }
        }

        private static void UpdateOptionVisuals(List<ClimateOptionButton> options, float selectedValue, Color activeColor)
        {
            if (options == null) return;

            foreach (ClimateOptionButton opt in options)
            {
                if (opt == null) continue;
                bool active = Mathf.Abs(opt.value - selectedValue) < 0.05f;

                if (opt.background != null)
                {
                    opt.background.color = active ? activeColor : InactiveBg;
                }

                if (opt.label != null)
                {
                    opt.label.color = active ? Color.white : InactiveText;
                    opt.label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        private static Color ResolveBadgeColor(ClimateInputs inputs)
        {
            if (Mathf.Abs(inputs.RainfallDeltaPct) < 0.1f && Mathf.Abs(inputs.TemperatureDeltaC) < 0.1f)
            {
                return BadgeBaseline;
            }

            if (inputs.RainfallDeltaPct < -0.5f || inputs.TemperatureDeltaC >= 2.5f)
            {
                return BadgeHotDry;
            }

            if (inputs.RainfallDeltaPct > 0.5f && inputs.TemperatureDeltaC <= 1.6f)
            {
                return BadgeWet;
            }

            return BadgeWarm;
        }
    }
}
