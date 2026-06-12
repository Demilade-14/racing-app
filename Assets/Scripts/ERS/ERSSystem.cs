using UnityEngine;
using RacingGame.Core;
namespace RacingGame.ERS
{
    /// <summary>
    /// 2026 ERS (Energy Recovery System)
    /// 8.5MJ battery capacity, 350kW electric motor
    /// </summary>
    public class ERSSystem : MonoBehaviour
    {
        [Header("2026 Regulations")]
        public float batteryCapacityMJ = 8.5f;
        public float motorPowerKW = 350f;
        public float combustionPowerKW = 400f;
        public float recoveryPerLap = 1.8f;      // MJ recovered per lap
        public float deployPerLap = 2.4f;        // MJ used per lap in Attack mode
        [Header("Overtake Mode")]
        public float overtakeBoostKW = 200f;     // Extra power
        public float overtakeDuration = 5f;      // Seconds
        public float overtakeCooldown = 20f;     // Seconds
        public float minBatteryForOvertake = 0.30f; // 30% minimum
        [Header("State")]
        public float currentBatteryMJ;
        public float batteryPercent;
        public DeploymentMode deployMode;
        public bool overtakeModeActive;
        public float overtakeTimer;
        public float overtakeCooldownTimer;
        [Header("Mobile Optimization")]
        public bool useSimplifiedERS = true;
        void Start()
        {
            currentBatteryMJ = batteryCapacityMJ * 0.5f; // Start at 50%
            UpdateBatteryPercent();
        }
        void Update()
        {
            UpdateOvertakeMode();
            UpdateBatteryPercent();
        }
        /// <summary>
        /// Call every frame to manage ERS deployment
        /// </summary>
        public void UpdateDeployment(float deltaTime, float throttle)
        {
            if (overtakeModeActive)
            {
                // Full power deployment
                float powerUsed = (motorPowerKW + overtakeBoostKW) * deltaTime / 3600f; // Convert to MJ
                currentBatteryMJ -= powerUsed;
            }
            else
            {
                // Normal deployment based on mode
                float deployRate = GetDeployRate();
                float powerUsed = deployRate * throttle * deltaTime / 3600f;
                currentBatteryMJ -= powerUsed;
            }
            // Recovery (simplified - based on braking)
            if (throttle < 0.1f) // Braking
            {
                float recovery = 0.5f * deltaTime / 3600f; // MJ per second
                currentBatteryMJ += recovery;
            }
            // Clamp battery
            currentBatteryMJ = Mathf.Clamp(currentBatteryMJ, 0f, batteryCapacityMJ);
            UpdateBatteryPercent();
        }
        float GetDeployRate()
        {
            return deployMode switch
            {
                DeploymentMode.Harvest => 0f,           // No deployment, only recovery
                DeploymentMode.Balanced => motorPowerKW * 0.6f,
                DeploymentMode.Attack => motorPowerKW,
                _ => motorPowerKW * 0.5f
            };
        }
        void UpdateOvertakeMode()
        {
            if (overtakeModeActive)
            {
                overtakeTimer -= Time.deltaTime;
                if (overtakeTimer <= 0f)
                {
                    DeactivateOvertakeMode();
                }
            }
            if (overtakeCooldownTimer > 0f)
            {
                overtakeCooldownTimer -= Time.deltaTime;
            }
        }
        /// <summary>
        /// Activate overtake mode (if conditions met)
        /// </summary>
        public bool ActivateOvertakeMode()
        {
            if (overtakeModeActive || overtakeCooldownTimer > 0f)
                return false;
            if (batteryPercent < minBatteryForOvertake * 100f)
                return false;
            overtakeModeActive = true;
            overtakeTimer = overtakeDuration;
            overtakeCooldownTimer = overtakeCooldown;
            return true;
        }
        void DeactivateOvertakeMode()
        {
            overtakeModeActive = false;
            overtakeTimer = 0f;
        }
        void UpdateBatteryPercent()
        {
            batteryPercent = (currentBatteryMJ / batteryCapacityMJ) * 100f;
        }
        /// <summary>
        /// Set deployment mode (called by driver or AI)
        /// </summary>
        public void SetDeploymentMode(DeploymentMode mode)
        {
            deployMode = mode;
        }
        /// <summary>
        /// Get total power available (ICE + ERS)
        /// </summary>
        public float GetTotalPower()
        {
            float ersPower = overtakeModeActive 
                ? motorPowerKW + overtakeBoostKW 
                : GetDeployRate();
            return combustionPowerKW + ersPower;
        }
        /// <summary>
        /// Get ERS state for UI/HUD
        /// </summary>
        public ERSState2026 GetERSState()
        {
            return new ERSState2026
            {
                BatteryPercent = batteryPercent,
                currentBatteryMJ = currentBatteryMJ,
                deployMode = deployMode,
                overtakeModeActive = overtakeModeActive,
                overtakeAvailable = CanUseOvertake(),
                totalPowerKW = GetTotalPower()
            };
        }
        bool CanUseOvertake()
        {
            return !overtakeModeActive && 
                   overtakeCooldownTimer <= 0f && 
                   batteryPercent >= minBatteryForOvertake * 100f;
        }
    }
    [System.Serializable]
    public class ERSState2026
    {
        public float BatteryPercent;
        public float currentBatteryMJ;
        public DeploymentMode deployMode;
        public bool overtakeModeActive;
        public bool overtakeAvailable;
        public float totalPowerKW;
    }
}
