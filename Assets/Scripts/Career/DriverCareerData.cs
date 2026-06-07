using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ENUMS
    // ═══════════════════════════════════════════════════════════════════════
    public enum CareerModeType      { Driver, Manager }
    public enum CareerPhase         { PreSeason, RaceWeekend, PostSeason, TransferWindow }
    public enum SessionPhase        { FP1, FP2, FP3, Qualifying, Race }
    public enum ObjectiveStatus     { Pending, Met, Failed }
    public enum AccoladeRarity      { Bronze, Silver, Gold, Legendary }
    public enum DriverIconType      { Custom, Icon }
    public enum TeamRelationship    { Poor, Neutral, Good, Excellent }
    public enum MidSeasonTransferReason { PoorForm, TeamDissatisfaction, BetterOffer }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER PROFILE  —  the persistent player identity
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverProfile
    {
        // ── Identity ──────────────────────────────────────────────────────
        public string        profileId       = Guid.NewGuid().ToString("N");
        public string        driverName;
        public string        driverCode;          // 3-letter e.g. "HAR"
        public int           raceNumber;
        public string        nationality;
        public int           age             = 19;
        public DriverIconType iconType       = DriverIconType.Custom;
        public string        iconBaseName;        // set if iconType == Icon

        // ── Core stats (0-99, F1 25 style) ───────────────────────────────
        public int pace;
        public int awareness;
        public int racecraft;
        public int experience;

        // Derived OVR shown on driver card
        public int OVR => Mathf.RoundToInt(pace * 0.30f + racecraft * 0.28f
                                         + awareness * 0.22f + experience * 0.20f);

        // ── Reputation & relationship ─────────────────────────────────────
        public float reputation        = 10f;    // 0-100, gates contract tier
        public TeamRelationship teamRelationship = TeamRelationship.Neutral;
        public float teamRelationshipScore = 50f; // 0-100, tracks morale

        // ── Stat XP (fills until next point is awarded) ───────────────────
        public float paceXP;
        public float awarenessXP;
        public float racecraftXP;
        public float experienceXP;
        public int   skillPoints;           // spendable points from practice programs

        // ── Career metadata ────────────────────────────────────────────────
        public SeriesTier    tier           = SeriesTier.Formula2;
        public int           season         = 1;
        public string        currentTeam;
        public int           seatNumber     = 2;    // 1 = #1 driver, 2 = #2
        public bool          hasNumberOne;

        // ── Financial ─────────────────────────────────────────────────────
        public float         balance;
        public DriverContract activeContract;

        // ── Season stats (reset each season) ─────────────────────────────
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
        public List<Accolade>         accolades       = new();
        public List<DriverRival>      rivals          = new();
        public List<SeasonObjective>  objectives      = new();
        public List<DriverContract>   contractHistory = new();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER ICON  —  legendary drivers the player can pick
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverIcon
    {
        public string  name;
        public string  code;
        public string  nationality;
        public int     peakSeason;      // year their stats reflect
        public int     pace;
        public int     awareness;
        public int     racecraft;
        public int     experience;
        public float   startingReputation;
        public string  legacyDescription;
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
        public float  performanceReleaseThreshold;  // rep floor — fall below = team can release
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
        public float  teamInterestScore;    // 0-100, how much team wants the player
        public float  minReputationRequired;

        // Player negotiation counters
        public float  playerCounterSalary;
        public bool   playerRequestsNumberOne;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SEASON OBJECTIVE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class SeasonObjective
    {
        public string          id;
        public string          description;
        public ObjectiveStatus status      = ObjectiveStatus.Pending;
        public float           reputationReward;
        public float           salaryBonusReward;
        public int             skillPointReward;
        public bool            isPrimary;      // primary = affects contract renewal
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ACCOLADE  (milestone achievement)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class Accolade
    {
        public string        id;
        public string        title;
        public string        description;
        public AccoladeRarity rarity;
        public float         reputationBonus;
        public DateTime      earnedDate;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER RIVAL
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverRival
    {
        public string rivalName;
        public string rivalTeam;
        public int    pointsDelta;          // their pts minus player's pts
        public int    headToHeadWins;
        public int    headToHeadLosses;
        public float  intensityScore;       // 0-1, how fierce the rivalry is
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
        public string            category;   // "Media", "Sponsor", "TeamDispute", "Training"
        public List<EventChoice> choices     = new();
    }

    [Serializable]
    public class EventChoice
    {
        public string choiceText;
        public float  reputationDelta;
        public float  teamRelationshipDelta;
        public float  balanceDelta;
        public string statToBoost;          // "pace"|"awareness"|"racecraft"|null
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
        public string name;             // "Tyre Management", "Quali Sim", "Race Pace"
        public string statRewarded;     // which stat XP this grows
        public float  xpReward;
        public int    skillPointReward;
        public bool   completed;
        public float  score;            // 0-100, set after session
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER RACE WEEKEND  (extended from existing RaceWeekend)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverRaceWeekend
    {
        public int    round;
        public string circuitName;
        public bool   fp1Done, fp2Done, fp3Done, qualiDone;
        public int    gridPosition;
        public int    qualiEliminated;      // 0=none, 1=Q1, 2=Q2, 3=Q3
        public RaceResult result;
        public List<PracticeProgram> practicePrograms = new();
        public bool   IsComplete => result != null;

        // Stats earned this weekend
        public int    overtakesThisRace;
        public bool   achievedFastestLap;
        public float  consistencyScore;     // 0-1 based on lap time variance
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CHAMPIONSHIP ENTRY  (shared between Driver & Manager modes)
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
        public int[]  raceResults = new int[23]; // finish pos per round
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER CAREER SAVE  —  full serializable state for one save slot
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverCareerSave
    {
        public string              saveId       = Guid.NewGuid().ToString("N");
        public string              saveSlotName;
        public CareerModeType      modeType     = CareerModeType.Driver;
        public DriverProfile       profile      = new();
        public CareerPhase         phase        = CareerPhase.PreSeason;
        public int                 currentRound = 0;
        public List<DriverRaceWeekend> calendar = new();
        public List<EventCard>     pendingEvents= new();
        public DateTime            lastSaved;

        // Legacy goals (multi-season)
        public int  legacyWinsTarget       = 50;
        public int  legacyChampTarget      = 3;
        public int  legacyPolesTarget      = 40;
        public bool legacyCompleted        = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER SLOT HEADER  (shown on the selection screen, no full load)
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
    }
}
