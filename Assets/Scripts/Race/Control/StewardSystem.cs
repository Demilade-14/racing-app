using UnityEngine;
using System.Collections.Generic;
using RacingGame.Race;
namespace RacingGame.Race.Control
{
    /// <summary>
    /// Simulates FIA Stewards handing out time penalties for aggressive driving or track limits.
    /// </summary>
    public class StewardSystem
    {
        public List<string> penaltiesLog = new List<string>();
        // Base chance for an aggressive driver to get a penalty
        public float penaltyChance = 0.15f;
        /// <summary>
        /// Evaluate if a driver receives a time penalty this lap.
        /// </summary>
        public void EvaluatePenalties(DriverResult result, float aggression)
        {
            if (result.dnf) return; // No point penalizing a DNF
            // Higher aggression = higher chance of track limits or collision penalties
            float risk = aggression * 0.01f;
            if (Random.value < risk * penaltyChance)
            {
                // 5s or 10s penalty
                float penaltyTime = Random.value < 0.7f ? 5f : 10f;
                // Apply penalty to total lap time
                result.lapTime += penaltyTime;
                string logEntry = $"{result.driverName} +{penaltyTime:F0}s Penalty (Track Limits/Aggression)";
                penaltiesLog.Add(logEntry);
                Debug.Log($"🧑‍⚖️ PENALTY: {logEntry}");
            }
        }
        /// <summary>
        /// Clear the log at the start of a new race.
        /// </summary>
        public void ClearLog()
        {
            penaltiesLog.Clear();
        }
    }
}
