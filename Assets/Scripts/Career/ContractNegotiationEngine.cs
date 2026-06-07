using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CONTRACT NEGOTIATION ENGINE – evaluates offers, generates counter-offers
    // ═══════════════════════════════════════════════════════════════════════
    public class ContractNegotiationEngine : MonoBehaviour
    {
        public enum NegotiationResult { Accepted, Countered, Rejected, Pending }

        [System.Serializable]
        public class NegotiationState
        {
            public ContractOffer originalOffer;
            public float playerCounterWage;
            public bool playerRequestsNumberOne;
            public int playerCounterSeasons;
            public NegotiationResult status = NegotiationResult.Pending;
        }

        // ── OFFER GENERATION ──────────────────────────────────────────────
        public static ContractOffer GenerateTeamOffer(
            string teamName,
            SeriesTier tier,
            DriverProfile player,
            int position,  // constructor position
            float teamBudget)
        {
            // Base salary scales with player rating and team budget
            float baseMultiplier = (player.attributes.OverallRating / 99f) * (teamBudget / 100_000_000f);
            float baseSalary = baseMultiplier * 15_000_000f;  // 0-15M depending on rating

            // Offer number 1 driver status if player is rated highly
            bool offersNumber1 = player.attributes.OverallRating >= 80 && position <= 5;

            // Number of seasons (good teams offer 2-3 yrs, backmarkers 1-2 yrs)
            int seasons = position <= 5 ? UnityEngine.Random.Range(2, 4) : UnityEngine.Random.Range(1, 3);

            return new ContractOffer
            {
                teamName = teamName,
                tier = tier,
                offeredSalary = baseSalary,
                podiumBonus = baseSalary * 0.15f,
                winBonus = baseSalary * 0.40f,
                seasons = seasons,
                offersNumberOne = offersNumber1,
                teamInterestScore = 50f + (player.attributes.OverallRating - 50) * 0.5f
            };
        }

        // ── EVALUATION LOGIC ──────────────────────────────────────────────
        public static NegotiationResult EvaluateOffer(
            ContractOffer offer,
            DriverProfile player,
            NegotiationState state)
        {
            float score = 0f;

            // ── Salary assessment ────────────────────────────────────────
            // Compare to player's current contract
            float currentWage = player.activeContract?.annualSalary ?? 0f;
            float salaryRatio = offer.offeredSalary / Mathf.Max(currentWage, offer.offeredSalary * 0.5f);

            if (salaryRatio >= 1.2f) score += 40f;      // 20%+ raise
            else if (salaryRatio >= 1.0f) score += 30f; // Match or slight raise
            else if (salaryRatio >= 0.8f) score += 10f; // Acceptable decrease
            else score -= 20f;                           // Too low

            // ── Team competitiveness ─────────────────────────────────────
            // Players prefer competitive teams
            float teamStrength = offer.teamInterestScore;
            score += Mathf.Clamp(teamStrength - 30f, 0f, 30f);

            // ── Number 1 driver status ───────────────────────────────────
            if (offer.offersNumberOne && player.attributes.OverallRating >= 75)
                score += 25f;  // Elite drivers want #1 status

            // ── Contract length ─────────────────────────────────────────
            // Younger players prefer longer deals for security
            int preferredLength = player.attributes.OverallRating >= 85 ? 1 : 2;  // Elites like flexibility
            if (offer.seasons == preferredLength) score += 15f;
            else if (Mathf.Abs(offer.seasons - preferredLength) == 1) score += 5f;

            // ── Series tier preference ───────────────────────────────────
            // Always want to move to F1 (or stay there)
            if (offer.tier == SeriesTier.Formula1)
                score += 20f;

            // ── Personality factors ──────────────────────────────────────
            // Player personality affects decision
            if (player.attributes.OverallRating < 60)
                score += 10f;  // Lower-rated drivers take first decent offer

            // ── Random element (real drivers aren't perfectly rational!) ───
            score += UnityEngine.Random.Range(-15f, 15f);

            // ── DECISION ─────────────────────────────────────────────────
            if (score >= 70f)
            {
                state.status = NegotiationResult.Accepted;
                return NegotiationResult.Accepted;
            }

            if (score >= 40f)
            {
                state.status = NegotiationResult.Countered;
                GeneratePlayerCounter(offer, state, player);
                return NegotiationResult.Countered;
            }

            state.status = NegotiationResult.Rejected;
            return NegotiationResult.Rejected;
        }

        static void GeneratePlayerCounter(
            ContractOffer offer,
            NegotiationState state,
            DriverProfile player)
        {
            // Player counters with higher wage
            float counterMultiplier = UnityEngine.Random.Range(1.1f, 1.25f);
            state.playerCounterWage = offer.offeredSalary * counterMultiplier;

            // Might request number 1 if not offered
            state.playerRequestsNumberOne = !offer.offersNumberOne && player.attributes.OverallRating >= 80;

            // Might request longer contract
            state.playerCounterSeasons = offer.seasons + UnityEngine.Random.Range(0, 2);
        }

        // ── FINAL RESOLUTION ──────────────────────────────────────────────
        public static bool ResolveFinalOffer(
            ContractOffer finalOffer,
            DriverProfile player)
        {
            // After negotiation rounds, player decides on final offer
            // If offer is within ~10% of expectations, accept it
            float acceptanceThreshold = 0.9f;

            bool acceptable = finalOffer.offeredSalary >= (player.activeContract?.annualSalary ?? 1_000_000f) * acceptanceThreshold;

            if (acceptable)
            {
                player.activeContract = new DriverContract
                {
                    teamName = finalOffer.teamName,
                    tier = finalOffer.tier,
                    annualSalary = finalOffer.offeredSalary,
                    podiumBonus = finalOffer.podiumBonus,
                    winBonus = finalOffer.winBonus,
                    durationSeasons = finalOffer.seasons,
                    seasonsRemaining = finalOffer.seasons,
                    hasNumberOneClause = finalOffer.offersNumberOne
                };

                player.currentTeam = finalOffer.teamName;
                player.contractHistory.Add(player.activeContract);

                return true;
            }

            return false;
        }

        // ── MARKET VALUE CALCULATION ─────────────────────────────────────
        public static float CalculateMarketValue(
            DriverProfile driver,
            int championshipPosition)
        {
            float baseValue = (driver.attributes.OverallRating / 99f) * 80_000_000f;

            // Championship position multiplier
            float positionMultiplier = championshipPosition switch
            {
                1 => 1.8f,      // Champion earns way more
                2 => 1.5f,
                3 => 1.3f,
                <= 6 => 1.1f,
                <= 10 => 1.0f,
                _ => 0.85f
            };

            return baseValue * positionMultiplier;
        }

        // ── DISPLAY ───────────────────────────────────────────────────────
        public static string FormatSalary(float salary)
        {
            if (salary >= 1_000_000)
                return $"${salary / 1_000_000:F1}M/year";
            return $"${salary / 1_000:F0}K/year";
        }

        public static string GetNegotiationStatusText(NegotiationResult status)
        {
            return status switch
            {
                NegotiationResult.Accepted => "✅ Contract accepted!",
                NegotiationResult.Countered => "🤝 Team is willing to negotiate further.",
                NegotiationResult.Rejected => "❌ Team has decided to pursue other drivers.",
                NegotiationResult.Pending => "⏳ Awaiting decision...",
                _ => "Unknown status"
            };
        }
    }
}
