using UnityEngine;
using System;
namespace RacingGame.Career
{
    /// <summary>
    /// Calculates advanced driver attributes based on the 4 core stats.
    /// This keeps save files small while providing deep simulation logic.
    /// </summary>
    public static class DriverSkillCalculator
    {
        /// <summary>
        /// Calculates Wet Weather Skill (0-99).
        /// Heavily relies on Awareness (reading the track) and Experience.
        /// </summary>
        public static int GetWetWeatherSkill(DriverProfile profile)
        {
            if (profile == null) return 50;
            float skill = (profile.awareness * 0.6f) + (profile.experience * 0.4f);
            // Add a small random variance based on driver ID so it's consistent per driver
            // In a full game, this might be a fixed trait, but this works for generated drivers.
            return Mathf.Clamp(Mathf.RoundToInt(skill), 0, 99);
        }
        /// <summary>
        /// Calculates Tire Management Skill (0-99).
        /// Combines the raw 'tyreManagementSkill' field with Racecraft.
        /// </summary>
        public static int GetTireManagement(DriverProfile profile)
        {
            if (profile == null) return 50;
            // Blend the specific skill field with racecraft
            float skill = (profile.tyreManagementSkill * 0.7f) + (profile.racecraft * 0.3f);
            return Mathf.Clamp(Mathf.RoundToInt(skill), 0, 99);
        }
        /// <summary>
        /// Calculates Aggression (0-99).
        /// High aggression = better overtaking, higher crash risk.
        /// Derived from Racecraft and a "personality" hash of the name.
        /// </summary>
        public static int GetAggression(DriverProfile profile)
        {
            if (profile == null) return 50;
            float baseAggression = profile.racecraft;
            // Add personality factor based on name length/characters (deterministic)
            int nameHash = profile.driverName != null ? profile.driverName.GetHashCode() : 0;
            float personalityFactor = (Math.Abs(nameHash) % 40) - 20; // -20 to +20
            return Mathf.Clamp(Mathf.RoundToInt(baseAggression + personalityFactor), 0, 99);
        }
        /// <summary>
        /// Calculates Consistency (0-99).
        /// High consistency = smaller lap time variance.
        /// Derived from Experience and Awareness.
        /// </summary>
        public static int GetConsistency(DriverProfile profile)
        {
            if (profile == null) return 50;
            float skill = (profile.experience * 0.5f) + (profile.awareness * 0.5f);
            return Mathf.Clamp(Mathf.RoundToInt(skill), 0, 99);
        }
        /// <summary>
        /// Calculates Qualifying Pace vs Race Pace.
        /// Returns a multiplier (e.g., 1.05 means 5% faster in Quali than Race).
        /// </summary>
        public static float GetQualifyingBias(DriverProfile profile)
        {
            if (profile == null) return 1.0f;
            // High pace + low experience = great qualifier, poor racer
            // High experience = consistent racer
            float paceFactor = profile.pace / 99f;
            float expFactor = profile.experience / 99f;
            // Bias ranges from 1.02 (2% faster in quali) to 1.08 (8% faster)
            float bias = 1.02f + (paceFactor * 0.04f) - (expFactor * 0.02f);
            return Mathf.Clamp(bias, 1.0f, 1.1f);
        }
        /// <summary>
        /// Calculates Overtaking Success Chance (0-1).
        /// Used by AI and Player during race logic.
        /// </summary>
        public static float GetOvertakeSuccessChance(DriverProfile attacker, DriverProfile defender)
        {
            if (attacker == null || defender == null) return 0.5f;
            int attackSkill = GetAggression(attacker) + (attacker.racecraft / 2);
            int defendSkill = (defender.awareness) + (defender.racecraft / 2);
            float chance = 0.5f + ((attackSkill - defendSkill) / 200f);
            return Mathf.Clamp01(chance);
        }
    }
}
