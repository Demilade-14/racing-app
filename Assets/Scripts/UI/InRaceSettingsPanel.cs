using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Data;
using RacingGame.Physics;
using RacingGame.Audio;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  IN-RACE SETTINGS PANEL
    //
    //  Mirrors the real F1 Multi-Function Display (MFD). Lets the player
    //  adjust while driving:
    //    • Brake bias  (forward / rearward in 1% steps)
    //    • On-throttle differential
    //    • Off-throttle differential
    //    • ERS deployment mode  (Harvest / Balanced / Overtake / Qualifying)
    //    • Engine mode           (Eco / Normal / Push / Overtake)
    //
    //  Design principle: every change writes directly to CarSetup / VehicleState
    //  and takes effect next FixedUpdate – no buffering.
    // ═══════════════════════════════════════════════════════════════════════
    public class InRaceSettingsPanel : MonoBehaviour
    {
        // ── Tabs (each tab = one MFD page) ────────────────────────────────
        public enum MFDTab { BrakeBias, Differential, ERS, Engine }

        [Header("Panel Root")]
        public GameObject panelRoot;
        public bool       openOnStart = false;

        [Header("References")]
        public PhysicsIntegrator physics;

        // ── Tab navigation ────────────────────────────────────────────────
        [Header("Tab Panels")]
        public GameObject brakeBiasTabPanel;
        public GameObject differentialTabPanel;
        public GameObject ersTabPanel;
        public GameObject engineTabPanel;

        [Header("Tab Buttons")]
        public Button brakeBiasTabBtn;
        public Button differentialTabBtn;
        public Button ersTabBtn;
        public Button engineTabBtn;

        // ── Brake Bias UI ─────────────────────────────────────────────────
        [Header("Brake Bias")]
        public Button             brakeBiasForwardBtn;
        public Button             brakeBiasRearwardBtn;
        public TextMeshProUGUI    brakeBiasValueText;   // e.g. "57.0%"
        public Slider             brakeBiasSlider;
        public TextMeshProUGUI    brakeMigrationLabel;  // shows migration value
        [Tooltip("Highlight when bias is outside recommended band")]
        public Image              brakeBiasWarningIcon;

        // ── Differential UI ───────────────────────────────────────────────
        [Header("Differential")]
        public Button          diffOnUpBtn;
        public Button          diffOnDownBtn;
        public TextMeshProUGUI diffOnValueText;         // e.g. "75%"

        public Button          diffOffUpBtn;
        public Button          diffOffDownBtn;
        public TextMeshProUGUI diffOffValueText;

        public Slider          diffOnSlider;
        public Slider          diffOffSlider;

        // ── ERS UI ────────────────────────────────────────────────────────
        [Header("ERS")]
        public Button          ersPrevBtn;
        public Button          ersNextBtn;
        public TextMeshProUGUI ersModeText;             // e.g. "BALANCED"
        public Image           ersSoCBar;
        public TextMeshProUGUI ersSoCText;
        public Image           ersOvertakeTimerBar;
        public TextMeshProUGUI ersOvertakeTimerText;
        public Button          ersOvertakeShortcutBtn;  // dedicated OVERTAKE button

        // ── Engine Mode UI ────────────────────────────────────────────────
        [Header("Engine")]
        public Button          enginePrevBtn;
        public Button          engineNextBtn;
        public TextMeshProUGUI engineModeValueText;

        // ── Colour palette ────────────────────────────────────────────────
        static readonly Color COL_GREEN  = new(0.0f, 0.9f, 0.2f);
        static readonly Color COL_YELLOW = new(1.0f, 0.85f, 0.0f);
        static readonly Color COL_RED    = new(0.9f, 0.1f, 0.1f);
        static readonly Color COL_CYAN   = Color.cyan;
        static readonly Color COL_WHITE  = Color.white;
        static readonly Color COL_ORANGE = new(1.0f, 0.55f, 0.0f);

        // ── ERS mode cycle order ──────────────────────────────────────────
        static readonly ERSMode[]    ERS_MODES    = { ERSMode.Harvest, ERSMode.Balanced, ERSMode.Overtake, ERSMode.Qualifying };
        static readonly EngineMode[] ENGINE_MODES = { EngineMode.Eco, EngineMode.Normal, EngineMode.Push, EngineMode.Overtake };

        MFDTab _activeTab = MFDTab.BrakeBias;
        bool   _isOpen;

        // ─────────────────────────────────────────────────────────────────
        //  LIFECYCLE
        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            BindButtons();
        }

        void Start()
        {
            SetTab(MFDTab.BrakeBias);
            SetVisible(openOnStart);
        }

        void Update()
        {
            if (!_isOpen || physics == null) return;
            RefreshAllDisplays();
        }

        // ─────────────────────────────────────────────────────────────────
        //  PANEL VISIBILITY
        // ─────────────────────────────────────────────────────────────────
        public void TogglePanel() => SetVisible(!_isOpen);

        public void SetVisible(bool visible)
        {
            _isOpen = visible;
            if (panelRoot != null) panelRoot.SetActive(visible);
            if (visible) RefreshAllDisplays();
        }

        // ─────────────────────────────────────────────────────────────────
        //  TAB NAVIGATION
        // ─────────────────────────────────────────────────────────────────
        public void OpenBrakeBiasTab()    => SetTab(MFDTab.BrakeBias);
        public void OpenDifferentialTab() => SetTab(MFDTab.Differential);
        public void OpenERSTab()          => SetTab(MFDTab.ERS);
        public void OpenEngineTab()       => SetTab(MFDTab.Engine);

        /// <summary>Cycle to next tab — bind to gamepad D-pad or swipe gesture.</summary>
        public void NextTab()
        {
            int next = ((int)_activeTab + 1) % 4;
            SetTab((MFDTab)next);
        }

        public void PrevTab()
        {
            int prev = ((int)_activeTab + 3) % 4;
            SetTab((MFDTab)prev);
        }

        void SetTab(MFDTab tab)
        {
            _activeTab = tab;
            if (brakeBiasTabPanel)    brakeBiasTabPanel.SetActive(tab == MFDTab.BrakeBias);
            if (differentialTabPanel) differentialTabPanel.SetActive(tab == MFDTab.Differential);
            if (ersTabPanel)          ersTabPanel.SetActive(tab == MFDTab.ERS);
            if (engineTabPanel)       engineTabPanel.SetActive(tab == MFDTab.Engine);

            RefreshAllDisplays();
        }

        // ─────────────────────────────────────────────────────────────────
        //  BRAKE BIAS  –  actions
        // ─────────────────────────────────────────────────────────────────
        public void OnBrakeBiasForward()
        {
            if (physics == null) return;
            BrakeMigrationController.BrakeBiasForward(physics.Setup);
            RefreshBrakeBias();
            PlayClick();
        }

        public void OnBrakeBiasRearward()
        {
            if (physics == null) return;
            BrakeMigrationController.BrakeBiasRearward(physics.Setup);
            RefreshBrakeBias();
            PlayClick();
        }

        /// <summary>Called by a UI Slider's OnValueChanged event.</summary>
        public void OnBrakeBiasSliderChanged(float value)
        {
            if (physics == null) return;
            // Slider range configured as 0.48 – 0.70 in the inspector
            physics.Setup.brakeBias = Mathf.Clamp(value, 0.48f, 0.70f);
            RefreshBrakeBias();
        }

        // ─────────────────────────────────────────────────────────────────
        //  DIFFERENTIAL  –  actions
        // ─────────────────────────────────────────────────────────────────
        public void OnDiffOnUp()
        {
            if (physics == null) return;
            BrakeMigrationController.DiffOnThrottleUp(physics.Setup);
            RefreshDifferential();
            PlayClick();
        }

        public void OnDiffOnDown()
        {
            if (physics == null) return;
            BrakeMigrationController.DiffOnThrottleDown(physics.Setup);
            RefreshDifferential();
            PlayClick();
        }

        public void OnDiffOffUp()
        {
            if (physics == null) return;
            BrakeMigrationController.DiffOffThrottleUp(physics.Setup);
            RefreshDifferential();
            PlayClick();
        }

        public void OnDiffOffDown()
        {
            if (physics == null) return;
            BrakeMigrationController.DiffOffThrottleDown(physics.Setup);
            RefreshDifferential();
            PlayClick();
        }

        // ─────────────────────────────────────────────────────────────────
        //  ERS MODE  –  actions
        // ─────────────────────────────────────────────────────────────────
        public void OnERSNext()
        {
            if (physics == null) return;
            int idx = System.Array.IndexOf(ERS_MODES, physics.State.ersMode);
            physics.SetERSMode(ERS_MODES[(idx + 1) % ERS_MODES.Length]);
            RefreshERS();
            PlayClick();
        }

        public void OnERSPrev()
        {
            if (physics == null) return;
            int idx = System.Array.IndexOf(ERS_MODES, physics.State.ersMode);
            physics.SetERSMode(ERS_MODES[(idx + ERS_MODES.Length - 1) % ERS_MODES.Length]);
            RefreshERS();
            PlayClick();
        }

        /// <summary>One-tap overtake burst — usable outside MFD as a shortcut button.</summary>
        public void OnOvertakeShortcut()
        {
            if (physics == null) return;
            physics.ActivateOvertake();
            RefreshERS();
            // Distinct audio cue for overtake activation
            AudioManager.Instance?.PlayDRSActivated();
        }

        // ─────────────────────────────────────────────────────────────────
        //  ENGINE MODE  –  actions
        // ─────────────────────────────────────────────────────────────────
        public void OnEngineNext()
        {
            if (physics == null) return;
            int idx = System.Array.IndexOf(ENGINE_MODES, physics.State.engineMode);
            physics.State.engineMode = ENGINE_MODES[(idx + 1) % ENGINE_MODES.Length];
            RefreshEngine();
            PlayClick();
        }

        public void OnEnginePrev()
        {
            if (physics == null) return;
            int idx = System.Array.IndexOf(ENGINE_MODES, physics.State.engineMode);
            physics.State.engineMode = ENGINE_MODES[(idx + ENGINE_MODES.Length - 1) % ENGINE_MODES.Length];
            RefreshEngine();
            PlayClick();
        }

        // ─────────────────────────────────────────────────────────────────
        //  DISPLAY REFRESH
        // ─────────────────────────────────────────────────────────────────
        void RefreshAllDisplays()
        {
            switch (_activeTab)
            {
                case MFDTab.BrakeBias:    RefreshBrakeBias();    break;
                case MFDTab.Differential: RefreshDifferential(); break;
                case MFDTab.ERS:          RefreshERS();          break;
                case MFDTab.Engine:       RefreshEngine();       break;
            }
        }

        void RefreshBrakeBias()
        {
            if (physics == null) return;
            var setup = physics.Setup;
            float bias    = setup.brakeBias;
            float biasKph = physics.State.speed;
            float liveBias = setup.EffectiveBrakeBias(biasKph);

            // Value label: shows base bias
            if (brakeBiasValueText)
                brakeBiasValueText.text = $"{bias * 100f:F1}%";

            // Migration label: shows live effective bias at current speed
            if (brakeMigrationLabel)
            {
                float delta = liveBias - bias;
                string sign = delta >= 0f ? "+" : "";
                brakeMigrationLabel.text = $"Live: {liveBias * 100f:F1}%  ({sign}{delta * 100f:F1}%)";
                brakeMigrationLabel.color = Mathf.Abs(delta) > 0.015f ? COL_YELLOW : COL_WHITE;
            }

            // Slider (0.48 – 0.70 mapped to 0 – 1)
            if (brakeBiasSlider)
            {
                brakeBiasSlider.SetValueWithoutNotify(bias);
            }

            // Warning: bias outside 53–63% is risky
            bool warn = bias < 0.53f || bias > 0.63f;
            if (brakeBiasWarningIcon) brakeBiasWarningIcon.gameObject.SetActive(warn);
            if (brakeBiasValueText)   brakeBiasValueText.color = warn ? COL_RED : COL_WHITE;
        }

        void RefreshDifferential()
        {
            if (physics == null) return;
            var setup = physics.Setup;

            if (diffOnValueText)
            {
                diffOnValueText.text  = $"{setup.onThrottleDiff}%";
                diffOnValueText.color = DiffColor(setup.onThrottleDiff);
            }

            if (diffOffValueText)
            {
                diffOffValueText.text  = $"{setup.offThrottleDiff}%";
                diffOffValueText.color = DiffColor(setup.offThrottleDiff);
            }

            if (diffOnSlider)  diffOnSlider.SetValueWithoutNotify(setup.onThrottleDiff);
            if (diffOffSlider) diffOffSlider.SetValueWithoutNotify(setup.offThrottleDiff);
        }

        void RefreshERS()
        {
            if (physics == null) return;
            var state = physics.State;

            // Mode label
            if (ersModeText)
            {
                ersModeText.text  = state.ersMode.ToString().ToUpper();
                ersModeText.color = ERSModeColor(state.ersMode);
            }

            // SoC bar
            float soc = state.ersSoC / 100f;
            if (ersSoCBar)  ersSoCBar.fillAmount = soc;
            if (ersSoCText) ersSoCText.text = $"{state.ersSoC:F0}%";
            if (ersSoCBar)  ersSoCBar.color = soc < 0.15f ? COL_RED
                                            : soc < 0.40f ? COL_YELLOW : COL_GREEN;

            // Overtake timer
            bool overtakeActive = state.ersMode == ERSMode.Overtake
                                && state.ersOvertakeTimer > 0f;
            if (ersOvertakeTimerBar)
            {
                ersOvertakeTimerBar.gameObject.SetActive(overtakeActive);
                if (overtakeActive)
                    ersOvertakeTimerBar.fillAmount = state.ersOvertakeTimer / 10f;
            }
            if (ersOvertakeTimerText)
            {
                ersOvertakeTimerText.gameObject.SetActive(overtakeActive);
                if (overtakeActive)
                    ersOvertakeTimerText.text = $"{state.ersOvertakeTimer:F1}s";
            }

            // Shortcut button: grey out if SoC too low
            if (ersOvertakeShortcutBtn)
                ersOvertakeShortcutBtn.interactable = state.ersSoC >= 10f;
        }

        void RefreshEngine()
        {
            if (physics == null) return;
            var mode = physics.State.engineMode;

            if (engineModeValueText)
            {
                engineModeValueText.text  = mode.ToString().ToUpper();
                engineModeValueText.color = EngineModeColor(mode);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  BUTTON BINDING
        // ─────────────────────────────────────────────────────────────────
        void BindButtons()
        {
            // Tab nav
            brakeBiasTabBtn?.onClick.AddListener(OpenBrakeBiasTab);
            differentialTabBtn?.onClick.AddListener(OpenDifferentialTab);
            ersTabBtn?.onClick.AddListener(OpenERSTab);
            engineTabBtn?.onClick.AddListener(OpenEngineTab);

            // Brake bias
            brakeBiasForwardBtn?.onClick.AddListener(OnBrakeBiasForward);
            brakeBiasRearwardBtn?.onClick.AddListener(OnBrakeBiasRearward);
            brakeBiasSlider?.onValueChanged.AddListener(OnBrakeBiasSliderChanged);

            // Differential
            diffOnUpBtn?.onClick.AddListener(OnDiffOnUp);
            diffOnDownBtn?.onClick.AddListener(OnDiffOnDown);
            diffOffUpBtn?.onClick.AddListener(OnDiffOffUp);
            diffOffDownBtn?.onClick.AddListener(OnDiffOffDown);

            // ERS
            ersNextBtn?.onClick.AddListener(OnERSNext);
            ersPrevBtn?.onClick.AddListener(OnERSPrev);
            ersOvertakeShortcutBtn?.onClick.AddListener(OnOvertakeShortcut);

            // Engine
            engineNextBtn?.onClick.AddListener(OnEngineNext);
            enginePrevBtn?.onClick.AddListener(OnEnginePrev);
        }

        // ─────────────────────────────────────────────────────────────────
        //  COLOUR HELPERS
        // ─────────────────────────────────────────────────────────────────
        static Color DiffColor(int value)
        {
            if (value < 60) return COL_CYAN;    // low lock = agile
            if (value < 80) return COL_GREEN;   // mid = balanced
            return COL_ORANGE;                  // high lock = stable/stiff
        }

        static Color ERSModeColor(ERSMode mode) => mode switch
        {
            ERSMode.Harvest    => COL_CYAN,
            ERSMode.Balanced   => COL_GREEN,
            ERSMode.Overtake   => COL_ORANGE,
            ERSMode.Qualifying => COL_RED,
            _                  => COL_WHITE
        };

        static Color EngineModeColor(EngineMode mode) => mode switch
        {
            EngineMode.Eco      => COL_CYAN,
            EngineMode.Normal   => COL_WHITE,
            EngineMode.Push     => COL_YELLOW,
            EngineMode.Overtake => COL_RED,
            _                   => COL_WHITE
        };

        static void PlayClick()
        {
            // Reuse pit-confirmed clip as a lightweight MFD click sound
            AudioManager.Instance?.PlayPitConfirmed();
        }
    }
}
