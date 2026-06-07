using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Physics;
using RacingGame.AI;

namespace RacingGame.Race
{
    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER ENTRY (one slot in the race, player or AI)
    // ═══════════════════════════════════════════════════════════════════════
    public class DriverEntry
    {
        public string      driverId;
        public string      driverName;
        public string      teamName;
        public bool        isPlayer;
        public int         currentLap;
        public float       totalRaceTime;
        public float       lastLapTime;
        public float       bestLapTime  = float.MaxValue;
        public float[]     sectorSplit  = new float[3];
        public int         currentSector;
        public float       lapStartTime;
        public int         position;
        public bool        retired;
        public bool        inPit;
        public float       pitEntry;
        public int         pitStopCount;
        public PenaltyType pendingPenalty;
        public float       penaltySeconds;
        public List<PitStopData> pitHistory = new();

        // For gap calculation
        public float       distanceRaced;    // total metres
        public float       gapAhead  = 999f;
        public float       gapBehind = 999f;

        public float GapToLeader(DriverEntry leader)
        {
            if (leader == this) return 0f;
            return leader.totalRaceTime - totalRaceTime;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  QUALIFYING SESSION
    // ═══════════════════════════════════════════════════════════════════════
    public class QualifyingSession
    {
        const float Q1_DURATION = 18f * 60f;    // 18 min
        const float Q2_DURATION = 15f * 60f;
        const float Q3_DURATION = 12f * 60f;

        public int   QSegment    { get; private set; } = 1;
        public float TimeLeft    { get; private set; }
        public bool  IsActive    { get; private set; }
        public List<QualifyingResult> Results { get; } = new();

        readonly List<DriverEntry> _drivers;
        readonly CircuitData       _circuit;

        public QualifyingSession(List<DriverEntry> drivers, CircuitData circuit)
        {
            _drivers = drivers;
            _circuit = circuit;
            TimeLeft = Q1_DURATION;
        }

        public void StartSegment() => IsActive = true;

        public void Tick(float dt)
        {
            if (!IsActive) return;
            TimeLeft -= dt;
            if (TimeLeft <= 0f) EndSegment();
        }

        void EndSegment()
        {
            IsActive = false;
            EliminateDrivers();
            if (QSegment < 3) { QSegment++; TimeLeft = QSegment == 2 ? Q2_DURATION : Q3_DURATION; }
        }

        void EliminateDrivers()
        {
            var ranked = Results.OrderBy(r => r.q1Time).ToList();
            int cutLine = QSegment == 1 ? 15 : QSegment == 2 ? 10 : 0;

            for (int i = cutLine; i < ranked.Count; i++)
            {
                if (QSegment == 1) ranked[i].eliminated_Q1 = true;
                if (QSegment == 2) ranked[i].eliminated_Q2 = true;
            }
        }

        /// <summary>Submit a lap time for a driver in this qualifying session.</summary>
        public void SubmitLapTime(string driverName, string teamName, float lapTime)
        {
            var existing = Results.FirstOrDefault(r => r.driverName == driverName);
            if (existing == null)
            {
                existing = new QualifyingResult { driverName = driverName, teamName = teamName };
                Results.Add(existing);
            }

            switch (QSegment)
            {
                case 1:
                    if (lapTime < existing.q1Time || existing.q1Time == 0f)
                        existing.q1Time = lapTime;
                    break;
                case 2:
                    if (lapTime < existing.q2Time || existing.q2Time == 0f)
                        existing.q2Time = lapTime;
                    break;
                case 3:
                    if (lapTime < existing.q3Time || existing.q3Time == 0f)
                        existing.q3Time = lapTime;
                    break;
            }
        }

        public List<QualifyingResult> GetGridOrder()
        {
            return Results
                .OrderBy(r => r.eliminated_Q1 ? 1 : r.eliminated_Q2 ? 0 : -1)
                .ThenBy(r => r.q3Time > 0f ? r.q3Time : r.q2Time > 0f ? r.q2Time : r.q1Time)
                .ToList();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PIT STOP CONTROLLER
    // ═══════════════════════════════════════════════════════════════════════
    public class PitStopController
    {
        const float BASE_STOP_TIME = 2.4f;    // seconds stationary (F1 ~2.4s)
        const float CREW_VARIANCE  = 0.5f;

        public float CalculateStopDuration(TeamData team, bool repairFrontWing,
                                           bool repairSuspension)
        {
            float crew      = 1f - team.pitCrewSpeed / 100f * 0.4f;   // elite = 0.6x time
            float stop      = BASE_STOP_TIME * crew;
            float variance  = Random.Range(-CREW_VARIANCE, CREW_VARIANCE) * crew;

            if (repairFrontWing)   stop += 3.5f;
            if (repairSuspension)  stop += 8.0f;

            return Mathf.Max(1.8f, stop + variance);
        }

        public PitStopData ExecutePitStop(DriverEntry entry, float raceTime,
                                          TireCompound newCompound, TeamData team,
                                          ComponentDamage damage,
                                          bool fixFrontWing, bool fixSuspension)
        {
            float dur = CalculateStopDuration(team, fixFrontWing, fixSuspension);

            DamageSystem.RepairInPit(damage, fixFrontWing || fixSuspension ? 80f : 30f);

            var stop = new PitStopData
            {
                lap             = entry.currentLap,
                inTime          = raceTime,
                outTime         = raceTime + dur + 20f,   // 20s pit lane loss approx
                stopDuration    = dur,
                newCompound     = newCompound,
                repairFrontWing = fixFrontWing,
                repairSuspension = fixSuspension
            };

            entry.pitHistory.Add(stop);
            entry.pitStopCount++;
            entry.inPit = false;
            return stop;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE DIRECTOR
    // ═══════════════════════════════════════════════════════════════════════
    public class RaceDirector : MonoBehaviour
    {
        [Header("Config")]
        public CircuitData    circuit;
        public int            totalLaps = 50;
        public WeatherData    weather   = new();

        public RaceDirectorState DirectorState { get; } = new();
        public List<DriverEntry> Entries       { get; private set; } = new();
        public bool              RaceStarted   { get; private set; }
        public bool              RaceFinished  { get; private set; }
        public float             RaceTime      { get; private set; }
        public int               FinisherCount { get; private set; }

        // Weather transitions
        float _weatherTimer;
        float _weatherTransitionInterval = 120f;   // every 2 minutes check

        // Safety car
        float _safetyCarTimer;
        bool  _safetyCarDeployed;

        // Events
        public event Action<DriverEntry>     OnLapCompleted;
        public event Action<DriverEntry>     OnRetirement;
        public event Action<FlagStatus>      OnFlagChange;
        public event Action<RaceDirectorState> OnIncident;
        public event Action                  OnRaceFinished;

        void Update()
        {
            if (!RaceStarted || RaceFinished) return;
            float dt = Time.deltaTime;
            RaceTime += dt;

            TickWeather(dt);
            TickSafetyCar(dt);
            UpdatePositions();
        }

        // ── Race Control ──────────────────────────────────────────────────
        public void StartRace()
        {
            RaceStarted  = true;
            RaceFinished = false;
            RaceTime     = 0f;
            SetFlag(FlagStatus.Green);

            foreach (var e in Entries)
            {
                e.currentLap   = 1;
                e.lapStartTime = 0f;
            }
        }

        public void RegisterLapComplete(string driverId)
        {
            var entry = Entries.FirstOrDefault(e => e.driverId == driverId);
            if (entry == null || entry.retired) return;

            float lapTime = RaceTime - entry.lapStartTime;
            entry.lastLapTime = lapTime;
            if (lapTime < entry.bestLapTime) entry.bestLapTime = lapTime;
            entry.lapStartTime  = RaceTime;
            entry.currentLap++;
            entry.totalRaceTime = RaceTime;

            OnLapCompleted?.Invoke(entry);

            if (entry.currentLap > totalLaps && !entry.isPlayer)
                FinishDriver(entry);
        }

        public void RegisterPlayerFinish(DriverEntry entry)
        {
            FinishDriver(entry);
        }

        void FinishDriver(DriverEntry entry)
        {
            entry.position = ++FinisherCount;
            entry.retired  = false;
            if (FinisherCount == 1) SetFlag(FlagStatus.Chequered);
            if (FinisherCount >= Entries.Count(e => !e.retired)) EndRace();
        }

        public void RegisterRetirement(string driverId, string reason)
        {
            var entry = Entries.FirstOrDefault(e => e.driverId == driverId);
            if (entry == null) return;
            entry.retired = true;
            OnRetirement?.Invoke(entry);
            DirectorState.incidentMessage = $"{entry.driverName} retired: {reason}";
            OnIncident?.Invoke(DirectorState);

            // Possibly deploy safety car
            if (Random.value < 0.35f) DeploySafetyCar();
        }

        public void IssuePenalty(string driverId, PenaltyType type, float seconds = 0f)
        {
            var entry = Entries.FirstOrDefault(e => e.driverId == driverId);
            if (entry == null) return;
            entry.pendingPenalty  = type;
            entry.penaltySeconds += seconds;
        }

        // ── Positions ─────────────────────────────────────────────────────
        void UpdatePositions()
        {
            var active = Entries
                .Where(e => !e.retired)
                .OrderByDescending(e => e.currentLap)
                .ThenByDescending(e => e.distanceRaced)
                .ToList();

            for (int i = 0; i < active.Count; i++)
                active[i].position = i + 1;
        }

        // ── Safety Car ────────────────────────────────────────────────────
        void DeploySafetyCar()
        {
            if (_safetyCarDeployed) return;
            _safetyCarDeployed = true;
            _safetyCarTimer    = 120f;  // deploy for 2 laps approx
            SetFlag(FlagStatus.SafetyCar);

            DirectorState.safetyCarSpeed = 80f;
            DirectorState.pitLaneOpen    = true;
        }

        void TickSafetyCar(float dt)
        {
            if (!_safetyCarDeployed) return;
            _safetyCarTimer -= dt;
            if (_safetyCarTimer <= 0f)
            {
                _safetyCarDeployed = false;
                SetFlag(FlagStatus.Green);
            }
        }

        // ── Weather ───────────────────────────────────────────────────────
        void TickWeather(float dt)
        {
            _weatherTimer += dt;
            WeatherSystem.TickWeather(weather, dt);

            if (_weatherTimer >= _weatherTransitionInterval)
            {
                _weatherTimer = 0f;
                var prev = weather.condition;
                weather.condition = WeatherSystem.TransitionWeather(weather.condition, 0.18f);
                if (weather.condition != prev)
                {
                    DirectorState.incidentMessage = $"Weather change: {weather.condition}";
                    OnIncident?.Invoke(DirectorState);
                }
            }
        }

        // ── Flags ─────────────────────────────────────────────────────────
        void SetFlag(FlagStatus flag)
        {
            DirectorState.flag = flag;
            OnFlagChange?.Invoke(flag);
        }

        void EndRace()
        {
            RaceFinished = true;
            SetFlag(FlagStatus.Chequered);
            OnRaceFinished?.Invoke();
        }

        // ── Setup helper ──────────────────────────────────────────────────
        public void InitEntries(List<DriverEntry> entries)
        {
            Entries = entries;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE RESULT BUILDER
    // ═══════════════════════════════════════════════════════════════════════
    public static class RaceResultBuilder
    {
        static readonly int[] POINTS = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
        const int FASTEST_LAP_POINT  = 1;   // bonus if in top 10

        public static List<RaceResult> Build(List<DriverEntry> entries)
        {
            var results     = new List<RaceResult>();
            DriverEntry flap = entries
                .Where(e => !e.retired)
                .OrderBy(e => e.bestLapTime)
                .FirstOrDefault();

            foreach (var e in entries.OrderBy(e => e.retired ? 999 : e.position))
            {
                int pts = e.retired ? 0 : PointsFor(e.position);
                if (flap != null && e == flap && e.position <= 10) pts += FASTEST_LAP_POINT;

                results.Add(new RaceResult
                {
                    playerId       = e.driverId,
                    driverName     = e.driverName,
                    teamName       = e.teamName,
                    finishPosition = e.position,
                    totalTime      = e.totalRaceTime + e.penaltySeconds,
                    fastestLap     = e.bestLapTime,
                    pitStops       = e.pitStopCount,
                    retired        = e.retired,
                    hasFastestLap  = e == flap,
                    pointsEarned   = pts,
                    penalty        = e.pendingPenalty,
                    penaltySeconds = e.penaltySeconds
                });
            }
            return results;
        }

        static int PointsFor(int pos) =>
            pos >= 1 && pos <= POINTS.Length ? POINTS[pos - 1] : 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  REPLAY RECORDER
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class ReplayFrame
    {
        public float     time;
        public string    driverId;
        public Vector3   position;
        public Quaternion rotation;
        public float     speed;
        public int       gear;
    }

    public class ReplayRecorder : MonoBehaviour
    {
        const float RECORD_INTERVAL = 0.05f;   // 20 Hz replay

        readonly List<ReplayFrame> _frames = new();
        float _recordTimer;
        bool  _recording;

        Dictionary<string, Transform> _drivers = new();

        public IReadOnlyList<ReplayFrame> Frames => _frames;

        public void StartRecording(Dictionary<string, Transform> driverTransforms)
        {
            _drivers   = driverTransforms;
            _recording = true;
            _frames.Clear();
        }

        public void StopRecording() => _recording = false;

        void Update()
        {
            if (!_recording) return;
            _recordTimer += Time.deltaTime;
            if (_recordTimer < RECORD_INTERVAL) return;
            _recordTimer = 0f;

            foreach (var (id, tf) in _drivers)
            {
                if (tf == null) continue;
                var physics = tf.GetComponent<PhysicsIntegrator>();
                _frames.Add(new ReplayFrame
                {
                    time     = Time.time,
                    driverId = id,
                    position = tf.position,
                    rotation = tf.rotation,
                    speed    = physics != null ? physics.State.speed : 0f,
                    gear     = physics != null ? physics.State.gear  : 0
                });
            }
        }
    }
}
