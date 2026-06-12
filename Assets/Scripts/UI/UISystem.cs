using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Data;
using RacingGame.Physics;
using RacingGame.Race;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  UI SYSTEM  –  2026 Extension
    //  Adds: ERS_HUD (battery bar, deploy mode, overtake button),
    //        TV-style timing tower (live scrolling leaderboard),
    //        Active aero indicator, tyre compound badges,
    //        VSC / night mode status banners.
    //  All original classes retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════════════
    //  ERS HUD  (2026 new)
    // ═══════════════════════════════════════════════════════════════════════

    public class ERS_HUD : MonoBehaviour
    {
        [Header("Battery")]
        public Image           batteryFill;       // 0-1 fill, green → red
        public TextMeshProUGUI batteryPctText;

        [Header("Deploy Lap Budget")]
        public Image           lapBudgetFill;     // how much of 4000 kJ used this lap
        public TextMeshProUGUI lapBudgetText;

        [Header("Deploy Mode")]
        public TextMeshProUGUI deployModeText;
        public Button[]        deployModeButtons; // Hotlap / Race_High / Race_Medium / Race_Low / Harvest

        [Header("Overtake")]
        public Button          overtakeButton;
        public Image           overtakeCooldownFill;  // grey-out during cooldown
        public TextMeshProUGUI overtakeCooldownText;
        public GameObject      overtakeModeActivePanel;  // visible while mode is live

        [Header("Current Deploy kW")]
        public Image           deployKwBar;
        public TextMeshProUGUI deployKwText;

        // ── Events ────────────────────────────────────────────────────────
        public System.Action              OnOvertakeRequested;
        public System.Action<DeploymentMode> OnDeployModeChanged;

        // ── State ─────────────────────────────────────────────────────────
        private ERSState2026   _ersState;
        private DeploymentMode _currentMode = DeploymentMode.Race_Medium;
        private bool           _overtakeModeActive;

        static readonly Color COL_GREEN   = new(0.10f, 0.90f, 0.20f);
        static readonly Color COL_AMBER   = new(1.00f, 0.75f, 0.00f);
        static readonly Color COL_RED     = new(0.90f, 0.10f, 0.10f);
        static readonly Color COL_BLUE    = new(0.15f, 0.55f, 1.00f);
        static readonly Color COL_OVERTAKE = new(1.00f, 0.20f, 0.00f);

        void Awake()
        {
            // Wire deploy mode buttons
            string[] modeLabels = { "HOT", "HI", "MED", "LO", "HARV" };
            DeploymentMode[] modes =
            {
                DeploymentMode.Hotlap,
                DeploymentMode.Race_High,
                DeploymentMode.Race_Medium,
                DeploymentMode.Race_Low,
                DeploymentMode.Harvest
            };

            if (deployModeButtons != null)
            {
                for (int i = 0; i < deployModeButtons.Length && i < modes.Length; i++)
                {
                    int idx = i;
                    deployModeButtons[i]?.onClick.AddListener(() => SetDeployMode(modes[idx]));
                }
            }

            overtakeButton?.onClick.AddListener(() => OnOvertakeRequested?.Invoke());
        }

        /// <summary>Called every frame by HudBuilder (or directly from RaceDirector telemetry).</summary>
        public void Refresh(ERSState2026 ers)
        {
            if (ers == null) return;
            _ersState = ers;

            // ── Battery bar ───────────────────────────────────────────────
            float battPct = ers.BatteryPercent;
            if (batteryFill)
            {
                batteryFill.fillAmount = battPct / 100f;
                batteryFill.color = battPct > 50f ? COL_GREEN
                                  : battPct > 25f ? COL_AMBER
                                  : COL_RED;
            }
            if (batteryPctText) batteryPctText.text = $"{battPct:F0}%";

            // ── Lap deploy budget ─────────────────────────────────────────
            float budgetUsed = ers.lapDeployKJ / 4000f;
            if (lapBudgetFill)
            {
                lapBudgetFill.fillAmount = budgetUsed;
                lapBudgetFill.color = budgetUsed > 0.85f ? COL_RED
                                    : budgetUsed > 0.60f ? COL_AMBER
                                    : COL_BLUE;
            }
            if (lapBudgetText)
                lapBudgetText.text = ers.deployLimitReached
                    ? "LIMIT" : $"{ers.lapDeployKJ:F0}/{4000} kJ";

            // ── Current deploy kW bar ─────────────────────────────────────
            if (deployKwBar)
            {
                deployKwBar.fillAmount = ers.currentDeployKw / 350f;
                deployKwBar.color      = _overtakeModeActive ? COL_OVERTAKE : COL_BLUE;
            }
            if (deployKwText) deployKwText.text = $"{ers.currentDeployKw:F0} kW";

            // ── Overtake cooldown ─────────────────────────────────────────
            bool canOvertake = !ers.overtakeModeActive && ers.overtakeCooldown <= 0f
                               && ers.storedEnergyKJ >= 300f && !ers.deployLimitReached;

            if (overtakeButton) overtakeButton.interactable = canOvertake;

            if (overtakeCooldownFill)
            {
                overtakeCooldownFill.fillAmount = ers.overtakeCooldown > 0f
                    ? ers.overtakeCooldown / 25f : 0f;
            }
            if (overtakeCooldownText)
            {
                overtakeCooldownText.text = ers.overtakeCooldown > 0f
                    ? $"{ers.overtakeCooldown:F1}s" : "";
            }

            // ── Overtake mode active panel ────────────────────────────────
            if (overtakeModeActivePanel)
                overtakeModeActivePanel.SetActive(ers.overtakeModeActive);

            // Sync deploy mode label
            if (deployModeText) deployModeText.text = FormatMode(_currentMode);
        }

        public void SetOvertakeModeVisual(bool active)
        {
            _overtakeModeActive = active;
            if (overtakeModeActivePanel) overtakeModeActivePanel.SetActive(active);
        }

        void SetDeployMode(DeploymentMode mode)
        {
            _currentMode = mode;
            OnDeployModeChanged?.Invoke(mode);
            if (deployModeText) deployModeText.text = FormatMode(mode);
        }

        static string FormatMode(DeploymentMode m) => m switch
        {
            DeploymentMode.Hotlap      => "HOTLAP",
            DeploymentMode.Race_High   => "HI",
            DeploymentMode.Race_Medium => "MED",
            DeploymentMode.Race_Low    => "LO",
            DeploymentMode.Harvest     => "HARV",
            DeploymentMode.Overtake    => "OVT",
            _                          => "MED"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ACTIVE AERO INDICATOR  (2026 new — cockpit status widget)
    // ═══════════════════════════════════════════════════════════════════════

    public class ActiveAeroIndicator : MonoBehaviour
    {
        [Header("Wing angle gauge")]
        public Image            aeroFill;          // 0 = low drag, 1 = max downforce
        public TextMeshProUGUI  aeroAngleText;
        public TextMeshProUGUI  aeroModeLabel;
        public Image            aeroModeIcon;

        [Header("Colors")]
        public Color lowDragColor      = new(0.1f, 0.9f, 0.2f);
        public Color balancedColor     = new(1.0f, 0.85f, 0.0f);
        public Color maxDownforceColor = new(0.9f, 0.1f, 0.1f);

        public void Refresh(ActiveAeroState aero, ActiveAeroMode mode)
        {
            if (aero == null) return;

            float t = Mathf.InverseLerp(-2.5f, 18.0f, aero.currentAngleDeg);

            if (aeroFill)
            {
                aeroFill.fillAmount = t;
                aeroFill.color = Color.Lerp(lowDragColor, maxDownforceColor, t);
            }

            if (aeroAngleText)
                aeroAngleText.text = $"{aero.currentAngleDeg:F1}°";

            if (aeroModeLabel)
                aeroModeLabel.text = mode switch
                {
                    ActiveAeroMode.LowDrag      => "LOW DRAG",
                    ActiveAeroMode.Balanced     => "BALANCED",
                    ActiveAeroMode.MaxDownforce => "DOWNFORCE",
                    ActiveAeroMode.Auto         => "AUTO",
                    _                           => "AUTO"
                };

            // Transitioning: flash the label
            if (aero.isTransitioning && aeroModeLabel)
                aeroModeLabel.color = Color.Lerp(Color.white, balancedColor,
                    Mathf.PingPong(Time.time * 4f, 1f));
            else if (aeroModeLabel)
                aeroModeLabel.color = Color.white;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TV TIMING TOWER  (2026 new — live scrolling leaderboard)
    // ═══════════════════════════════════════════════════════════════════════

    public class TVTimingTower : MonoBehaviour
    {
        [Header("Row prefab")]
        public GameObject     timingRowPrefab;    // has: pos, code, team, gap, tyre, drs, ers fields
        public Transform      rowContainer;

        [Header("Header")]
        public TextMeshProUGUI lapCounterHeader;
        public TextMeshProUGUI raceTimeHeader;
        public TextMeshProUGUI flagStatusHeader;
        public Image           flagStatusBar;

        [Header("Refresh")]
        public float refreshInterval = 0.5f;      // seconds between full redraws

        // ── Internal ──────────────────────────────────────────────────────
        float _refreshTimer;
        readonly List<TimingTowerRow> _rows = new();
        RaceDirector _director;

        // ── Colors ────────────────────────────────────────────────────────
        static readonly Color COL_LEADER    = new(1.00f, 0.85f, 0.00f);
        static readonly Color COL_PLAYER    = new(0.30f, 0.70f, 1.00f);
        static readonly Color COL_RETIRED   = new(0.50f, 0.50f, 0.50f);
        static readonly Color COL_SC        = new(1.00f, 0.85f, 0.00f);
        static readonly Color COL_VSC       = new(0.30f, 0.60f, 1.00f);
        static readonly Color COL_GREEN_F   = new(0.10f, 0.90f, 0.20f);
        static readonly Color COL_RED_F     = new(0.90f, 0.10f, 0.10f);
        static readonly Color COL_SOFT      = new(1.00f, 0.20f, 0.20f);
        static readonly Color COL_MEDIUM    = new(1.00f, 0.85f, 0.00f);
        static readonly Color COL_HARD      = new(0.90f, 0.90f, 0.90f);
        static readonly Color COL_INTER     = new(0.10f, 0.80f, 0.30f);
        static readonly Color COL_WET_TYRE  = new(0.20f, 0.50f, 1.00f);

        public void Initialise(RaceDirector director)
        {
            _director = director;
            BuildRows(director.Entries.Count);
        }

        void BuildRows(int count)
        {
            // Clear old rows
            foreach (Transform child in rowContainer)
                Destroy(child.gameObject);
            _rows.Clear();

            for (int i = 0; i < count; i++)
            {
                var go  = Instantiate(timingRowPrefab, rowContainer);
                var row = go.GetComponent<TimingTowerRow>() ?? go.AddComponent<TimingTowerRow>();
                _rows.Add(row);
            }
        }

        void Update()
        {
            if (_director == null) return;
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < refreshInterval) return;
            _refreshTimer = 0f;

            RefreshAll();
        }

        void RefreshAll()
        {
            // Header
            if (lapCounterHeader && _director != null)
            {
                var leader = _director.Entries.OrderBy(e => e.position).FirstOrDefault();
                lapCounterHeader.text = leader != null
                    ? $"LAP {leader.currentLap}/{_director.totalLaps}"
                    : "---";
            }
            if (raceTimeHeader)
                raceTimeHeader.text = FormatRaceTime(_director.RaceTime);

            // Flag bar
            RefreshFlagBanner();

            // Driver rows
            var sorted = _director.Entries
                .OrderBy(e => e.retired ? 999 : e.position)
                .ToList();

            DriverEntry leaderEntry = sorted.FirstOrDefault(e => !e.retired);

            for (int i = 0; i < _rows.Count && i < sorted.Count; i++)
            {
                var entry = sorted[i];
                var row   = _rows[i];
                if (row == null) continue;

                // Position
                row.SetPosition(entry.position, entry.isPlayer, entry.retired);

                // Driver code
                row.SetDriverCode(entry.driverName?.Length >= 3
                    ? entry.driverName.Substring(0, 3).ToUpper()
                    : entry.driverName?.ToUpper() ?? "---");

                // Team colour (simplified — use team name hash)
                row.SetTeamColor(TeamColorFromName(entry.teamName));

                // Gap
                if (entry.retired)
                    row.SetGap("OUT", COL_RETIRED);
                else if (entry == leaderEntry)
                    row.SetGap("LEADER", COL_LEADER);
                else
                {
                    float gap = entry.gapAhead;
                    row.SetGap(gap < 60f ? $"+{gap:F3}" : $"+{gap:F1}s", Color.white);
                }

                // 2026 Tyre compound badge
                var tyreStates = _director.GetTyreStates(entry.driverId);
                if (tyreStates != null && tyreStates.Length > 0)
                    row.SetTyreCompound(tyreStates[0].currentCompound);

                // 2026 ERS battery indicator (mini bar)
                var ersState = _director.GetERSState(entry.driverId);
                if (ersState != null)
                    row.SetERSBar(ersState.BatteryPercent / 100f,
                                  ersState.overtakeModeActive);

                // Pit status
                row.SetPitIndicator(entry.inPit);
            }
        }

        void RefreshFlagBanner()
        {
            if (_director == null) return;
            var flag = _director.DirectorState.flag;

            if (flagStatusBar)
            {
                flagStatusBar.color = flag switch
                {
                    FlagStatus.Green      => COL_GREEN_F,
                    FlagStatus.Yellow     => COL_SC,
                    FlagStatus.SafetyCar  => COL_SC,
                    FlagStatus.VSC        => COL_VSC,
                    FlagStatus.Red        => COL_RED_F,
                    FlagStatus.Chequered  => Color.white,
                    _                    => COL_GREEN_F
                };
            }

            if (flagStatusHeader)
            {
                flagStatusHeader.text = flag switch
                {
                    FlagStatus.SafetyCar  => "SAFETY CAR",
                    FlagStatus.VSC        => "VIRTUAL SAFETY CAR",
                    FlagStatus.Red        => "RED FLAG",
                    FlagStatus.Chequered  => "CHEQUERED FLAG",
                    _                    => ""
                };
                flagStatusHeader.gameObject.SetActive(flag != FlagStatus.Green);
            }
        }

        static Color TeamColorFromName(string teamName)
        {
            // Deterministic colour from team name hash
            if (string.IsNullOrEmpty(teamName)) return Color.white;
            var hash = teamName.GetHashCode();
            float h = ((hash & 0xFF) / 255f);
            return Color.HSVToRGB(h, 0.75f, 0.90f);
        }

        static string FormatRaceTime(float t)
        {
            int hours = (int)(t / 3600f);
            int mins  = (int)((t % 3600f) / 60f);
            int secs  = (int)(t % 60f);
            return hours > 0 ? $"{hours}:{mins:00}:{secs:00}" : $"{mins}:{secs:00}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Timing Tower Row component — attaches to each row prefab instance
    // ─────────────────────────────────────────────────────────────────────

    public class TimingTowerRow : MonoBehaviour
    {
        [Header("Fields")]
        public TextMeshProUGUI positionText;
        public TextMeshProUGUI driverCodeText;
        public Image           teamColorBar;
        public TextMeshProUGUI gapText;
        public Image           tyreCompoundIcon;
        public TextMeshProUGUI tyreCompoundLetter;
        public Image           ersMiniBar;
        public GameObject      pitIndicator;
        public GameObject      overtakeIndicator;
        public Image           rowBackground;

        static readonly Color COL_PLAYER_BG  = new(0.20f, 0.45f, 0.80f, 0.35f);
        static readonly Color COL_DEFAULT_BG  = new(0.08f, 0.08f, 0.10f, 0.70f);
        static readonly Color COL_RETIRED_BG  = new(0.20f, 0.20f, 0.20f, 0.50f);

        public void SetPosition(int pos, bool isPlayer, bool retired)
        {
            if (positionText)
            {
                positionText.text  = retired ? "RET" : pos.ToString();
                positionText.color = pos == 1 ? new Color(1f, 0.85f, 0f) : Color.white;
            }
            if (rowBackground)
                rowBackground.color = retired ? COL_RETIRED_BG
                                    : isPlayer ? COL_PLAYER_BG
                                    : COL_DEFAULT_BG;
        }

        public void SetDriverCode(string code)
        {
            if (driverCodeText) driverCodeText.text = code;
        }

        public void SetTeamColor(Color c)
        {
            if (teamColorBar) teamColorBar.color = c;
        }

        public void SetGap(string gap, Color col)
        {
            if (gapText) { gapText.text = gap; gapText.color = col; }
        }

        public void SetTyreCompound(TyreCompound2026 compound)
        {
            Color tyreColor = compound switch
            {
                TyreCompound2026.Soft         => new Color(1.00f, 0.20f, 0.20f),
                TyreCompound2026.Medium       => new Color(1.00f, 0.85f, 0.00f),
                TyreCompound2026.Hard         => new Color(0.90f, 0.90f, 0.90f),
                TyreCompound2026.Intermediate => new Color(0.10f, 0.80f, 0.30f),
                TyreCompound2026.Wet          => new Color(0.20f, 0.50f, 1.00f),
                _                             => Color.white
            };
            string letter = compound switch
            {
                TyreCompound2026.Soft         => "S",
                TyreCompound2026.Medium       => "M",
                TyreCompound2026.Hard         => "H",
                TyreCompound2026.Intermediate => "I",
                TyreCompound2026.Wet          => "W",
                _                             => "?"
            };

            if (tyreCompoundIcon)   tyreCompoundIcon.color    = tyreColor;
            if (tyreCompoundLetter) tyreCompoundLetter.text   = letter;
            if (tyreCompoundLetter) tyreCompoundLetter.color  = compound == TyreCompound2026.Hard
                                                                   ? Color.black : Color.white;
        }

        public void SetERSBar(float batteryNorm, bool overtakeActive)
        {
            if (ersMiniBar)
            {
                ersMiniBar.fillAmount = batteryNorm;
                ersMiniBar.color = overtakeActive ? new Color(1f, 0.2f, 0f)
                                  : batteryNorm > 0.5f ? new Color(0.2f, 0.6f, 1f)
                                  : new Color(1f, 0.75f, 0f);
            }
            if (overtakeIndicator) overtakeIndicator.SetActive(overtakeActive);
        }

        public void SetPitIndicator(bool inPit)
        {
            if (pitIndicator) pitIndicator.SetActive(inPit);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ORIGINAL CLASSES  (unchanged)
    // ═══════════════════════════════════════════════════════════════════════

    public class HudData
    {
        public float  speed;
        public int    gear;
        public float  rpm;
        public float  throttlePercent;
        public float  brakePercent;
        public float  ersDeploy;
        public float  ersEnergy;
        public float[] tireWear      = new float[4];
        public float[] tireTemps     = new float[4];
        public string[] tireCompounds = new string[4];
        public float  fuelRemaining;
        public float  fuelPercent;
        public int    lapsOfFuelLeft;
        public int    racePosition;
        public int    totalCars;
        public float  gapToLeader;
        public float  gapToAhead;
        public float  gapToBehind;
        public string driverAheadName;
        public string driverBehindName;
        public int    currentLap;
        public int    totalLaps;
        public float  lastLapTime;
        public float  bestLapTime;
        public float  currentLapTime;
        public float  deltaToPersonalBest;
        public float  deltaToLeader;
        public float[] sectorTimes      = new float[3];
        public float[] sectorBestTimes  = new float[3];
        public int    currentSector;
        public bool   drsActive;
        public bool   drsEligible;
        public bool   drsAvailableZone;
        public FlagStatus       currentFlag;
        public WeatherCondition weather;
        public float            trackWetness;
        public EngineMode       engineMode;
        public bool             damageWarning;
        public DamageLevel      damageLevel;
        public bool             tyrePressureWarning;
        // 2026 additions
        public ERSState2026     ersState2026;
        public ActiveAeroState  aeroState;
        public TyreCompound2026 frontLeftCompound2026;
        public bool             overtakeModeActive;
        public bool             vscActive;
    }

    public class TelemetrySample
    {
        public float  time;
        public float  speed;
        public float  throttle;
        public float  brake;
        public float  steering;
        public float  gLong;
        public float  gLat;
        public float  rpm;
        public float[] tireTemps = new float[4];
        public float[] tireWear  = new float[4];
        public float  fuel;
        public float  lapDistance;
    }

    public class TelemetryRecorder : MonoBehaviour
    {
        const float SAMPLE_INTERVAL = 0.05f;
        float _timer;
        public float MaxRecordSeconds = 120f;

        readonly List<TelemetrySample> _samples = new();
        public IReadOnlyList<TelemetrySample> Samples => _samples;

        PhysicsIntegrator _physics;
        void Awake() => _physics = GetComponent<PhysicsIntegrator>();

        void Update()
        {
            if (_physics == null) return;
            _timer += Time.deltaTime;
            if (_timer < SAMPLE_INTERVAL) return;
            _timer = 0f;

            var s   = _physics.State;
            var rec = new TelemetrySample
            {
                time     = Time.time, speed = s.speed, throttle = s.throttle,
                brake    = s.brake,   steering = s.steering,
                gLong    = s.gLongitudinal, gLat = s.gLateral,
                rpm      = s.rpm,     fuel = s.fuelLoad
            };
            for (int i = 0; i < 4; i++)
            {
                rec.tireTemps[i] = s.tires[i].temperature;
                rec.tireWear[i]  = s.tires[i].wearPercent;
            }
            _samples.Add(rec);

            float cutoff = Time.time - MaxRecordSeconds;
            while (_samples.Count > 0 && _samples[0].time < cutoff) _samples.RemoveAt(0);
        }

        public void ClearSession() => _samples.Clear();
    }

    public class HudBuilder : MonoBehaviour
    {
        public PhysicsIntegrator  physics;
        public RaceDirector       raceDirector;
        public float              maxFuel = 110f;
        public string             localDriverId;

        // 2026 additions
        public ERS_HUD            ersHud;
        public ActiveAeroIndicator aeroIndicator;
        public TVTimingTower      timingTower;

        public HudData Current { get; private set; } = new();

        void Update()
        {
            if (physics == null) return;
            var s = physics.State;
            var h = Current;

            h.speed           = s.speed;
            h.gear            = s.gear;
            h.rpm             = s.rpm;
            h.throttlePercent = s.throttle * 100f;
            h.brakePercent    = s.brake    * 100f;
            h.ersDeploy       = s.ersDeploy  * 100f;
            h.ersEnergy       = s.ersEnergy  * 100f;
            h.fuelRemaining   = s.fuelLoad;
            h.fuelPercent     = s.fuelLoad / maxFuel * 100f;
            h.drsActive       = s.drsActive;
            h.drsEligible     = s.drsEligible;
            h.engineMode      = s.engineMode;
            h.damageLevel     = s.damage.OverallLevel;
            h.damageWarning   = s.damage.OverallLevel >= DamageLevel.Moderate;

            for (int i = 0; i < 4; i++)
            {
                h.tireWear[i]       = s.tires[i].wearPercent;
                h.tireTemps[i]      = s.tires[i].temperature;
                h.tireCompounds[i]  = s.tires[i].compound.ToString()[0].ToString();
            }

            if (raceDirector != null)
            {
                var entry = raceDirector.Entries.FirstOrDefault(e => e.driverId == localDriverId);
                if (entry != null)
                {
                    h.racePosition   = entry.position;
                    h.currentLap     = entry.currentLap;
                    h.totalLaps      = raceDirector.totalLaps;
                    h.lastLapTime    = entry.lastLapTime;
                    h.bestLapTime    = entry.bestLapTime;
                    h.currentLapTime = raceDirector.RaceTime - entry.lapStartTime;
                    h.gapToAhead     = entry.gapAhead;
                    h.gapToBehind    = entry.gapBehind;
                }
                h.currentFlag  = raceDirector.DirectorState.flag;
                h.weather      = raceDirector.weather.condition;
                h.trackWetness = raceDirector.weather.trackWetness;
                h.totalCars    = raceDirector.Entries.Count;

                // 2026 feeds
                var telemetry = raceDirector.GetPlayerTelemetry(localDriverId);
                if (telemetry != null)
                {
                    h.ersState2026       = telemetry.ersState;
                    h.aeroState          = telemetry.aeroState;
                    h.overtakeModeActive = telemetry.ersState?.overtakeModeActive ?? false;
                    h.vscActive          = telemetry.vscActive;
                }
            }

            // Push to 2026 sub-HUDs
            if (ersHud != null && h.ersState2026 != null)
                ersHud.Refresh(h.ersState2026);

            if (aeroIndicator != null && h.aeroState != null)
                aeroIndicator.Refresh(h.aeroState, ActiveAeroMode.Auto);
        }
    }

    public class RacingHUD : MonoBehaviour
    {
        [Header("Speed / Gear")]
        public TextMeshProUGUI speedText;
        public TextMeshProUGUI gearText;
        public Image           rpmBar;

        [Header("Pedals")]
        public Image throttleBar;
        public Image brakeBar;

        [Header("Tires")]
        public Image[]           tireWearImages    = new Image[4];
        public TextMeshProUGUI[] tireTempTexts     = new TextMeshProUGUI[4];
        public TextMeshProUGUI[] tireCompoundTexts = new TextMeshProUGUI[4];

        [Header("Fuel")]
        public Image           fuelBar;
        public TextMeshProUGUI fuelText;

        [Header("Timing")]
        public TextMeshProUGUI positionText;
        public TextMeshProUGUI currentLapText;
        public TextMeshProUGUI lastLapText;
        public TextMeshProUGUI bestLapText;
        public TextMeshProUGUI deltaText;
        public TextMeshProUGUI lapCounterText;

        [Header("Gaps")]
        public TextMeshProUGUI gapAheadText;
        public TextMeshProUGUI gapBehindText;

        [Header("DRS")]
        public GameObject drsActivePanel;
        public GameObject drsEligiblePanel;

        [Header("Flag")]
        public Image           flagBar;
        public TextMeshProUGUI flagText;

        [Header("Engine Mode")]
        public TextMeshProUGUI engineModeText;

        [Header("Damage")]
        public GameObject      damageWarningPanel;
        public TextMeshProUGUI damageText;

        [Header("ERS")]
        public Image ersDeployBar;
        public Image ersEnergyBar;

        // 2026
        [Header("— 2026 —")]
        public GameObject overtakeModePanel;
        public GameObject vscPanel;
        public TextMeshProUGUI vscText;

        static readonly Color COL_GREEN  = new(0.0f, 0.9f, 0.2f);
        static readonly Color COL_YELLOW = new(1.0f, 0.85f, 0.0f);
        static readonly Color COL_RED    = new(0.9f, 0.1f, 0.1f);
        static readonly Color COL_PURPLE = new(0.6f, 0.0f, 0.9f);
        static readonly Color COL_WHITE  = Color.white;
        static readonly Color COL_BLUE   = new(0.15f, 0.55f, 1.0f);

        HudBuilder _builder;
        void Awake() => _builder = GetComponentInParent<HudBuilder>();

        void Update()
        {
            if (_builder == null) return;
            var h = _builder.Current;
            if (h == null) return;

            ApplySpeed(h);    ApplyPedals(h);  ApplyTires(h);
            ApplyFuel(h);     ApplyTiming(h);  ApplyDRS(h);
            ApplyFlag(h);     ApplyERS(h);     ApplyEngineMode(h);
            ApplyDamage(h);   Apply2026(h);
        }

        void Apply2026(HudData h)
        {
            if (overtakeModePanel) overtakeModePanel.SetActive(h.overtakeModeActive);

            if (vscPanel)
            {
                vscPanel.SetActive(h.vscActive);
                if (vscText && h.vscActive) vscText.color = Color.Lerp(
                    COL_BLUE, Color.white, Mathf.PingPong(Time.time * 2f, 1f));
            }
        }

        void ApplySpeed(HudData h)
        {
            if (speedText) speedText.text = Mathf.RoundToInt(h.speed).ToString();
            if (gearText)
            {
                gearText.text  = h.gear == 0 ? "N" : h.gear.ToString();
                gearText.color = h.gear >= 7 ? COL_RED : COL_WHITE;
            }
            if (rpmBar) rpmBar.fillAmount = h.rpm / 15000f;
        }

        void ApplyPedals(HudData h)
        {
            if (throttleBar) throttleBar.fillAmount = h.throttlePercent / 100f;
            if (brakeBar)    brakeBar.fillAmount    = h.brakePercent    / 100f;
        }

        void ApplyTires(HudData h)
        {
            for (int i = 0; i < 4; i++)
            {
                if (tireWearImages[i])   tireWearImages[i].fillAmount   = 1f - h.tireWear[i] / 100f;
                if (tireTempTexts[i])    tireTempTexts[i].text          = Mathf.RoundToInt(h.tireTemps[i]) + "°";
                if (tireCompoundTexts[i]) tireCompoundTexts[i].text     = h.tireCompounds[i];
                if (tireWearImages[i])
                {
                    float t = h.tireTemps[i];
                    tireWearImages[i].color = t < 50f ? Color.blue : t < 70f ? COL_WHITE
                                           : t < 100f ? COL_GREEN : COL_RED;
                }
            }
        }

        void ApplyFuel(HudData h)
        {
            if (fuelBar)  fuelBar.fillAmount = h.fuelPercent / 100f;
            if (fuelText) fuelText.text      = $"{h.fuelRemaining:F1}L";
            if (fuelBar)  fuelBar.color      = h.fuelPercent < 15f ? COL_RED
                                             : h.fuelPercent < 30f ? COL_YELLOW : COL_GREEN;
        }

        void ApplyTiming(HudData h)
        {
            if (positionText)   positionText.text   = $"P{h.racePosition}";
            if (lapCounterText) lapCounterText.text = $"LAP {h.currentLap}/{h.totalLaps}";
            if (currentLapText) currentLapText.text = FormatTime(h.currentLapTime);
            if (lastLapText)    lastLapText.text    = FormatTime(h.lastLapTime);
            if (bestLapText)    bestLapText.text    = FormatTime(h.bestLapTime);

            if (deltaText)
            {
                float delta = h.currentLapTime - h.bestLapTime;
                deltaText.text  = (delta >= 0 ? "+" : "") + $"{delta:F3}";
                deltaText.color = delta < 0 ? COL_PURPLE : delta < 0.3f ? COL_GREEN : COL_YELLOW;
            }

            if (gapAheadText)
                gapAheadText.text = h.gapToAhead >= 999f ? "---" : $"+{h.gapToAhead:F3} {h.driverAheadName}";
            if (gapBehindText)
                gapBehindText.text = h.gapToBehind >= 999f ? "---" : $"-{h.gapToBehind:F3} {h.driverBehindName}";
        }

        void ApplyDRS(HudData h)
        {
            if (drsActivePanel)   drsActivePanel.SetActive(h.drsActive);
            if (drsEligiblePanel) drsEligiblePanel.SetActive(h.drsEligible && !h.drsActive);
        }

        void ApplyFlag(HudData h)
        {
            if (flagBar == null) return;
            flagBar.color = h.currentFlag switch
            {
                FlagStatus.Green     => COL_GREEN,
                FlagStatus.Yellow    => COL_YELLOW,
                FlagStatus.SafetyCar => COL_YELLOW,
                FlagStatus.VSC       => COL_BLUE,
                FlagStatus.Red       => COL_RED,
                FlagStatus.Chequered => COL_WHITE,
                _ => COL_GREEN
            };
            if (flagText) flagText.text = h.currentFlag switch
            {
                FlagStatus.SafetyCar          => "SAFETY CAR",
                FlagStatus.VSC                => "VSC",
                FlagStatus.VirtualSafetyCar   => "VSC",
                FlagStatus.Red                => "RED FLAG",
                FlagStatus.Chequered          => "CHEQUERED",
                _ => ""
            };
        }

        void ApplyERS(HudData h)
        {
            if (ersDeployBar) ersDeployBar.fillAmount = h.ersDeploy  / 100f;
            if (ersEnergyBar)
            {
                ersEnergyBar.fillAmount = h.ersEnergy / 100f;
                ersEnergyBar.color = h.ersEnergy < 20f ? COL_RED : COL_GREEN;
            }
        }

        void ApplyEngineMode(HudData h)
        {
            if (!engineModeText) return;
            engineModeText.text  = h.engineMode.ToString().ToUpper();
            engineModeText.color = h.engineMode switch
            {
                EngineMode.Eco      => Color.cyan,
                EngineMode.Push     => COL_YELLOW,
                EngineMode.Overtake => COL_RED,
                _ => COL_WHITE
            };
        }

        void ApplyDamage(HudData h)
        {
            if (damageWarningPanel) damageWarningPanel.SetActive(h.damageWarning);
            if (damageText && h.damageWarning)
                damageText.text = h.damageLevel.ToString().ToUpper() + " DAMAGE";
        }

        static string FormatTime(float t)
        {
            if (t <= 0f || t == float.MaxValue) return "--:--.---";
            int min  = (int)(t / 60f);
            float sec = t % 60f;
            return $"{min}:{sec:00.000}";
        }
    }

    [System.Serializable]
    public class DriverStanding
    {
        public string driverName;
        public string driverCode;
        public string teamName;
        public int    points;
        public int    wins;
        public int    podiums;
        public int    position;
        public int    pointsDelta;
    }

    public static class StandingsCalculator
    {
        static readonly int[] POINTS = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

        public static List<DriverStanding> BuildDriverStandings(
            List<RaceResult> allResults, Dictionary<string, string> driverTeamMap)
        {
            var grouped = allResults
                .GroupBy(r => r.driverName)
                .Select(g => new DriverStanding
                {
                    driverName = g.Key,
                    driverCode = g.Key.Length >= 3 ? g.Key.Substring(0, 3).ToUpper() : g.Key.ToUpper(),
                    teamName   = driverTeamMap.TryGetValue(g.Key, out var t) ? t : "Unknown",
                    points     = g.Sum(r => r.pointsEarned),
                    wins       = g.Count(r => r.finishPosition == 1),
                    podiums    = g.Count(r => r.finishPosition <= 3)
                })
                .OrderByDescending(d => d.points)
                .ThenByDescending(d => d.wins)
                .ToList();

            for (int i = 0; i < grouped.Count; i++) grouped[i].position = i + 1;
            return grouped;
        }

        public static List<DriverStanding> BuildConstructorStandings(List<RaceResult> allResults)
        {
            var grouped = allResults
                .GroupBy(r => r.teamName)
                .Select(g => new DriverStanding
                {
                    driverName = g.Key, teamName = g.Key,
                    points     = g.Sum(r => r.pointsEarned),
                    wins       = g.Count(r => r.finishPosition == 1)
                })
                .OrderByDescending(d => d.points).ToList();

            for (int i = 0; i < grouped.Count; i++) grouped[i].position = i + 1;
            return grouped;
        }
    }

    public class RaceResultScreen : MonoBehaviour
    {
        [Header("References")]
        public Transform       resultRowParent;
        public GameObject      resultRowPrefab;
        public TextMeshProUGUI headerText;
        public GameObject      fastestLapBanner;
        public TextMeshProUGUI fastestLapText;

        public void ShowResults(List<RaceResult> results, string localDriverId)
        {
            if (headerText) headerText.text = "RACE RESULT";
            foreach (Transform child in resultRowParent) Destroy(child.gameObject);

            foreach (var r in results.OrderBy(x => x.retired ? 999 : x.finishPosition))
            {
                var row   = Instantiate(resultRowPrefab, resultRowParent);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 5)
                {
                    texts[0].text = r.retired ? "RET" : r.finishPosition.ToString();
                    texts[1].text = r.driverName;
                    texts[2].text = r.teamName;
                    texts[3].text = FormatTime(r.totalTime);
                    texts[4].text = r.pointsEarned > 0 ? $"+{r.pointsEarned}" : "0";
                    if (r.playerId == localDriverId)
                        row.GetComponent<Image>().color = new Color(1f, 1f, 0f, 0.15f);
                }
                if (r.hasFastestLap)
                {
                    if (fastestLapBanner) fastestLapBanner.SetActive(true);
                    if (fastestLapText)   fastestLapText.text = $"FL: {r.driverName}  {FormatTime(r.fastestLap)}";
                }
            }
            gameObject.SetActive(true);
        }

        static string FormatTime(float t)
        {
            if (t <= 0f) return "---";
            int min = (int)(t / 60f); float sec = t % 60f;
            return $"{min}:{sec:00.000}";
        }
    }
}