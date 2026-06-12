using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER CAREER MANAGER  –  Season loop, race progression, rivals,
    //                            training, objectives, media integration
    //  Extends the original DriverCareerManager with 2026 features.
    // ═══════════════════════════════════════════════════════════════════════

    public class DriverCareerManager : MonoBehaviour
    {
        public static DriverCareerManager Instance { get; private set; }

        // ── Core State ────────────────────────────────────────────────────
        public DriverCareerSave   CurrentSeason   { get; private set; }
        public DriverProfile      PlayerProfile   { get; private set; }

        // ── Sub-systems ───────────────────────────────────────────────────
        public ChampionshipManager   Championship   { get; private set; }
        public RnDManager            RnD            { get; private set; }
        public MediaEventSystem      MediaEvents    { get; private set; }
        public ObjectivesManager     Objectives     { get; private set; }
        public RivalEngine           Rivals         { get; private set; }

        // ── Events ────────────────────────────────────────────────────────
        public event Action<RaceResult, int>   OnRaceCompleted;   // (result, xp earned)
        public event Action<int>               OnSeasonComplete;  // (championship position)
        public event Action                    OnContractExpiring;
        public event Action<string>            OnNewspaper;       // (headline)
        public event Action<SeasonObjective>   OnObjectiveCompleted;
        public event Action<RivalEntry>        OnRivalEvent;
        public event Action<int>               OnRoundAdvanced;   // (new round index)
        public event Action<string>            OnTrainingComplete;// (attribute trained)
        public event Action<MediaEvent>        OnMediaEventReady;

        // ── Internal ──────────────────────────────────────────────────────
        private bool _careerActive = false;
        private int  _trainingTokensThisWeek = 0;
        private const int MAX_TRAINING_PER_WEEK = 2;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            Championship = FindObjectOfType<ChampionshipManager>();
            RnD          = FindObjectOfType<RnDManager>();
            MediaEvents  = FindObjectOfType<MediaEventSystem>();

            // Instantiate sub-systems that don't require scene objects
            Objectives = new ObjectivesManager(this);
            Rivals     = new RivalEngine(this);
        }

        // ═════════════════════════════════════════════════════════════════
        //  CAREER SETUP
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Primary entry point — creates a new driver career from the
        /// DriverCreation screen values.
        /// </summary>
        public void StartDriverCareer(DriverCreationPayload payload)
        {
            CurrentSeason = new DriverCareerSave();

            PlayerProfile = new DriverProfile
            {
                driverName    = payload.driverName,
                driverCode    = GenerateDriverCode(payload.driverName),
                nationality   = payload.nationality,
                tier          = payload.startingTier,
                attributes    = BuildStartingAttributes(payload.archetype),
                raceNumber    = payload.raceNumber > 0 ? payload.raceNumber : UnityEngine.Random.Range(2, 99),
                currentTeam   = payload.startingTeam,
                reputationScore = payload.startingReputation
            };

            CurrentSeason.profile  = PlayerProfile;
            CurrentSeason.phase    = CareerPhase.PreSeason;
            CurrentSeason.modeType = CareerModeType.Driver;

            ResetSeasonStats();
            GenerateCalendar();
            Objectives.GenerateSeasonObjectives(PlayerProfile);
            Rivals.InitialiseRivals(PlayerProfile);
            TriggerMediaEvent(MediaEventType.CareerStart);

            _careerActive = true;

            Debug.Log($"[Career] StartDriverCareer: {PlayerProfile.driverName} | " +
                      $"Tier: {PlayerProfile.tier} | Team: {PlayerProfile.currentTeam}");
        }

        // Legacy overload (keeps old callers working)
        public void StartNewCareer(string driverName, string nationality, int startingSeries = 1)
        {
            StartDriverCareer(new DriverCreationPayload
            {
                driverName   = driverName,
                nationality  = nationality,
                startingTier = (SeriesTier)startingSeries
            });
        }

        DriverAttributes BuildStartingAttributes(DriverArchetype archetype)
        {
            var a = new DriverAttributes();
            switch (archetype)
            {
                case DriverArchetype.Overtaker:
                    a.SetBase("overtaking", 65); a.SetBase("racecraft", 60);
                    a.SetBase("consistency", 50); a.SetBase("awareness", 55);
                    break;
                case DriverArchetype.Qualifier:
                    a.SetBase("qualifying", 68); a.SetBase("consistency", 62);
                    a.SetBase("overtaking", 48); a.SetBase("awareness", 52);
                    break;
                case DriverArchetype.TyreWhisperer:
                    a.SetBase("tyreManagement", 70); a.SetBase("consistency", 65);
                    a.SetBase("overtaking", 50); a.SetBase("awareness", 58);
                    break;
                case DriverArchetype.Balanced:
                default:
                    a.SetBase("overtaking", 55); a.SetBase("racecraft", 55);
                    a.SetBase("consistency", 55); a.SetBase("awareness", 55);
                    a.SetBase("qualifying", 55); a.SetBase("tyreManagement", 55);
                    break;
            }
            return a;
        }

        // ═════════════════════════════════════════════════════════════════
        //  ROUND ADVANCEMENT
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Call this when the player confirms they are ready to move
        /// to the next race weekend.
        /// </summary>
        public void AdvanceRound()
        {
            if (!_careerActive) return;

            _trainingTokensThisWeek = 0;  // reset weekly training budget

            CurrentSeason.currentRound++;
            int round = CurrentSeason.currentRound;

            if (round > CurrentSeason.calendar.Count)
            {
                // All races complete — trigger season-end flow
                int pos = Championship != null
                    ? Championship.GetPlayerPosition()
                    : EvaluateSimulatedPosition();
                AdvanceSeason(pos);
                return;
            }

            CurrentSeason.phase = CareerPhase.RaceWeekend;

            // Contract expiry warning
            if (PlayerProfile.activeContract != null &&
                PlayerProfile.activeContract.seasonsRemaining == 1 &&
                round == CurrentSeason.calendar.Count - 2)
            {
                OnContractExpiring?.Invoke();
            }

            // Rival encounter check
            Rivals.CheckRoundRivalEvent(round);

            // Media prompt
            TriggerMediaEvent(MediaEventType.PreRace);

            OnRoundAdvanced?.Invoke(round);

            Debug.Log($"[Career] AdvanceRound → Round {round} / {CurrentSeason.calendar.Count}");
        }

        // ═════════════════════════════════════════════════════════════════
        //  SEASON ADVANCEMENT
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Finalises the current season and sets up the next one.
        /// </summary>
        public void AdvanceSeason(int finalChampionshipPosition)
        {
            if (!_careerActive) return;

            // ── Evaluate objectives ───────────────────────────────────────
            Objectives.EvaluateEndOfSeason(PlayerProfile, finalChampionshipPosition);

            // ── Archive season ────────────────────────────────────────────
            var record = new SeasonRecord
            {
                season              = PlayerProfile.season,
                tier                = PlayerProfile.tier,
                teamName            = PlayerProfile.currentTeam,
                championshipPos     = finalChampionshipPosition,
                wins                = PlayerProfile.seasonWins,
                podiums             = PlayerProfile.seasonPodiums,
                poles               = PlayerProfile.seasonPoles,
                fastestLaps         = PlayerProfile.seasonFastestLaps,
                dnfs                = PlayerProfile.seasonDNFs,
                totalPoints         = PlayerProfile.seasonPoints,
                isChampionshipYear  = finalChampionshipPosition == 1
            };
            PlayerProfile.seasonHistory.Add(record);

            // ── Legacy / reputation ───────────────────────────────────────
            PlayerProfile.UpdateLegacyTier();
            ApplyReputationFromSeason(finalChampionshipPosition);

            // ── Promotion / relegation ────────────────────────────────────
            bool promoted = false;
            if (finalChampionshipPosition <= 3 && PlayerProfile.tier < SeriesTier.Formula1)
            {
                promoted = true;
                PlayerProfile.tier = (SeriesTier)((int)PlayerProfile.tier + 1);
                OnNewspaper?.Invoke($"🚀 PROMOTION! {PlayerProfile.driverName} moves up to {PlayerProfile.tier}!");
            }

            // ── Career stats ──────────────────────────────────────────────
            PlayerProfile.season++;
            if (finalChampionshipPosition == 1)
                PlayerProfile.careerChampionships++;

            // ── Rival evolution ───────────────────────────────────────────
            Rivals.EvolveRivals(finalChampionshipPosition);

            // ── Reset for next season ─────────────────────────────────────
            ResetSeasonStats();
            GenerateCalendar();
            Objectives.GenerateSeasonObjectives(PlayerProfile);

            CurrentSeason.currentRound = 0;
            CurrentSeason.phase        = CareerPhase.PreSeason;

            // Tick contract
            if (PlayerProfile.activeContract != null)
                PlayerProfile.activeContract.seasonsRemaining--;

            TriggerMediaEvent(MediaEventType.SeasonEnd);
            OnSeasonComplete?.Invoke(finalChampionshipPosition);

            Debug.Log($"[Career] AdvanceSeason → Season {PlayerProfile.season} | " +
                      $"P{finalChampionshipPosition} | Promoted: {promoted}");
        }

        // ═════════════════════════════════════════════════════════════════
        //  TRAINING SYSTEM
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Apply a training session in the pre-race week.
        /// attribute: "overtaking" | "racecraft" | "consistency" |
        ///            "awareness" | "qualifying" | "tyreManagement" |
        ///            "ersManagement"
        /// </summary>
        public bool ApplyTraining(string attribute, TrainingIntensity intensity = TrainingIntensity.Normal)
        {
            if (_trainingTokensThisWeek >= MAX_TRAINING_PER_WEEK)
            {
                Debug.LogWarning("[Career] Training limit reached for this week.");
                return false;
            }

            int xpGain = intensity switch
            {
                TrainingIntensity.Light    => 20,
                TrainingIntensity.Normal   => 40,
                TrainingIntensity.Intense  => 70,
                _                          => 40
            };

            // Injury risk on intense sessions
            if (intensity == TrainingIntensity.Intense)
            {
                float injuryRoll = UnityEngine.Random.value;
                if (injuryRoll < 0.08f)  // 8 % chance
                {
                    OnNewspaper?.Invoke($"⚠️ {PlayerProfile.driverName} picks up a minor strain from intense training!");
                    xpGain /= 2;
                }
            }

            PlayerProfile.attributes.AddXP(attribute, xpGain);
            _trainingTokensThisWeek++;

            // 2026-specific: ERS management skill feeds into in-race ERS system
            if (attribute == "ersManagement")
                PlayerProfile.ersManagementSkill = Mathf.Min(PlayerProfile.ersManagementSkill + xpGain * 0.1f, 100f);

            if (attribute == "tyreManagement")
                PlayerProfile.tyreManagementSkill = Mathf.Min(PlayerProfile.tyreManagementSkill + xpGain * 0.1f, 100f);

            OnTrainingComplete?.Invoke(attribute);
            Debug.Log($"[Career] Training: +{xpGain} XP to {attribute} (intensity: {intensity})");
            return true;
        }

        // ═════════════════════════════════════════════════════════════════
        //  RACE PROCESSING  (retained + extended from original)
        // ═════════════════════════════════════════════════════════════════

        public void ProcessRaceWeekend(DriverRaceWeekend weekend, RaceResult result)
        {
            if (result == null || PlayerProfile == null) return;

            weekend.result = result;

            int xpEarned = CalculateRaceXP(result, weekend);
            AwardRaceXP(result, xpEarned);

            int pts = PointsFor(result.finishPosition);
            if (result.hasFastestLap && result.finishPosition <= 10) pts++;

            PlayerProfile.seasonPoints  += pts;
            PlayerProfile.careerPoints  += pts;
            PlayerProfile.careerRaces++;
            PlayerProfile.racesSinceCareerStart++;

            if (result.finishPosition == 1) { PlayerProfile.seasonWins++;   PlayerProfile.careerWins++; }
            if (result.finishPosition <= 3) { PlayerProfile.seasonPodiums++; PlayerProfile.careerPodiums++; }
            if (result.hasFastestLap)        PlayerProfile.seasonFastestLaps++;
            if (result.retired)              PlayerProfile.seasonDNFs++;
            if (weekend.gridPosition == 1)   PlayerProfile.seasonPoles++;

            // Update reputation score
            ApplyReputationFromRace(result);

            UpdateTeammateRelationship(result);
            GeneratePostRaceNews(result);

            // Objectives check
            Objectives.CheckRaceObjectives(result, weekend, PlayerProfile);

            // Rival post-race reaction
            Rivals.ProcessRaceResult(result);

            // R&D
            int rndPoints = CalculateRnDPoints(result);
            RnD?.AwardTokens(rndPoints);

            // Media
            TriggerMediaEvent(MediaEventType.PostRace);

            OnRaceCompleted?.Invoke(result, xpEarned);

            Debug.Log($"[Career] Race complete: P{result.finishPosition}, +{pts}pts, XP+{xpEarned}");
        }

        // ═════════════════════════════════════════════════════════════════
        //  MEDIA EVENT INTEGRATION
        // ═════════════════════════════════════════════════════════════════

        public void TriggerMediaEvent(MediaEventType type)
        {
            if (MediaEvents == null) return;

            var evt = MediaEvents.GenerateEvent(type, PlayerProfile, CurrentSeason);
            if (evt != null)
                OnMediaEventReady?.Invoke(evt);
        }

        /// <summary>
        /// Called by the React UI when the player picks a media response option.
        /// </summary>
        public void RespondToMediaEvent(MediaEvent evt, int choiceIndex)
        {
            if (evt == null || choiceIndex < 0 || choiceIndex >= evt.choices.Count) return;

            var choice = evt.choices[choiceIndex];

            PlayerProfile.reputationScore    += choice.reputationDelta;
            PlayerProfile.teamRelationship   += choice.teamRelationshipDelta;
            PlayerProfile.fanbase            += choice.fanbaseDelta;

            PlayerProfile.reputationScore  = Mathf.Clamp(PlayerProfile.reputationScore,  0f, 100f);
            PlayerProfile.teamRelationship = Mathf.Clamp(PlayerProfile.teamRelationship, 0f, 100f);

            // Rival reaction
            if (choice.antagRival)
                Rivals.ProvokePrimaryRival();

            OnNewspaper?.Invoke(choice.headline);

            Debug.Log($"[Career] Media choice: '{choice.label}' | " +
                      $"Rep Δ{choice.reputationDelta:+0;-0} | " +
                      $"TeamRel Δ{choice.teamRelationshipDelta:+0;-0}");
        }

        // ═════════════════════════════════════════════════════════════════
        //  CONTRACT HELPERS
        // ═════════════════════════════════════════════════════════════════

        public void OfferContract(ContractOffer offer)
        {
            if (offer.seasons <= 0) return;

            PlayerProfile.activeContract = new DriverContract
            {
                teamName         = offer.teamName,
                tier             = offer.tier,
                annualSalary     = offer.offeredSalary,
                podiumBonus      = offer.podiumBonus,
                winBonus         = offer.winBonus,
                durationSeasons  = offer.seasons,
                seasonsRemaining = offer.seasons,
                hasNumberOneClause = offer.offersNumberOne
            };

            PlayerProfile.currentTeam = offer.teamName;
            PlayerProfile.contractHistory.Add(PlayerProfile.activeContract);

            OnNewspaper?.Invoke($"✍️ Contract signed! {PlayerProfile.driverName} joins {offer.teamName}");
        }

        // ═════════════════════════════════════════════════════════════════
        //  INTERNAL HELPERS
        // ═════════════════════════════════════════════════════════════════

        void ResetSeasonStats()
        {
            PlayerProfile.seasonPoints      = 0;
            PlayerProfile.seasonWins        = 0;
            PlayerProfile.seasonPodiums     = 0;
            PlayerProfile.seasonPoles       = 0;
            PlayerProfile.seasonFastestLaps = 0;
            PlayerProfile.seasonDNFs        = 0;
        }

        void GenerateCalendar()
        {
            CurrentSeason.calendar.Clear();
            bool is2026 = CurrentSeason.is2026Regulations;

            // 2026 calendar includes Madrid (MADRING)
            string[] f1Circuits = is2026
                ? new[] { "Bahrain","Saudi Arabia","Australia","Japan","China","Miami",
                          "Imola","Monaco","Spain","Madrid","Canada","Austria",
                          "Silverstone","Hungary","Spa","Netherlands","Monza","Azerbaijan",
                          "Singapore","United States","Mexico","São Paulo","Las Vegas","Abu Dhabi" }
                : new[] { "Bahrain","Saudi Arabia","Australia","Japan","China","Miami",
                          "Imola","Monaco","Spain","Canada","Austria","Silverstone",
                          "Hungary","Spa","Netherlands","Monza","Singapore","Japan",
                          "United States","Mexico","São Paulo","Las Vegas","Abu Dhabi" };

            string[] lowerCircuits = { "Circuit A","Circuit B","Circuit C","Circuit D",
                                       "Circuit E","Circuit F","Circuit G","Circuit H",
                                       "Circuit I","Circuit J","Circuit K","Circuit L",
                                       "Circuit M","Circuit N","Circuit O","Circuit P",
                                       "Circuit Q","Circuit R","Circuit S","Circuit T" };

            bool isF1  = PlayerProfile.tier == SeriesTier.Formula1;
            var  list  = isF1 ? f1Circuits : lowerCircuits;
            int  count = isF1 ? (is2026 ? 24 : 23) : 20;

            for (int i = 0; i < count; i++)
            {
                CurrentSeason.calendar.Add(new DriverRaceWeekend
                {
                    round       = i + 1,
                    circuitName = list[i % list.Length]
                });
            }
        }

        int CalculateRaceXP(RaceResult result, DriverRaceWeekend weekend)
        {
            int xp = result.finishPosition switch
            {
                1    => 50,
                2    => 45,
                3    => 40,
                <= 6 => 30,
                <= 10 => 20,
                _    => result.retired ? 0 : 10
            };

            if (weekend.gridPosition <= 3)  xp += 20;
            else if (weekend.gridPosition <= 6)  xp += 15;
            else if (weekend.gridPosition <= 10) xp += 10;

            if (result.hasFastestLap) xp += 15;
            xp += Mathf.Min(weekend.overtakesThisRace * 5, 50);

            return xp;
        }

        void AwardRaceXP(RaceResult result, int totalXp)
        {
            if (result.finishPosition <= 3)
                PlayerProfile.attributes.AddXP("consistency", totalXp / 2);

            if (result.finishPosition <= 6)
                PlayerProfile.attributes.AddXP("racecraft", totalXp / 3);

            if (result.finishPosition <= 10)
                PlayerProfile.attributes.AddXP("overtaking", totalXp / 4);

            PlayerProfile.attributes.AddXP("awareness", totalXp / 5);
            PlayerProfile.skillPointsSpendable += totalXp / 250;
        }

        int CalculateRnDPoints(RaceResult result) =>
            result.finishPosition switch
            {
                1    => 8,
                2    => 6,
                3    => 4,
                <= 6 => 2,
                _    => result.retired ? 0 : 1
            };

        void UpdateTeammateRelationship(RaceResult result)
        {
            int tmateFinish = UnityEngine.Random.Range(5, 15);
            bool playerAhead = result.finishPosition < tmateFinish;

            PlayerProfile.teamRelationship += playerAhead ? 5f : -2f;
            PlayerProfile.teamRelationship  = Mathf.Clamp(PlayerProfile.teamRelationship, 0f, 100f);
        }

        void GeneratePostRaceNews(RaceResult result)
        {
            string circuit = CurrentSeason.currentRound > 0 &&
                             CurrentSeason.currentRound <= CurrentSeason.calendar.Count
                ? CurrentSeason.calendar[CurrentSeason.currentRound - 1].circuitName
                : "the circuit";

            string headline = result.finishPosition switch
            {
                1    => $"🏆 {PlayerProfile.driverName} WINS at {circuit}!",
                2    => $"🥈 Runner-up finish for {PlayerProfile.driverName} at {circuit}",
                3    => $"🥉 Podium for {PlayerProfile.driverName} at {circuit}",
                <= 6 => $"Points finish: {PlayerProfile.driverName} scores at {circuit}",
                _    => result.retired
                    ? $"DNF: {PlayerProfile.driverName} retires from {circuit}"
                    : $"Mid-field result for {PlayerProfile.driverName} at {circuit}"
            };

            OnNewspaper?.Invoke(headline);
        }

        void ApplyReputationFromRace(RaceResult result)
        {
            float delta = result.finishPosition switch
            {
                1    =>  5f,
                2    =>  3f,
                3    =>  2f,
                <= 6 =>  1f,
                <= 10 => 0.5f,
                _    => result.retired ? -1f : 0f
            };

            PlayerProfile.reputationScore = Mathf.Clamp(PlayerProfile.reputationScore + delta, 0f, 100f);
        }

        void ApplyReputationFromSeason(int championshipPos)
        {
            float delta = championshipPos switch
            {
                1    => 15f,
                2    => 10f,
                3    =>  7f,
                <= 6 =>  4f,
                _    =>  1f
            };

            PlayerProfile.reputationScore = Mathf.Clamp(PlayerProfile.reputationScore + delta, 0f, 100f);
        }

        int EvaluateSimulatedPosition()
        {
            // Fallback when no ChampionshipManager is available (e.g. in tests)
            if (PlayerProfile.seasonWins >= 8)    return 1;
            if (PlayerProfile.seasonWins >= 5)    return 2;
            if (PlayerProfile.seasonPodiums >= 8) return 3;
            if (PlayerProfile.seasonPoints >= 120) return 4;
            return UnityEngine.Random.Range(5, 15);
        }

        static readonly int[] POINTS_TABLE = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
        int PointsFor(int position) =>
            position > 0 && position <= POINTS_TABLE.Length ? POINTS_TABLE[position - 1] : 0;

        string GenerateDriverCode(string name)
        {
            if (string.IsNullOrEmpty(name)) return "GHO";
            string code = name.ToUpper();
            return code.Length >= 3 ? code.Substring(0, 3) : (code + "GHO").Substring(0, 3);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  OBJECTIVES MANAGER
    // ═══════════════════════════════════════════════════════════════════════

    public class ObjectivesManager
    {
        private readonly DriverCareerManager _career;

        public ObjectivesManager(DriverCareerManager career) => _career = career;

        public void GenerateSeasonObjectives(DriverProfile profile)
        {
            _career.CurrentSeason.objectives.Clear();

            bool isF1 = profile.tier == SeriesTier.Formula1;

            // Primary — championship-affecting
            Add("season_wins",
                $"Score {(isF1 ? 3 : 2)} race wins",
                isPrimary: true, rep: 20f, sp: 50);

            Add("beat_teammate",
                "Out-qualify and out-score your teammate all season",
                isPrimary: true, rep: 15f, sp: 30);

            Add("top5_championship",
                $"Finish top {(isF1 ? 5 : 3)} in the championship",
                isPrimary: true, rep: 25f, sp: 60);

            // Secondary
            Add("podiums",
                $"Score {(isF1 ? 8 : 6)} podiums",
                isPrimary: false, rep: 10f, sp: 20);

            Add("fastest_laps",
                $"Set {(isF1 ? 3 : 2)} fastest laps",
                isPrimary: false, rep: 5f, sp: 10);

            Add("no_dnf_streak",
                "Complete 5 races without retiring",
                isPrimary: false, rep: 8f, sp: 15);
        }

        public void CheckRaceObjectives(RaceResult result, DriverRaceWeekend weekend, DriverProfile profile)
        {
            foreach (var obj in _career.CurrentSeason.objectives)
            {
                if (obj.isCompleted) continue;

                switch (obj.id)
                {
                    case "season_wins"   when result.finishPosition == 1:
                        obj.currentProgress++;
                        TryComplete(obj, profile);
                        break;
                    case "podiums"       when result.finishPosition <= 3:
                        obj.currentProgress++;
                        TryComplete(obj, profile);
                        break;
                    case "fastest_laps"  when result.hasFastestLap:
                        obj.currentProgress++;
                        TryComplete(obj, profile);
                        break;
                    case "no_dnf_streak" when !result.retired:
                        obj.currentProgress++;
                        TryComplete(obj, profile);
                        break;
                    case "no_dnf_streak" when result.retired:
                        obj.currentProgress = 0;   // reset streak
                        break;
                }
            }
        }

        public void EvaluateEndOfSeason(DriverProfile profile, int championshipPos)
        {
            foreach (var obj in _career.CurrentSeason.objectives)
            {
                if (obj.isCompleted) continue;

                switch (obj.id)
                {
                    case "top5_championship":
                        if (championshipPos <= 5) TryComplete(obj, profile);
                        break;
                    case "beat_teammate":
                        // Simplified: if teamRelationship stayed high we'll assume player
                        // won the internal battle (proper check needs ChampionshipManager)
                        if (profile.teamRelationship >= 60f) TryComplete(obj, profile);
                        break;
                }
            }
        }

        void TryComplete(SeasonObjective obj, DriverProfile profile)
        {
            if (obj.isCompleted) return;
            if (obj.currentProgress < obj.targetProgress) return;

            obj.isCompleted = true;
            profile.reputationScore   = Mathf.Min(profile.reputationScore   + obj.reputationReward, 100f);
            profile.skillPointsSpendable += obj.skillPointReward;

            _career.OnObjectiveCompleted?.Invoke(obj);
            _career.OnNewspaper?.Invoke($"✅ Objective complete: {obj.description}");

            Debug.Log($"[Objectives] Completed: {obj.id}");
        }

        void Add(string id, string desc, bool isPrimary, float rep, int sp, int target = 1)
        {
            _career.CurrentSeason.objectives.Add(new SeasonObjective
            {
                id              = id,
                description     = desc,
                isPrimary       = isPrimary,
                reputationReward = rep,
                skillPointReward = sp,
                targetProgress  = target,
                currentProgress = 0,
                isCompleted     = false
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RIVAL ENGINE
    // ═══════════════════════════════════════════════════════════════════════

    public class RivalEngine
    {
        private readonly DriverCareerManager _career;
        private List<RivalEntry>             _rivals = new();
        private RivalEntry                   _primaryRival;

        public List<RivalEntry> Rivals => _rivals;

        public RivalEngine(DriverCareerManager career) => _career = career;

        public void InitialiseRivals(DriverProfile profile)
        {
            _rivals.Clear();

            // Seed three rivals appropriate to the tier
            var seeds = profile.tier switch
            {
                SeriesTier.Formula3 => new[] { ("Alex Varner","VRN"),("Marco Santi","SAN"),("Lee Zhang","ZHA") },
                SeriesTier.Formula2 => new[] { ("Kai Hoffmann","HOF"),("Rui Delgado","DEL"),("Sam Okafor","OKA") },
                _                   => new[] { ("Viktor Rasmus","RAS"),("Carlos Nieto","NIE"),("Pierre Fontaine","FON") }
            };

            bool first = true;
            foreach (var (name, code) in seeds)
            {
                var rival = new RivalEntry
                {
                    rivalName        = name,
                    rivalCode        = code,
                    intensity        = first ? RivalIntensity.Primary : RivalIntensity.Secondary,
                    relationshipScore = 50f
                };
                _rivals.Add(rival);
                if (first) { _primaryRival = rival; first = false; }
            }

            Debug.Log($"[Rivals] Initialised {_rivals.Count} rivals for {profile.tier}");
        }

        public void CheckRoundRivalEvent(int round)
        {
            if (_primaryRival == null) return;

            // 30 % chance of a rival event each round
            if (UnityEngine.Random.value > 0.3f) return;

            var types = new[] {
                RivalEventType.Overtake, RivalEventType.BattleOnTrack,
                RivalEventType.TeamRadioComment, RivalEventType.MediaSpat
            };

            var evt = new RivalEntry.Event
            {
                round     = round,
                eventType = types[UnityEngine.Random.Range(0, types.Length)]
            };

            _primaryRival.events.Add(evt);
            _career.OnRivalEvent?.Invoke(_primaryRival);
        }

        public void ProcessRaceResult(RaceResult result)
        {
            if (_primaryRival == null) return;

            // Simulate rival finish (within 3 positions for drama)
            int rivalFinish = Mathf.Clamp(result.finishPosition + UnityEngine.Random.Range(-2, 4), 1, 20);
            bool playerAhead = result.finishPosition < rivalFinish;

            _primaryRival.relationshipScore += playerAhead ? -5f : 3f;  // rivalry intensifies if you beat them
            _primaryRival.relationshipScore  = Mathf.Clamp(_primaryRival.relationshipScore, 0f, 100f);
        }

        public void ProvokePrimaryRival()
        {
            if (_primaryRival == null) return;
            _primaryRival.relationshipScore = Mathf.Max(_primaryRival.relationshipScore - 15f, 0f);
            _primaryRival.intensity = RivalIntensity.Nemesis;
            _career.OnRivalEvent?.Invoke(_primaryRival);
        }

        public void EvolveRivals(int playerChampPos)
        {
            foreach (var rival in _rivals)
            {
                // If player finishes above rivals, they become more hostile
                if (playerChampPos <= 3)
                    rival.relationshipScore = Mathf.Max(rival.relationshipScore - 10f, 0f);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SUPPORTING DATA TYPES  (new in Compartment 2)
    // ═══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class DriverCreationPayload
    {
        public string         driverName        = "Player";
        public string         nationality       = "Unknown";
        public SeriesTier     startingTier      = SeriesTier.Formula3;
        public DriverArchetype archetype        = DriverArchetype.Balanced;
        public string         startingTeam      = "";
        public int            raceNumber        = 0;
        public float          startingReputation = 30f;
    }

    [Serializable]
    public class RivalEntry
    {
        public string        rivalName;
        public string        rivalCode;
        public RivalIntensity intensity;
        public float         relationshipScore; // 0 = nemesis, 100 = respect
        public List<Event>   events = new();

        [Serializable]
        public class Event
        {
            public int            round;
            public RivalEventType eventType;
        }
    }

    [Serializable]
    public class MediaEvent
    {
        public MediaEventType    type;
        public string            question;
        public List<MediaChoice> choices = new();
    }

    [Serializable]
    public class MediaChoice
    {
        public string label;
        public string headline;
        public float  reputationDelta;
        public float  teamRelationshipDelta;
        public float  fanbaseDelta;
        public bool   antagRival;
    }

    // ── Supporting enums ──────────────────────────────────────────────────

    public enum DriverArchetype  { Balanced, Overtaker, Qualifier, TyreWhisperer }
    public enum TrainingIntensity { Light, Normal, Intense }
    public enum RivalIntensity   { Secondary, Primary, Nemesis }
    public enum RivalEventType   { Overtake, BattleOnTrack, TeamRadioComment, MediaSpat, PublicChallenge }
    public enum MediaEventType   { CareerStart, PreRace, PostRace, SeasonEnd, ContractNews, RivalEvent }
}