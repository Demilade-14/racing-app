using UnityEngine;
using RacingGame.Core;
namespace RacingGame.Tires
{
    /// <summary>
    /// Tracks the real-time state of a single tire
    /// </summary>
    [System.Serializable]
    public class TireState
    {
        public TireCompound currentCompound;
        public float wearPercent;           // 0-100 (100 = destroyed)
        public float temperature;           // Celsius
        public float pressure;              // PSI
        public float gripLevel;             // 0-1 (current grip after wear/temp)
        public float degradation;           // 0-1 (how much performance lost)
        public int lapsOnTire;
        public bool isDestroyed;
        private TireCompoundData compoundData;
        public void Initialize(TireCompound compound)
        {
            currentCompound = compound;
            compoundData = TireCompoundData.GetCompound(compound);
            wearPercent = 0f;
            temperature = compoundData.optimalTempMin;
            pressure = 23f; // Standard F1 pressure
            gripLevel = 1f;
            degradation = 0f;
            lapsOnTire = 0;
            isDestroyed = false;
        }
        /// <summary>
        /// Update tire physics for one frame
        /// </summary>
        public void UpdateTire(float deltaTime, float trackTemp, float ambientTemp, bool isWet)
        {
            if (isDestroyed) return;
            // Update compound data (in case of compound change)
            compoundData = TireCompoundData.GetCompound(currentCompound);
            // Temperature physics
            UpdateTemperature(deltaTime, trackTemp, ambientTemp, isWet);
            // Wear physics
            UpdateWear(deltaTime, isWet);
            // Grip calculation
            CalculateGrip();
            // Check for destruction
            if (wearPercent >= 100f)
            {
                isDestroyed = true;
                gripLevel = 0.1f; // Minimal grip on rims
            }
        }
        void UpdateTemperature(float deltaTime, float trackTemp, float ambientTemp, bool isWet)
        {
            // Target temperature based on conditions
            float targetTemp = isWet 
                ? (compoundData.optimalTempMin + compoundData.optimalTempMax) / 2f * 0.7f
                : (compoundData.optimalTempMin + compoundData.optimalTempMax) / 2f;
            // Heat up from friction (simplified)
            float frictionHeat = 15f * deltaTime;
            // Cool down from air
            float cooling = (temperature - ambientTemp) * compoundData.coolingRate * deltaTime * 0.5f;
            // Apply changes
            temperature += frictionHeat * compoundData.heatCapacity - cooling;
            // Clamp to realistic range
            temperature = Mathf.Clamp(temperature, ambientTemp, 150f);
        }
        void UpdateWear(float deltaTime, bool isWet)
        {
            if (isWet)
            {
                // Less wear in wet conditions
                wearPercent += compoundData.wearRate * 0.3f * deltaTime;
            }
            else
            {
                // Normal wear
                wearPercent += compoundData.wearRate * deltaTime;
                // Extra wear if overheating
                if (temperature > compoundData.optimalTempMax)
                {
                    float overheatFactor = (temperature - compoundData.optimalTempMax) / 50f;
                    wearPercent += overheatFactor * deltaTime * 2f;
                }
            }
            // Increment laps (simplified - assume 1 lap per 60 seconds)
            lapsOnTire += Mathf.FloorToInt(deltaTime / 60f);
        }
        void CalculateGrip()
        {
            // Base grip from compound
            float grip = compoundData.baseGrip;
            // Temperature penalty (optimal window)
            float tempDiff = Mathf.Abs(temperature - (compoundData.optimalTempMin + compoundData.optimalTempMax) / 2f);
            float tempPenalty = Mathf.Clamp01(tempDiff / 40f);
            grip *= (1f - tempPenalty * 0.3f);
            // Wear penalty
            float wearPenalty = wearPercent / 100f;
            grip *= (1f - wearPenalty * 0.4f);
            // Degradation
            grip *= (1f - degradation);
            gripLevel = Mathf.Clamp01(grip);
        }
        /// <summary>
        /// Call when tire is changed in pit stop
        /// </summary>
        public void ChangeCompound(TireCompound newCompound)
        {
            currentCompound = newCompound;
            Initialize(newCompound);
        }
        /// <summary>
        /// Get tire performance as percentage (0-100)
        /// </summary>
        public float GetPerformance()
        {
            return gripLevel * 100f;
        }
    }
}
