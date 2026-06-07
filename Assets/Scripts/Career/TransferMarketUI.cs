using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RacingGame.Manager;
using RacingGame.Data;

namespace RacingGame.Career
{
    public class TransferMarketUI : MonoBehaviour
    {
        [Header("References")]
        public MyTeamManager teamManager;
        public Transform     listRoot;
        public TransferDriverRow rowPrefab;

        [Header("UI")]
        public TextMeshProUGUI headerLabel;
        public TextMeshProUGUI budgetLabel;
        public TextMeshProUGUI windowLabel;
        public TextMeshProUGUI newsLabel;

        [Header("Sort / Filter")]
        public Button sortRatingButton;
        public Button sortValueButton;
        public Button filterAllButton;
        public Button filterFreeAgentButton;
        public Button filterAvailableButton;

        [Header("Quick Actions")]
        public Button quickSignRatingButton;
        public Button quickSignValueButton;

        public int defaultSeat = 1;

        string _activeSort = "ovr";
        string _activeFilter = "all";
        List<DriverMarketEntry> _currentMarket = new();

        void Start()
        {
            BindButtons();
            RefreshMarket();
        }

        void BindButtons()
        {
            sortRatingButton?.onClick.AddListener(() => SetSort("ovr"));
            sortValueButton?.onClick.AddListener(() => SetSort("value"));
            filterAllButton?.onClick.AddListener(() => SetFilter("all"));
            filterFreeAgentButton?.onClick.AddListener(() => SetFilter("freeAgents"));
            filterAvailableButton?.onClick.AddListener(() => SetFilter("available"));

            quickSignRatingButton?.onClick.AddListener(() => QuickSignByRating());
            quickSignValueButton?.onClick.AddListener(() => QuickSignByValue());
        }

        public void RefreshMarket()
        {
            if (teamManager == null)
                return;

            UpdateHeader();
            _currentMarket = BuildMarketList();
            RenderMarketList();
        }

        void UpdateHeader()
        {
            if (budgetLabel != null)
            {
                budgetLabel.text = teamManager.Save.finances.AvailableCashStr;
            }
            if (windowLabel != null)
            {
                windowLabel.text = teamManager.Save.windowState.ToString();
            }
        }

        List<DriverMarketEntry> BuildMarketList()
        {
            var market = teamManager.GetMarketSorted(_activeSort, _activeFilter != "all");
            return _activeFilter switch
            {
                "freeAgents"  => market.Where(d => d.isFreeAgent).ToList(),
                "available"   => market.Where(d => d.isFreeAgent || d.contractStatus == ContractStatus.Expiring || d.mood == DriverMood.WantsOut).ToList(),
                _              => market
            };
        }

        void RenderMarketList()
        {
            if (listRoot == null || rowPrefab == null) return;

            foreach (Transform t in listRoot)
                Destroy(t.gameObject);

            foreach (var driver in _currentMarket)
            {
                var row = Instantiate(rowPrefab, listRoot);
                row.Bind(driver, OnDriverSignRequested);
            }
        }

        void OnDriverSignRequested(DriverMarketEntry driver)
        {
            if (teamManager == null) return;

            var offer = teamManager.BuildOwnerOffer(driver, driver.overallRating >= 85);
            bool signed = teamManager.AttemptSign(driver, offer, defaultSeat);
            newsLabel.text = signed ? $"Signed {driver.driverName} for £{offer.offeredWage:F0}/season." : $"Failed to sign {driver.driverName}.";
            RefreshMarket();
        }

        void SetSort(string sortKey)
        {
            _activeSort = sortKey;
            RefreshMarket();
        }

        void SetFilter(string filterKey)
        {
            _activeFilter = filterKey;
            RefreshMarket();
        }

        void QuickSignByRating()
        {
            if (teamManager == null) return;
            if (!teamManager.QuickSignByRating(defaultSeat))
                newsLabel.text = "No available driver could be signed by rating.";
            else
                newsLabel.text = "Quick-signed top driver by rating.";
            RefreshMarket();
        }

        void QuickSignByValue()
        {
            if (teamManager == null) return;
            if (!teamManager.QuickSignByValue(defaultSeat))
                newsLabel.text = "No available driver could be signed by value.";
            else
                newsLabel.text = "Quick-signed best value driver.";
            RefreshMarket();
        }
    }
}
