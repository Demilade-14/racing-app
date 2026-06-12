using System.Collections.Generic;
using UnityEngine;
using RacingGame.Track;
namespace RacingGame.Championship
{
    /// <summary>
    /// Single round in the season calendar
    /// </summary>
    [System.Serializable]
    public class SeasonRound
    {
        public int roundNumber;
        public string circuitName;
        public string country;
        public bool sprintWeekend;
        public bool isCompleted;
        public SeasonRound(int round, CircuitProfile circuit, bool sprint)
        {
            roundNumber = round;
            circuitName = circuit.circuitName;
            country = circuit.region;
            sprintWeekend = sprint;
            isCompleted = false;
        }
    }
    /// <summary>
    /// Manages the season calendar and round progression
    /// </summary>
    public class SeasonCalendar : MonoBehaviour
    {
        public static SeasonCalendar Instance;
        [Header("Calendar")]
        public List<SeasonRound> rounds = new List<SeasonRound>();
        [Header("Settings")]
        public int totalRounds;
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        void Start()
        {
            BuildCalendar();
        }
        /// <summary>
        /// Build the complete season calendar from track database
        /// </summary>
        public void BuildCalendar()
        {
            rounds.Clear();
            var circuits = TrackDatabase.GetAllCircuits();
            totalRounds = circuits.Count;
            // Define sprint weekends (6 per season in 2026)
            var sprintRounds = new HashSet<int> { 5, 9, 13, 17, 21, 24 }; // Example sprint rounds
            for (int i = 0; i < circuits.Count; i++)
            {
                var circuit = circuits[i];
                bool isSprint = sprintRounds.Contains(i + 1);
                rounds.Add(new SeasonRound(i + 1, circuit, isSprint));
            }
            Debug.Log($"[Calendar] Built {totalRounds}-round calendar for 2026 season");
        }
        /// <summary>
        /// Get round by number
        /// </summary>
        public SeasonRound GetRound(int roundNumber)
        {
            return rounds.Find(r => r.roundNumber == roundNumber);
        }
        /// <summary>
        /// Get current round
        /// </summary>
        public SeasonRound GetCurrentRound()
        {
            return rounds.Find(r => !r.isCompleted);
        }
        /// <summary>
        /// Mark round as completed
        /// </summary>
        public void CompleteRound(int roundNumber)
        {
            var round = GetRound(roundNumber);
            if (round != null)
            {
                round.isCompleted = true;
                Debug.Log($"[Calendar] Round {roundNumber} completed: {round.circuitName}");
            }
        }
        /// <summary>
        /// Check if season is complete
        /// </summary>
        public bool IsSeasonComplete()
        {
            return rounds.TrueForAll(r => r.isCompleted);
        }
        /// <summary>
        /// Get remaining rounds
        /// </summary>
        public int GetRemainingRounds()
        {
            return rounds.FindAll(r => !r.isCompleted).Count;
        }
        /// <summary>
        /// Print calendar to console
        /// </summary>
        public void PrintCalendar()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("  2026 F1 SEASON CALENDAR");
            Debug.Log("═══════════════════════════════════════");
            foreach (var round in rounds)
            {
                string sprintTag = round.sprintWeekend ? " [SPRINT]" : "";
                string status = round.isCompleted ? "✓" : "○";
                Debug.Log($"{status} Round {round.roundNumber,2}: {round.circuitName,-30} ({round.country}){sprintTag}");
            }
        }
    }
}
