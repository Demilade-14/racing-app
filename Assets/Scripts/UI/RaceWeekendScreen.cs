using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;
using RacingGame.Physics;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  RACE WEEKEND SCREEN  —  2026 Extension
    //  Adds: Full ERS bar with lap budget, overtake cooldown ring,
    //        active aero angle gauge, tyre compound selector with life bars,
    //        VSC/SC status banner, Madrid circuit special panel,
    //        deployment mode labels with kW readout, sprint weekend support.
    //  All original fields and methods retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════
    public class RaceWeekendScreen : MonoBehaviour
    {
        // ── Original fields ───────────────────────────────────────────────
        [Header("Header")]
        [SerializeField] private TMP_Text   circuitNameText;
        [SerializeField] private TMP_Text   roundInfoText;
        [SerializeField] private GameObject new2026Badge;

        [Header("Session Tabs (Horizontal Scroll)")]
        [SerializeField] private Transform  sessionTabsContainer;
        [SerializeField] private GameObject sessionTabPrefab;

        [Header("Practice Programs (Vertical List)")]
        [SerializeField] private GameObject practicePanel;
        [SerializeField] private Transform  practiceList;
        [SerializeField] private GameObject practiceProgramPrefab;

        [Header("Session Actions")]
        [SerializeField] private Button     simulateBtn;
        [SerializeField] private Button     startSessionBtn;
        [SerializeField] private Button     backBtn;

        [Header("ERS Panel (Bottom Sheet)")]
        [SerializeField] private Image      batteryBar;
        [SerializeField] private TMP_Text   batteryPctText;
        [SerializeField] private Button[]   deployModeBtns;
        [SerializeField] private Button     overtakeBtn;
        [SerializeField] private GameObject overtakeActivePanel;

        [Header("Active Aero Panel")]
        [SerializeField] private Button aeroOpenBtn;
        [SerializeField] private Button aeroClosedBtn;

        // ── 2026 new fields ───────────────────────────────────────────────
        [Header("2026 — ERS Extended")]
        [SerializeField] private Image    lapBudgetBar;          // kJ used this lap / 4000
        [SerializeField] private TMP_Text lapBudgetText;         // "2340 / 4000 kJ"
        [SerializeField] private TMP_Text deployKwText;          // "280 kW"
        [SerializeField] private TMP_Text[] deployModeLabelTexts; // Labels on each button
        [SerializeField] private Image    overtakeCooldownRing;  // radial fill
        [SerializeField] private TMP_Text overtakeCooldownText;  // "18s"
        [SerializeField] private TMP_Text overtakeBtnLabel;      // "OVERTAKE" / "COOLDOWN"

        [Header("2026 — Active Aero Gauge")]
        [SerializeField] private Image    aeroAngleBar;          // 0 = open, 1 = closed
        [SerializeField] private TMP_Text aeroAngleText;         // "-2.5°" to "18.0°"
        [SerializeField] private TMP_Text aeroModeLabel;
        [SerializeField] private Button   aeroAutoBtn;
        [SerializeField] private Button   aeroBalancedBtn;
        [SerializeField] private Button   aeroMaxDFBtn;

        [Header("2026 — Tyre Compound Selector")]
        [SerializeField] private Button[]   tyreCompoundBtns;    // S, M, H, I, W
        [SerializeField] private Image[]    tyreBadgeImages;      // colored circles
        [SerializeField] private Image      tyreLifeBar;
        [SerializeField] private TMP_Text   tyreLifeText;
        [SerializeField] private TMP_Text   tyreTempText;

        [Header("2026 — Status Banners")]
        [SerializeField] private GameObject scBanner;
        [SerializeField] private GameObject vscBanner;
        [SerializeField] private TMP_Text   scBannerText;

        [Header("2026 — Madrid Panel")]
        [SerializeField] private GameObject madridInfoPanel;
        [SerializeField] private TMP_Text   madridStrategyText;
        [SerializeField] private TMP_Text   madridTyreWarnText;

        [Header("2026 — Sprint Weekend")]
        [SerializeField] private GameObject sprintTabsPanel;
        [SerializeField] private Button     sprintRaceBtn;
        [SerializeField] private Button     sprintQualiBtn;

        // ── State (original + 2026) ───────────────────────────────────────
        private DriverRaceWeekend weekend;
        private Car2026Data       carData       = new Car2026Data();
        private ERSState2026      ersState      = new ERSState2026();
        private ActiveAeroState   aeroState     = new ActiveAeroState();
        private TyreCompound2026  selectedTyre  = TyreCompound2026.Medium;
        private ActiveAeroMode    aeroMode      = ActiveAeroMode.Auto;

        private string activeSession = "fp1";
        private readonly string[] sessions      = { "fp1", "fp2", "fp3", "qualifying", "race" };
        private readonly string[] sessionLabels = { "FP1", "FP2", "FP3", "QUALI", "RACE" };

        private readonly TyreCompound2026[] tyreOrder =
        {
            TyreCompound2026.Soft, TyreCompound2026.Medium, TyreCompound2026.Hard,
            TyreCompound2026.Intermediate, TyreCompound2026.Wet
        };

        private readonly string[] deployModeLabels = { "HARVEST", "LO", "MED", "HI", "HOTLAP" };
        private readonly DeploymentMode[] deployModes =
        {
            DeploymentMode.Harvest, DeploymentMode.Race_Low, DeploymentMode.Race_Medium,
            DeploymentMode.Race_High, DeploymentMode.Hotlap
        };

        // ── Events ────────────────────────────────────────────────────────
        public System.Action<string>         OnSimulate;
        public System.Action<string>         OnStartSession;
        public System.Action<DeploymentMode> OnDeployModeChanged;
        public System.Action                 OnOvertakeRequested;
        public System.Action<TyreCompound2026> OnTyreSelected;
        public System.Action<ActiveAeroMode> OnAeroModeChanged;
        public System.Action OnBack;

        // ─────────────────────────────────────────────────────────────────
        void Start()
        {
            // Original wiring
            backBtn?.onClick.AddListener(()          => OnBack?.Invoke());
            simulateBtn?.onClick.AddListener(()      => OnSimulate?.Invoke(activeSession));
            startSessionBtn?.onClick.AddListener(()  => OnStartSession?.Invoke(activeSession));
            overtakeBtn?.onClick.AddListener(OnOvertakeTapped);

            // Original deploy modes (extended to 5)
            for (int i = 0; i < deployModeBtns.Length && i < deployModes.Length; i++)
            {
                int idx = i;
                deployModeBtns[i]?.onClick.AddListener(() =>
                {
                    carData.currentDeployMode = deployModes[idx];
                    OnDeployModeChanged?.Invoke(deployModes[idx]);
                    UpdateERSVisuals();
                });
            }

            // Original aero buttons (now extended)
            aeroOpenBtn?.onClick.AddListener(()    => SetAeroMode(ActiveAeroMode.LowDrag));
            aeroClosedBtn?.onClick.AddListener(()  => SetAeroMode(ActiveAeroMode.MaxDownforce));
            aeroAutoBtn?.onClick.AddListener(()    => SetAeroMode(ActiveAeroMode.Auto));
            aeroBalancedBtn?.onClick.AddListener(() => SetAeroMode(ActiveAeroMode.Balanced));
            aeroMaxDFBtn?.onClick.AddListener(()   => SetAeroMode(ActiveAeroMode.MaxDownforce));

            // 2026: tyre compound buttons
            for (int i = 0; i < tyreCompoundBtns.Length && i < tyreOrder.Length; i++)
            {
                int idx = i;
                tyreCompoundBtns[i]?.onClick.AddListener(() => SelectTyre(tyreOrder[idx]));
            }

            // 2026: deploy mode labels
            if (deployModeLabelTexts != null)
                for (int i = 0; i < deployModeLabelTexts.Length && i < deployModeLabels.Length; i++)
                    if (deployModeLabelTexts[i] != null)
                        deployModeLabelTexts[i].text = deployModeLabels[i];

            // 2026: sprint buttons
            sprintRaceBtn?.onClick.AddListener(() => { activeSession = "sprint"; RefreshUI(); });
            sprintQualiBtn?.onClick.AddListener(() => { activeSession = "sprint_quali"; RefreshUI(); });

            // Hide status banners initially
            if (scBanner)  scBanner.SetActive(false);
            if (vscBanner) vscBanner.SetActive(false);

            BuildSessionTabs();
            UpdateERSVisuals();
            UpdateAeroVisuals();
            UpdateTyreVisuals();
        }

        // ── Public API ────────────────────────────────────────────────────
        public void SetData(DriverRaceWeekend weekend, Car2026Data carData)
        {
            this.weekend  = weekend;
            this.carData  = carData ?? new Car2026Data();
            RefreshUI();
        }

        /// <summary>Call from RaceDirector telemetry feed to update live ERS/aero.</summary>
        public void UpdateLiveTelemetry(ERSState2026 ers, ActiveAeroState aero)
        {
            ersState  = ers  ?? ersState;
            aeroState = aero ?? aeroState;
            UpdateERSVisuals();
            UpdateAeroVisuals();
        }

        public void ShowSafetyCarBanner(bool isFull, string message = "")
        {
            if (isFull)
            {
                if (scBanner)    scBanner.SetActive(true);
                if (vscBanner)   vscBanner.SetActive(false);
                if (scBannerText) scBannerText.text = string.IsNullOrEmpty(message) ? "SAFETY CAR DEPLOYED" : message;
            }
            else
            {
                if (scBanner)  scBanner.SetActive(false);
                if (vscBanner) vscBanner.SetActive(true);
            }
        }

        public void HideStatusBanners()
        {
            if (scBanner)  scBanner.SetActive(false);
            if (vscBanner) vscBanner.SetActive(false);
        }

        // ── Original: RefreshUI (extended) ───────────────────────────────
        public void RefreshUI()
        {
            if (weekend == null) return;

            if (circuitNameText) circuitNameText.text = weekend.circuitName ?? "Unknown Circuit";
            if (roundInfoText)   roundInfoText.text   = $"Round {weekend.round} · F1 2026";

            bool isMadrid = weekend.circuitName != null &&
                            weekend.circuitName.ToUpper().Contains("MAD");

            if (new2026Badge)    new2026Badge.SetActive(isMadrid);
            if (madridInfoPanel) madridInfoPanel.SetActive(isMadrid);

            if (isMadrid)
            {
                if (madridStrategyText) madridStrategyText.text =
                    "RECOMMENDED: 2-stop · Soft → Medium → Hard";
                if (madridTyreWarnText) madridTyreWarnText.text =
                    "⚠ High abrasion surface · +45% tyre wear";
            }

            // Sprint weekend: show extra tabs
            if (sprintTabsPanel) sprintTabsPanel.SetActive(weekend.isSprint);

            UpdateSessionTabs();
            UpdatePracticePanel();
            UpdateERSVisuals();
            UpdateAeroVisuals();
            UpdateTyreVisuals();

            bool isDone = IsSessionDone(activeSession);
            if (simulateBtn)    simulateBtn.gameObject.SetActive(!isDone);
            if (startSessionBtn) startSessionBtn.gameObject.SetActive(!isDone);
        }

        // ── 2026: ERS visuals (original expanded) ────────────────────────
        void UpdateERSVisuals()
        {
            float batteryPct = ersState != null ? ersState.BatteryPercent : carData.currentBatteryPercent;

            if (batteryBar)
            {
                batteryBar.fillAmount = batteryPct / 100f;
                batteryBar.color = batteryPct > 60f ? new Color(0.13f, 0.77f, 0.37f)
                                 : batteryPct > 30f ? new Color(0.98f, 0.75f, 0.14f)
                                 : new Color(0.91f, 0f, 0.18f);
            }
            if (batteryPctText) batteryPctText.text = $"{batteryPct:F0}%";

            // 2026: lap budget bar
            if (lapBudgetBar && ersState != null)
            {
                float budgetUsed = ersState.lapDeployKJ / 4000f;
                lapBudgetBar.fillAmount = budgetUsed;
                lapBudgetBar.color = budgetUsed > 0.85f ? new Color(0.91f, 0f, 0.18f)
                                   : budgetUsed > 0.6f  ? new Color(0.98f, 0.75f, 0.14f)
                                   : new Color(0.38f, 0.65f, 0.98f);
            }
            if (lapBudgetText && ersState != null)
                lapBudgetText.text = ersState.deployLimitReached
                    ? "LIMIT REACHED"
                    : $"{ersState.lapDeployKJ:F0} / 4000 kJ";

            if (deployKwText && ersState != null)
                deployKwText.text = $"{ersState.currentDeployKw:F0} kW";

            // Overtake button state
            bool canOvertake = ersState == null ||
                               (!ersState.overtakeModeActive &&
                                ersState.overtakeCooldown <= 0f &&
                                ersState.storedEnergyKJ >= 300f);

            if (overtakeBtn) overtakeBtn.interactable = canOvertake;

            if (overtakeCooldownRing && ersState != null)
                overtakeCooldownRing.fillAmount = ersState.overtakeCooldown > 0f
                    ? ersState.overtakeCooldown / 25f : 0f;

            if (overtakeCooldownText && ersState != null)
                overtakeCooldownText.text = ersState.overtakeCooldown > 0f
                    ? $"{ersState.overtakeCooldown:F0}s" : "";

            if (overtakeBtnLabel)
                overtakeBtnLabel.text = (ersState != null && ersState.overtakeModeActive)
                    ? "ACTIVE" : "OVERTAKE";

            if (overtakeActivePanel)
                overtakeActivePanel.SetActive(ersState?.overtakeModeActive ?? carData.overtakeModeActive);

            // Highlight active deploy button (original logic, extended to 5 modes)
            for (int i = 0; i < deployModeBtns.Length && i < deployModes.Length; i++)
            {
                if (deployModeBtns[i] == null) continue;
                bool isActive = carData.currentDeployMode == deployModes[i];
                var img = deployModeBtns[i].GetComponent<Image>();
                if (img) img.color = isActive
                    ? GetDeployModeColor(deployModes[i])
                    : new Color(0.15f, 0.15f, 0.15f);
            }
        }

        // ── 2026: Active Aero visuals ─────────────────────────────────────
        void UpdateAeroVisuals()
        {
            float angle = aeroState?.currentAngleDeg ?? 6f;
            float t     = Mathf.InverseLerp(-2.5f, 18f, angle);

            if (aeroAngleBar)
            {
                aeroAngleBar.fillAmount = t;
                aeroAngleBar.color = Color.Lerp(new Color(0.13f, 0.77f, 0.37f), new Color(0.91f, 0f, 0.18f), t);
            }
            if (aeroAngleText)  aeroAngleText.text  = $"{angle:F1}°";
            if (aeroModeLabel)  aeroModeLabel.text  = aeroMode switch
            {
                ActiveAeroMode.LowDrag      => "LOW DRAG",
                ActiveAeroMode.Balanced     => "BALANCED",
                ActiveAeroMode.MaxDownforce => "MAX DF",
                _                           => "AUTO"
            };
        }

        void SetAeroMode(ActiveAeroMode mode)
        {
            aeroMode = mode;
            OnAeroModeChanged?.Invoke(mode);
            UpdateAeroVisuals();
        }

        // ── 2026: Tyre visuals ────────────────────────────────────────────
        void UpdateTyreVisuals()
        {
            for (int i = 0; i < tyreBadgeImages.Length && i < tyreOrder.Length; i++)
            {
                if (tyreBadgeImages[i] == null) continue;
                tyreBadgeImages[i].color = GetTyreColor(tyreOrder[i]);

                // Highlight selected
                tyreBadgeImages[i].transform.localScale =
                    tyreOrder[i] == selectedTyre ? Vector3.one * 1.15f : Vector3.one;
            }

            // Life and temp from carData
            if (tyreLifeBar)  tyreLifeBar.fillAmount  = carData.tyreLifePercent / 100f;
            if (tyreLifeText) tyreLifeText.text        = $"{carData.tyreLifePercent:F0}%";
            if (tyreTempText) tyreTempText.text        = $"{carData.tyreTemp:F0}°C";

            if (tyreLifeBar)
                tyreLifeBar.color = carData.tyreLifePercent < 15f ? new Color(0.91f, 0f, 0.18f)
                                  : carData.tyreLifePercent < 35f ? new Color(0.98f, 0.75f, 0.14f)
                                  : new Color(0.13f, 0.77f, 0.37f);
        }

        void SelectTyre(TyreCompound2026 compound)
        {
            selectedTyre = compound;
            OnTyreSelected?.Invoke(compound);
            UpdateTyreVisuals();
        }

        // ── 2026: Overtake tap ────────────────────────────────────────────
        void OnOvertakeTapped()
        {
            if (ersState != null && ersState.overtakeCooldown > 0f)
            {
                // Visual feedback: flash the cooldown ring red
                StartCoroutine(FlashCooldownRing());
                return;
            }
            OnOvertakeRequested?.Invoke();
        }

        IEnumerator FlashCooldownRing()
        {
            if (overtakeCooldownRing == null) yield break;
            Color original = overtakeCooldownRing.color;
            overtakeCooldownRing.color = new Color(0.91f, 0f, 0.18f);
            yield return new WaitForSeconds(0.2f);
            overtakeCooldownRing.color = original;
        }

        // ── Original: BuildSessionTabs (unchanged) ────────────────────────
        void BuildSessionTabs()
        {
            if (sessionTabsContainer == null || sessionTabPrefab == null) return;
            foreach (Transform child in sessionTabsContainer) Destroy(child.gameObject);

            for (int i = 0; i < sessions.Length; i++)
            {
                var tab  = Instantiate(sessionTabPrefab, sessionTabsContainer);
                var text = tab.GetComponentInChildren<TMP_Text>();
                if (text) text.text = sessionLabels[i];
                string sessionId = sessions[i];
                tab.GetComponent<Button>()?.onClick.AddListener(() => { activeSession = sessionId; RefreshUI(); });
            }
        }

        void UpdateSessionTabs()
        {
            if (sessionTabsContainer == null) return;
            for (int i = 0; i < sessionTabsContainer.childCount && i < sessions.Length; i++)
            {
                var tab = sessionTabsContainer.GetChild(i).gameObject;
                bool isActive = sessions[i] == activeSession;
                bool isDone   = IsSessionDone(sessions[i]);
                var img = tab.GetComponent<Image>();
                if (img) img.color = isActive ? new Color(0.91f, 0f, 0.18f) : new Color(0.1f, 0.1f, 0.1f);
                var badge = tab.transform.Find("DoneBadge");
                if (badge) badge.SetActive(isDone);
            }
        }

        bool IsSessionDone(string session)
        {
            if (weekend == null) return false;
            return session switch
            {
                "fp1"         => weekend.fp1Done,
                "fp2"         => weekend.fp2Done,
                "fp3"         => weekend.fp3Done,
                "qualifying"  => weekend.qualiDone,
                "race"        => weekend.IsComplete,
                _             => false
            };
        }

        // ── Original: practice panel (unchanged) ─────────────────────────
        void UpdatePracticePanel()
        {
            bool showPractice = activeSession == "fp1" || activeSession == "fp2" || activeSession == "fp3";
            if (practicePanel) practicePanel.SetActive(showPractice);
            if (!showPractice || practiceList == null || practiceProgramPrefab == null) return;

            foreach (Transform child in practiceList) Destroy(child.gameObject);
            if (weekend.practicePrograms == null) return;

            foreach (var program in weekend.practicePrograms)
            {
                var row      = Instantiate(practiceProgramPrefab, practiceList);
                var texts    = row.GetComponentsInChildren<TMP_Text>();
                var checkbox = row.transform.Find("Checkbox")?.GetComponent<Image>();

                if (texts.Length >= 2)
                {
                    texts[0].text = program.name;
                    texts[1].text = $"+{program.xpReward:F0} XP ({program.statRewarded})";
                }
                if (checkbox)
                    checkbox.color = program.completed
                        ? new Color(0.13f, 0.77f, 0.37f)
                        : new Color(0.3f, 0.3f, 0.3f);

                if (!program.completed)
                {
                    var captured = program;
                    row.GetComponent<Button>()?.onClick.AddListener(() =>
                    { captured.completed = true; RefreshUI(); });
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        Color GetDeployModeColor(DeploymentMode mode) => mode switch
        {
            DeploymentMode.Harvest    => new Color(0f,    0.4f,  0.8f),
            DeploymentMode.Race_Low   => new Color(0.1f,  0.5f,  0.2f),
            DeploymentMode.Race_Medium=> new Color(0.12f, 0.23f, 0.12f),
            DeploymentMode.Race_High  => new Color(0.8f,  0.5f,  0f),
            DeploymentMode.Hotlap    => new Color(0.91f, 0f,    0.18f),
            _                         => new Color(0.3f,  0.3f,  0.3f)
        };

        Color GetTyreColor(TyreCompound2026 compound) => compound switch
        {
            TyreCompound2026.Soft         => new Color(1.00f, 0.20f, 0.20f),
            TyreCompound2026.Medium       => new Color(1.00f, 0.85f, 0.00f),
            TyreCompound2026.Hard         => new Color(0.90f, 0.90f, 0.90f),
            TyreCompound2026.Intermediate => new Color(0.10f, 0.80f, 0.30f),
            TyreCompound2026.Wet          => new Color(0.20f, 0.50f, 1.00f),
            _                             => Color.white
        };
    }
}