using UnityEngine;
namespace RacingGame.Drivers
{
    /// <summary>
    /// Complete driver profile for AI and career mode
    /// </summary>
    [System.Serializable]
    public class DriverData
    {
        [Header("Identity")]
        public string fullName;
        public string shortName; // 3-letter code (e.g., "VER", "NOR")
        public int age;
        public string nationality;
        public string teamName;
        public int driverNumber;
        [Header("Skills")]
        public DriverRating rating = new DriverRating();
        [Header("Career Stats")]
        public int championshipPoints;
        public int wins;
        public int podiums;
        public int poles;
        public int fastestLaps;
        public int championships;
        [Header("Status")]
        public bool isActive = true;
        public bool isRookie;
        public int seasonsInF1;
        /// <summary>
        /// Get driver code (first 3 letters of last name)
        /// </summary>
        public string GetDriverCode()
        {
            if (!string.IsNullOrEmpty(shortName))
                return shortName.ToUpper();
            var parts = fullName.Split(' ');
            if (parts.Length >= 2)
                return parts[1].Substring(0, Mathf.Min(3, parts[1].Length)).ToUpper();
            return fullName.Substring(0, Mathf.Min(3, fullName.Length)).ToUpper();
        }
        /// <summary>
        /// Get overall rating
        /// </summary>
        public float GetOverallRating()
        {
            return rating.GetOverall();
        }
    }
}
