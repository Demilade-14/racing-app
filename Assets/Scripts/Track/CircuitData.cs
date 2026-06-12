using UnityEngine;
namespace RacingGame.Track
{
    [System.Serializable]
    public class CircuitData
    {
        public string circuitName;
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
