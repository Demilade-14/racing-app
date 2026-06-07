using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER CAREER MANAGER  – handles season loop, race progression
    // ═══════════════════════════════════════════════════════════════════════
    public class DriverCareerManager : MonoBehaviour
    {
        public static DriverCareerManager Instance { get; private set; }

        public DriverCareerSave CurrentSeason { get; private set; }
        public DriverProfile PlayerProfile { get; private set; }
        public ChampionshipManager Championship { get; private set; }
        public RnDManager RnD { get; private set; }
        public MediaEventSystem MediaEvents { get; private set; }

        // Events
        public event Action<RaceResult, int> OnRaceCompleted;  // (result, xp earned)
        public event Action<int> OnSeasonComplete;             // (championship position)
        public event Action OnContractExpiring;
        public event Action<string> OnNewspaper;               // (headline)

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            Championship = FindObjectOfType<ChampionshipManager>();
            RnD = FindObjectOfType<RnDManager>();
            MediaEvents = FindObjectOfType<MediaEventSystem>();
        }

        // ── CAREER SETUP ──────────────────────────────────────────────────
        public void StartNewCareer(string driverName, string nationality, int startingSeries = 1)
        {
            CurrentSeason = new DriverCareerSave();
            PlayerProfile = new DriverProfile
            {
                driverName = driverName,
                driverCode = GenerateDriverCode(driverName),
                nationality = nationality,
                tier = (SeriesTier)startingSeries,  // 0 = F3, 1 = F2, 2 = F1
                attributes = new DriverAttributes(),
                raceNumber = UnityEngine.Random.Range(1, 99)
            };

            CurrentSeason.profile = PlayerProfile;
            CurrentSeason.phase = CareerPhase.PreSeason;
            CurrentSeason.modeType = CareerModeType.Driver;

            // Init empty season
            ResetSeasonStats();
            GenerateCalendar();
            GenerateSeasonObjectives();

            Debug.Log($"[Career] New driver career started: {driverName} ({nationality}) in {PlayerProfile.tier}");
        }

        // ── RACE PROCESSING ──────────────────────────────────────────────
        public void ProcessRaceWeekend(DriverRaceWeekend weekend, RaceResult result)
        {
            if (result == null || PlayerProfile == null) return;

            weekend.result = result;

            // Award XP based on performance
            int xpEarned = CalculateRaceXP(result, weekend);
            AwardRaceXP(result, xpEarned);

            // Update stats
            int pts = PointsFor(result.finishPosition);
            if (result.hasFastestLap && result.finishPosition <= 10) pts++;

            PlayerProfile.seasonPoints += pts;
            PlayerProfile.careerPoints += pts;
            PlayerProfile.careerRaces++;
            PlayerProfile.racesSinceCareerStart++;

            if (result.finishPosition == 1) { PlayerProfile.seasonWins++; PlayerProfile.careerWins++; }
            if (result.finishPosition <= 3) { PlayerProfile.seasonPodiums++; PlayerProfile.careerPodiums++; }
            if (result.hasFastestLap) PlayerProfile.seasonFastestLaps++;
            if (result.retired) PlayerProfile.seasonDNFs++;
            if (weekend.gridPosition == 1) PlayerProfile.seasonPoles++;

            // Process teammate dynamics
            UpdateTeammateRelationship(result);

            // Media event triggered
            GeneratePostRaceNews(result);

            // Award R&D points
            int rndPoints = CalculateRnDPoints(result);
            RnD?.AwardTokens(rndPoints);

            OnRaceCompleted?.Invoke(result, xpEarned);

            Debug.Log($"[Career] Race complete: P{result.finishPosition}, +{pts}pts, XP+{xpEarned}");
        }

        int CalculateRaceXP(RaceResult result, DriverRaceWeekend weekend)
        {
            int xp = 0;

            // Finishing position (up to 50 XP)
            xp += result.finishPosition switch
            {
                1 => 50,
                2 => 45,
                3 => 40,
                <= 6 => 30,
                <= 10 => 20,
                _ => result.retired ? 0 : 10
            };

            // Qualifying bonus (10-20 XP)
            if (weekend.gridPosition <= 3) xp += 20;
            else if (weekend.gridPosition <= 6) xp += 15;
            else if (weekend.gridPosition <= 10) xp += 10;

            // Fastest lap bonus (15 XP)
            if (result.hasFastestLap) xp += 15;

            // Overtakes XP (5 XP per overtake, up to 50 total)
            xp += Mathf.Min(weekend.overtakesThisRace * 5, 50);

            return xp;
        }

        void AwardRaceXP(RaceResult result, int totalXp)
        {
            // Distribute XP based on race performance
            if (result.finishPosition <= 3)
                PlayerProfile.attributes.AddXP("consistency", totalXp / 2);

            if (result.finishPosition <= 6)
                PlayerProfile.attributes.AddXP("racecraft", totalXp / 3);

            // Overtake skill
            if (result.finishPosition <= 10)
                PlayerProfile.attributes.AddXP("overtaking", totalXp / 4);

            // Awareness (always grows)
            PlayerProfile.attributes.AddXP("awareness", totalXp / 5);

            // Skill points (player spendable)
            PlayerProfile.skillPointsSpendable += totalXp / 250;
        }

        int CalculateRnDPoints(RaceResult result) =>
            result.finishPosition switch
            {
                1 => 8,
                2 => 6,
                3 => 4,
                <= 6 => 2,
                _ => result.retired ? 0 : 1
            };

        void UpdateTeammateRelationship(RaceResult result)
        {
            // Simulate teammate result (simplified)
            int tmateFinish = UnityEngine.Random.Range(5, 15);
            bool playerAhead = result.finishPosition < tmateFinish;

            if (playerAhead)
                PlayerProfile.teamRelationship += 5f;  // Beat teammate = team happy
            else
                PlayerProfile.teamRelationship -= 2f;

            PlayerProfile.teamRelationship = Mathf.Clamp(PlayerProfile.teamRelationship, 0f, 100f);
        }

        void GeneratePostRaceNews(RaceResult result)
        {
            string headline = result.finishPosition switch
            {
                1 => $"🏆 {PlayerProfile.driverName} WINS! Dominant display at {CurrentSeason.calendar[CurrentSeason.currentRound - 1].circuitName}",
                2 => $"🥈 Runner-up finish for {PlayerProfile.driverName} — strong pace in the championship fight",
                3 => $"🥉 Podium! {PlayerProfile.driverName} secures third place",
                <= 6 => $"Points finish: {PlayerProfile.driverName} completes solid race",
                _ => result.retired ? $"DNF: {PlayerProfile.driverName} retires from race" : "Mid-field result for player"
            };

            OnNewspaper?.Invoke(headline);
        }

        // ── SEASON MANAGEMENT ─────────────────────────────────────────────
        void ResetSeasonStats()
        {
            PlayerProfile.seasonPoints = 0;
            PlayerProfile.seasonWins = 0;
            PlayerProfile.seasonPodiums = 0;
            PlayerProfile.seasonPoles = 0;
            PlayerProfile.seasonFastestLaps = 0;
            PlayerProfile.seasonDNFs = 0;
        }

        void GenerateCalendar()
        {
            CurrentSeason.calendar.Clear();
            int races = PlayerProfile.tier == SeriesTier.Formula1 ? 24 : 20;

            string[] circuits = { "Monaco", "Silverstone", "Monza", "Spa", "Suzuka", "Hungary", "Singapore", "Abu Dhabi" };

            for (int i = 0; i < races; i++)
            {
                CurrentSeason.calendar.Add(new DriverRaceWeekend
                {
                    round = i + 1,
                    circuitName = circuits[i % circuits.Length]
                });
            }
        }

        void GenerateSeasonObjectives()
        {
            CurrentSeason.objectives.Clear();

            // Primary objectives (championship-affecting)
            CurrentSeason.objectives.Add(new SeasonObjective
            {
                id = "season_wins",
                description = $"Score {(PlayerProfile.tier == SeriesTier.Formula1 ? 3 : 2)} wins this season",
                isPrimary = true,
                reputationReward = 20f,
                skillPointReward = 50
            });

            CurrentSeason.objectives.Add(new SeasonObjective
            {
                id = "beat_teammate",
                description = "Out-qualify and out-score your teammate all season",
                isPrimary = true,
                reputationReward = 15f,
                skillPointReward = 30
            });

            // Secondary objectives
            CurrentSeason.objectives.Add(new SeasonObjective
            {
                id = "podiums",
                description = $"Score {(PlayerProfile.tier == SeriesTier.Formula1 ? 8 : 6)} podiums",
                isPrimary = false,
                reputationReward = 10f,
                skillPointReward = 20
            });
        }

        public void EndSeason(int finalChampionshipPosition)
        {
            bool promoted = false;

            if (finalChampionshipPosition <= 3 && PlayerProfile.tier < SeriesTier.Formula1)
            {
                promoted = true;
                PlayerProfile.tier = (SeriesTier)((int)PlayerProfile.tier + 1);
                OnNewspaper?.Invoke($"🚀 PROMOTION! {PlayerProfile.driverName} moves up to {PlayerProfile.tier}!");
            }

            PlayerProfile.season++;
            PlayerProfile.careerChampionships += finalChampionshipPosition == 1 ? 1 : 0;

            ResetSeasonStats();
            GenerateCalendar();
            GenerateSeasonObjectives();

            CurrentSeason.currentRound = 0;
            CurrentSeason.phase = CareerPhase.PreSeason;

            OnSeasonComplete?.Invoke(finalChampionshipPosition);

            Debug.Log($"[Career] Season ended. Position: {finalChampionshipPosition}, Promoted: {promoted}");
        }

        public void OfferContract(ContractOffer offer)
        {
            if (offer.seasons > 0)
            {
                PlayerProfile.activeContract = new DriverContract
                {
                    teamName = offer.teamName,
                    tier = offer.tier,
                    annualSalary = offer.offeredSalary,
                    podiumBonus = offer.podiumBonus,
                    winBonus = offer.winBonus,
                    durationSeasons = offer.seasons,
                    seasonsRemaining = offer.seasons,
                    hasNumberOneClause = offer.offersNumberOne
                };

                PlayerProfile.currentTeam = offer.teamName;
                PlayerProfile.contractHistory.Add(PlayerProfile.activeContract);

                OnNewspaper?.Invoke($"✍️ Contract signed! {PlayerProfile.driverName} joins {offer.teamName}");
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────
        static readonly int[] POINTS_TABLE = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
        int PointsFor(int position) => position > 0 && position <= POINTS_TABLE.Length ? POINTS_TABLE[position - 1] : 0;

        string GenerateDriverCode(string name)
        {
            if (string.IsNullOrEmpty(name)) return "GHO";
            string code = name.ToUpper();
            return code.Length >= 3 ? code.Substring(0, 3) : (code + "GHO").Substring(0, 3);
        }
    }
}
