using UnityEngine;
using RacingGame.Core;
namespace RacingGame.Aero
{
    /// <summary>
    /// 2026 Active Aero System (DRS replacement)
    /// More sophisticated than traditional DRS with multiple zones
    /// </summary>
    public class ActiveAeroSystem : MonoBehaviour
    {
        [Header("Aero State")]
        public bool isDeployed;
        public ActiveAeroMode currentMode;
        [Header("2026 Regulations")]
        public float dragReductionFactor = 0.23f;      // 23% drag reduction when open
        public float downforceLossFactor = 0.35f;      // 35% downforce loss when open
        public float deploySpeedThreshold = 280f;      // km/h required
        public float steeringThreshold = 15f;          // degrees (retracts if turning)
        [Header("Overtake Zones")]
        public OvertakeZone[] overtakingZones;
        [Header("Mobile Optimization")]
        public bool useSimplifiedAero = true;
        private float currentSpeed;
        private float currentSteeringAngle;
        private float currentDragCoefficient;
        private float currentDownforceCoefficient;
        private float baseDragCoefficient = 0.35f;
        private float baseDownforceCoefficient = 3.5f;
        void Update()
        {
            UpdateAeroPhysics();
            CheckDeploymentConditions();
        }
        void UpdateAeroPhysics()
        {
            // Base coefficients
            currentDragCoefficient = baseDragCoefficient;
            currentDownforceCoefficient = baseDownforceCoefficient;
            // Apply mode modifiers
            switch (currentMode)
            {
                case ActiveAeroMode.LowDrag:
                    currentDragCoefficient *= (1f - dragReductionFactor);
                    currentDownforceCoefficient *= (1f - downforceLossFactor);
                    isDeployed = true;
                    break;
                case ActiveAeroMode.MaxDownforce:
                    currentDownforceCoefficient *= 1.15f;
                    currentDragCoefficient *= 1.08f;
                    isDeployed = false;
                    break;
                case ActiveAeroMode.Balanced:
                default:
                    isDeployed = false;
                    break;
            }
        }
        void CheckDeploymentConditions()
        {
            if (currentMode != ActiveAeroMode.LowDrag)
            {
                isDeployed = false;
                return;
            }
            // Check speed threshold
            if (currentSpeed < deploySpeedThreshold)
            {
                isDeployed = false;
                return;
            }
            // Check steering angle (safety feature)
            if (Mathf.Abs(currentSteeringAngle) > steeringThreshold)
            {
                isDeployed = false;
                return;
            }
            // Check if in overtaking zone
            if (!IsInOvertakingZone())
            {
                isDeployed = false;
                return;
            }
            // All conditions met - deploy!
            isDeployed = true;
        }
        bool IsInOvertakingZone()
        {
            // TODO: Get position from RaceDirector
            // For now, allow deployment everywhere (will be refined later)
            return true;
        }
        /// <summary>
        /// Set aero mode (called by driver input or AI)
        /// </summary>
        public void SetMode(ActiveAeroMode mode)
        {
            currentMode = mode;
        }
        /// <summary>
        /// Update speed from physics
        /// </summary>
        public void UpdateSpeed(float speedKmh)
        {
            currentSpeed = speedKmh;
        }
        /// <summary>
        /// Update steering angle from input
        /// </summary>
        public void UpdateSteering(float angle)
        {
            currentSteeringAngle = angle;
        }
        /// <summary>
        /// Get current drag coefficient for physics calculation
        /// </summary>
        public float GetDragCoefficient()
        {
            return currentDragCoefficient;
        }
        /// <summary>
        /// Get current downforce coefficient for physics calculation
        /// </summary>
        public float GetDownforceCoefficient()
        {
            return currentDownforceCoefficient;
        }
        /// <summary>
        /// Get top speed bonus from DRS (for UI)
        /// </summary>
        public float GetTopSpeedBonus()
        {
            return isDeployed ? 15f : 0f; // ~15 km/h bonus
        }
    }
    [System.Serializable]
    public class OvertakeZone
    {
        public string zoneName;
        public float startDistance;  // Meters from start/finish
        public float endDistance;
        public bool isActive;
    }
    public enum ActiveAeroMode
    {
        LowDrag,           // DRS-like (overtaking)
        Balanced,          // Default
        MaxDownforce,      // Qualifying/corners
        Auto               // AI controlled
    }
}
