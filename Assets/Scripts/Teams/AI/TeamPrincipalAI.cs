using UnityEngine;
using System.Collections.Generic;
using RacingGame.Drivers;
using RacingGame.Transfers;
using RacingGame.Career;
namespace RacingGame.Teams.AI
{
    /// <summary>
    /// Represents a single team principal's AI decision-making system.
    /// Handles driver evaluations, contract decisions, and personality-driven behavior.
    /// </summary>
    [System.Serializable]
    public class TeamPrincipalAI
    {
        [Header("Identity")]
        public string teamName;
        public TeamPersonalityType personality = TeamPersonalityType.Balanced;
        [Header("Drivers")]
        public string driver1Name;
        public string driver2Name;
        [Header("Financial")]
        public float teamBudget;
        public float performanceExpectation = 90f;
        [Header("Internal Politics")]
        public float favoritismBias = 0.15f;  // 0 = fair, 1 = heavily favors #1 driver
        public string favoredDriver;          // The "number 1" driver in the team
        [Header("Season State")]
        public float currentSeasonPerformance;
        public bool isUnderPressure;
        public int consecutiveBadResults;
        // ── DECISION LOG ────────────────────────────────────────────────────
        private List<string> _decisionLog = new List<string>();
        public IReadOnlyList<string> DecisionLog => _decisionLog;
        /// <summary>
        /// Evaluate the team's season performance and adjust personality accordingly.
        /// </summary>
        public void EvaluateSeasonPerformance(float currentPerformance)
        {
            currentSeasonPerformance = currentPerformance;
            // Underperformance triggers personality shift
            if (currentPerformance < performanceExpectation - 5f)
            {
                isUnderPressure = true;
                consecutiveBadResults++;
                // Teams under pressure become more aggressive/ruthless
                if (consecutiveBadResults >= 3)
                {
                    if (personality == TeamPersonalityType.Conservative)
                        personality = TeamPersonalityType.Balanced;
                    else if (personality == TeamPersonalityType.Balanced)
                        personality = TeamPersonalityType.Aggressive;
                    else if (personality == TeamPersonalityType.Aggressive)
                        personality = TeamPersonalityType.Ruthless;
                    LogDecision($"⚠️ Team under pressure! Personality shifted to {personality}");
                }
            }
            else if (currentPerformance > performanceExpectation + 5f)
            {
                // Success makes teams more conservative (protect the lead)
                isUnderPressure = false;
                consecutiveBadResults = 0;
                if (personality == TeamPersonalityType.Ruthless)
                    personality = TeamPersonalityType.Aggressive;
                else if (personality == TeamPersonalityType.Aggressive)
                    personality = TeamPersonalityType.Balanced;
                LogDecision($"✅ Strong season! Personality shifted to {personality}");
            }
        }
        /// <summary>
        /// Decide what action to take regarding the team's drivers.
        /// Returns an action string: "No action", "Replace X", "Extend contract", etc.
        /// </summary>
        public string DecideDriverAction(DriverData d1, DriverData d2, PlayerCareerContext player)
        {
            if (d1 == null || d2 == null) return "No action (missing driver data)";
            float d1Score = EvaluateDriver(d1);
            float d2Score = EvaluateDriver(d2);
            // Apply favoritism bias
            if (d1.fullName == favoredDriver) d1Score += favoritismBias * 10f;
            if (d2.fullName == favoredDriver) d2Score += favoritismBias * 10f;
            // ── RUTHLESS PERSONALITY ────────────────────────────────────────
            if (personality == TeamPersonalityType.Ruthless)
            {
                if (d1Score < d2Score - 5f)
                {
                    LogDecision($"🔪 Ruthless: Dropping {d1.fullName} (score {d1Score:F0} vs {d2Score:F0})");
                    return $"Replace {d1.fullName}";
                }
                if (d2Score < d1Score - 5f)
                {
                    LogDecision($"🔪 Ruthless: Dropping {d2.fullName} (score {d2Score:F0} vs {d1Score:F0})");
                    return $"Replace {d2.fullName}";
                }
            }
            // ── AGGRESSIVE PERSONALITY ──────────────────────────────────────
            if (personality == TeamPersonalityType.Aggressive)
            {
                // 30% chance to consider a swap even if both drivers are decent
                if (Random.value < 0.3f)
                {
                    LogDecision($"🎲 Aggressive: Considering driver swap for fresh talent");
                    return "Consider driver swap next season";
                }
            }
            // ── PLAYER INTERACTION ──────────────────────────────────────────
            if (player != null && player.playerDriverName != null)
            {
                bool isPlayerD1 = d1.fullName == player.playerDriverName;
                bool isPlayerD2 = d2.fullName == player.playerDriverName;
                if (isPlayerD1 || isPlayerD2)
                {
                    float playerScore = isPlayerD1 ? d1Score : d2Score;
                    float teammateScore = isPlayerD1 ? d2Score : d1Score;
                    // Player is title contender → team supports them
                    if (player.isTitleContender)
                    {
                        LogDecision($"🏆 Title contender detected! Prioritizing {player.playerDriverName}");
                        favoredDriver = player.playerDriverName;
                        return $"Support {player.playerDriverName} as #1 driver";
                    }
                    // Player underperforming vs teammate → team may drop them
                    if (playerScore < teammateScore - 10f && personality != TeamPersonalityType.Conservative)
                    {
                        LogDecision($"⚠️ Player underperforming teammate! Team may look for replacement");
                        return $"Consider replacing {player.playerDriverName}";
                    }
                    // Player dominating → early contract extension
                    if (playerScore > 90f && player.isTitleContender)
                    {
                        LogDecision($"🤝 Player dominating! Offering early contract extension");
                        return $"Extend contract with {player.playerDriverName}";
                    }
                }
            }
            // ── CONSERVATIVE PERSONALITY ────────────────────────────────────
            if (personality == TeamPersonalityType.Conservative)
            {
                // Only replace if driver is truly terrible
                if (d1Score < 60f)
                {
                    LogDecision($"🛡️ Conservative: {d1.fullName} is underperforming severely");
                    return $"Replace {d1.fullName}";
                }
                if (d2Score < 60f)
                {
                    LogDecision($"🛡️ Conservative: {d2.fullName} is underperforming severely");
                    return $"Replace {d2.fullName}";
                }
            }
            return "No action";
        }
        /// <summary>
        /// Evaluate a driver's overall value to the team.
        /// </summary>
        public float EvaluateDriver(DriverData driver)
        {
            if (driver == null || driver.rating == null) return 0f;
            return (driver.rating.pace * 0.40f) +
                   (driver.rating.consistency * 0.20f) +
                   (driver.rating.overtaking * 0.15f) +
                   (driver.rating.defending * 0.15f) +
                   (driver.rating.tireManagement * 0.10f);
        }
        /// <summary>
        /// Make a contract decision based on team personality and driver value.
        /// </summary>
        public void MakeContractDecision(DriverData driver, float marketValue)
        {
            if (driver == null) return;
            float driverValue = EvaluateDriver(driver);
            // ── RUTHLESS: Drop underperformers ──────────────────────────────
            if (driverValue < 75f && personality == TeamPersonalityType.Ruthless)
            {
                LogDecision($"🔪 Dropping underperforming driver: {driver.fullName} (value {driverValue:F0})");
                return;
            }
            // ── CONSERVATIVE: Reject expensive contracts ────────────────────
            if (teamBudget < marketValue && personality == TeamPersonalityType.Conservative)
            {
                LogDecision($"💰 Rejecting expensive contract for {driver.fullName} (budget {teamBudget:C0} < market {marketValue:C0})");
                return;
            }
            // ── AGGRESSIVE/BALANCED: Extend top talent early ────────────────
            if (driverValue > 90f && personality != TeamPersonalityType.Conservative)
            {
                LogDecision($"🤝 Extending contract early for star driver: {driver.fullName}");
                return;
            }
            // ── DEFAULT: Standard negotiation ───────────────────────────────
            LogDecision($"📋 Standard contract negotiation for {driver.fullName}");
        }
        /// <summary>
        /// Log a decision for UI/debugging display.
        /// </summary>
        void LogDecision(string decision)
        {
            _decisionLog.Add(decision);
            Debug.Log($"🏁 [{teamName}] {decision}");
            // Keep log manageable
            if (_decisionLog.Count > 50)
                _decisionLog.RemoveAt(0);
        }
        /// <summary>
        /// Clear the decision log (call at start of new season).
        /// </summary>
        public void ClearLog()
        {
            _decisionLog.Clear();
        }
    }
}
