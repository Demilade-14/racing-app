using UnityEngine;
using RacingGame.Core;
namespace RacingGame.Tires
{
    /// <summary>
    /// Complete tire state - extends your existing TyreState2026 with
    /// grip calculations, temperature modeling, and wear physics.
    /// </summary>
    [System.Serializable]
    public class TireData
    {
        // ── Identity ──────────────────────────────────────────────────────
        public TireCompound compound;
        public int position; // 0=FL, 1=FR, 2=RL, 3=RR
        // ── State ─────────────────────────────────────────────────────────
        public float lifePercent = 100f;        // 0-100%
        public float temperature;               // Celsius
        public float wearRate;                  // Current wear rate (affected by temp, load)
        public float gripMultiplier = 1f;       // Current grip (0.5 - 1.15)
        // ── Derived Properties ────────────────────────────────────────────
        public bool IsInCliff => lifePercent < TireConstants.CLIFF_THRESHOLD;
        public bool IsCritical => lifePercent < TireConstants.CRITICAL_THRESHOLD;
        public bool IsInOptimalWindow => IsTemperatureOptimal();
        // ── Temperature Modeling ──────────────────────────────────────────
        public void UpdateTemperature(float loadFactor, float ambientTemp, float deltaTime)
        {
            // Load factor: 0 = coasting, 1 = full throttle/braking
            float targetTemp = TireConstants.TEMP_AMBIENT + (loadFactor * 60f);
            // Smooth temperature transition
            float tempDiff = targetTemp - temperature;
            temperature += tempDiff * deltaTime * 0.5f;
            // Clamp to reasonable range
            temperature = Mathf.Clamp(temperature, 20f, 130f);
        }
        public bool IsTemperatureOptimal()
        {
            float min, max;
            GetOptimalWindow(out min, out max);
            return temperature >= min && temperature <= max;
        }
        public void GetOptimalWindow(out float min, out float max)
        {
            switch (compound)
            {
                case TireCompound.Soft:
                    min = TireConstants.SOFT_OPTIMAL_MIN;
                    max = TireConstants.SOFT_OPTIMAL_MAX;
                    break;
                case TireCompound.Medium:
                    min = TireConstants.MEDIUM_OPTIMAL_MIN;
                    max = TireConstants.MEDIUM_OPTIMAL_MAX;
                    break;
                case TireCompound.Hard:
                    min = TireConstants.HARD_OPTIMAL_MIN;
                    max = TireConstants.HARD_OPTIMAL_MAX;
                    break;
                case TireCompound.Intermediate:
                    min = TireConstants.INTER_OPTIMAL_MIN;
                    max = TireConstants.INTER_OPTIMAL_MAX;
                    break;
                case TireCompound.Wet:
                    min = TireConstants.WET_OPTIMAL_MIN;
                    max = TireConstants.WET_OPTIMAL_MAX;
                    break;
                default:
                    min = 80f; max = 100f;
                    break;
            }
        }
        // ── Wear Calculation ──────────────────────────────────────────────
        public void UpdateWear(float loadFactor, float temperatureFactor, float deltaTime)
        {
            if (lifePercent <= 0f) return;
            // Base wear rate from compound
            float baseWear = GetBaseWearRate();
            // Temperature penalty (too hot or too cold = more wear)
            float tempPenalty = 1f;
            if (!IsTemperatureOptimal())
            {
                float min, max;
                GetOptimalWindow(out min, out max);
                float center = (min + max) / 2f;
                float deviation = Mathf.Abs(temperature - center);
                tempPenalty = 1f + (deviation / 50f); // Up to 2x wear if way off temp
            }
            // Load factor (braking/acceleration wears tires more)
            float loadMultiplier = 0.5f + (loadFactor * 1.5f); // 0.5x to 2.0x
            // Cliff effect (below 20% = massive wear)
            float cliffMultiplier = IsInCliff ? 3f : 1f;
            // Final wear calculation
            wearRate = baseWear * tempPenalty * loadMultiplier * cliffMultiplier;
            lifePercent -= wearRate * deltaTime;
            lifePercent = Mathf.Max(0f, lifePercent);
            // Update grip based on life and temperature
            UpdateGrip();
        }
        float GetBaseWearRate()
        {
            return compound switch
            {
                TireCompound.Soft => TireConstants.SOFT_WEAR_RATE,
                TireCompound.Medium => TireConstants.MEDIUM_WEAR_RATE,
                TireCompound.Hard => TireConstants.HARD_WEAR_RATE,
                TireCompound.Intermediate => TireConstants.INTER_WEAR_RATE,
                TireCompound.Wet => TireConstants.WET_WEAR_RATE,
                _ => TireConstants.MEDIUM_WEAR_RATE
            };
        }
        // ── Grip Calculation ──────────────────────────────────────────────
        void UpdateGrip()
        {
            // Base grip from compound
            float baseGrip = GetBaseGrip();
            // Temperature factor (optimal = 1.0, off-temp = 0.7-0.9)
            float tempFactor = 1f;
            if (IsTemperatureOptimal())
            {
                tempFactor = 1f;
            }
            else
            {
                float min, max;
                GetOptimalWindow(out min, out max);
                float deviation = Mathf.Max(
                    Mathf.Abs(temperature - min),
                    Mathf.Abs(temperature - max)
                );
                tempFactor = Mathf.Max(0.7f, 1f - (deviation / 100f));
            }
            // Life factor (100% life = 1.0, 0% life = 0.5)
            float lifeFactor = 0.5f + (lifePercent / 200f);
            // Cliff penalty
            float cliffFactor = IsInCliff ? 0.6f : 1f;
            gripMultiplier = baseGrip * tempFactor * lifeFactor * cliffFactor;
        }
        float GetBaseGrip()
        {
            return compound switch
            {
                TireCompound.Soft => TireConstants.SOFT_GRIP,
                TireCompound.Medium => TireConstants.MEDIUM_GRIP,
                TireCompound.Hard => TireConstants.HARD_GRIP,
                TireCompound.Intermediate => TireConstants.INTER_GRIP,
                TireCompound.Wet => TireConstants.WET_GRIP,
                _ => TireConstants.MEDIUM_GRIP
            };
        }
        // ─ Pit Stop Reset ────────────────────────────────────────────────
        public void ResetToNew(TireCompound newCompound)
        {
            compound = newCompound;
            lifePercent = 100f;
            temperature = 60f; // Cold tires
            gripMultiplier = GetBaseGrip();
        }
    }
}
