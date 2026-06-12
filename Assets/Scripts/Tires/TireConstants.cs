using UnityEngine;
namespace RacingGame.Tires
{
    /// <summary>
    /// 2026 Pirelli tire data - all compounds, wear rates, temperature windows.
    /// Centralized for easy balancing.
    /// </summary>
    public static class TireConstants
    {
        // ── Optimal Temperature Windows (Celsius) ─────────────────────────
        public const float SOFT_OPTIMAL_MIN = 90f;
        public const float SOFT_OPTIMAL_MAX = 110f;
        public const float SOFT_OPTIMAL_CENTER = 100f;
        public const float MEDIUM_OPTIMAL_MIN = 80f;
        public const float MEDIUM_OPTIMAL_MAX = 100f;
        public const float MEDIUM_OPTIMAL_CENTER = 90f;
        public const float HARD_OPTIMAL_MIN = 70f;
        public const float HARD_OPTIMAL_MAX = 90f;
        public const float HARD_OPTIMAL_CENTER = 80f;
        public const float INTER_OPTIMAL_MIN = 50f;
        public const float INTER_OPTIMAL_MAX = 70f;
        public const float INTER_OPTIMAL_CENTER = 60f;
        public const float WET_OPTIMAL_MIN = 40f;
        public const float WET_OPTIMAL_MAX = 60f;
        public const float WET_OPTIMAL_CENTER = 50f;
        // ─ Wear Rates (percent per lap at optimal temp) ──────────────────
        public const float SOFT_WEAR_RATE = 1.8f;      // Degrades fastest
        public const float MEDIUM_WEAR_RATE = 1.2f;
        public const float HARD_WEAR_RATE = 0.8f;      // Most durable
        public const float INTER_WEAR_RATE = 1.5f;     // Wet conditions
        public const float WET_WEAR_RATE = 1.0f;
        // ── Temperature Change Rates ──────────────────────────────────────
        public const float TEMP_GAIN_PER_LAP = 8f;     // Celsius per lap under load
        public const float TEMP_COOL_PER_LAP = 3f;     // Celsius per lap when cool
        public const float TEMP_AMBIENT = 25f;         // Default ambient
        // ── Grip Multipliers (at optimal temp, 100% life) ─────────────────
        public const float SOFT_GRIP = 1.15f;          // 15% faster than baseline
        public const float MEDIUM_GRIP = 1.0f;         // Baseline
        public const float HARD_GRIP = 0.92f;          // 8% slower but durable
        public const float INTER_GRIP = 0.85f;         // Wet grip
        public const float WET_GRIP = 0.75f;           // Full wet
        // ── Wear Thresholds ───────────────────────────────────────────────
        public const float CLIFF_THRESHOLD = 20f;      // Below 20% = cliff (massive deg)
        public const float CRITICAL_THRESHOLD = 10f;   // Below 10% = critical (blowout risk)
        // ── Mobile Performance ────────────────────────────────────────────
        public const int MAX_TIRE_SIMULATIONS = 80;    // 20 cars × 4 tires
    }
}
