using UnityEngine;
using RacingGame.Race;
namespace RacingGame.Career
{
    /// <summary>
    /// Tracks the player's current state in the championship.
    /// Used by simulation systems to apply player-specific effects.
    /// </summary>
    [System.Serializable]
    public class PlayerCareerContext
    {
        public string playerDriverName;
        public string playerTeamName;
        [Header("Championship State")]
        public int currentChampionshipPosition;
        public int currentChampionshipPoints;
        public int leaderPoints;
        public int racesRemaining;
        [Header("Performance")]
        public float seasonPerformanceRating;  // 0-100, how well player is doing
        public bool isTitleContender;
        public float pressureLevel;            // 0-1, calculated from championship gap
        [Header("Race State")]
        public DriverState currentRaceState;   // Push, Defend, Conserve, Recover
        /// <summary>
        /// Update the context before each race weekend
        /// </summary>
        public void Update(int position, int points, int leaderPts, int racesLeft)
        {
            currentChampionshipPosition = position;
            currentChampionshipPoints = points;
            leaderPoints = leaderPts;
            racesRemaining = racesLeft;
            // Calculate if title contender (within 2 races' worth of points)
            int pointsGap = leaderPts - points;
            isTitleContender = pointsGap <= (racesLeft * 26);
            // Calculate pressure level
            pressureLevel = PlayerPressureSystem.CalculatePressure(points, leaderPts, racesLeft);
        }
        /// <summary>
        /// Check if player is leading the championship
        /// </summary>
        public bool IsLeadingChampionship()
        {
            return currentChampionshipPosition == 1;
        }
    }
}
