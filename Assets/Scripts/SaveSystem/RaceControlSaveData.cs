using System;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// Preserves race control state for mid-session saves (optional)
    /// </summary>
    [Serializable]
    public class RaceControlSaveData
    {
        public bool safetyCarActive;
        public bool virtualSafetyCarActive;
        public int safetyCarLapsRemaining;
        public int vscLapsRemaining;
        public int currentLap;
        public int totalLaps;
        public float trackRubberLevel;
        public bool isWetRace;
        /// <summary>
        /// Check if race is in progress
        /// </summary>
        public bool IsRaceInProgress()
        {
            return currentLap > 0 && currentLap < totalLaps;
        }
    }
}
