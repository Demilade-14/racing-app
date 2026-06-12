using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ENUMS
    // ═══════════════════════════════════════════════════════════════════════
    public enum CareerModeType         { Driver, Manager }
    public enum CareerPhase            { PreSeason, RaceWeekend, PostSeason, TransferWindow }
    public enum SessionPhase           { FP1, FP2, FP3, Qualifying, Race }
    public enum ObjectiveStatus        { Pending, Met, Failed }
    public enum AccoladeRarity         { Bronze, Silver, Gold, Legendary }
    public enum DriverIconType         { Custom, Icon, RealDriver }
    public enum TeamRelationship       { Poor, Neutral, Good, Excellent }
    public enum MidSeasonTransferReason{ PoorForm, TeamDissatisfaction, BetterOffer }
    public enum SeriesTier             { Formula3, Formula2, Formula1 }
    public enum CircuitType            { Permanent, Street, SemiStreet }
    public enum CircuitLighting        { Standard, Cinematic, Night }
    public enum DeploymentMode         { Harvest, Balanced, Attack }
    public enum TyreCompound           { Soft, Medium, Hard, Inter, Wet }
    public enum EngineManufacturer     { Mercedes, Ferrari, RedBull, Renault, Honda, Audi, Cadillac }
    public enum InterestLevel          { None, Cold, Warm, Hot }
    public enum LegacyTier             { Rookie, SolidPro, RaceWinner, Champion, Legend, GOAT }
    public enum NegotiationResult      { Accept, CounterOffer, Reject }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER PROFILE  —  persistent player identity (EXTENDED for 2026)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverProfile
    {
        // ── Identity ──────────────────────────────────────────────────────
        public string         profileId            = Guid.NewGuid().ToString("N");
        public string         driverName;
        public string         driverCode;           // 3-letter e.g. "HAR"
        public int            raceNumber;
        public string         nationality;
        public int            age                  = 19;
        public DriverIconType iconType             = DriverIconType.Custom;
        public string         iconBaseName;

        // ── Core stats (0-99) ─────────────────────────────────────────────
        public int pace;
        public int awareness;
        public int racecraft;
        public int experience;

        // Derived OVR
        public int OVR => Mathf.RoundToInt(
            pace        * 0.30f +
            racecraft   * 0.28f +
            awareness   * 0.22f +
            experience  * 0.20f);

        // ── Reputation & relationship ─────────────────────────────────────
        public float          reputation            = 10f;
        public string         reputationTier        = "Rookie"; // Rookie/Prospect/Established/Star/Legend
        public TeamRelationship teamRelationship    = TeamRelationship.Neutral;
        public float          teamRelationshipScore = 50f;

        // ── Stat XP ───────────────────────────────────────────────────────
        public float paceXP;
        public float awarenessXP;
        public float racecraftXP;
        public float experienceXP;
        public int   skillPoints;

        // ── Career metadata ────────────────────────────────────────────────
        public SeriesTier tier          = SeriesTier.Formula2;
        public int        season        = 1;
        public string     currentTeam;
        public int        seatNumber    = 2;
        public bool       hasNumberOne;

        // ── Financial ─────────────────────────────────────────────────────
        public float          balance;
        public DriverContract activeContract;

        // ── Season stats ──────────────────────────────────────────────────
        public int seasonPoints;
        public int seasonWins;
        public int seasonPodiums;
        public int seasonPoles;
        public int seasonFastestLaps;
        public int seasonDNFs;
        public int seasonOvertakes;
        public int driverChampionshipPos;

        // ── Career totals ──────────────────────────────────────────────────
        public int careerRaces;
        public int careerWins;
        public int careerPodiums;
        public int careerPoles;
        public int careerPoints;
        public int careerChampionships;
        public int careerFastestLaps;

        // ── Collections ───────────────────────────────────────────────────
        public List<Accolade>        accolades       = new();
        public List<DriverRival>     rivals          = new();
        public List<SeasonObjective> objectives      = new();
        public List<DriverContract>  contractHistory = new();
        public List<SeasonRecord>    seasonHistory   = new();

        // ── 2026: ERS / Active Aero preferences ───────────────────────────
        public DeploymentMode preferredDeployMode = DeploymentMode.Balanced;
        public float          ersManagementSkill  = 50f; // affects battery efficiency
        public float          tyreManagementSkill = 50f; // affects tyre deg with high ERS

        // ── Legacy goal tracking ───────────────────────────────────────────
        public LegacyTier legacyTier = LegacyTier.Rookie;

        // ── Computed reputation tier ───────────────────────────────────────
        public void UpdateReputationTier()
        {
            reputationTier = reputation switch
            {
                < 20f  => "Rookie",
                < 40f  => "Prospect",
                < 60f  => "Established",
                < 80f  => "Star",
                _      => "Legend"
            };
        }

        // ── Computed legacy tier ───────────────────────────────────────────
        public void UpdateLegacyTier()
        {
            legacyTier = careerWins switch
            {
                0          => LegacyTier.Rookie,
                < 5        => LegacyTier.SolidPro,
                < 15       => LegacyTier.RaceWinner,
                < 30       when careerChampionships < 1 => LegacyTier.RaceWinner,
                _          when careerChampionships >= 1 && careerChampionships < 3 => LegacyTier.Champion,
                _          when careerChampionships >= 3 && careerChampionships < 7 => LegacyTier.Legend,
                _          => LegacyTier.GOAT
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SEASON RECORD  —  summary of one completed season
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class SeasonRecord
    {
        public int    season;
        public int    finalPosition;
        public int    points;
        public int    wins;
        public int    podiums;
        public int    poles;
        public int    fastestLaps;
        public int    dnfs;
        public string teamName;
        public bool   wonChampionship;
        public int    teammateFinishPos;   // end-of-season teammate comparison
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER ICON  —  legendary drivers the player can pick
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverIcon
    {
        public string name;
        public string code;
        public string nationality;
        public int    peakSeason;
        public int    pace;
        public int    awareness;
        public int    racecraft;
        public int    experience;
        public float  startingReputation;
        public string legacyDescription;
        public bool   canBeAISigned;        // 2026: AI teams can sign icons
        public bool   isEnabled;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER CONTRACT
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverContract
    {
        public string teamName;
        public SeriesTier tier;
        public float  annualSalary;
        public float  raceBonus;
        public float  podiumBonus;
        public float  winBonus;
        public float  championshipBonus;
        public int    durationSeasons;
        public int    seasonsRemaining;
        public bool   hasNumberOneClause;
        public bool   hasVetoOnTeammate;
        public float  performanceReleaseThreshold;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CONTRACT OFFER  —  team sends this to the player
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class ContractOffer
    {
        public string teamName;
        public SeriesTier tier;
        public float  offeredSalary;
        public float  podiumBonus;
        public float  winBonus;
        public int    seasons;
        public bool   offersNumberOne;
        public float  teamInterestScore;
        public float  minReputationRequired;
        public InterestLevel interestLevel;

        // Player negotiation counters
        public float  playerCounterSalary;
        public bool   playerRequestsNumberOne;

        // Team budget — used internally for negotiation evaluation
        [HideInInspector] public float teamBudgetAllocation;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SEASON OBJECTIVE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class SeasonObjective
    {
        public string          id;
        public string          description;
        public ObjectiveStatus status           = ObjectiveStatus.Pending;
        public float           reputationReward;
        public float           salaryBonusReward;
        public int             skillPointReward;
        public bool            isPrimary;
        public float           progressValue;    // e.g. current championship pos
        public float           targetValue;      // e.g. top-8 = 8
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ACCOLADE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class Accolade
    {
        public string         id;
        public string         title;
        public string         description;
        public AccoladeRarity rarity;
        public float          reputationBonus;
        public DateTime       earnedDate;
        public int            earnedSeason;
        public int            earnedRound;
        public string         category;   // "Race" | "Season" | "Career" | "Legendary"
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER RIVAL
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverRival
    {
        public string rivalName;
        public string rivalTeam;
        public int    pointsDelta;
        public int    headToHeadWins;
        public int    headToHeadLosses;
        public float  intensityScore;
        public bool   isTeammate;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  OFF-TRACK EVENT CARD
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class EventCard
    {
        public string            id;
        public string            headline;
        public string            bodyText;
        public string            category;   // "Media"|"Sponsor"|"TeamDispute"|"Training"
        public List<EventChoice> choices     = new();
    }

    [Serializable]
    public class EventChoice
    {
        public string choiceText;
        public float  reputationDelta;
        public float  teamRelationshipDelta;
        public float  balanceDelta;
        public string statToBoost;
        public float  statXPReward;
        public string outcomeDescription;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PRACTICE PROGRAM
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class PracticeProgram
    {
        public string programId;
        public string name;
        public string statRewarded;
        public float  xpReward;
        public int    skillPointReward;
        public bool   completed;
        public float  score;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER RACE WEEKEND
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverRaceWeekend
    {
        public int    round;
        public string circuitName;
        public bool   fp1Done, fp2Done, fp3Done, qualiDone;
        public int    gridPosition;
        public int    qualiEliminated;
        public RaceResult result;
        public List<PracticeProgram> practicePrograms = new();
        public bool   IsComplete => result != null;

        public int    overtakesThisRace;
        public bool   achievedFastestLap;
        public float  consistencyScore;

        // ── 2026 race weekend data ─────────────────────────────────────────
        public float  avgBatteryDeployPercent;   // average ERS deploy % across race
        public int    overtakeModeActivations;   // how many times player used overtake mode
        public bool   activeAeroFaultOccurred;   // mechanical failure flag
        public TyreCompound startingTyre;
        public TyreCompound finalTyre;
        public int    tyreStops;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CHAMPIONSHIP ENTRY
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class ChampionshipEntry
    {
        public string driverName;
        public string teamName;
        public int    points;
        public int    wins;
        public int    podiums;
        public int    poles;
        public int    fastestLaps;
        public int    position;
        public bool   isPlayer;
        public bool   isRetired;
        public int[]  raceResults = new int[24]; // 2026 has up to 24 rounds
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  2026 — CAR DATA
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class Car2026Data
    {
        // Active aerodynamics
        public bool  hasActiveAero             = true;
        public float aeroDragReductionFactor   = 0.23f;  // % drag cut when aero opens
        public float aeroDownforceLossFactor   = 0.35f;  // % downforce lost when open
        public float aeroDeploySpeedThreshold  = 280f;   // km/h threshold for auto-deploy
        public float aeroSteeringThreshold     = 15f;    // degrees of steering that retracts

        // ERS / Hybrid
        public float ersCapacityMJ             = 8.5f;   // 2026 regulation limit
        public float motorPowerKW              = 350f;   // electric motor output
        public float combustionPowerKW         = 400f;   // ICE contribution
        public float batteryRecoveryPerLap     = 1.8f;   // MJ recovered per average lap
        public float batteryDeployAttackPerLap = 2.4f;   // MJ used in Attack mode per lap

        // Overtake mode
        public bool  hasOvertakeMode           = true;
        public float overtakeBoostKW           = 200f;
        public float overtakeBoostDurationSec  = 5f;
        public float overtakeCooldownSec       = 20f;
        public float overtakeBatteryThreshold  = 0.30f; // must have > 30% battery

        // Current runtime state (not saved — reset each race)
        [NonSerialized] public float  currentBatteryPercent = 100f;
        [NonSerialized] public bool   overtakeModeActive;
        [NonSerialized] public float  overtakeDurationRemaining;
        [NonSerialized] public float  overtakeCooldownRemaining;
        [NonSerialized] public bool   aeroDeployed;
        [NonSerialized] public DeploymentMode currentDeployMode = DeploymentMode.Balanced;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  2026 — TEAM DATA  (extends existing TeamData concept)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class TeamData2026
    {
        public string  teamName;
        public string  shortName;
        public string  primaryColorHex;
        public string  secondaryColorHex;
        public float   performanceRating;      // 0-100
        public float   carRating;
        public float   reliabilityRating;      // 2026: active aero adds failure risk
        public float   annualBudget;
        public bool    isNew2026Entry;
        public bool    is11thTeam;
        public EngineManufacturer engineSupplier;
        public float   budgetAllocationPerSeat; // used in contract offer generation
        public Car2026Data carSpecs = new();

        // ── Preset: Audi F1 Team ───────────────────────────────────────────
        public static TeamData2026 Audi() => new()
        {
            teamName             = "Audi F1 Team",
            shortName            = "AUDI",
            primaryColorHex      = "#E8002D",
            secondaryColorHex    = "#000000",
            performanceRating    = 72f,
            carRating            = 70f,
            reliabilityRating    = 68f,
            annualBudget         = 320_000_000f,
            isNew2026Entry       = true,
            is11thTeam           = false,
            engineSupplier       = EngineManufacturer.Audi,
            budgetAllocationPerSeat = 18_000_000f
        };

        // ── Preset: Cadillac F1 Team ───────────────────────────────────────
        public static TeamData2026 Cadillac() => new()
        {
            teamName             = "Cadillac F1 Team",
            shortName            = "CAD",
            primaryColorHex      = "#1B3B8A",
            secondaryColorHex    = "#FFFFFF",
            performanceRating    = 65f,
            carRating            = 63f,
            reliabilityRating    = 62f,
            annualBudget         = 280_000_000f,
            isNew2026Entry       = true,
            is11thTeam           = true,
            engineSupplier       = EngineManufacturer.Cadillac,
            budgetAllocationPerSeat = 12_000_000f
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  2026 — CIRCUIT DATA  (extends existing CircuitData concept)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class CircuitData2026
    {
        public string         name;
        public string         shortName;
        public string         country;
        public string         countryCode;
        public float          lapLengthKm;
        public int            numberOfCorners;
        public int            drsZones;
        public CircuitType    circuitType;
        public CircuitLighting lighting;
        public float          lapRecord;
        public int            raceRound;
        public float          safetyCárProbability;    // 0-1
        public float          overtakeOpportunityRating; // 0-1, how easy to overtake
        public float          tyreWearMultiplier;       // higher = faster tyre deg

        // ── Preset: Madrid "MADRING" ───────────────────────────────────────
        public static CircuitData2026 Madrid() => new()
        {
            name                     = "Madrid Street Circuit",
            shortName                = "MADRING",
            country                  = "Spain",
            countryCode              = "ES",
            lapLengthKm              = 5.47f,
            numberOfCorners          = 20,
            drsZones                 = 3,
            circuitType              = CircuitType.Street,
            lighting                 = CircuitLighting.Cinematic,
            lapRecord                = 0f,
            raceRound                = 9,
            safetyCárProbability     = 0.35f,
            overtakeOpportunityRating = 0.68f,
            tyreWearMultiplier       = 1.25f    // street circuit = higher tyre wear
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEAM INTEREST ENTRY  —  used by TransferMarket to show player
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class TeamInterestEntry
    {
        public string        teamName;
        public InterestLevel interest;
        public float         estimatedSalaryMin;
        public float         estimatedSalaryMax;
        public bool          seatAvailable;
        public string        availableRole;   // "No.1 Driver" | "No.2 Driver"
        public float         performanceRating;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER CAREER SAVE  —  full serializable state (EXTENDED)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverCareerSave
    {
        public string           saveId        = Guid.NewGuid().ToString("N");
        public string           saveSlotName;
        public CareerModeType   modeType      = CareerModeType.Driver;
        public DriverProfile    profile       = new();
        public CareerPhase      phase         = CareerPhase.PreSeason;
        public int              currentRound  = 0;
        public List<DriverRaceWeekend> calendar  = new();
        public List<EventCard>  pendingEvents = new();
        public DateTime         lastSaved;

        // Legacy goals
        public int  legacyWinsTarget       = 50;
        public int  legacyChampTarget      = 3;
        public int  legacyPolesTarget      = 40;
        public bool legacyCompleted        = false;

        // 2026 additions
        public bool  startedInF2           = true;
        public bool  iconsEnabledOnGrid    = true;    // allow AI to sign Driver Icons
        public bool  cadillacOnGrid        = true;    // 11th team toggle
        public bool  audiOnGrid            = true;
        public string gameVersion          = "2026";
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER SLOT HEADER  (shown on selection screen)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class CareerSlotHeader
    {
        public string         saveId;
        public string         saveSlotName;
        public CareerModeType modeType;
        public string         driverOrTeamName;
        public string         currentTeam;
        public int            season;
        public int            careerWins;
        public int            careerPoints;
        public SeriesTier     tier;
        public DateTime       lastSaved;
        public string         gameVersion;     // "2026" tag shown on slot card
    }
}