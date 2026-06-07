using System;
using System.Collections.Generic;
using UnityEngine;

namespace RacingGame.Data
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ENUMS
    // ═══════════════════════════════════════════════════════════════════════
    public enum TireCompound       { Soft, Medium, Hard, Inter, Wet }
    public enum WeatherCondition   { Dry, Cloudy, LightRain, HeavyRain, Storm }
    public enum EngineMode         { Eco, Normal, Push, Overtake }
    public enum DamageLevel        { None, Minor, Moderate, Severe, DNF }
    public enum AIPersonality      { Aggressive, Balanced, Conservative, YoungTalent, Veteran }
    public enum AIState            { FollowLine, ChaseLeader, DefendPosition, PitPlanning, Recover, ManageGap, SafetyCar, VSC, OutLap, WarmTires }
    public enum LobbyState         { Waiting, Loading, Racing, Results }
    public enum SessionType        { Practice1, Practice2, Qualifying, Race, SprintRace }
    public enum FlagStatus         { Green, Yellow, SafetyCar, VirtualSafetyCar, Red, Chequered }
    public enum SeriesTier         { Formula3, Formula2, Formula1 }
    public enum PenaltyType        { None, DriveThrough, StopAndGo, TimeAddition, Disqualified }
    public enum CameraMode         { Cockpit, Chase, TV, Helicopter, Track }
    public enum GameMode           { Career, QuickRace, TimeTrial, Multiplayer, Practice }
    public enum CareerType         { Driver, Manager }

    // ═══════════════════════════════════════════════════════════════════════
    //  ERS DEPLOYMENT MODE
    // ═══════════════════════════════════════════════════════════════════════
    public enum ERSMode { Harvest, Balanced, Overtake, Qualifying }

    // ═══════════════════════════════════════════════════════════════════════
    //  TIRE  (dual-layer thermal model)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class TireState
    {
        public TireCompound compound;

        // ── Dual-layer temperatures ───────────────────────────────────────
        /// <summary>Rubber surface layer (0-5 mm). Heats fast, cools fast.</summary>
        public float surfaceTemp;       // degrees C
        /// <summary>Carcass / core layer. Heats and cools slowly.</summary>
        public float coreTemp;          // degrees C
        /// <summary>Legacy single-value used by existing grip formula (= surfaceTemp).</summary>
        public float temperature => surfaceTemp;

        // ── Mechanical state ──────────────────────────────────────────────
        public float wearPercent;       // 0-100
        public float slipAngle;         // degrees
        public float slipRatio;         // longitudinal slip  (-1 to +1)
        public float load;              // N (vertical load on this corner)
        public float surfaceGrip;       // local track grip modifier  0-1
        public int   lapAge;            // laps on this set

        // ── Degradation flags ─────────────────────────────────────────────
        /// <summary>Graining severity 0-1. Cold surface on soft rubber causes micro-tears.
        /// Increases when surfaceTemp is below OptimalTempMin and lateral load is high.</summary>
        public float grainingLevel;
        /// <summary>Blistering severity 0-1. Overheated core cooking the rubber.
        /// Increases when coreTemp exceeds BlisterThreshold.</summary>
        public float blisteringLevel;

        // ── Tyre pressure ─────────────────────────────────────────────────
        /// <summary>Base cold pressure (PSI). Core temp raises it dynamically.</summary>
        public float coldPressurePSI    = 23f;
        /// <summary>Live pressure. Higher pressure = smaller contact patch = less grip.</summary>
        public float currentPressurePSI;

        // ── Dirty-air influence (written by AerodynamicWakeSystem) ────────
        /// <summary>Extra slip angle imposed by dirty air turbulence (degrees).</summary>
        public float dirtyAirSlipDelta;
        /// <summary>Extra surface-temp heat added per tick by turbulence friction.</summary>
        public float dirtyAirHeatDelta;

        // ── Compound constants ────────────────────────────────────────────
        public float BaseGrip => compound switch
        {
            TireCompound.Soft   => 1.00f,
            TireCompound.Medium => 0.88f,
            TireCompound.Hard   => 0.78f,
            TireCompound.Inter  => 0.82f,
            TireCompound.Wet    => 0.75f,
            _ => 0.88f
        };

        public float WearRate => compound switch
        {
            TireCompound.Soft   => 1.00f,
            TireCompound.Medium => 0.65f,
            TireCompound.Hard   => 0.48f,
            TireCompound.Inter  => 0.55f,
            TireCompound.Wet    => 0.45f,
            _ => 0.65f
        };

        public float OptimalTempMin => compound switch
        {
            TireCompound.Soft   => 80f,
            TireCompound.Medium => 70f,
            TireCompound.Hard   => 60f,
            TireCompound.Inter  => 50f,
            TireCompound.Wet    => 40f,
            _ => 70f
        };

        public float OptimalTempMax => compound switch
        {
            TireCompound.Soft   => 100f,
            TireCompound.Medium =>  95f,
            TireCompound.Hard   =>  90f,
            TireCompound.Inter  =>  70f,
            TireCompound.Wet    =>  60f,
            _ => 90f
        };

        /// <summary>Core temperature that triggers blistering.</summary>
        public float BlisterThreshold => compound switch
        {
            TireCompound.Soft   => 105f,
            TireCompound.Medium => 115f,
            TireCompound.Hard   => 125f,
            TireCompound.Inter  =>  90f,
            TireCompound.Wet    =>  80f,
            _ => 110f
        };

        // ── Convenience init ──────────────────────────────────────────────
        public void InitTemps(float ambient)
        {
            surfaceTemp         = ambient + 10f;
            coreTemp            = ambient + 5f;
            currentPressurePSI  = coldPressurePSI;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  VEHICLE STATE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class VehicleState
    {
        public Vector3    position;
        public Vector3    velocity;
        public Vector3    angularVelocity;
        public Quaternion rotation;
        public float      speed;            // km/h
        public float      speedMs;          // m/s
        public int        gear;             // 0=neutral, 1-8
        public float      rpm;              // 0-15000
        public float      throttle;         // 0-1
        public float      brake;            // 0-1
        public float      steering;         // -1 to 1
        public float      clutch;           // 0-1
        public float      fuelLoad;         // litres

        // ── ERS (expanded) ────────────────────────────────────────────────
        public float      ersDeploy;        // 0-1 (legacy deploy fraction)
        public float      ersEnergy;        // 0-1 (legacy stored fraction)
        public float      ersSoC;           // 0-100 % State of Charge
        public ERSMode    ersMode;          // Harvest / Balanced / Overtake / Qualifying
        public float      ersOvertakeTimer; // seconds remaining in Overtake burst

        // ── Aero wake ─────────────────────────────────────────────────────
        public bool       inDirtyAir;           // true if within wake cone of car ahead
        public float      dirtyAirDownforceLoss; // 0-1 fraction of downforce lost
        public float      slipstreamSpeedBonus;  // m/s extra top speed
        public float      gapToCarAheadSec;      // seconds gap to car directly ahead

        public bool       drsActive;
        public bool       drsEligible;
        public EngineMode engineMode;
        public TireState[] tires = new TireState[4];
        public ComponentDamage damage = new();

        // ── Telemetry ─────────────────────────────────────────────────────
        public float gLongitudinal;
        public float gLateral;
        public float downforce;         // N
        public float dragForce;         // N
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DAMAGE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class ComponentDamage
    {
        public float frontWing;     // 0–100
        public float rearWing;
        public float suspension;
        public float engine;
        public float brakes;
        public float floor;
        public float bargeboard;
        public bool  hasPuncture;

        public bool IsDNF => engine >= 100f || suspension >= 100f || hasPuncture;

        public DamageLevel OverallLevel
        {
            get
            {
                float max = Mathf.Max(frontWing, rearWing, suspension, engine, brakes);
                if (max >= 100f) return DamageLevel.DNF;
                if (max >= 60f)  return DamageLevel.Severe;
                if (max >= 30f)  return DamageLevel.Moderate;
                if (max >= 10f)  return DamageLevel.Minor;
                return DamageLevel.None;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CAR SETUP  –  full F1-spec data structure
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class CarSetup
    {
        // ── Aerodynamics ──────────────────────────────────────────────────
        [Range(1, 11)] public int   frontWingAngle      = 5;
        [Range(1, 11)] public int   rearWingAngle       = 5;

        // ── Suspension ────────────────────────────────────────────────────
        [Range(1, 11)] public int   suspensionStiffnessFront = 5;
        [Range(1, 11)] public int   suspensionStiffnessRear  = 5;
        [Range(1, 11)] public int   antiRollBarFront    = 5;
        [Range(1, 11)] public int   antiRollBarRear     = 5;
        /// <summary>Ride height 1=low (fast, grounding risk) to 11=high (slower, safe)</summary>
        [Range(1, 11)] public int   rideHeightFront     = 3;
        [Range(1, 11)] public int   rideHeightRear      = 4;
        /// <summary>Camber: negative = tilt top inward. More negative = better cornering, more wear.</summary>
        [Range(-30, -10)] public int camberFront        = -20;  // tenths of a degree × 10
        [Range(-20, -5)]  public int camberRear         = -12;
        /// <summary>Toe: positive = toe-out (stability), negative = toe-in (agility)</summary>
        [Range(-5, 5)]    public int toeFront           =  5;   // tenths of a degree × 10
        [Range(5, 15)]    public int toeRear            = 10;

        // ── Differential ─────────────────────────────────────────────────
        /// <summary>% lock when on throttle. High = stability, Low = agility</summary>
        [Range(50, 100)] public int onThrottleDiff      = 75;
        /// <summary>% lock when off throttle. High = understeer entry, Low = rotation</summary>
        [Range(50, 100)] public int offThrottleDiff     = 60;

        // ── Brakes ───────────────────────────────────────────────────────
        /// <summary>Brake bias: 1.0 = full front, 0.0 = full rear. F1 typical ~0.54-0.60</summary>
        [Range(0.48f, 0.70f)] public float brakeBias   = 0.57f;
        /// <summary>Brake migration: how much bias shifts toward rear as speed bleeds off (0=none, 1=max).
        /// Prevents rear lock under heavy trail-braking.</summary>
        [Range(0f, 1f)]       public float brakeMigration = 0.35f;
        /// <summary>Brake duct size: 1=smallest (low drag), 11=largest (most cooling)</summary>
        [Range(1, 11)] public int   brakeDuctFront     = 4;
        [Range(1, 11)] public int   brakeDuctRear      = 3;

        // ── Assistance ────────────────────────────────────────────────────
        [Range(0f, 1f)] public float tractionControl   = 0.5f;
        [Range(0f, 1f)] public float abs               = 0.5f;

        // ── Tyre pressures (PSI) ──────────────────────────────────────────
        [Range(20f, 28f)] public float tyrePressureFrontLeft  = 23f;
        [Range(20f, 28f)] public float tyrePressureFrontRight = 23f;
        [Range(20f, 28f)] public float tyrePressureRearLeft   = 21.5f;
        [Range(20f, 28f)] public float tyrePressureRearRight  = 21.5f;

        // ── Helpers ───────────────────────────────────────────────────────
        /// <summary>Returns effective brake bias at a given speed (migration applied).</summary>
        public float EffectiveBrakeBias(float speedKph)
        {
            float migrationFactor = Mathf.Clamp01(1f - speedKph / 300f);
            return brakeBias - brakeMigration * migrationFactor * 0.08f;
        }

        /// <summary>Cornering stiffness index (0-1) from combined suspension/ARB settings.</summary>
        public float CorneringStiffness =>
            ((suspensionStiffnessFront + suspensionStiffnessRear) / 22f
           + (antiRollBarFront + antiRollBarRear)          / 22f) * 0.5f;

        /// <summary>Downforce balance (positive = understeer tendency).</summary>
        public float AeroBalance =>
            (frontWingAngle - rearWingAngle) / 10f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TRACK LIMIT ZONE  (per-corner data defined in circuit)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class TrackLimitZone
    {
        public int    waypointIndex;       // which racing-line waypoint
        public float  timeGainIfCut;       // seconds gained by taking the shortcut
        public float  detectionRadius;     // metres outside line that triggers penalty
        public bool   cameraMonitored;     // true = penalty more likely detected
        public int    penaltyLapsCount;    // laps within which repeat = black flag
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CHAMPIONSHIP CONTEXT  (fed to AI for risk-vs-reward decisions)
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class ChampionshipContext
    {
        public int   playerChampionshipPosition;   // 1 = leading
        public int   pointsGapToLeader;            // 0 if leading
        public int   racesRemaining;
        public bool  lastRaceBeforeTitleDecider;

        /// <summary>Risk tolerance 0-1. High when mathematically must attack.</summary>
        public float RiskTolerance
        {
            get
            {
                if (racesRemaining == 0) return 0f;
                float mustScore = Mathf.Clamp01(pointsGapToLeader / (racesRemaining * 26f));
                return Mathf.Clamp01(mustScore + (lastRaceBeforeTitleDecider ? 0.3f : 0f));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  WEATHER
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class WeatherData
    {
        public WeatherCondition condition;
        public float ambientTemp;       // °C
        public float trackTemp;         // °C
        public float rainIntensity;     // 0–1
        public float windSpeed;         // m/s
        public float windDirection;     // degrees
        public float trackWetness;      // 0–1 (dries over time)

        public float GripMultiplier => condition switch
        {
            WeatherCondition.Dry       => 1.00f,
            WeatherCondition.Cloudy    => 0.97f,
            WeatherCondition.LightRain => 0.85f,
            WeatherCondition.HeavyRain => 0.70f,
            WeatherCondition.Storm     => 0.55f,
            _ => 1.0f
        };

        public bool RequiresWetTires   => condition >= WeatherCondition.HeavyRain;
        public bool RequiresInterTires => condition == WeatherCondition.LightRain;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverStats
    {
        public string driverName;
        public string driverCode;       // e.g. "VEL"
        public int    number;           // race number
        public int    age;
        public string nationality;
        public string teamName;

        [Range(0, 100)] public int speed;
        [Range(0, 100)] public int braking;
        [Range(0, 100)] public int cornering;
        [Range(0, 100)] public int racecraft;
        [Range(0, 100)] public int consistency;
        [Range(0, 100)] public int wetWeather;
        [Range(0, 100)] public int tireManagement;

        public float OverallRating =>
            (speed + braking + cornering + racecraft + consistency) / 500f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEAM
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class TeamData
    {
        public string teamName;
        public string teamCode;         // e.g. "APX"
        public Color  primaryColor;
        public Color  secondaryColor;
        [Range(0, 100)] public int enginePower;
        [Range(0, 100)] public int reliability;
        [Range(0, 100)] public int aeroEfficiency;
        [Range(0, 100)] public int pitCrewSpeed;    // 0–100 (affects pit stop time)
        public float annualBudget;
        public List<string> driverNames = new();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CIRCUIT
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class CircuitData
    {
        public string circuitName;
        public string circuitCode;
        public string location;
        public string country;
        public float  trackLengthKm;
        public float  baseLapTimeSeconds;
        public int    totalLaps;
        public int    drsZones;
        public float  pitLaneTimeLoss;      // seconds
        public float  overtakingDifficulty; // 0–1 (1 = very hard)
        public WeatherCondition defaultWeather;
        public float  trackEvolutionRate;   // grip improvement per lap
        public Vector3[]  racingLineWaypoints;
        public float[]    waypointSpeeds;
        public int[]             drsActivationWaypoints;
        public int[]             drsDetectionWaypoints;
        public TrackLimitZone[]  trackLimitZones;
        public int               maxTrackLimitWarnings = 3;  // before auto-penalty
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE RESULT
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class RaceResult
    {
        public string playerId;
        public string driverName;
        public string teamName;
        public int    finishPosition;
        public float  totalTime;
        public float  fastestLap;
        public float[] sectorTimes   = new float[3];
        public int    pitStops;
        public bool   retired;
        public bool   hasFastestLap;
        public int    pointsEarned;
        public PenaltyType penalty;
        public float  penaltySeconds;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PIT STOP
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class PitStopData
    {
        public int         lap;
        public float       inTime;          // race time on entry
        public float       outTime;         // race time on exit
        public float       stopDuration;    // seconds stationary
        public TireCompound newCompound;
        public bool        repairFrontWing;
        public bool        repairSuspension;

        public float TotalTimeLoss => outTime - inTime;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  QUALIFYING RESULT
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class QualifyingResult
    {
        public string driverName;
        public string teamName;
        public int    gridPosition;
        public float  q1Time;
        public float  q2Time;
        public float  q3Time;
        public bool   eliminated_Q1;
        public bool   eliminated_Q2;
        public TireCompound q3StartTire;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MULTIPLAYER
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class PlayerNetData
    {
        public string playerId;
        public string username;
        public string region;
        public int    skillRating    = 1500;
        public int    wins, losses, totalRaces;
        public float  bestLapTime;
    }

    [Serializable]
    public class LobbyData
    {
        public string             lobbyId;
        public string             hostId;
        public LobbyState         state;
        public string             circuit;
        public int                maxPlayers;
        public int                raceLaps;
        public WeatherCondition   weather;
        public bool               damageEnabled;
        public bool               safetyCarEnabled;
        public int                aiCount;
        public List<PlayerNetData> players = new();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NETWORK PACKET
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class VehicleUpdatePacket
    {
        public string    playerId;
        public Vector3   position;
        public Vector3   velocity;
        public Quaternion rotation;
        public byte      gear;
        public float     throttle;
        public float     brake;
        public float     steering;
        public int       damageFlags;
        public float[]   tireTemps = new float[4];
        public float[]   tireWear  = new float[4];
        public float     fuelLoad;
        public bool      drsActive;
        public long      serverTimestamp;
        public int       lapNumber;
        public float     lapTime;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE DIRECTOR STATE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class RaceDirectorState
    {
        public FlagStatus  flag             = FlagStatus.Green;
        public int         safetyCarLap     = -1;
        public bool        pitLaneOpen      = true;
        public float       safetyCarSpeed   = 80f;  // km/h
        public string      incidentMessage;
        public List<string> yellowSectors   = new();
    }
}
