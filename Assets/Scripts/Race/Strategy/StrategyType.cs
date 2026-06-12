namespace RacingGame.Race.Strategy
{
    /// <summary>
    /// Defines the strategic approach a team will take during a race.
    /// </summary>
    public enum StrategyType
    {
        AggressiveUndercut,     // Pit early to gain track position on fresh tires
        Balanced,               // Standard pit window
        ConservativeOvercut,    // Stay out longer to benefit from clear air/track position
        DefensiveTrackPosition, // Pit as late as possible to hold position (often used by leaders)
        SafetyCarGamble         // Desperation pit when SC is out to minimize time loss
    }
}
