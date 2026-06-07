using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Audio;

namespace RacingGame.Race
{
    // ═══════════════════════════════════════════════════════════════════════
    //  RADIO MESSAGE
    // ═══════════════════════════════════════════════════════════════════════
    public enum RadioPriority { Low, Medium, High, Urgent }

    [System.Serializable]
    public class RadioMessage
    {
        public string         text;
        public RadioPriority  priority;
        public float          timestamp;
        public bool           spoken;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE RADIO MANAGER
    // ═══════════════════════════════════════════════════════════════════════
    public class RaceRadioManager : MonoBehaviour
    {
        [Header("References")]
        public RaceDirector director;

        [Header("Audio Clips (optional)")]
        public AudioClip boxBoxClip;
        public AudioClip tireWarnClip;
        public AudioClip intervalClip;
        public AudioClip scWindowClip;
        public AudioClip pushNowClip;
        public AudioClip saveFuelClip;
        public AudioClip ersDeployClip;

        // Message queue
        readonly Queue<RadioMessage>  _queue    = new();
        readonly List<RadioMessage>   _history  = new();
        public   IReadOnlyList<RadioMessage> History => _history;

        float _intervalTimer = 0f;
        float _tireCheckTimer = 0f;
        bool  _scWindowBroadcast = false;

        // Player vehicle state reference (set by RaceSceneController)
        public RacingGame.Physics.PhysicsIntegrator playerPhysics;
        float _lastSoC = 80f;

        void Update()
        {
            if (director == null || playerPhysics == null) return;

            _intervalTimer  += Time.deltaTime;
            _tireCheckTimer += Time.deltaTime;

            CheckFlagMessages();
            CheckTireMessages();
            CheckERSMessages();
            CheckIntervalMessages();
        }

        // ── Flag events ───────────────────────────────────────────────────
        void CheckFlagMessages()
        {
            var flag = director.DirectorState.flag;

            if (flag == FlagStatus.SafetyCar && !_scWindowBroadcast)
            {
                _scWindowBroadcast = true;
                Enqueue("Safety Car deployed. Pit window is OPEN. Box this lap for free tyres.",
                        RadioPriority.Urgent, scWindowClip);
            }

            if (flag == FlagStatus.Green && _scWindowBroadcast)
            {
                _scWindowBroadcast = false;
                Enqueue("Safety Car in. Track is green. Push now!", RadioPriority.High, pushNowClip);
            }

            if (flag == FlagStatus.VirtualSafetyCar)
                Enqueue("Virtual Safety Car. Maintain delta. Pit window open.",
                        RadioPriority.High, scWindowClip);
        }

        // ── Tire messages ─────────────────────────────────────────────────
        void CheckTireMessages()
        {
            if (_tireCheckTimer < 8f) return;
            _tireCheckTimer = 0f;

            var state = playerPhysics.State;
            float avgWear = 0f;
            float avgSurface = 0f;
            foreach (var t in state.tires)
            {
                avgWear    += t.wearPercent;
                avgSurface += t.surfaceTemp;
            }
            avgWear    /= 4f;
            avgSurface /= 4f;

            if (avgWear > 70f)
                Enqueue($"Tyre wear critical: {avgWear:F0}%. Box next lap.", RadioPriority.Urgent, tireWarnClip);
            else if (avgWear > 55f)
                Enqueue($"Tyres at {avgWear:F0}%. Plan your pit window.", RadioPriority.Medium, tireWarnClip);

            // Graining warning
            foreach (var t in state.tires)
                if (t.grainingLevel > 0.5f)
                {
                    Enqueue("Graining detected. Tyre management mode.", RadioPriority.Medium, tireWarnClip);
                    break;
                }

            // Blistering warning
            foreach (var t in state.tires)
                if (t.blisteringLevel > 0.4f)
                {
                    Enqueue("BLISTERING warning! Reduce tyre stress immediately.", RadioPriority.Urgent, tireWarnClip);
                    break;
                }
        }

        // ── ERS messages ──────────────────────────────────────────────────
        void CheckERSMessages()
        {
            var state = playerPhysics.State;
            float soc = state.ersSoC;

            if (_lastSoC > 20f && soc <= 20f)
                Enqueue("ERS battery low. Switch to Harvest mode.", RadioPriority.High, ersDeployClip);

            if (_lastSoC <= 15f && soc > 15f)
                Enqueue("ERS recovering. Balanced mode available.", RadioPriority.Low, ersDeployClip);

            if (state.inDirtyAir && state.gapToCarAheadSec < 0.5f && soc > 40f)
                Enqueue("DRS and ERS Overtake available. Deploy on the straight.",
                        RadioPriority.Medium, ersDeployClip);

            _lastSoC = soc;
        }

        // ── Gap / interval messages ───────────────────────────────────────
        void CheckIntervalMessages()
        {
            if (_intervalTimer < 20f) return;
            _intervalTimer = 0f;

            var entries  = director?.Entries;
            if (entries == null || entries.Count == 0) return;

            var player = entries.Find(e => e.isPlayer);
            if (player == null) return;

            string msg = $"P{player.position}/{director.Entries.Count}. ";
            if (player.position > 1)
                msg += $"Gap to ahead: {player.gapAhead:F1}s.";
            else
                msg += "You are leading the race.";

            Enqueue(msg, RadioPriority.Low, intervalClip);

            // Fuel warning
            float fuel = playerPhysics.State.fuelLoad;
            if (fuel < 8f)
                Enqueue($"Fuel critical: {fuel:F1}L. Lift and coast.", RadioPriority.Urgent, saveFuelClip);
            else if (fuel < 20f)
                Enqueue("Save fuel – switch to Eco engine mode.", RadioPriority.Medium, saveFuelClip);
        }

        // ── Queue / delivery ──────────────────────────────────────────────
        void Enqueue(string text, RadioPriority priority, AudioClip clip = null)
        {
            // Don't repeat the same message within 30 s
            float now = Time.time;
            foreach (var m in _history)
                if (m.text == text && now - m.timestamp < 30f) return;

            var msg = new RadioMessage
            { text = text, priority = priority, timestamp = now };

            _queue.Enqueue(msg);
            _history.Add(msg);

            if (_history.Count > 100) _history.RemoveAt(0);

            // Play audio through AudioManager if clip supplied
            if (clip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayRadio(clip);

            Debug.Log($"[Radio] {priority}: {text}");
        }

        /// <summary>UI polls this to display the latest unread message.</summary>
        public RadioMessage DequeueMessage() =>
            _queue.Count > 0 ? _queue.Dequeue() : null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TYRE STRATEGY PREDICTOR  (shows optimal stop windows on HUD)
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class StrategyWindow
    {
        public int          idealPitLap;
        public int          latestPitLap;
        public TireCompound recommendedCompound;
        public float        projectedTimeLoss;    // seconds vs staying out
        public float        projectedTimeGain;    // from fresh rubber pace delta
        public bool         isSafetyCarWindow;
    }

    public class TyreStrategyPredictor : MonoBehaviour
    {
        [Header("References")]
        public RaceDirector director;
        public RacingGame.Physics.PhysicsIntegrator playerPhysics;

        public StrategyWindow Primary   { get; private set; } = new();
        public StrategyWindow Alternate { get; private set; } = new();

        float _updateTimer = 0f;
        const float UPDATE_INTERVAL = 5f;

        void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer < UPDATE_INTERVAL) return;
            _updateTimer = 0f;
            RecalculateWindows();
        }

        void RecalculateWindows()
        {
            if (director == null || playerPhysics == null) return;

            var state       = playerPhysics.State;
            var player      = director.Entries?.Find(e => e.isPlayer);
            if (player == null) return;

            int  currentLap  = player.currentLap;
            int  totalLaps   = director.totalLaps;
            int  lapsLeft    = Mathf.Max(1, totalLaps - currentLap);

            float avgWear    = 0f;
            float wearPerLap = 0f;
            foreach (var t in state.tires) avgWear += t.wearPercent;
            avgWear /= 4f;

            // Estimate wear rate from last known wear (simplified)
            wearPerLap = Mathf.Max(0.5f, avgWear / Mathf.Max(1, player.currentLap));

            float lapsUntilDead = Mathf.Max(1f, (100f - avgWear) / Mathf.Max(0.1f, wearPerLap));

            // Primary: optimal stop based on tire life
            int idealLap  = currentLap + Mathf.FloorToInt(lapsUntilDead * 0.80f);
            int latestLap = currentLap + Mathf.FloorToInt(lapsUntilDead * 0.95f);
            idealLap  = Mathf.Clamp(idealLap,  currentLap + 1, totalLaps - 3);
            latestLap = Mathf.Clamp(latestLap, idealLap,       totalLaps - 1);

            Primary = new StrategyWindow
            {
                idealPitLap          = idealLap,
                latestPitLap         = latestLap,
                recommendedCompound  = PickCompound(totalLaps - idealLap, director.weather),
                projectedTimeLoss    = director.circuit?.pitLaneTimeLoss ?? 22f,
                projectedTimeGain    = (avgWear / 100f) * 15f,  // rough pace delta
                isSafetyCarWindow    = director.DirectorState.flag == FlagStatus.SafetyCar
            };

            // Alternate: one-stop aggressive (soft to the end)
            Alternate = new StrategyWindow
            {
                idealPitLap          = currentLap + 2,
                latestPitLap         = currentLap + 5,
                recommendedCompound  = TireCompound.Soft,
                projectedTimeLoss    = Primary.projectedTimeLoss,
                projectedTimeGain    = Primary.projectedTimeGain * 1.2f,
                isSafetyCarWindow    = Primary.isSafetyCarWindow
            };
        }

        static TireCompound PickCompound(int lapsRemaining, WeatherData w)
        {
            if (w.RequiresWetTires)   return TireCompound.Wet;
            if (w.RequiresInterTires) return TireCompound.Inter;
            if (lapsRemaining <= 12)  return TireCompound.Soft;
            if (lapsRemaining <= 28)  return TireCompound.Medium;
            return TireCompound.Hard;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CHAMPIONSHIP STANDINGS TRACKER  (live season standings during race)
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class LiveStandingEntry
    {
        public string driverName;
        public string teamName;
        public int    championshipPoints;
        public int    projectedPointsGain;   // if they finish current position
        public int    projectedPosition;     // in championship after this race
    }

    public class ChampionshipStandingsTracker : MonoBehaviour
    {
        public RaceDirector director;

        readonly List<LiveStandingEntry> _standings = new();
        public IReadOnlyList<LiveStandingEntry> Standings => _standings;

        static readonly int[] POINTS_TABLE = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

        // Pre-race standings passed in from Career system
        public void LoadPreRaceStandings(List<LiveStandingEntry> preRace)
        {
            _standings.Clear();
            _standings.AddRange(preRace);
        }

        void Update()
        {
            if (director == null) return;
            RefreshProjections();
        }

        void RefreshProjections()
        {
            foreach (var entry in _standings)
            {
                var raceEntry = director.Entries?.Find(e => e.driverName == entry.driverName);
                if (raceEntry == null) continue;

                int pos   = raceEntry.position;
                int pts   = pos >= 1 && pos <= POINTS_TABLE.Length ? POINTS_TABLE[pos - 1] : 0;
                entry.projectedPointsGain = pts;
            }

            // Sort by projected total points
            _standings.Sort((a, b) =>
                (b.championshipPoints + b.projectedPointsGain)
                    .CompareTo(a.championshipPoints + a.projectedPointsGain));

            for (int i = 0; i < _standings.Count; i++)
                _standings[i].projectedPosition = i + 1;
        }
    }
}
