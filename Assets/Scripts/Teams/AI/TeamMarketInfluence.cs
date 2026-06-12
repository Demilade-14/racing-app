using UnityEngine;
using RacingGame.Drivers;
namespace RacingGame.Teams.AI
{
    /// <summary>
    /// Calculates how team personalities affect driver demand and market values.
    /// </summary>
    public static class TeamMarketInfluence
    {
        /// <summary>
        /// Get a multiplier for how much a team values a driver based on their personality.
        /// </summary>
        public static float GetDriverDemandMultiplier(DriverData driver, TeamPersonalityType personality)
        {
            if (driver == null || driver.rating == null) return 1f;
            float baseValue = driver.GetOverallRating();
            switch (personality)
            {
                case TeamPersonalityType.Ruthless:
                    // Ruthless teams pay premium for top talent, but undervalue consistency
                    float paceWeight = driver.rating.pace * 0.6f + driver.rating.consistency * 0.4f;
                    return paceWeight / baseValue * 1.2f;
                case TeamPersonalityType.Aggressive:
                    // Aggressive teams value young talent and high potential
                    float potentialFactor = driver.age < 25 ? 1.15f : 1.0f;
                    return (baseValue / 100f) * 1.1f * potentialFactor;
                case TeamPersonalityType.Balanced:
                    // Balanced teams value overall rating fairly
                    return baseValue / 100f;
                case TeamPersonalityType.Conservative:
                    // Conservative teams undervalue expensive talent, prefer loyalty
                    float experienceBonus = driver.seasonsInF1 * 0.02f;
                    return (baseValue / 100f) * 0.9f + experienceBonus;
                default:
                    return baseValue / 100f;
            }
        }
        /// <summary>
        /// Calculate the salary a team is willing to offer based on their personality.
        /// </summary>
        public static float CalculateTeamSalaryOffer(DriverData driver, TeamPersonalityType personality, float teamBudget)
        {
            if (driver == null) return 0f;
            float baseSalary = driver.GetOverallRating() * 100_000f; // 1M per OVR point
            float multiplier = GetDriverDemandMultiplier(driver, personality);
            float offer = baseSalary * multiplier;
            // Cap at 40% of team budget (realistic constraint)
            float maxOffer = teamBudget * 0.4f;
            offer = Mathf.Min(offer, maxOffer);
            return offer;
        }
        /// <summary>
        /// Check if a team would be interested in signing a driver.
        /// </summary>
        public static bool WouldTeamSignDriver(DriverData driver, TeamPersonalityType personality, float teamPerformance)
        {
            if (driver == null) return false;
            float driverValue = driver.GetOverallRating();
            switch (personality)
            {
                case TeamPersonalityType.Ruthless:
                    // Only sign if driver is significantly better than current average
                    return driverValue > teamPerformance - 5f;
                case TeamPersonalityType.Aggressive:
                    // Willing to take risks on young talent
                    if (driver.age < 23 && driverValue > 75f) return true;
                    return driverValue > teamPerformance - 10f;
                case TeamPersonalityType.Balanced:
                    return driverValue > teamPerformance - 8f;
                case TeamPersonalityType.Conservative:
                    // Prefer experienced drivers, slow to sign new talent
                    return driverValue > teamPerformance - 5f && driver.seasonsInF1 >= 2;
                default:
                    return driverValue > teamPerformance - 10f;
            }
        }
        /// <summary>
        /// Get a description of the team's transfer strategy for UI display.
        /// </summary>
        public static string GetTransferStrategyDescription(TeamPersonalityType personality)
        {
            return personality switch
            {
                TeamPersonalityType.Conservative => "Values loyalty and experience. Slow to make changes.",
                TeamPersonalityType.Balanced => "Balanced approach. Mix of experience and talent.",
                TeamPersonalityType.Aggressive => "Takes risks. Targets young talent and high-potential drivers.",
                TeamPersonalityType.Ruthless => "Results-driven. Quick to drop underperformers.",
                _ => "Unknown strategy"
            };
        }
    }
}
