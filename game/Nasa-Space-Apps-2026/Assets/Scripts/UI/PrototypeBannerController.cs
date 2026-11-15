using TMPro;
using UnityEngine;
using FieldShift.Core;
using FieldShift.Data;

namespace FieldShift.UI
{
    /// <summary>
    /// Drives the top PROTOTYPE status banner and farm metadata from DataManager,
    /// and opens the Limitations modal on click.
    /// </summary>
    public class PrototypeBannerController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject bannerContainer;
        [SerializeField] private TMP_Text bannerText;
        [SerializeField] private TMP_Text farmMetadataText;
        [SerializeField] private UnityEngine.UI.Button limitationsButton;
        [SerializeField] private LimitationsModalController limitationsModal;

        [Header("Dependencies")]
        [SerializeField] private DataManager dataManager;

        private void Awake()
        {
            if (dataManager == null)
            {
                dataManager = DataManager.Instance != null ? DataManager.Instance : FindAnyObjectByType<DataManager>();
            }

            if (limitationsButton != null)
            {
                limitationsButton.onClick.AddListener(HandleLimitationsClicked);
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
            if (dataManager != null && dataManager.IsLoaded)
            {
                HandleDataLoaded(dataManager.RawResults);
            }
        }

        private void OnDestroy()
        {
            if (limitationsButton != null)
            {
                limitationsButton.onClick.RemoveListener(HandleLimitationsClicked);
            }
        }

        private void HandleDataLoaded(ResultsRoot raw)
        {
            if (raw == null) return;

            string statusMsg = dataManager != null ? dataManager.GetPrototypeBannerText() : string.Empty;
            bool showBanner = !string.IsNullOrWhiteSpace(statusMsg);

            if (bannerContainer != null)
            {
                bannerContainer.SetActive(showBanner);
            }

            if (bannerText != null && showBanner)
            {
                bannerText.text = statusMsg;
            }

            if (farmMetadataText != null && raw.Farm != null)
            {
                string farmName = !string.IsNullOrWhiteSpace(raw.Farm.Name)
                    ? raw.Farm.Name
                    : $"Farm ({raw.Farm.Latitude:0.00}, {raw.Farm.Longitude:0.00})";
                string period = !string.IsNullOrWhiteSpace(raw.Farm.AnalysisPeriod)
                    ? raw.Farm.AnalysisPeriod
                    : "Baseline";
                string source = !string.IsNullOrWhiteSpace(raw.Farm.SoilSource)
                    ? raw.Farm.SoilSource
                    : (!string.IsNullOrWhiteSpace(raw.Et0Source) ? "NASA POWER ET0" : $"{raw.Farm.Latitude:0.00}N, {raw.Farm.Longitude:0.00}E");
                farmMetadataText.text = $"{farmName}  •  {source}  •  {period}";
            }
        }

        private void HandleLimitationsClicked()
        {
            if (limitationsModal != null)
            {
                limitationsModal.Open();
            }
        }
    }
}
