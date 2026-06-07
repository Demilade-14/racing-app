using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Physics;

namespace RacingGame.AI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  RACING LINE DATA
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class RacingLineData
    {
        public List<Vector3> waypoints       = new();
        public List<float>   speedAtWaypoint = new();
        public List<float>   throttlePoint   = new();
        public List<float>   brakingDistance = new();
        public List<bool>    isDrsZone       = new();
        public List<bool>    isTrackLimitRisk = new();  // corners where cutting saves time
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DIFFICULTY PROFILE
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class DifficultyProfile
    {
        [Range(0.55f, 1.00f)] public float gripUtilisation  = 0.85f;
        [Range(0.00f, 0.25f)] public float reactionLag      = 0.10f;
        [Range(0.10f, 1.00f)] public float linePrecision    = 0.50f;
        [Range(0.00f, 0.35f)] public float mistakeFrequency = 0.10f;
        [Range(0.00f, 1.00f)] public float aggression       = 0.50f;
        [Range(0.88f, 1.00f)] public float paceFactor       = 0.95f;
        [Range(0.50f, 1.00f)] public float tireManagement   = 0.75f;
        [Range(0.50f, 1.00f)] public float strategyIQ       = 0.70f;
        [Range(0.00f, 1.00f)] public float ersManagement    = 0.70f;
        [Range(0.00f, 1.00f)] public float trackLimitRisk   = 0.30f;

        public static DifficultyProfile Easy() => new()
        {
            gripUtilisation = 0.58f, reactionLag = 0.22f, linePrecision = 0.22f,
            mistakeFrequency = 0.32f, aggression = 0.18f, paceFactor = 0.88f,
            tireManagement = 0.50f, strategyIQ = 0.50f,
            ersManagement = 0.40f, trackLimitRisk = 0.05f
        };

        public static DifficultyProfile Medium() => new()
        {
            gripUtilisation = 0.75f, reactionLag = 0.12f, linePrecision = 0.55f,
            mistakeFrequency = 0.15f, aggression = 0.45f, paceFactor = 0.94f,
            tireManagement = 0.70f, strategyIQ = 0.70f,
            ersManagement = 0.65f, trackLimitRisk = 0.20f
        };

        public static DifficultyProfile Hard() => new()
        {
            gripUtilisation = 0.88f, reactionLag = 0.06f, linePrecision = 0.80f,
            mistakeFrequency = 0.06f, aggression = 0.65f, paceFactor = 0.98f,
            tireManagement = 0.85f, strategyIQ = 0.85f,
            ersManagement = 0.82f, trackLimitRisk = 0.45f
        };

        public static DifficultyProfile Expert() => new()
        {
            gripUtilisation = 0.96f, reactionLag = 0.02f, linePrecision = 0.95f,
            mistakeFrequency = 0.02f, aggression = 0.82f, paceFactor = 1.00f,
            tireManagement = 0.97f, strategyIQ = 0.97f,
            ersManagement = 0.97f, trackLimitRisk = 0.70f
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AI DRIVER
    // ═══════════════════════════════════════════════════════════════════════
    public class AIDriver : MonoBehaviour
    {
        [Header("Identity")]
        public DriverStats       stats       = new();
        public AIPersonality     personality = AIPersonality.Balanced;
        public DifficultyProfile difficulty  = new();

        [Header("Race State")]
        public int   racePosition   = 1;
        public int   totalCars      = 20;
        public int   lapNumber      = 1;
        public int   totalLaps      = 50;
        public float gapAhead       = 999f;
        public float gapBehind      = 999f;
        public float form           = 1.0f;
        public bool  inPit          = false;
        public bool  onOutLap       = false;

        [Header("Championship")]
        public ChampionshipContext championship = new();

        [Header("Track Limits")]
        public int trackLimitWarnings  = 0;
        public int trackLimitViolations = 0;

        public AIState CurrentState { get; private set; } = AIState.FollowLine;

        readonly VehicleState _vehicle = new();
        public   VehicleState Vehicle  => _vehicle;

        AIBrain          _brain;
        PitStrategyCalc  _pitCalc;
        ERSBatteryAI     _ersAI;
        TrackLimitsAI    _trackLimitsAI;
        WeatherData      _weather = new();
        FlagStatus       _lastFlag = FlagStatus.Green;

        void Awake()
        {
            _brain         = new AIBrain(this);
            _pitCalc       = new PitStrategyCalc(this);
            _ersAI         = new ERSBatteryAI(this);
            _trackLimitsAI = new TrackLimitsAI(this);
            form           = Random.Range(0.88f, 1.12f);

            for (int i = 0; i < 4; i++)
            {
                _vehicle.tires[i] = new TireState { compound = TireCompound.Medium };
                _vehicle.tires[i].InitTemps(25f);
            }
            _vehicle.fuelLoad = 110f;
            _vehicle.gear     = 1;
            _vehicle.ersSoC   = 80f;
            _vehicle.ersMode  = ERSMode.Balanced;
        }

        void FixedUpdate()
        {
            _brain.Tick(Time.fixedDeltaTime);
            _ersAI.Tick(Time.fixedDeltaTime);
            _trackLimitsAI.Tick(Time.fixedDeltaTime);
        }

        public void SetState(AIState s)              => CurrentState = s;
        public void SetWeather(WeatherData w)        => _weather = w;
        public void NotifyFlag(FlagStatus flag)      => OnFlagChange(flag);
        public WeatherData CurrentWeather            => _weather;
        public ERSBatteryAI ERSManager               => _ersAI;
        public PitStrategyCalc PitCalc               => _pitCalc;
        public TrackLimitsAI TrackLimitsAI           => _trackLimitsAI;

        void OnFlagChange(FlagStatus flag)
        {
            if (flag == _lastFlag) return;
            _lastFlag = flag;

            switch (flag)
            {
                case FlagStatus.SafetyCar:
                    _pitCalc.NotifySafetyCar();
                    SetState(AIState.SafetyCar);
                    break;
                case FlagStatus.VirtualSafetyCar:
                    _pitCalc.NotifyVSC();
                    SetState(AIState.VSC);
                    break;
                case FlagStatus.Green:
                    if (CurrentState == AIState.SafetyCar ||
                        CurrentState == AIState.VSC)
                        SetState(AIState.FollowLine);
                    break;
            }
        }

        public float EffectivePace =>
            difficulty.paceFactor
          * form
          * (stats.OverallRating * 0.40f + 0.60f)
          * (_weather.rainIntensity > 0.3f
                ? stats.wetWeather / 100f * 0.15f + 0.85f : 1f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ERS BATTERY AI
    // ═══════════════════════════════════════════════════════════════════════
    public class ERSBatteryAI
    {
        readonly AIDriver _d;

        // Thresholds
        const float SOC_CRITICAL    = 15f;   // % – must harvest
        const float SOC_LOW         = 30f;
        const float SOC_HIGH        = 75f;
        const float SOC_FULL        = 92f;

        // State
        float _overtakeWindowTimer = 0f;     // how long we've been within DRS range
        bool  _blockingDeployActive = false;

        public ERSBatteryAI(AIDriver d) => _d = d;

        public void Tick(float dt)
        {
            var v    = _d.Vehicle;
            float soc = v.ersSoC;

            // Track-limit out-lap: heat tires with aggressive ERS bursts
            if (_d.CurrentState == AIState.OutLap)
            {
                HandleOutLap(v, soc);
                return;
            }

            // Safety car / VSC: harvest everything
            if (_d.CurrentState == AIState.SafetyCar ||
                _d.CurrentState == AIState.VSC)
            {
                v.ersMode = ERSMode.Harvest;
                return;
            }

            // Critical battery – force harvest regardless
            if (soc < SOC_CRITICAL)
            {
                v.ersMode = ERSMode.Harvest;
                return;
            }

            // Defensive AI: save battery to block on main straight
            if (ShouldBlockWithERS(v, soc))
            {
                HandleDefensiveERS(v, soc, dt);
                return;
            }

            // Aggressive / chasing: deploy on out-lap and overtake attempts
            if (_d.CurrentState == AIState.ChaseLeader && soc > SOC_LOW)
            {
                _overtakeWindowTimer += dt;
                // Only activate overtake mode when slipstream available
                bool hasSlipstream = v.inDirtyAir && v.gapToCarAheadSec < 0.8f;

                if (hasSlipstream && _overtakeWindowTimer > 0.5f &&
                    _d.difficulty.ersManagement > 0.5f)
                {
                    ERSSystem.ActivateOvertake(v);
                }
                else
                {
                    v.ersMode = soc > SOC_HIGH ? ERSMode.Balanced : ERSMode.Harvest;
                }
                return;
            }
            else
            {
                _overtakeWindowTimer = 0f;
            }

            // Default: balanced management based on battery level
            v.ersMode = ChooseDefaultMode(soc);
        }

        void HandleOutLap(VehicleState v, float soc)
        {
            // Aggressive drivers use ERS bursts on out-lap to heat tires faster
            bool aggressive = _d.personality == AIPersonality.Aggressive ||
                              _d.personality == AIPersonality.YoungTalent;

            if (aggressive && soc > SOC_LOW && _d.difficulty.ersManagement > 0.6f)
            {
                // Short bursts through slow corners to generate tire heat
                bool inCorner = Mathf.Abs(v.steering) > 0.3f;
                v.ersMode = inCorner ? ERSMode.Overtake : ERSMode.Balanced;

                if (inCorner && v.ersOvertakeTimer <= 0f && soc > SOC_LOW)
                    ERSSystem.ActivateOvertake(v);
            }
            else
            {
                v.ersMode = ERSMode.Harvest;
            }
        }

        void HandleDefensiveERS(VehicleState v, float soc, float dt)
        {
            // Conservative/veteran: hold battery, deploy only on main straight to defend
            bool onStraight = Mathf.Abs(v.steering) < 0.1f && v.speed > 200f;

            if (onStraight && _d.gapBehind < 1.2f && soc > SOC_LOW)
            {
                _blockingDeployActive = true;
                v.ersMode = ERSMode.Overtake;
                if (v.ersOvertakeTimer <= 0f) ERSSystem.ActivateOvertake(v);
            }
            else if (!onStraight)
            {
                _blockingDeployActive = false;
                v.ersMode = soc < SOC_HIGH ? ERSMode.Harvest : ERSMode.Balanced;
            }
        }

        bool ShouldBlockWithERS(VehicleState v, float soc)
        {
            bool defensivePersonality = _d.personality == AIPersonality.Conservative ||
                                        _d.personality == AIPersonality.Veteran;
            return defensivePersonality &&
                   _d.gapBehind < 1.5f &&
                   soc > SOC_LOW &&
                   _d.difficulty.ersManagement > 0.55f;
        }

        static ERSMode ChooseDefaultMode(float soc)
        {
            if (soc > SOC_FULL)   return ERSMode.Balanced;
            if (soc > SOC_HIGH)   return ERSMode.Balanced;
            if (soc > SOC_LOW)    return ERSMode.Harvest;
            return ERSMode.Harvest;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TRACK LIMITS AI
    // ═══════════════════════════════════════════════════════════════════════
    public class TrackLimitsAI
    {
        readonly AIDriver _d;

        // Per-corner cut tracking
        readonly Dictionary<int, int> _cutCountPerCorner = new();
        float _penaltyRiskCooldown = 0f;

        public TrackLimitsAI(AIDriver d) => _d = d;

        public void Tick(float dt)
        {
            if (_penaltyRiskCooldown > 0f)
                _penaltyRiskCooldown -= dt;
        }

        /// <summary>
        /// Called by AIBrain when approaching a track-limit risk waypoint.
        /// Returns true if the AI should attempt the cut this lap.
        /// </summary>
        public bool ShouldCutCorner(int waypointIndex, float timeGainSec)
        {
            if (_penaltyRiskCooldown > 0f) return false;

            // Penalty risk scales with warning count
            float penaltyRisk = _d.trackLimitWarnings / 3f;

            // Championship pressure increases risk tolerance
            float championshipRisk = _d.championship.RiskTolerance;

            // Difficulty scales how often the AI attempts cuts
            float baseWillingness = _d.difficulty.trackLimitRisk;

            // Is the time gain worth the risk?
            float gainValue = Mathf.Clamp01(timeGainSec / 0.3f); // normalise against 0.3s

            float totalWillingness = (baseWillingness + championshipRisk * 0.3f - penaltyRisk * 0.5f)
                                   * gainValue;

            bool willCut = Random.value < totalWillingness;

            if (willCut)
            {
                // Track how many times we've cut this specific corner
                if (!_cutCountPerCorner.ContainsKey(waypointIndex))
                    _cutCountPerCorner[waypointIndex] = 0;

                _cutCountPerCorner[waypointIndex]++;

                // After 2 cuts on same corner same lap, back off (stewards watching)
                if (_cutCountPerCorner[waypointIndex] > 2)
                {
                    _penaltyRiskCooldown = 15f;  // back off for 15 seconds
                    return false;
                }
            }

            return willCut;
        }

        /// <summary>Called when Race Control issues a track-limit warning to this driver.</summary>
        public void ReceiveWarning()
        {
            _d.trackLimitWarnings++;
            _penaltyRiskCooldown = 20f;  // back off after each warning

            // Reset cut counts so AI re-evaluates
            _cutCountPerCorner.Clear();
        }

        public void StartNewLap() => _cutCountPerCorner.Clear();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AI BRAIN
    // ═══════════════════════════════════════════════════════════════════════
    public class AIBrain
    {
        readonly AIDriver        _d;
        readonly PitStrategyCalc _pit;
        RacingLineData           _line;

        int   _wpIdx         = 0;
        float _decisionTimer = 0f;
        float _lagTimer      = 0f;
        AIState _pendingState = AIState.FollowLine;

        const float DECISION_FREQ = 0.10f;

        public AIBrain(AIDriver d)
        {
            _d   = d;
            _pit = new PitStrategyCalc(d);
        }

        public void SetRacingLine(RacingLineData line) => _line = line;

        public void Tick(float dt)
        {
            _decisionTimer += dt;
            if (_decisionTimer >= DECISION_FREQ)
            {
                _decisionTimer = 0f;
                _pendingState  = EvaluateState();
            }

            _lagTimer += dt;
            if (_lagTimer >= _d.difficulty.reactionLag)
            {
                _lagTimer = 0f;
                _d.SetState(_pendingState);
            }

            Execute(dt);
        }

        AIState EvaluateState()
        {
            // SC / VSC override – handled by flag callback
            if (_d.CurrentState == AIState.SafetyCar) return AIState.SafetyCar;
            if (_d.CurrentState == AIState.VSC)       return AIState.VSC;

            // Out-lap after pit stop
            if (_d.onOutLap) return AIState.OutLap;

            if (_pit.ShouldPit()) return AIState.PitPlanning;

            if (_d.racePosition > 1 && _d.gapAhead < 1.5f && CanOvertake())
                return AIState.ChaseLeader;

            if (_d.gapBehind < 0.8f) return AIState.DefendPosition;

            if (_d.racePosition <= 3 && _d.gapAhead > 4f) return AIState.ManageGap;

            return AIState.FollowLine;
        }

        void Execute(float dt)
        {
            switch (_d.CurrentState)
            {
                case AIState.FollowLine:     DriveRacingLine(1.00f); break;
                case AIState.ChaseLeader:    DriveRacingLine(1.08f); break;
                case AIState.DefendPosition: DriveDefensive();       break;
                case AIState.PitPlanning:    ApproachPit();          break;
                case AIState.Recover:        DriveRacingLine(1.05f); break;
                case AIState.ManageGap:      DriveRacingLine(0.96f); break;
                case AIState.SafetyCar:      DriveSafetyCar();       break;
                case AIState.VSC:            DriveVSC();             break;
                case AIState.OutLap:         DriveOutLap();          break;
                case AIState.WarmTires:      DriveWarmTires();       break;
            }
        }

        void DriveRacingLine(float paceMultiplier)
        {
            if (_line == null || _line.waypoints.Count == 0) return;

            Vector3 target = _line.waypoints[_wpIdx];
            if ((_d.Vehicle.position - target).sqrMagnitude < 16f)
                _wpIdx = (_wpIdx + 1) % _line.waypoints.Count;

            // Check track limits opportunity at this waypoint
            bool isLimitZone = _line.isTrackLimitRisk != null &&
                               _line.isTrackLimitRisk.Count > _wpIdx &&
                               _line.isTrackLimitRisk[_wpIdx];

            float speedAdjust = 1f;
            if (isLimitZone)
            {
                float timeGain = 0.15f;  // approximate gain from cut
                if (_d.TrackLimitsAI.ShouldCutCorner(_wpIdx, timeGain))
                    speedAdjust = 1.04f;  // slightly push through the corner
            }

            // Steering
            Vector3 dir   = (target - _d.Vehicle.position).normalized;
            Vector3 fwd   = _d.transform.forward;
            float   cross = Vector3.Cross(fwd, dir).y;
            float   steer = Mathf.Clamp(cross * 2.5f, -1f, 1f);
            steer += Random.Range(-1f, 1f) * _d.difficulty.mistakeFrequency * 0.2f;

            float targetKph = _line.speedAtWaypoint[_wpIdx]
                            * _d.difficulty.gripUtilisation
                            * _d.EffectivePace
                            * paceMultiplier
                            * speedAdjust;

            if (_d.CurrentWeather.rainIntensity > 0.3f)
                targetKph *= Mathf.Lerp(1f, 0.78f, _d.CurrentWeather.rainIntensity);

            ApplyInputs(steer, targetKph);

            // DRS
            if (_line.isDrsZone != null && _line.isDrsZone.Count > _wpIdx)
                _d.Vehicle.drsActive = _line.isDrsZone[_wpIdx] && _d.gapAhead < 1.0f;
        }

        void DriveDefensive()
        {
            DriveRacingLine(0.97f);
            _d.Vehicle.throttle = Mathf.Min(_d.Vehicle.throttle, 0.95f);
            _d.Vehicle.drsActive = false;
        }

        void ApproachPit()
        {
            _d.Vehicle.drsActive = false;
            _d.Vehicle.ersMode   = ERSMode.Harvest;
            float spd = _d.Vehicle.speed;
            if (spd > 82f)
            {
                _d.Vehicle.throttle = 0f;
                _d.Vehicle.brake    = Mathf.Clamp01((spd - 80f) / 40f);
            }
            else
            {
                _d.Vehicle.throttle = 0.25f;
                _d.Vehicle.brake    = 0f;
            }
        }

        void DriveSafetyCar()
        {
            ApplyInputs(_d.Vehicle.steering, 80f);
            _d.Vehicle.drsActive = false;
            _d.Vehicle.ersMode   = ERSMode.Harvest;
        }

        void DriveVSC()
        {
            // VSC: hold 40% below normal pace, brake gently
            if (_line != null && _line.waypoints.Count > 0)
            {
                float targetKph = _line.speedAtWaypoint[_wpIdx] * 0.60f;
                Vector3 dir  = (_line.waypoints[_wpIdx] - _d.Vehicle.position).normalized;
                float   cross = Vector3.Cross(_d.transform.forward, dir).y;
                ApplyInputs(Mathf.Clamp(cross * 2.5f, -1f, 1f), targetKph);
            }
            _d.Vehicle.drsActive = false;
            _d.Vehicle.ersMode   = ERSMode.Harvest;
        }

        void DriveOutLap()
        {
            // Weaving and deliberate steering to generate tire heat
            float baseTarget = _line != null && _line.speedAtWaypoint.Count > _wpIdx
                             ? _line.speedAtWaypoint[_wpIdx] * 0.85f : 120f;

            // Aggressive tire-heating weave on straights
            bool onStraight = Mathf.Abs(_d.Vehicle.steering) < 0.15f && _d.Vehicle.speed > 80f;
            if (onStraight)
            {
                float weave = Mathf.Sin(Time.time * 1.5f) * 0.35f
                            * _d.difficulty.tireManagement;
                _d.Vehicle.steering = weave;
            }

            DriveRacingLine(0.88f);

            // Check if tires are up to temperature
            bool tiresReady = AreTiresInWindow();
            if (tiresReady) _d.onOutLap = false;
        }

        void DriveWarmTires()
        {
            // Gentle weave while following SC
            float weave = Mathf.Sin(Time.time * 1.2f) * 0.25f;
            _d.Vehicle.steering = Mathf.Lerp(_d.Vehicle.steering, weave, 0.3f);
            DriveSafetyCar();
        }

        bool AreTiresInWindow()
        {
            foreach (var t in _d.Vehicle.tires)
                if (t.surfaceTemp < t.OptimalTempMin * 0.85f) return false;
            return true;
        }

        bool CanOvertake()
        {
            float avgWear = 0f;
            foreach (var t in _d.Vehicle.tires) avgWear += t.wearPercent;
            if (avgWear / 4f > 75f) return false;

            float aggrBonus = _d.personality switch
            {
                AIPersonality.Aggressive   =>  0.30f,
                AIPersonality.Conservative => -0.30f,
                AIPersonality.YoungTalent  =>  0.15f,
                AIPersonality.Veteran      =>  0.08f,
                _                          =>  0.00f
            };
            return (Random.value + aggrBonus) > (1f - _d.difficulty.aggression);
        }

        void ApplyInputs(float steer, float targetKph)
        {
            float cur    = _d.Vehicle.speed;
            bool  braking = cur > targetKph;
            _d.Vehicle.steering = steer;
            _d.Vehicle.throttle = braking ? 0f : Mathf.Clamp01((targetKph - cur) / 60f);
            _d.Vehicle.brake    = braking ? Mathf.Clamp01((cur - targetKph) / 90f) : 0f;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PIT STRATEGY CALCULATOR  (with SC/VSC free-stop logic)
    // ═══════════════════════════════════════════════════════════════════════
    public class PitStrategyCalc
    {
        readonly AIDriver _d;

        const float TIRE_THRESHOLD    = 72f;
        const float FUEL_THRESHOLD    = 6f;
        const float SC_PIT_WEAR_MIN   = 25f;  // pit under SC even at 25% wear
        const float VSC_PIT_WEAR_MIN  = 35f;

        bool  _safetyCarActive = false;
        bool  _vscActive       = false;
        bool  _scPitDecided    = false;   // prevent flip-flopping under SC

        public PitStrategyCalc(AIDriver d) => _d = d;

        public void NotifySafetyCar()
        {
            _safetyCarActive = true;
            _vscActive       = false;
            _scPitDecided    = false;
        }

        public void NotifyVSC()
        {
            _vscActive       = true;
            _safetyCarActive = false;
            _scPitDecided    = false;
        }

        public void NotifyGreenFlag()
        {
            _safetyCarActive = false;
            _vscActive       = false;
        }

        public bool ShouldPit()
        {
            if (_d.inPit) return false;

            float avgWear = AvgWear();

            // ── Safety Car: free stop window ──────────────────────────────
            if (_safetyCarActive && !_scPitDecided)
            {
                bool decision = EvaluateSafetyCarPit(avgWear);
                _scPitDecided = true;
                if (decision) return true;
            }

            // ── VSC: cheaper stop window ──────────────────────────────────
            if (_vscActive && !_scPitDecided)
            {
                bool decision = EvaluateVSCPit(avgWear);
                _scPitDecided = true;
                if (decision) return true;
            }

            // ── Normal race conditions ────────────────────────────────────
            if (avgWear >= TIRE_THRESHOLD)           return true;
            if (_d.Vehicle.fuelLoad <= FUEL_THRESHOLD) return true;

            bool needsWet = _d.CurrentWeather.RequiresWetTires &&
                            _d.Vehicle.tires[0].compound <= TireCompound.Hard;
            if (needsWet) return true;

            return CanUndercut(avgWear);
        }

        bool EvaluateSafetyCarPit(float avgWear)
        {
            // Under SC the pit-lane time loss is ~halved → lower threshold to pit
            int lapsLeft = Mathf.Max(1, _d.totalLaps - _d.lapNumber);

            // Must pit if: tires worn enough to matter AND enough laps left to benefit
            bool tiresWorthChanging = avgWear > SC_PIT_WEAR_MIN;
            bool enoughRaceLeft     = lapsLeft >= 5;
            bool wouldLosePosition  = _d.racePosition <= 3 && _d.gapAhead > 8f;

            // High-strategy IQ drivers always take the free stop
            if (_d.difficulty.strategyIQ > 0.85f && tiresWorthChanging && enoughRaceLeft)
                return true;

            // Aggressive personality more willing to sacrifice track position
            bool aggressive = _d.personality == AIPersonality.Aggressive ||
                              _d.personality == AIPersonality.YoungTalent;

            if (aggressive && tiresWorthChanging && enoughRaceLeft)
                return true;

            // If losing positions anyway, always pit
            if (wouldLosePosition && tiresWorthChanging)
                return true;

            // Random chance scaled by strategy IQ for remaining cases
            return tiresWorthChanging && enoughRaceLeft &&
                   Random.value < _d.difficulty.strategyIQ * 0.8f;
        }

        bool EvaluateVSCPit(float avgWear)
        {
            // VSC reduces pit loss by ~30% – only worth it with meaningful wear
            int lapsLeft    = Mathf.Max(1, _d.totalLaps - _d.lapNumber);
            bool worthwhile = avgWear > VSC_PIT_WEAR_MIN && lapsLeft >= 8;
            return worthwhile && Random.value < _d.difficulty.strategyIQ * 0.65f;
        }

        bool CanUndercut(float avgWear)
        {
            bool aggressive = _d.personality == AIPersonality.Aggressive ||
                              _d.personality == AIPersonality.YoungTalent;
            return aggressive
                && _d.gapAhead < 1.5f
                && avgWear > 48f
                && _d.difficulty.strategyIQ > 0.75f;
        }

        public TireCompound SelectNextCompound()
        {
            int lapsLeft = Mathf.Max(1, _d.totalLaps - _d.lapNumber);
            if (_d.CurrentWeather.RequiresWetTires)   return TireCompound.Wet;
            if (_d.CurrentWeather.RequiresInterTires) return TireCompound.Inter;
            if (lapsLeft <= 12) return TireCompound.Soft;
            if (lapsLeft <= 28) return TireCompound.Medium;
            return TireCompound.Hard;
        }

        float AvgWear()
        {
            float t = 0f;
            foreach (var tire in _d.Vehicle.tires) t += tire.wearPercent;
            return t / 4f;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AI PERFORMANCE STATS
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class AIPerformanceData
    {
        public float lapTimeDelta;
        public float overtakeSuccessRate;
        public float crashFrequency;
        public float pitQuality;
        public float strategyScore;
        public float ersEfficiency;
        public int   trackLimitCuts;
    }
}
