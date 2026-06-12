using System.Collections.Generic;
using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Transfers;
using RacingGame.Teams.AI;
using RacingGame.Career;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// Converts live simulation state into saveable data structures.
    /// Call this before saving to capture the current world state.
    /// </summary>
    public static class WorldStateSerializer
    {
        /// <summary>
        /// Capture the entire world state into a saveable format
        /// </summary>
        public static WorldSaveData CaptureWorld(
            int currentSeason,
            int currentRound,
            CareerPhase phase,
            DriverCareerSave playerCareer,
            PlayerCareerContext playerContext)
        {
            WorldSaveData data = new WorldSaveData
            {
                currentSeason = currentSeason,
                currentRound = currentRound,
                currentPhase = phase,
                playerCareer = playerCareer,
                playerContext = playerContext
            };
            // Capture all drivers
            data.allDrivers = new List<DriverData>(DriverDatabase.Drivers);
            // Capture all teams
            data.allTeams = new List<TransferTeamData>(TransferTeamDatabase.Teams);
            // Capture team AI states
            data.teamAIStates = CaptureTeamAIStates();
            // Capture championship
            data.championship = CaptureChampionship();
            // Capture season history (from player career)
            if (playerCareer != null && playerCareer.profile != null)
            {
                data.seasonHistory = new List<SeasonRecord>(playerCareer.profile.seasonHistory);
            }
            return data;
        }
        /// <summary>
        /// Capture all team principal AI states
        /// </summary>
        private static List<TeamSaveData> CaptureTeamAIStates()
        {
            var states = new List<TeamSaveData>();
            var paddockManager = PaddockAIManager.Instance;
            if (paddockManager == null)
            {
                Debug.LogWarning("[Serializer] PaddockAIManager not found");
                return states;
            }
            foreach (var team in paddockManager.teams)
            {
                states.Add(new TeamSaveData
                {
                    teamName = team.teamName,
                    budget = team.teamBudget,
                    carPerformance = team.performanceExpectation,
                    driver1Name = team.driver1Name,
                    driver2Name = team.driver2Name,
                    favoredDriver = team.favoredDriver,
                    personality = team.personality.ToString(),
                    performanceExpectation = team.performanceExpectation,
                    favoritismBias = team.favoritismBias,
                    isUnderPressure = team.isUnderPressure,
                    consecutiveBadResults = team.consecutiveBadResults,
                    currentSeasonPerformance = team.currentSeasonPerformance
                });
            }
            return states;
        }
        /// <summary>
        /// Capture championship standings
        /// </summary>
        private static ChampionshipSaveData CaptureChampionship()
        {
            var data = new ChampionshipSaveData();
            // Get from ChampionshipSystem if available
            var championship = Object.FindObjectOfType<RacingGame.Championship.ChampionshipSystem>();
            if (championship == null) return data;
            foreach (var standing in championship.driverStandings)
            {
                if (standing.driver != null)
                {
                    data.driverPoints[standing.driver.fullName] = standing.points;
                    data.driverWins[standing.driver.fullName] = standing.wins;
                    data.driverPodiums[standing.driver.fullName] = standing.podiums;
                }
            }
            foreach (var standing in championship.constructorStandings)
            {
                data.teamPoints[standing.teamName] = standing.points;
                data.teamWins[standing.teamName] = standing.wins;
            }
            data.completedRaces = championship.completedRaces.Count;
            data.totalRacesInSeason = 24; // F1 2026 has 24 rounds
            return data;
        }
    }
}
