using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Data;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  RACE HUD
    //  Extends the existing RacingHUD by adding elements not covered there:
    //   – Radial RPM gauge with needle
    //   – Per-corner tire temp color coding (Blue/White/Green/Red)
    //   – ERS SoC bar + mode indicator
    //   – Delta time (current lap vs personal best)
    //   – Gap to car ahead / behind
    //  Reads from HudBuilder.Current (same pipeline as RacingHUD).
    // ═══════════════════════════════════════════════════════════════════════
    public class RaceHUD : MonoBehaviour
    {
        // ── RPM / Gear ─────────────────────────────────────────────────────
        [Header("RPM & Gear")]
        public Image           rpmFill;          // radial Image (360° fillAmount)
        public RectTransform   rpmNeedle;        // optional needle pivot
        [Tooltip("Needle angle at 0 RPM")]
        public float           needleMinAngle = -130f;
        [Tooltip("Needle angle at 15 000 RPM")]
        public float           needleMaxAngle =  130f;
        public TextMeshProUGUI gearLabel;
        public TextMeshProUGUI speedLabel;
        public Image           rpmOverheatFlash; // blinks red when rpm > 13 500

        // ── Tire temps (FL, FR, RL, RR) ───────────────────────────────────
        [Header("Tire Temps — order: FL FR RL RR")]
        public Image[]           tireTempBg      = new Image[4];    // background panel
        public TextMeshProUGUI[] tireTempLabel   = new TextMeshProUGUI[4];
        public TextMeshProUGUI[] tireWearLabel   = new TextMeshProUGUI[4];
        public TextMeshProUGUI[] tireCompoundLbl = new TextMeshProUGUI[4];
        public Image[]           tireWearFill    = new Image[4];    // vertical fill bar

        // ── ERS ────────────────────────────────────────────────────────────
        [Header("ERS")]
        public Image           ersSoCBar;        // fillAmount = SoC / 100
        public TextMeshProUGUI ersSoCLabel;
        public TextMeshProUGUI ersModeLabel;
        public Image[]         ersModeSegments  = new Image[4];  // H / B / O / Q lights

        // ── Delta time ─────────────────────────────────────────────────────
        [Header("Delta")]
        public TextMeshProUGUI deltaLabel;       // "+0.342" / "-0.128"
        public Image           deltaBg;          // color shifts green/purple/yellow

        // ── Timing strip ───────────────────────────────────────────────────
        [Header("Timing")]
        public TextMeshProUGUI currentLapLabel;
        public TextMeshProUGUI lastLapLabel;
        public TextMeshProUGUI bestLapLabel;
        public TextMeshProUGUI lapCounterLabel;  // "LAP 12/58"
        public TextMeshProUGUI positionLabel;    // "P3"

        // ── Gap strip ──────────────────────────────────────────────────────
        [Header("Gaps")]
        public TextMeshProUGUI gapAheadLabel;    // "+1.234  HAM"
        public TextMeshProUGUI gapBehindLabel;   // "-0.891  VER"

        // ── Flag banner ────────────────────────────────────────────────────
        [Header("Flag")]
        public Image           flagBanner;
        public TextMeshProUGUI flagLabel;

        // ── Damage warning ─────────────────────────────────────────────────
        [Header("Damage")]
        public GameObject      damagePanel;
        public TextMeshProUGUI damageLabel;

        // ── Colors (shared constants) ──────────────────────────────────────
        static readonly Color C_COLD     = new Color(0.20f, 0.45f, 1.00f);  // blue
        static readonly Color C_WARM     = new Color(1.00f, 1.00f, 1.00f);  // white
        static readonly Color C_OPTIMAL  = new Color(0.10f, 0.85f, 0.20f);  // green
        static readonly Color C_HOT      = new Color(1.00f, 0.55f, 0.00f);  // orange
        static readonly Color C_CRITICAL = new Color(0.90f, 0.10f, 0.10f);  // red
        static readonly Color C_PURPLE   = new Color(0.60f, 0.00f, 0.90f);  // personal best
        static readonly Color C_YELLOW   = new Color(1.00f, 0.85f, 0.00f);

        HudBuilder _builder;
        float      _flashTimer;

        void Awake() => _builder = GetComponentInParent<HudBuilder>()
                                ?? FindFirstObjectByType<HudBuilder>();

        void Update()
        {
            if (_builder == null) return;
            HudData h = _builder.Current;

            DrawRPM(h);
            DrawTireTempGrid(h);
            DrawERS(h);
            DrawDelta(h);
            DrawTiming(h);
            DrawGaps(h);
            DrawFlag(h);
            DrawDamage(h);
        }

        // ── RPM ────────────────────────────────────────────────────────────
        void DrawRPM(HudData h)
        {
            float t = h.rpm / 15000f;

            if (rpmFill)   rpmFill.fillAmount = t;
            if (rpmNeedle) rpmNeedle.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Lerp(needleMinAngle, needleMaxAngle, t));

            if (gearLabel)  gearLabel.text  = h.gear == 0 ? "N" : h.gear.ToString();
            if (speedLabel) speedLabel.text = Mathf.RoundToInt(h.speed).ToString();

            // RPM overrev flash
            if (rpmOverheatFlash)
            {
                _flashTimer += Time.deltaTime;
                bool overrev = h.rpm > 13500f;
                rpmOverheatFlash.gameObject.SetActive(overrev && Mathf.Sin(_flashTimer * 12f) > 0f);
            }
        }

        // ── Tire temp color-coding ─────────────────────────────────────────
        // Temp thresholds per tire state:
        //   < 50 °C  → Cold   (blue)
        //   50-70 °C → Warm   (white)
        //   70-100°C → Optimal(green)
        //  100-115°C → Hot    (orange)
        //   > 115°C  → Critical (red)
        void DrawTireTempGrid(HudData h)
        {
            for (int i = 0; i < 4; i++)
            {
                float temp = h.tireTemps[i];
                float wear = h.tireWear[i];
                Color col  = TempColor(temp);

                if (tireTempBg[i])      tireTempBg[i].color          = col * 0.75f;
                if (tireTempLabel[i])
                {
                    tireTempLabel[i].text  = $"{Mathf.RoundToInt(temp)}°";
                    tireTempLabel[i].color = col;
                }
                if (tireWearLabel[i])
                {
                    tireWearLabel[i].text  = $"{Mathf.RoundToInt(wear)}%";
                    tireWearLabel[i].color = wear > 75f ? C_CRITICAL
                                           : wear > 50f ? C_YELLOW
                                           : Color.white;
                }
                if (tireCompoundLbl[i]) tireCompoundLbl[i].text = h.tireCompounds[i];
                if (tireWearFill[i])    tireWearFill[i].fillAmount = 1f - wear / 100f;
            }
        }

        static Color TempColor(float t)
        {
            if (t <  50f) return C_COLD;
            if (t <  70f) return Color.Lerp(C_COLD, C_WARM,    (t -  50f) / 20f);
            if (t < 100f) return Color.Lerp(C_WARM, C_OPTIMAL, (t -  70f) / 30f);
            if (t < 115f) return Color.Lerp(C_OPTIMAL, C_HOT,  (t - 100f) / 15f);
            return C_CRITICAL;
        }

        // ── ERS ────────────────────────────────────────────────────────────
        void DrawERS(HudData h)
        {
            float soc = h.ersEnergy;   // 0–100 from HudData

            if (ersSoCBar)
            {
                ersSoCBar.fillAmount = soc / 100f;
                ersSoCBar.color      = soc < 20f ? C_CRITICAL : soc < 50f ? C_YELLOW : C_OPTIMAL;
            }
            if (ersSoCLabel) ersSoCLabel.text = $"{soc:F0}%";

            ERSMode mode = (ERSMode)(int)h.engineMode;   // mapped from engine mode
            if (ersModeLabel) ersModeLabel.text = mode.ToString().ToUpper();

            // Segment lights: H / B / O / Q
            for (int i = 0; i < ersModeSegments.Length && i < 4; i++)
            {
                if (ersModeSegments[i])
                    ersModeSegments[i].color = (i == (int)mode)
                        ? Color.white
                        : new Color(1f, 1f, 1f, 0.2f);
            }
        }

        // ── Delta ──────────────────────────────────────────────────────────
        void DrawDelta(HudData h)
        {
            if (!deltaLabel) return;

            float delta = h.currentLapTime - h.bestLapTime;
            bool  valid = h.bestLapTime > 0f && h.currentLapTime > 5f;

            deltaLabel.text  = valid ? (delta >= 0 ? "+" : "") + $"{delta:F3}" : "--";

            Color textCol;
            Color bgCol;
            if (!valid)                         { textCol = Color.grey;  bgCol = Color.clear; }
            else if (delta < -0.001f)           { textCol = C_PURPLE;   bgCol = new Color(0.4f, 0f, 0.7f, 0.4f); }
            else if (delta < 0.3f)              { textCol = C_OPTIMAL;  bgCol = new Color(0f, 0.5f, 0.1f, 0.4f); }
            else                                { textCol = C_YELLOW;   bgCol = new Color(0.5f, 0.4f, 0f, 0.4f); }

            deltaLabel.color = textCol;
            if (deltaBg) deltaBg.color = bgCol;
        }

        // ── Timing strip ───────────────────────────────────────────────────
        void DrawTiming(HudData h)
        {
            if (currentLapLabel)  currentLapLabel.text  = FormatLapTime(h.currentLapTime);
            if (lastLapLabel)     lastLapLabel.text      = FormatLapTime(h.lastLapTime);
            if (bestLapLabel)     bestLapLabel.text      = FormatLapTime(h.bestLapTime);
            if (lapCounterLabel)  lapCounterLabel.text   = $"LAP {h.currentLap} / {h.totalLaps}";
            if (positionLabel)
            {
                positionLabel.text  = $"P{h.racePosition}";
                positionLabel.color = h.racePosition == 1 ? C_YELLOW
                                    : h.racePosition <= 3 ? C_OPTIMAL
                                    : Color.white;
            }
        }

        // ── Gap strip ──────────────────────────────────────────────────────
        void DrawGaps(HudData h)
        {
            if (gapAheadLabel)
                gapAheadLabel.text = h.gapToAhead  >= 999f ? "---"
                    : $"+{h.gapToAhead:F3}  {h.driverAheadName}";

            if (gapBehindLabel)
                gapBehindLabel.text = h.gapToBehind >= 999f ? "---"
                    : $"-{h.gapToBehind:F3}  {h.driverBehindName}";
        }

        // ── Flag banner ────────────────────────────────────────────────────
        void DrawFlag(HudData h)
        {
            if (!flagBanner) return;

            (Color col, string txt) = h.currentFlag switch
            {
                FlagStatus.Yellow           => (C_YELLOW,   "YELLOW FLAG"),
                FlagStatus.SafetyCar        => (C_YELLOW,   "SAFETY CAR"),
                FlagStatus.VirtualSafetyCar => (C_YELLOW,   "VSC"),
                FlagStatus.Red              => (C_CRITICAL, "RED FLAG"),
                FlagStatus.Chequered        => (Color.white,"CHEQUERED FLAG"),
                _                           => (Color.clear,"")
            };

            flagBanner.color              = col;
            flagBanner.gameObject.SetActive(h.currentFlag != FlagStatus.Green);
            if (flagLabel) flagLabel.text = txt;
        }

        // ── Damage ─────────────────────────────────────────────────────────
        void DrawDamage(HudData h)
        {
            if (damagePanel) damagePanel.SetActive(h.damageWarning);
            if (damageLabel && h.damageWarning)
                damageLabel.text = h.damageLevel.ToString().ToUpper() + " DAMAGE";
        }

        // ── Helpers ────────────────────────────────────────────────────────
        static string FormatLapTime(float t)
        {
            if (t <= 0f) return "--:--.---";
            int   min = (int)(t / 60f);
            float sec = t % 60f;
            return $"{min}:{sec:00.000}";
        }
    }
}
