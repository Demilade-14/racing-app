using System.Collections.Generic;
using UnityEngine;
using RacingGame.Career; // References CircuitData2026 from DriverCareerData.cs
namespace RacingGame.Data
{
    /// <summary>
    /// Centralized database for all 24 F1 2026 circuits.
    /// Provides static access to track data for calendar generation and race setup.
    /// </summary>
    public static class TrackDatabase
    {
        private static List<CircuitData2026> _allCircuits;
        /// <summary>
        /// Get all 24 circuits for the 2026 season.
        /// </summary>
        public static List<CircuitData2026> GetAllCircuits()
        {
            if (_allCircuits == null || _allCircuits.Count == 0)
            {
                _allCircuits = Build2026Calendar();
            }
            return _allCircuits;
        }
        /// <summary>
        /// Get a specific circuit by its round number (1-24).
        /// </summary>
        public static CircuitData2026 GetCircuitByRound(int round)
        {
            var circuits = GetAllCircuits();
            if (round < 1 || round > circuits.Count) return null;
            return circuits[round - 1];
        }
        /// <summary>
        /// Get a circuit by its short name (e.g., "MADRING", "MONZA").
        /// </summary>
        public static CircuitData2026 GetCircuitByShortName(string shortName)
        {
            return GetAllCircuits().Find(c => c.shortName == shortName.ToUpper());
        }
        /// <summary>
        /// Builds the official 2026 F1 calendar with realistic 2026 data.
        /// </summary>
        private static List<CircuitData2026> Build2026Calendar()
        {
            return new List<CircuitData2026>
            {
                // Round 1: Australia
                new CircuitData2026 {
                    name = "Albert Park Circuit", shortName = "MELBOURNE", country = "Australia", countryCode = "AU",
                    lapLengthKm = 5.27f, numberOfCorners = 14, drsZones = 4,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 76.4f, raceRound = 1, safetyCárProbability = 0.45f,
                    overtakeOpportunityRating = 0.75f, tyreWearMultiplier = 1.1f
                },
                // Round 2: China
                new CircuitData2026 {
                    name = "Shanghai International Circuit", shortName = "SHANGHAI", country = "China", countryCode = "CN",
                    lapLengthKm = 5.45f, numberOfCorners = 16, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 92.0f, raceRound = 2, safetyCárProbability = 0.30f,
                    overtakeOpportunityRating = 0.60f, tyreWearMultiplier = 0.95f
                },
                // Round 3: Japan
                new CircuitData2026 {
                    name = "Suzuka International Racing Course", shortName = "SUZUKA", country = "Japan", countryCode = "JP",
                    lapLengthKm = 5.80f, numberOfCorners = 18, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 99.0f, raceRound = 3, safetyCárProbability = 0.35f,
                    overtakeOpportunityRating = 0.55f, tyreWearMultiplier = 1.15f
                },
                // Round 4: Bahrain
                new CircuitData2026 {
                    name = "Bahrain International Circuit", shortName = "BAHRAIN", country = "Bahrain", countryCode = "BH",
                    lapLengthKm = 5.41f, numberOfCorners = 15, drsZones = 3,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Night,
                    lapRecord = 91.4f, raceRound = 4, safetyCárProbability = 0.25f,
                    overtakeOpportunityRating = 0.70f, tyreWearMultiplier = 1.2f
                },
                // Round 5: Saudi Arabia
                new CircuitData2026 {
                    name = "Jeddah Corniche Circuit", shortName = "JEDDAH", country = "Saudi Arabia", countryCode = "SA",
                    lapLengthKm = 6.17f, numberOfCorners = 27, drsZones = 3,
                    circuitType = CircuitType.Street, lighting = CircuitLighting.Night,
                    lapRecord = 88.2f, raceRound = 5, safetyCárProbability = 0.55f,
                    overtakeOpportunityRating = 0.80f, tyreWearMultiplier = 1.05f
                },
                // Round 6: Miami
                new CircuitData2026 {
                    name = "Miami International Autodrome", shortName = "MIAMI", country = "USA", countryCode = "US",
                    lapLengthKm = 5.41f, numberOfCorners = 19, drsZones = 3,
                    circuitType = CircuitType.Street, lighting = CircuitLighting.Standard,
                    lapRecord = 89.0f, raceRound = 6, safetyCárProbability = 0.40f,
                    overtakeOpportunityRating = 0.65f, tyreWearMultiplier = 1.1f
                },
                // Round 7: Imola
                new CircuitData2026 {
                    name = "Autodromo Enzo e Dino Ferrari", shortName = "IMOLA", country = "Italy", countryCode = "IT",
                    lapLengthKm = 4.90f, numberOfCorners = 19, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 78.0f, raceRound = 7, safetyCárProbability = 0.30f,
                    overtakeOpportunityRating = 0.45f, tyreWearMultiplier = 1.0f
                },
                // Round 8: Monaco
                new CircuitData2026 {
                    name = "Circuit de Monaco", shortName = "MONACO", country = "Monaco", countryCode = "MC",
                    lapLengthKm = 3.33f, numberOfCorners = 19, drsZones = 1,
                    circuitType = CircuitType.Street, lighting = CircuitLighting.Standard,
                    lapRecord = 73.0f, raceRound = 8, safetyCárProbability = 0.65f,
                    overtakeOpportunityRating = 0.15f, tyreWearMultiplier = 0.8f
                },
                // Round 9: Madrid (NEW 2026)
                CircuitData2026.Madrid(), 
                // Round 10: Canada
                new CircuitData2026 {
                    name = "Circuit Gilles Villeneuve", shortName = "MONTREAL", country = "Canada", countryCode = "CA",
                    lapLengthKm = 4.36f, numberOfCorners = 14, drsZones = 2,
                    circuitType = CircuitType.SemiStreet, lighting = CircuitLighting.Standard,
                    lapRecord = 76.0f, raceRound = 10, safetyCárProbability = 0.40f,
                    overtakeOpportunityRating = 0.70f, tyreWearMultiplier = 1.05f
                },
                // Round 11: Austria
                new CircuitData2026 {
                    name = "Red Bull Ring", shortName = "SPIELBERG", country = "Austria", countryCode = "AT",
                    lapLengthKm = 4.31f, numberOfCorners = 10, drsZones = 3,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 65.0f, raceRound = 11, safetyCárProbability = 0.35f,
                    overtakeOpportunityRating = 0.85f, tyreWearMultiplier = 1.15f
                },
                // Round 12: UK
                new CircuitData2026 {
                    name = "Silverstone Circuit", shortName = "SILVERSTONE", country = "United Kingdom", countryCode = "GB",
                    lapLengthKm = 5.89f, numberOfCorners = 18, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 90.0f, raceRound = 12, safetyCárProbability = 0.40f,
                    overtakeOpportunityRating = 0.75f, tyreWearMultiplier = 1.1f
                },
                // Round 13: Belgium
                new CircuitData2026 {
                    name = "Circuit de Spa-Francorchamps", shortName = "SPA", country = "Belgium", countryCode = "BE",
                    lapLengthKm = 7.00f, numberOfCorners = 19, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 105.0f, raceRound = 13, safetyCárProbability = 0.50f,
                    overtakeOpportunityRating = 0.60f, tyreWearMultiplier = 1.05f
                },
                // Round 14: Hungary
                new CircuitData2026 {
                    name = "Hungaroring", shortName = "BUDAPEST", country = "Hungary", countryCode = "HU",
                    lapLengthKm = 4.38f, numberOfCorners = 14, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 77.0f, raceRound = 14, safetyCárProbability = 0.30f,
                    overtakeOpportunityRating = 0.40f, tyreWearMultiplier = 1.25f
                },
                // Round 15: Netherlands
                new CircuitData2026 {
                    name = "Circuit Zandvoort", shortName = "ZANDVOORT", country = "Netherlands", countryCode = "NL",
                    lapLengthKm = 4.25f, numberOfCorners = 14, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 70.0f, raceRound = 15, safetyCárProbability = 0.35f,
                    overtakeOpportunityRating = 0.50f, tyreWearMultiplier = 1.1f
                },
                // Round 16: Monza
                new CircuitData2026 {
                    name = "Autodromo Nazionale Monza", shortName = "MONZA", country = "Italy", countryCode = "IT",
                    lapLengthKm = 5.79f, numberOfCorners = 11, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 81.0f, raceRound = 16, safetyCárProbability = 0.30f,
                    overtakeOpportunityRating = 0.80f, tyreWearMultiplier = 1.0f
                },
                // Round 17: Azerbaijan
                new CircuitData2026 {
                    name = "Baku City Circuit", shortName = "BAKU", country = "Azerbaijan", countryCode = "AZ",
                    lapLengthKm = 6.00f, numberOfCorners = 20, drsZones = 2,
                    circuitType = CircuitType.Street, lighting = CircuitLighting.Standard,
                    lapRecord = 104.0f, raceRound = 17, safetyCárProbability = 0.60f,
                    overtakeOpportunityRating = 0.75f, tyreWearMultiplier = 1.15f
                },
                // Round 18: Singapore
                new CircuitData2026 {
                    name = "Marina Bay Street Circuit", shortName = "SINGAPORE", country = "Singapore", countryCode = "SG",
                    lapLengthKm = 4.94f, numberOfCorners = 19, drsZones = 3,
                    circuitType = CircuitType.Street, lighting = CircuitLighting.Night,
                    lapRecord = 96.0f, raceRound = 18, safetyCárProbability = 0.55f,
                    overtakeOpportunityRating = 0.65f, tyreWearMultiplier = 1.3f
                },
                // Round 19: Austin
                new CircuitData2026 {
                    name = "Circuit of the Americas", shortName = "AUSTIN", country = "USA", countryCode = "US",
                    lapLengthKm = 5.51f, numberOfCorners = 20, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 97.0f, raceRound = 19, safetyCárProbability = 0.35f,
                    overtakeOpportunityRating = 0.70f, tyreWearMultiplier = 1.1f
                },
                // Round 20: Mexico
                new CircuitData2026 {
                    name = "Autódromo Hermanos Rodríguez", shortName = "MEXICO", country = "Mexico", countryCode = "MX",
                    lapLengthKm = 4.30f, numberOfCorners = 17, drsZones = 3,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 79.0f, raceRound = 20, safetyCárProbability = 0.30f,
                    overtakeOpportunityRating = 0.65f, tyreWearMultiplier = 1.05f
                },
                // Round 21: Brazil
                new CircuitData2026 {
                    name = "Autódromo José Carlos Pace", shortName = "INTERLAGOS", country = "Brazil", countryCode = "BR",
                    lapLengthKm = 4.30f, numberOfCorners = 15, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Standard,
                    lapRecord = 71.0f, raceRound = 21, safetyCárProbability = 0.45f,
                    overtakeOpportunityRating = 0.75f, tyreWearMultiplier = 1.15f
                },
                // Round 22: Las Vegas
                new CircuitData2026 {
                    name = "Las Vegas Strip Circuit", shortName = "VEGAS", country = "USA", countryCode = "US",
                    lapLengthKm = 6.12f, numberOfCorners = 17, drsZones = 2,
                    circuitType = CircuitType.Street, lighting = CircuitLighting.Night,
                    lapRecord = 94.0f, raceRound = 22, safetyCárProbability = 0.50f,
                    overtakeOpportunityRating = 0.70f, tyreWearMultiplier = 0.9f
                },
                // Round 23: Qatar
                new CircuitData2026 {
                    name = "Lusail International Circuit", shortName = "LUSAIL", country = "Qatar", countryCode = "QA",
                    lapLengthKm = 5.41f, numberOfCorners = 16, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Night,
                    lapRecord = 84.0f, raceRound = 23, safetyCárProbability = 0.30f,
                    overtakeOpportunityRating = 0.60f, tyreWearMultiplier = 1.2f
                },
                // Round 24: Abu Dhabi
                new CircuitData2026 {
                    name = "Yas Marina Circuit", shortName = "ABUDHABI", country = "UAE", countryCode = "AE",
                    lapLengthKm = 5.28f, numberOfCorners = 16, drsZones = 2,
                    circuitType = CircuitType.Permanent, lighting = CircuitLighting.Cinematic,
                    lapRecord = 86.0f, raceRound = 24, safetyCárProbability = 0.25f,
                    overtakeOpportunityRating = 0.55f, tyreWearMultiplier = 1.05f
                }
            };
        }
    }
}
