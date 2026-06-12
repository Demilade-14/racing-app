using UnityEngine;
namespace RacingGame.Track
{
    /// <summary>
    /// Detailed profile for a circuit, used by TrackManager for physics and weather.
    /// NOTE: Named CircuitProfile to avoid conflict with RacingGame.Data.CircuitData.
    /// </summary>
    [System.Serializable]
    public class CircuitProfile
    {
        public string circuitName;
        public string region;
        public float lengthKm;
        public int corners;
        public int drsZones;
        public float pitLossSeconds;
        public float tyreWearFactor;
        public float rainProbability;
        public float overtakingDifficulty;
        public float trackEvolutionRate;
        public float averageTrackTemperature;
        public bool streetCircuit;
    }
}
