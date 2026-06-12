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
    //  RACE DIRECTOR  –  2026 Extension
    //  Adds: OvertakeMode integration, active aero zone management,
    //        Madrid circuit behaviour, TyreStrategy2026 enforcement,
    //        Virtual Safety Car, ERS telemetry feed, 2026 flag rules.
    //  All original code retained intact below new additions.
    // ═══════════════════════════════════════════════════════════════════════

    public class RaceDirector : MonoBehaviour
    {
        [Header("Config")]
        public CircuitData    circuit;
        public int            totalLaps = 50;
        public WeatherData    weather   = new();

        [Header("2026 Regulation Flags")]
        public bool           is2026Regulations = true;
        public bool           isMadridCircuit   = false;

        public RaceDirectorState  DirectorState { get; } = new();
        public List<DriverEntry>  Entries       { get; private set; } = new();
        public bool               RaceStarted   { get; private set; }
        public bool               RaceFinished  { get; private set; }
        public float              RaceTime      { get; private set; }
        public int                FinisherCount { get; private set; }

        // ── 2026 state ────────────────────────────────────────────────────
        private Dictionary<string, ActiveAeroState>  _aeroStates   = new();
        private Dictionary<string, ERSState2026>     _ersStates    = new();
        private Dictionary<string, TyreState2026[]>  _tyreStates   = new();
        private Dictionary<string, DeploymentMode>   _deployModes  = new();
        private Dictionary<string, ActiveAeroMode>   _aeroModes    = new();
        private Dictionary<string, bool>             _overtakeArmed = new(); // driver requested overtake mode
        private List<OvertakeZone>                   _overtakeZones = new();
        private VirtualSafetyCarController           _vsc;

        // ── Internal ──────────────────────────────────────────────────────
        float _weatherTimer;
        float _weatherTransitionInterval = 120f;
        float _safetyCarTimer;
        bool  _safetyCarDeployed;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<DriverEntry>         OnLapCompleted;
        public event Action<DriverEntry>         OnRetirement;
        public event Action<FlagStatus>          OnFlagChange;
        public event Action<RaceDirectorState>   OnIncident;
        public event Action                      OnRaceFinished;

        // 2026 new events
        public event Action<string, bool>        OnOvertakeModeChanged;   // (driverId, active)
        public event Action<string, ERSState2026> OnERSTelemetry;         // fires each lap
        public event Action<string, TyreState2026> OnTyreCliff;           // tyre entered cliff
        public event Action<bool>                OnVSCDeployed;           // virtual safety car
        public event Action<string, int>         OnMandatoryTyreComplied; // (driverId, compoundsUsed)

        // ═════════════════════════════════════════════════════════════════
        //  INIT
        // ═════════════════════════════════════════════════════════════════

        void Awake()
        {
            _vsc = new VirtualSafetyCarController();
        }

        public void InitEntries(List<DriverEntry> entries)
        {
            Entries = entries;

            // Initialise 2026 sub-states for every driver
            foreach (var e in entries)
            {
                _aeroStates[e.driverId]    = new ActiveAeroState();
                _ersStates[e.driverId]     = new ERSState2026();
                _deployModes[e.driverId]   = DeploymentMode.Race_Medium;
                _aeroModes[e.driverId]     = ActiveAeroMode.Auto;
                _overtakeArmed[e.driverId] = false;

                // Four tyres per driver; default to Medium
                _tyreStates[e.driverId] = new TyreState2026[]
                {
                    new() { currentCompound = TyreCompound2026.Medium },
                    new() { currentCompound = TyreCompound2026.Medium },
                    new() { currentCompound = TyreCompound2026.Medium },
                    new() { currentCompound = TyreCompound2026.Medium }
                };
            }

            // Set up overtake zones
            if (isMadridCircuit)
                BuildMadridOvertakeZones();
            else
                BuildGenericOvertakeZones();
        }

        // ═════════════════════════════════════════════════════════════════
        //  UPDATE LOOP
        // ═════════════════════════════════════════════════════════════════

        void Update()
        {
            if (!RaceStarted || RaceFinished) return;
            float dt = Time.deltaTime;
            RaceTime += dt;

            TickWeather(dt);
            TickSafetyCar(dt);
            _vsc.Tick(dt, DirectorState);
            UpdatePositions();

            if (is2026Regulations)
            {
                Tick2026Systems(dt);
            }
        }

        void Tick2026Systems(float dt)
        {
            foreach (var entry in Entries)
            {
                if (entry.retired) continue;

                string id = entry.driverId;

                // Resolve a VehicleState proxy for physics simulation
                var vehicle = entry.vehicleState;
                if (vehicle == null) continue;

                var aero    = _aeroStates[id];
                var ers     = _ersStates[id];
                var tyres   = _tyreStates[id];
                var deploy  = _deployModes[id];
                var aeroMode = _aeroModes[id];

                // Check active aero zone override
                var zone = GetCurrentOvertakeZone(entry.distanceRaced % (circuit?.lapLength ?? 5000f));
                if (zone != null) aeroMode = ActiveAeroMode.LowDrag;

                // Overtake mode activation
                if (_overtakeArmed[id])
                {
                    var ahead = GetDriverAhead(entry);
                    float gap = ahead != null ? entry.GapToLeader(ahead) : 999f;
                    if (CarPhysics.TryActivateOvertakeMode(ers, vehicle, Mathf.Abs(gap)))
                    {
                        OnOvertakeModeChanged?.Invoke(id, true);
                        _overtakeArmed[id] = false;
                    }
                }

                // Overtake mode expired notification
                if (!ers.overtakeModeActive && vehicle.overtakeModeVFX)
                {
                    vehicle.overtakeModeVFX = false;
                    OnOvertakeModeChanged?.Invoke(id, false);
                }

                // Full 2026 physics tick
                CarPhysics.SimulateVehicleDynamics2026(
                    vehicle, entry.carSetup ?? new CarSetup(),
                    weather, aero, ers, tyres,
                    deploy, aeroMode, isMadridCircuit, dt);

                // Tyre cliff detection
                foreach (var tyre in tyres)
                {
                    if (tyre.inCliff)
                        OnTyreCliff?.Invoke(id, tyre);
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  PLAYER CONTROLS  (called from React UI via Unity bridge)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Player requests overtake mode from the ERS HUD button.</summary>
        public void RequestOvertakeMode(string driverId)
        {
            if (!_overtakeArmed.ContainsKey(driverId)) return;
            var ers = _ersStates[driverId];

            if (ers.overtakeCooldown > 0f)
            {
                Debug.Log($"[RaceDirector] Overtake mode on cooldown: {ers.overtakeCooldown:F1}s remaining");
                return;
            }
            _overtakeArmed[driverId] = true;
        }

        public void SetDeploymentMode(string driverId, DeploymentMode mode)
        {
            if (_deployModes.ContainsKey(driverId))
                _deployModes[driverId] = mode;
        }

        public void SetAeroMode(string driverId, ActiveAeroMode mode)
        {
            // Prevent driver opening aero in wet conditions
            if (weather.wetness > 0.5f && mode == ActiveAeroMode.LowDrag)
            {
                Debug.LogWarning("[RaceDirector] Cannot set LowDrag in wet conditions.");
                return;
            }
            if (_aeroModes.ContainsKey(driverId))
                _aeroModes[driverId] = mode;
        }

        /// <summary>Called when a driver pits to change tyres.</summary>
        public void RegisterTyreChange(string driverId, TyreCompound2026 newCompound)
        {
            if (!_tyreStates.ContainsKey(driverId)) return;

            var newTyres = new TyreState2026[]
            {
                new() { currentCompound = newCompound, lifePercent = 100f, tyreTemp = 60f },
                new() { currentCompound = newCompound, lifePercent = 100f, tyreTemp = 60f },
                new() { currentCompound = newCompound, lifePercent = 100f, tyreTemp = 60f },
                new() { currentCompound = newCompound, lifePercent = 100f, tyreTemp = 60f }
            };
            _tyreStates[driverId] = newTyres;

            // Track mandatory compound compliance (must use ≥2 dry compounds)
            var entry = Entries.FirstOrDefault(e => e.driverId == driverId);
            if (entry != null)
            {
                entry.compoundsUsed.Add(newCompound);
                int distinctDry = entry.compoundsUsed
                    .Where(c => c != TyreCompound2026.Intermediate && c != TyreCompound2026.Wet)
                    .Distinct().Count();
                OnMandatoryTyreComplied?.Invoke(driverId, distinctDry);
            }

            // Reset ERS lap counters on pit (fresh start)
            if (_ersStates.TryGetValue(driverId, out var ers))
                ers.ResetLapCounters();
        }

        // ═════════════════════════════════════════════════════════════════
        //  RACE CONTROL  (original + extended)
        // ═════════════════════════════════════════════════════════════════

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
                e.compoundsUsed = new HashSet<TyreCompound2026>();

                // Register starting compound
                if (_tyreStates.TryGetValue(e.driverId, out var tyres))
                    e.compoundsUsed.Add(tyres[0].currentCompound);
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

            // 2026: reset ERS lap counters and broadcast telemetry
            if (is2026Regulations && _ersStates.TryGetValue(driverId, out var ers))
            {
                OnERSTelemetry?.Invoke(driverId, ers);
                ers.ResetLapCounters();
            }

            OnLapCompleted?.Invoke(entry);

            if (entry.currentLap > totalLaps && !entry.isPlayer)
                FinishDriver(entry);
        }

        public void RegisterPlayerFinish(DriverEntry entry) => FinishDriver(entry);

        void FinishDriver(DriverEntry entry)
        {
            // 2026: penalise if mandatory tyre rule not met (no rain exemption here)
            if (is2026Regulations)
                EnforceMandatoryTyreRule(entry);

            entry.position = ++FinisherCount;
            entry.retired  = false;

            if (FinisherCount == 1) SetFlag(FlagStatus.Chequered);
            if (FinisherCount >= Entries.Count(e => !e.retired)) EndRace();
        }

        void EnforceMandatoryTyreRule(DriverEntry entry)
        {
            bool wetRace = weather.wetness > 0.7f;
            if (wetRace) return;  // mandate waived in wet conditions

            int distinctDry = entry.compoundsUsed?
                .Where(c => c != TyreCompound2026.Intermediate && c != TyreCompound2026.Wet)
                .Distinct().Count() ?? 0;

            if (distinctDry < 2)
            {
                entry.penaltySeconds += 30f;  // 30s post-race penalty
                DirectorState.incidentMessage =
                    $"⚖️ {entry.driverName}: +30s — mandatory tyre rule violation.";
                OnIncident?.Invoke(DirectorState);
            }
        }

        public void RegisterRetirement(string driverId, string reason)
        {
            var entry = Entries.FirstOrDefault(e => e.driverId == driverId);
            if (entry == null) return;

            entry.retired = true;
            OnRetirement?.Invoke(entry);
            DirectorState.incidentMessage = $"{entry.driverName} retired: {reason}";
            OnIncident?.Invoke(DirectorState);

            // 2026: minor incidents → VSC; major → Full SC
            if (reason.Contains("crash") || reason.Contains("fire"))
                DeploySafetyCar();
            else if (Random.value < 0.45f)
                _vsc.Deploy(DirectorState, OnVSCDeployed);
            else if (Random.value < 0.25f)
                DeploySafetyCar();
        }

        public void IssuePenalty(string driverId, PenaltyType type, float seconds = 0f)
        {
            var entry = Entries.FirstOrDefault(e => e.driverId == driverId);
            if (entry == null) return;
            entry.pendingPenalty  = type;
            entry.penaltySeconds += seconds;
        }

        // ═════════════════════════════════════════════════════════════════
        //  VIRTUAL SAFETY CAR  (2026 new)
        // ═════════════════════════════════════════════════════════════════

        public void DeployVSC()
        {
            _vsc.Deploy(DirectorState, OnVSCDeployed);
            SetFlag(FlagStatus.VSC);
        }

        // ═════════════════════════════════════════════════════════════════
        //  OVERTAKE ZONES
        // ═════════════════════════════════════════════════════════════════

        void BuildMadridOvertakeZones()
        {
            // Madrid MADRING — 3 DRS/active-aero zones
            _overtakeZones = new List<OvertakeZone>
            {
                new() { startM = 350f,  endM = 950f,  name = "Main Straight" },
                new() { startM = 2100f, endM = 2500f, name = "Back Straight" },
                new() { startM = 3800f, endM = 4200f, name = "Sector 3 Straight" }
            };
        }

        void BuildGenericOvertakeZones()
        {
            // Default single zone for non-Madrid circuits
            _overtakeZones = new List<OvertakeZone>
            {
                new() { startM = 200f, endM = 800f, name = "Main Straight" }
            };
        }

        OvertakeZone GetCurrentOvertakeZone(float distanceOnLap)
        {
            return _overtakeZones.FirstOrDefault(
                z => distanceOnLap >= z.startM && distanceOnLap <= z.endM);
        }

        // ═════════════════════════════════════════════════════════════════
        //  TELEMETRY ACCESSORS  (for React UI)
        // ═════════════════════════════════════════════════════════════════

        public ERSState2026 GetERSState(string driverId) =>
            _ersStates.TryGetValue(driverId, out var s) ? s : null;

        public ActiveAeroState GetAeroState(string driverId) =>
            _aeroStates.TryGetValue(driverId, out var s) ? s : null;

        public TyreState2026[] GetTyreStates(string driverId) =>
            _tyreStates.TryGetValue(driverId, out var t) ? t : null;

        public RaceWeekendTelemetry GetPlayerTelemetry(string driverId)
        {
            var entry = Entries.FirstOrDefault(e => e.driverId == driverId);
            if (entry == null) return null;

            return new RaceWeekendTelemetry
            {
                driverId         = driverId,
                position         = entry.position,
                currentLap       = entry.currentLap,
                totalLaps        = totalLaps,
                lastLapTime      = entry.lastLapTime,
                gapAhead         = entry.gapAhead,
                pitStopCount     = entry.pitStopCount,
                ersState         = GetERSState(driverId),
                aeroState        = GetAeroState(driverId),
                frontLeftTyre    = GetTyreStates(driverId)?[0],
                frontRightTyre   = GetTyreStates(driverId)?[1],
                rearLeftTyre     = GetTyreStates(driverId)?[2],
                rearRightTyre    = GetTyreStates(driverId)?[3],
                deployMode       = _deployModes.TryGetValue(driverId, out var d) ? d : DeploymentMode.Race_Medium,
                flag             = DirectorState.flag,
                safetyCarActive  = _safetyCarDeployed,
                vscActive        = _vsc.IsActive
            };
        }

        // ═════════════════════════════════════════════════════════════════
        //  ORIGINAL METHODS  (unchanged)
        // ═════════════════════════════════════════════════════════════════

        void UpdatePositions()
        {
            var active = Entries
                .Where(e => !e.retired)
                .OrderByDescending(e => e.currentLap)
                .ThenByDescending(e => e.distanceRaced)
                .ToList();

            for (int i = 0; i < active.Count; i++)
            {
                active[i].position = i + 1;

                // Update gaps
                if (i == 0) active[i].gapAhead = 0f;
                else        active[i].gapAhead  = active[i].GapToLeader(active[i - 1]);
                if (i < active.Count - 1) active[i].gapBehind = active[i + 1].gapAhead;
            }
        }

        void DeploySafetyCar()
        {
            if (_safetyCarDeployed) return;
            _safetyCarDeployed = true;
            _safetyCarTimer    = 120f;
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

                    // 2026: lock active aero in heavy rain
                    if (weather.wetness > 0.8f)
                    {
                        foreach (var aero in _aeroStates.Values)
                            aero.lockedForWet = true;
                    }
                    else
                    {
                        foreach (var aero in _aeroStates.Values)
                            aero.lockedForWet = false;
                    }
                }
            }
        }

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

        DriverEntry GetDriverAhead(DriverEntry entry)
        {
            return Entries
                .Where(e => !e.retired && e.position == entry.position - 1)
                .FirstOrDefault();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  VIRTUAL SAFETY CAR CONTROLLER  (2026 new)
    // ═══════════════════════════════════════════════════════════════════════

    public class VirtualSafetyCarController
    {
        public bool  IsActive    { get; private set; }
        public float TimeLeft    { get; private set; }
        const float  VSC_DURATION = 90f;   // seconds
        const float  VSC_SPEED    = 160f;  // km/h

        private Action<bool> _onVSCDeployed;

        public void Deploy(RaceDirectorState state, Action<bool> onDeployedEvent)
        {
            if (IsActive) return;
            IsActive      = true;
            TimeLeft      = VSC_DURATION;
            _onVSCDeployed = onDeployedEvent;

            state.flag         = FlagStatus.VSC;
            state.safetyCarSpeed = VSC_SPEED;
            state.pitLaneOpen  = true;

            onDeployedEvent?.Invoke(true);
            Debug.Log("[RaceDirector] VSC deployed.");
        }

        public void Tick(float dt, RaceDirectorState state)
        {
            if (!IsActive) return;
            TimeLeft -= dt;
            if (TimeLeft <= 0f) End(state);
        }

        void End(RaceDirectorState state)
        {
            IsActive         = false;
            state.flag       = FlagStatus.Green;
            state.pitLaneOpen = false;
            _onVSCDeployed?.Invoke(false);
            Debug.Log("[RaceDirector] VSC ended — green flag.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  OVERTAKE ZONE
    // ═══════════════════════════════════════════════════════════════════════

    public class OvertakeZone
    {
        public float  startM;
        public float  endM;
        public string name;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE WEEKEND TELEMETRY  (fed to React HUD via Unity bridge)
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class RaceWeekendTelemetry
    {
        public string           driverId;
        public int              position;
        public int              currentLap;
        public int              totalLaps;
        public float            lastLapTime;
        public float            gapAhead;
        public int              pitStopCount;
        public ERSState2026     ersState;
        public ActiveAeroState  aeroState;
        public TyreState2026    frontLeftTyre;
        public TyreState2026    frontRightTyre;
        public TyreState2026    rearLeftTyre;
        public TyreState2026    rearRightTyre;
        public DeploymentMode   deployMode;
        public FlagStatus       flag;
        public bool             safetyCarActive;
        public bool             vscActive;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  EXTENDED DriverEntry  (2026 additions)
    //  Extend the existing class with new fields.
    // ═══════════════════════════════════════════════════════════════════════

    public partial class DriverEntry
    {
        public HashSet<TyreCompound2026> compoundsUsed = new();
        public VehicleState              vehicleState;           // live physics state
        public CarSetup                  carSetup;               // setup chosen in garage
    }

    // ── FlagStatus extended ───────────────────────────────────────────────
    // Add VSC to whatever enum your codebase already has.
    // If FlagStatus is defined elsewhere, add VSC there instead.
    public partial class RaceDirectorState
    {
        // Existing fields assumed: flag, safetyCarSpeed, pitLaneOpen, incidentMessage
        // No changes needed — VSC reuses flag = FlagStatus.VSC
    }
}