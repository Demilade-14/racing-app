using UnityEngine;
using RacingGame.Transfers;
namespace RacingGame.Development
{
    /// <summary>
    /// Handles team car development between seasons.
    /// Creates realistic performance cycles: top teams decline, midfield rises, breakthroughs happen.
    /// </summary>
    public static class TeamDevelopmentSystem
    {
        /// <summary>
        /// Process end-of-season development for all teams.
        /// Call this after the championship ends but before the new season starts.
        /// </summary>
        public static void ProcessSeasonDevelopment(int currentSeason)
        {
            UnityEngine.Debug.Log($"[Development] Processing Season {currentSeason} development...");
            foreach (var team in TransferTeamDatabase.Teams)
            {
                float oldPerf = team.carPerformance;
                // 1. Base growth (all teams improve slightly)
                ApplyBaseGrowth(team);
                // 2. Performance pressure (diminishing returns for top teams)
                ApplyPerformancePressure(team);
                // 3. Random breakthrough (8% chance of major improvement)
                ApplyRandomBreakthrough(team);
                // 4. Budget impact (richer teams develop more reliably)
                ApplyBudgetImpact(team);
                // 5. Clamp to realistic range
                ClampRatings(team);
                float newPerf = team.carPerformance;
                string trend = newPerf > oldPerf ? "↑" : newPerf < oldPerf ? "↓" : "→";
                UnityEngine.Debug.Log($"[Development] {team.teamName}: {oldPerf:F0} → {newPerf:F0} {trend}");
            }
        }
        /// <summary>
        /// All teams get slight base improvement (simulates natural development)
        /// </summary>
        static void ApplyBaseGrowth(TransferTeamData team)
        {
            float growth = Random.Range(0.2f, 1.2f);
            team.aeroRating += growth;
            team.engineRating += growth * 0.9f;
            team.chassisRating += growth * 0.8f;
        }
        /// <summary>
        /// Top teams suffer diminishing returns, midfield can improve faster
        /// </summary>
        static void ApplyPerformancePressure(TransferTeamData team)
        {
            float avgPerformance = (team.aeroRating + team.engineRating + team.chassisRating) / 3f;
            if (avgPerformance > 95f)
            {
                // Top teams: very small gains, risk of regression
                team.aeroRating += Random.Range(-0.3f, 0.4f);
                team.engineRating += Random.Range(-0.2f, 0.3f);
            }
            else if (avgPerformance > 90f)
            {
                // Upper midfield: moderate gains
                team.aeroRating += Random.Range(0.1f, 0.8f);
            }
            else if (avgPerformance < 80f)
            {
                // Backmarkers: can improve faster (catch-up effect)
                team.aeroRating += Random.Range(0.5f, 1.5f);
                team.engineRating += Random.Range(0.5f, 1.5f);
                team.chassisRating += Random.Range(0.4f, 1.2f);
            }
        }
        /// <summary>
        /// 8% chance of a "breakthrough season" (like Mercedes 2014, Red Bull 2021)
        /// </summary>
        static void ApplyRandomBreakthrough(TransferTeamData team)
        {
            if (Random.value < 0.08f)
            {
                float boost = Random.Range(2f, 5f);
                team.aeroRating += boost;
                team.engineRating += boost;
                team.chassisRating += boost;
                UnityEngine.Debug.Log($"[Development] 🚀 {team.teamName} had a BREAKTHROUGH season! (+{boost:F1})");
            }
        }
        /// <summary>
        /// Budget affects development reliability
        /// </summary>
        static void ApplyBudgetImpact(TransferTeamData team)
        {
            // Normalize budget (assume 500M is max, 200M is min)
            float budgetFactor = Mathf.InverseLerp(200_000_000f, 500_000_000f, team.budget);
            // Higher budget = more consistent development
            if (budgetFactor > 0.7f)
            {
                // Rich teams: smaller variance
                team.aeroRating += Random.Range(0f, 0.3f);
            }
            else if (budgetFactor < 0.4f)
            {
                // Poor teams: higher variance (can regress)
                team.aeroRating += Random.Range(-0.5f, 0.5f);
            }
        }
        /// <summary>
        /// Ensure ratings stay in realistic range (60-99)
        /// </summary>
        static void ClampRatings(TransferTeamData team)
        {
            team.aeroRating = Mathf.Clamp(team.aeroRating, 60f, 99f);
            team.engineRating = Mathf.Clamp(team.engineRating, 60f, 99f);
            team.chassisRating = Mathf.Clamp(team.chassisRating, 60f, 99f);
            // Recalculate overall car performance
            team.carPerformance = (team.aeroRating + team.engineRating + team.chassisRating) / 3f;
        }
        /// <summary>
        /// Get development report for UI display
        /// </summary>
        public static string GetDevelopmentReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine("  END OF SEASON DEVELOPMENT REPORT");
            report.AppendLine("═══════════════════════════════════════");
            foreach (var team in TransferTeamDatabase.Teams)
            {
                report.AppendLine($"{team.teamName,-25} Performance: {team.carPerformance:F0}");
            }
            return report.ToString();
        }
    }
}
