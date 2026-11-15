using System.Collections.Generic;
using UnityEngine;
using FieldShift.Core;

namespace FieldShift.Farm
{
    /// <summary>
    /// Step 7 diorama visual polish:
    /// - Gentle wind sway on trees, grass clumps, wildflowers, and living plot crops (keeping roots planted).
    /// - Slow drifting soft clouds in the upper meadow sky, subtly tinted by weather and season.
    /// - Subtle gentle breathing on water/atmosphere props without obstructing plot readability.
    /// </summary>
    public class FarmDioramaPolishController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ClimateEnvironmentController climateEnvironment;

        [Header("Wind Sway Settings")]
        [SerializeField] private float cropSwayDegrees = 2.6f;
        [SerializeField] private float treeSwayDegrees = 1.4f;
        [SerializeField] private float grassSwayDegrees = 3.2f;
        [SerializeField] private float swayFrequency = 1.45f;

        [Header("Sky Cloud Settings")]
        [SerializeField] private int cloudSortingOrder = -90;
        [SerializeField] private float cloudDriftSpeed = 0.14f;

        private struct SwayTarget
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 BaseLocalPos;
            public float BaseZRot;
            public float PhaseOffset;
            public float AmplitudeDeg;
            public float RootCompX;
            public bool RequiresEnabledRenderer;
        }

        private struct CloudItem
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float Speed;
            public float BaseAlpha;
            public float BobPhase;
            public float BaseY;
        }

        private readonly List<SwayTarget> _swayTargets = new List<SwayTarget>();
        private readonly List<CloudItem> _clouds = new List<CloudItem>();
        private Texture2D _cloudTexture;
        private Sprite _cloudSprite;
        private float _elapsed;

        private void Awake()
        {
            if (climateEnvironment == null)
            {
                climateEnvironment = GetComponent<ClimateEnvironmentController>();
            }

            CollectFoliageAndCropTargets();
            CreateSkyClouds();
        }

        private void OnDestroy()
        {
            if (_cloudSprite != null) Destroy(_cloudSprite);
            if (_cloudTexture != null) Destroy(_cloudTexture);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float wind = climateEnvironment != null ? climateEnvironment.CurrentWindStrength : 0.7f;
            UpdateWindSway(wind);
            UpdateClouds();
        }

        private void CollectFoliageAndCropTargets()
        {
            _swayTargets.Clear();

            // 1. Background foliage in FarmEnvironment
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                string n = sr.name;
                if (n.EndsWith("_Shadow")) continue;

                float amp = 0f;
                float rootComp = 0f;
                if (n.StartsWith("Tree_"))
                {
                    amp = treeSwayDegrees;
                    rootComp = 0.032f;
                }
                else if (n.StartsWith("Grass_") || n.StartsWith("Flower_"))
                {
                    amp = grassSwayDegrees;
                    rootComp = 0.015f;
                }
                else if (n.StartsWith("Bush_") || n.StartsWith("Scare"))
                {
                    amp = treeSwayDegrees * 0.85f;
                    rootComp = 0.014f;
                }

                if (amp > 0f)
                {
                    AddSwayTarget(sr.transform, sr, amp, rootComp, requireEnabled: false);
                }
            }

            // 2. Active crop stems on each FarmPlotView
            foreach (FarmPlotView plot in FindObjectsByType<FarmPlotView>())
            {
                if (plot == null || plot.CropRenderers == null) continue;
                foreach (SpriteRenderer cropSr in plot.CropRenderers)
                {
                    if (cropSr == null) continue;
                    AddSwayTarget(cropSr.transform, cropSr, cropSwayDegrees, 0.020f, requireEnabled: true);
                }
            }
        }

        private void AddSwayTarget(Transform t, SpriteRenderer sr, float ampDeg, float rootCompX, bool requireEnabled)
        {
            Vector3 wp = t.position;
            float phase = (wp.x * 0.73f) + (wp.y * 1.19f);
            _swayTargets.Add(new SwayTarget
            {
                Transform = t,
                Renderer = sr,
                BaseLocalPos = t.localPosition,
                BaseZRot = t.localEulerAngles.z,
                PhaseOffset = phase,
                AmplitudeDeg = ampDeg,
                RootCompX = rootCompX,
                RequiresEnabledRenderer = requireEnabled
            });
        }

        private void UpdateWindSway(float windStrength)
        {
            float baseTime = _elapsed * swayFrequency;
            for (int i = 0; i < _swayTargets.Count; i++)
            {
                SwayTarget item = _swayTargets[i];
                if (item.Transform == null) continue;
                if (item.RequiresEnabledRenderer && (item.Renderer == null || !item.Renderer.enabled))
                {
                    continue;
                }

                float wave = Mathf.Sin(baseTime + item.PhaseOffset)
                           + 0.35f * Mathf.Sin((baseTime * 1.9f) + (item.PhaseOffset * 1.4f));
                float angle = wave * item.AmplitudeDeg * windStrength;
                item.Transform.localRotation = Quaternion.Euler(0f, 0f, item.BaseZRot - angle);

                Vector3 pos = item.BaseLocalPos;
                pos.x += wave * item.RootCompX * windStrength;
                item.Transform.localPosition = pos;
            }
        }

        private void CreateSkyClouds()
        {
            _cloudTexture = BuildSoftCloudTexture(96, 44);
            _cloudSprite = Sprite.Create(
                _cloudTexture,
                new Rect(0f, 0f, _cloudTexture.width, _cloudTexture.height),
                new Vector2(0.5f, 0.5f),
                32f);

            var cloudConfigs = new[]
            {
                (pos: new Vector3(-7.5f, 3.15f, 2f), scale: new Vector3(1.55f, 1.05f, 1f), speed: 1.00f, alpha: 0.42f),
                (pos: new Vector3(-1.4f, 3.55f, 2f), scale: new Vector3(1.90f, 1.18f, 1f), speed: 0.78f, alpha: 0.36f),
                (pos: new Vector3(4.8f,  2.95f, 2f), scale: new Vector3(1.40f, 0.96f, 1f), speed: 1.15f, alpha: 0.38f),
                (pos: new Vector3(9.8f,  3.45f, 2f), scale: new Vector3(1.65f, 1.08f, 1f), speed: 0.88f, alpha: 0.34f)
            };

            for (int i = 0; i < cloudConfigs.Length; i++)
            {
                var cfg = cloudConfigs[i];
                var go = new GameObject($"AmbientCloud_{i + 1}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = cfg.pos;
                go.transform.localScale = cfg.scale;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _cloudSprite;
                sr.sortingOrder = cloudSortingOrder;
                sr.color = new Color(1f, 1f, 1f, cfg.alpha);

                _clouds.Add(new CloudItem
                {
                    Transform = go.transform,
                    Renderer = sr,
                    Speed = cloudDriftSpeed * cfg.speed,
                    BaseAlpha = cfg.alpha,
                    BobPhase = i * 1.7f,
                    BaseY = cfg.pos.y
                });
            }
        }

        private void UpdateClouds()
        {
            float rainDelta = 0f;
            float tempDelta = 0f;
            if (ScenarioCalculationLayer.Instance != null && ScenarioCalculationLayer.Instance.CurrentState != null)
            {
                rainDelta = ScenarioCalculationLayer.Instance.CurrentState.Inputs.RainfallDeltaPct;
                tempDelta = ScenarioCalculationLayer.Instance.CurrentState.Inputs.TemperatureDeltaC;
            }

            float wet = rainDelta > 0.5f ? Mathf.Clamp01(rainDelta / 20f) : 0f;
            float dry = rainDelta < -0.5f ? Mathf.Clamp01(-rainDelta / 20f) : 0f;
            float heat = tempDelta > 0.05f ? Mathf.Clamp01(tempDelta / 3f) : 0f;

            Color cloudTint = Color.white;
            if (wet > 0f) cloudTint = Color.Lerp(cloudTint, new Color(0.76f, 0.84f, 0.92f, 1f), wet * 0.85f);
            if (heat > 0f || dry > 0f)
            {
                float warmAmt = Mathf.Clamp01((heat * 0.5f) + (dry * 0.4f));
                cloudTint = Color.Lerp(cloudTint, new Color(1.00f, 0.95f, 0.86f, 1f), warmAmt);
            }

            float alphaScale = 1f + (wet * 0.35f) - (dry * 0.25f);

            for (int i = 0; i < _clouds.Count; i++)
            {
                CloudItem c = _clouds[i];
                if (c.Transform == null) continue;

                Vector3 p = c.Transform.localPosition;
                p.x += c.Speed * Time.deltaTime;
                if (p.x > 13.5f) p.x = -13.5f;
                p.y = c.BaseY + Mathf.Sin((_elapsed * 0.25f) + c.BobPhase) * 0.05f;
                c.Transform.localPosition = p;

                if (c.Renderer != null)
                {
                    Color col = cloudTint;
                    col.a = Mathf.Clamp(c.BaseAlpha * alphaScale, 0.12f, 0.72f);
                    c.Renderer.color = col;
                }
            }
        }

        private static Texture2D BuildSoftCloudTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var lobes = new[]
            {
                (cx: 0.28f, cy: 0.42f, rx: 0.22f, ry: 0.28f),
                (cx: 0.48f, cy: 0.55f, rx: 0.26f, ry: 0.36f),
                (cx: 0.70f, cy: 0.43f, rx: 0.23f, ry: 0.28f),
                (cx: 0.50f, cy: 0.36f, rx: 0.36f, ry: 0.22f)
            };

            for (int y = 0; y < height; y++)
            {
                float ny = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    float maxVal = 0f;
                    foreach (var l in lobes)
                    {
                        float dx = (nx - l.cx) / l.rx;
                        float dy = (ny - l.cy) / l.ry;
                        float d2 = (dx * dx) + (dy * dy);
                        float v = Mathf.Clamp01(1f - d2);
                        if (v > maxVal) maxVal = v;
                    }

                    float alpha = Mathf.SmoothStep(0f, 1f, maxVal) * 0.92f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}

