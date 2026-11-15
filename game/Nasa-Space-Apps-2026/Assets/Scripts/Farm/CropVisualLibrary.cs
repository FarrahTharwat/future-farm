using System;
using System.Collections.Generic;
using UnityEngine;

namespace FieldShift.Farm
{
    public enum CropGrowthStage
    {
        Fallow = 0,
        Planting = 1,
        Growing = 2,
        Flowering = 3,
        Harvest = 4
    }

    [Serializable]
    public class CropVisualStyle
    {
        public string cropKeyword;
        public string displayName;
        public Color tintColor = Color.white;
        public Color harvestTint = Color.white;
        public Color badgeColor = new Color(0.24f, 0.56f, 0.34f, 1f);
        public float scaleMultiplier = 1f;

        [Header("Per-Crop Stage Sprites (Optional Override)")]
        public Sprite plantingSprite;
        public Sprite growingSprite;
        public Sprite floweringSprite;
        public Sprite harvestSprite;
    }

    /// <summary>
    /// Maps crop names and growth stages to isometric farm sprites, tints, and scales.
    /// Shared across all FarmPlotView instances so visual styling is centralized.
    /// </summary>
    public class CropVisualLibrary : MonoBehaviour
    {
        public static CropVisualLibrary Instance { get; private set; }

        [Header("Default Fallback Stage Sprites")]
        [SerializeField] private Sprite plantingSprite;
        [SerializeField] private Sprite growingSprite;
        [SerializeField] private Sprite floweringSprite;
        [SerializeField] private Sprite harvestSprite;
        [SerializeField] private Sprite fallowSoilSprite;
        [SerializeField] private Sprite farmlandSoilSprite;

        [Header("Crop Appearance Styles")]
        [SerializeField] private List<CropVisualStyle> cropStyles = new List<CropVisualStyle>
        {
            new CropVisualStyle
            {
                cropKeyword = "alfalfa_establishment",
                displayName = "Alfalfa (Est.)",
                tintColor = new Color(0.88f, 0.98f, 0.90f, 1f),
                harvestTint = new Color(0.92f, 0.98f, 0.90f, 1f),
                badgeColor = new Color(0.28f, 0.66f, 0.44f, 1f),
                scaleMultiplier = 0.86f
            },
            new CropVisualStyle
            {
                cropKeyword = "alfalfa",
                displayName = "Alfalfa",
                tintColor = Color.white,
                harvestTint = Color.white,
                badgeColor = new Color(0.14f, 0.50f, 0.30f, 1f),
                scaleMultiplier = 1.05f
            },
            new CropVisualStyle
            {
                cropKeyword = "wheat",
                displayName = "Winter Wheat",
                tintColor = Color.white,
                harvestTint = new Color(1.00f, 0.95f, 0.80f, 1f),
                badgeColor = new Color(0.78f, 0.50f, 0.09f, 1f),
                scaleMultiplier = 1.05f
            },
            new CropVisualStyle
            {
                cropKeyword = "barley",
                displayName = "Barley",
                tintColor = new Color(0.92f, 0.96f, 0.72f, 1f),
                harvestTint = new Color(1.00f, 0.88f, 0.54f, 1f),
                badgeColor = new Color(0.74f, 0.34f, 0.18f, 1f),
                scaleMultiplier = 0.94f
            },
            new CropVisualStyle
            {
                cropKeyword = "pea",
                displayName = "Field Pea",
                tintColor = Color.white,
                harvestTint = new Color(0.95f, 0.98f, 0.82f, 1f),
                badgeColor = new Color(0.12f, 0.50f, 0.56f, 1f),
                scaleMultiplier = 1.00f
            },
            new CropVisualStyle
            {
                cropKeyword = "corn",
                displayName = "Corn Silage",
                tintColor = Color.white,
                harvestTint = new Color(1.00f, 0.95f, 0.82f, 1f),
                badgeColor = new Color(0.54f, 0.52f, 0.12f, 1f),
                scaleMultiplier = 1.06f
            },
            new CropVisualStyle
            {
                cropKeyword = "rye",
                displayName = "Cereal Rye",
                tintColor = new Color(0.88f, 0.96f, 0.82f, 1f),
                harvestTint = new Color(0.98f, 0.92f, 0.72f, 1f),
                badgeColor = new Color(0.22f, 0.43f, 0.70f, 1f),
                scaleMultiplier = 0.98f
            },
            new CropVisualStyle
            {
                cropKeyword = "vetch",
                displayName = "Hairy Vetch",
                tintColor = new Color(0.92f, 0.90f, 0.98f, 1f),
                harvestTint = Color.white,
                badgeColor = new Color(0.48f, 0.27f, 0.62f, 1f),
                scaleMultiplier = 0.96f
            },
            new CropVisualStyle
            {
                cropKeyword = "cover",
                displayName = "Winter Cover Crop",
                tintColor = new Color(0.84f, 0.96f, 0.88f, 1f),
                harvestTint = Color.white,
                badgeColor = new Color(0.48f, 0.27f, 0.62f, 1f),
                scaleMultiplier = 1.02f
            }
        };

        private static readonly CropVisualStyle DefaultStyle = new CropVisualStyle
        {
            cropKeyword = "default",
            displayName = "Crop",
            tintColor = new Color(0.72f, 0.92f, 0.60f, 1f),
            badgeColor = new Color(0.28f, 0.54f, 0.36f, 1f),
            scaleMultiplier = 0.95f
        };

        public Sprite FarmlandSoilSprite => farmlandSoilSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public CropVisualStyle GetStyleForCrop(string cropName)
        {
            if (string.IsNullOrWhiteSpace(cropName))
            {
                return DefaultStyle;
            }

            string lower = cropName.ToLowerInvariant().Replace(' ', '_');
            string normalizedSpaces = lower.Replace('_', ' ');
            foreach (CropVisualStyle style in cropStyles)
            {
                if (string.IsNullOrWhiteSpace(style.cropKeyword)) continue;
                string kw = style.cropKeyword.ToLowerInvariant();
                if (lower.Contains(kw) || normalizedSpaces.Contains(kw.Replace('_', ' ')))
                {
                    return style;
                }
            }

            return DefaultStyle;
        }

        public Sprite GetSpriteForStage(CropGrowthStage stage)
        {
            return GetSpriteForStage((CropVisualStyle)null, stage);
        }

        public Sprite GetSpriteForStage(string cropName, CropGrowthStage stage)
        {
            return GetSpriteForStage(GetStyleForCrop(cropName), stage);
        }

        public Sprite GetSpriteForStage(CropVisualStyle style, CropGrowthStage stage)
        {
            if (stage == CropGrowthStage.Fallow)
            {
                return fallowSoilSprite;
            }

            if (style != null)
            {
                Sprite overrideSprite = stage switch
                {
                    CropGrowthStage.Planting => style.plantingSprite,
                    CropGrowthStage.Growing => style.growingSprite,
                    CropGrowthStage.Flowering => style.floweringSprite,
                    CropGrowthStage.Harvest => style.harvestSprite,
                    _ => null
                };

                if (overrideSprite != null)
                {
                    return overrideSprite;
                }
            }

            return stage switch
            {
                CropGrowthStage.Planting => plantingSprite != null ? plantingSprite : growingSprite,
                CropGrowthStage.Growing => growingSprite != null ? growingSprite : floweringSprite,
                CropGrowthStage.Flowering => floweringSprite != null ? floweringSprite : growingSprite,
                CropGrowthStage.Harvest => harvestSprite != null ? harvestSprite : floweringSprite,
                _ => fallowSoilSprite
            };
        }
    }
}
