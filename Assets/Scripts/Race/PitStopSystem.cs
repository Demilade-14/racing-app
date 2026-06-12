using UnityEngine;
using RacingGame.Race;
using RacingGame.Data;
namespace RacingGame.Race
{
    /// <summary>
    /// Manages pit stops during a race.
    /// Handles pit lane speed limits, stop duration, tyre changes, and unsafe releases.
    /// </summary>
    public static class PitStopSystem
    {
        // 2026 Regulations
        public const float PIT_LANE_SPEED_LIMIT = 80f; // km/h
        public const float BASE_PIT_STOP_TIME = 2.5f;  // seconds for tyre change
        /// <summary>
        /// Execute a pit stop for a driver.
        /// </summary>
        public static PitStopResult ExecutePitStop(
            string driverId, 
            TyreCompound2026 newCompound, 
            RaceDirector director,
            float crewSkill = 1.0f) // 1.0 = 100% skill
        {
            var result = new PitStopResult();
            result.driverId = driverId;
            result.requestedCompound = newCompound;
            // 1. Calculate pit stop duration based on crew skill
            float stopDuration = BASE_PIT_STOP_TIME / crewSkill;
            // 2. Check for unsafe release (simplified: 5% chance if rushing)
            bool unsafeRelease = Random.value < 0.05f;
            if (unsafeRelease)
            {
                stopDuration += 5f; // 5 second penalty
                result.isUnsafeRelease = true;
                result.penaltySeconds = 5f;
                director.IssuePenalty(driverId, PenaltyType.FiveSeconds, 5f);
                Debug.LogWarning($"[PitStopSystem] Unsafe release for {driverId}! +5s penalty.");
            }
            // 3. Register tyre change with RaceDirector
            director.RegisterTyreChange(driverId, newCompound);
            // 4. Update result
            result.stopDuration = stopDuration;
            result.success = true;
            Debug.Log($"[PitStopSystem] {driverId} pitted for {newCompound}. Stop time: {stopDuration:F2}s.");
            return result;
        }
        /// <summary>
        /// Check if a driver is exceeding the pit lane speed limit.
        /// </summary>
        public static bool CheckPitLaneSpeed(float currentSpeedKmh)
        {
            return currentSpeedKmh > PIT_LANE_SPEED_LIMIT;
        }
        /// <summary>
        /// Calculate time lost in pit lane (entry + stop + exit).
        /// </summary>
        public static float CalculateTotalPitLaneTime(float stopDuration, float pitLaneLengthKm = 0.5f)
        {
            // Time = Distance / Speed
            float entryExitTime = (pitLaneLengthKm / (PIT_LANE_SPEED_LIMIT / 3600f)); // Convert km/h to km/s
            return entryExitTime + stopDuration;
        }
    }
    /// <summary>
    /// Result data for a pit stop.
    /// </summary>
    [System.Serializable]
    public class PitStopResult
    {
        public string driverId;
        public TyreCompound2026 requestedCompound;
        public float stopDuration;
        public bool isUnsafeRelease;
        public float penaltySeconds;
        public bool success;
    }
}
