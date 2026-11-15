using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FieldShift.Core;
using FieldShift.Data;
using FieldShift.Farm;

namespace FieldShift.UI
{
    [Serializable]
    public class CalendarRowRefs
    {
        public string rotationId;
        public TMP_Text labelText;
        public RectTransform trackRect;
        public RectTransform miniTrackRect;
    }

    /// <summary>
    /// Renders the 3-year per-crop planting-to-harvest schedule for all three rotations
    /// both as a compact 3-lane strip synced with the bottom TimeControlsBar and an
    /// expandable interactive Season Calendar Drawer with clickable crop windows and playhead.
    /// </summary>
    public class RotationCalendarController : MonoBehaviour
    {
        [Header("Drawer UI")]
        [SerializeField] private GameObject drawerRoot;
        [SerializeField] private UnityEngine.UI.Button toggleButton;
        [SerializeField] private TMP_Text toggleButtonLabel;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private RectTransform playheadRect;

        [Header("Rotation Rows")]
        [SerializeField] private List<CalendarRowRefs> rows = new List<CalendarRowRefs>();

        [Header("Styling")]
        [SerializeField] private Sprite barSprite;
        [SerializeField] private TMP_FontAsset barFont;
        [SerializeField] private bool startExpanded = false;

        private readonly List<SegmentVisual> _activeSegments = new List<SegmentVisual>();
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private bool _subscribedScenario;
        private bool _subscribedTime;
        private bool _isOpen;
        private string _builtSignature;

        public bool IsOpen => _isOpen;
        public event Action<bool> OnDrawerStateChanged;

        private struct SegmentVisual
        {
            public CropSeasonEntry Entry;
            public UnityEngine.UI.Image Image;
            public Color BaseColor;
        }

        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleDrawer);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseDrawer);
            }

            SetDrawerOpen(startExpanded);
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            RebuildFromCurrentState();
            UpdatePlayheadAndHighlights();
        }

        private void Start()
        {
            EnsureSubscribed();
            RebuildFromCurrentState();
            UpdatePlayheadAndHighlights();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (toggleButton != null) toggleButton.onClick.RemoveListener(ToggleDrawer);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseDrawer);
        }

        private void EnsureSubscribed()
        {
            if (!_subscribedScenario && ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated += HandleScenarioEvaluated;
                _subscribedScenario = true;
            }

            if (!_subscribedTime && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated += HandleTimeUpdated;
                _subscribedTime = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedScenario && ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated -= HandleScenarioEvaluated;
                _subscribedScenario = false;
            }

            if (_subscribedTime && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                _subscribedTime = false;
            }
        }

        public void ToggleDrawer()
        {
            SetDrawerOpen(!_isOpen);
        }

        public void CloseDrawer()
        {
            SetDrawerOpen(false);
        }

        public void SetDrawerOpen(bool open)
        {
            _isOpen = open;
            if (drawerRoot != null)
            {
                drawerRoot.SetActive(open);
            }

            if (toggleButtonLabel != null)
            {
                toggleButtonLabel.text = open ? "Close Calendar" : "Season Calendar";
            }

            if (open)
            {
                UpdatePlayheadAndHighlights();
            }

            OnDrawerStateChanged?.Invoke(_isOpen);
        }

        private void HandleScenarioEvaluated(EvaluatedScenarioState state)
        {
            RebuildFromState(state);
            UpdatePlayheadAndHighlights();
        }

        private void HandleTimeUpdated()
        {
            UpdatePlayheadAndHighlights();
        }

        private void RebuildFromCurrentState()
        {
            if (ScenarioCalculationLayer.Instance != null && ScenarioCalculationLayer.Instance.CurrentState != null)
            {
                RebuildFromState(ScenarioCalculationLayer.Instance.CurrentState);
            }
        }

        private void RebuildFromState(EvaluatedScenarioState state)
        {
            if (state == null) return;

            int totalYears = TimeManager.Instance != null ? TimeManager.Instance.TotalYears : 5;
            foreach (CalendarRowRefs row in rows)
            {
                if (row?.labelText == null) continue;
                EvaluatedRotationData rot = state.GetRotationById(row.rotationId);
                if (rot == null) continue;

                string rankPrefix = rot.Rank > 0 ? $"#{rot.Rank} " : string.Empty;
                int cycleLen = Mathf.Max(1, rot.RotationLengthYears);
                row.labelText.text = $"{rankPrefix}{rot.Name} ({cycleLen}y)";
            }

            string sig = $"{totalYears}:{state.Rotations?.Count ?? 0}";
            if (sig == _builtSignature && _spawnedObjects.Count > 0)
            {
                return;
            }
            _builtSignature = sig;

            ClearSpawned();

            CropVisualLibrary visualLib = CropVisualLibrary.Instance;
            foreach (CalendarRowRefs row in rows)
            {
                if (row == null) continue;
                EvaluatedRotationData rot = state.GetRotationById(row.rotationId);
                if (rot?.Seasons == null) continue;

                int rotLength = Mathf.Max(1, rot.RotationLengthYears);
                for (int displayYear = 1; displayYear <= totalYears; displayYear++)
                {
                    int cycleYear = ((displayYear - 1) % rotLength) + 1;
                    bool isRepeatedCycle = displayYear > rotLength;

                    foreach (CropSeasonEntry sourceEntry in rot.Seasons)
                    {
                        if (sourceEntry == null || sourceEntry.Year != cycleYear) continue;

                        var displayEntry = sourceEntry.Clone();
                        displayEntry.Year = displayYear;

                        string cropKey = !string.IsNullOrWhiteSpace(displayEntry.RawCropId)
                            ? displayEntry.RawCropId
                            : displayEntry.Crop;
                        CropVisualStyle style = visualLib != null ? visualLib.GetStyleForCrop(cropKey) : null;
                        Color cropColor = style != null ? style.badgeColor : new Color(0.24f, 0.56f, 0.34f, 1f);
                        if (isRepeatedCycle)
                        {
                            // Softly desaturate repeated cycle previews beyond the rotation's native cycle length.
                            cropColor = Color.Lerp(cropColor, new Color(0.26f, 0.30f, 0.28f, 1f), 0.14f);
                        }

                        ComputeSegmentNormalizedRange(displayEntry, totalYears, out float startNorm, out float endNorm);

                        if (row.trackRect != null)
                        {
                            CreateExpandedSegment(row.trackRect, displayEntry, startNorm, endNorm, cropColor);
                        }

                        if (row.miniTrackRect != null)
                        {
                            CreateMiniSegment(row.miniTrackRect, displayEntry, startNorm, endNorm, cropColor);
                        }
                    }
                }
            }
        }

        private void CreateExpandedSegment(
            RectTransform parentTrack,
            CropSeasonEntry entry,
            float startNorm,
            float endNorm,
            Color baseColor)
        {
            var go = new GameObject($"Seg_{entry.Year}_{entry.Crop}", typeof(RectTransform));
            go.transform.SetParent(parentTrack, false);
            _spawnedObjects.Add(go);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(startNorm, 0.14f);
            rt.anchorMax = new Vector2(endNorm, 0.86f);
            rt.offsetMin = new Vector2(1.5f, 0f);
            rt.offsetMax = new Vector2(-1.5f, 0f);

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = barSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = baseColor;
            img.raycastTarget = true;

            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            CropSeasonEntry captured = entry;
            btn.onClick.AddListener(() =>
            {
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.Pause();
                    TimeManager.Instance.SetTime(captured.Year, captured.PlantMonth, 0.25f);
                }
            });

            var lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = new Vector2(6f, 0f);
            lblRt.offsetMax = new Vector2(-6f, 0f);

            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            if (barFont != null) tmp.font = barFont;
            int spanMonths = entry.HarvestMonth >= entry.PlantMonth
                ? (entry.HarvestMonth - entry.PlantMonth + 1)
                : ((12 - entry.PlantMonth + 1) + entry.HarvestMonth);
            string pShort = TimeManager.GetMonthShortName(entry.PlantMonth);
            string hShort = TimeManager.GetMonthShortName(entry.HarvestMonth);
            tmp.text = spanMonths >= 4
                ? $"{entry.Crop} ({pShort}-{hShort})"
                : spanMonths >= 2
                    ? entry.Crop
                    : AbbreviateSingleMonthCrop(entry.Crop);
            tmp.fontSize = 13.5f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8.5f;
            tmp.fontSizeMax = 13.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;

            _activeSegments.Add(new SegmentVisual
            {
                Entry = entry,
                Image = img,
                BaseColor = baseColor
            });
        }

        private void CreateMiniSegment(
            RectTransform parentTrack,
            CropSeasonEntry entry,
            float startNorm,
            float endNorm,
            Color baseColor)
        {
            var go = new GameObject($"Mini_{entry.Year}_{entry.Crop}", typeof(RectTransform));
            go.transform.SetParent(parentTrack, false);
            _spawnedObjects.Add(go);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(startNorm, 0f);
            rt.anchorMax = new Vector2(endNorm, 1f);
            rt.offsetMin = new Vector2(1f, 0f);
            rt.offsetMax = new Vector2(-1f, 0f);

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = barSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = baseColor;
            img.raycastTarget = false;
        }

        private void UpdatePlayheadAndHighlights()
        {
            TimeManager tm = TimeManager.Instance;
            if (tm == null) return;

            if (playheadRect != null)
            {
                float x = Mathf.Clamp01(tm.NormalizedCycleProgress);
                playheadRect.anchorMin = new Vector2(x, 0f);
                playheadRect.anchorMax = new Vector2(x, 1f);
                playheadRect.anchoredPosition = Vector2.zero;
            }

            foreach (SegmentVisual seg in _activeSegments)
            {
                if (seg.Image == null || seg.Entry == null) continue;
                bool activeNow = seg.Entry.Year == tm.CurrentYear
                    && IsMonthInRange(tm.CurrentMonth, seg.Entry.PlantMonth, seg.Entry.HarvestMonth);

                seg.Image.color = activeNow
                    ? Color.Lerp(seg.BaseColor, Color.white, 0.22f)
                    : Color.Lerp(seg.BaseColor, new Color(0.18f, 0.22f, 0.20f, 1f), 0.16f);
            }
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawnedObjects.Count; i++)
            {
                if (_spawnedObjects[i] != null)
                {
                    Destroy(_spawnedObjects[i]);
                }
            }
            _spawnedObjects.Clear();
            _activeSegments.Clear();
        }

        private static void ComputeSegmentNormalizedRange(
            CropSeasonEntry entry,
            int totalYears,
            out float startNorm,
            out float endNorm)
        {
            int totalMonths = Mathf.Max(12, totalYears * 12);
            int startIndex = Mathf.Max(0, ((entry.Year - 1) * 12) + (entry.PlantMonth - 1));
            int span = entry.HarvestMonth >= entry.PlantMonth
                ? (entry.HarvestMonth - entry.PlantMonth + 1)
                : ((12 - entry.PlantMonth + 1) + entry.HarvestMonth);

            startNorm = Mathf.Clamp01((float)startIndex / totalMonths);
            endNorm = Mathf.Clamp01((float)(startIndex + Mathf.Max(1, span)) / totalMonths);
        }

        private static bool IsMonthInRange(int month, int startMonth, int endMonth)
        {
            if (startMonth <= endMonth)
            {
                return month >= startMonth && month <= endMonth;
            }
            return month >= startMonth || month <= endMonth;
        }

        private static string AbbreviateSingleMonthCrop(string cropDisplay)
        {
            if (string.IsNullOrWhiteSpace(cropDisplay)) return string.Empty;
            string lower = cropDisplay.ToLowerInvariant();
            if (lower.Contains("vetch")) return "Vetch";
            if (lower.Contains("rye")) return "Rye";
            if (lower.Contains("wheat")) return "Wheat";
            if (lower.Contains("corn")) return "Corn";
            if (lower.Contains("pea")) return "Pea";
            if (lower.Contains("barley")) return "Barley";
            if (lower.Contains("alfalfa")) return "Alf.";
            return cropDisplay;
        }
    }
}
