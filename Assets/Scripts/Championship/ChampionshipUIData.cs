using System.Collections.Generic;
namespace RacingGame.Championship
{
    /// <summary>
    /// Data structure for championship UI display
    /// </summary>
    [System.Serializable]
    public class ChampionshipUIData
    {
        public string leaderName;
        public string leaderTeam;
        public int leaderPoints;
        public int currentRound;
        public int totalRounds;
        public int currentSeason;
        public List<string> topFiveDrivers = new List<string>();
        public List<int> topFivePoints = new List<int>();
        public List<string> topThreeTeams = new List<string>();
        public List<int> topThreeTeamPoints = new List<int>();
        /// <summary>
        /// Populate from championship system
        /// </summary>
        public void PopulateFromChampionship(ChampionshipSystem championship, SeasonCalendar calendar)
        {
            currentRound = championship.currentRound - 1;
            totalRounds = calendar.totalRounds;
            currentSeason = championship.currentSeason;
            // Leader
            var leader = championship.GetLeader();
            if (leader != null)
            {
                leaderName = leader.driver.fullName;
                leaderTeam = leader.driver.teamName;
                leaderPoints = leader.points;
            }
            // Top 5 drivers
            topFiveDrivers.Clear();
            topFivePoints.Clear();
            for (int i = 0; i < 5 && i < championship.driverStandings.Count; i++)
            {
                var standing = championship.driverStandings[i];
                topFiveDrivers.Add($"{standing.position}. {standing.driver.shortName}");
                topFivePoints.Add(standing.points);
            }
            // Top 3 teams
            topThreeTeams.Clear();
            topThreeTeamPoints.Clear();
            for (int i = 0; i < 3 && i < championship.constructorStandings.Count; i++)
            {
                var standing = championship.constructorStandings[i];
                topThreeTeams.Add(standing.teamName);
                topThreeTeamPoints.Add(standing.points);
            }
        }
        /// <summary>
        /// Get progress percentage
        /// </summary>
        public float GetProgressPercent()
        {
            return totalRounds > 0 ? (float)currentRound / totalRounds * 100f : 0f;
        }
    }
}
