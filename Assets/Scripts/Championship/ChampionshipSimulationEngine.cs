using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Driver;
using RacingGame.Track;
namespace RacingGame.Championship
{
    /// <summary>
    /// AI Driver entry for championship simulation
    /// </summary>
    [System.Serializable]
    public class AIDriverEntry
    {
        public string driverName;
        public string teamName;
        public string driverCode;
        public DriverSkills skills;
        public int carPerformance; // 0-100
        // Championship stats
        public int points;
        public int wins;
        public int podiums;
        public int poles;
        public int fastestLaps;
        public int position;
        // Race results (last 5 races)
        public List<int> recentResults = new List<int>();
    }
    /// <summary>
    /// Simulates full championship with AI drivers
    /// </summary>
    public static class ChampionshipSimulationEngine
    {
        private static List<AIDriverEntry> _aiDrivers;
        /// <summary>
        /// Initialize championship with AI drivers
        /// </summary>
        public static void InitializeChampionship()
        {
            _aiDrivers = CreateAIDrivers();
            Debug.Log($"[ChampionshipSim] Initialized with {_aiDrivers.Count} AI drivers");
        }
        /// <summary>
        /// Simulate a complete race weekend for AI drivers
        /// </summary>
        public static List<AIDriverEntry> SimulateRaceWeekend(int roundNumber, bool isSprintWeekend)
        {
            if (_aiDrivers == null) InitializeChampionship();
            var circuit = TrackDatabase.GetCircuitByRound(roundNumber);
            if (circuit == null)
            {
                Debug.LogError($"[ChampionshipSim] No circuit found for round {roundNumber}");
                return _aiDrivers;
            }
            // Simulate qualifying
            SimulateQualifying(circuit);
            // Simulate sprint race if applicable
            if (isSprintWeekend)
            {
                SimulateSprintRace(circuit);
            }
            // Simulate main race
            SimulateRace(circuit, isSprintWeekend);
            // Update championship standings
            UpdateStandings();
            return _aiDrivers;
        }
        /// <summary>
        /// Simulate qualifying session
        /// </summary>
        private static void SimulateQualifying(CircuitData circuit)
        {
            foreach (var driver in _aiDrivers)
            {
                // Base pace from skills and car
                float basePace = (driver.skills.overallRating * 0.6f) + (driver.carPerformance * 0.4f);
                // Add randomness
                float variance = Random.Range(0.95f, 1.05f);
                float qualifyingPace = basePace * variance;
                // Store as temporary score (higher is better)
                driver.qualifyingScore = qualifyingPace;
            }
            // Sort by qualifying pace
            var sorted = _aiDrivers.OrderByDescending(d => d.qualifyingScore).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].qualifyingPosition = i + 1;
                if (i == 0) sorted[i].poles++;
            }
        }
        /// <summary>
        /// Simulate sprint race (shorter race, half points)
        /// </summary>
        private static void SimulateSprintRace(CircuitData circuit)
        {
            SimulateRaceLogic(circuit, true);
        }
        /// <summary>
        /// Simulate main race
        /// </summary>
        private static void SimulateRace(CircuitData circuit, bool isSprint)
        {
            SimulateRaceLogic(circuit, false);
        }
        /// <summary>
        /// Core race simulation logic
        /// </summary>
        private static void SimulateRaceLogic(CircuitData circuit, bool isSprint)
        {
            // Calculate race pace for each driver
            foreach (var driver in _aiDrivers)
            {
                float racePace = CalculateRacePace(driver, circuit);
                driver.racePace = racePace;
            }
            // Simulate race positions based on pace + randomness
            var sorted = _aiDrivers
                .OrderByDescending(d => d.racePace)
                .ThenBy(d => Random.value)
                .ToList();
            // Assign positions and points
            for (int i = 0; i < sorted.Count; i++)
            {
                int position = i + 1;
                sorted[i].racePosition = position;
                // Add to recent results
                sorted[i].recentResults.Add(position);
                if (sorted[i].recentResults.Count > 5)
                    sorted[i].recentResults.RemoveAt(0);
                // Award points (sprint = half points)
                int points = GetPointsForPosition(position, isSprint);
                sorted[i].points += points;
                // Track wins and podiums
                if (position == 1) sorted[i].wins++;
                if (position <= 3) sorted[i].podiums++;
            }
        }
        /// <summary>
        /// Calculate race pace based on skills, car, and circuit
        /// </summary>
        private static float CalculateRacePace(AIDriverEntry driver, CircuitData circuit)
        {
            // Base performance
            float basePace = (driver.skills.overallRating * 0.5f) + (driver.carPerformance * 0.5f);
            // Circuit-specific modifiers
            float circuitModifier = 1.0f;
            if (circuit.overtakingDifficulty > 0.7f)
            {
                // High overtaking difficulty favors defending skill
                circuitModifier += (driver.skills.defending - 50) * 0.002f;
            }
            else
            {
                // Low overtaking difficulty favors overtaking skill
                circuitModifier += (driver.skills.overtaking - 50) * 0.002f;
            }
            // Tire management affects late race pace
            float tireFactor = DriverSkillCalculator.CalculateTireWearReduction(driver.skills);
            // Consistency affects average pace
            float consistencyFactor = DriverSkillCalculator.CalculateAIConsistency(driver.skills);
            // Add randomness
            float variance = Random.Range(0.97f, 1.03f);
            return basePace * circuitModifier * tireFactor * consistencyFactor * variance;
        }
        /// <summary>
        /// Get points for finishing position
        /// </summary>
        private static int GetPointsForPosition(int position, bool isSprint)
        {
            int[] standardPoints = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
            int[] sprintPoints = { 8, 7, 6, 5, 4, 3, 2, 1 };
            if (position < 1 || position > 20) return 0;
            if (isSprint)
            {
                return position <= sprintPoints.Length ? sprintPoints[position - 1] : 0;
            }
            else
            {
                return position <= standardPoints.Length ? standardPoints[position - 1] : 0;
            }
        }
        /// <summary>
        /// Update championship standings
        /// </summary>
        private static void UpdateStandings()
        {
            var sorted = _aiDrivers.OrderByDescending(d => d.points).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].position = i + 1;
            }
        }
        /// <summary>
        /// Get current championship standings
        /// </summary>
        public static List<AIDriverEntry> GetStandings()
        {
            if (_aiDrivers == null) InitializeChampionship();
            return _aiDrivers.OrderBy(d => d.position).ToList();
        }
        /// <summary>
        /// Create default AI drivers for 2026 season
        /// </summary>
        private static List<AIDriverEntry> CreateAIDrivers()
        {
            return new List<AIDriverEntry>
            {
                new AIDriverEntry {
                    driverName = "Max Verstappen", teamName = "Red Bull Racing", driverCode = "VER",
                    skills = new DriverSkills { braking = 95, racecraft = 97, cornering = 96, consistency = 94, wetSkill = 92, tireSaving = 88, overtaking = 96, defending = 95 },
                    carPerformance = 95
                },
                new AIDriverEntry {
                    driverName = "Charles Leclerc", teamName = "Ferrari", driverCode = "LEC",
                    skills = new DriverSkills { braking = 93, racecraft = 94, cornering = 97, consistency = 89, wetSkill = 91, tireSaving = 85, overtaking = 92, defending = 88 },
                    carPerformance = 92
                },
                new AIDriverEntry {
                    driverName = "Lando Norris", teamName = "McLaren", driverCode = "NOR",
                    skills = new DriverSkills { braking = 91, racecraft = 93, cornering = 94, consistency = 92, wetSkill = 88, tireSaving = 90, overtaking = 94, defending = 90 },
                    carPerformance = 93
                },
                new AIDriverEntry {
                    driverName = "George Russell", teamName = "Mercedes", driverCode = "RUS",
                    skills = new DriverSkills { braking = 90, racecraft = 91, cornering = 93, consistency = 91, wetSkill = 87, tireSaving = 89, overtaking = 90, defending = 92 },
                    carPerformance = 90
                },
                new AIDriverEntry {
                    driverName = "Carlos Sainz", teamName = "Ferrari", driverCode = "SAI",
                    skills = new DriverSkills { braking = 89, racecraft = 92, cornering = 91, consistency = 93, wetSkill = 90, tireSaving = 91, overtaking = 88, defending = 91 },
                    carPerformance = 91
                },
                new AIDriverEntry {
                    driverName = "Lewis Hamilton", teamName = "Ferrari", driverCode = "HAM",
                    skills = new DriverSkills { braking = 94, racecraft = 96, cornering = 95, consistency = 95, wetSkill = 96, tireSaving = 93, overtaking = 93, defending = 94 },
                    carPerformance = 92
                },
                new AIDriverEntry {
                    driverName = "Oscar Piastri", teamName = "McLaren", driverCode = "PIA",
                    skills = new DriverSkills { braking = 88, racecraft = 89, cornering = 91, consistency = 90, wetSkill = 85, tireSaving = 88, overtaking = 89, defending = 87 },
                    carPerformance = 92
                },
                new AIDriverEntry {
                    driverName = "Sergio Perez", teamName = "Red Bull Racing", driverCode = "PER",
                    skills = new DriverSkills { braking = 87, racecraft = 88, cornering = 86, consistency = 85, wetSkill = 84, tireSaving = 86, overtaking = 87, defending = 89 },
                    carPerformance = 94
                }
            };
        }
    }
    // Extension fields for AIDriverEntry (added via partial class or just add to the class)
    public static class AIDriverEntryExtensions
    {
        public static float qualifyingScore;
        public static int qualifyingPosition;
        public static float racePace;
        public static int racePosition;
    }
}
