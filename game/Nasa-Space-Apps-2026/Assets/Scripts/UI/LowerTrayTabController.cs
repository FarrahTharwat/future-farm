using TMPro;
using UnityEngine;

namespace FieldShift.UI
{
    public enum LowerTrayTab
    {
        None = 0,
        Comparison = 1,
        Calendar = 2
    }

    /// <summary>
    /// Coordinates the two lower dashboard drawers (Score Comparison and Season Calendar)
    /// as mutually exclusive tabs so only one can be open at a time.
    /// </summary>
    public class LowerTrayTabController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private ResultsComparisonDashboard comparisonDashboard;
        [SerializeField] private RotationCalendarController calendarController;

        [Header("Tab Buttons")]
        [SerializeField] private UnityEngine.UI.Button comparisonTabButton;
        [SerializeField] private UnityEngine.UI.Image comparisonTabBg;
        [SerializeField] private TMP_Text comparisonTabLabel;

        [SerializeField] private UnityEngine.UI.Button calendarTabButton;
        [SerializeField] private UnityEngine.UI.Image calendarTabBg;
        [SerializeField] private TMP_Text calendarTabLabel;

        [Header("Initial State")]
        [SerializeField] private LowerTrayTab initialTab = LowerTrayTab.Comparison;

        public LowerTrayTab ActiveTab { get; private set; } = LowerTrayTab.None;

        private static readonly Color ActiveTabColor = new Color(0.16f, 0.54f, 0.36f, 1f);
        private static readonly Color ActiveCalendarColor = new Color(0.14f, 0.48f, 0.48f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.20f, 0.28f, 0.25f, 1f);
        private static readonly Color InactiveTextColor = new Color(0.80f, 0.88f, 0.83f, 1f);

        private void Awake()
        {
            if (comparisonTabButton != null)
            {
                comparisonTabButton.onClick.AddListener(HandleComparisonClicked);
            }

            if (calendarTabButton != null)
            {
                calendarTabButton.onClick.AddListener(HandleCalendarClicked);
            }

            if (calendarController != null)
            {
                calendarController.OnDrawerStateChanged += HandleCalendarDrawerChanged;
            }
        }

        private void Start()
        {
            SelectTab(initialTab);
        }

        private void OnDestroy()
        {
            if (comparisonTabButton != null) comparisonTabButton.onClick.RemoveListener(HandleComparisonClicked);
            if (calendarTabButton != null) calendarTabButton.onClick.RemoveListener(HandleCalendarClicked);
            if (calendarController != null) calendarController.OnDrawerStateChanged -= HandleCalendarDrawerChanged;
        }

        public void SelectTab(LowerTrayTab tab)
        {
            ActiveTab = tab;

            if (comparisonDashboard != null)
            {
                comparisonDashboard.SetExpanded(tab == LowerTrayTab.Comparison);
            }

            if (calendarController != null)
            {
                calendarController.SetDrawerOpen(tab == LowerTrayTab.Calendar);
            }

            RefreshTabVisuals();
        }

        private void HandleComparisonClicked()
        {
            SelectTab(ActiveTab == LowerTrayTab.Comparison ? LowerTrayTab.None : LowerTrayTab.Comparison);
        }

        private void HandleCalendarClicked()
        {
            SelectTab(ActiveTab == LowerTrayTab.Calendar ? LowerTrayTab.None : LowerTrayTab.Calendar);
        }

        private void HandleCalendarDrawerChanged(bool isOpen)
        {
            if (!isOpen && ActiveTab == LowerTrayTab.Calendar)
            {
                ActiveTab = LowerTrayTab.None;
                RefreshTabVisuals();
            }
        }

        private void RefreshTabVisuals()
        {
            bool compActive = ActiveTab == LowerTrayTab.Comparison;
            bool calActive = ActiveTab == LowerTrayTab.Calendar;

            if (comparisonTabBg != null) comparisonTabBg.color = compActive ? ActiveTabColor : InactiveTabColor;
            if (comparisonTabLabel != null)
            {
                comparisonTabLabel.text = "Score Comparison";
                comparisonTabLabel.color = compActive ? Color.white : InactiveTextColor;
            }

            if (calendarTabBg != null) calendarTabBg.color = calActive ? ActiveCalendarColor : InactiveTabColor;
            if (calendarTabLabel != null)
            {
                calendarTabLabel.text = "Season Calendar";
                calendarTabLabel.color = calActive ? Color.white : InactiveTextColor;
            }
        }
    }
}
