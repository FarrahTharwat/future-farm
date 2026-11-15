using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FieldShift.Core;

namespace FieldShift.UI
{
    /// <summary>
    /// Binds the bottom Time Controls bar (Play/Pause, Fast-Forward, Month step, Reset,
    /// date/season indicator, and 3-year timeline scrubber) to the global TimeManager.
    /// </summary>
    public class TimeControlsController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button playPauseButton;
        [SerializeField] private UnityEngine.UI.Button fastForwardButton;
        [SerializeField] private UnityEngine.UI.Button stepBackButton;
        [SerializeField] private UnityEngine.UI.Button stepForwardButton;
        [SerializeField] private UnityEngine.UI.Button resetButton;

        [Header("Labels & Visuals")]
        [SerializeField] private TMP_Text playPauseLabel;
        [SerializeField] private UnityEngine.UI.Image playPauseBackground;
        [SerializeField] private TMP_Text speedBadgeText;
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text seasonBadgeText;
        [SerializeField] private UnityEngine.UI.Slider timelineSlider;

        private bool _subscribed;
        private bool _suppressSliderCallback;

        private static readonly Color PlayButtonColor = new Color(0.16f, 0.56f, 0.34f, 1f);
        private static readonly Color PauseButtonColor = new Color(0.82f, 0.50f, 0.14f, 1f);

        private void Awake()
        {
            if (playPauseButton != null)
            {
                playPauseButton.onClick.AddListener(HandlePlayPauseClicked);
            }

            if (fastForwardButton != null)
            {
                fastForwardButton.onClick.AddListener(HandleFastForwardClicked);
            }

            if (stepBackButton != null)
            {
                stepBackButton.onClick.AddListener(HandleStepBackClicked);
            }

            if (stepForwardButton != null)
            {
                stepForwardButton.onClick.AddListener(HandleStepForwardClicked);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(HandleResetClicked);
            }

            if (timelineSlider != null)
            {
                timelineSlider.minValue = 0f;
                timelineSlider.maxValue = 1f;
                timelineSlider.onValueChanged.AddListener(HandleSliderValueChanged);
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

        private void EnsureSubscribed()
        {
            if (_subscribed || TimeManager.Instance == null) return;

            TimeManager.Instance.OnTimeUpdated += HandleTimeUpdated;
            TimeManager.Instance.OnPlayStateChanged += HandlePlayStateChanged;
            TimeManager.Instance.OnSpeedChanged += HandleSpeedChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                TimeManager.Instance.OnPlayStateChanged -= HandlePlayStateChanged;
                TimeManager.Instance.OnSpeedChanged -= HandleSpeedChanged;
            }
            _subscribed = false;
        }

        private void HandlePlayPauseClicked()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.TogglePlayPause();
            }
        }

        private void HandleFastForwardClicked()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.CycleFastForward();
            }
        }

        private void HandleStepBackClicked()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.Pause();
                TimeManager.Instance.StepMonth(-1);
            }
        }

        private void HandleStepForwardClicked()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.Pause();
                TimeManager.Instance.StepMonth(1);
            }
        }

        private void HandleResetClicked()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ResetTime();
            }
        }

        private void HandleSliderValueChanged(float normalized01)
        {
            if (_suppressSliderCallback || TimeManager.Instance == null) return;
            TimeManager.Instance.SetNormalizedCycleProgress(normalized01);
        }

        private void HandleTimeUpdated()
        {
            RefreshTimeDisplay();
        }

        private void HandlePlayStateChanged(bool isPlaying)
        {
            RefreshPlaybackButtons();
        }

        private void HandleSpeedChanged(float speed)
        {
            RefreshPlaybackButtons();
        }

        public void RefreshDisplay()
        {
            RefreshTimeDisplay();
            RefreshPlaybackButtons();
        }

        private void RefreshTimeDisplay()
        {
            TimeManager tm = TimeManager.Instance;
            if (tm == null) return;

            if (dateText != null)
            {
                dateText.text = $"Year {tm.CurrentYear}/{tm.TotalYears}  •  {TimeManager.GetMonthFullName(tm.CurrentMonth)}";
            }

            if (seasonBadgeText != null)
            {
                seasonBadgeText.text = TimeManager.GetSeasonName(tm.CurrentMonth).ToUpperInvariant();
            }

            if (timelineSlider != null)
            {
                _suppressSliderCallback = true;
                timelineSlider.SetValueWithoutNotify(tm.NormalizedCycleProgress);
                _suppressSliderCallback = false;
            }
        }

        private void RefreshPlaybackButtons()
        {
            TimeManager tm = TimeManager.Instance;
            if (tm == null) return;

            if (playPauseLabel != null)
            {
                playPauseLabel.text = tm.IsPlaying ? "PAUSE" : "PLAY";
            }

            if (playPauseBackground != null)
            {
                playPauseBackground.color = tm.IsPlaying ? PauseButtonColor : PlayButtonColor;
            }

            if (speedBadgeText != null)
            {
                speedBadgeText.text = tm.SpeedMultiplier >= 1f
                    ? $"{tm.SpeedMultiplier:0}x"
                    : $"{tm.SpeedMultiplier:0.0}x";
            }
        }
    }
}
