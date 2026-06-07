using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;
using RacingGame.Data;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CONTRACT NEGOTIATION UI  – display and negotiate team offers
    // ═══════════════════════════════════════════════════════════════════════
    public class ContractNegotiationUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI teamNameText;
        [SerializeField] TextMeshProUGUI salaryText;
        [SerializeField] TextMeshProUGUI bonusesText;
        [SerializeField] TextMeshProUGUI durationText;
        [SerializeField] TextMeshProUGUI numberOneText;

        [SerializeField] Button acceptButton;
        [SerializeField] Button counterButton;
        [SerializeField] Button rejectButton;
        [SerializeField] Button closeButton;

        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI statusText;

        DriverCareerManager _career;
        ContractOffer _currentOffer;
        ContractNegotiationEngine.NegotiationState _negotiationState;

        void Start()
        {
            _career = DriverCareerManager.Instance;

            acceptButton?.onClick.AddListener(OnAcceptClicked);
            counterButton?.onClick.AddListener(OnCounterClicked);
            rejectButton?.onClick.AddListener(OnRejectClicked);
            closeButton?.onClick.AddListener(OnCloseClicked);

            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        public void ShowContractOffer(ContractOffer offer)
        {
            if (_career?.PlayerProfile == null) return;

            _currentOffer = offer;
            _negotiationState = new ContractNegotiationEngine.NegotiationState
            {
                originalOffer = offer,
                status = ContractNegotiationEngine.NegotiationResult.Pending
            };

            // Display offer details
            teamNameText.text = offer.teamName;
            salaryText.text = ContractNegotiationEngine.FormatSalary(offer.offeredSalary);
            bonusesText.text = $\"Podium: {ContractNegotiationEngine.FormatSalary(offer.podiumBonus)} | \" +
                              $\"Win: {ContractNegotiationEngine.FormatSalary(offer.winBonus)}\";
            durationText.text = $\"{offer.seasons} year{(offer.seasons > 1 ? \"s\" : \"\")}\";
            numberOneText.text = offer.offersNumberOne ? \"✅ #1 Driver Status\" : \"❌ #2 Driver Status\";

            // Evaluate offer and get recommendation
            var result = ContractNegotiationEngine.EvaluateOffer(offer, _career.PlayerProfile, _negotiationState);
            statusText.text = ContractNegotiationEngine.GetNegotiationStatusText(result);

            gameObject.SetActive(true);
            CanvasGroupFade(0, 1, 0.3f);

            Debug.Log($\"[ContractUI] Offer from {offer.teamName}: {ContractNegotiationEngine.FormatSalary(offer.offeredSalary)}\");
        }

        void OnAcceptClicked()
        {
            _career.OfferContract(_currentOffer);
            statusText.text = \"✅ Contract accepted!\";
            statusText.color = Color.green;
            Invoke(nameof(OnCloseClicked), 2f);
        }

        void OnCounterClicked()
        {
            // Generate counter offer UI
            var counterOffer = GeneratePlayerCounter();
            ShowCounterOfferDialog(counterOffer);
        }

        ContractOffer GeneratePlayerCounter()
        {
            // Player counters with higher wage (10-25% increase)
            float counterMultiplier = Random.Range(1.1f, 1.25f);
            float newSalary = _currentOffer.offeredSalary * counterMultiplier;

            var counter = new ContractOffer
            {
                teamName = _currentOffer.teamName,
                tier = _currentOffer.tier,
                offeredSalary = newSalary,
                podiumBonus = _currentOffer.podiumBonus,
                winBonus = _currentOffer.winBonus,
                seasons = _currentOffer.seasons + Random.Range(0, 2),
                offersNumberOne = !_currentOffer.offersNumberOne  // Request if not offered
            };

            return counter;
        }

        void ShowCounterOfferDialog(ContractOffer counterOffer)
        {
            salaryText.text = ContractNegotiationEngine.FormatSalary(counterOffer.offeredSalary);
            durationText.text = $\"{counterOffer.seasons} years\";

            statusText.text = \"Waiting for team response...\";
            statusText.color = Color.yellow;

            counterButton.interactable = false;
            acceptButton.interactable = false;

            Invoke(nameof(ResolveFinalOffer), 1.5f);
        }

        void ResolveFinalOffer()
        {
            // Team responds to counter
            bool accepted = Random.value > 0.3f;  // 70% acceptance rate

            if (accepted)
            {
                statusText.text = \"✅ Team accepts your counter!\";
                statusText.color = Color.green;
                Invoke(nameof(OnCloseClicked), 1.5f);
            }
            else
            {
                statusText.text = \"❌ Team declined. Lower your demands or seek other offers.\";
                statusText.color = Color.red;
                counterButton.interactable = true;
            }
        }

        void OnRejectClicked()
        {
            statusText.text = \"❌ Offer rejected. Seeking other opportunities...\";
            statusText.color = Color.red;
            Invoke(nameof(OnCloseClicked), 1.5f);
        }

        void OnCloseClicked()
        {
            CanvasGroupFade(1, 0, 0.3f);
            Invoke(nameof(DisablePanel), 0.3f);
        }

        void DisablePanel()
        {
            gameObject.SetActive(false);
        }

        void CanvasGroupFade(float from, float to, float duration)
        {
            StartCoroutine(FadeCoroutine(from, to, duration));
        }

        System.Collections.IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        void OnDestroy()
        {
            acceptButton?.onClick.RemoveListener(OnAcceptClicked);
            counterButton?.onClick.RemoveListener(OnCounterClicked);
            rejectButton?.onClick.RemoveListener(OnRejectClicked);
            closeButton?.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
