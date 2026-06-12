using UnityEngine;
namespace RacingGame.Race.Strategy
{
    /// <summary>
    /// Evaluates real-time telemetry to decide if a driver should pit THIS lap.
    /// Integrates with the TireDegradationSystem and RaceSimulationEngine.
    /// </summary>
    public static class PitDecisionSystem
    {
        /// <summary>
        /// Returns true if the driver should enter the pits this lap.
        /// </summary>
        /// <param name="tireWear">Current tire wear (0-100)</param>
        /// <param name="gapToCarAhead">Seconds to the car ahead</param>
        /// <param name="gapToCarBehind">Seconds to the car behind</param>
        /// <param name="strategy">The team's chosen strategy</param>
        /// <param name="safetyCar">Is SC/VSC active?</param>
        public static bool ShouldPit(
            float tireWear, 
            float gapToCarAhead, 
            float gapToCarBehind, 
            StrategyType strategy, 
            bool safetyCar)
        {
            // 1. Emergency: Safety Car is out. Pit now to get "free" pit stop time.
            if (safetyCar) return true;
            // 2. Critical: Tires are completely dead (cliff). Must pit to avoid crash/loss.
            if (tireWear > 85f) return true;
            // 3. Strategy-specific triggers
            switch (strategy)
            {
                case StrategyType.AggressiveUndercut:
                    // Pit if tires are getting bad, OR if we are close enough to use fresh tires to jump them.
                    return tireWear > 60f || gapToCarAhead < 2.5f;
                case StrategyType.Balanced:
                    // Standard tire threshold
                    return tireWear > 70f;
                case StrategyType.ConservativeOvercut:
                    // Stay out as long as possible, unless tires are critical or we are about to be lapped.
                    return tireWear > 78f;
                case StrategyType.DefensiveTrackPosition:
                    // Stay out to block, only pit if tires are completely gone or rival is right behind.
                    return tireWear > 80f || gapToCarBehind < 1.0f;
                case StrategyType.SafetyCarGamble:
                    return true;
                default:
                    return false;
            }
        }
    }
}
