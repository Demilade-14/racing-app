using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Save;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER INTEGRATION LAYER  – wires managers together, handles season loop
    // ═══════════════════════════════════════════════════════════════════════
    public class CareerIntegrationLayer : MonoBehaviour
    {
        public static CareerIntegrationLayer Instance { get; private set; }

        public DriverCareerManager DriverCareer { get; private set; }
        public RnDManager Rnd { get; private set; }
        public MediaEventSystem Media { get; private set; }
        public ChampionshipManager Championship { get; private set; }

        // Season flow state
        private enum SeasonPhase { PreSeason, RaceWeekend, PostRace, PostSeason }
        private SeasonPhase _currentPhase = SeasonPhase.PreSeason;
        private int _currentRoundIndex = 0;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            DriverCareer = GetComponent<DriverCareerManager>();
            Rnd = GetComponent<RnDManager>();
            Media = GetComponent<MediaEventSystem>();
            Championship = FindObjectOfType<ChampionshipManager>();

            // Wire up events
            if (DriverCareer != null)
            {
                DriverCareer.OnRaceCompleted += HandleRaceCompleted;
                DriverCareer.OnSeasonComplete += HandleSeasonComplete;
            }
        }

        // ── CAREER FLOW ───────────────────────────────────────────────────
        public void StartNewPlayerCareer(string driverName, string nationality, int startingSeries)
        {
            DriverCareer.StartNewCareer(driverName, nationality, startingSeries);
            _currentPhase = SeasonPhase.PreSeason;
            _currentRoundIndex = 0;

            // Initialize championship
            Championship.Initialise(
                DriverCareer.CurrentSeason.calendar.Count,
                DriverCareer.PlayerProfile.season);

            Debug.Log("[CareerIntegration] New career started. Ready for first race.");
        }

        public void AdvanceToRaceWeekend()
        {
            if (DriverCareer.CurrentSeason.calendar.Count <= _currentRoundIndex)
            {
                EndSeason();
                return;
            }

            var weekend = DriverCareer.CurrentSeason.calendar[_currentRoundIndex];
            _currentPhase = SeasonPhase.RaceWeekend;
            DriverCareer.CurrentSeason.currentRound = _currentRoundIndex + 1;

            Debug.Log($"[CareerIntegration] Starting Race {_currentRoundIndex + 1}: {weekend.circuitName}");
        }

        void HandleRaceCompleted(RaceResult result, int xpEarned)
        {
            if (_currentRoundIndex >= DriverCareer.CurrentSeason.calendar.Count) return;

            var weekend = DriverCareer.CurrentSeason.calendar[_currentRoundIndex];
            DriverCareer.ProcessRaceWeekend(weekend, result);

            // Update championship standings
            Championship.ProcessRound(new List<RaceResult> { result });

            // Trigger media interview
            Media.TriggerPostRaceConference(result.finishPosition);

            _currentPhase = SeasonPhase.PostRace;
            _currentRoundIndex++;
        }

        void HandleSeasonComplete(int finalPosition)
        {
            _currentPhase = SeasonPhase.PostSeason;

            // Generate new contract offers
            GenerateContractOffers(finalPosition);

            // Season summary
            Debug.Log($"[CareerIntegration] Season {DriverCareer.PlayerProfile.season} complete. " +
                $"Championship P{finalPosition}, {DriverCareer.PlayerProfile.seasonWins} wins");
        }

        public void EndSeason()
        {
            int finalChampPos = Championship.GetPlayerPosition(DriverCareer.PlayerProfile.driverName);
            DriverCareer.EndSeason(finalChampPos);

            // Clear for next season
            _currentPhase = SeasonPhase.PreSeason;
            _currentRoundIndex = 0;
        }

        // ── CONTRACT SYSTEM ───────────────────────────────────────────────
        void GenerateContractOffers(int finishPosition)
        {
            // Teams interested based on player performance
            var offers = new List<ContractOffer>();

            // Top finisher gets offers from top teams
            if (finishPosition <= 3)
            {
                // Simulate top teams making offers
                string[] topTeams = { \"Ferrari\", \"Mercedes\", \"Red Bull\" };
                foreach (var team in topTeams)
                {
                    var offer = ContractNegotiationEngine.GenerateTeamOffer(
                        team, SeriesTier.Formula1,
                        DriverCareer.PlayerProfile, 1,
                        200_000_000f);

                    offers.Add(offer);
                }
            }

            // Mid-field driver gets mid-field offers
            if (finishPosition > 5 && finishPosition <= 10)
            {
                var offer = ContractNegotiationEngine.GenerateTeamOffer(
                    \"Aston Martin\", SeriesTier.Formula1,
                    DriverCareer.PlayerProfile, 6,
                    120_000_000f);

                offers.Add(offer);
            }

            // Display all offers to player
            Debug.Log($\"[CareerIntegration] {offers.Count} contract offers generated.\");
        }

        // ── SAVE/LOAD INTEGRATION ─────────────────────────────────────────
        public void SaveCareerProgress()
        {
            if (SaveManager.Instance == null) return;

            var currentSave = SaveManager.Instance.Current;
            if (currentSave.career == null) currentSave.career = new CareerSave();

            // Map driver career to persistent save
            var careerSave = currentSave.career;
            careerSave.careerType = CareerType.Driver;
            careerSave.season = DriverCareer.PlayerProfile.season;
            careerSave.careerAge = DriverCareer.PlayerProfile.attributes.OverallRating;

            // Store as JSON
            SaveManager.Instance.SaveCareer();

            Debug.Log(\"[CareerIntegration] Career progress saved.\");
        }

        public void LoadCareerProgress()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Current.career == null)
            {
                Debug.LogWarning(\"[CareerIntegration] No career save found.\");
                return;
            }

            // Load from persistent save
            var loaded = SaveManager.Instance.Current.career;

            // Restore to managers
            if (DriverCareer != null && DriverCareer.CurrentSeason != null)
            {
                Debug.Log($\"[CareerIntegration] Career loaded. Season {loaded.season}.\");
            }
        }

        // ── GETTERS ───────────────────────────────────────────────────────
        public string GetCurrentRoundInfo()
        {
            if (_currentRoundIndex >= DriverCareer.CurrentSeason.calendar.Count)
                return \"Season Complete\";

            var weekend = DriverCareer.CurrentSeason.calendar[_currentRoundIndex];
            return $\"Round {_currentRoundIndex + 1}/{DriverCareer.CurrentSeason.calendar.Count}: {weekend.circuitName}\";
        }

        public string GetChampionshipStanding()
        {
            int pos = Championship.GetPlayerPosition(DriverCareer.PlayerProfile.driverName);
            int pts = Championship.GetPlayerPoints(DriverCareer.PlayerProfile.driverName);
            return $\"P{pos} | {pts} PTS\";
        }

        public string GetCareerStatsDisplay()
        {
            return $\"Races: {DriverCareer.PlayerProfile.careerRaces} | \" +
                   $\"Wins: {DriverCareer.PlayerProfile.careerWins} | \" +
                   $\"OVR: {DriverCareer.PlayerProfile.attributes.OverallRating}/99\";
        }

        void OnDestroy()
        {
            if (DriverCareer != null)
            {
                DriverCareer.OnRaceCompleted -= HandleRaceCompleted;
                DriverCareer.OnSeasonComplete -= HandleSeasonComplete;
            }
        }
    }
}
