using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Telemetry;
using UnityEngine.UI;
using TMPro;

namespace RacingGame.Telemetry
{
    // ═══════════════════════════════════════════════════════════════════════
    //  RECORD MANAGER  – save and display track records, milestones
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class TrackRecord
    {
        public string trackName;
        public float lapTime;
        public string driverName;
        public DateTime recordDate;
        public int raceNumber;  // Which race/session
        public float maxSpeed;
        public List<float> sectorTimes = new();
    }

    [System.Serializable]
    public class CareerMilestone
    {
        public string title;           // "Most wins in a season", "Fastest lap record"
        public string description;
        public int value;             // 5 wins, 289 km/h, etc.
        public DateTime achievedDate;
        public string circuitName;
    }

    public class RecordManager : MonoBehaviour
    {
        public static RecordManager Instance { get; private set; }

        public List<TrackRecord> trackRecords = new();
        public List<CareerMilestone> careerMilestones = new();
        public List<LapRecording> personalBests = new();

        // Events
        public event Action<TrackRecord> OnNewTrackRecord;
        public event Action<CareerMilestone> OnMilestoneAchieved;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── TRACK RECORDS ─────────────────────────────────────────────────
        public void SaveLapTime(LapRecording lap)
        {
            var newRecord = new TrackRecord
            {
                trackName = lap.trackName,
                lapTime = lap.lapTime,
                driverName = "Player",
                recordDate = DateTime.Now,
                maxSpeed = lap.maxSpeed,
                sectorTimes = new List<float>(lap.sectorTimes)
            };

            // Check if this is a new personal best for this track
            var existingRecord = trackRecords.Find(r => r.trackName == lap.trackName);
            
            if (existingRecord == null || lap.lapTime < existingRecord.lapTime)
            {
                if (existingRecord != null)
                    trackRecords.Remove(existingRecord);

                trackRecords.Add(newRecord);
                OnNewTrackRecord?.Invoke(newRecord);

                Debug.Log($"[Records] New track record at {lap.trackName}: {lap.lapTime:F2}s");
            }

            // Also save to personal bests
            personalBests.Add(lap);

            // Check for milestones
            CheckMilestones();
        }

        public TrackRecord GetTrackRecord(string trackName)
        {
            return trackRecords.Find(r => r.trackName == trackName);
        }

        public List<TrackRecord> GetAllRecords()
        {
            trackRecords.Sort((a, b) => a.lapTime.CompareTo(b.lapTime));
            return trackRecords;
        }

        // ── CAREER MILESTONES ─────────────────────────────────────────────
        void CheckMilestones()
        {
            // Check for fastest lap record
            if (trackRecords.Count > 0)
            {
                float fastestLap = trackRecords[0].lapTime;

                var fastestMilestone = careerMilestones.Find(m => m.title == "Fastest Lap");
                if (fastestMilestone == null || fastestLap < fastestMilestone.value)
                {
                    var milestone = new CareerMilestone
                    {
                        title = "Fastest Lap",
                        description = $"Fastest lap achieved: {fastestLap:F2}s",
                        value = (int)(fastestLap * 1000),  // Store as int (milliseconds)
                        achievedDate = DateTime.Now,
                        circuitName = trackRecords[0].trackName
                    };

                    careerMilestones.Add(milestone);
                    OnMilestoneAchieved?.Invoke(milestone);
                }
            }

            // Check for highest speed
            float maxSpeed = 0;
            foreach (var lap in personalBests)
            {
                if (lap.maxSpeed > maxSpeed)
                    maxSpeed = lap.maxSpeed;
            }

            if (maxSpeed > 300)
            {
                var milestone = new CareerMilestone
                {
                    title = "Speed Demon",
                    description = $"Reached {maxSpeed:F0} km/h",
                    value = (int)maxSpeed,
                    achievedDate = DateTime.Now
                };

                if (!careerMilestones.Exists(m => m.title == "Speed Demon"))
                {
                    careerMilestones.Add(milestone);
                    OnMilestoneAchieved?.Invoke(milestone);
                }
            }
        }

        public string GetMilestoneDisplay()
        {
            if (careerMilestones.Count == 0)
                return "No achievements yet";

            return $"{careerMilestones.Count} milestones achieved";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DELTA TIMER UI  – real-time lap comparison display
    // ═══════════════════════════════════════════════════════════════════════
    public class DeltaTimerUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI deltaText;
        [SerializeField] Image deltaBackground;
        [SerializeField] Color aheadColor = Color.green;
        [SerializeField] Color behindColor = Color.red;

        private LapRecording _compareLap;
        private float _currentLapDistance = 0;

        public void SetCompareLap(LapRecording lap)
        {
            _compareLap = lap;
            Debug.Log($"[DeltaTimer] Comparing against: {lap.lapTime:F2}s");
        }

        void Update()
        {
            if (_compareLap == null || deltaText == null)
                return;

            // Simulate lap distance (0-1)
            _currentLapDistance = (Time.time % 90f) / 90f;  // Assume 90 second lap

            float delta = _compareLap.GetDeltaAtDistance(_currentLapDistance, _compareLap);

            // Display delta
            if (Mathf.Abs(delta) < 0.05f)
                deltaText.text = "0.000";
            else if (delta < 0)
                deltaText.text = $"{delta:F3}";  // Green (ahead)
            else
                deltaText.text = $"+{delta:F3}";  // Red (behind)

            // Color code
            deltaBackground.color = delta < 0 ? aheadColor : behindColor;
            deltaBackground.color = new Color(deltaBackground.color.r, deltaBackground.color.g, 
                                              deltaBackground.color.b, 0.7f);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TELEMETRY UI  – post-race lap data visualization
    // ═══════════════════════════════════════════════════════════════════════
    public class TelemetryUI : MonoBehaviour
    {
        [SerializeField] Image speedGraphImage;
        [SerializeField] Image throttleGraphImage;
        [SerializeField] Image brakeGraphImage;
        [SerializeField] TextMeshProUGUI lapTimeText;
        [SerializeField] TextMeshProUGUI maxSpeedText;
        [SerializeField] TextMeshProUGUI avgSpeedText;

        public void DisplayLapTelemetry(LapRecording lap)
        {
            if (lap == null || lap.frames.Count == 0)
                return;

            // Update text display
            lapTimeText.text = $"Lap Time: {lap.lapTime:F2}s";
            maxSpeedText.text = $"Max Speed: {lap.maxSpeed:F0} km/h";
            avgSpeedText.text = $"Avg Speed: {lap.avgSpeed:F0} km/h";

            // Draw graphs
            DrawSpeedGraph(lap);
            DrawThrottleGraph(lap);
            DrawBrakeGraph(lap);

            Debug.Log($"[TelemetryUI] Displayed lap data: {lap.lapTime:F2}s");
        }

        void DrawSpeedGraph(LapRecording lap)
        {
            if (speedGraphImage == null) return;

            // Create texture for speed graph
            Texture2D graphTexture = new Texture2D(512, 256, TextureFormat.RGBA32, false);

            // Normalized speed values
            for (int x = 0; x < 512; x++)
            {
                int frameIdx = Mathf.RoundToInt((x / 512f) * (lap.frames.Count - 1));
                if (frameIdx >= lap.frames.Count) frameIdx = lap.frames.Count - 1;

                float speed = lap.frames[frameIdx].speed;
                float normalizedSpeed = Mathf.Clamp(speed / lap.maxSpeed, 0, 1);
                int barHeight = Mathf.RoundToInt(normalizedSpeed * 256);

                // Draw column
                Color graphColor = Color.Lerp(Color.red, Color.green, normalizedSpeed);
                for (int y = 0; y < barHeight; y++)
                {
                    graphTexture.SetPixel(x, y, graphColor);
                }
            }

            graphTexture.Apply();
            speedGraphImage.sprite = Sprite.Create(graphTexture, new Rect(0, 0, 512, 256), Vector2.zero);
        }

        void DrawThrottleGraph(LapRecording lap)
        {
            if (throttleGraphImage == null) return;

            Texture2D graphTexture = new Texture2D(512, 128, TextureFormat.RGBA32, false);

            for (int x = 0; x < 512; x++)
            {
                int frameIdx = Mathf.RoundToInt((x / 512f) * (lap.frames.Count - 1));
                if (frameIdx >= lap.frames.Count) frameIdx = lap.frames.Count - 1;

                float throttle = lap.frames[frameIdx].throttle;
                int barHeight = Mathf.RoundToInt(throttle * 128);

                for (int y = 0; y < barHeight; y++)
                {
                    graphTexture.SetPixel(x, y, Color.yellow);
                }
            }

            graphTexture.Apply();
            throttleGraphImage.sprite = Sprite.Create(graphTexture, new Rect(0, 0, 512, 128), Vector2.zero);
        }

        void DrawBrakeGraph(LapRecording lap)
        {
            if (brakeGraphImage == null) return;

            Texture2D graphTexture = new Texture2D(512, 128, TextureFormat.RGBA32, false);

            for (int x = 0; x < 512; x++)
            {
                int frameIdx = Mathf.RoundToInt((x / 512f) * (lap.frames.Count - 1));
                if (frameIdx >= lap.frames.Count) frameIdx = lap.frames.Count - 1;

                float brake = lap.frames[frameIdx].brake;
                int barHeight = Mathf.RoundToInt(brake * 128);

                for (int y = 0; y < barHeight; y++)
                {
                    graphTexture.SetPixel(x, y, Color.red);
                }
            }

            graphTexture.Apply();
            brakeGraphImage.sprite = Sprite.Create(graphTexture, new Rect(0, 0, 512, 128), Vector2.zero);
        }
    }
}
