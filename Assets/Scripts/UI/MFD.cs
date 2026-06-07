using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Data;
using RacingGame.Physics;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  MFD  —  Multi-Functional Display
    //  Three tabs: TYRES | FUEL | SETUP
    //  Toggle visible with ToggleMFD(); auto-hides after inactivity.
    // ═══════════════════════════════════════════════════════════════════════
    public class MFD : MonoBehaviour
    {
        public enum Tab { Tyres, Fuel, Setup }

        // ── Root ──────────────────────────────────────────────────────────
        [Header("Root")]
        public GameObject mfdRoot;
        [Tooltip("Seconds before MFD auto-hides")]
        public float      autoHideDelay = 8f;

        // ── Tab buttons ────────────────────────────────────────────────────
        [Header("Tab Buttons")]
        public Button tyresTabBtn;
        public Button fuelTabBtn;
        public Button setupTabBtn;

        // ── Tab panels ────────────────────────────────────────────────────
        [Header("Tab Panels")]
        public GameObject tyresPanel;
        public GameObject fuelPanel;
        public GameObject setupPanel;

        // ── Tyres tab ──────────────────────────────────────────────────────
        [Header("Tyres Tab — order FL FR RL RR")]
        public TextMeshProUGUI[] tireWearPct   = new TextMeshProUGUI[4]; // "73%"
        public Image[]           tireWearBar   = new Image[4];           // fillAmount
        public TextMeshProUGUI[] tireTempVal   = new TextMeshProUGUI[4]; // "94°C"
        public TextMeshProUGUI[] tireCompound  = new TextMeshProUGUI[4]; // "M"
        public TextMeshProUGUI[] tireGraining  = new TextMeshProUGUI[4]; // "GRAIN 0.3"
        public TextMeshProUGUI[] tireBlisters  = new TextMeshProUGUI[4]; // "BLIST 0.0"
        public TextMeshProUGUI   lapAgeLabel;                            // "Lap age: 12"

        // ── Fuel tab ───────────────────────────────────────────────────────
        [Header("Fuel Tab")]
        public TextMeshProUGUI   fuelLoadLabel;   // "47.3 L"
        public TextMeshProUGUI   fuelLapsLabel;   // "~18 laps"
        public Image             fuelBar;
        public TextMeshProUGUI   engineModeLabel;
        public TextMeshProUGUI   fuelWarningLabel; // shown < 5 laps

        // ── Setup tab ──────────────────────────────────────────────────────
        [Header("Setup Tab")]
        public TextMeshProUGUI frontWingLabel;    // "FW: 7"
        public TextMeshProUGUI rearWingLabel;     // "RW: 5"
        public TextMeshProUGUI brakeBiasLabel;    // "BB: 57.0%"
        public TextMeshProUGUI suspFrontLabel;    // "SUSP F: 6"
        public TextMeshProUGUI suspRearLabel;     // "SUSP R: 4"
        public TextMeshProUGUI arbFrontLabel;
        public TextMeshProUGUI arbRearLabel;
        public TextMeshProUGUI diffOnLabel;
        public TextMeshProUGUI diffOffLabel;
        public TextMeshProUGUI pressureLabel;     // "FL 23.4 | FR 23.2 | RL 21.8 | RR 21.6"

        // ── Color constants ────────────────────────────────────────────────
        static readonly Color C_GREEN    = new Color(0.10f, 0.85f, 0.20f);
        static readonly Color C_YELLOW   = new Color(1.00f, 0.85f, 0.00f);
        static readonly Color C_RED      = new Color(0.90f, 0.10f, 0.10f);
        static readonly Color C_BLUE     = new Color(0.20f, 0.45f, 1.00f);
        static readonly Color C_WHITE    = Color.white;

        // ── State ──────────────────────────────────────────────────────────
        Tab    _activeTab    = Tab.Tyres;
        float  _hideTimer;
        bool   _visible;

        HudBuilder        _hud;
        PhysicsIntegrator _physics;
        MobileInputManager _input;

        void Awake()
        {
            _hud     = FindFirstObjectByType<HudBuilder>();
            _physics = FindFirstObjectByType<PhysicsIntegrator>();
            _input   = FindFirstObjectByType<MobileInputManager>();

            tyresTabBtn?.onClick.AddListener(() => SwitchTab(Tab.Tyres));
            fuelTabBtn ?.onClick.AddListener(() => SwitchTab(Tab.Fuel));
            setupTabBtn?.onClick.AddListener(() => SwitchTab(Tab.Setup));

            mfdRoot?.SetActive(false);
        }

        void Update()
        {
            if (!_visible) return;

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f) SetVisible(false);

            RefreshActiveTab();
        }

        // ── Public API ────────────────────────────────────────────────────
        public void ToggleMFD()
        {
            SetVisible(!_visible);
        }

        public void SetVisible(bool show)
        {
            _visible = show;
            _hideTimer = autoHideDelay;
            mfdRoot?.SetActive(show);
        }

        void SwitchTab(Tab tab)
        {
            _activeTab = tab;
            _hideTimer = autoHideDelay;   // reset timer on interaction

            tyresPanel?.SetActive(tab == Tab.Tyres);
            fuelPanel ?.SetActive(tab == Tab.Fuel);
            setupPanel?.SetActive(tab == Tab.Setup);
        }

        // ── Refresh ───────────────────────────────────────────────────────
        void RefreshActiveTab()
        {
            switch (_activeTab)
            {
                case Tab.Tyres: RefreshTyres(); break;
                case Tab.Fuel:  RefreshFuel();  break;
                case Tab.Setup: RefreshSetup(); break;
            }
        }

        void RefreshTyres()
        {
            if (_physics == null) return;
            var s = _physics.State;

            for (int i = 0; i < 4; i++)
            {
                var t = s.tires[i];
                if (tireWearPct[i])
                {
                    tireWearPct[i].text  = $"{t.wearPercent:F0}%";
                    tireWearPct[i].color = WearColor(t.wearPercent);
                }
                if (tireWearBar[i])
                    tireWearBar[i].fillAmount = 1f - t.wearPercent / 100f;

                if (tireTempVal[i])  tireTempVal[i].text  = $"{t.surfaceTemp:F0}°C";
                if (tireCompound[i]) tireCompound[i].text = t.compound.ToString()[0].ToString();
                if (tireGraining[i]) tireGraining[i].text = $"GRAIN {t.grainingLevel:F2}";
                if (tireBlisters[i]) tireBlisters[i].text = $"BLIST {t.blisteringLevel:F2}";
            }

            if (lapAgeLabel) lapAgeLabel.text = $"Lap age: {s.tires[0].lapAge}";
        }

        void RefreshFuel()
        {
            if (_physics == null || _hud == null) return;
            var h = _hud.Current;
            var s = _physics.State;

            if (fuelLoadLabel) fuelLoadLabel.text = $"{s.fuelLoad:F1} L";
            if (fuelBar)
            {
                fuelBar.fillAmount = h.fuelPercent / 100f;
                fuelBar.color      = h.fuelPercent < 15f ? C_RED
                                   : h.fuelPercent < 30f ? C_YELLOW
                                   : C_GREEN;
            }

            if (fuelLapsLabel)
            {
                int lapsLeft = Mathf.Max(0, h.totalLaps - h.currentLap);
                fuelLapsLabel.text = $"~{h.lapsOfFuelLeft} laps rem | {lapsLeft} to go";
                fuelLapsLabel.color = h.lapsOfFuelLeft < 5 ? C_RED : C_WHITE;
            }

            if (engineModeLabel) engineModeLabel.text = s.engineMode.ToString().ToUpper();

            if (fuelWarningLabel)
            {
                bool warn = h.lapsOfFuelLeft < 5;
                fuelWarningLabel.gameObject.SetActive(warn);
                if (warn) fuelWarningLabel.text = "⚠ FUEL CRITICAL";
            }
        }

        void RefreshSetup()
        {
            if (_physics == null) return;
            var setup = _physics.setup;
            var s     = _physics.State;

            float bb = _input != null ? _input.State.BrakeBias : setup.brakeBias;

            if (frontWingLabel) frontWingLabel.text = $"FRONT WING:  {setup.frontWingAngle}";
            if (rearWingLabel)  rearWingLabel.text  = $"REAR WING:   {setup.rearWingAngle}";
            if (brakeBiasLabel) brakeBiasLabel.text = $"BRAKE BIAS:  {bb * 100f:F1}%";
            if (suspFrontLabel) suspFrontLabel.text = $"SUSP FRONT:  {setup.suspensionStiffnessFront}";
            if (suspRearLabel)  suspRearLabel.text  = $"SUSP REAR:   {setup.suspensionStiffnessRear}";
            if (arbFrontLabel)  arbFrontLabel.text  = $"ARB FRONT:   {setup.antiRollBarFront}";
            if (arbRearLabel)   arbRearLabel.text   = $"ARB REAR:    {setup.antiRollBarRear}";
            if (diffOnLabel)    diffOnLabel.text    = $"DIFF ON:     {setup.onThrottleDiff}%";
            if (diffOffLabel)   diffOffLabel.text   = $"DIFF OFF:    {setup.offThrottleDiff}%";

            if (pressureLabel)
            {
                var t = s.tires;
                pressureLabel.text =
                    $"FL {t[0].currentPressurePSI:F1} | FR {t[1].currentPressurePSI:F1} | " +
                    $"RL {t[2].currentPressurePSI:F1} | RR {t[3].currentPressurePSI:F1} PSI";
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        static Color WearColor(float wear)
        {
            if (wear < 40f) return C_GREEN;
            if (wear < 65f) return C_YELLOW;
            return C_RED;
        }
    }
}
