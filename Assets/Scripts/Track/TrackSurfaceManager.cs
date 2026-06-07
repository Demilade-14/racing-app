using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Physics;

namespace RacingGame.Track
{
    // ═══════════════════════════════════════════════════════════════════════
    //  TRACK SURFACE MANAGER – rubber evolution, marbles, grip dynamics
    // ═══════════════════════════════════════════════════════════════════════
    public class TrackSurfaceManager : MonoBehaviour
    {
        [System.Serializable]
        public class TrackSection
        {
            public string name;              // "Turn 1 apex", "Straight 1", etc.
            public Vector3 centerPosition;
            public float width = 12f;
            public float rubberLevel = 0f;  // 0-100: grip from accumulated rubber
            public float marbleLevel = 0f;  // 0-100: loose rubber on edge
            public float tempFactor = 1f;   // 0.8-1.2: track temp modifier
        }

        public static TrackSurfaceManager Instance { get; private set; }

        public List<TrackSection> trackSections = new();
        public float totalLapsCompleted = 0f;
        public float baseGripCoefficient = 1.2f;

        // Events
        public event Action<string, float> OnRubberIn;      // (section, newRubberLevel)
        public event Action<string, float> OnMarbleBuildup;  // (section, marbleLevel)

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            GenerateTrackSections();
        }

        // ── TRACK EVOLUTION ───────────────────────────────────────────────
        void GenerateTrackSections()
        {
            // Define racing line sections (example for Monaco circuit)
            trackSections.Add(new TrackSection { name = "Turn 1 - Casino", centerPosition = Vector3.zero });
            trackSections.Add(new TrackSection { name = "Turn 2 - Mirabeau", centerPosition = Vector3.forward * 100 });
            trackSections.Add(new TrackSection { name = "Turn 3 - Portier", centerPosition = Vector3.forward * 200 });
            trackSections.Add(new TrackSection { name = "Tunnel", centerPosition = Vector3.forward * 300 });
            trackSections.Add(new TrackSection { name = "Tabac", centerPosition = Vector3.forward * 400 });

            Debug.Log($"[TrackSurface] Generated {trackSections.Count} track sections");
        }

        public void UpdateTrackState(Vector3 playerPosition, float lapProgress, float speed)
        {
            // Find which section player is in
            TrackSection currentSection = FindNearestSection(playerPosition);
            if (currentSection == null) return;

            // Determine if on racing line or outside (marbles zone)
            float distanceFromLine = DistanceFromRacingLine(playerPosition, currentSection);
            bool onRacingLine = distanceFromLine < 2f;

            if (onRacingLine)
            {
                // Accumulate rubber (grip improves)
                currentSection.rubberLevel += Time.deltaTime * 0.5f;  // Rubber in
                currentSection.rubberLevel = Mathf.Clamp(currentSection.rubberLevel, 0, 100);
                currentSection.marbleLevel *= 0.99f;  // Racing line clears marbles

                OnRubberIn?.Invoke(currentSection.name, currentSection.rubberLevel);
            }
            else
            {
                // Accumulate marbles (grip lost)
                currentSection.marbleLevel += Time.deltaTime * 0.3f;
                currentSection.marbleLevel = Mathf.Clamp(currentSection.marbleLevel, 0, 100);

                OnMarbleBuildup?.Invoke(currentSection.name, currentSection.marbleLevel);
            }

            // Track temperature affects grip (hotter = slightly more grip but tire degradation)
            currentSection.tempFactor = 0.9f + (Mathf.Sin(Time.time * 0.1f) * 0.15f);  // 0.8-1.2 cycle
        }

        TrackSection FindNearestSection(Vector3 position)
        {
            float minDistance = float.MaxValue;
            TrackSection nearest = null;

            foreach (var section in trackSections)
            {
                float dist = Vector3.Distance(position, section.centerPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = section;
                }
            }

            return nearest;
        }

        float DistanceFromRacingLine(Vector3 position, TrackSection section)
        {
            // Simplified: distance to section center (in reality, would use Bezier curve for racing line)
            return Vector3.Distance(position, section.centerPosition);
        }

        // ── GRIP CALCULATION ──────────────────────────────────────────────
        public float GetGripCoefficient(Vector3 position, float tireTemp)
        {
            TrackSection section = FindNearestSection(position);
            if (section == null) return baseGripCoefficient;

            // Base grip modified by rubber, marbles, and temp
            float gripModifier = 1f;

            // Rubber in increases grip (0-100 rubber = 1.0-1.15x multiplier)
            gripModifier += (section.rubberLevel / 100f) * 0.15f;

            // Marbles reduce grip (0-100 marbles = 1.0-0.7x multiplier)
            gripModifier -= (section.marbleLevel / 100f) * 0.3f;

            // Track temperature effect
            gripModifier *= section.tempFactor;

            // Tire temperature coupling
            float tireGrip = Mathf.Clamp(tireTemp / 100f, 0.5f, 1.2f);  // 50-120°C window
            gripModifier *= tireGrip;

            return baseGripCoefficient * gripModifier;
        }

        public float GetMarbleImpact(Vector3 position)
        {
            TrackSection section = FindNearestSection(position);
            if (section == null) return 0f;

            // Marbles cause grip loss and tire damage
            float marbleGripLoss = (section.marbleLevel / 100f) * 0.4f;  // Up to 40% grip loss
            return marbleGripLoss;
        }

        // ── LAP COUNTING ──────────────────────────────────────────────────
        public void CompleteLap()
        {
            totalLapsCompleted += 1f;

            // Gradually reset rubber/marbles each new session
            if (totalLapsCompleted % 3 == 0)  // Every 3 laps
            {
                foreach (var section in trackSections)
                {
                    section.rubberLevel *= 0.95f;  // Slight reset
                    section.marbleLevel *= 1.05f;  // Marbles accumulate faster
                }
            }

            Debug.Log($"[TrackSurface] Lap complete. Total laps: {totalLapsCompleted}");
        }

        // ── RESET ─────────────────────────────────────────────────────────
        public void ResetSession()
        {
            foreach (var section in trackSections)
            {
                section.rubberLevel = 0f;
                section.marbleLevel = 0f;
            }

            totalLapsCompleted = 0f;
            Debug.Log("[TrackSurface] Session reset - fresh track");
        }

        public void RainEvent()
        {
            // Rain washes away marbles, resets rubber
            foreach (var section in trackSections)
            {
                section.marbleLevel = 0f;
                section.rubberLevel *= 0.3f;  // Rubber less effective when wet
            }

            Debug.Log("[TrackSurface] Rain event - track reset to intermediate conditions");
        }
    }
}
