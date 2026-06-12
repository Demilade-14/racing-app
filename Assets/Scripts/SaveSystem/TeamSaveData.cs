using System;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// Preserves team principal AI state between sessions
    /// </summary>
    [Serializable]
    public class TeamSaveData
    {
        public string teamName;
        public float budget;
        public float carPerformance;
        public string driver1Name;
        public string driver2Name;
        public string favoredDriver;
        public string personality; // Serialized enum
        public float performanceExpectation;
        public float favoritismBias;
        public bool isUnderPressure;
        public int consecutiveBadResults;
        public float currentSeasonPerformance;
        /// <summary>
        /// Get personality as enum
        /// </summary>
        public RacingGame.Teams.AI.TeamPersonalityType GetPersonality()
        {
            if (System.Enum.TryParse(personality, out RacingGame.Teams.AI.TeamPersonalityType result))
                return result;
            return RacingGame.Teams.AI.TeamPersonalityType.Balanced;
        }
    }
}
