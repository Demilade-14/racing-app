using System;
using System.Collections.Generic;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// Preserves championship standings and race history
    /// </summary>
    [Serializable]
    public class ChampionshipSaveData
    {
        public Dictionary<string, int> driverPoints = new Dictionary<string, int>();
        public Dictionary<string, int> driverWins = new Dictionary<string, int>();
        public Dictionary<string, int> driverPodiums = new Dictionary<string, int>();
        public Dictionary<string, int> teamPoints = new Dictionary<string, int>();
        public Dictionary<string, int> teamWins = new Dictionary<string, int>();
        public int completedRaces;
        public int totalRacesInSeason;
        /// <summary>
        /// Get driver championship position (1-based)
        /// </summary>
        public int GetDriverPosition(string driverName)
        {
            if (!driverPoints.ContainsKey(driverName)) return -1;
            var sorted = new List<KeyValuePair<string, int>>(driverPoints);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].Key == driverName) return i + 1;
            }
            return -1;
        }
        /// <summary>
        /// Get championship leader
        /// </summary>
        public string GetLeader()
        {
            if (driverPoints.Count == 0) return null;
            string leader = null;
            int maxPoints = -1;
            foreach (var kvp in driverPoints)
            {
                if (kvp.Value > maxPoints)
                {
                    maxPoints = kvp.Value;
                    leader = kvp.Key;
                }
            }
            return leader;
        }
    }
}
