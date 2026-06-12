using UnityEngine;
using RacingGame.Track;
using RacingGame.Core;
namespace RacingGame.Tires
{
    /// <summary>
    /// Manages all 4 tires for a single car
    /// Interfaces with Physics, Weather, and Pit Stop systems
    /// </summary>
    public class TireManager : MonoBehaviour
    {
        [Header("Tires")]
        public TireState frontLeft;
        public TireState frontRight;
        public TireState rearLeft;
        public TireState rearRight;
        [Header("Settings")]
        public TireCompound startingCompound;
        [Header("Mobile Optimization")]
        public bool useSimplifiedPhysics = true; // Less CPU on mobile
        private WeatherData currentWeather;
        private float trackTemperature;
        private float ambientTemperature;
        void Awake()
        {
            InitializeTires();
        }
        void InitializeTires()
        {
            frontLeft = new TireState();
            frontRight = new TireState();
            rearLeft = new TireState();
            rearRight = new TireState();
            frontLeft.Initialize(startingCompound);
            frontRight.Initialize(startingCompound);
            rearLeft.Initialize(startingCompound);
            rearRight.Initialize(startingCompound);
        }
        void Update()
        {
            // Get weather data
            UpdateWeatherData();
            // Update all tires
            float deltaTime = useSimplifiedPhysics ? Time.deltaTime * 0.5f : Time.deltaTime;
            frontLeft.UpdateTire(deltaTime, trackTemperature, ambientTemperature, IsWetTrack());
            frontRight.UpdateTire(deltaTime, trackTemperature, ambientTemperature, IsWetTrack());
            rearLeft.UpdateTire(deltaTime, trackTemperature, ambientTemperature, IsWetTrack());
            rearRight.UpdateTire(deltaTime, trackTemperature, ambientTemperature, IsWetTrack());
        }
        void UpdateWeatherData()
        {
            if (RacingGame.Track.TrackManager.Instance != null &&
                RacingGame.Track.TrackManager.Instance.CurrentCircuit != null)
            {
                ambientTemperature =
                    RacingGame.Track.TrackManager.Instance.CurrentCircuit.averageTrackTemperature - 5f;
                trackTemperature =
                    RacingGame.Track.TrackManager.Instance.CurrentCircuit.averageTrackTemperature;
            }
            else
            {
                ambientTemperature = 24f;
                trackTemperature = 36f;
            }
        }
        }
        bool IsWetTrack()
        {
            // TODO: Interface with WeatherSystem
            return currentWeather != null && currentWeather.rainIntensity > 0.3f;
        }
        /// <summary>
        /// Change all tires to new compound (called during pit stop)
        /// </summary>
        public void ChangeAllTires(TireCompound newCompound)
        {
            frontLeft.ChangeCompound(newCompound);
            frontRight.ChangeCompound(newCompound);
            rearLeft.ChangeCompound(newCompound);
            rearRight.ChangeCompound(newCompound);
        }
        /// <summary>
        /// Get average grip level across all tires
        /// </summary>
        public float GetAverageGrip()
        {
            return (frontLeft.gripLevel + frontRight.gripLevel + 
                    rearLeft.gripLevel + rearRight.gripLevel) / 4f;
        }
        /// <summary>
        /// Get the most worn tire
        /// </summary>
        public TireState GetMostWornTire()
        {
            TireState[] tires = { frontLeft, frontRight, rearLeft, rearRight };
            TireState mostWorn = tires[0];
            foreach (var tire in tires)
            {
                if (tire.wearPercent > mostWorn.wearPercent)
                    mostWorn = tire;
            }
            return mostWorn;
        }
        /// <summary>
        /// Check if any tire is destroyed (for DNF detection)
        /// </summary>
        public bool HasDestroyedTire()
        {
            return frontLeft.isDestroyed || frontRight.isDestroyed ||
                   rearLeft.isDestroyed || rearRight.isDestroyed;
        }
        /// <summary>
        /// Get tire data for UI/HUD
        /// </summary>
        public TireData2026 GetTireData()
        {
            return new TireData2026
            {
                frontLeftWear = frontLeft.wearPercent,
                frontRightWear = frontRight.wearPercent,
                rearLeftWear = rearLeft.wearPercent,
                rearRightWear = rearRight.wearPercent,
                frontLeftTemp = frontLeft.temperature,
                frontRightTemp = frontRight.temperature,
                rearLeftTemp = rearLeft.temperature,
                rearRightTemp = rearRight.temperature,
                currentCompound = frontLeft.currentCompound, // Assume all same
                averageGrip = GetAverageGrip()
            };
        }
    }
    // Data structure for UI
    [System.Serializable]
    public class TireData2026
    {
        public float frontLeftWear;
        public float frontRightWear;
        public float rearLeftWear;
        public float rearRightWear;
        public float frontLeftTemp;
        public float frontRightTemp;
        public float rearLeftTemp;
        public float rearRightTemp;
        public RacingGame.Core.TireCompound currentCompound;
        public float averageGrip;
    }
}


