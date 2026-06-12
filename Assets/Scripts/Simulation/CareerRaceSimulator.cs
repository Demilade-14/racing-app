using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Track;
namespace RacingGame.Simulation
{
    /// <summary>
    /// Simulates a full race weekend for the AI grid.
    /// Used for Career Mode when the player is not driving, or for fast-forwarding.
    /// </summary>
    public class CareerRaceSimulator : MonoBehaviour
    {
        public static CareerRaceSimulator Instance { get; private set; }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        /// <summary>
        /// Simulate a full race and return the finishing order.
        /// </summary>
        public List<DriverData> SimulateRace(List<DriverData> grid, CircuitProfile circuit, bool isWetRace)
        {
            int totalLaps = Mathf.RoundToInt(305f / circuit.lengthKm); // F1 ~305km race distance
            totalLaps = Mathf.Clamp(totalLaps, 40, 78);
            // Simulation state
            var driverStates = new Dictionary<DriverData, DriverRaceState>();
            foreach (var driver in grid)
            {
                driverStates[driver] = new DriverRaceState
                {
                    driver = driver,
                    tireLife = 100f,
                    hasPitted = false,
                    isRetired = false,
                    totalTime = 0f
                };
            }
            bool safetyCarActive = false;
            int safetyCarLaps = 0;
            // Race Loop
            for (int lap = 1; lap <= totalLaps; lap++)
            {
                foreach (var state in driverStates.Values)
                {
                    if (state.isRetired) continue;
                    // 1. Check for DNF
                    if (RaceIncidentManager.CheckForDNF(state.driver, circuit, lap, totalLaps))
                    {
                        state.isRetired = true;
                        state.retireReason = "Mechanical Failure";
                        continue;
                    }
                    // 2. Calculate Lap Time
                    float lapTime = LapTimeCalculator.CalculateLapTime(
                        state.driver, circuit, isWetRace, state.tireLife);
                    // 3. Apply Safety Car penalty (if active)
                    if (safetyCarActive)
                    {
                        lapTime *= 1.4f; // 40% slower under SC
                    }
                    // 4. Tire Degradation
                    float wearRate = 100f / (totalLaps * 1.2f); // Tires last ~1.2x race distance
                    state.tireLife -= wearRate * circuit.tyreWearFactor;
                    // 5. AI Pit Stop Logic (Pit when tires < 20%)
                    if (state.tireLife < 20f && !state.hasPitted)
                    {
                        float pitLoss = RaceIncidentManager.GetPitStopTimeLoss(circuit);
                        lapTime += pitLoss;
                        state.tireLife = 100f;
                        state.hasPitted = true;
                    }
                    state.totalTime += lapTime;
                }
                // 6. Safety Car Logic
                if (!safetyCarActive && RaceIncidentManager.CheckForSafetyCar(circuit, lap, isWetRace))
                {
                    safetyCarActive = true;
                    safetyCarLaps = Random.Range(2, 5);
                }
                else if (safetyCarActive)
                {
                    safetyCarLaps--;
                    if (safetyCarLaps <= 0) safetyCarActive = false;
                }
            }
            // Sort by total time (Retired drivers go to bottom)
            var results = driverStates.Values
                .OrderBy(s => s.isRetired ? 1 : 0)
                .ThenBy(s => s.totalTime)
                .Select(s => s.driver)
                .ToList();
            return results;
        }
        /// <summary>
        /// Simulate Qualifying and return grid order.
        /// </summary>
        public List<DriverData> SimulateQualifying(List<DriverData> drivers, CircuitProfile circuit, bool isWet)
        {
            var qualiTimes = new Dictionary<DriverData, float>();
            foreach (var driver in drivers)
            {
                // Simulate 3 laps, take the best
                float bestLap = float.MaxValue;
                for (int i = 0; i < 3; i++)
                {
                    float lap = LapTimeCalculator.CalculateQualiLapTime(driver, circuit, isWet);
                    if (lap < bestLap) bestLap = lap;
                }
                qualiTimes[driver] = bestLap;
            }
            return qualiTimes.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }
        // Internal state class for simulation
        private class DriverRaceState
        {
            public DriverData driver;
            public float tireLife;
            public bool hasPitted;
            public bool isRetired;
            public string retireReason;
            public float totalTime;
        }
    }
}
