using UnityEngine;
namespace RacingGame.Tires
{
    /// <summary>
    /// Defines properties for each tire compound (2026 Pirelli regulations)
    /// </summary>
    [System.Serializable]
    public class TireCompoundData
    {
        public RacingGame.Core.TireCompound compound;
        [Header("Performance")]
        [Range(0f, 1f)] public float baseGrip;           // 0-1 grip multiplier
        [Range(0f, 2f)] public float wearRate;           // Degradation per lap
        public float optimalTempMin;                     // Celsius
        public float optimalTempMax;
        [Header("Thermal Properties")]
        public float heatCapacity;                       // How fast it heats up
        public float coolingRate;                        // How fast it cools
        [Header("Degradation")]
        public float degradationRateSoft;                // Grip loss over time
        public float degradationRateMedium;
        public float degradationRateHard;
        public static TireCompoundData GetCompound(RacingGame.Core.TireCompound compound)
        {
            return compound switch
            {
                RacingGame.Core.TireCompound.Soft => new TireCompoundData
                {
                    compound = RacingGame.Core.TireCompound.Soft,
                    baseGrip = 1.0f,
                    wearRate = 1.5f,
                    optimalTempMin = 90f,
                    optimalTempMax = 110f,
                    heatCapacity = 1.2f,
                    coolingRate = 0.8f,
                    degradationRateSoft = 0.02f
                },
                RacingGame.Core.TireCompound.Medium => new TireCompoundData
                {
                    compound = RacingGame.Core.TireCompound.Medium,
                    baseGrip = 0.92f,
                    wearRate = 1.0f,
                    optimalTempMin = 80f,
                    optimalTempMax = 100f,
                    heatCapacity = 1.0f,
                    coolingRate = 1.0f,
                    degradationRateMedium = 0.015f
                },
                RacingGame.Core.TireCompound.Hard => new TireCompoundData
                {
                    compound = RacingGame.Core.TireCompound.Hard,
                    baseGrip = 0.85f,
                    wearRate = 0.7f,
                    optimalTempMin = 70f,
                    optimalTempMax = 90f,
                    heatCapacity = 0.9f,
                    coolingRate = 1.1f,
                    degradationRateHard = 0.01f
                },
                RacingGame.Core.TireCompound.Inter => new TireCompoundData
                {
                    compound = RacingGame.Core.TireCompound.Inter,
                    baseGrip = 0.88f,
                    wearRate = 0.5f,
                    optimalTempMin = 40f,
                    optimalTempMax = 60f,
                    heatCapacity = 0.7f,
                    coolingRate = 1.3f,
                    degradationRateMedium = 0.008f
                },
                RacingGame.Core.TireCompound.Wet => new TireCompoundData
                {
                    compound = RacingGame.Core.TireCompound.Wet,
                    baseGrip = 0.85f,
                    wearRate = 0.3f,
                    optimalTempMin = 30f,
                    optimalTempMax = 50f,
                    heatCapacity = 0.6f,
                    coolingRate = 1.4f,
                    degradationRateHard = 0.005f
                },
                _ => new TireCompoundData()
            };
        }
    }
}
