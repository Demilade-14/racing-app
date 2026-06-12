using UnityEngine;
namespace RacingGame.Track
{
    public class TrackManager : MonoBehaviour
    {
        public static TrackManager Instance;
        public CircuitProfile CurrentCircuit;
        void Awake()
        {
            Instance = this;
        }
        public void LoadCircuit(string circuitName)
        {
            var circuit = TrackDatabase.GetCircuitByName(circuitName);
            if (circuit != null)
            {
                CurrentCircuit = circuit;
                Debug.Log($"Loaded Circuit: {circuit.circuitName} ({circuit.region})");
                return;
            }
            Debug.LogError($"Circuit not found: {circuitName}");
        }
    }
}
