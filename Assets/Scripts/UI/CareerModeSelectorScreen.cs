using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RacingGame.Career;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER MODE SELECTOR SCREEN  –  2026 Extension
    //  Adds: Single Season card, 2026 regulation badge, animated card
    //        selection highlight, season rules info panel, Audi/Cadillac
    //        availability notice, coroutine-driven intro animation.
    //  All original fields and methods retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════
    public class CareerModeSelectorScreen : MonoBehaviour
    {
        // ── Original fields ───────────────────────────────────────────────
        [Header("UI References - Assign in Inspector")]
        [SerializeField] private GameObject driverCard;
        [SerializeField] private GameObject managerCard;
        [SerializeField] private Button     driverNewButton;
        [SerializeField] private Button     driverLoadButton;
        [SerializeField] private Button     managerNewButton;
        [SerializeField] private Button     managerLoadButton;

        [Header("Visual Effects")]
        [SerializeField] private Image driverAccentBar;
        [SerializeField] private Image managerAccentBar;

        [Header("Colors")]
        [SerializeField] private Color driverAccentColor  = new Color(0.91f, 0f,   0.18f); // #E8002D
        [SerializeField] private Color managerAccentColor = new Color(0f,    0.4f, 0.8f);  // #0066CC

        // ── 2026 new fields ───────────────────────────────────────────────
        [Header("— 2026: Single Season Card —")]
        [SerializeField] private GameObject singleSeasonCard;
        [SerializeField] private Button     singleSeasonNewButton;
        [SerializeField] private Image      singleSeasonAccentBar;
        [SerializeField] private Color      singleSeasonAccentColor = new Color(0.1f, 0.75f, 0.4f);

        [Header("— 2026: Regulation Badge —")]
        [SerializeField] private GameObject regulationBadge;       // "2026 REGS" pill
        [SerializeField] private TMP_Text   regulationBadgeText;
        [SerializeField] private GameObject audiCadillacNotice;    // "Audi & Cadillac available"

        [Header("— 2026: Info Panel —")]
        [SerializeField] private GameObject infoPanel;             // bottom-sheet or overlay
        [SerializeField] private TMP_Text   infoPanelText;
        [SerializeField] private Button     infoPanelCloseBtn;
        [SerializeField] private Button     driverInfoBtn;         // "?" next to driver card
        [SerializeField] private Button     seasonInfoBtn;         // "?" next to single-season card

        [Header("— 2026: Selected Card Highlight —")]
        [SerializeField] private Image      selectionGlow;         // full-panel glow overlay
        [SerializeField] private TMP_Text   selectedCardLabel;     // e.g. "DRIVER CAREER SELECTED"

        [Header("— 2026: Intro Animation —")]
        [SerializeField] private CanvasGroup rootCanvasGroup;      // for fade-in
        [SerializeField] private float       introDuration = 0.45f;

        // ── State ─────────────────────────────────────────────────────────
        private CareerModeType _selectedMode = CareerModeType.Driver;
        private bool           _infoPanelOpen = false;

        // ── Events ────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when player selects a mode.
        /// Parameters: (mode, action) where action is "new" or "load".
        /// </summary>
        public System.Action<CareerModeType, string> OnModeSelected;

        // ─────────────────────────────────────────────────────────────────
        void Start()
        {
            // ── Original button wiring ────────────────────────────────────
            if (driverNewButton  != null) driverNewButton.onClick.AddListener(()  => SelectAndFire(CareerModeType.Driver,  "new"));
            if (driverLoadButton != null) driverLoadButton.onClick.AddListener(() => SelectAndFire(CareerModeType.Driver,  "load"));
            if (managerNewButton != null) managerNewButton.onClick.AddListener(() => SelectAndFire(CareerModeType.Manager, "new"));
            if (managerLoadButton!= null) managerLoadButton.onClick.AddListener(()=> SelectAndFire(CareerModeType.Manager, "load"));

            // ── 2026: Single Season ───────────────────────────────────────
            if (singleSeasonNewButton != null)
                singleSeasonNewButton.onClick.AddListener(() => SelectAndFire(CareerModeType.SingleSeason, "new"));

            // ── 2026: Info panel ──────────────────────────────────────────
            if (driverInfoBtn   != null) driverInfoBtn.onClick.AddListener(() => OpenInfoPanel(CareerModeType.Driver));
            if (seasonInfoBtn   != null) seasonInfoBtn.onClick.AddListener(() => OpenInfoPanel(CareerModeType.SingleSeason));
            if (infoPanelCloseBtn != null) infoPanelCloseBtn.onClick.AddListener(CloseInfoPanel);

            // ── Hover effects (original) ──────────────────────────────────
            SetupCardHover(driverCard,       driverAccentBar,       driverAccentColor);
            SetupCardHover(managerCard,      managerAccentBar,      managerAccentColor);
            SetupCardHover(singleSeasonCard, singleSeasonAccentBar, singleSeasonAccentColor);

            // ── 2026: Regulation badge ────────────────────────────────────
            if (regulationBadge)    regulationBadge.SetActive(true);
            if (regulationBadgeText) regulationBadgeText.text = "2026 REGULATIONS ACTIVE";
            if (audiCadillacNotice) audiCadillacNotice.SetActive(true);

            // ── Info panel starts hidden ──────────────────────────────────
            if (infoPanel) infoPanel.SetActive(false);

            // ── Initial selection highlight ───────────────────────────────
            UpdateSelectionVisuals();

            // ── Intro animation ───────────────────────────────────────────
            if (rootCanvasGroup != null)
                StartCoroutine(FadeIn());
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: SELECT & FIRE
        // ═════════════════════════════════════════════════════════════════

        void SelectAndFire(CareerModeType mode, string action)
        {
            _selectedMode = mode;
            UpdateSelectionVisuals();

            // Manager is locked — show info instead of firing
            if (mode == CareerModeType.Manager && action == "new")
            {
                OpenInfoPanel(CareerModeType.Manager);
                return;
            }

            OnModeSelected?.Invoke(mode, action);
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: SELECTION HIGHLIGHT
        // ═════════════════════════════════════════════════════════════════

        void UpdateSelectionVisuals()
        {
            // Dim all cards, highlight selected
            SetCardSelected(driverCard,       _selectedMode == CareerModeType.Driver);
            SetCardSelected(managerCard,      _selectedMode == CareerModeType.Manager);
            SetCardSelected(singleSeasonCard, _selectedMode == CareerModeType.SingleSeason);

            if (selectedCardLabel)
            {
                selectedCardLabel.text = _selectedMode switch
                {
                    CareerModeType.Driver       => "DRIVER CAREER SELECTED",
                    CareerModeType.Manager      => "TEAM PRINCIPAL SELECTED",
                    CareerModeType.SingleSeason => "SINGLE SEASON SELECTED",
                    _                           => ""
                };
            }
        }

        void SetCardSelected(GameObject card, bool selected)
        {
            if (card == null) return;
            var img = card.GetComponent<Image>();
            if (img == null) return;

            // Selected: slightly brighter background tint
            img.color = selected
                ? new Color(0.18f, 0.05f, 0.04f, 1f)
                : new Color(0.09f, 0.09f, 0.10f, 1f);
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: INFO PANEL
        // ═════════════════════════════════════════════════════════════════

        void OpenInfoPanel(CareerModeType mode)
        {
            if (infoPanel == null) return;

            string info = mode switch
            {
                CareerModeType.Driver =>
                    "DRIVER CAREER\n\n" +
                    "• Start in F3, F2, or jump straight to F1.\n" +
                    "• Build your driver through training, media events, and rivalries.\n" +
                    "• Negotiate contracts with all 12 teams — including Audi and Cadillac.\n" +
                    "• 2026 Active Aero, ERS Overtake Mode, and Madrid Circuit included.\n" +
                    "• Career spans up to 20 seasons with full legacy tracking.",

                CareerModeType.SingleSeason =>
                    "SINGLE SEASON\n\n" +
                    "• Skip the career ladder — jump straight into F1.\n" +
                    "• Choose any team including Audi and Cadillac.\n" +
                    "• Full 24-race 2026 calendar including Madrid.\n" +
                    "• Progress does not carry over to a full career.",

                CareerModeType.Manager =>
                    "TEAM PRINCIPAL — COMING SOON\n\n" +
                    "• Hire and fire drivers from the transfer market.\n" +
                    "• Manage R&D, budget, and pit crew.\n" +
                    "• Fight for the Constructor's Championship.\n\n" +
                    "This mode is under development.",

                _ => ""
            };

            if (infoPanelText) infoPanelText.text = info;
            infoPanel.SetActive(true);
            _infoPanelOpen = true;
        }

        void CloseInfoPanel()
        {
            if (infoPanel) infoPanel.SetActive(false);
            _infoPanelOpen = false;
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: INTRO FADE-IN
        // ═════════════════════════════════════════════════════════════════

        IEnumerator FadeIn()
        {
            rootCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < introDuration)
            {
                elapsed += Time.deltaTime;
                rootCanvasGroup.alpha = Mathf.Clamp01(elapsed / introDuration);
                yield return null;
            }
            rootCanvasGroup.alpha = 1f;
        }

        // ═════════════════════════════════════════════════════════════════
        //  ORIGINAL — Hover effects (unchanged)
        // ═════════════════════════════════════════════════════════════════

        void SetupCardHover(GameObject card, Image accentBar, Color accentColor)
        {
            if (card == null || accentBar == null) return;

            var trigger = card.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
                trigger = card.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) =>
            {
                card.transform.localScale = Vector3.one * 1.02f;
                accentBar.color = accentColor;
                var rect = accentBar.rectTransform;
                rect.sizeDelta = new Vector2(420f, rect.sizeDelta.y);
            });
            trigger.triggers.Add(enterEntry);

            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) =>
            {
                card.transform.localScale = Vector3.one;
                accentBar.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
                var rect = accentBar.rectTransform;
                rect.sizeDelta = new Vector2(168f, rect.sizeDelta.y);
            });
            trigger.triggers.Add(exitEntry);
        }

        // ═════════════════════════════════════════════════════════════════
        //  ORIGINAL — OnDestroy (extended with new buttons)
        // ═════════════════════════════════════════════════════════════════

        void OnDestroy()
        {
            if (driverNewButton   != null) driverNewButton.onClick.RemoveAllListeners();
            if (driverLoadButton  != null) driverLoadButton.onClick.RemoveAllListeners();
            if (managerNewButton  != null) managerNewButton.onClick.RemoveAllListeners();
            if (managerLoadButton != null) managerLoadButton.onClick.RemoveAllListeners();

            // 2026 new
            if (singleSeasonNewButton != null) singleSeasonNewButton.onClick.RemoveAllListeners();
            if (infoPanelCloseBtn     != null) infoPanelCloseBtn.onClick.RemoveAllListeners();
            if (driverInfoBtn         != null) driverInfoBtn.onClick.RemoveAllListeners();
            if (seasonInfoBtn         != null) seasonInfoBtn.onClick.RemoveAllListeners();
        }
    }
}