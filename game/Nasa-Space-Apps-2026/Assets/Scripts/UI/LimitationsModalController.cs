using System.Text;
using TMPro;
using UnityEngine;
using FieldShift.Core;
using FieldShift.Data;

namespace FieldShift.UI
{
    /// <summary>
    /// Displays the "What we don't know yet" limitations & prototype disclaimer modal,
    /// populated directly from results.json limitations.
    /// </summary>
    public class LimitationsModalController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button backdropButton;

        [Header("Dependencies")]
        [SerializeField] private DataManager dataManager;

        private void Awake()
        {
            if (dataManager == null)
            {
                dataManager = DataManager.Instance != null ? DataManager.Instance : FindAnyObjectByType<DataManager>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(Close);
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
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
                Populate(dataManager.RawResults);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (backdropButton != null) backdropButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (dataManager != null && dataManager.IsLoaded)
            {
                Populate(dataManager.RawResults);
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }
        }

        public void Close()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        private void HandleDataLoaded(ResultsRoot raw)
        {
            Populate(raw);
        }

        private void Populate(ResultsRoot raw)
        {
            if (titleText != null)
            {
                titleText.text = "What We Don't Know Yet — Limitations";
            }

            if (subtitleText != null)
            {
                string status = dataManager != null ? dataManager.GetPrototypeBannerText() : "PROTOTYPE";
                subtitleText.text = $"{status}. These scores are comparative estimates, not yield predictions. Consult local agricultural extension experts before making farming decisions.";
            }

            if (bodyText != null)
            {
                if (raw?.Limitations == null || raw.Limitations.Count == 0)
                {
                    bodyText.text = "• No limitations listed in results.json.";
                    return;
                }

                var sb = new StringBuilder();
                foreach (string item in raw.Limitations)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        sb.Append("•  ").AppendLine(item);
                    }
                }
                bodyText.text = sb.ToString().TrimEnd();
            }
        }
    }
}
