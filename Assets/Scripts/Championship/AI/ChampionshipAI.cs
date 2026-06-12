using UnityEngine;
using RacingGame.Drivers;
namespace RacingGame.Championship.AI
{
    /// <summary>
    /// Modifies driver behavior based on their position in the championship.
    /// This creates emergent gameplay: leaders become conservative, chasers become reckless.
    /// </summary>
    public static class ChampionshipAI
    {
        /// <summary>
        /// Calculates the pressure level (0.0 to 1.0) a driver is under.
        /// </summary>
        /// <param name="pointsGap">Points behind the leader (0 if leading)</param>
        /// <param name="racesRemaining">How many races are left in the season</param>
        public static float GetPressureLevel(int pointsGap, int racesRemaining)
        {
            if (racesRemaining <= 0) return 1.0f; // Maximum pressure at the final race
            // Max points per race is 25 (win) + 1 (FL) = 26. 
            // If gap is within 3 races worth of points, pressure is high.
            float mathematicallyPossible = racesRemaining * 26f;
            float pressureRatio = 1f - (pointsGap / mathematicallyPossible);
            return Mathf.Clamp01(pressureRatio);
        }
        /// <summary>
        /// Calculates a risk multiplier for the driver based on pressure and standing.
        /// </summary>
        public static float GetRiskFactor(float pressureLevel, bool isLeadingChampionship)
        {
            // Leaders drive safer (0.8x risk), chasers drive harder (1.2x risk)
            float baseRisk = isLeadingChampionship ? 0.85f : 1.15f;
            // Pressure amplifies this behavior
            return baseRisk + (pressureLevel * 0.3f); 
        }
        /// <summary>
        /// Returns a modified copy of the driver's stats to reflect championship pressure.
        /// Call this before simulating a race weekend.
        /// </summary>
        public static DriverData ApplyChampionshipBehavior(DriverData driver, float pressureLevel, bool isTitleContender)
        {
            // Create a shallow copy so we don't permanently alter the database mid-season
            DriverData modified = new DriverData
            {
                fullName = driver.fullName,
                shortName = driver.shortName,
                age = driver.age,
                nationality = driver.nationality,
                teamName = driver.teamName,
                rating = new DriverRating // Copy the rating object
                {
                    pace = driver.rating.pace,
                    wetSkill = driver.rating.wetSkill,
                    tireManagement = driver.rating.tireManagement,
                    overtaking = driver.rating.overtaking,
                    defending = driver.rating.defending,
                    consistency = driver.rating.consistency,
                    aggression = driver.rating.aggression
                }
            };
            if (!isTitleContender) return modified;
            // ── TITLE CONTENDER MODIFIERS ─────────────────────────────────────
            // High pressure makes consistency drop slightly (mistakes happen)
            float consistencyDrop = pressureLevel * 5f; 
            modified.rating.consistency = Mathf.Clamp(modified.rating.consistency - consistencyDrop, 1, 99);
            // High pressure makes aggression spike (desperation moves)
            float aggressionBoost = pressureLevel * 15f;
            modified.rating.aggression = Mathf.Clamp(modified.rating.aggression + aggressionBoost, 1, 99);
            // Overtaking becomes more desperate
            modified.rating.overtaking = Mathf.Clamp(modified.rating.overtaking + (pressureLevel * 5f), 1, 99);
            return modified;
        }
    }
}
