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
    //  HUD DATA MODEL
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

        public float[] tireWear   = new float[4];
        public float[] tireTemps  = new float[4];
        public string[] tireCompounds = new string[4];

        public float fuelRemaining;
        public float fuelPercent;
        public int   lapsOfFuelLeft;

        public int   racePosition;
        public int   totalCars;
        public float gapToLeader;
        public float gapToAhead;
        public float gapToBehind;
        public string driverAheadName;
        public string driverBehindName;

        public int   currentLap;
        public int   totalLaps;
        public float lastLapTime;
        public float bestLapTime;
        public float currentLapTime;
        public float deltaToPersonalBest;   // +/- seconds vs best
        public float deltaToLeader;

        public float[] sectorTimes       = new float[3];
        public float[] sectorBestTimes   = new float[3];
        public int     currentSector;

        public bool  drsActive;
        public bool  drsEligible;
        public bool  drsAvailableZone;

        public FlagStatus  currentFlag;
        public WeatherCondition weather;
        public float        trackWetness;
        public EngineMode   engineMode;
        public bool         damageWarning;
        public DamageLevel  damageLevel;
        public bool         tyrePressureWarning;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TELEMETRY
    // ═══════════════════════════════════════════════════════════════════════
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
                time     = Time.time,
                speed    = s.speed,
                throttle = s.throttle,
                brake    = s.brake,
                steering = s.steering,
                gLong    = s.gLongitudinal,
                gLat     = s.gLateral,
                rpm      = s.rpm,
                fuel     = s.fuelLoad
            };
            for (int i = 0; i < 4; i++)
            {
                rec.tireTemps[i] = s.tires[i].temperature;
                rec.tireWear[i]  = s.tires[i].wearPercent;
            }
            _samples.Add(rec);

            // Rolling window
            float cutoff = Time.time - MaxRecordSeconds;
            while (_samples.Count > 0 && _samples[0].time < cutoff)
                _samples.RemoveAt(0);
        }

        public void ClearSession() => _samples.Clear();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HUD BUILDER  (reads state → HudData)
    // ═══════════════════════════════════════════════════════════════════════
    public class HudBuilder : MonoBehaviour
    {
        public PhysicsIntegrator  physics;
        public RaceDirector       raceDirector;
        public float              maxFuel = 110f;
        public string             localDriverId;

        public HudData Current { get; private set; } = new();

        void Update()
        {
            if (physics == null) return;
            var s = physics.State;
            var h = Current;

            h.speed             = s.speed;
            h.gear              = s.gear;
            h.rpm               = s.rpm;
            h.throttlePercent   = s.throttle    * 100f;
            h.brakePercent      = s.brake       * 100f;
            h.ersDeploy         = s.ersDeploy   * 100f;
            h.ersEnergy         = s.ersEnergy   * 100f;
            h.fuelRemaining     = s.fuelLoad;
            h.fuelPercent       = s.fuelLoad / maxFuel * 100f;
            h.drsActive         = s.drsActive;
            h.drsEligible       = s.drsEligible;
            h.engineMode        = s.engineMode;
            h.damageLevel       = s.damage.OverallLevel;
            h.damageWarning     = s.damage.OverallLevel >= DamageLevel.Moderate;

            for (int i = 0; i < 4; i++)
            {
                h.tireWear[i]      = s.tires[i].wearPercent;
                h.tireTemps[i]     = s.tires[i].temperature;
                h.tireCompounds[i] = s.tires[i].compound.ToString()[0].ToString();
            }

            if (raceDirector != null)
            {
                var entry = raceDirector.Entries
                    .FirstOrDefault(e => e.driverId == localDriverId);
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
                h.currentFlag    = raceDirector.DirectorState.flag;
                h.weather        = raceDirector.weather.condition;
                h.trackWetness   = raceDirector.weather.trackWetness;
                h.totalCars      = raceDirector.Entries.Count;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACING HUD  (MonoBehaviour that binds HudData → Unity UI)
    // ═══════════════════════════════════════════════════════════════════════
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
        public Image[]           tireWearImages   = new Image[4];
        public TextMeshProUGUI[] tireTempTexts    = new TextMeshProUGUI[4];
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

        // Colors
        static readonly Color COL_GREEN  = new(0.0f, 0.9f, 0.2f);
        static readonly Color COL_YELLOW = new(1.0f, 0.85f, 0.0f);
        static readonly Color COL_RED    = new(0.9f, 0.1f, 0.1f);
        static readonly Color COL_PURPLE = new(0.6f, 0.0f, 0.9f);
        static readonly Color COL_WHITE  = Color.white;

        HudBuilder _builder;

        void Awake() => _builder = GetComponentInParent<HudBuilder>();

        void Update()
        {
            if (_builder == null) return;
            var h = _builder.Current;
            if (h == null) return;

            ApplySpeed(h);
            ApplyPedals(h);
            ApplyTires(h);
            ApplyFuel(h);
            ApplyTiming(h);
            ApplyDRS(h);
            ApplyFlag(h);
            ApplyERS(h);
            ApplyEngineMode(h);
            ApplyDamage(h);
        }

        void ApplySpeed(HudData h)
        {
            if (speedText) speedText.text = Mathf.RoundToInt(h.speed).ToString();
            if (gearText)
            {
                gearText.text = h.gear == 0 ? "N" : h.gear.ToString();
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
                if (tireWearImages[i])
                    tireWearImages[i].fillAmount = 1f - h.tireWear[i] / 100f;

                if (tireTempTexts[i])
                    tireTempTexts[i].text = Mathf.RoundToInt(h.tireTemps[i]) + "°";

                if (tireCompoundTexts[i])
                    tireCompoundTexts[i].text = h.tireCompounds[i];

                // Color by temperature
                if (tireWearImages[i])
                {
                    float t = h.tireTemps[i];
                    if (t < 50f)       tireWearImages[i].color = Color.blue;
                    else if (t < 70f)  tireWearImages[i].color = COL_WHITE;
                    else if (t < 100f) tireWearImages[i].color = COL_GREEN;
                    else               tireWearImages[i].color = COL_RED;
                }
            }
        }

        void ApplyFuel(HudData h)
        {
            if (fuelBar)  fuelBar.fillAmount = h.fuelPercent / 100f;
            if (fuelText) fuelText.text = $"{h.fuelRemaining:F1}L";
            if (fuelBar)  fuelBar.color = h.fuelPercent < 15f ? COL_RED
                        : h.fuelPercent < 30f ? COL_YELLOW : COL_GREEN;
        }

        void ApplyTiming(HudData h)
        {
            if (positionText)  positionText.text   = $"P{h.racePosition}";
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
            {
                gapAheadText.text = h.gapToAhead >= 999f ? "---"
                    : $"+{h.gapToAhead:F3} {h.driverAheadName}";
            }
            if (gapBehindText)
            {
                gapBehindText.text = h.gapToBehind >= 999f ? "---"
                    : $"-{h.gapToBehind:F3} {h.driverBehindName}";
            }
        }

        void ApplyDRS(HudData h)
        {
            if (drsActivePanel)   drsActivePanel.SetActive(h.drsActive);
            if (drsEligiblePanel) drsEligiblePanel.SetActive(h.drsEligible && !h.drsActive);
        }

        void ApplyFlag(HudData h)
        {
            if (flagBar == null) return;

            Color flagColor = h.currentFlag switch
            {
                FlagStatus.Green      => COL_GREEN,
                FlagStatus.Yellow     => COL_YELLOW,
                FlagStatus.SafetyCar  => COL_YELLOW,
                FlagStatus.Red        => COL_RED,
                FlagStatus.Chequered  => COL_WHITE,
                _ => COL_GREEN
            };
            flagBar.color = flagColor;

            if (flagText) flagText.text = h.currentFlag switch
            {
                FlagStatus.SafetyCar          => "SAFETY CAR",
                FlagStatus.VirtualSafetyCar   => "VSC",
                FlagStatus.Red                => "RED FLAG",
                FlagStatus.Chequered          => "CHEQUERED",
                _ => ""
            };
        }

        void ApplyERS(HudData h)
        {
            if (ersDeployBar)  ersDeployBar.fillAmount  = h.ersDeploy  / 100f;
            if (ersEnergyBar)  ersEnergyBar.fillAmount  = h.ersEnergy  / 100f;
            if (ersEnergyBar)  ersEnergyBar.color = h.ersEnergy < 20f ? COL_RED : COL_GREEN;
        }

        void ApplyEngineMode(HudData h)
        {
            if (!engineModeText) return;
            engineModeText.text = h.engineMode.ToString().ToUpper();
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

    // ═══════════════════════════════════════════════════════════════════════
    //  STANDINGS SCREEN
    // ═══════════════════════════════════════════════════════════════════════
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
        public int    pointsDelta;   // vs last round
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
                    driverCode = g.Key.Length >= 3 ? g.Key.Substring(0,3).ToUpper() : g.Key.ToUpper(),
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

        public static List<DriverStanding> BuildConstructorStandings(
            List<RaceResult> allResults)
        {
            var grouped = allResults
                .GroupBy(r => r.teamName)
                .Select(g => new DriverStanding
                {
                    driverName = g.Key,
                    teamName   = g.Key,
                    points     = g.Sum(r => r.pointsEarned),
                    wins       = g.Count(r => r.finishPosition == 1)
                })
                .OrderByDescending(d => d.points)
                .ToList();

            for (int i = 0; i < grouped.Count; i++) grouped[i].position = i + 1;
            return grouped;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE RESULT SCREEN
    // ═══════════════════════════════════════════════════════════════════════
    public class RaceResultScreen : MonoBehaviour
    {
        [Header("References")]
        public Transform          resultRowParent;
        public GameObject         resultRowPrefab;
        public TextMeshProUGUI    headerText;
        public GameObject         fastestLapBanner;
        public TextMeshProUGUI    fastestLapText;

        public void ShowResults(List<RaceResult> results, string localDriverId)
        {
            if (headerText) headerText.text = "RACE RESULT";

            foreach (Transform child in resultRowParent)
                Destroy(child.gameObject);

            foreach (var r in results.OrderBy(x => x.retired ? 999 : x.finishPosition))
            {
                var row = Instantiate(resultRowPrefab, resultRowParent);
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
                    if (fastestLapText)
                        fastestLapText.text = $"FL: {r.driverName}  {FormatTime(r.fastestLap)}";
                }
            }

            gameObject.SetActive(true);
        }

        static string FormatTime(float t)
        {
            if (t <= 0f) return "---";
            int min   = (int)(t / 60f);
            float sec = t % 60f;
            return $"{min}:{sec:00.000}";
        }
    }
}
