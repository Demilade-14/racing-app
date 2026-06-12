using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Track;
namespace RacingGame.Championship
{
    /// <summary>
    /// Driver standing entry
    /// </summary>
    [Serializable]
    public class DriverStanding
    {
        public DriverData driver;
        public int points;
        public int wins;
        public int podiums;
        public int poles;
        public int fastestLaps;
        public int position;
        public DriverStanding(DriverData d)
        {
            driver = d;
            points = 0;
            wins = 0;
            podiums = 0;
            poles = 0;
            fastestLaps = 0;
        }
    }
    /// <summary>
    /// Constructor (team) standing entry
    /// </summary>
    [Serializable]
    public class ConstructorStanding
    {
        public string teamName;
        public int points;
        public int wins;
        public int podiums;
        public int position;
        public ConstructorStanding(string team)
        {
            teamName = team;
            points = 0;
            wins = 0;
            podiums = 0;
        }
    }
    /// <summary>
    /// Race result data
    /// </summary>
    [Serializable]
    public class RaceResult
    {
        public string circuitName;
        public int roundNumber;
        public List<DriverData> finishingOrder = new List<DriverData>();
        public string fastestLapDriver;
        public DateTime raceDate;
    }
    /// <summary>
    /// Main championship system - handles standings, points, and season progression
    /// </summary>
    public class ChampionshipSystem : MonoBehaviour
    {
        public static ChampionshipSystem Instance;
        [Header("Championship State")]
        public int currentRound = 1;
        public int currentSeason = 2026;
        [Header("Standings")]
        public List<DriverStanding> driverStandings = new List<DriverStanding>();
        public List<ConstructorStanding> constructorStandings = new List<ConstructorStanding>();
        public List<RaceResult> completedRaces = new List<RaceResult>();
        [Header("Points Tables")]
        private readonly int[] racePoints = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
        private readonly int[] sprintPoints = { 8, 7, 6, 5, 4, 3, 2, 1 };
        private readonly int fastestLapBonus = 1; // Extra point for fastest lap (if in top 10)
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        void Start()
        {
            InitializeChampionship();
        }
        /// <summary>
        /// Initialize championship with all drivers and teams
        /// </summary>
        public void InitializeChampionship()
        {
            driverStandings.Clear();
            constructorStandings.Clear();
            completedRaces.Clear();
            currentRound = 1;
            // Initialize driver standings
            foreach (var driver in DriverDatabase.Drivers)
            {
                driverStandings.Add(new DriverStanding(driver));
            }
            // Initialize constructor standings (get unique teams)
            var teams = DriverDatabase.Drivers.Select(d => d.teamName).Distinct();
            foreach (var team in teams)
            {
                constructorStandings.Add(new ConstructorStanding(team));
            }
            Debug.Log($"[Championship] Initialized {currentSeason} season with {driverStandings.Count} drivers and {constructorStandings.Count} teams");
        }
        /// <summary>
        /// Apply race results and update standings
        /// </summary>
        public void ApplyRaceResult(string circuitName, List<DriverData> finishOrder, string fastestLapDriver = null)
        {
            RaceResult result = new RaceResult
            {
                circuitName = circuitName,
                roundNumber = currentRound,
                finishingOrder = finishOrder,
                fastestLapDriver = fastestLapDriver,
                raceDate = DateTime.Now
            };
            completedRaces.Add(result);
            // Award points
            for (int i = 0; i < finishOrder.Count; i++)
            {
                DriverStanding standing = GetDriverStanding(finishOrder[i]);
                if (standing == null) continue;
                // Race points
                if (i < racePoints.Length)
                {
                    standing.points += racePoints[i];
                    // Also add to constructor
                    AddConstructorPoints(finishOrder[i].teamName, racePoints[i]);
                }
                // Win
                if (i == 0)
                {
                    standing.wins++;
                    AddConstructorWin(finishOrder[i].teamName);
                }
                // Podium
                if (i <= 2)
                {
                    standing.podiums++;
                    AddConstructorPodium(finishOrder[i].teamName);
                }
            }
            // Fastest lap bonus point
            if (!string.IsNullOrEmpty(fastestLapDriver))
            {
                var flDriver = DriverDatabase.GetDriverByName(fastestLapDriver);
                if (flDriver != null)
                {
                    var flStanding = GetDriverStanding(flDriver);
                    if (flStanding != null)
                    {
                        // Only award if finished in top 10
                        int position = finishOrder.IndexOf(flDriver);
                        if (position >= 0 && position < 10)
                        {
                            flStanding.points += fastestLapBonus;
                            flStanding.fastestLaps++;
                            AddConstructorPoints(flDriver.teamName, fastestLapBonus);
                        }
                    }
                }
            }
            SortStandings();
            currentRound++;
            Debug.Log($"[Championship] Race Complete: {circuitName} (Round {result.roundNumber})");
        }
        /// <summary>
        /// Apply sprint race results
        /// </summary>
        public void ApplySprintResult(List<DriverData> finishOrder)
        {
            for (int i = 0; i < finishOrder.Count; i++)
            {
                DriverStanding standing = GetDriverStanding(finishOrder[i]);
                if (standing == null) continue;
                if (i < sprintPoints.Length)
                {
                    standing.points += sprintPoints[i];
                    AddConstructorPoints(finishOrder[i].teamName, sprintPoints[i]);
                }
            }
            SortStandings();
        }
        /// <summary>
        /// Apply qualifying results (for pole position tracking)
        /// </summary>
        public void ApplyQualifyingResult(DriverData poleSitter)
        {
            var standing = GetDriverStanding(poleSitter);
            if (standing != null)
            {
                standing.poles++;
            }
        }
        /// <summary>
        /// Get driver standing by driver data
        /// </summary>
        DriverStanding GetDriverStanding(DriverData driver)
        {
            return driverStandings.Find(s => s.driver == driver);
        }
        /// <summary>
        /// Add points to constructor
        /// </summary>
        void AddConstructorPoints(string teamName, int points)
        {
            var constructor = constructorStandings.Find(c => c.teamName == teamName);
            if (constructor != null)
            {
                constructor.points += points;
            }
        }
        /// <summary>
        /// Add win to constructor
        /// </summary>
        void AddConstructorWin(string teamName)
        {
            var constructor = constructorStandings.Find(c => c.teamName == teamName);
            if (constructor != null)
            {
                constructor.wins++;
            }
        }
        /// <summary>
        /// Add podium to constructor
        /// </summary>
        void AddConstructorPodium(string teamName)
        {
            var constructor = constructorStandings.Find(c => c.teamName == teamName);
            if (constructor != null)
            {
                constructor.podiums++;
            }
        }
        /// <summary>
        /// Sort all standings
        /// </summary>
        void SortStandings()
        {
            // Sort driver standings
            driverStandings = driverStandings
                .OrderByDescending(s => s.points)
                .ThenByDescending(s => s.wins)
                .ThenByDescending(s => s.podiums)
                .ToList();
            for (int i = 0; i < driverStandings.Count; i++)
            {
                driverStandings[i].position = i + 1;
            }
            // Sort constructor standings
            constructorStandings = constructorStandings
                .OrderByDescending(c => c.points)
                .ThenByDescending(c => c.wins)
                .ToList();
            for (int i = 0; i < constructorStandings.Count; i++)
            {
                constructorStandings[i].position = i + 1;
            }
        }
        /// <summary>
        /// Get championship leader
        /// </summary>
        public DriverStanding GetLeader()
        {
            return driverStandings.Count > 0 ? driverStandings[0] : null;
        }
        /// <summary>
        /// Get constructor champion
        /// </summary>
        public ConstructorStanding GetConstructorLeader()
        {
            return constructorStandings.Count > 0 ? constructorStandings[0] : null;
        }
        /// <summary>
        /// Get points gap between two drivers
        /// </summary>
        public int GetPointsGap(DriverData driver1, DriverData driver2)
        {
            var standing1 = GetDriverStanding(driver1);
            var standing2 = GetDriverStanding(driver2);
            if (standing1 == null || standing2 == null) return 0;
            return standing1.points - standing2.points;
        }
        /// <summary>
        /// Check if season is complete
        /// </summary>
        public bool IsSeasonComplete(int totalRounds)
        {
            return currentRound > totalRounds;
        }
        /// <summary>
        /// Print current standings to console
        /// </summary>
        public void PrintStandings()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"  {currentSeason} DRIVER STANDINGS (Round {currentRound - 1})");
            Debug.Log("═══════════════════════════════════════");
            for (int i = 0; i < Mathf.Min(10, driverStandings.Count); i++)
            {
                var s = driverStandings[i];
                Debug.Log($"{s.position,2}. {s.driver.fullName,-25} {s.points,4} pts  ({s.wins}W, {s.podiums}P)");
            }
            Debug.Log("\n═══════════════════════════════════════");
            Debug.Log("  CONSTRUCTOR STANDINGS");
            Debug.Log("═══════════════════════════════════════");
            for (int i = 0; i < constructorStandings.Count; i++)
            {
                var c = constructorStandings[i];
                Debug.Log($"{c.position,2}. {c.teamName,-25} {c.points,4} pts  ({c.wins}W)");
            }
        }
        /// <summary>
        /// Reset championship for new season
        /// </summary>
        public void ResetForNewSeason()
        {
            currentSeason++;
            InitializeChampionship();
            Debug.Log($"[Championship] Started new season: {currentSeason}");
        }
    }
}
