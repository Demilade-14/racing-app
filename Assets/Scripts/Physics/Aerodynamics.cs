using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Physics
{
    public static class Aerodynamics
    {
        const float AIR_DENSITY = 1.225f;
        const float REF_AREA = 1.52f;
        const float BASE_CL = 2.8f;
        const float DRS_DRAG_FACTOR = 0.72f;
        const float BOTTOM_OUT_DOWNFORCE_MULTIPLIER = 0.42f;

        public static float CalculateDownforce(float speedMs, CarSetup setup,
            ComponentDamage damage, float rideHeight, bool bottomedOut)
        {
            float wingLoad = (setup.frontWingAngle + setup.rearWingAngle) / 22f;
            float cl = BASE_CL * wingLoad;

            float floorHealth = 1f - damage.floor / 100f * 0.30f;
            float bargeHealth = 1f - damage.bargeboard / 100f * 0.15f;
            float groundEffect = CalculateGroundEffect(rideHeight);

            float raw = 0.5f * cl * AIR_DENSITY * REF_AREA * speedMs * speedMs;
            float downforce = raw * floorHealth * bargeHealth * groundEffect;

            if (bottomedOut)
            {
                downforce *= BOTTOM_OUT_DOWNFORCE_MULTIPLIER;
            }

            return downforce;
        }

        public static float CalculateDrag(float speedMs, CarSetup setup,
            bool drsActive, ComponentDamage damage, VehicleState car = null)
        {
            float wingLoad = (setup.frontWingAngle + setup.rearWingAngle) / 22f;
            float cd = 0.88f + wingLoad * 0.45f;

            if (drsActive)
            {
                cd *= DRS_DRAG_FACTOR;
            }

            float impactPenalty = 1f + (damage.frontWing + damage.rearWing) / 100f * 0.16f;
            float raw = 0.5f * cd * AIR_DENSITY * REF_AREA * speedMs * speedMs * impactPenalty;

            return car != null ? AerodynamicWakeSystem.AdjustDrag(raw, car) : raw;
        }

        public static float CalculateGroundEffect(float rideHeight)
        {
            float normalizedHeight = Mathf.InverseLerp(0.095f, 0.032f, rideHeight);
            return Mathf.Lerp(0.80f, 1.25f, Mathf.Clamp01(normalizedHeight));
        }

        public static bool IsBottomedOut(float rideHeight)
        {
            return rideHeight <= 0.015f;
        }
    }
}
