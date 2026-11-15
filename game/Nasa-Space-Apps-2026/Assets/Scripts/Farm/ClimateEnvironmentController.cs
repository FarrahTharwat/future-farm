using System.Collections.Generic;
using UnityEngine;
using FieldShift.Core;

namespace FieldShift.Farm
{
    /// <summary>
    /// Reacts to ScenarioCalculationLayer climate inputs (Rainfall -20%/0%/+20%, Temperature 0/+1.5/+3C)
    /// across the surrounding FarmEnvironment diorama: sky/meadow tint, foliage vigor/parching,
    /// structure lighting wash, full-scene atmospheric haze overlay, and WebGL-safe weather particles.
    /// </summary>
    public class ClimateEnvironmentController : MonoBehaviour
    {
        [Header("Scene Backdrop & Camera")]
        [SerializeField] private SpriteRenderer meadowBackdrop;
        [SerializeField] private Camera targetCamera;

        [Header("Environment Sprite Groups")]
        [SerializeField] private List<SpriteRenderer> foliageRenderers = new List<SpriteRenderer>();
        [SerializeField] private List<SpriteRenderer> structureRenderers = new List<SpriteRenderer>();

        [Header("Weather Visual Settings")]
        [SerializeField] private int atmosphereSortingOrder = 45;
        [SerializeField] private int weatherParticlesSortingOrder = 50;
        [SerializeField] private float transitionSpeed = 6f;

        private readonly List<Color> _foliageBaseColors = new List<Color>();
        private readonly List<Color> _structureBaseColors = new List<Color>();
        private Color _meadowBaseColor = Color.white;
        private Color _cameraBaseColor = new Color(0.62f, 0.82f, 0.92f, 1f);

        private Color _targetMeadowColor;
        private Color _targetFoliageColor;
        private Color _targetStructureColor;
        private Color _targetCameraColor;
        private Color _targetAtmosphereColor;

        private Color _currentMeadowColor;
        private Color _currentFoliageColor;
        private Color _currentStructureColor;
        private Color _currentCameraColor;
        private Color _currentAtmosphereColor;

        private SpriteRenderer _atmosphereOverlay;
        private ParticleSystem _rainParticles;
        private ParticleSystem _heatDustParticles;
        private Material _runtimeParticleMaterial;
        private Texture2D _rainTexture;
        private Texture2D _moteTexture;
        private Sprite _whiteSprite;

        private bool _subscribed;
        private bool _timeSubscribed;
        private bool _hasAppliedOnce;
        private float _lastRainDeltaPct;
        private float _lastTempDeltaC;
        private int _currentMonth = 5;

        public float CurrentWindStrength { get; private set; } = 0.65f;
        public Color CurrentAtmosphereColor => _currentAtmosphereColor;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            AutoCollectRenderersIfNeeded();
            CacheBaseColors();
            CreateAtmosphereOverlay();
            CreateWeatherParticleSystems();
            SetTargetsForInputs(0f, 0f, immediate: true);
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            if (ScenarioCalculationLayer.Instance != null && ScenarioCalculationLayer.Instance.CurrentState != null)
            {
                ApplyScenarioState(ScenarioCalculationLayer.Instance.CurrentState, immediate: !_hasAppliedOnce);
            }
        }

        private void Start()
        {
            EnsureSubscribed();
            if (TimeManager.Instance != null)
            {
                _currentMonth = TimeManager.Instance.CurrentMonth;
            }

            if (ScenarioCalculationLayer.Instance != null && ScenarioCalculationLayer.Instance.CurrentState != null)
            {
                ApplyScenarioState(ScenarioCalculationLayer.Instance.CurrentState, immediate: true);
            }
        }

        private void OnDisable()
        {
            if (_subscribed && ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated -= HandleScenarioEvaluated;
                _subscribed = false;
            }

            if (_timeSubscribed && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMonthChanged -= HandleMonthChanged;
                _timeSubscribed = false;
            }
        }

        private void OnDestroy()
        {
            if (_runtimeParticleMaterial != null) Destroy(_runtimeParticleMaterial);
            if (_rainTexture != null) Destroy(_rainTexture);
            if (_moteTexture != null) Destroy(_moteTexture);
            if (_whiteSprite != null)
            {
                if (_whiteSprite.texture != null) Destroy(_whiteSprite.texture);
                Destroy(_whiteSprite);
            }
        }

        private void Update()
        {
            float t = Mathf.Clamp01(Time.deltaTime * transitionSpeed);
            _currentMeadowColor = Color.Lerp(_currentMeadowColor, _targetMeadowColor, t);
            _currentFoliageColor = Color.Lerp(_currentFoliageColor, _targetFoliageColor, t);
            _currentStructureColor = Color.Lerp(_currentStructureColor, _targetStructureColor, t);
            _currentCameraColor = Color.Lerp(_currentCameraColor, _targetCameraColor, t);
            _currentAtmosphereColor = Color.Lerp(_currentAtmosphereColor, _targetAtmosphereColor, t);

            ApplyCurrentColorsToScene();
        }

        private void EnsureSubscribed()
        {
            if (!_subscribed && ScenarioCalculationLayer.Instance != null)
            {
                ScenarioCalculationLayer.Instance.OnScenarioEvaluated += HandleScenarioEvaluated;
                _subscribed = true;
            }

            if (!_timeSubscribed && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMonthChanged += HandleMonthChanged;
                _currentMonth = TimeManager.Instance.CurrentMonth;
                _timeSubscribed = true;
            }
        }

        private void HandleMonthChanged(int year, int month)
        {
            if (month == _currentMonth) return;
            _currentMonth = month;
            SetTargetsForInputs(_lastRainDeltaPct, _lastTempDeltaC, immediate: false);
        }

        private void AutoCollectRenderersIfNeeded()
        {
            if (meadowBackdrop == null)
            {
                Transform bg = transform.Find("ScenicSkyMeadow");
                if (bg != null) meadowBackdrop = bg.GetComponent<SpriteRenderer>();
            }

            if (foliageRenderers.Count == 0 && structureRenderers.Count == 0)
            {
                foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr == null || sr == meadowBackdrop) continue;
                    string n = sr.name;
                    if (n.EndsWith("_Shadow")) continue;

                    if (n.StartsWith("Tree_") || n.StartsWith("Bush_") || n.StartsWith("Grass_") || n.StartsWith("Flower_"))
                    {
                        foliageRenderers.Add(sr);
                    }
                    else
                    {
                        structureRenderers.Add(sr);
                    }
                }
            }
        }

        private void CacheBaseColors()
        {
            if (meadowBackdrop != null) _meadowBaseColor = meadowBackdrop.color;
            if (targetCamera != null) _cameraBaseColor = targetCamera.backgroundColor;

            _foliageBaseColors.Clear();
            foreach (SpriteRenderer sr in foliageRenderers)
            {
                _foliageBaseColors.Add(sr != null ? sr.color : Color.white);
            }

            _structureBaseColors.Clear();
            foreach (SpriteRenderer sr in structureRenderers)
            {
                _structureBaseColors.Add(sr != null ? sr.color : Color.white);
            }
        }

        private void CreateAtmosphereOverlay()
        {
            var go = new GameObject("ClimateAtmosphereOverlay");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0f);
            go.transform.localScale = new Vector3(26f, 16f, 1f);

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);

            _atmosphereOverlay = go.AddComponent<SpriteRenderer>();
            _atmosphereOverlay.sprite = _whiteSprite;
            _atmosphereOverlay.sortingOrder = atmosphereSortingOrder;
            _atmosphereOverlay.color = new Color(1f, 1f, 1f, 0f);
        }

        private void CreateWeatherParticleSystems()
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            _runtimeParticleMaterial = new Material(spriteShader);

            // 1. Slanted rain streak texture & system (+20% Rain)
            _rainTexture = BuildSoftStreakTexture(4, 24);
            var rainMat = new Material(spriteShader) { mainTexture = _rainTexture };
            _rainParticles = CreateParticleEmitter(
                "Weather_RainStreaks",
                new Vector3(0f, 5.5f, 0f),
                new Vector3(20f, 0.5f, 1f),
                rainMat,
                new Vector2(0.07f, 0.52f),
                new Vector3(-1.8f, -11.0f, 0f),
                1.30f,
                new Color(0.84f, 0.94f, 1.00f, 0.62f));
            var rainRenderer = _rainParticles.GetComponent<ParticleSystemRenderer>();
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.velocityScale = 0.055f;
            rainRenderer.lengthScale = 1.8f;

            // 2. Rising golden heat-shimmer / dry dust motes (-20% Rain or +1.5/+3C warming)
            _moteTexture = BuildRadialSoftTexture(16);
            var moteMat = new Material(spriteShader) { mainTexture = _moteTexture };
            _heatDustParticles = CreateParticleEmitter(
                "Weather_HeatDustMotes",
                new Vector3(0f, -1.4f, 0f),
                new Vector3(19f, 5.2f, 1f),
                moteMat,
                new Vector2(0.16f, 0.16f),
                new Vector3(0.55f, 0.70f, 0f),
                3.0f,
                new Color(1.00f, 0.86f, 0.44f, 0.55f));
        }

        private ParticleSystem CreateParticleEmitter(
            string goName,
            Vector3 localPos,
            Vector3 boxSize,
            Material mat,
            Vector2 startSizeXY,
            Vector3 velocity,
            float lifetime,
            Color tint)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startColor = tint;
            main.maxParticles = 320;
            if (Mathf.Approximately(startSizeXY.x, startSizeXY.y))
            {
                main.startSize = new ParticleSystem.MinMaxCurve(startSizeXY.x * 0.65f, startSizeXY.x * 1.35f);
            }
            else
            {
                main.startSize3D = true;
                main.startSizeX = startSizeXY.x;
                main.startSizeY = startSizeXY.y;
                main.startSizeZ = 1f;
            }

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(velocity.x * 0.85f, velocity.x * 1.15f);
            vel.y = new ParticleSystem.MinMaxCurve(velocity.y * 0.85f, velocity.y * 1.15f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.sortingOrder = weatherParticlesSortingOrder;

            return ps;
        }

        private void HandleScenarioEvaluated(EvaluatedScenarioState state)
        {
            if (state == null) return;
            ApplyScenarioState(state, immediate: !_hasAppliedOnce);
        }

        public void ApplyScenarioState(EvaluatedScenarioState state, bool immediate = false)
        {
            if (state == null) return;
            _hasAppliedOnce = true;
            SetTargetsForInputs(state.Inputs.RainfallDeltaPct, state.Inputs.TemperatureDeltaC, immediate);
        }

        private void SetTargetsForInputs(float rainDeltaPct, float tempDeltaC, bool immediate)
        {
            _lastRainDeltaPct = rainDeltaPct;
            _lastTempDeltaC = tempDeltaC;

            float dryFactor = rainDeltaPct < -0.5f ? Mathf.Clamp01(-rainDeltaPct / 20f) : 0f;
            float wetFactor = rainDeltaPct > 0.5f ? Mathf.Clamp01(rainDeltaPct / 20f) : 0f;
            float heatFactor = tempDeltaC > 0.05f ? Mathf.Clamp01(tempDeltaC / 3f) : 0f;

            CurrentWindStrength = 0.65f + (wetFactor * 0.55f) + (heatFactor * 0.25f) + (dryFactor * 0.15f);

            // Subtle seasonal daylight warmth layered underneath scenario weather
            Color seasonMeadowTint = GetSeasonalMeadowTint(_currentMonth);
            Color seasonHazeTint = GetSeasonalAtmosphereTint(_currentMonth);

            // 1. Meadow & Backdrop tint (warm dry ochre on drought/heat; cool overcast emerald on wet)
            Color dryMeadow = new Color(1.06f, 0.87f, 0.60f, 1f);
            Color heatMeadow = new Color(1.06f, 0.92f, 0.72f, 1f);
            Color wetMeadow = new Color(0.84f, 0.94f, 0.96f, 1f);

            Color meadow = seasonMeadowTint;
            if (wetFactor > 0f) meadow = Color.Lerp(meadow, wetMeadow, wetFactor * 0.80f);
            if (dryFactor > 0f) meadow = Color.Lerp(meadow, dryMeadow, dryFactor * 0.65f);
            if (heatFactor > 0f) meadow = Color.Lerp(meadow, heatMeadow, heatFactor * 0.65f);
            _targetMeadowColor = meadow;

            // 2. Foliage tint (trees, bushes, grass clumps, flowers)
            Color dryFoliage = new Color(0.96f, 0.82f, 0.48f, 1f);
            Color wetFoliage = new Color(0.85f, 1.00f, 0.88f, 1f);
            Color foliage = Color.Lerp(Color.white, seasonMeadowTint, 0.45f);
            if (wetFactor > 0f) foliage = Color.Lerp(foliage, wetFoliage, wetFactor * 0.70f);
            float parching = Mathf.Clamp01((dryFactor * 0.65f) + (heatFactor * 0.55f));
            if (parching > 0f) foliage = Color.Lerp(foliage, dryFoliage, parching);
            _targetFoliageColor = foliage;

            // 3. Structures tint (subtle atmospheric bounce lighting)
            Color structWarm = new Color(1.00f, 0.93f, 0.82f, 1f);
            Color structCool = new Color(0.89f, 0.94f, 1.00f, 1f);
            Color structTint = Color.Lerp(Color.white, seasonMeadowTint, 0.35f);
            if (wetFactor > 0f) structTint = Color.Lerp(structTint, structCool, wetFactor * 0.70f);
            if (parching > 0f) structTint = Color.Lerp(structTint, structWarm, parching * 0.60f);
            _targetStructureColor = structTint;

            // 4. Camera sky clear color
            Color skyTarget = _cameraBaseColor;
            if (wetFactor > 0f) skyTarget = Color.Lerp(skyTarget, new Color(0.50f, 0.68f, 0.78f, 1f), wetFactor * 0.70f);
            if (parching > 0f) skyTarget = Color.Lerp(skyTarget, new Color(0.84f, 0.80f, 0.64f, 1f), parching * 0.55f);
            _targetCameraColor = skyTarget;

            // 5. Full-scene soft atmosphere wash overlay
            Color hazeColor = seasonHazeTint;
            if (wetFactor > 0f)
            {
                float alpha = 0.08f * wetFactor * (1f - 0.35f * heatFactor);
                hazeColor = new Color(0.45f, 0.66f, 0.82f, alpha);
            }
            if (parching > 0.05f)
            {
                Color warmHaze = new Color(0.98f, 0.68f, 0.24f, 0.14f * parching);
                hazeColor = Color.Lerp(hazeColor, warmHaze, parching);
            }
            _targetAtmosphereColor = hazeColor;

            // 6. Weather Particle Emitters
            UpdateParticleStates(wetFactor, dryFactor, heatFactor, immediate);

            if (immediate)
            {
                _currentMeadowColor = _targetMeadowColor;
                _currentFoliageColor = _targetFoliageColor;
                _currentStructureColor = _targetStructureColor;
                _currentCameraColor = _targetCameraColor;
                _currentAtmosphereColor = _targetAtmosphereColor;
                ApplyCurrentColorsToScene();
            }
        }

        private static Color GetSeasonalMeadowTint(int month)
        {
            return month switch
            {
                12 or 1 or 2 => new Color(0.93f, 0.96f, 1.01f, 1f),  // Crisp cool winter light
                3 or 4 or 5 => new Color(0.98f, 1.01f, 0.97f, 1f),   // Fresh spring morning
                6 or 7 or 8 => new Color(1.02f, 1.00f, 0.94f, 1f),   // Warm summer noon
                _ => new Color(1.02f, 0.96f, 0.89f, 1f)              // Golden autumn harvest warmth
            };
        }

        private static Color GetSeasonalAtmosphereTint(int month)
        {
            return month switch
            {
                12 or 1 or 2 => new Color(0.72f, 0.85f, 1.00f, 0.035f),
                3 or 4 or 5 => new Color(0.88f, 1.00f, 0.84f, 0.020f),
                6 or 7 or 8 => new Color(1.00f, 0.94f, 0.72f, 0.030f),
                _ => new Color(0.98f, 0.80f, 0.54f, 0.040f)
            };
        }

        private void UpdateParticleStates(float wetFactor, float dryFactor, float heatFactor, bool immediate)
        {
            if (_rainParticles != null)
            {
                var em = _rainParticles.emission;
                if (wetFactor > 0.05f)
                {
                    em.rateOverTime = 140f * wetFactor;
                    if (!_rainParticles.isPlaying) _rainParticles.Play();
                    if (immediate) _rainParticles.Simulate(1.2f, true, false);
                }
                else
                {
                    em.rateOverTime = 0f;
                    _rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (_heatDustParticles != null)
            {
                float intensity = Mathf.Clamp01((dryFactor * 0.55f) + (heatFactor * 0.70f));
                var em = _heatDustParticles.emission;
                if (intensity > 0.10f)
                {
                    var main = _heatDustParticles.main;
                    main.startColor = dryFactor > 0.1f
                        ? new Color(0.98f, 0.80f, 0.40f, 0.28f + (0.24f * intensity))
                        : new Color(1.00f, 0.88f, 0.54f, 0.22f + (0.18f * intensity));
                    em.rateOverTime = 48f * intensity;
                    if (!_heatDustParticles.isPlaying) _heatDustParticles.Play();
                    if (immediate) _heatDustParticles.Simulate(1.5f, true, false);
                }
                else
                {
                    em.rateOverTime = 0f;
                    _heatDustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void ApplyCurrentColorsToScene()
        {
            if (meadowBackdrop != null)
            {
                meadowBackdrop.color = MultiplyRgb(_meadowBaseColor, _currentMeadowColor);
            }

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = _currentCameraColor;
            }

            for (int i = 0; i < foliageRenderers.Count; i++)
            {
                SpriteRenderer sr = foliageRenderers[i];
                if (sr != null)
                {
                    sr.color = MultiplyRgb(_foliageBaseColors[i], _currentFoliageColor);
                }
            }

            for (int i = 0; i < structureRenderers.Count; i++)
            {
                SpriteRenderer sr = structureRenderers[i];
                if (sr != null)
                {
                    sr.color = MultiplyRgb(_structureBaseColors[i], _currentStructureColor);
                }
            }

            if (_atmosphereOverlay != null)
            {
                _atmosphereOverlay.color = _currentAtmosphereColor;
            }
        }

        private static Color MultiplyRgb(Color baseColor, Color tint)
        {
            return new Color(
                Mathf.Clamp01(baseColor.r * tint.r),
                Mathf.Clamp01(baseColor.g * tint.g),
                Mathf.Clamp01(baseColor.b * tint.b),
                baseColor.a);
        }

        private static Texture2D BuildSoftStreakTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                float vFade = Mathf.Clamp01(1f - ny * ny);
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f;
                    float hFade = Mathf.Clamp01(1f - nx * nx);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, vFade * hFade));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildRadialSoftTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
