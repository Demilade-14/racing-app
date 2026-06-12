using UnityEngine;
namespace RacingGame.Core
{
    /// <summary>
    /// Hardcoded 2026 F1 Regulations and Physics Constants.
    /// Centralizing these makes balancing the game much easier later.
    /// </summary>
    public static class GameConstants
    {
        // ── 2026 ERS Regulations ────────────────────────────────────────────
        public const float ERS_BATTERY_CAPACITY_MJ = 8.5f;       // Max battery size
        public const float ERS_MOTOR_POWER_KW = 350f;            // Max electric deployment
        public const float ERS_OVERTAKE_BOOST_KW = 200f;         // Extra power for overtake mode
        public const float ERS_OVERTAKE_DURATION_SEC = 5f;       // How long overtake lasts
        public const float ERS_OVERTAKE_COOLDOWN_SEC = 20f;      // Cooldown between uses
        public const float ERS_MIN_BATTERY_FOR_OVERTAKE = 0.30f; // 30% minimum to activate
        // ─ Tire Physics (Approximate 2026 Pirelli Data) ────────────────────
        public const float TIRE_OPTIMAL_TEMP_SOFT = 100f;        // Celsius
        public const float TIRE_OPTIMAL_TEMP_MEDIUM = 90f;
        public const float TIRE_OPTIMAL_TEMP_HARD = 80f;
        public const float TIRE_DEGRADATION_SOFT = 1.5f;         // Wear rate per lap
        public const float TIRE_DEGRADATION_MEDIUM = 1.0f;
        public const float TIRE_DEGRADATION_HARD = 0.7f;
        // ── Active Aero (2026 New Feature) ──────────────────────────────────
        public const float AERO_DRAG_REDUCTION_PERCENT = 23f;    // DRS effect
        public const float AERO_DEPLOY_SPEED_THRESHOLD = 280f;   // km/h required to open
        // ── Mobile Performance Limits ───────────────────────────────────────
        public const int MAX_AI_CARS = 19;                       // Keep low for mobile
        public const float PHYSICS_FIXED_TIMESTEP = 0.02f;       // 50Hz physics
    }
}
