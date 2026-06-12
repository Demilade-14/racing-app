using UnityEngine;
using RacingGame.Track;
namespace RacingGame.Drivers
{
    /// <summary>
    /// Calculates driver performance based on skills, conditions, and track
    /// </summary>
    public static class DriverPerformanceCalculator
    {
        /// <summary>
        /// Calculate race pace for a driver
        /// </summary>
        public static float CalculateRacePace(DriverData driver, bool wetRace, float tireLife)
        {
            float pace = driver.rating.pace;
            // Wet weather bonus/penalty
            if (wetRace)
            {
                float wetModifier = driver.rating.wetSkill / 100f;
                pace *= (0.85f + wetModifier * 0.30f); // 85% to 115% based on wet skill
            }
            // Consistency bonus (reduces variance)
            pace += driver.rating.consistency * 0.1f;
            // Tire management affects pace as tires degrade
            float tireFactor = driver.rating.tireManagement / 100f;
            pace += tireFactor * (tireLife / 100f) * 5f;
            return pace;
        }
        /// <summary>
        /// Calculate qualifying pace
        /// </summary>
        public static float CalculateQualifyingPace(DriverData driver, bool wetQualifying)
        {
            float pace = driver.rating.pace * 1.02f; // Quali is slightly faster than race
            if (wetQualifying)
            {
                float wetModifier = driver.rating.wetSkill / 100f;
                pace *= (0.88f + wetModifier * 0.24f);
            }
            // Consistency matters less in quali (one lap)
            pace += driver.rating.consistency * 0.05f;
            return pace;
        }
        /// <summary>
        /// Calculate tire wear rate for a driver
        /// </summary>
        public static float CalculateTireWearRate(DriverData driver, CircuitProfile circuit)
        {
            float baseWear = circuit.tyreWearFactor;
            float driverFactor = 1f - (driver.rating.tireManagement / 200f); // 0.5 to 1.0
            return baseWear * driverFactor;
        }
        /// <summary>
        /// Calculate overtaking success chance
        /// </summary>
        public static float CalculateOvertakeSuccess(DriverData attacker, DriverData defender, float speedDifference)
        {
            float baseChance = attacker.rating.GetOvertakeSuccessChance(defender.rating);
            // Speed difference modifier
            float speedModifier = Mathf.Clamp(speedDifference / 20f, -0.2f, 0.3f);
            return Mathf.Clamp01(baseChance + speedModifier);
        }
        /// <summary>
        /// Calculate defending success chance
        /// </summary>
        public static float CalculateDefendSuccess(DriverData defender, DriverData attacker)
        {
            return 1f - CalculateOvertakeSuccess(attacker, defender, 0f);
        }
        /// <summary>
        /// Calculate lap time variance based on consistency
        /// </summary>
        public static float GetLapTimeVariance(DriverData driver)
        {
            return driver.rating.GetLapTimeVariance();
        }
        /// <summary>
        /// Get driver's preferred racing line aggression
        /// </summary>
        public static float GetAggressionLevel(DriverData driver)
        {
            return driver.rating.aggression / 100f;
        }
    }
}
