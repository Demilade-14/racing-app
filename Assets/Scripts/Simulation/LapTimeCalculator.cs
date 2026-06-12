using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Track;
namespace RacingGame.Simulation
{
    /// <summary>
    /// Calculates simulated lap times based on driver skill, car, tires, and weather.
    /// </summary>
    public static class LapTimeCalculator
    {
        /// <summary>
        /// Calculate a single lap time in seconds.
        /// </summary>
        public static float CalculateLapTime(DriverData driver, CircuitProfile circuit, bool isWet, float tireLifePercent)
        {
            // 1. Base lap time (Circuit length / average speed)
            // Assume average F1 speed is ~200 km/h (55.5 m/s)
            float baseTime = (circuit.lengthKm * 1000f) / 55.5f;
            // 2. Driver & Car Performance Modifier
            float performance = DriverPerformanceCalculator.CalculateRacePace(driver, isWet, tireLifePercent);
            float timeModifier = 100f / performance; // Higher performance = lower time
            float lapTime = baseTime * timeModifier;
            // 3. Tire Degradation Penalty (Non-linear: gets much slower as tires die)
            float tirePenalty = 0f;
            if (tireLifePercent < 40f)
            {
                // "The Cliff" - exponential time loss
                float cliffFactor = (40f - tireLifePercent) / 40f;
                tirePenalty = cliffFactor * cliffFactor * 2.5f; // Up to 2.5 seconds lost
            }
            lapTime += tirePenalty;
            // 4. Consistency Variance (Randomness based on driver consistency)
            float variance = DriverPerformanceCalculator.GetLapTimeVariance(driver);
            float randomFactor = Random.Range(1f - variance, 1f + variance);
            lapTime *= randomFactor;
            return lapTime;
        }
        /// <summary>
        /// Calculate qualifying lap time (usually faster, less tire wear focus)
        /// </summary>
        public static float CalculateQualiLapTime(DriverData driver, CircuitProfile circuit, bool isWet)
        {
            float baseTime = (circuit.lengthKm * 1000f) / 58f; // Faster average speed in quali
            float performance = DriverPerformanceCalculator.CalculateQualifyingPace(driver, isWet);
            float timeModifier = 100f / performance;
            float lapTime = baseTime * timeModifier;
            // Quali has less variance (push laps)
            float variance = DriverPerformanceCalculator.GetLapTimeVariance(driver) * 0.5f;
            lapTime *= Random.Range(1f - variance, 1f + variance);
            return lapTime;
        }
    }
}
