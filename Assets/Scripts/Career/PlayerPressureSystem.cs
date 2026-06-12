using UnityEngine;
namespace RacingGame.Career
{
    /// <summary>
    /// Calculates the pressure level on the player based on championship position.
    /// Used to trigger mistakes under pressure and clutch moments.
    /// </summary>
    public static class PlayerPressureSystem
    {
        /// <summary>
        /// Calculate pressure level (0.0 to 1.0)
        /// 0.0 = no pressure (leading comfortably)
        /// 1.0 = maximum pressure (final race, must win)
        /// </summary>
        public static float CalculatePressure(int playerPoints, int leaderPoints, int racesRemaining)
        {
            if (racesRemaining <= 0) return 1.0f;
            int gap = leaderPoints - playerPoints;
            // If player is leading, lower pressure
            if (gap <= 0)
            {
                // But still some pressure if lead is small
                float leadPressure = Mathf.InverseLerp(50f, 0f, -gap); // 50pt lead = 0 pressure
                return leadPressure * 0.3f; // Max 0.3 pressure when leading
            }
            // If chasing, pressure increases as gap grows relative to races remaining
            float maxPossiblePoints = racesRemaining * 26f; // 25 for win + 1 for fastest lap
            float pressureRatio = gap / maxPossiblePoints;
            // More races remaining = less immediate pressure
            float timeFactor = Mathf.InverseLerp(10f, 1f, racesRemaining);
            float pressure = pressureRatio * 0.6f + timeFactor * 0.4f;
            return Mathf.Clamp01(pressure);
        }
        /// <summary>
        /// Get pressure description for UI
        /// </summary>
        public static string GetPressureDescription(float pressure)
        {
            if (pressure < 0.2f) return "Low Pressure";
            if (pressure < 0.4f) return "Moderate Pressure";
            if (pressure < 0.6f) return "High Pressure";
            if (pressure < 0.8f) return "Extreme Pressure";
            return "CRITICAL PRESSURE";
        }
        /// <summary>
        /// Check if player should make a mistake due to pressure
        /// </summary>
        public static bool ShouldMakeMistake(float pressure)
        {
            if (pressure < 0.5f) return false;
            // Higher pressure = higher chance of mistake
            float mistakeChance = (pressure - 0.5f) * 0.2f; // 0-10% chance
            return Random.value < mistakeChance;
        }
        /// <summary>
        /// Check if player should have a "clutch moment" (heroic performance under pressure)
        /// </summary>
        public static bool ShouldHaveClutchMoment(float pressure, bool isTitleContender)
        {
            if (!isTitleContender) return false;
            if (pressure < 0.6f) return false;
            // 15% chance of clutch moment when title contender under high pressure
            return Random.value < 0.15f;
        }
    }
}
