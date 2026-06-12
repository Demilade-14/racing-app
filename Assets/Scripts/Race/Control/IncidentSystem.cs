using UnityEngine;
using RacingGame.Race;
namespace RacingGame.Race.Control
{
    /// <summary>
    /// Handles specific incident triggers between drivers or with the environment.
    /// </summary>
    public class IncidentSystem
    {
        /// <summary>
        /// Check if two drivers collide (e.g., battling for position).
        /// </summary>
        public bool TriggerCollision(float aggressionA, float aggressionB)
        {
            // Combined aggression increases collision risk
            float risk = (aggressionA + aggressionB) * 0.005f;
            return Random.value < risk;
        }
        /// <summary>
        /// Check if a driver spins out (due to tire wear or wet conditions).
        /// </summary>
        public bool TriggerSpin(float tireWear, float weatherFactor)
        {
            // weatherFactor: 1.0 = dry, 0.0 = heavy rain
            float risk = (tireWear * 0.002f) + ((1f - weatherFactor) * 0.3f);
            return Random.value < risk;
        }
        /// <summary>
        /// Check if a car suffers a mechanical failure.
        /// </summary>
        public bool TriggerMechanicalFailure(float carHealth)
        {
            // carHealth: 1.0 = perfect, 0.0 = broken
            float risk = (1f - carHealth) * 0.2f;
            return Random.value < risk;
        }
    }
}
