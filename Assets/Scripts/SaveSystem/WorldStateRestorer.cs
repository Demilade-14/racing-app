using System.Collections.Generic;
using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Transfers;
using RacingGame.Teams.AI;
using RacingGame.Career;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// Restores live simulation state from saved data.
    /// Call this after loading to rebuild the world.
    /// </summary>
    public static class WorldStateRestorer
    {
        /// <summary>
        /// Restore the entire world from save data
        /// </summary>
        public static bool RestoreWorld(WorldSaveData data)
        {
            if (data == null || !data.Validate())
            {
                Debug.LogError("[Restorer] Invalid save data");
                return false;
            }
            try
            {
                // Restore drivers
                RestoreDrivers(data.allDrivers);
                // Restore teams
                RestoreTeams(data.allTeams);
                // Restore team AI states
                RestoreTeamAI(data.teamAIStates);
                // Restore championship
                RestoreChampionship(data.championship);
                Debug.Log($"✅ [Restorer] World restored successfully (Season {data.currentSeason})");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [Restorer] Restore failed: {e.Message}");
                return false;
            }
        }
        /// <summary>
        /// Restore driver database
        /// </summary>
        private static void RestoreDrivers(List<DriverData> drivers)
        {
            if (drivers == null) return;
            DriverDatabase.Drivers.Clear();
            DriverDatabase.Drivers.AddRange(drivers);
            Debug.Log($"[Restorer] Restored {drivers.Count} drivers");
        }
        /// <summary>
        /// Restore team database
        /// </summary>
        private static void RestoreTeams(List<TransferTeamData> teams)
        {
            if (teams == null) return;
            TransferTeamDatabase.Teams.Clear();
            TransferTeamDatabase.Teams.AddRange(teams);
            Debug.Log($"[Restorer] Restored {teams.Count} teams");
        }
        /// <summary>
        /// Restore team principal AI states
        /// </summary>
        private static void RestoreTeamAI(List<TeamSaveData> states)
        {
            if (states == null) return;
            var paddockManager = PaddockAIManager.Instance;
            if (paddockManager == null)
            {
                Debug.LogWarning("[Restorer] PaddockAIManager not found - creating new instance");
                var go = new GameObject("PaddockAIManager");
                paddockManager = go.AddComponent<PaddockAIManager>();
            }
            paddockManager.teams.Clear();
            foreach (var state in states)
            {
                var ai = new TeamPrincipalAI
                {
                    teamName = state.teamName,
                    teamBudget = state.budget,
                    performanceExpectation = state.performanceExpectation,
                    driver1Name = state.driver1Name,
                    driver2Name = state.driver2Name,
                    favoredDriver = state.favoredDriver,
                    personality = state.GetPersonality(),
                    performanceExpectation = state.performanceExpectation,
                    favoritismBias = state.favoritismBias,
                    isUnderPressure = state.isUnderPressure,
                    consecutiveBadResults = state.consecutiveBadResults,
                    currentSeasonPerformance = state.currentSeasonPerformance
                };
                paddockManager.teams.Add(ai);
            }
            Debug.Log($"[Restorer] Restored {states.Count} team AI states");
        }
        /// <summary>
        /// Restore championship standings
        /// </summary>
        private static void RestoreChampionship(ChampionshipSaveData data)
        {
            if (data == null) return;
            var championship = Object.FindObjectOfType<RacingGame.Championship.ChampionshipSystem>();
            if (championship == null)
            {
                Debug.LogWarning("[Restorer] ChampionshipSystem not found");
                return;
            }
            // Rebuild driver standings
            championship.driverStandings.Clear();
            foreach (var kvp in data.driverPoints)
            {
                var driver = DriverDatabase.GetDriverByName(kvp.Key);
                if (driver != null)
                {
                    var standing = new RacingGame.Championship.DriverStanding(driver)
                    {
                        points = kvp.Value,
                        wins = data.driverWins.ContainsKey(kvp.Key) ? data.driverWins[kvp.Key] : 0,
                        podiums = data.driverPodiums.ContainsKey(kvp.Key) ? data.driverPodiums[kvp.Key] : 0
                    };
                    championship.driverStandings.Add(standing);
                }
            }
            // Rebuild constructor standings
            championship.constructorStandings.Clear();
            foreach (var kvp in data.teamPoints)
            {
                var standing = new RacingGame.Championship.ConstructorStanding(kvp.Key)
                {
                    points = kvp.Value,
                    wins = data.teamWins.ContainsKey(kvp.Key) ? data.teamWins[kvp.Key] : 0
                };
                championship.constructorStandings.Add(standing);
            }
            // Sort standings
            championship.driverStandings.Sort((a, b) => b.points.CompareTo(a.points));
            championship.constructorStandings.Sort((a, b) => b.points.CompareTo(a.points));
            // Update positions
            for (int i = 0; i < championship.driverStandings.Count; i++)
                championship.driverStandings[i].position = i + 1;
            for (int i = 0; i < championship.constructorStandings.Count; i++)
                championship.constructorStandings[i].position = i + 1;
            Debug.Log($"[Restorer] Restored championship with {data.completedRaces} completed races");
        }
    }
}
