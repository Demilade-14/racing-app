using UnityEngine;
using System;
namespace RacingGame.Weekend
{
    /// <summary>
    /// Session types for F1 2026 race weekend
    /// </summary>
    public enum WeekendSession
    {
        Practice1,
        Practice2,
        Practice3,
        SprintQualifying,
        SprintRace,
        Qualifying,
        Race,
        Finished
    }
    /// <summary>
    /// Manages the complete F1 race weekend flow.
    /// Handles session progression, lap counting, and sprint weekend logic.
    /// </summary>
    public class RaceWeekendManager : MonoBehaviour
    {
        public static RaceWeekendManager Instance;
        [Header("Session State")]
        public WeekendSession CurrentSession;
        public int currentLap;
        public int totalLaps;
        [Header("Weekend Type")]
        public bool sprintWeekend;
        public int currentRound;
        public string circuitName;
        [Header("Session Results")]
        public int fp1Position;
        public int fp2Position;
        public int fp3Position;
        public int qualiPosition;
        public int sprintQualiPosition;
        public int sprintRacePosition;
        public int racePosition;
        [Header("2026 Features")]
        public bool activeAeroEnabled = true;
        public bool overtakeModeEnabled = true;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        /// <summary>
        /// Initialize a new race weekend
        /// </summary>
        public void StartWeekend(int round, string circuit, bool isSprintWeekend)
        {
            currentRound = round;
            circuitName = circuit;
            sprintWeekend = isSprintWeekend;
            CurrentSession = WeekendSession.Practice1;
            currentLap = 0;
            // Reset session results
            fp1Position = fp2Position = fp3Position = 0;
            qualiPosition = sprintQualiPosition = sprintRacePosition = 0;
            racePosition = 0;
            Debug.Log($"[RaceWeekendManager] Starting Round {round}: {circuit} (Sprint: {isSprintWeekend})");
        }
        /// <summary>
        /// Advance to the next session in the weekend flow
        /// </summary>
        public void AdvanceSession()
        {
            WeekendSession nextSession;
            if (sprintWeekend)
            {
                // Sprint weekend flow: FP1 → FP2 → Sprint Quali → Sprint Race → Quali → Race
                nextSession = CurrentSession switch
                {
                    WeekendSession.Practice1 => WeekendSession.Practice2,
                    WeekendSession.Practice2 => WeekendSession.SprintQualifying,
                    WeekendSession.SprintQualifying => WeekendSession.SprintRace,
                    WeekendSession.SprintRace => WeekendSession.Qualifying,
                    WeekendSession.Qualifying => WeekendSession.Race,
                    WeekendSession.Race => WeekendSession.Finished,
                    _ => WeekendSession.Finished
                };
            }
            else
            {
                // Standard weekend flow: FP1 → FP2 → FP3 → Quali → Race
                nextSession = CurrentSession switch
                {
                    WeekendSession.Practice1 => WeekendSession.Practice2,
                    WeekendSession.Practice2 => WeekendSession.Practice3,
                    WeekendSession.Practice3 => WeekendSession.Qualifying,
                    WeekendSession.Qualifying => WeekendSession.Race,
                    WeekendSession.Race => WeekendSession.Finished,
                    _ => WeekendSession.Finished
                };
            }
            CurrentSession = nextSession;
            currentLap = 0;
            Debug.Log($"[RaceWeekendManager] Advanced to: {CurrentSession}");
        }
        /// <summary>
        /// Check if current session is a practice session
        /// </summary>
        public bool IsPracticeSession()
        {
            return CurrentSession == WeekendSession.Practice1 ||
                   CurrentSession == WeekendSession.Practice2 ||
                   CurrentSession == WeekendSession.Practice3;
        }
        /// <summary>
        /// Check if current session is qualifying
        /// </summary>
        public bool IsQualifyingSession()
        {
            return CurrentSession == WeekendSession.Qualifying ||
                   CurrentSession == WeekendSession.SprintQualifying;
        }
        /// <summary>
        /// Check if current session is a race
        /// </summary>
        public bool IsRaceSession()
        {
            return CurrentSession == WeekendSession.Race ||
                   CurrentSession == WeekendSession.SprintRace;
        }
        /// <summary>
        /// Get session name for UI display
        /// </summary>
        public string GetSessionDisplayName()
        {
            return CurrentSession switch
            {
                WeekendSession.Practice1 => "Free Practice 1",
                WeekendSession.Practice2 => "Free Practice 2",
                WeekendSession.Practice3 => "Free Practice 3",
                WeekendSession.SprintQualifying => "Sprint Qualifying",
                WeekendSession.SprintRace => "Sprint Race",
                WeekendSession.Qualifying => "Qualifying",
                WeekendSession.Race => "Race",
                WeekendSession.Finished => "Weekend Complete",
                _ => "Unknown"
            };
        }
        /// <summary>
        /// Get total laps for current session
        /// </summary>
        public int GetSessionLaps()
        {
            return CurrentSession switch
            {
                WeekendSession.Practice1 => 30,
                WeekendSession.Practice2 => 45,
                WeekendSession.Practice3 => 30,
                WeekendSession.SprintQualifying => 12,
                WeekendSession.SprintRace => 20,
                WeekendSession.Qualifying => 15,
                WeekendSession.Race => totalLaps,
                _ => 0
            };
        }
    }
}
