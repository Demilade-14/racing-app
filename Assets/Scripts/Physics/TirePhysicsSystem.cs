using UnityEngine;
using RacingGame.Race; // References TyreState2026 and TyreCompound2026 from your RaceDirector
namespace RacingGame.Physics
{
    /// <summary>
    /// Calculates tire wear, temperature, and grip for 2026 regulations.
    /// Updates the existing TyreState2026 objects used by RaceDirector.
    /// </summary>
    public class TirePhysicsSystem
    {
        // ── 2026 Pirelli Compound Data ──────────────────────────────────────
        private static readonly float[] BASE_GRIP = { 1.0f, 0.92f, 0.85f, 0.88f, 0.82f }; // S, M, H, I, W
        private static readonly float[] WEAR_RATES = { 1.2f, 0.8f, 0.5f, 0.3f, 0.2f };
        private static readonly float[] OPTIMAL_TEMP_MIN = { 80f, 70f, 60f, 40f, 30f };
        private static readonly float[] OPTIMAL_TEMP_MAX = { 110f, 100f, 90f, 60f, 50f };
        /// <summary>
        /// Call this every FixedUpdate to update tire states
        /// </summary>
        public static void UpdateTires(
            TyreState2026[] tires, 
            float speedKmh, 
            float throttleInput, 
            float brakeInput, 
            float steeringInput,
            float trackTemperature,
            float deltaTime)
        {
            if (tires == null || tires.Length != 4) return;
            // Calculate overall stress factors
            float lateralStress = Mathf.Abs(steeringInput) * (speedKmh / 200f);
            float longitudinalStress = (Mathf.Abs(throttleInput) + Mathf.Abs(brakeInput)) * 0.5f;
            float totalStress = (lateralStress + longitudinalStress) * deltaTime;
            for (int i = 0; i < 4; i++)
            {
                var tire = tires[i];
                int compoundIndex = GetCompoundIndex(tire.currentCompound);
                // 1. Update Temperature
                UpdateTemperature(tire, compoundIndex, trackTemperature, totalStress, deltaTime);
                // 2. Update Wear (Life Percent)
                UpdateWear(tire, compoundIndex, totalStress, deltaTime);
                // 3. Calculate Grip
                CalculateGrip(tire, compoundIndex);
                // 4. Check for "The Cliff" (sudden loss of grip)
                CheckCliff(tire, compoundIndex);
            }
        }
        static void UpdateTemperature(TyreState2026 tire, int compoundIdx, float trackTemp, float stress, float dt)
        {
            // Heat generation from friction/stress
            float heatGen = stress * 15f * dt;
            // Cooling towards track temperature
            float cooling = (tire.tyreTemp - trackTemp) * 0.05f * dt;
            tire.tyreTemp += heatGen - cooling;
            // Clamp realistic temps
            tire.tyreTemp = Mathf.Clamp(tire.tyreTemp, trackTemp, 140f);
        }
        static void UpdateWear(TyreState2026 tire, int compoundIdx, float stress, float dt)
        {
            if (tire.lifePercent <= 0f) 
            {
                tire.lifePercent = 0f;
                return;
            }
            // Base wear + stress wear
            float wearAmount = (WEAR_RATES[compoundIdx] * 0.1f + stress * 2f) * dt;
            tire.lifePercent -= wearAmount;
            tire.lifePercent = Mathf.Clamp01(tire.lifePercent);
        }
        static void CalculateGrip(TyreState2026 tire, int compoundIdx)
        {
            float grip = BASE_GRIP[compoundIdx];
            // Temperature penalty (outside optimal window)
            float optimalMid = (OPTIMAL_TEMP_MIN[compoundIdx] + OPTIMAL_TEMP_MAX[compoundIdx]) / 2f;
            float optimalRange = (OPTIMAL_TEMP_MAX[compoundIdx] - OPTIMAL_TEMP_MIN[compoundIdx]) / 2f;
            float tempDiff = Mathf.Abs(tire.tyreTemp - optimalMid);
            if (tempDiff > optimalRange)
            {
                float penalty = (tempDiff - optimalRange) / 40f;
                grip -= penalty * 0.3f;
            }
            // Wear penalty (non-linear: grip drops faster as tire gets old)
            float wearFactor = Mathf.Pow(tire.lifePercent, 1.5f);
            grip *= wearFactor;
            tire.gripMultiplier = Mathf.Clamp01(grip);
        }
        static void CheckCliff(TyreState2026 tire, int compoundIdx)
        {
            // "The Cliff" happens when life < 20% and temp is too high
            bool isCliff = tire.lifePercent < 0.20f && tire.tyreTemp > OPTIMAL_TEMP_MAX[compoundIdx];
            if (isCliff && !tire.inCliff)
            {
                tire.inCliff = true;
                // Grip drops drastically in the cliff
                tire.gripMultiplier *= 0.6f; 
            }
            else if (!isCliff)
            {
                tire.inCliff = false;
            }
        }
        static int GetCompoundIndex(TyreCompound2026 compound)
        {
            return compound switch
            {
                TyreCompound2026.Soft => 0,
                TyreCompound2026.Medium => 1,
                TyreCompound2026.Hard => 2,
                TyreCompound2026.Intermediate => 3,
                TyreCompound2026.Wet => 4,
                _ => 1
            };
        }
    }
}
