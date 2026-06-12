using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RacingGame.Career;
using RacingGame.Data;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CONTRACT OFFICE / TRANSFER MARKET UI
    //  Shows available contract offers during transfer window.
    //  Player can view, evaluate, and sign contracts from interested teams.
    //  Wires to DriverCareerManager and TeamInterestTracker.
    // ═══════════════════════════════════════════════════════════════════════
    public class TransferMarketUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] TextMeshProUGUI        titleText;
        [SerializeField] TextMeshProUGUI        reputationText;
        [SerializeField] TextMeshProUGUI        balanceText;

        [Header("Filters")]
        [SerializeField] Button                 filterAllButton;
        [SerializeField] Button                 filterHotButton;
        [SerializeField] Button                 filterWarmButton;
        [SerializeField] Button                 filterColdButton;

        [Header("Sort")]
        [SerializeField] Button                 sortBySalaryButton;
        [SerializeField] Button                 sortByTeamButton;
        [SerializeField] Button                 sortByInterestButton;

        [Header("List")]
        [SerializeField] Transform              offersListRoot;
        [SerializeField] GameObject             offerRowPrefab;

        [Header("Detail Panel")]
        [SerializeField] GameObject             detailPanel;
        [SerializeField] TextMeshProUGUI        detailTeamNameText;
        [SerializeField] TextMeshProUGUI        detailPerformanceText;
        [SerializeField] TextMeshProUGUI        detailSalaryText;
        [SerializeField] TextMeshProUGUI        detailBonusesText;
        [SerializeField] TextMeshProUGUI        detailDurationText;
        [SerializeField] TextMeshProUGUI        detailNumberOneText;
        [SerializeField] Button                 detailAcceptButton;
        [SerializeField] Button                 detailNegotiateButton;
        [SerializeField] Button                 detailRejectButton;
        [SerializeField] Button                 detailCloseButton;

        [Header("News Feed")]
        [SerializeField] TextMeshProUGUI        newsText;

        // ── Private state ─────────────────────────────────────────────────
        DriverCareerManager _career;
        List<ContractOffer> _allOffers          = new();
        List<ContractOffer> _displayedOffers    = new();
        ContractOffer       _selectedOffer;

        string _activeFilter = "all";    // all | hot | warm | cold
        string _activeSort   = "salary"; // salary | team | interest

        // ═══════════════════════════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════
        void Start()
        {
            _career = DriverCareerManager.Instance;
            BindAllButtons();
            HideDetailPanel();
        }

        void OnEnable()
        {
            RefreshOffers();
        }

        void OnDestroy()
        {
            UnbindAllButtons();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  BUTTON BINDING
        // ═══════════════════════════════════════════════════════════════════
        void BindAllButtons()
        {
            // Filter buttons
            filterAllButton?.onClick.AddListener(() => SetFilter("all"));
            filterHotButton?.onClick.AddListener(() => SetFilter("hot"));
            filterWarmButton?.onClick.AddListener(() => SetFilter("warm"));
            filterColdButton?.onClick.AddListener(() => SetFilter("cold"));

            // Sort buttons
            sortBySalaryButton?.onClick.AddListener(() => SetSort("salary"));
            sortByTeamButton?.onClick.AddListener(() => SetSort("team"));
            sortByInterestButton?.onClick.AddListener(() => SetSort("interest"));

            // Detail panel buttons
            detailAcceptButton?.onClick.AddListener(OnAcceptOffer);
            detailNegotiateButton?.onClick.AddListener(OnNegotiateOffer);
            detailRejectButton?.onClick.AddListener(OnRejectOffer);
            detailCloseButton?.onClick.AddListener(HideDetailPanel);
        }

        void UnbindAllButtons()
        {
            filterAllButton?.onClick.RemoveAllListeners();
            filterHotButton?.onClick.RemoveAllListeners();
            filterWarmButton?.onClick.RemoveAllListeners();
            filterColdButton?.onClick.RemoveAllListeners();

            sortBySalaryButton?.onClick.RemoveAllListeners();
            sortByTeamButton?.onClick.RemoveAllListeners();
            sortByInterestButton?.onClick.RemoveAllListeners();

            detailAcceptButton?.onClick.RemoveAllListeners();
            detailNegotiateButton?.onClick.RemoveAllListeners();
            detailRejectButton?.onClick.RemoveAllListeners();
            detailCloseButton?.onClick.RemoveAllListeners();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  REFRESH & POPULATE
        // ═══════════════════════════════════════════════════════════════════
        public void RefreshOffers()
        {
            if (_career == null) return;

            UpdateHeader();
            FetchOffersFromManager();
            ApplyFilterAndSort();
            RenderOffersList();
        }

        void UpdateHeader()
        {
            var profile = _career.PlayerProfile;
            if (profile == null) return;

            if (titleText) titleText.text = "CONTRACT OFFICE";
            if (reputationText)
                reputationText.text = $"{profile.reputationTier.ToUpper()} · Reputation {profile.reputation:F0}/100";
            if (balanceText)
                balanceText.text = $"Balance: ${profile.balance / 1_000_000:F1}M";
        }

        void FetchOffersFromManager()
        {
            _allOffers.Clear();

            // Fetch from DriverCareerManager's contract offer system
            // In real flow: OnContractOffersReady event from DriverCareerManager populates this
            // For now, we'll build a basic list from TeamInterestTracker

            if (_career?.TeamInterest == null)
            {
                Debug.LogWarning("[TransferMarketUI] No TeamInterestTracker available");
                return;
            }

            var allTeams = _career.TeamInterest.GetAll();
            foreach (var entry in allTeams)
            {
                if (entry.interest == InterestLevel.None) continue;

                var offer = new ContractOffer
                {
                    teamName              = entry.teamName,
                    tier                  = SeriesTier.Formula1,
                    offeredSalary         = entry.estimatedSalaryMin + 
                                           (entry.estimatedSalaryMax - entry.estimatedSalaryMin) * 0.5f,
                    podiumBonus           = entry.estimatedSalaryMin * 0.10f,
                    winBonus              = entry.estimatedSalaryMin * 0.08f,
                    seasons               = entry.interest == InterestLevel.Hot ? 2 : 1,
                    offersNumberOne       = entry.interest == InterestLevel.Hot,
                    interestLevel         = entry.interest,
                    teamBudgetAllocation  = entry.estimatedSalaryMax,
                    minReputationRequired = 0f,
                };

                _allOffers.Add(offer);
            }

            Debug.Log($"[TransferMarketUI] Loaded {_allOffers.Count} contract offers");
        }

        void ApplyFilterAndSort()
        {
            // ── Filter ────────────────────────────────────────────────────
            _displayedOffers = _activeFilter switch
            {
                "hot"  => _allOffers.Where(o => o.interestLevel == InterestLevel.Hot).ToList(),
                "warm" => _allOffers.Where(o => o.interestLevel == InterestLevel.Warm).ToList(),
                "cold" => _allOffers.Where(o => o.interestLevel == InterestLevel.Cold).ToList(),
                _      => new List<ContractOffer>(_allOffers)
            };

            // ── Sort ──────────────────────────────────────────────────────
            _displayedOffers = _activeSort switch
            {
                "salary"   => _displayedOffers.OrderByDescending(o => o.offeredSalary).ToList(),
                "team"     => _displayedOffers.OrderBy(o => o.teamName).ToList(),
                "interest" => _displayedOffers.OrderByDescending(o => o.interestLevel).ToList(),
                _          => _displayedOffers
            };
        }

        void RenderOffersList()
        {
            if (offersListRoot == null) return;

            // Clear old rows
            foreach (Transform child in offersListRoot)
                Destroy(child.gameObject);

            if (_displayedOffers.Count == 0)
            {
                if (newsText) newsText.text = "No contract offers available.";
                return;
            }

            // Render each offer as a clickable row
            foreach (var offer in _displayedOffers)
            {
                var row = Instantiate(offerRowPrefab, offersListRoot);
                var button = row.GetComponent<Button>();
                if (button)
                {
                    button.onClick.AddListener(() => ShowDetailPanel(offer));
                }

                // Bind row data
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 4)
                {
                    texts[0].text = offer.teamName;
                    texts[1].text = $"${offer.offeredSalary / 1_000_000:F1}M";
                    texts[2].text = $"{offer.seasons}yr";
                    texts[3].text = offer.interestLevel switch
                    {
                        InterestLevel.Hot  => "🔥 HOT",
                        InterestLevel.Warm => "⭐ WARM",
                        _                  => "❄️ COLD"
                    };
                }

                // Color by interest level
                var img = row.GetComponent<Image>();
                if (img)
                {
                    img.color = offer.interestLevel switch
                    {
                        InterestLevel.Hot  => new Color(1.0f, 0.3f, 0.3f, 0.15f),
                        InterestLevel.Warm => new Color(1.0f, 0.6f, 0.0f, 0.10f),
                        _                  => new Color(0.5f, 0.5f, 1.0f, 0.05f)
                    };
                }
            }

            if (newsText) newsText.text = $"Showing {_displayedOffers.Count} of {_allOffers.Count} offers";
        }

        // ═══════════════════════════════════════════════════════════════════
        //  FILTER & SORT
        // ═══════════════════════════════════════════════════════════════════
        void SetFilter(string filterKey)
        {
            _activeFilter = filterKey;
            RefreshOffers();
            Debug.Log($"[TransferMarketUI] Filter: {filterKey}");
        }

        void SetSort(string sortKey)
        {
            _activeSort = sortKey;
            RefreshOffers();
            Debug.Log($"[TransferMarketUI] Sort: {sortKey}");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  DETAIL PANEL
        // ═══════════════════════════════════════════════════════════════════
        void ShowDetailPanel(ContractOffer offer)
        {
            if (detailPanel == null) return;

            _selectedOffer = offer;
            detailPanel.SetActive(true);

            // Populate detail fields
            if (detailTeamNameText) detailTeamNameText.text = offer.teamName;
            if (detailPerformanceText)
                detailPerformanceText.text = $"Budget: ${offer.teamBudgetAllocation / 1_000_000:F0}M";

            if (detailSalaryText)
                detailSalaryText.text = $"${offer.offeredSalary / 1_000_000:F1}M/year";

            if (detailBonusesText)
                detailBonusesText.text =
                    $"Podium: ${offer.podiumBonus / 1_000_000:F2}M | Win: ${offer.winBonus / 1_000_000:F2}M";

            if (detailDurationText)
                detailDurationText.text = $"{offer.seasons} year{(offer.seasons > 1 ? "s" : "")}";

            if (detailNumberOneText)
            {
                detailNumberOneText.text = offer.offersNumberOne
                    ? "✅ #1 DRIVER STATUS"
                    : "❌ #2 DEVELOPMENT ROLE";
            }
        }

        void HideDetailPanel()
        {
            if (detailPanel) detailPanel.SetActive(false);
            _selectedOffer = null;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  DETAIL PANEL ACTIONS
        // ═══════════════════════════════════════════════════════════════════
        void OnAcceptOffer()
        {
            if (_career == null || _selectedOffer == null) return;

            _career.SignContract(_selectedOffer);

            if (newsText)
                newsText.text = $"✅ Signed with {_selectedOffer.teamName}!";

            HideDetailPanel();
            RefreshOffers();

            Debug.Log($"[TransferMarketUI] Contract accepted: {_selectedOffer.teamName}");
        }

        void OnNegotiateOffer()
        {
            if (_selectedOffer == null) return;

            // Route to ContractNegotiationUI
            var negotiationUI = GetComponent<ContractNegotiationUI>();
            if (negotiationUI)
            {
                negotiationUI.ShowContractOffer(_selectedOffer);
                HideDetailPanel();
            }
            else
            {
                Debug.LogWarning("[TransferMarketUI] ContractNegotiationUI not found");
            }
        }

        void OnRejectOffer()
        {
            if (_selectedOffer == null) return;

            if (newsText)
                newsText.text = $"❌ Rejected offer from {_selectedOffer.teamName}";

            _allOffers.Remove(_selectedOffer);
            HideDetailPanel();
            RefreshOffers();

            Debug.Log($"[TransferMarketUI] Offer rejected: {_selectedOffer.teamName}");
        }
    }
}