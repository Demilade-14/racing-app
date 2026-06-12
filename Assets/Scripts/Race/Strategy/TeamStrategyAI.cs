using UnityEngine;
using RacingGame.Drivers;
namespace RacingGame.Race.Strategy
{
    /// <summary>
    /// Determines the overall race strategy for a driver based on race context.
    /// </summary>
    public static class TeamStrategyAI
    {
        /// <summary>
        /// Decides the strategy for the current race phase.
        /// </summary>
        /// <param name="teamPerformanceGap">Difference in car performance vs rival (positive = faster)</param>
        /// <param name="racePosition">Current track position (1 = P1)</param>
        /// <param name="lapsRemaining">How many laps are left</param>
        /// <param name="safetyCarActive">Is SC or VSC currently deployed?</param>
        public static StrategyType DecideStrategy(
            float teamPerformanceGap, 
            int racePosition, 
            int lapsRemaining, 
            bool safetyCarActive)
        {
            // 1. Safety Car Reaction: Everyone pits to minimize time loss
            if (safetyCarActive)
                return StrategyType.SafetyCarGamble;
            // 2. Late Race Defense: If leading in the final stages, protect position
            if (racePosition <= 3 && lapsRemaining < 10)
                return StrategyType.DefensiveTrackPosition;
            // 3. Performance Gap Logic
            if (teamPerformanceGap > 0.5f)
            {
                // We are significantly faster. Use the undercut to jump the rival.
                return StrategyType.AggressiveUndercut;
            }
            if (teamPerformanceGap < -0.3f)
            {
                // We are slower. Stay out, hope for clear air, and try an overcut.
                return StrategyType.ConservativeOvercut;
            }
            // 4. Default: Matched pace, standard strategy
            return StrategyType.Balanced;
        }
        /// <summary>
        /// Calculates the ideal pit window (as a percentage of race progress) based on strategy.
        /// </summary>
        public static float GetPitWindowProgress(StrategyType strategy)
        {
            return strategy switch
            {
                StrategyType.AggressiveUndercut     => 0.40f, // Pit around 40% of race
                StrategyType.Balanced               => 0.50f, // Pit around 50%
                StrategyType.ConservativeOvercut    => 0.65f, // Pit around 65%
                StrategyType.DefensiveTrackPosition => 0.75f, // Pit around 75%
                StrategyType.SafetyCarGamble        => 0.00f, // Pit immediately
                _                                   => 0.50f
            };
        }
    }
}
