using UnityEngine;
using System.Collections.Generic;
using RacingGame.Career;
namespace RacingGame.Career
{
    /// <summary>
    /// Simulates Qualifying sessions (Q1, Q2, Q3).
    /// Calculates lap times based on driver stats and car performance.
    /// </summary>
    public static class QualifyingManager
    {
        /// <summary>
        /// Simulate the entire qualifying session and return the player's grid position.
        /// </summary>
        public static int SimulateQualifying(DriverProfile profile, DriverRaceWeekend weekend, float carPerformanceRating)
        {
            weekend.qualiDone = true;
            // Calculate player's qualifying pace
            float playerPace = CalculateQualifyingPace(profile, carPerformanceRating);
            // Simulate 19 other drivers (or load from championship data)
            List<float> allPaces = new List<float> { playerPace };
            // Generate AI paces (simplified random distribution around 80-95)
            for (int i = 0; i < 19; i++)
            {
                float aiPace = Random.Range(75f, 98f);
                allPaces.Add(aiPace);
            }
            // Sort paces (highest is fastest)
            allPaces.Sort((a, b) => b.CompareTo(a));
            // Find player's position
            int gridPosition = allPaces.IndexOf(playerPace) + 1;
            weekend.gridPosition = gridPosition;
            weekend.qualiEliminated = gridPosition > 15 ? 1 : (gridPosition > 10 ? 2 : 3); // Q1, Q2, or Q3 exit
            Debug.Log($"[QualifyingManager] {profile.driverName} qualified P{gridPosition} at {weekend.circuitName}.");
            return gridPosition;
        }
        /// <summary>
        /// Calculate a driver's qualifying lap time score (higher is better).
        /// </summary>
        static float CalculateQualifyingPace(DriverProfile profile, float carPerformance)
        {
            // Qualifying relies heavily on Pace and Awareness
            float driverSkill = (profile.pace * 0.6f) + (profile.awareness * 0.3f) + (profile.racecraft * 0.1f);
            // Add car performance
            float totalPace = (driverSkill * 0.7f) + (carPerformance * 0.3f);
            // Add randomness (consistency factor)
            float consistency = profile.experience / 100f;
            float variance = Random.Range(0.95f, 1.05f);
            return totalPace * variance;
        }
        /// <summary>
        /// Get a formatted lap time string for UI display.
        /// </summary>
        public static string FormatLapTime(float lapTimeSeconds)
        {
            int minutes = Mathf.FloorToInt(lapTimeSeconds / 60f);
            float seconds = lapTimeSeconds % 60f;
            return $"{minutes}:{seconds:00.000}";
        }
    }
}
