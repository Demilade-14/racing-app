using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;
using RacingGame.Data;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CONTRACT NEGOTIATION UI
    //  Displays contract offers and handles accept/counter/reject flow.
    //  Wires to DriverCareerManager.SignContract() and negotiation system.
    // ═══════════════════════════════════════════════════════════════════════
    public class ContractNegotiationUI : MonoBehaviour
    {
        [Header("Team Info")]
        [SerializeField] TextMeshProUGUI teamNameText;
        [SerializeField] Image          teamAccentBar;
        [SerializeField] TextMeshProUGUI teamPerformanceText;

        [Header("Contract Details")]
        [SerializeField] TextMeshProUGUI salaryText;
        [SerializeField] TextMeshProUGUI podiumBonusText;
        [SerializeField] TextMeshProUGUI winBonusText;
        [SerializeField] TextMeshProUGUI durationText;
        [SerializeField] TextMeshProUGUI numberOneText;
        [SerializeField] TextMeshProUGUI interestText;

        [Header("Negotiation")]
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] TextMeshProUGUI recommendationText;
        [SerializeField] Image          recommendationBar;

        [Header("Buttons")]
        [SerializeField] Button acceptButton;
        [SerializeField] Button counterButton;
        [SerializeField] Button rejectButton;
        [SerializeField] Button closeButton;

        [Header("Animation")]
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float fadeDuration = 0.3f;

        DriverCareerManager _career;
        ContractOffer      _currentOffer;
        bool               _isCountering = false;

        // ── Team color mapping for accent bars ────────────────────────────
        static readonly System.Collections.Generic.Dictionary<string, Color> TEAM_COLORS =
            new()
            {
                { "Red Bull Racing",    new Color(0.12f, 0.19f, 0.38f) },
                { "Ferrari",            new Color(0.86f, 0.00f, 0.00f) },
                { "Mercedes",           new Color(0.00f, 0.82f, 0.74f) },
                { "McLaren",            new Color(1.00f, 0.50f, 0.00f) },
                { "Aston Martin",       new Color(0.00f, 0.44f, 0.38f) },
                { "Alpine",             new Color(0.00f, 0.57f, 1.00f) },
                { "Williams",           new Color(0.00f, 0.35f, 1.00f) },
                { "Haas",               new Color(0.71f, 0.73f, 0.74f) },
                { "Kick Sauber",        new Color(0.20f, 0.88f, 0.20f) },
                { "RB",                 new Color(0.40f, 0.57f, 1.00f) },
                { "Audi F1 Team",       new Color(0.91f, 0.00f, 0.18f) },
                { "Cadillac F1 Team",   new Color(0.11f, 0.23f, 0.54f) },
            };

        // ═══════════════════════════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════
        void Start()
        {
            _career = DriverCareerManager.Instance;

            acceptButton?.onClick.AddListener(OnAcceptClicked);
            counterButton?.onClick.AddListener(OnCounterClicked);
            rejectButton?.onClick.AddListener(OnRejectClicked);
            closeButton?.onClick.AddListener(OnCloseClicked);

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            acceptButton?.onClick.RemoveAllListeners();
            counterButton?.onClick.RemoveAllListeners();
            rejectButton?.onClick.RemoveAllListeners();
            closeButton?.onClick.RemoveAllListeners();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  DISPLAY OFFER
        // ═══════════════════════════════════════════════════════════════════
        public void ShowContractOffer(ContractOffer offer)
        {
            if (_career?.PlayerProfile == null || offer == null) return;

            _currentOffer = offer;
            _isCountering = false;

            gameObject.SetActive(true);
            StartCoroutine(FadeCanvasGroup(0, 1, fadeDuration));

            PopulateOffer();
            EvaluateOffer();

            Debug.Log($"[ContractUI] Showing offer from {offer.teamName}: ${offer.offeredSalary / 1_000_000:F1}M");
        }

        void PopulateOffer()
        {
            // ── Team info ─────────────────────────────────────────────────
            if (teamNameText) teamNameText.text = _currentOffer.teamName;

            Color teamColor = TEAM_COLORS.TryGetValue(_currentOffer.teamName, out var c)
                ? c : new Color(0.91f, 0.00f, 0.18f);
            if (teamAccentBar) teamAccentBar.color = teamColor;

            if (teamPerformanceText)
                teamPerformanceText.text = $"Team Rating: {_currentOffer.teamBudgetAllocation / 1_000_000:F0}M budget";

            // ── Contract details ──────────────────────────────────────────
            if (salaryText)
                salaryText.text = $"${_currentOffer.offeredSalary / 1_000_000:F1}M/year";

            if (podiumBonusText)
                podiumBonusText.text = $"Podium: ${_currentOffer.podiumBonus / 1_000_000:F2}M";

            if (winBonusText)
                winBonusText.text = $"Win: ${_currentOffer.winBonus / 1_000_000:F2}M";

            if (durationText)
                durationText.text = $"{_currentOffer.seasons} year{(_currentOffer.seasons > 1 ? "s" : "")} remaining";

            if (numberOneText)
            {
                numberOneText.text = _currentOffer.offersNumberOne
                    ? "✅ #1 DRIVER STATUS"
                    : "❌ #2 DRIVER (Development Role)";
                numberOneText.color = _currentOffer.offersNumberOne
                    ? new Color(0.13f, 0.77f, 0.37f) : new Color(0.91f, 0.00f, 0.18f);
            }

            if (interestText)
            {
                string interestLabel = _currentOffer.interestLevel switch
                {
                    InterestLevel.Hot   => "🔥 VERY INTERESTED",
                    InterestLevel.Warm  => "⭐ INTERESTED",
                    InterestLevel.Cold  => "❄️ LOW INTEREST",
                    _                   => "NEUTRAL"
                };
                interestText.text = interestLabel;
            }

            // Reset button state
            counterButton.interactable = !_isCountering;
            acceptButton.interactable = true;
            rejectButton.interactable = true;

            if (statusText) statusText.text = "";
        }

        void EvaluateOffer()
        {
            if (recommendationText == null || recommendationBar == null) return;

            var profile = _career.PlayerProfile;
            float score = 0f;
            string rec  = "";

            // ── Salary vs reputation ───────────────────────────────────────
            float repFactor = profile.reputation / 100f;
            float salaryVsExpected = _currentOffer.offeredSalary / (1_000_000 * repFactor * 15f);
            score += Mathf.Clamp(salaryVsExpected * 100f - 50f, -30f, 30f);

            // ── Team performance ───────────────────────────────────────────
            // Higher perf = better (championship contender)
            float teamRating = Mathf.Clamp(_currentOffer.teamBudgetAllocation / 320_000_000f, 0.5f, 1.0f);
            score += teamRating * 20f;

            // ── #1 status bonus ────────────────────────────────────────────
            if (_currentOffer.offersNumberOne) score += 15f;

            // ── Contract length ───────────────────────────────────────────
            score += Mathf.Clamp(_currentOffer.seasons, 1, 3) * 5f;

            score = Mathf.Clamp(score, 0f, 100f);

            if (score >= 75f)
                rec = "🟢 EXCELLENT OPPORTUNITY — Accept immediately!";
            else if (score >= 60f)
                rec = "🟡 SOLID OFFER — Consider accepting or counter.";
            else if (score >= 40f)
                rec = "🔴 MEDIOCRE — You may want to counter or reject.";
            else
                rec = "⛔ POOR OFFER — Strongly recommend rejection.";

            recommendationText.text = rec;
            recommendationBar.fillAmount = score / 100f;
            recommendationBar.color = Color.Lerp(
                new Color(0.91f, 0.00f, 0.18f),
                new Color(0.13f, 0.77f, 0.37f),
                score / 100f
            );
        }

        // ═══════════════════════════════════════════════════════════════════
        //  BUTTON HANDLERS
        // ═══════════════════════════════════════════════════════════════════
        void OnAcceptClicked()
        {
            if (_career == null) return;

            _career.SignContract(_currentOffer);

            if (statusText)
            {
                statusText.text = "✅ CONTRACT SIGNED!";
                statusText.color = new Color(0.13f, 0.77f, 0.37f);
            }

            acceptButton.interactable = false;
            counterButton.interactable = false;
            rejectButton.interactable = false;

            StartCoroutine(DelayedClose(2f));
        }

        void OnCounterClicked()
        {
            if (_isCountering) return;
            _isCountering = true;

            // Generate player counter (10-20% salary increase, +1 year if available)
            float counterMultiplier = Random.Range(1.10f, 1.20f);
            float newSalary = _currentOffer.offeredSalary * counterMultiplier;

            var counter = new ContractOffer
            {
                teamName              = _currentOffer.teamName,
                tier                  = _currentOffer.tier,
                offeredSalary         = newSalary,
                podiumBonus           = _currentOffer.podiumBonus * 1.05f,
                winBonus              = _currentOffer.winBonus * 1.05f,
                seasons               = _currentOffer.seasons + (Random.value > 0.5f ? 1 : 0),
                offersNumberOne       = !_currentOffer.offersNumberOne,
                teamBudgetAllocation  = _currentOffer.teamBudgetAllocation,
                interestLevel         = _currentOffer.interestLevel,
                minReputationRequired = _currentOffer.minReputationRequired,
            };

            if (statusText) statusText.text = "📞 Counter proposed... Waiting for team response...";

            counterButton.interactable = false;
            acceptButton.interactable = false;
            rejectButton.interactable = false;

            StartCoroutine(ResolveCounter(counter));
        }

        IEnumerator ResolveCounter(ContractOffer counter)
        {
            yield return new WaitForSeconds(1.5f);

            // 65% team accepts counter
            bool accepted = Random.value > 0.35f;

            if (accepted)
            {
                _currentOffer = counter;
                if (statusText)
                {
                    statusText.text = "✅ Team accepts your counter!";
                    statusText.color = new Color(0.13f, 0.77f, 0.37f);
                }

                PopulateOffer();

                acceptButton.interactable = true;
                rejectButton.interactable = true;

                StartCoroutine(DelayedClose(2f));
            }
            else
            {
                if (statusText)
                {
                    statusText.text = "❌ Team declined counter. Try again or reject.";
                    statusText.color = new Color(0.91f, 0.00f, 0.18f);
                }

                _isCountering = false;
                counterButton.interactable = true;
                rejectButton.interactable = true;
            }
        }

        void OnRejectClicked()
        {
            if (statusText)
            {
                statusText.text = "❌ Offer rejected. Seeking other opportunities...";
                statusText.color = new Color(0.91f, 0.00f, 0.18f);
            }

            acceptButton.interactable = false;
            counterButton.interactable = false;
            rejectButton.interactable = false;

            StartCoroutine(DelayedClose(1.5f));
        }

        void OnCloseClicked()
        {
            StartCoroutine(FadeCanvasGroup(1, 0, fadeDuration));
        }

        IEnumerator DelayedClose(float delay)
        {
            yield return new WaitForSeconds(delay);
            OnCloseClicked();
        }

        IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;

            if (to == 0) gameObject.SetActive(false);
        }
    }
}