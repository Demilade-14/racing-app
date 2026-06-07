using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RacingGame.Career;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  R&D GARAGE UI  – car upgrade management
    // ═══════════════════════════════════════════════════════════════════════
    public class RnDGarageUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tokenBudgetText;           // 340/500 tokens
        [SerializeField] TextMeshProUGUI performanceMultiplierText; // Overall +11%

        [SerializeField] Transform categoryContainer;
        [SerializeField] GameObject categoryRowPrefab;

        RnDManager _rnd;
        List<RnDCategoryRow> _categoryRows = new();

        void Start()
        {
            _rnd = RnDManager.Instance;

            if (_rnd == null)
            {
                Debug.LogError("[RnDGarageUI] RnDManager not found!");
                return;
            }

            // Listen to upgrade events
            _rnd.OnTokensAwarded += RefreshBudget;
            _rnd.OnUpgradeCompleted += OnUpgradeCompleted;

            PopulateCategoryRows();
            RefreshBudget(0);
        }

        void PopulateCategoryRows()
        {
            _categoryRows.Clear();

            foreach (var category in _rnd.GetAllCategories())
            {
                var rowObj = Instantiate(categoryRowPrefab, categoryContainer);
                var row = rowObj.GetComponent<RnDCategoryRow>();

                if (row != null)
                {
                    row.Bind(category, this);
                    _categoryRows.Add(row);
                }
            }
        }

        void RefreshBudget(float tokensAwarded)
        {
            tokenBudgetText.text = _rnd.GetTokenDisplay();

            // Calculate overall performance bonus
            float overall = _rnd.GetOverallPerformanceMultiplier();
            float gain = (overall - 1.0f) * 100f;
            performanceMultiplierText.text = $"Car Performance: +{gain:F1}%";

            Debug.Log($"[RnDGarageUI] Budget: {_rnd.tokenBudgetRemaining:F0}/500 | Overall: +{gain:F1}%");
        }

        void OnUpgradeCompleted(string categoryName, int newLevel)
        {
            Debug.Log($"[RnDGarageUI] {categoryName} upgraded to level {newLevel}");

            // Refresh all rows to show updated costs/levels
            foreach (var row in _categoryRows)
                row.Refresh();

            RefreshBudget(0);
        }

        void OnDestroy()
        {
            if (_rnd)
            {
                _rnd.OnTokensAwarded -= RefreshBudget;
                _rnd.OnUpgradeCompleted -= OnUpgradeCompleted;
            }
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    public class RnDCategoryRow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI categoryNameText;
        [SerializeField] TextMeshProUGUI levelText;              // "Level 3/5"
        [SerializeField] TextMeshProUGUI performanceGainText;   // "+4.5%"
        [SerializeField] TextMeshProUGUI costText;              // "300 tokens"
        [SerializeField] Button upgradeButton;

        RnDManager.RnDCategory _category;
        RnDGarageUI _parent;

        public void Bind(RnDManager.RnDCategory category, RnDGarageUI parent)
        {
            _category = category;
            _parent = parent;

            if (upgradeButton)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            Refresh();
        }

        public void Refresh()
        {
            if (_category == null) return;

            categoryNameText.text = _category.name;
            levelText.text = $"Level {_category.level}/{_category.maxLevel}";
            performanceGainText.text = $"+{_category.PerformanceGain * 100f:F1}%";

            if (_category.level >= _category.maxLevel)
            {
                costText.text = "MAX LEVEL";
                upgradeButton.interactable = false;
            }
            else
            {
                float cost = _category.TokensToNextLevel;
                costText.text = $"{cost:F0} tokens";
                upgradeButton.interactable = RnDManager.Instance.tokenBudgetRemaining >= cost;
            }
        }

        void OnUpgradeClicked()
        {
            if (RnDManager.Instance.TryUpgradeCategory(_category.name))
            {
                Debug.Log($"[RnDCategoryRow] Upgraded {_category.name}");
            }
            else
            {
                Debug.LogWarning($"[RnDCategoryRow] Could not upgrade {_category.name}");
            }

            Refresh();
        }

        void OnDestroy()
        {
            if (upgradeButton)
                upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }
    }
}
