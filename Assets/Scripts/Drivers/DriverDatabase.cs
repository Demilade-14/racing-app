using System.Collections.Generic;
using UnityEngine;
namespace RacingGame.Drivers
{
    /// <summary>
    /// Complete database of all F1 2026 drivers with realistic skill ratings
    /// </summary>
    public static class DriverDatabase
    {
        private static List<DriverData> _drivers;
        /// <summary>
        /// Get all drivers in the database
        /// </summary>
        public static List<DriverData> Drivers
        {
            get
            {
                if (_drivers == null || _drivers.Count == 0)
                    _drivers = Build2026Grid();
                return _drivers;
            }
        }
        /// <summary>
        /// Remove a driver from the database (for retirement)
        /// </summary>
        public static bool RemoveDriver(DriverData driver)
        {
            if (_drivers == null) return false;
            return _drivers.Remove(driver);
        }
        /// <summary>
        /// Add a new driver to the database (for regen)
        /// </summary>
        public static void AddDriver(DriverData driver)
        {
            if (_drivers == null) _drivers = Build2026Grid();
            _drivers.Add(driver);
        }
        /// <summary>
        /// Get driver by full name
        /// </summary>
        public static DriverData GetDriverByName(string fullName)
        {
            return Drivers.Find(d => d.fullName.ToLower() == fullName.ToLower());
        }
        /// <summary>
        /// Get driver by short code
        /// </summary>
        public static DriverData GetDriverByCode(string code)
        {
            return Drivers.Find(d => d.shortName.ToUpper() == code.ToUpper());
        }
        /// <summary>
        /// Build the complete 2026 F1 grid with realistic ratings
        /// </summary>
        private static List<DriverData> Build2026Grid()
        {
            return new List<DriverData>
            {
                // ── RED BULL RACING ─────────────────────────────────────────
                new DriverData
                {
                    fullName = "Max Verstappen", shortName = "VER", age = 28, nationality = "Netherlands",
                    teamName = "Red Bull Racing", driverNumber = 1, seasonsInF1 = 9,
                    rating = new DriverRating { pace = 99, wetSkill = 96, tireManagement = 97, overtaking = 99, defending = 98, consistency = 99, aggression = 75 },
                    championships = 4, wins = 63, podiums = 112, poles = 40
                },
                new DriverData
                {
                    fullName = "Sergio Perez", shortName = "PER", age = 35, nationality = "Mexico",
                    teamName = "Red Bull Racing", driverNumber = 11, seasonsInF1 = 14,
                    rating = new DriverRating { pace = 87, wetSkill = 84, tireManagement = 86, overtaking = 87, defending = 89, consistency = 85, aggression = 65 },
                    wins = 6, podiums = 39, poles = 4
                },
                // ── FERRARI ─────────────────────────────────────────────────
                new DriverData
                {
                    fullName = "Charles Leclerc", shortName = "LEC", age = 27, nationality = "Monaco",
                    teamName = "Ferrari", driverNumber = 16, seasonsInF1 = 7,
                    rating = new DriverRating { pace = 96, wetSkill = 91, tireManagement = 88, overtaking = 93, defending = 89, consistency = 90, aggression = 70 },
                    wins = 7, podiums = 40, poles = 26
                },
                new DriverData
                {
                    fullName = "Lewis Hamilton", shortName = "HAM", age = 40, nationality = "United Kingdom",
                    teamName = "Ferrari", driverNumber = 44, seasonsInF1 = 19,
                    rating = new DriverRating { pace = 94, wetSkill = 97, tireManagement = 95, overtaking = 94, defending = 95, consistency = 96, aggression = 60 },
                    championships = 7, wins = 105, podiums = 201, poles = 104
                },
                // ── McLAREN ─────────────────────────────────────────────────
                new DriverData
                {
                    fullName = "Lando Norris", shortName = "NOR", age = 25, nationality = "United Kingdom",
                    teamName = "McLaren", driverNumber = 4, seasonsInF1 = 7,
                    rating = new DriverRating { pace = 95, wetSkill = 90, tireManagement = 91, overtaking = 94, defending = 91, consistency = 93, aggression = 68 },
                    wins = 4, podiums = 24, poles = 8
                },
                new DriverData
                {
                    fullName = "Oscar Piastri", shortName = "PIA", age = 24, nationality = "Australia",
                    teamName = "McLaren", driverNumber = 81, seasonsInF1 = 3,
                    rating = new DriverRating { pace = 92, wetSkill = 87, tireManagement = 89, overtaking = 90, defending = 88, consistency = 90, aggression = 65 },
                    wins = 2, podiums = 11, poles = 3
                },
                // ── MERCEDES ────────────────────────────────────────────────
                new DriverData
                {
                    fullName = "George Russell", shortName = "RUS", age = 26, nationality = "United Kingdom",
                    teamName = "Mercedes", driverNumber = 63, seasonsInF1 = 6,
                    rating = new DriverRating { pace = 91, wetSkill = 88, tireManagement = 90, overtaking = 89, defending = 90, consistency = 91, aggression = 62 },
                    wins = 3, podiums = 23, poles = 6
                },
                new DriverData
                {
                    fullName = "Kimi Antonelli", shortName = "ANT", age = 19, nationality = "Italy",
                    teamName = "Mercedes", driverNumber = 12, seasonsInF1 = 1, isRookie = true,
                    rating = new DriverRating { pace = 85, wetSkill = 82, tireManagement = 83, overtaking = 84, defending = 81, consistency = 83, aggression = 70 }
                },
                // ── ASTON MARTIN ────────────────────────────────────────────
                new DriverData
                {
                    fullName = "Fernando Alonso", shortName = "ALO", age = 44, nationality = "Spain",
                    teamName = "Aston Martin", driverNumber = 14, seasonsInF1 = 22,
                    rating = new DriverRating { pace = 89, wetSkill = 94, tireManagement = 93, overtaking = 91, defending = 93, consistency = 94, aggression = 55 },
                    championships = 2, wins = 32, podiums = 106, poles = 22
                },
                new DriverData
                {
                    fullName = "Lance Stroll", shortName = "STR", age = 26, nationality = "Canada",
                    teamName = "Aston Martin", driverNumber = 18, seasonsInF1 = 9,
                    rating = new DriverRating { pace = 82, wetSkill = 83, tireManagement = 81, overtaking = 80, defending = 81, consistency = 80, aggression = 60 },
                    podiums = 3, poles = 1
                },
                // ── ALPINE ──────────────────────────────────────────────────
                new DriverData
                {
                    fullName = "Pierre Gasly", shortName = "GAS", age = 29, nationality = "France",
                    teamName = "Alpine", driverNumber = 10, seasonsInF1 = 9,
                    rating = new DriverRating { pace = 86, wetSkill = 85, tireManagement = 84, overtaking = 85, defending = 84, consistency = 85, aggression = 65 },
                    wins = 1, podiums = 5
                },
                new DriverData
                {
                    fullName = "Esteban Ocon", shortName = "OCO", age = 28, nationality = "France",
                    teamName = "Alpine", driverNumber = 31, seasonsInF1 = 9,
                    rating = new DriverRating { pace = 85, wetSkill = 84, tireManagement = 85, overtaking = 84, defending = 83, consistency = 84, aggression = 63 },
                    wins = 1, podiums = 4
                },
                // ── WILLIAMS ────────────────────────────────────────────────
                new DriverData
                {
                    fullName = "Alex Albon", shortName = "ALB", age = 29, nationality = "Thailand",
                    teamName = "Williams", driverNumber = 23, seasonsInF1 = 6,
                    rating = new DriverRating { pace = 84, wetSkill = 86, tireManagement = 83, overtaking = 83, defending = 82, consistency = 83, aggression = 62 },
                    podiums = 2
                },
                new DriverData
                {
                    fullName = "Carlos Sainz", shortName = "SAI", age = 30, nationality = "Spain",
                    teamName = "Williams", driverNumber = 55, seasonsInF1 = 10,
                    rating = new DriverRating { pace = 90, wetSkill = 89, tireManagement = 91, overtaking = 88, defending = 88, consistency = 90, aggression = 64 },
                    wins = 4, podiums = 30, poles = 5
                },
                // ── HAAS ────────────────────────────────────────────────────
                new DriverData
                {
                    fullName = "Nico Hulkenberg", shortName = "HUL", age = 37, nationality = "Germany",
                    teamName = "Haas", driverNumber = 27, seasonsInF1 = 13,
                    rating = new DriverRating { pace = 83, wetSkill = 85, tireManagement = 84, overtaking = 82, defending = 83, consistency = 84, aggression = 58 },
                    poles = 1
                },
                new DriverData
                {
                    fullName = "Oliver Bearman", shortName = "BEA", age = 20, nationality = "United Kingdom",
                    teamName = "Haas", driverNumber = 87, seasonsInF1 = 2,
                    rating = new DriverRating { pace = 86, wetSkill = 83, tireManagement = 84, overtaking = 85, defending = 82, consistency = 84, aggression = 72 }
                },
                // ── RB (VCARB) ──────────────────────────────────────────────
                new DriverData
                {
                    fullName = "Yuki Tsunoda", shortName = "TSU", age = 25, nationality = "Japan",
                    teamName = "RB", driverNumber = 22, seasonsInF1 = 5,
                    rating = new DriverRating { pace = 83, wetSkill = 81, tireManagement = 80, overtaking = 84, defending = 80, consistency = 81, aggression = 78 }
                },
                new DriverData
                {
                    fullName = "Isack Hadjar", shortName = "HAD", age = 20, nationality = "France",
                    teamName = "RB", driverNumber = 6, seasonsInF1 = 1, isRookie = true,
                    rating = new DriverRating { pace = 84, wetSkill = 81, tireManagement = 82, overtaking = 83, defending = 80, consistency = 82, aggression = 74 }
                },
                // ── AUDI (NEW 2026) ─────────────────────────────────────────
                new DriverData
                {
                    fullName = "Valtteri Bottas", shortName = "BOT", age = 36, nationality = "Finland",
                    teamName = "Audi", driverNumber = 77, seasonsInF1 = 13,
                    rating = new DriverRating { pace = 84, wetSkill = 86, tireManagement = 87, overtaking = 82, defending = 84, consistency = 86, aggression = 55 },
                    wins = 10, podiums = 67, poles = 20
                },
                new DriverData
                {
                    fullName = "Zhou Guanyu", shortName = "ZHO", age = 26, nationality = "China",
                    teamName = "Audi", driverNumber = 24, seasonsInF1 = 4,
                    rating = new DriverRating { pace = 81, wetSkill = 80, tireManagement = 82, overtaking = 79, defending = 80, consistency = 81, aggression = 58 }
                },
                // ── CADILLAC (NEW 11TH TEAM 2026) ───────────────────────────
                new DriverData
                {
                    fullName = "Colton Herta", shortName = "HER", age = 25, nationality = "USA",
                    teamName = "Cadillac", driverNumber = 28, seasonsInF1 = 1, isRookie = true,
                    rating = new DriverRating { pace = 83, wetSkill = 80, tireManagement = 81, overtaking = 82, defending = 79, consistency = 81, aggression = 73 }
                },
                new DriverData
                {
                    fullName = "Andrea Kimi Antonelli", shortName = "AKA", age = 19, nationality = "Italy",
                    teamName = "Cadillac", driverNumber = 99, seasonsInF1 = 1, isRookie = true,
                    rating = new DriverRating { pace = 85, wetSkill = 82, tireManagement = 83, overtaking = 84, defending = 81, consistency = 83, aggression = 71 }
                }
            };
        }
        /// <summary>
        /// Get all active drivers
        /// </summary>
        public static List<DriverData> GetActiveDrivers()
        {
            return Drivers.FindAll(d => d.isActive);
        }
        /// <summary>
        /// Get drivers by team
        /// </summary>
        public static List<DriverData> GetDriversByTeam(string teamName)
        {
            return Drivers.FindAll(d => d.teamName.ToLower() == teamName.ToLower());
        }
        /// <summary>
        /// Get top drivers by overall rating
        /// </summary>
        public static List<DriverData> GetTopDrivers(int count = 10)
        {
            return Drivers.OrderByDescending(d => d.GetOverallRating()).Take(count).ToList();
        }
    }
}

