using System.Collections.Generic;
using System;
namespace RacingGame.Transfers
{
    /// <summary>
    /// Team data specifically for the Transfer/Regen system.
    /// Renamed to TransferTeamData to avoid conflict with RacingGame.Career.TeamData2026.
    /// </summary>
    [Serializable]
    public class TransferTeamData
    {
        public string teamName;
        public float carPerformance;
        public float aeroRating;
        public float engineRating;
        public float chassisRating;
        public float budget;
        public int maxDrivers = 2;
        public List<string> drivers = new List<string>();
        public bool HasSeatAvailable()
        {
            return drivers.Count < maxDrivers;
        }
        public void AddDriver(string driverName)
        {
            if (!drivers.Contains(driverName) && HasSeatAvailable())
            {
                drivers.Add(driverName);
                UnityEngine.Debug.Log($"[Transfer] {driverName} signed for {teamName}");
            }
        }
        public void RemoveDriver(string driverName)
        {
            if (drivers.Contains(driverName))
            {
                drivers.Remove(driverName);
                UnityEngine.Debug.Log($"[Transfer] {driverName} left {teamName}");
            }
        }
    }
}
