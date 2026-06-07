using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  R&D MANAGER  – car upgrade/development token system
    // ═══════════════════════════════════════════════════════════════════════
    public class RnDManager : MonoBehaviour
    {
        [System.Serializable]
        public class RnDCategory
        {
            public string name;              // "Aerodynamics", "PowerUnit", "Chassis", "Reliability"
            public int level = 1;
            public int maxLevel = 5;
            public float tokens;             // accumulated this season
            public float performanceMultiplier = 1.0f;  // 1.0 = baseline, 1.02 = 2% faster

            public float PerformanceGain => performanceMultiplier - 1.0f;  // e.g., 0.02 = +2%
            public float TokensToNextLevel => 100 * level;  // 100, 200, 300, 400, 500
        }

        public event Action<string, int> OnUpgradeCompleted;  // (category, newLevel)
        public event Action<float> OnTokensAwarded;

        public List<RnDCategory> categories = new();
        public float tokenBudgetRemaining;
        public float tokensEarned;

        void Awake()
        {
            InitializeRnD();
        }

        void InitializeRnD()
        {
            categories.Clear();

            // Each category starts at level 1
            categories.Add(new RnDCategory { name = "Aerodynamics", level = 1 });
            categories.Add(new RnDCategory { name = "PowerUnit", level = 1 });
            categories.Add(new RnDCategory { name = "Chassis", level = 1 });
            categories.Add(new RnDCategory { name = "Reliability", level = 1 });

            ResetSeasonBudget();
        }

        void ResetSeasonBudget()
        {
            tokenBudgetRemaining = 500f;  // F1 budget cap equivalent
            tokensEarned = 0f;
        }

        // ── AWARD TOKENS ──────────────────────────────────────────────────
        public void AwardTokens(float amount)
        {
            tokensEarned += amount;
            tokenBudgetRemaining = Mathf.Min(tokensEarned, 500f);  // cap at budget
            OnTokensAwarded?.Invoke(amount);
        }

        // ── UPGRADE LOGIC ─────────────────────────────────────────────────
        public bool TryUpgradeCategory(string categoryName)
        {
            var cat = FindCategory(categoryName);
            if (cat == null) return false;

            if (cat.level >= cat.maxLevel)
            {
                Debug.LogWarning($"[RnD] {categoryName} already at max level {cat.maxLevel}");
                return false;
            }

            float costTokens = cat.TokensToNextLevel;
            if (tokenBudgetRemaining < costTokens)
            {
                Debug.LogWarning($"[RnD] Not enough tokens for {categoryName}. Need {costTokens}, have {tokenBudgetRemaining}");
                return false;
            }

            tokenBudgetRemaining -= costTokens;
            cat.tokens += costTokens;
            cat.level++;

            // Update performance multiplier
            cat.performanceMultiplier = 1.0f + (cat.level - 1) * 0.015f;  // 1.5% per level

            OnUpgradeCompleted?.Invoke(categoryName, cat.level);

            Debug.Log($"[RnD] {categoryName} upgraded to level {cat.level}. Performance: +{cat.PerformanceGain * 100:F1}%");

            return true;
        }

        // ── CAR PERFORMANCE MODIFIER ──────────────────────────────────────
        public float GetOverallPerformanceMultiplier()
        {
            float combined = 1.0f;
            foreach (var cat in categories)
                combined *= cat.performanceMultiplier;
            return combined;
        }

        public float GetCategoryMultiplier(string categoryName)
        {
            var cat = FindCategory(categoryName);
            return cat?.performanceMultiplier ?? 1.0f;
        }

        RnDCategory FindCategory(string name) =>
            categories.Find(c => c.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));

        // ── DISPLAY HELPERS ───────────────────────────────────────────────
        public string GetTokenDisplay() => $"{tokenBudgetRemaining:F0} / 500";

        public List<RnDCategory> GetAllCategories() => categories;

        public void ResetSeason()
        {
            ResetSeasonBudget();
            foreach (var cat in categories)
                cat.tokens = 0;
        }
    }
}
