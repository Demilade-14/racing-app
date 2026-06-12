using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Data; // For TeamData2026
namespace RacingGame.Career
{
    /// <summary>
    /// Simulates the F1 driver market. 
    /// Evaluates driver performance, generates contract offers for the player,
    /// and simulates AI teams signing AI drivers during the off-season.
    /// </summary>
    public static class DriverMarketAI
    {
        /// <summary>
        /// Evaluate how interested a team is in a specific driver.
        /// Returns an InterestLevel and an estimated salary range.
        /// </summary>
        public static TeamInterestEntry EvaluateDriver(DriverProfile driver, TeamData2026 team)
        {
            if (driver == null || team == null) return null;
            // Calculate driver "value" based on stats and recent performance
            float driverValue = CalculateDriverValue(driver);
            // Calculate team "requirement" based on their performance rating
            float teamRequirement = team.performanceRating;
            // Interest score: How well does the driver match the team's needs?
            float interestScore = 100f - Mathf.Abs(driverValue - teamRequirement);
            // Reputation bonus/penalty
            interestScore += (driver.reputation - 50f) * 0.5f;
            // Determine interest level
            InterestLevel interest = interestScore switch
            {
                > 80f  => InterestLevel.Hot,
                > 60f  => InterestLevel.Warm,
                > 40f  => InterestLevel.Cold,
                _      => InterestLevel.None
            };
            // Calculate estimated salary based on team budget and driver value
            float baseSalary = team.budgetAllocationPerSeat;
            float salaryMultiplier = driverValue / 100f;
            float estimatedSalary = baseSalary * salaryMultiplier;
            return new TeamInterestEntry
            {
                teamName = team.teamName,
                interest = interest,
                estimatedSalaryMin = estimatedSalary * 0.9f,
                estimatedSalaryMax = estimatedSalary * 1.1f,
                seatAvailable = true, // Simplified: assume seat is open
                availableRole = driverValue > teamRequirement ? "No.1 Driver" : "No.2 Driver",
                performanceRating = team.performanceRating
            };
        }
        /// <summary>
        /// Generate a list of contract offers for the player from interested teams.
        /// </summary>
        public static List<ContractOffer> GeneratePlayerOffers(DriverProfile player, List<TeamData2026> allTeams)
        {
            var offers = new List<ContractOffer>();
            foreach (var team in allTeams)
            {
                // Skip player's current team (they renew separately)
                if (team.teamName == player.currentTeam) continue;
                var interestEntry = EvaluateDriver(player, team);
                // Only generate offers if interest is Warm or Hot
                if (interestEntry.interest == InterestLevel.Warm || interestEntry.interest == InterestLevel.Hot)
                {
                    float offeredSalary = Random.Range(interestEntry.estimatedSalaryMin, interestEntry.estimatedSalaryMax);
                    offers.Add(new ContractOffer
                    {
                        teamName = team.teamName,
                        tier = SeriesTier.Formula1, // Simplified
                        offeredSalary = offeredSalary,
                        podiumBonus = offeredSalary * 0.1f,
                        winBonus = offeredSalary * 0.2f,
                        seasons = Random.Range(1, 4),
                        offersNumberOne = interestEntry.availableRole == "No.1 Driver",
                        teamInterestScore = interestEntry.interest == InterestLevel.Hot ? 90f : 70f,
                        minReputationRequired = 40f, // Simplified
                        interestLevel = interestEntry.interest,
                        teamBudgetAllocation = team.budgetAllocationPerSeat
                    });
                }
            }
            // Sort by salary (highest first)
            return offers.OrderByDescending(o => o.offeredSalary).ToList();
        }
        /// <summary>
        /// Simulate the off-season: AI teams sign AI drivers.
        /// This updates the grid for the next season.
        /// </summary>
        public static void SimulateOffSeason(List<TeamData2026> teams, List<DriverProfile> allDrivers)
        {
            // Simplified simulation: 
            // In a full game, this would run a complex algorithm to match drivers to teams.
            // For now, we just log that the simulation happened.
            Debug.Log($"[DriverMarketAI] Simulating off-season for {teams.Count} teams and {allDrivers.Count} drivers.");
            // TODO: Implement full matching algorithm
            // 1. Rank all drivers by value
            // 2. Rank all teams by performance
            // 3. Match top drivers to top teams
            // 4. Update team rosters
        }
        /// <summary>
        /// Calculate a driver's overall "market value" (0-100).
        /// </summary>
        private static float CalculateDriverValue(DriverProfile driver)
        {
            // Weighted average of stats
            float statValue = (driver.pace * 0.3f) + 
                              (driver.racecraft * 0.3f) + 
                              (driver.awareness * 0.2f) + 
                              (driver.experience * 0.2f);
            // Performance bonus (recent wins/podiums)
            float performanceBonus = (driver.seasonWins * 5f) + (driver.seasonPodiums * 2f);
            // Reputation factor
            float reputationFactor = driver.reputation;
            // Combine (normalized to 0-100)
            float totalValue = (statValue * 0.5f) + (performanceBonus * 0.3f) + (reputationFactor * 0.2f);
            return Mathf.Clamp(totalValue, 0f, 100f);
        }
    }
}
