using UnityEngine;
using RacingGame.Transfers;
namespace RacingGame.Development
{
    /// <summary>
    /// Manages the complete end-of-season evolution process.
    /// Coordinates team development, driver market, and championship reset.
    /// </summary>
    public class SeasonEvolutionManager : MonoBehaviour
    {
        public static SeasonEvolutionManager Instance { get; private set; }
        [Header("Events")]
        public System.Action<int> OnSeasonDevelopmentComplete;
        public System.Action<int> OnNewSeasonStarted;
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        /// <summary>
        /// Process the complete end-of-season transition.
        /// Call this after the final race of the season.
        /// </summary>
        public void ProcessEndOfSeason(int currentSeason)
        {
            Debug.Log($"[SeasonEvolution] ═══════════════════════════════════════");
            Debug.Log($"[SeasonEvolution] Processing End of Season {currentSeason}");
            Debug.Log($"[SeasonEvolution] ═══════════════════════════════════════");
            // 1. Team car development
            TeamDevelopmentSystem.ProcessSeasonDevelopment(currentSeason);
            // 2. Driver market (retirements + regen)
            var marketManager = FindObjectOfType<RacingGame.Transfers.DriverMarketManager>();
            if (marketManager != null)
            {
                marketManager.currentSeason = currentSeason;
                marketManager.ProcessEndOfSeason();
            }
            OnSeasonDevelopmentComplete?.Invoke(currentSeason);
            // 3. Start new season
            int newSeason = currentSeason + 1;
            Debug.Log($"[SeasonEvolution] Starting Season {newSeason}...");
            OnNewSeasonStarted?.Invoke(newSeason);
        }
        /// <summary>
        /// Preview how teams will develop (for testing)
        /// </summary>
        public void PreviewDevelopment()
        {
            Debug.Log(TeamDevelopmentSystem.GetDevelopmentReport());
        }
    }
}
