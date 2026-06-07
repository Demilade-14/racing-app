using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RacingGame.Manager;

namespace RacingGame.Career
{
    public class TransferDriverRow : MonoBehaviour
    {
        public TextMeshProUGUI driverNameText;
        public TextMeshProUGUI teamText;
        public TextMeshProUGUI ratingText;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI wageText;
        public TextMeshProUGUI statusText;
        public Button           signButton;
        public Image            accentBar;

        DriverMarketEntry _driver;
        Action<DriverMarketEntry> _onSign;

        public void Bind(DriverMarketEntry driver, Action<DriverMarketEntry> onSign)
        {
            _driver = driver;
            _onSign = onSign;

            driverNameText.text = driver.driverName;
            teamText.text       = driver.currentTeam;
            ratingText.text     = driver.overallRating.ToString();
            valueText.text      = driver.MarketValueStr;
            wageText.text       = driver.AskingWageStr;
            statusText.text     = driver.contractStatus == ContractStatus.Active ? "Contracted" : "Available";

            if (accentBar != null)
            {
                accentBar.color = driver.isFreeAgent ? new Color(0.18f, 0.67f, 0.35f) : new Color(0.14f, 0.45f, 0.86f);
            }

            signButton.onClick.RemoveAllListeners();
            signButton.interactable = driver.contractStatus != ContractStatus.Active;
            signButton.onClick.AddListener(() => _onSign?.Invoke(_driver));
        }
    }
}
