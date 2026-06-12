using UnityEngine;
namespace RacingGame.Career
{
    /// <summary>
    /// Handles the logic when a player tries to negotiate a contract offer.
    /// Determines if the team accepts, counters, or rejects the player's terms.
    /// </summary>
    public static class ContractNegotiationEngine
    {
        /// <summary>
        /// Process a player's counter-offer.
        /// </summary>
        /// <param name="offer">The original offer from the team.</param>
        /// <param name="playerCounterSalary">The salary the player is asking for.</param>
        /// <param name="requestNumberOne">Whether the player is demanding the No.1 driver role.</param>
        /// <returns>The result of the negotiation.</returns>
        public static NegotiationResult Negotiate(
            ContractOffer offer, 
            float playerCounterSalary, 
            bool requestNumberOne)
        {
            if (offer == null) return NegotiationResult.Reject;
            // 1. Check if player's demands are within team's budget
            float maxBudget = offer.teamBudgetAllocation * 1.2f; // Teams can stretch budget by 20% for top talent
            if (playerCounterSalary > maxBudget)
            {
                // Way over budget - automatic reject
                Debug.Log($"[Negotiation] {offer.teamName} rejected: Salary {playerCounterSalary} exceeds max budget {maxBudget}.");
                return NegotiationResult.Reject;
            }
            // 2. Calculate team's willingness to accept
            float acceptanceThreshold = offer.teamInterestScore; // Higher interest = more willing to negotiate
            // Salary factor: How much higher is the player asking vs the original offer?
            float salaryIncreasePercent = (playerCounterSalary - offer.offeredSalary) / offer.offeredSalary;
            // Number 1 driver factor: Teams are less willing to give this up
            float no1Penalty = requestNumberOne && !offer.offersNumberOne ? 20f : 0f;
            // Final acceptance score (0-100)
            float acceptanceScore = acceptanceThreshold - (salaryIncreasePercent * 100f) - no1Penalty;
            // 3. Determine result
            if (acceptanceScore > 70f)
            {
                // Team accepts the player's terms
                offer.offeredSalary = playerCounterSalary;
                offer.offersNumberOne = requestNumberOne;
                Debug.Log($"[Negotiation] {offer.teamName} accepted your terms!");
                return NegotiationResult.Accept;
            }
            else if (acceptanceScore > 40f)
            {
                // Team makes a counter-offer (meet in the middle)
                float counterSalary = (offer.offeredSalary + playerCounterSalary) / 2f;
                offer.playerCounterSalary = counterSalary; // Store the counter
                offer.offersNumberOne = requestNumberOne && Random.value > 0.5f; // 50/50 chance on No.1 role
                Debug.Log($"[Negotiation] {offer.teamName} countered with {counterSalary}.");
                return NegotiationResult.CounterOffer;
            }
            else
            {
                // Team rejects
                Debug.Log($"[Negotiation] {offer.teamName} rejected your counter-offer.");
                return NegotiationResult.Reject;
            }
        }
    }
}
