using System;
using UnityEngine;

namespace FieldShift.Core
{
    /// <summary>
    /// Global single source of truth for simulation time (year, month, play/pause, speed multiplier).
    /// All visual crop growth, timelines, and seasonal displays read from and subscribe to TimeManager.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Cycle Configuration")]
        [Tooltip("Total years in the rotation simulation loop.")]
        [SerializeField] private int totalYears = 3;

        [Tooltip("Real-world seconds per simulated month at 1x speed.")]
        [SerializeField] private float secondsPerMonthAt1x = 2.4f;

        [Tooltip("Allowed speed steps when cycling fast-forward.")]
        [SerializeField] private float[] speedPresets = { 1f, 2f, 4f };

        [Header("Initial State")]
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private int startYear = 1;
        [SerializeField] private int startMonth = 5;

        public bool IsPlaying { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public int TotalYears => Mathf.Max(1, totalYears);

        /// <summary>1-based current year within the rotation cycle (1 .. TotalYears).</summary>
        public int CurrentYear { get; private set; } = 1;

        /// <summary>1-based current month within the year (1 .. 12).</summary>
        public int CurrentMonth { get; private set; } = 1;

        /// <summary>0..1 fractional progress within the current month.</summary>
        public float MonthProgress { get; private set; }

        /// <summary>Continuous elapsed months from Year 1 Month 1 (0 .. TotalYears * 12).</summary>
        public float ContinuousMonth => ((CurrentYear - 1) * 12) + (CurrentMonth - 1) + MonthProgress;

        /// <summary>0..1 normalized progress through the current year.</summary>
        public float NormalizedYearProgress => ((CurrentMonth - 1) + MonthProgress) / 12f;

        /// <summary>0..1 normalized progress through the entire multi-year rotation cycle.</summary>
        public float NormalizedCycleProgress => ContinuousMonth / (TotalYears * 12f);

        public event Action OnTimeUpdated;
        public event Action<int, int> OnMonthChanged;
        public event Action<int> OnYearChanged;
        public event Action<bool> OnPlayStateChanged;
        public event Action<float> OnSpeedChanged;

        private float _elapsedCycleSeconds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeTime(startYear, startMonth, 0f);
            IsPlaying = playOnStart;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!IsPlaying || secondsPerMonthAt1x <= 0.0001f)
            {
                return;
            }

            AdvanceTime(Time.deltaTime * SpeedMultiplier);
        }

        public void SetTotalYears(int years)
        {
            totalYears = Mathf.Max(1, years);
            SetTime(Mathf.Clamp(CurrentYear, 1, totalYears), CurrentMonth, MonthProgress);
        }

        public void Play()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            OnPlayStateChanged?.Invoke(true);
        }

        public void Pause()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            OnPlayStateChanged?.Invoke(false);
        }

        public void TogglePlayPause()
        {
            if (IsPlaying) Pause();
            else Play();
        }

        public void SetSpeed(float multiplier)
        {
            float clamped = Mathf.Max(0.1f, multiplier);
            if (Mathf.Approximately(SpeedMultiplier, clamped)) return;
            SpeedMultiplier = clamped;
            OnSpeedChanged?.Invoke(SpeedMultiplier);
        }

        /// <summary>
        /// Cycles through speedPresets (e.g. 1x -> 2x -> 4x -> 1x) and ensures playback starts.
        /// </summary>
        public void CycleFastForward()
        {
            if (speedPresets == null || speedPresets.Length == 0)
            {
                SetSpeed(SpeedMultiplier >= 4f ? 1f : SpeedMultiplier * 2f);
                Play();
                return;
            }

            int currentIndex = 0;
            float bestDiff = float.MaxValue;
            for (int i = 0; i < speedPresets.Length; i++)
            {
                float diff = Mathf.Abs(speedPresets[i] - SpeedMultiplier);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    currentIndex = i;
                }
            }

            int nextIndex = (currentIndex + 1) % speedPresets.Length;
            SetSpeed(speedPresets[nextIndex]);
            Play();
        }

        public void ResetTime()
        {
            Pause();
            SetSpeed(1f);
            SetTime(1, 1, 0f);
        }

        public void StepYear(int deltaYears)
        {
            StepMonth(deltaYears * 12);
        }

        public void StepMonth(int deltaMonths)
        {
            int totalMonths = TotalYears * 12;
            int currentZeroBased = ((CurrentYear - 1) * 12) + (CurrentMonth - 1);
            int nextZeroBased = ((currentZeroBased + deltaMonths) % totalMonths + totalMonths) % totalMonths;
            int nextYear = (nextZeroBased / 12) + 1;
            int nextMonth = (nextZeroBased % 12) + 1;
            SetTime(nextYear, nextMonth, 0f);
        }

        public void SetTime(int year, int month, float monthProgress = 0f)
        {
            int prevYear = CurrentYear;
            int prevMonth = CurrentMonth;

            InitializeTime(year, month, monthProgress);

            if (CurrentYear != prevYear)
            {
                OnYearChanged?.Invoke(CurrentYear);
            }
            if (CurrentMonth != prevMonth || CurrentYear != prevYear)
            {
                OnMonthChanged?.Invoke(CurrentYear, CurrentMonth);
            }
            OnTimeUpdated?.Invoke();
        }

        public void SetNormalizedCycleProgress(float normalized01)
        {
            float clamped = Mathf.Clamp01(normalized01);
            if (clamped >= 1f) clamped = 0.9999f;

            float totalMonthsFloat = clamped * TotalYears * 12f;
            int wholeMonths = Mathf.FloorToInt(totalMonthsFloat);
            float frac = totalMonthsFloat - wholeMonths;

            int year = (wholeMonths / 12) + 1;
            int month = (wholeMonths % 12) + 1;
            SetTime(year, month, frac);
        }

        private static readonly string[] MonthShortNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        private static readonly string[] MonthFullNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        private void OnValidate()
        {
            totalYears = Mathf.Max(1, totalYears);
            secondsPerMonthAt1x = Mathf.Max(0.05f, secondsPerMonthAt1x);
            startYear = Mathf.Clamp(startYear, 1, totalYears);
            startMonth = Mathf.Clamp(startMonth, 1, 12);
        }

        public string GetFormattedDate()
        {
            return $"Year {CurrentYear} — {GetMonthShortName(CurrentMonth)}";
        }

        public static string GetMonthShortName(int month)
        {
            return MonthShortNames[Mathf.Clamp(month - 1, 0, 11)];
        }

        public static string GetMonthFullName(int month)
        {
            return MonthFullNames[Mathf.Clamp(month - 1, 0, 11)];
        }

        public static string GetSeasonName(int month)
        {
            int m = Mathf.Clamp(month, 1, 12);
            if (m >= 3 && m <= 5) return "Spring";
            if (m >= 6 && m <= 8) return "Summer";
            if (m >= 9 && m <= 11) return "Autumn";
            return "Winter";
        }

        private void InitializeTime(int year, int month, float monthProgress)
        {
            CurrentYear = Mathf.Clamp(year, 1, TotalYears);
            CurrentMonth = Mathf.Clamp(month, 1, 12);
            MonthProgress = Mathf.Clamp01(monthProgress);

            float monthsElapsed = ((CurrentYear - 1) * 12) + (CurrentMonth - 1) + MonthProgress;
            _elapsedCycleSeconds = monthsElapsed * secondsPerMonthAt1x;
        }

        private void AdvanceTime(float scaledDeltaSeconds)
        {
            int totalMonths = TotalYears * 12;
            float cycleDurationSeconds = totalMonths * secondsPerMonthAt1x;
            if (cycleDurationSeconds <= 0.0001f) return;

            _elapsedCycleSeconds = (_elapsedCycleSeconds + scaledDeltaSeconds) % cycleDurationSeconds;
            if (_elapsedCycleSeconds < 0f) _elapsedCycleSeconds += cycleDurationSeconds;

            float totalMonthsFloat = _elapsedCycleSeconds / secondsPerMonthAt1x;
            int wholeMonths = Mathf.FloorToInt(totalMonthsFloat) % totalMonths;
            float frac = totalMonthsFloat - Mathf.Floor(totalMonthsFloat);

            int newYear = (wholeMonths / 12) + 1;
            int newMonth = (wholeMonths % 12) + 1;

            bool yearChanged = newYear != CurrentYear;
            bool monthChanged = newMonth != CurrentMonth || yearChanged;

            CurrentYear = newYear;
            CurrentMonth = newMonth;
            MonthProgress = frac;

            if (yearChanged) OnYearChanged?.Invoke(CurrentYear);
            if (monthChanged) OnMonthChanged?.Invoke(CurrentYear, CurrentMonth);
            OnTimeUpdated?.Invoke();
        }
    }
}
