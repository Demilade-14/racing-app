using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Track;
namespace RacingGame.Simulation
{
    /// <summary>
    /// Handles random race events: DNFs, Mechanical Failures, Safety Cars.
    /// </summary>
    public static class RaceIncidentManager
    {
        /// <summary>
        /// Check if a driver retires from the race (DNF).
        /// Returns true if DNF occurs.
        /// </summary>
        public static bool CheckForDNF(DriverData driver, CircuitProfile circuit, int currentLap, int totalLaps)
        {
            // Base DNF chance per lap (very low, ~0.2% per lap)
            float baseDNFChance = 0.002f;
            // Aggression increases DNF chance (crashes)
            float aggressionFactor = (driver.rating.aggression / 100f) * 0.003f;
            // Street circuits have higher DNF chance (walls)
            float circuitFactor = circuit.streetCircuit ? 0.002f : 0.0005f;
            // Wet weather increases DNF chance
            // (This would be passed in, simplified here)
            float totalChance = baseDNFChance + aggressionFactor + circuitFactor;
            return Random.value < totalChance;
        }
        /// <summary>
        /// Check if a Safety Car should be deployed this lap.
        /// Returns true if SC is deployed.
        /// </summary>
        public static bool CheckForSafetyCar(CircuitProfile circuit, int currentLap, bool isWet)
        {
            // Base SC chance per lap
            float baseSCChance = 0.005f;
            // Circuit specific probability (Monaco/Baku high, Monza low)
            float circuitModifier = circuit.safetyCarProbability * 0.01f;
            // Wet weather drastically increases SC chance
            float weatherModifier = isWet ? 0.02f : 0f;
            float totalChance = baseSCChance + circuitModifier + weatherModifier;
            return Random.value < totalChance;
        }
        /// <summary>
        /// Simulate a pit stop time loss (approx 20-25 seconds)
        /// </summary>
        public static float GetPitStopTimeLoss(CircuitProfile circuit)
        {
            return circuit.pitLossSeconds + Random.Range(-1f, 1f);
        }
    }
}
