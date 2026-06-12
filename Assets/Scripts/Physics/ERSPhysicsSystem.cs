using UnityEngine;
using RacingGame.Race; // References ERSState2026 and DeploymentMode from your RaceDirector
namespace RacingGame.Physics
{
    /// <summary>
    /// Calculates ERS deployment, recovery, and Overtake Mode.
    /// Updates the existing ERSState2026 objects used by RaceDirector.
    /// Implements 2026 F1 Regulations: 8.5MJ (8500kJ) battery, 350kW max deploy.
    /// </summary>
    public static class ERSPhysicsSystem
    {
        // ─ 2026 Regulation Constants ───────────────────────────────────────
        private const float MAX_BATTERY_KJ = 8500f;      // 8.5 MJ
        private const float MAX_DEPLOY_KW = 350f;        // Max electric motor power
        private const float OVERTAKE_BOOST_KW = 200f;    // Extra power for Overtake Mode
        private const float OVERTAKE_DURATION = 5f;      // Seconds
        private const float OVERTAKE_COOLDOWN = 20f;     // Seconds
        private const float OVERTAKE_MIN_BATTERY = 300f; // kJ required to activate
        private const float LAP_DEPLOY_LIMIT_KJ = 4000f; // Max deploy per lap
        // Deploy power multipliers based on mode
        private static float GetDeployMultiplier(DeploymentMode mode)
        {
            return mode switch
            {
                DeploymentMode.Harvest    => 0f,
                DeploymentMode.Race_Low   => 0.40f, // ~140 kW
                DeploymentMode.Race_Medium=> 0.70f, // ~245 kW
                DeploymentMode.Race_High  => 1.00f, // 350 kW
                DeploymentMode.Hotlap     => 1.00f, // 350 kW
                _                         => 0.70f
            };
        }
        /// <summary>
        /// Call this every FixedUpdate inside RaceDirector.Tick2026Systems
        /// </summary>
        public static void UpdateERS(
            ERSState2026 ers,
            float throttleInput,    // 0 to 1
            float brakeInput,       // 0 to 1
            DeploymentMode currentMode,
            float deltaTime)
        {
            if (ers == null) return;
            // 1. Handle Overtake Mode Timers
            UpdateOvertakeTimers(ers, deltaTime);
            // 2. Calculate Target Deploy Power (kW)
            float targetDeployKw = 0f;
            bool isHarvesting = false;
            if (ers.overtakeModeActive)
            {
                // Overtake mode overrides everything: Max power + boost
                targetDeployKw = MAX_DEPLOY_KW + OVERTAKE_BOOST_KW;
            }
            else if (currentMode == DeploymentMode.Harvest)
            {
                isHarvesting = true;
                targetDeployKw = 0f;
            }
            else
            {
                // Normal deployment scaled by throttle
                float multiplier = GetDeployMultiplier(currentMode);
                targetDeployKw = MAX_DEPLOY_KW * multiplier * throttleInput;
            }
            // 3. Apply Deployment (Drain Battery)
            if (targetDeployKw > 0f && ers.storedEnergyKJ > 0f)
            {
                // Convert kW to kJ per frame: kW * (dt / 3600) * 1000 = kW * dt / 3.6
                float energyUsed = (targetDeployKw * deltaTime) / 3.6f;
                // Clamp to available battery
                energyUsed = Mathf.Min(energyUsed, ers.storedEnergyKJ);
                ers.storedEnergyKJ -= energyUsed;
                ers.currentDeployKw = (energyUsed * 3.6f) / deltaTime; // Actual kW used
                // Track lap budget
                ers.lapDeployKJ += energyUsed;
            }
            else
            {
                ers.currentDeployKw = 0f;
            }
            // 4. Apply Recovery (Charge Battery)
            if (isHarvesting || brakeInput > 0.1f)
            {
                // Harvest mode recovers faster than standard braking
                float recoveryRateKw = isHarvesting ? 300f : 150f; 
                float recoveryMultiplier = isHarvesting ? 1f : brakeInput;
                float energyRecovered = (recoveryRateKw * recoveryMultiplier * deltaTime) / 3.6f;
                ers.storedEnergyKJ += energyRecovered;
            }
            // 5. Clamp Battery and Update UI Percentages
            ers.storedEnergyKJ = Mathf.Clamp(ers.storedEnergyKJ, 0f, MAX_BATTERY_KJ);
            ers.BatteryPercent = (ers.storedEnergyKJ / MAX_BATTERY_KJ) * 100f;
            // 6. Check Lap Deploy Limit
            ers.deployLimitReached = ers.lapDeployKJ >= LAP_DEPLOY_LIMIT_KJ;
            if (ers.deployLimitReached)
            {
                ers.currentDeployKw = 0f; // Cut deploy if limit reached
            }
        }
        /// <summary>
        /// Call this when the player presses the Overtake button
        /// </summary>
        public static bool TryActivateOvertakeMode(ERSState2026 ers)
        {
            if (ers == null) return false;
            // Check conditions: Not active, not on cooldown, enough battery, not over lap limit
            if (!ers.overtakeModeActive && 
                ers.overtakeCooldown <= 0f && 
                ers.storedEnergyKJ >= OVERTAKE_MIN_BATTERY &&
                !ers.deployLimitReached)
            {
                ers.overtakeModeActive = true;
                ers.overtakeCooldown = OVERTAKE_COOLDOWN; // Start cooldown immediately
                return true;
            }
            return false;
        }
        /// <summary>
        /// Call this at the start of every new lap to reset lap-specific counters
        /// </summary>
        public static void ResetLapCounters(ERSState2026 ers)
        {
            if (ers != null)
            {
                ers.lapDeployKJ = 0f;
                ers.deployLimitReached = false;
            }
        }
        private static void UpdateOvertakeTimers(ERSState2026 ers, float dt)
        {
            if (ers.overtakeModeActive)
            {
                // We don't track a separate duration timer in the state, 
                // so we assume the UI/Controller handles deactivation, 
                // OR we can track it here if you add a float 'overtakeTimer' to ERSState2026.
                // For now, we just ensure cooldown ticks down.
            }
            if (ers.overtakeCooldown > 0f)
            {
                ers.overtakeCooldown -= dt;
                if (ers.overtakeCooldown < 0f) ers.overtakeCooldown = 0f;
            }
        }
    }
}
