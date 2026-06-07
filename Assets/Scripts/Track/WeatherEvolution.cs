using System;
using System.Collections.Generic;
using UnityEngine;

namespace RacingGame.Track
{
    // ═══════════════════════════════════════════════════════════════════════
    //  WEATHER EVOLUTION SYSTEM – puddles, wind, drying line dynamics
    // ═══════════════════════════════════════════════════════════════════════
    public class WeatherEvolution : MonoBehaviour
    {
        public enum WeatherCondition { Dry, Wet, Intermediate }

        [System.Serializable]
        public class Puddle
        {
            public Vector3 position;
            public float radius = 5f;
            public float depth = 0.2f;  // affects aquaplane intensity
            public float waterLevel = 100f;  // 0-100%, decreases as it dries
            public int meshInstanceId;  // Reference to puddle visual mesh
        }

        [System.Serializable]
        public class WindState
        {
            public Vector3 direction = Vector3.forward;
            public float strength = 0f;  // 0-100 km/h
            public float gustIntensity = 0f;  // Random variation
            public float gustFrequency = 2f;  // Seconds per gust
        }

        public static WeatherEvolution Instance { get; private set; }

        public WeatherCondition currentWeather = WeatherCondition.Dry;
        public float wetness = 0f;  // 0-100%
        public List<Puddle> activePuddles = new();
        public WindState wind = new();

        // Drying line
        public Vector3 dryingLineStart = Vector3.zero;
        public Vector3 dryingLineEnd = Vector3.forward * 1000;
        public float dryingLineWidth = 3f;

        // Events
        public event Action<WeatherCondition> OnWeatherChanged;
        public event Action<Puddle> OnPuddleDetected;
        public event Action<Vector3> OnAquaplane;  // Position where aquaplaning occurred
        public event Action<float, Vector3> OnWindGust;  // (strength, direction)

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            wind.direction = Random.onUnitSphere;
            wind.strength = 5f;
        }

        void FixedUpdate()
        {
            UpdateWeatherState();
            UpdatePuddles();
            UpdateWind();
        }

        // ── WEATHER STATE ─────────────────────────────────────────────────
        void UpdateWeatherState()
        {
            // Gradually change wetness over time
            if (currentWeather == WeatherCondition.Wet)
            {
                wetness = Mathf.Clamp(wetness + Time.deltaTime * 2f, 0, 100);
            }
            else if (currentWeather == WeatherCondition.Intermediate)
            {
                wetness = Mathf.Clamp(wetness - Time.deltaTime * 0.5f, 0, 100);

                // Transition to dry when wetness low enough
                if (wetness < 5f)
                {
                    SetWeather(WeatherCondition.Dry);
                }
            }
            else  // Dry
            {
                wetness = Mathf.Clamp(wetness - Time.deltaTime * 0.2f, 0, 10);
            }

            // Update drying line visibility based on current condition
            UpdateDryingLine();
        }

        public void SetWeather(WeatherCondition newWeather)
        {
            if (newWeather == currentWeather) return;

            currentWeather = newWeather;

            switch (newWeather)
            {
                case WeatherCondition.Wet:
                    wetness = 100f;
                    GeneratePuddles();
                    break;
                case WeatherCondition.Intermediate:
                    wetness = 50f;
                    // Keep puddles but reduce depth
                    break;
                case WeatherCondition.Dry:
                    wetness = 0f;
                    activePuddles.Clear();
                    break;
            }

            OnWeatherChanged?.Invoke(newWeather);
            Debug.Log($"[Weather] Changed to {newWeather}, wetness: {wetness:F1}%");
        }

        // ── PUDDLE SYSTEM ─────────────────────────────────────────────────
        void GeneratePuddles()
        {
            activePuddles.Clear();

            // Generate puddles in low-lying track areas (simplified: random on track)
            for (int i = 0; i < 5; i++)
            {
                var puddle = new Puddle
                {
                    position = new Vector3(
                        Random.Range(-30, 30),
                        0.1f,
                        Random.Range(0, 500)
                    ),
                    radius = Random.Range(3f, 8f),
                    depth = Random.Range(0.1f, 0.4f),
                    waterLevel = 100f
                };

                activePuddles.Add(puddle);
                OnPuddleDetected?.Invoke(puddle);
            }

            Debug.Log($"[Weather] Generated {activePuddles.Count} puddles");
        }

        void UpdatePuddles()
        {
            // Puddles dry out gradually
            foreach (var puddle in activePuddles)
            {
                puddle.waterLevel -= Time.deltaTime * 3f;  // 3% per second
                puddle.depth = (puddle.waterLevel / 100f) * 0.4f;
            }

            // Remove dried puddles
            activePuddles.RemoveAll(p => p.waterLevel <= 0);
        }

        // ── AQUAPLANING ───────────────────────────────────────────────────
        public bool CheckAquaplane(Vector3 carPosition, float carSpeed)
        {
            if (currentWeather == WeatherCondition.Dry)
                return false;  // No aquaplaning on dry track

            // Check if car is in a puddle
            foreach (var puddle in activePuddles)
            {
                float distToPuddle = Vector3.Distance(carPosition, puddle.position);

                if (distToPuddle < puddle.radius)
                {
                    // Aquaplane if speed is high enough (over 150 km/h) and water level high
                    float speedThreshold = 150f - (puddle.waterLevel * 0.5f);  // Deeper = less threshold
                    
                    if (carSpeed > speedThreshold && puddle.waterLevel > 20f)
                    {
                        OnAquaplane?.Invoke(carPosition);
                        return true;
                    }
                }
            }

            return false;
        }

        // ── DRYING LINE ───────────────────────────────────────────────────
        void UpdateDryingLine()
        {
            // Drying line becomes active only in intermediate conditions
            if (currentWeather != WeatherCondition.Intermediate)
                return;

            // In real implementation, would visualize best-grip line to player
            // For now, we track it and apply grip bonus if on line
        }

        public float GetDryingLineGripBonus(Vector3 carPosition)
        {
            if (currentWeather != WeatherCondition.Intermediate)
                return 0f;

            // Check distance to drying line (simplified as center track line)
            float distFromLine = Mathf.Abs((carPosition - dryingLineStart).x);
            
            if (distFromLine < dryingLineWidth)
            {
                return 0.15f;  // +15% grip on drying line
            }

            return 0f;
        }

        // ── WIND SYSTEM ───────────────────────────────────────────────────
        void UpdateWind()
        {
            // Wind gusts occur periodically
            wind.gustIntensity = Mathf.Sin(Time.time * (Mathf.PI / wind.gustFrequency)) * 0.5f + 0.5f;

            // Wind strength varies
            wind.strength = Mathf.Clamp(
                wind.strength + Random.Range(-1f, 1f) * Time.deltaTime,
                0f, 40f  // Max 40 km/h wind
            );

            // Wind direction drifts slowly
            wind.direction = (wind.direction + Random.insideUnitSphere * 0.1f * Time.deltaTime).normalized;
        }

        public void SetWindDirection(Vector3 direction, float strength)
        {
            wind.direction = direction.normalized;
            wind.strength = Mathf.Clamp(strength, 0, 40f);
            Debug.Log($"[Weather] Wind set: {strength:F1} km/h, direction: {direction}");
        }

        // ── EFFECT CALCULATIONS ───────────────────────────────────────────
        public float GetTopSpeedModifier()
        {
            // Headwind reduces top speed, tailwind increases it
            float windEffect = Vector3.Dot(wind.direction, Vector3.forward) * (wind.strength / 100f) * 0.05f;
            return 1f - windEffect;  // Negative = headwind
        }

        public float GetStabilityModifier()
        {
            // Crosswind reduces stability
            float crosswindMagnitude = new Vector3(wind.direction.x, 0, wind.direction.z).magnitude;
            return 1f - (crosswindMagnitude * wind.strength * 0.001f);  // Up to 5% stability loss
        }

        public string GetWeatherDisplay()
        {
            return currentWeather switch
            {
                WeatherCondition.Wet => $"🌧️ WET ({wetness:F0}%)",
                WeatherCondition.Intermediate => $"🌦️ INT ({wetness:F0}%)",
                WeatherCondition.Dry => $"☀️ DRY",
                _ => "Unknown"
            };
        }

        public string GetWindDisplay()
        {
            string direction = wind.direction.z > 0.5f ? "Tailwind" : wind.direction.z < -0.5f ? "Headwind" : "Crosswind";
            return $"{direction}: {wind.strength:F1} km/h";
        }
    }
}
