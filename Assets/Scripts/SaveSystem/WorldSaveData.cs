using System;
using System.Collections.Generic;
using RacingGame.Drivers;
using RacingGame.Transfers;
using RacingGame.Career;
using RacingGame.Teams.AI;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// Master container for the entire F1 world state.
    /// This is what gets serialized to JSON and saved to disk.
    /// </summary>
    [Serializable]
    public class WorldSaveData
    {
        [Header("Versioning")]
        public string saveVersion = "1.0.0";
        public string saveId = Guid.NewGuid().ToString("N");
        public DateTime saveTimestamp = DateTime.Now;
        public string saveSlotName;
        [Header("Season State")]
        public int currentSeason = 1;
        public int currentRound = 1;
        public CareerPhase currentPhase = CareerPhase.PreSeason;
        [Header("Driver Database")]
        public List<DriverData> allDrivers = new List<DriverData>();
        public int totalRetiredThisSeason;
        public int totalRegenThisSeason;
        [Header("Team Database")]
        public List<TransferTeamData> allTeams = new List<TransferTeamData>();
        [Header("Player Career")]
        public DriverCareerSave playerCareer; // Uses your existing DriverCareerSave class
        public PlayerCareerContext playerContext;
        [Header("Team AI States")]
        public List<TeamSaveData> teamAIStates = new List<TeamSaveData>();
        [Header("Championship")]
        public ChampionshipSaveData championship;
        [Header("Race Control (Optional)")]
        public RaceControlSaveData raceControl;
        [Header("Season History")]
        public List<SeasonRecord> seasonHistory = new List<SeasonRecord>();
        /// <summary>
        /// Validate the save data integrity
        /// </summary>
        public bool Validate()
        {
            if (allDrivers == null || allDrivers.Count == 0)
            {
                UnityEngine.Debug.LogError("[SaveSystem] Invalid save: No drivers found");
                return false;
            }
            if (allTeams == null || allTeams.Count == 0)
            {
                UnityEngine.Debug.LogError("[SaveSystem] Invalid save: No teams found");
                return false;
            }
            if (currentSeason < 1)
            {
                UnityEngine.Debug.LogError("[SaveSystem] Invalid save: Season must be >= 1");
                return false;
            }
            return true;
        }
    }
}
