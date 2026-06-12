using UnityEngine;
namespace RacingGame.Drivers
{
    /// <summary>
    /// Driver skill attributes (1-100 scale)
    /// These determine AI behavior and performance in different conditions
    /// </summary>
    [System.Serializable]
    public class DriverRating
    {
        [Header("Core Performance")]
        [Range(1, 100)] public int pace = 80;
        [Range(1, 100)] public int consistency = 80;
        [Header("Technical Skills")]
        [Range(1, 100)] public int wetSkill = 80;
        [Range(1, 100)] public int tireManagement = 80;
        [Header("Race Craft")]
        [Range(1, 100)] public int overtaking = 80;
        [Range(1, 100)] public int defending = 80;
        [Header("Personality")]
        [Range(1, 100)] public int aggression = 50;
        /// <summary>
        /// Calculate overall rating from all attributes
        /// </summary>
        public float GetOverall()
        {
            return (pace + wetSkill + tireManagement + overtaking + defending + consistency) / 6f;
        }
        /// <summary>
        /// Get performance modifier for wet conditions
        /// </summary>
        public float GetWetPerformanceModifier()
        {
            return wetSkill / 100f;
        }
        /// <summary>
        /// Get tire wear reduction factor
        /// </summary>
        public float GetTireWearFactor()
        {
            return 1f - (tireManagement / 200f); // 0.5 to 1.0
        }
        /// <summary>
        /// Get overtaking success chance against defender
        /// </summary>
        public float GetOvertakeSuccessChance(DriverRating defender)
        {
            float attackPower = overtaking * 0.6f + aggression * 0.2f + pace * 0.2f;
            float defendPower = defender.defending * 0.7f + defender.consistency * 0.3f;
            float chance = 0.5f + ((attackPower - defendPower) / 200f);
            return Mathf.Clamp01(chance);
        }
        /// <summary>
        /// Get lap time variance (lower consistency = higher variance)
        /// </summary>
        public float GetLapTimeVariance()
        {
            return (100 - consistency) / 100f * 0.05f; // 0 to 0.05 seconds variance
        }
    }
}
