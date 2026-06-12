using UnityEngine;
using System.Collections.Generic;
using RacingGame.Race;
namespace RacingGame.Race.Control
{
    /// <summary>
    /// Defines the types of incidents that can occur during a race.
    /// </summary>
    public enum IncidentType
    {
        None,
        TrackLimits,
        Collision,
        Spin,
        MechanicalFailure
    }
    /// <summary>
    /// Manages race control decisions: Safety Car, VSC, and incident evaluation.
    /// </summary>
    public class RaceControlSystem
    {
        public bool safetyCarActive = false;
        public bool virtualSafetyCarActive = false;
        public int safetyCarLapsRemaining = 0;
        public int vscLapsRemaining = 0;
        public float safetyCarProbability = 0.08f;
        public float vscProbability = 0.12f;
        public List<string> warnings = new List<string>();
        /// <summary>
        /// Evaluate if a specific driver has an incident this lap.
        /// </summary>
        public IncidentType EvaluateIncident(float aggression, float tireWear)
        {
            // Risk increases with aggressive driving and worn tires
            float risk = (aggression * 0.01f) + (tireWear * 0.002f);
            if (Random.value < risk * 0.2f)
                return IncidentType.Collision;
            if (Random.value < risk * 0.3f)
                return IncidentType.Spin;
            if (Random.value < risk * 0.1f)
                return IncidentType.TrackLimits;
            return IncidentType.None;
        }
        /// <summary>
        /// Check if a Safety Car or VSC should be deployed based on race incidents.
        /// Call this at the end of every lap.
        /// </summary>
        public void EvaluateSafetyCar()
        {
            if (safetyCarActive || virtualSafetyCarActive)
                return;
            float incidentChance = Random.value;
            if (incidentChance < safetyCarProbability)
            {
                safetyCarActive = true;
                safetyCarLapsRemaining = Random.Range(3, 6); // 3 to 5 laps
                Debug.Log("🚨 SAFETY CAR DEPLOYED");
            }
            else if (incidentChance < vscProbability)
            {
                virtualSafetyCarActive = true;
                vscLapsRemaining = Random.Range(1, 3); // 1 or 2 laps
                Debug.Log("🟡 VIRTUAL SAFETY CAR DEPLOYED");
            }
        }
        /// <summary>
        /// Tick the safety car periods down. Call this at the end of every lap.
        /// </summary>
        public void TickSafetyPeriods()
        {
            if (safetyCarActive)
            {
                safetyCarLapsRemaining--;
                if (safetyCarLapsRemaining <= 0)
                {
                    safetyCarActive = false;
                    Debug.Log("🏁 Safety Car in this lap - Green Flag");
                }
            }
            if (virtualSafetyCarActive)
            {
                vscLapsRemaining--;
                if (vscLapsRemaining <= 0)
                {
                    virtualSafetyCarActive = false;
                    Debug.Log("🟡 VSC Ending - Green Flag");
                }
            }
        }
    }
}
