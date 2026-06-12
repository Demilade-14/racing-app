using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Core;
namespace RacingGame.Tires
{
    /// <summary>
    /// Helper class for displaying tire data in UI (HUD, timing tower, etc.)
    /// Attach to tire UI elements or call statically.
    /// </summary>
    public static class TireUIHelper
    {
        // ── Color Coding ──────────────────────────────────────────────────
        public static Color GetTireColor(TireCompound compound)
        {
            return compound switch
            {
                TireCompound.Soft => new Color(1f, 0.2f, 0.2f),      // Red
                TireCompound.Medium => new Color(1f, 0.85f, 0f),     // Yellow
                TireCompound.Hard => new Color(0.9f, 0.9f, 0.9f),    // White
                TireCompound.Intermediate => new Color(0.1f, 0.8f, 0.3f), // Green
                TireCompound.Wet => new Color(0.2f, 0.5f, 1f),       // Blue
                _ => Color.white
            };
        }
        public static string GetTireLetter(TireCompound compound)
        {
            return compound switch
            {
                TireCompound.Soft => "S",
                TireCompound.Medium => "M",
                TireCompound.Hard => "H",
                TireCompound.Intermediate => "I",
                TireCompound.Wet => "W",
                _ => "?"
            };
        }
        public static string GetTireName(TireCompound compound)
        {
            return compound switch
            {
                TireCompound.Soft => "SOFT",
                TireCompound.Medium => "MEDIUM",
                TireCompound.Hard => "HARD",
                TireCompound.Intermediate => "INTER",
                TireCompound.Wet => "WET",
                _ => "UNKNOWN"
            };
        }
        // ── Temperature Color ─────────────────────────────────────────────
        public static Color GetTemperatureColor(float temperature, TireCompound compound)
        {
            float min, max;
            GetOptimalWindow(compound, out min, out max);
            if (temperature < min) return Color.blue;           // Too cold
            if (temperature > max) return Color.red;            // Too hot
            return new Color(0.1f, 0.9f, 0.2f);                 // Optimal (green)
        }
        static void GetOptimalWindow(TireCompound compound, out float min, out float max)
        {
            switch (compound)
            {
                case TireCompound.Soft: min = 90f; max = 110f; break;
                case TireCompound.Medium: min = 80f; max = 100f; break;
                case TireCompound.Hard: min = 70f; max = 90f; break;
                case TireCompound.Intermediate: min = 50f; max = 70f; break;
                case TireCompound.Wet: min = 40f; max = 60f; break;
                default: min = 80f; max = 100f; break;
            }
        }
        // ── Wear Bar Color ────────────────────────────────────────────────
        public static Color GetWearColor(float lifePercent)
        {
            if (lifePercent > 60f) return new Color(0.1f, 0.9f, 0.2f);  // Green
            if (lifePercent > 30f) return new Color(1f, 0.85f, 0f);     // Yellow
            if (lifePercent > 15f) return new Color(1f, 0.5f, 0f);      // Orange
            return new Color(1f, 0.1f, 0.1f);                           // Red (critical)
        }
        // ── UI Update Helper ──────────────────────────────────────────────
        public static void UpdateTireUI(
            Image wearBar,
            TextMeshProUGUI tempText,
            TextMeshProUGUI compoundText,
            TireData tire)
        {
            if (wearBar != null)
            {
                wearBar.fillAmount = tire.lifePercent / 100f;
                wearBar.color = GetWearColor(tire.lifePercent);
            }
            if (tempText != null)
            {
                tempText.text = $"{Mathf.RoundToInt(tire.temperature)}°";
                tempText.color = GetTemperatureColor(tire.temperature, tire.compound);
            }
            if (compoundText != null)
            {
                compoundText.text = GetTireLetter(tire.compound);
                compoundText.color = GetTireColor(tire.compound);
            }
        }
    }
}
