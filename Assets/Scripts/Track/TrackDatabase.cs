using System.Collections.Generic;
using UnityEngine;
namespace RacingGame.Track
{
    public static class TrackDatabase
    {
        public static List<CircuitProfile> GetAllCircuits()
        {
            return new List<CircuitProfile>
            {
                // ── EUROPE (9) ──────────────────────────────────────────────
                new CircuitProfile { circuitName = "Monaco", region = "Europe", lengthKm = 3.337f, corners = 19, drsZones = 1, pitLossSeconds = 20f, tyreWearFactor = 0.85f, rainProbability = 0.25f, overtakingDifficulty = 0.98f, trackEvolutionRate = 1.50f, averageTrackTemperature = 29f, streetCircuit = true },
                new CircuitProfile { circuitName = "Silverstone", region = "Europe", lengthKm = 5.891f, corners = 18, drsZones = 2, pitLossSeconds = 20f, tyreWearFactor = 1.35f, rainProbability = 0.50f, overtakingDifficulty = 0.45f, trackEvolutionRate = 1.25f, averageTrackTemperature = 24f, streetCircuit = false },
                new CircuitProfile { circuitName = "Spa", region = "Europe", lengthKm = 7.004f, corners = 19, drsZones = 2, pitLossSeconds = 21f, tyreWearFactor = 1.25f, rainProbability = 0.55f, overtakingDifficulty = 0.35f, trackEvolutionRate = 1.20f, averageTrackTemperature = 22f, streetCircuit = false },
                new CircuitProfile { circuitName = "Monza", region = "Europe", lengthKm = 5.793f, corners = 11, drsZones = 2, pitLossSeconds = 23f, tyreWearFactor = 0.95f, rainProbability = 0.25f, overtakingDifficulty = 0.25f, trackEvolutionRate = 1.05f, averageTrackTemperature = 31f, streetCircuit = false },
                new CircuitProfile { circuitName = "Imola", region = "Europe", lengthKm = 4.909f, corners = 19, drsZones = 2, pitLossSeconds = 19f, tyreWearFactor = 1.10f, rainProbability = 0.30f, overtakingDifficulty = 0.55f, trackEvolutionRate = 1.15f, averageTrackTemperature = 28f, streetCircuit = false },
                new CircuitProfile { circuitName = "Hungary", region = "Europe", lengthKm = 4.381f, corners = 14, drsZones = 2, pitLossSeconds = 18f, tyreWearFactor = 1.40f, rainProbability = 0.20f, overtakingDifficulty = 0.75f, trackEvolutionRate = 1.30f, averageTrackTemperature = 32f, streetCircuit = false },
                new CircuitProfile { circuitName = "Austria", region = "Europe", lengthKm = 4.318f, corners = 10, drsZones = 3, pitLossSeconds = 17f, tyreWearFactor = 1.20f, rainProbability = 0.35f, overtakingDifficulty = 0.40f, trackEvolutionRate = 1.10f, averageTrackTemperature = 26f, streetCircuit = false },
                new CircuitProfile { circuitName = "Zandvoort", region = "Europe", lengthKm = 4.259f, corners = 14, drsZones = 2, pitLossSeconds = 19f, tyreWearFactor = 1.15f, rainProbability = 0.40f, overtakingDifficulty = 0.60f, trackEvolutionRate = 1.20f, averageTrackTemperature = 23f, streetCircuit = false },
                new CircuitProfile { circuitName = "Barcelona", region = "Europe", lengthKm = 4.657f, corners = 16, drsZones = 2, pitLossSeconds = 20f, tyreWearFactor = 1.30f, rainProbability = 0.15f, overtakingDifficulty = 0.50f, trackEvolutionRate = 1.25f, averageTrackTemperature = 35f, streetCircuit = false },
                // ── MIDDLE EAST (4) ─────────────────────────────────────────
                new CircuitProfile { circuitName = "Bahrain", region = "Middle East", lengthKm = 5.412f, corners = 15, drsZones = 3, pitLossSeconds = 22f, tyreWearFactor = 1.15f, rainProbability = 0.01f, overtakingDifficulty = 0.30f, trackEvolutionRate = 1.20f, averageTrackTemperature = 38f, streetCircuit = false },
                new CircuitProfile { circuitName = "Saudi Arabia", region = "Middle East", lengthKm = 6.174f, corners = 27, drsZones = 3, pitLossSeconds = 18f, tyreWearFactor = 1.00f, rainProbability = 0.02f, overtakingDifficulty = 0.40f, trackEvolutionRate = 1.10f, averageTrackTemperature = 34f, streetCircuit = true },
                new CircuitProfile { circuitName = "Qatar", region = "Middle East", lengthKm = 5.419f, corners = 16, drsZones = 2, pitLossSeconds = 21f, tyreWearFactor = 1.25f, rainProbability = 0.05f, overtakingDifficulty = 0.45f, trackEvolutionRate = 1.15f, averageTrackTemperature = 36f, streetCircuit = false },
                new CircuitProfile { circuitName = "Abu Dhabi", region = "Middle East", lengthKm = 5.281f, corners = 16, drsZones = 2, pitLossSeconds = 20f, tyreWearFactor = 1.10f, rainProbability = 0.03f, overtakingDifficulty = 0.50f, trackEvolutionRate = 1.10f, averageTrackTemperature = 37f, streetCircuit = false },
                // ── AMERICAS (6) ────────────────────────────────────────────
                new CircuitProfile { circuitName = "Miami", region = "Americas", lengthKm = 5.412f, corners = 19, drsZones = 3, pitLossSeconds = 20f, tyreWearFactor = 1.05f, rainProbability = 0.30f, overtakingDifficulty = 0.55f, trackEvolutionRate = 1.15f, averageTrackTemperature = 33f, streetCircuit = true },
                new CircuitProfile { circuitName = "Austin", region = "Americas", lengthKm = 5.513f, corners = 20, drsZones = 2, pitLossSeconds = 21f, tyreWearFactor = 1.20f, rainProbability = 0.25f, overtakingDifficulty = 0.45f, trackEvolutionRate = 1.20f, averageTrackTemperature = 30f, streetCircuit = false },
                new CircuitProfile { circuitName = "Mexico", region = "Americas", lengthKm = 4.304f, corners = 17, drsZones = 3, pitLossSeconds = 19f, tyreWearFactor = 1.00f, rainProbability = 0.20f, overtakingDifficulty = 0.35f, trackEvolutionRate = 1.10f, averageTrackTemperature = 28f, streetCircuit = false },
                new CircuitProfile { circuitName = "Brazil", region = "Americas", lengthKm = 4.309f, corners = 15, drsZones = 2, pitLossSeconds = 20f, tyreWearFactor = 1.15f, rainProbability = 0.45f, overtakingDifficulty = 0.40f, trackEvolutionRate = 1.25f, averageTrackTemperature = 27f, streetCircuit = false },
                new CircuitProfile { circuitName = "Las Vegas", region = "Americas", lengthKm = 6.120f, corners = 17, drsZones = 2, pitLossSeconds = 22f, tyreWearFactor = 0.90f, rainProbability = 0.10f, overtakingDifficulty = 0.50f, trackEvolutionRate = 1.05f, averageTrackTemperature = 18f, streetCircuit = true },
                new CircuitProfile { circuitName = "Canada", region = "Americas", lengthKm = 4.361f, corners = 14, drsZones = 2, pitLossSeconds = 19f, tyreWearFactor = 1.10f, rainProbability = 0.35f, overtakingDifficulty = 0.45f, trackEvolutionRate = 1.15f, averageTrackTemperature = 25f, streetCircuit = false },
                // ── ASIA-PACIFIC (4) ────────────────────────────────────────
                new CircuitProfile { circuitName = "Australia", region = "Asia-Pacific", lengthKm = 5.278f, corners = 14, drsZones = 4, pitLossSeconds = 20f, tyreWearFactor = 0.95f, rainProbability = 0.30f, overtakingDifficulty = 0.45f, trackEvolutionRate = 1.15f, averageTrackTemperature = 28f, streetCircuit = false },
                new CircuitProfile { circuitName = "China", region = "Asia-Pacific", lengthKm = 5.451f, corners = 16, drsZones = 2, pitLossSeconds = 21f, tyreWearFactor = 1.05f, rainProbability = 0.35f, overtakingDifficulty = 0.50f, trackEvolutionRate = 1.10f, averageTrackTemperature = 26f, streetCircuit = false },
                new CircuitProfile { circuitName = "Singapore", region = "Asia-Pacific", lengthKm = 4.940f, corners = 19, drsZones = 3, pitLossSeconds = 24f, tyreWearFactor = 1.30f, rainProbability = 0.40f, overtakingDifficulty = 0.70f, trackEvolutionRate = 1.20f, averageTrackTemperature = 32f, streetCircuit = true },
                new CircuitProfile { circuitName = "Suzuka", region = "Asia-Pacific", lengthKm = 5.807f, corners = 18, drsZones = 2, pitLossSeconds = 21f, tyreWearFactor = 1.20f, rainProbability = 0.45f, overtakingDifficulty = 0.55f, trackEvolutionRate = 1.15f, averageTrackTemperature = 27f, streetCircuit = false },
                // ── AFRICA (1 - Expansion) ──────────────────────────────────
                new CircuitProfile { circuitName = "Kyalami", region = "Africa", lengthKm = 4.522f, corners = 17, drsZones = 2, pitLossSeconds = 20f, tyreWearFactor = 1.15f, rainProbability = 0.30f, overtakingDifficulty = 0.50f, trackEvolutionRate = 1.20f, averageTrackTemperature = 29f, streetCircuit = false }
            };
        }
        public static CircuitProfile GetCircuitByName(string name)
        {
            foreach (var c in GetAllCircuits())
            {
                if (c.circuitName.ToLower() == name.ToLower()) return c;
            }
            return null;
        }
    }
}
