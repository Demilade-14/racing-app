using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Race;
namespace RacingGame.Career
{
    /// <summary>
    /// Injects the player into the race simulation.
    /// Applies pressure-based mistakes, clutch moments, and performance variance.
    /// </summary>
    public static class PlayerInfluenceSystem
    {
        /// <summary>
        /// Apply player-specific effects to a race result.
        /// Call this during the simulation loop for the player's driver.
        /// </summary>
        public static void ApplyPlayerEffects(DriverResult result, PlayerCareerContext player, DriverData driverData)
        {
            // Only apply to the player
            if (result.driverName != player.playerDriverName) return;
            float pressure = player.pressureLevel;
            // 1. Performance variance based on pressure
            // High pressure = more inconsistent (can be good or bad)
            float inconsistency = Random.Range(-pressure * 5f, pressure * 5f);
            result.totalPerformance += inconsistency;
            // 2. Clutch moments (player advantage mechanic)
            // When title contender under high pressure, chance of heroic performance
            if (PlayerPressureSystem.ShouldHaveClutchMoment(pressure, player.isTitleContender))
            {
                result.totalPerformance *= 1.05f; // 5% performance boost
                UnityEngine.Debug.Log($"[PlayerInfluence] 🏆 {player.playerDriverName} has a CLUTCH MOMENT!");
            }
            // 3. Mistakes under pressure
            // High pressure can cause errors (DNF or poor performance)
            if (PlayerPressureSystem.ShouldMakeMistake(pressure))
            {
                // 50% chance of DNF, 50% chance of just poor performance
                if (Random.value < 0.5f)
                {
                    result.dnf = true;
                    result.dnfReason = "Driver Error (Pressure)";
                    UnityEngine.Debug.Log($"[PlayerInfluence] 💥 {player.playerDriverName} made a CRITICAL ERROR under pressure!");
                }
                else
                {
                    result.totalPerformance *= 0.90f; // 10% performance penalty
                    UnityEngine.Debug.Log($"[PlayerInfluence] ⚠️ {player.playerDriverName} struggled under pressure.");
                }
            }
            // 4. Late-race heroics (player gets slight advantage in final laps)
            // This simulates the "player character" advantage in games
            if (player.isTitleContender && pressure > 0.7f)
            {
                // Small chance of extra performance in critical moments
                if (Random.value < 0.10f)
                {
                    result.totalPerformance *= 1.03f;
                    UnityEngine.Debug.Log($"[PlayerInfluence] 🔥 {player.playerDriverName} pushes HARD in the final laps!");
                }
            }
        }
        /// <summary>
        /// Get player performance modifier for UI display
        /// </summary>
        public static string GetPerformanceModifier(PlayerCareerContext player)
        {
            if (player.pressureLevel < 0.3f) return "Relaxed";
            if (player.pressureLevel < 0.6f) return "Focused";
            if (player.pressureLevel < 0.8f) return "Under Pressure";
            return "CRITICAL";
        }
    }
}
