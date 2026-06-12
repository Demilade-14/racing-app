namespace RacingGame.Teams.AI
{
    /// <summary>
    /// Defines the strategic personality of a team principal.
    /// Affects contract decisions, driver swaps, and risk tolerance.
    /// </summary>
    public enum TeamPersonalityType
    {
        /// <summary>Stable, slow to change, values loyalty over raw talent</summary>
        Conservative,
        /// <summary>Realistic F1 team behavior - balanced approach</summary>
        Balanced,
        /// <summary>Takes risks, signs young talents, aggressive negotiations</summary>
        Aggressive,
        /// <summary>Ruthless - fires underperformers immediately, pure results-driven</summary>
        Ruthless
    }
}
