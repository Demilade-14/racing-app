using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;
using RacingGame.Data;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER CREATION SCREEN  —  2026 Extension
    //  Adds: Step 4 (Archetype), F3 starting tier, ERS skill stat bar,
    //        Tyre Management stat bar, 2026 team full roster in team step,
    //        Audi/Cadillac NEW badges, live driver code preview,
    //        starting reputation display.
    //  All original fields, methods, and step logic retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════
    public class DriverCreationScreen : MonoBehaviour
    {
        // ── Original fields ───────────────────────────────────────────────
        [Header("Step Panels - Assign in Inspector")]
        [SerializeField] private GameObject step0Panel;
        [SerializeField] private GameObject step1Panel;
        [SerializeField] private GameObject step2Panel;
        [SerializeField] private GameObject step3Panel;

        [Header("Navigation")]
        [SerializeField] private Button   backButton;
        [SerializeField] private Button   prevButton;
        [SerializeField] private Button   nextButton;
        [SerializeField] private TMP_Text stepIndicatorText;
        [SerializeField] private Image[]  progressBars;

        [Header("Step 0: Driver Type")]
        [SerializeField] private Button customDriverButton;
        [SerializeField] private Button iconDriverButton;
        [SerializeField] private Button realDriverButton;

        [Header("Step 1: Custom Input Fields")]
        [SerializeField] private TMP_InputField driverNameInput;
        [SerializeField] private TMP_InputField raceNumberInput;
        [SerializeField] private TMP_Dropdown   nationalityDropdown;
        [SerializeField] private ColorPicker    helmetColorPicker;
        [SerializeField] private ColorPicker    suitColorPicker;

        [Header("Step 1: Icon Selection")]
        [SerializeField] private GameObject iconSelectionPanel;
        [SerializeField] private GameObject customInputPanel;
        [SerializeField] private Button[]   iconButtons;
        [SerializeField] private TMP_Text[] iconNameTexts;

        [Header("Step 1: Stat Preview")]
        [SerializeField] private Image    paceBar;
        [SerializeField] private Image    awarenessBar;
        [SerializeField] private Image    racecraftBar;
        [SerializeField] private Image    experienceBar;
        [SerializeField] private TMP_Text ovrText;

        [Header("Step 2: Team Selection")]
        [SerializeField] private Transform  teamGrid;
        [SerializeField] private GameObject teamCardPrefab;

        [Header("Step 3: Series Selection")]
        [SerializeField] private Button f2Button;
        [SerializeField] private Button f1Button;

        // ── 2026 new fields ───────────────────────────────────────────────
        [Header("2026 — Step 4: Archetype Panel")]
        [SerializeField] private GameObject step4Panel;
        [SerializeField] private Button[]   archetypeButtons;  // 4: Balanced, Overtaker, Qualifier, TyreWhisperer
        [SerializeField] private TMP_Text   archetypeNameText;
        [SerializeField] private TMP_Text   archetypeDescText;

        [Header("2026 — Extra Stat Bars (Step 1)")]
        [SerializeField] private Image    ersSkillBar;
        [SerializeField] private Image    tyreManagementBar;

        [Header("2026 — Series: F3 Button")]
        [SerializeField] private Button   f3Button;

        [Header("2026 — Live Driver Code Preview")]
        [SerializeField] private TMP_Text driverCodePreviewText;
        [SerializeField] private TMP_Text startingRepText;

        [Header("2026 — Team Card: New Badge Prefab")]
        [SerializeField] private GameObject newTeamBadgePrefab;

        // ── State ─────────────────────────────────────────────────────────
        private int           currentStep = 0;
        private int           totalSteps  = 5;   // was 4, now 5 with archetype
        private DriverProfile profile     = new DriverProfile();
        private List<TeamData2026> availableTeams = new List<TeamData2026>();
        private DriverArchetype _selectedArchetype = DriverArchetype.Balanced;

        private readonly DriverIcon[] legendaryIcons = new DriverIcon[]
        {
            new DriverIcon { name = "Ayrton Senna",        pace = 99, awareness = 97, racecraft = 99, experience = 90 },
            new DriverIcon { name = "Michael Schumacher",  pace = 98, awareness = 96, racecraft = 98, experience = 99 },
            new DriverIcon { name = "Alain Prost",         pace = 95, awareness = 99, racecraft = 96, experience = 97 },
            new DriverIcon { name = "Niki Lauda",          pace = 93, awareness = 98, racecraft = 95, experience = 96 }
        };

        private readonly string[] nationalities = new string[]
        {
            "British","German","Brazilian","French","Italian","Spanish",
            "Dutch","Finnish","Australian","Canadian","American","Japanese",
            "Mexican","Monegasque","Austrian","Thai","Danish","Nigerian"
        };

        private readonly DriverArchetype[] archetypeOrder = new DriverArchetype[]
        {
            DriverArchetype.Balanced, DriverArchetype.Overtaker,
            DriverArchetype.Qualifier, DriverArchetype.TyreWhisperer
        };

        // ── Events ────────────────────────────────────────────────────────
        public System.Action<DriverProfile> OnCreationComplete;
        public System.Action OnBack;

        // ─────────────────────────────────────────────────────────────────
        void Start()
        {
            // Build full 2026 team roster
            availableTeams.Add(TeamData2026.Audi());
            availableTeams.Add(TeamData2026.Cadillac());
            // Remaining teams via GameBootstrap if available
            var bootstrap = FindObjectOfType<GameBootstrap>();
            if (bootstrap?.AllTeams != null)
                foreach (var t in bootstrap.AllTeams)
                    if (!availableTeams.Exists(x => x.teamName == t.teamName))
                        availableTeams.Add(t);

            // Navigation (original)
            backButton?.onClick.AddListener(() => OnBack?.Invoke());
            prevButton?.onClick.AddListener(() => GoToStep(currentStep - 1));
            nextButton?.onClick.AddListener(OnNextClicked);

            // Step 0 (original)
            customDriverButton?.onClick.AddListener(() => { profile.iconType = DriverIconType.Custom;     RenderStep(); });
            iconDriverButton?.onClick.AddListener(()   => { profile.iconType = DriverIconType.Icon;       RenderStep(); });
            realDriverButton?.onClick.AddListener(()   => { profile.iconType = DriverIconType.RealDriver; RenderStep(); });

            // Step 1 inputs (original)
            driverNameInput?.onValueChanged.AddListener(val =>
            {
                profile.driverName = val;
                UpdateDriverCodePreview(val);
                nextButton.interactable = CanAdvance();
            });
            raceNumberInput?.onValueChanged.AddListener(val =>
            {
                if (int.TryParse(val, out int num)) profile.raceNumber = Mathf.Clamp(num, 1, 99);
            });

            for (int i = 0; i < iconButtons.Length && i < legendaryIcons.Length; i++)
            {
                int idx = i;
                iconButtons[i]?.onClick.AddListener(() => SelectIcon(idx));
            }

            // Step 3 series (original + F3)
            f2Button?.onClick.AddListener(() => { profile.tier = SeriesTier.Formula2; RenderStep(); });
            f1Button?.onClick.AddListener(() => { profile.tier = SeriesTier.Formula1; RenderStep(); });
            f3Button?.onClick.AddListener(() => { profile.tier = SeriesTier.Formula3; RenderStep(); });

            // 2026: Step 4 archetype buttons
            for (int i = 0; i < archetypeButtons.Length && i < archetypeOrder.Length; i++)
            {
                int idx = i;
                archetypeButtons[i]?.onClick.AddListener(() => SelectArchetype(archetypeOrder[idx]));
            }

            ShowStep(0);
        }

        // ── Step navigation ───────────────────────────────────────────────
        void OnNextClicked()
        {
            if (currentStep < totalSteps - 1)
                GoToStep(currentStep + 1);
            else
            {
                profile.UpdateReputationTier();
                OnCreationComplete?.Invoke(profile);
            }
        }

        void GoToStep(int step)
        {
            currentStep = Mathf.Clamp(step, 0, totalSteps - 1);
            ShowStep(currentStep);
        }

        void ShowStep(int step)
        {
            step0Panel?.SetActive(step == 0);
            step1Panel?.SetActive(step == 1);
            step2Panel?.SetActive(step == 2);
            step3Panel?.SetActive(step == 3);
            step4Panel?.SetActive(step == 4);   // 2026 archetype step

            if (stepIndicatorText) stepIndicatorText.text = $"Step {step + 1} of {totalSteps}";

            // Progress bars (now 5)
            for (int i = 0; i < progressBars.Length; i++)
            {
                if (progressBars[i] == null) continue;
                progressBars[i].fillAmount = i <= step ? 1f : 0f;
                progressBars[i].color = i <= step
                    ? new Color(0.91f, 0f, 0.18f)
                    : new Color(0.13f, 0.13f, 0.13f);
            }

            if (prevButton) prevButton.interactable = step > 0;
            if (nextButton)
            {
                nextButton.interactable = CanAdvance();
                var btnText = nextButton.GetComponentInChildren<TMP_Text>();
                if (btnText) btnText.text = step < totalSteps - 1 ? "NEXT →" : "START CAREER";
            }

            if (step == 1) RenderCustomiseStep();
            if (step == 2) RenderTeamStep();
            if (step == 4) RenderArchetypeStep();
        }

        void RenderStep() => ShowStep(currentStep);

        // ── CanAdvance (original + step 4) ───────────────────────────────
        bool CanAdvance()
        {
            return currentStep switch
            {
                0 => true,
                1 => !string.IsNullOrEmpty(profile.driverName),
                2 => !string.IsNullOrEmpty(profile.currentTeam),
                3 => profile.tier != SeriesTier.Formula3 || f3Button != null, // F3 now valid
                4 => true,  // archetype always valid (defaults to Balanced)
                _ => false
            };
        }

        // ── Step 1 (original + ERS/Tyre bars) ────────────────────────────
        void RenderCustomiseStep()
        {
            bool isIcon = profile.iconType == DriverIconType.Icon;
            if (iconSelectionPanel) iconSelectionPanel.SetActive(isIcon);
            if (customInputPanel)   customInputPanel.SetActive(!isIcon);

            if (nationalityDropdown != null && nationalityDropdown.options.Count == 0)
            {
                nationalityDropdown.ClearOptions();
                var opts = new List<TMP_Dropdown.OptionData>();
                foreach (var n in nationalities) opts.Add(new TMP_Dropdown.OptionData(n));
                nationalityDropdown.AddOptions(opts);
                nationalityDropdown.onValueChanged.AddListener(idx =>
                {
                    if (idx >= 0 && idx < nationalities.Length)
                        profile.nationality = nationalities[idx];
                });
            }

            UpdateStatPreview();
            UpdateDriverCodePreview(profile.driverName);
        }

        void SelectIcon(int index)
        {
            if (index < 0 || index >= legendaryIcons.Length) return;
            var icon = legendaryIcons[index];
            profile.driverName  = icon.name;
            profile.pace        = icon.pace;
            profile.awareness   = icon.awareness;
            profile.racecraft   = icon.racecraft;
            profile.experience  = icon.experience;
            UpdateStatPreview();
            UpdateDriverCodePreview(icon.name);

            for (int i = 0; i < iconButtons.Length; i++)
            {
                var img = iconButtons[i]?.GetComponent<Image>();
                if (img) img.color = i == index ? new Color(0.1f, 0f, 0.02f) : Color.white;
            }
        }

        // ── Step 2 (original + 2026 NEW badge for Audi/Cadillac) ─────────
        void RenderTeamStep()
        {
            if (teamGrid == null || teamCardPrefab == null) return;
            foreach (Transform child in teamGrid) Destroy(child.gameObject);

            foreach (var team in availableTeams)
            {
                var card  = Instantiate(teamCardPrefab, teamGrid);
                var texts = card.GetComponentsInChildren<TMP_Text>();
                var images = card.GetComponentsInChildren<Image>();

                if (texts.Length >= 2)
                {
                    texts[0].text = team.teamName;
                    texts[1].text = $"⭐ {team.performanceRating:F0}";
                }
                if (images.Length >= 2 &&
                    ColorUtility.TryParseHtmlString(team.primaryColorHex, out Color col))
                    images[1].color = col;

                if (profile.currentTeam == team.teamName)
                    card.GetComponent<Image>().color = new Color(0.1f, 0f, 0.02f);

                // 2026: NEW badge for Audi and Cadillac
                if (team.isNew2026Entry && newTeamBadgePrefab != null)
                    Instantiate(newTeamBadgePrefab, card.transform);

                var capturedTeam = team;
                card.GetComponent<Button>()?.onClick.AddListener(() =>
                {
                    profile.currentTeam = capturedTeam.teamName;
                    RenderTeamStep();
                });
            }
        }

        // ── Step 4: Archetype (2026 new) ──────────────────────────────────
        void RenderArchetypeStep()
        {
            UpdateArchetypeDisplay(_selectedArchetype);
            HighlightArchetypeButtons(_selectedArchetype);
        }

        void SelectArchetype(DriverArchetype arch)
        {
            _selectedArchetype = arch;
            UpdateArchetypeDisplay(arch);
            HighlightArchetypeButtons(arch);
        }

        void UpdateArchetypeDisplay(DriverArchetype arch)
        {
            if (archetypeNameText)
                archetypeNameText.text = arch switch
                {
                    DriverArchetype.Overtaker     => "OVERTAKER",
                    DriverArchetype.Qualifier     => "QUALIFIER",
                    DriverArchetype.TyreWhisperer => "TYRE WHISPERER",
                    _                             => "ALL-ROUNDER"
                };

            if (archetypeDescText)
                archetypeDescText.text = arch switch
                {
                    DriverArchetype.Overtaker     => "Born to hunt. High overtaking & ERS skill. Low tyre care.",
                    DriverArchetype.Qualifier     => "Front-row specialist. Elite one-lap pace. Struggles in race trim.",
                    DriverArchetype.TyreWhisperer => "Saves rubber. Comes alive in the final stint. Low raw pace.",
                    _                             => "No standout weakness. No exceptional peak. A solid foundation."
                };

            // Update stat bars to reflect archetype starting values
            UpdateStatPreviewForArchetype(arch);

            // 2026: starting reputation based on tier + archetype
            if (startingRepText)
            {
                float rep = profile.tier switch
                {
                    SeriesTier.Formula1 => 55f,
                    SeriesTier.Formula2 => 40f,
                    _                  => 25f
                };
                startingRepText.text = $"Starting Reputation: {rep:F0}/100";
            }
        }

        void UpdateStatPreviewForArchetype(DriverArchetype arch)
        {
            // Approximate starting values per archetype
            float pace, awareness, racecraft, experience, ers, tyre;
            switch (arch)
            {
                case DriverArchetype.Overtaker:
                    pace = 60f; awareness = 55f; racecraft = 62f;
                    experience = 40f; ers = 65f; tyre = 40f; break;
                case DriverArchetype.Qualifier:
                    pace = 70f; awareness = 52f; racecraft = 55f;
                    experience = 40f; ers = 58f; tyre = 48f; break;
                case DriverArchetype.TyreWhisperer:
                    pace = 50f; awareness = 60f; racecraft = 58f;
                    experience = 45f; ers = 50f; tyre = 72f; break;
                default:
                    pace = 55f; awareness = 55f; racecraft = 55f;
                    experience = 40f; ers = 55f; tyre = 55f; break;
            }

            if (paceBar)         paceBar.fillAmount         = pace        / 99f;
            if (awarenessBar)    awarenessBar.fillAmount     = awareness   / 99f;
            if (racecraftBar)    racecraftBar.fillAmount     = racecraft   / 99f;
            if (experienceBar)   experienceBar.fillAmount    = experience  / 99f;
            if (ersSkillBar)     ersSkillBar.fillAmount      = ers         / 99f;
            if (tyreManagementBar) tyreManagementBar.fillAmount = tyre     / 99f;
            if (ovrText)         ovrText.text = Mathf.RoundToInt((pace + awareness + racecraft + experience) / 4f).ToString();
        }

        void HighlightArchetypeButtons(DriverArchetype arch)
        {
            for (int i = 0; i < archetypeButtons.Length && i < archetypeOrder.Length; i++)
            {
                var img = archetypeButtons[i]?.GetComponent<Image>();
                if (img) img.color = archetypeOrder[i] == arch
                    ? new Color(0.91f, 0f, 0.18f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.15f, 1f);
            }
        }

        // ── Original stat preview ─────────────────────────────────────────
        void UpdateStatPreview()
        {
            if (paceBar)       paceBar.fillAmount       = profile.pace        / 99f;
            if (awarenessBar)  awarenessBar.fillAmount   = profile.awareness   / 99f;
            if (racecraftBar)  racecraftBar.fillAmount   = profile.racecraft   / 99f;
            if (experienceBar) experienceBar.fillAmount  = profile.experience  / 99f;
            if (ovrText)       ovrText.text              = profile.OVR.ToString();
        }

        // ── 2026: live driver code preview ────────────────────────────────
        void UpdateDriverCodePreview(string name)
        {
            if (driverCodePreviewText == null || string.IsNullOrEmpty(name)) return;
            string code = name.ToUpper();
            driverCodePreviewText.text = code.Length >= 3 ? code.Substring(0, 3) : (code + "GHO").Substring(0, 3);
        }

        // ── Original OnDestroy (extended) ─────────────────────────────────
        void OnDestroy()
        {
            backButton?.onClick.RemoveAllListeners();
            prevButton?.onClick.RemoveAllListeners();
            nextButton?.onClick.RemoveAllListeners();
            customDriverButton?.onClick.RemoveAllListeners();
            iconDriverButton?.onClick.RemoveAllListeners();
            realDriverButton?.onClick.RemoveAllListeners();
            f2Button?.onClick.RemoveAllListeners();
            f1Button?.onClick.RemoveAllListeners();
            f3Button?.onClick.RemoveAllListeners();
            if (archetypeButtons != null)
                foreach (var b in archetypeButtons) b?.onClick.RemoveAllListeners();
        }
    }
}