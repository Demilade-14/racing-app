using System;
using System.Collections.Generic;
using UnityEngine;

namespace RacingGame.Telemetry
{
    // ═══════════════════════════════════════════════════════════════════════
    //  TELEMETRY FRAME – single moment of lap data
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class TelemetryFrame
    {
        public float lapDistance;        // 0-1 = lap progress
        public float speed;
        public float throttle;           // 0-1
        public float brake;              // 0-1
        public float steering;           // -1 to 1
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
        public int gear;
        public float engineRPM;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LAP RECORDING – complete lap telemetry data
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class LapRecording
    {
        public string trackName;
        public float lapTime;
        public List<TelemetryFrame> frames = new();
        public float maxSpeed;
        public float avgSpeed;
        public DateTime recordedAt;
        public int sectorCount;
        public List<float> sectorTimes = new();

        public float GetDeltaAtDistance(float lapDistance, LapRecording compareTo)
        {
            if (compareTo == null || compareTo.frames.Count == 0)
                return 0f;

            // Find frames near target lap distance
            int myFrameIndex = Mathf.RoundToInt(lapDistance * (frames.Count - 1));
            int compareFrameIndex = Mathf.RoundToInt(lapDistance * (compareTo.frames.Count - 1));

            if (myFrameIndex >= frames.Count || compareFrameIndex >= compareTo.frames.Count)
                return 0f;

            float myTime = frames[myFrameIndex].timestamp;
            float compareTime = compareTo.frames[compareFrameIndex].timestamp;

            return myTime - compareTime;  // Negative = ahead
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GHOST SYSTEM  – render player's best lap as translucent ghost car
    // ═══════════════════════════════════════════════════════════════════════
    public class GhostSystem : MonoBehaviour
    {
        public static GhostSystem Instance { get; private set; }

        public LapRecording ghostLap;           // Best lap to display
        public GameObject ghostCarPrefab;
        public GameObject ghostCar;
        public int currentGhostFrameIndex = 0;
        public float ghostPlaySpeed = 1f;      // Can adjust to match player pace

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetGhostLap(LapRecording lap)
        {
            ghostLap = lap;

            // Create ghost car if not exists
            if (ghostCar == null && ghostCarPrefab != null)
            {
                ghostCar = Instantiate(ghostCarPrefab);
                ghostCar.name = "GhostCar";

                // Make translucent
                var renderer = ghostCar.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material ghostMat = new Material(renderer.material);
                    Color transparentColor = ghostMat.color;
                    transparentColor.a = 0.4f;
                    ghostMat.color = transparentColor;
                    renderer.material = ghostMat;
                }
            }

            Debug.Log($"[Ghost] Loaded ghost lap: {lap.lapTime:F2}s from {lap.trackName}");
        }

        void FixedUpdate()
        {
            if (ghostLap == null || ghostCar == null || ghostLap.frames.Count == 0)
                return;

            // Advance ghost frame based on actual lap progress
            currentGhostFrameIndex = Mathf.Min(
                currentGhostFrameIndex + (int)(ghostPlaySpeed * Time.deltaTime * 60f),
                ghostLap.frames.Count - 1
            );

            // Update ghost car position and rotation
            var frame = ghostLap.frames[currentGhostFrameIndex];
            ghostCar.transform.position = frame.position;
            ghostCar.transform.rotation = frame.rotation;
        }

        public void ResetGhost()
        {
            currentGhostFrameIndex = 0;
            if (ghostCar != null)
                ghostCar.transform.position = Vector3.zero;
        }

        public void ClearGhost()
        {
            if (ghostCar != null)
                Destroy(ghostCar);
            ghostLap = null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TELEMETRY RECORDER  – capture lap data in real-time
    // ═══════════════════════════════════════════════════════════════════════
    public class TelemetryRecorder : MonoBehaviour
    {
        public static TelemetryRecorder Instance { get; private set; }

        public LapRecording currentRecording;
        public float recordingStartTime;
        public bool isRecording = false;

        // References
        private PhysicsEngine _physics;
        private Transform _playerCar;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _physics = FindObjectOfType<PhysicsEngine>();
            _playerCar = FindObjectOfType<PlayerCarController>()?.transform;
        }

        public void StartRecording(string trackName)
        {
            currentRecording = new LapRecording
            {
                trackName = trackName,
                recordedAt = DateTime.Now,
                maxSpeed = 0,
                avgSpeed = 0
            };

            isRecording = true;
            recordingStartTime = Time.time;

            Debug.Log($"[Telemetry] Recording started: {trackName}");
        }

        void FixedUpdate()
        {
            if (!isRecording || _physics == null || _playerCar == null)
                return;

            // Capture frame data
            var frame = new TelemetryFrame
            {
                speed = _physics.GetCurrentSpeed(),
                throttle = Input.GetAxis("Throttle"),
                brake = Input.GetAxis("Brake"),
                steering = Input.GetAxis("Steering"),
                position = _playerCar.position,
                rotation = _playerCar.rotation,
                timestamp = Time.time - recordingStartTime,
                gear = _physics.GetCurrentGear(),
                engineRPM = _physics.GetCurrentRPM()
            };

            currentRecording.frames.Add(frame);

            // Update max/avg speed
            if (frame.speed > currentRecording.maxSpeed)
                currentRecording.maxSpeed = frame.speed;
        }

        public LapRecording EndRecording(float lapTime, int sectors = 3)
        {
            if (!isRecording) return null;

            isRecording = false;
            currentRecording.lapTime = lapTime;

            // Calculate sector times
            int framesPerSector = currentRecording.frames.Count / sectors;
            for (int i = 0; i < sectors; i++)
            {
                int startIdx = i * framesPerSector;
                int endIdx = (i + 1) * framesPerSector;
                if (endIdx > currentRecording.frames.Count)
                    endIdx = currentRecording.frames.Count - 1;

                float sectorTime = currentRecording.frames[endIdx].timestamp - 
                                   (startIdx > 0 ? currentRecording.frames[startIdx].timestamp : 0);
                currentRecording.sectorTimes.Add(sectorTime);
            }

            // Calculate average speed
            if (currentRecording.frames.Count > 0)
            {
                float totalSpeed = 0;
                foreach (var frame in currentRecording.frames)
                    totalSpeed += frame.speed;
                currentRecording.avgSpeed = totalSpeed / currentRecording.frames.Count;
            }

            Debug.Log($"[Telemetry] Recording ended: {lapTime:F2}s, {currentRecording.maxSpeed:F0} km/h max");

            return currentRecording;
        }
    }
}
