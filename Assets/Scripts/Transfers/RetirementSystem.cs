using UnityEngine;
using RacingGame.Drivers;
namespace RacingGame.Transfers
{
    public static class RetirementSystem
    {
        public static bool ShouldRetire(DriverData driver)
        {
            // No retirement before 32
            if (driver.age < 32) return false;
            // Base chance increases with age
            float baseChance = (driver.age - 31) * 0.06f;
            // Performance factor: Low pace increases retirement chance
            float performanceFactor = Mathf.Clamp01(1f - (driver.rating.pace / 100f));
            // Personality factor: High aggression leads to earlier burnout/retirement
            float aggressionFactor = (driver.rating.aggression / 100f) * 0.10f;
            float finalChance = baseChance + (performanceFactor * 0.15f) + aggressionFactor;
            return Random.value < finalChance;
        }
    }
}
