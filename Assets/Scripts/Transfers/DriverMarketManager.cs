using UnityEngine;
using RacingGame.Drivers;
using System.IO;
namespace RacingGame.Transfers
{
    public class DriverMarketManager : MonoBehaviour
    {
        public int currentSeason = 1;
        private string savePath;
        void Awake()
        {
            savePath = Path.Combine(Application.persistentDataPath, "driver_market_save.json");
            LoadState();
        }
        public void ProcessEndOfSeason()
        {
            Debug.Log($"[Market] Processing End of Season {currentSeason}...");
            // 1. Age drivers and process retirements
            for (int i = DriverDatabase.Drivers.Count - 1; i >= 0; i--)
            {
                var driver = DriverDatabase.Drivers[i];
                driver.age++;
                if (RetirementSystem.ShouldRetire(driver))
                {
                    Debug.Log($"[Market] {driver.fullName} (Age {driver.age}) has retired.");
                    RemoveFromTeams(driver.fullName);
                    DriverDatabase.Drivers.RemoveAt(i);
                }
            }
            // 2. Fill vacancies with Regen drivers
            int totalSeats = TransferTeamDatabase.Teams.Count * 2;
            int currentDrivers = DriverDatabase.Drivers.Count;
            int needed = totalSeats - currentDrivers;
            if (needed > 0)
            {
                Debug.Log($"[Market] Generating {needed} rookie drivers for Season {currentSeason + 1}...");
                for (int i = 0; i < needed; i++)
                {
                    var rookie = RegenGenerator.GenerateDriver(currentSeason);
                    DriverDatabase.Drivers.Add(rookie);
                }
            }
            currentSeason++;
            SaveState();
        }
        void RemoveFromTeams(string driverName)
        {
            foreach (var team in TransferTeamDatabase.Teams)
            {
                team.RemoveDriver(driverName);
            }
        }
        // ── PERSISTENCE SYSTEM (Fixes Critique #1) ──────────────────────────
        [System.Serializable]
        private class MarketSaveData
        {
            public int season;
            public System.Collections.Generic.List<string> teamRosters = new();
        }
        public void SaveState()
        {
            var data = new MarketSaveData { season = currentSeason };
            foreach (var team in TransferTeamDatabase.Teams)
            {
                data.teamRosters.Add(team.teamName + ":" + string.Join(",", team.drivers));
            }
            File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
        }
        public void LoadState()
        {
            if (!File.Exists(savePath)) return;
            try
            {
                var json = File.ReadAllText(savePath);
                var data = JsonUtility.FromJson<MarketSaveData>(json);
                currentSeason = data.season;
                // Restore rosters
                foreach (var team in TransferTeamDatabase.Teams) team.drivers.Clear();
                foreach (var entry in data.teamRosters)
                {
                    var parts = entry.Split(':');
                    var team = TransferTeamDatabase.GetTeamByName(parts[0]);
                    if (team != null && parts.Length > 1)
                    {
                        team.drivers.AddRange(parts[1].Split(','));
                    }
                }
                Debug.Log("[Market] State loaded from disk.");
            }
            catch (System.Exception e) { Debug.LogError("[Market] Load failed: " + e.Message); }
        }
    }
}
