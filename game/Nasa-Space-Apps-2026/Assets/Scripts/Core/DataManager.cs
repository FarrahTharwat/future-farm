using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using FieldShift.Data;

namespace FieldShift.Core
{
    /// <summary>
    /// Loads and holds the parsed results.json payload from StreamingAssets in a WebGL-safe manner.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private string resultsFileName = "results.json";
        [SerializeField] private bool loadOnAwake = true;

        public ResultsRoot RawResults { get; private set; }
        public bool IsLoaded { get; private set; }
        public string LoadError { get; private set; }

        public event Action<ResultsRoot> OnDataLoaded;
        public event Action<string> OnDataLoadFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (loadOnAwake)
            {
                LoadResults();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Triggers an asynchronous load of StreamingAssets/results.json.
        /// </summary>
        public void LoadResults()
        {
            StartCoroutine(LoadResultsRoutine());
        }

        private IEnumerator LoadResultsRoutine()
        {
            IsLoaded = false;
            LoadError = null;

            string path = BuildStreamingAssetUri(resultsFileName);
            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LoadError = $"Failed to load {resultsFileName} from '{path}': {request.error}";
                    Debug.LogError($"[DataManager] {LoadError}");
                    OnDataLoadFailed?.Invoke(LoadError);
                    yield break;
                }

                string json = request.downloadHandler.text;
                if (TryParseJson(json, out ResultsRoot parsed, out string parseError))
                {
                    RawResults = parsed;
                    IsLoaded = true;
                    Debug.Log($"[DataManager] Loaded '{resultsFileName}' ({RawResults.Rotations?.Count ?? 0} rotations, {RawResults.ScenarioVariants?.Count ?? 0} scenarios).");
                    OnDataLoaded?.Invoke(RawResults);
                }
                else
                {
                    LoadError = parseError;
                    Debug.LogError($"[DataManager] {LoadError}");
                    OnDataLoadFailed?.Invoke(LoadError);
                }
            }
        }

        private static readonly JsonSerializerSettings DeserializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public bool TryParseJson(string json, out ResultsRoot parsed, out string error)
        {
            parsed = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "results.json is empty.";
                return false;
            }

            try
            {
                parsed = JsonConvert.DeserializeObject<ResultsRoot>(json, DeserializerSettings);
                if (parsed == null)
                {
                    error = "Deserialized results.json returned null.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = $"JSON parse error: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Finds the closest matching precomputed ScenarioVariant for the given rainfall and temperature deltas.
        /// Returns null when scenario_variants is empty (e.g., minimal mock JSON).
        /// </summary>
        public ScenarioVariant FindClosestScenarioVariant(float rainfallDeltaPct, float temperatureDeltaC)
        {
            if (RawResults?.ScenarioVariants == null || RawResults.ScenarioVariants.Count == 0)
            {
                return null;
            }

            ScenarioVariant best = null;
            float bestDistance = float.MaxValue;

            foreach (ScenarioVariant variant in RawResults.ScenarioVariants)
            {
                // Normalize rainfall (~20% steps) and temperature (~1.5°C steps) onto comparable scales.
                float rainDiff = (variant.EffectiveRainfallDeltaPct - rainfallDeltaPct) / 20f;
                float tempDiff = (variant.EffectiveTemperatureDeltaC - temperatureDeltaC) / 1.5f;
                float dist = (rainDiff * rainDiff) + (tempDiff * tempDiff);

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = variant;
                }
            }

            return best;
        }

        /// <summary>
        /// Returns a user-facing prototype/data-status banner string driven by data_status or mock.
        /// </summary>
        public string GetPrototypeBannerText()
        {
            if (RawResults == null)
            {
                return "PROTOTYPE — Loading data...";
            }

            if (!string.IsNullOrWhiteSpace(RawResults.DataStatus))
            {
                return RawResults.DataStatus;
            }

            return RawResults.Mock
                ? "PROTOTYPE — Mock data active; results require verification"
                : string.Empty;
        }

        private static string BuildStreamingAssetUri(string fileName)
        {
            string combined = Path.Combine(Application.streamingAssetsPath, fileName);
            // WebGL streamingAssetsPath is already an http/https/relative URL; desktop paths need a file:// URI.
            if (!combined.Contains("://") && !combined.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                combined = new Uri(combined).AbsoluteUri;
            }
            return combined;
        }
    }
}
