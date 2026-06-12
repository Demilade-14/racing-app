using UnityEngine;
using System;
namespace RacingGame.Driver
{
    /// <summary>
    /// Driver skill attributes (0-99 scale)
    /// Affects both AI behavior and player performance
    /// </summary>
    [System.Serializable]
    public class DriverSkills
    {
        [Header("Core Skills")]
        [Range(0, 99)] public int braking;
        [Range(0, 99)] public int racecraft;
        [Range(0, 99)] public int cornering;
        [Range(0, 99)] public int consistency;
        [Header("Advanced Skills")]
        [Range(0, 99)] public int wetSkill;
        [Range(0, 99)] public int tireSaving;
        [Range(0, 99)] public int overtaking;
        [Range(0, 99)] public int defending;
        [Header("Derived Stats")]
        public int overallRating;
        /// <summary>
        /// Calculate overall rating from all skills
        /// </summary>
        public void CalculateOverallRating()
        {
            overallRating = Mathf.RoundToInt(
                (braking * 0.15f) +
                (racecraft * 0.20f) +
                (cornering * 0.15f) +
                (consistency * 0.15f) +
                (wetSkill * 0.10f) +
                (tireSaving * 0.10f) +
                (overtaking * 0.10f) +
                (defending * 0.05f)
            );
        }
        /// <summary>
        /// Get skill multiplier for performance calculations (0.5 - 1.5)
        /// </summary>
        public float GetSkillMultiplier(int skill)
        {
            return 0.5f + (skill / 99f);
        }
    }
    /// <summary>
    /// Manages driver skill calculations and AI behavior
    /// </summary>
    public static class DriverSkillCalculator
    {
        /// <summary>
        /// Calculate lap time modifier based on driver skills
        /// Returns multiplier: 1.0 = average, <1.0 = faster, >1.0 = slower
        /// </summary>
        public static float CalculateLapTimeModifier(DriverSkills skills, bool isWetCondition)
        {
            float modifier = 1.0f;
            // Cornering affects base pace
            modifier -= (skills.cornering - 50) * 0.002f;
            // Braking affects late braking zones
            modifier -= (skills.braking - 50) * 0.001f;
            // Consistency reduces lap time variance
            float consistencyFactor = 1.0f - (skills.consistency / 200f);
            modifier *= consistencyFactor;
            // Wet skill only matters in wet conditions
            if (isWetCondition)
            {
                modifier -= (skills.wetSkill - 50) * 0.003f;
            }
            return Mathf.Clamp(modifier, 0.85f, 1.15f);
        }
        /// <summary>
        /// Calculate tire wear reduction based on tire saving skill
        /// </summary>
        public static float CalculateTireWearReduction(DriverSkills skills)
        {
            // Higher tire saving = less wear (0.7 to 1.0 multiplier)
            return 1.0f - (skills.tireSaving * 0.003f);
        }
        /// <summary>
        /// Calculate overtaking success chance (0-1)
        /// </summary>
        public static float CalculateOvertakeSuccess(DriverSkills attacker, DriverSkills defender)
        {
            float attackSkill = attacker.overtaking * 0.6f + attacker.racecraft * 0.4f;
            float defendSkill = defender.defending * 0.7f + defender.racecraft * 0.3f;
            float chance = 0.5f + ((attackSkill - defendSkill) / 200f);
            return Mathf.Clamp01(chance);
        }
        /// <summary>
        /// Calculate defending success chance (0-1)
        /// </summary>
        public static float CalculateDefendSuccess(DriverSkills defender, DriverSkills attacker)
        {
            return 1.0f - CalculateOvertakeSuccess(attacker, defender);
        }
        /// <summary>
        /// Calculate AI aggression level based on skills
        /// </summary>
        public static float CalculateAIAggression(DriverSkills skills)
        {
            return (skills.overtaking * 0.4f + skills.racecraft * 0.3f + skills.braking * 0.3f) / 99f;
        }
        /// <summary>
        /// Calculate AI consistency (lap time variance)
        /// </summary>
        public static float CalculateAIConsistency(DriverSkills skills)
        {
            // Higher consistency = smaller variance (0.95 to 1.05)
            return 1.0f + ((50 - skills.consistency) * 0.001f);
        }
    }
}
