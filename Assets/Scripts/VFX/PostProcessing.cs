using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RacingGame.Physics;
using RacingGame.Graphics;

namespace RacingGame.VFX
{
    // ═══════════════════════════════════════════════════════════════════════
    //  POST PROCESS CONTROLLER  –  2026 Extension
    //  Adds: Cinematic night lighting, OvertakeMode screen flash,
    //        ERS glow bloom burst, Madrid heat-colour grade,
    //        Active aero transition warp, VSC blue flicker.
    //  All original methods retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════

    public class PostProcessController : MonoBehaviour
    {
        public static PostProcessController Instance { get; private set; }

        [Header("Volume")]
        public Volume globalVolume;

        [Header("Physics")]
        public PhysicsIntegrator playerPhysics;

        [Header("Speed Lines")]
        public UnityEngine.UI.RawImage speedLinesOverlay;

        [Header("Wet Lens")]
        public UnityEngine.UI.RawImage wetLensOverlay;

        // ── 2026 new refs ─────────────────────────────────────────────────
        [Header("— 2026: Night Circuit —")]
        public Light[]  trackFloodlights;         // Stadium lights around circuit
        public Light    pitLaneLight;             // Warm pit lane fill light
        public Color    nightSkyColor   = new(0.03f, 0.04f, 0.12f);
        public Color    daySkyCcolor    = new(0.50f, 0.70f, 1.00f);

        [Header("— 2026: Overtake Flash —")]
        public UnityEngine.UI.RawImage overtakeFlashOverlay;  // full-screen red tint

        [Header("— 2026: Madrid Grade —")]
        public bool isMadridCircuit = false;

        [Header("— 2026: VSC Blue Flicker —")]
        public UnityEngine.UI.RawImage vscOverlay;

        // ── Cached URP effects ────────────────────────────────────────────
        Bloom              _bloom;
        ChromaticAberration _chroma;
        Vignette           _vignette;
        MotionBlur         _motionBlur;
        ColorAdjustments   _colorAdj;
        LensDistortion     _lensDist;
        FilmGrain          _grain;
        ShadowsMidtonesHighlights _shadows;   // 2026 new
        Tonemapping        _tonemapping;       // 2026 new

        // ── Internal state ────────────────────────────────────────────────
        float _chromaTarget;
        float _speedLineOffset;

        bool  _nightMode;
        bool  _overtakeModeActive;
        bool  _vscActive;
        bool  _isMadridGradeApplied;
        float _overtakeFlashTimer;
        float _vscFlickerTimer;
        float _floodlightIntensityTarget = 8f;

        // Madrid colour grade: warm afternoon haze
        static readonly Color MADRID_SHADOW_COLOR    = new(0.80f, 0.72f, 0.60f);
        static readonly Color MADRID_HIGHLIGHT_COLOR = new(1.05f, 0.98f, 0.85f);
        static readonly Color NIGHT_SHADOW_COLOR     = new(0.12f, 0.15f, 0.28f);
        static readonly Color NIGHT_HIGHLIGHT_COLOR  = new(0.85f, 0.92f, 1.05f);

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            if (globalVolume?.profile != null)
            {
                globalVolume.profile.TryGet(out _bloom);
                globalVolume.profile.TryGet(out _chroma);
                globalVolume.profile.TryGet(out _vignette);
                globalVolume.profile.TryGet(out _motionBlur);
                globalVolume.profile.TryGet(out _colorAdj);
                globalVolume.profile.TryGet(out _lensDist);
                globalVolume.profile.TryGet(out _grain);
                globalVolume.profile.TryGet(out _shadows);
                globalVolume.profile.TryGet(out _tonemapping);
            }

            // Ensure overlays start hidden
            if (overtakeFlashOverlay) overtakeFlashOverlay.gameObject.SetActive(false);
            if (vscOverlay)           vscOverlay.gameObject.SetActive(false);
        }

        void Update()
        {
            if (playerPhysics == null) return;
            var s = playerPhysics.State;

            // Original ticks
            TickBloom(s.speed);
            TickSpeedLines(s.speed);
            TickVignette(s.gLateral);
            TickMotionBlur(s.steering, s.speed);
            TickChromaSmooth();

            // 2026 ticks
            TickOvertakeFlash();
            TickVSCFlicker();
            TickNightFloodlights();
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: CINEMATIC NIGHT LIGHTING
        // ═════════════════════════════════════════════════════════════════

        public void SetNightMode(bool night)
        {
            _nightMode = night;

            // ── Colour adjustments ────────────────────────────────────────
            if (_colorAdj != null)
            {
                _colorAdj.colorFilter.value  = night
                    ? new Color(0.70f, 0.82f, 1.00f)   // cool blue night
                    : new Color(1.00f, 0.97f, 0.90f);  // warm day
                _colorAdj.postExposure.value = night ? -0.4f : 0f;
                _colorAdj.contrast.value     = night ?  18f  : 5f;
                _colorAdj.saturation.value   = night ? -8f   : 0f;
            }

            // ── Shadows / Highlights (cinematic night) ────────────────────
            if (_shadows != null)
            {
                _shadows.shadows.value    = night
                    ? new Vector4(NIGHT_SHADOW_COLOR.r, NIGHT_SHADOW_COLOR.g, NIGHT_SHADOW_COLOR.b, 0f)
                    : Vector4.zero;
                _shadows.highlights.value = night
                    ? new Vector4(NIGHT_HIGHLIGHT_COLOR.r, NIGHT_HIGHLIGHT_COLOR.g, NIGHT_HIGHLIGHT_COLOR.b, 0f)
                    : Vector4.zero;
            }

            // ── Film grain (noise in low-light) ───────────────────────────
            if (_grain != null)
            {
                _grain.active           = night;
                _grain.intensity.value  = 0.28f;
                _grain.response.value   = 0.8f;
            }

            // ── Bloom pumps up for floodlit circuit ───────────────────────
            if (_bloom != null)
            {
                _bloom.threshold.value = night ? 0.6f  : 1.0f;
                _bloom.scatter.value   = night ? 0.65f : 0.45f;
            }

            // ── Tonemapping: ACES at night for deep blacks ────────────────
            if (_tonemapping != null)
                _tonemapping.mode.value = night ? TonemappingMode.ACES : TonemappingMode.Neutral;

            // ── Floodlights ───────────────────────────────────────────────
            _floodlightIntensityTarget = night ? 8f : 0f;
            if (pitLaneLight != null)
            {
                pitLaneLight.enabled   = night;
                pitLaneLight.color     = new Color(1f, 0.92f, 0.75f);  // warm tungsten
                pitLaneLight.intensity = 3.5f;
            }
        }

        void TickNightFloodlights()
        {
            if (trackFloodlights == null) return;
            foreach (var light in trackFloodlights)
            {
                if (light == null) continue;
                light.intensity = Mathf.Lerp(light.intensity, _floodlightIntensityTarget, 2f * Time.deltaTime);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: MADRID HEAT COLOUR GRADE
        // ═════════════════════════════════════════════════════════════════

        public void SetMadridGrade(bool active)
        {
            if (_isMadridGradeApplied == active) return;
            _isMadridGradeApplied = active;

            if (_colorAdj != null && active)
            {
                // Warm, slightly desaturated afternoon haze
                _colorAdj.colorFilter.value  = new Color(1.08f, 0.98f, 0.82f);
                _colorAdj.saturation.value   = -12f;
                _colorAdj.contrast.value     =  10f;
                _colorAdj.postExposure.value =   0.15f;
            }
            else if (_colorAdj != null)
            {
                _colorAdj.colorFilter.value  = new Color(1.00f, 0.97f, 0.90f);
                _colorAdj.saturation.value   = 0f;
                _colorAdj.contrast.value     = 5f;
                _colorAdj.postExposure.value = 0f;
            }

            if (_shadows != null && active)
            {
                _shadows.shadows.value = new Vector4(
                    MADRID_SHADOW_COLOR.r, MADRID_SHADOW_COLOR.g, MADRID_SHADOW_COLOR.b, 0f);
                _shadows.highlights.value = new Vector4(
                    MADRID_HIGHLIGHT_COLOR.r, MADRID_HIGHLIGHT_COLOR.g, MADRID_HIGHLIGHT_COLOR.b, 0f);
            }
            else if (_shadows != null)
            {
                _shadows.shadows.value    = Vector4.zero;
                _shadows.highlights.value = Vector4.zero;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: OVERTAKE MODE SCREEN FLASH
        // ═════════════════════════════════════════════════════════════════

        public void SetOvertakeMode(bool active)
        {
            _overtakeModeActive = active;

            if (active)
            {
                _overtakeFlashTimer = 0.25f;
                StartCoroutine(OvertakeBloomBurst());
            }
        }

        void TickOvertakeFlash()
        {
            if (overtakeFlashOverlay == null) return;

            if (_overtakeModeActive)
            {
                overtakeFlashOverlay.gameObject.SetActive(true);
                // Pulse red tint at 3.5 Hz while active
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.5f * Mathf.PI * 2f);
                var c = overtakeFlashOverlay.color;
                c.a = pulse * 0.18f;
                overtakeFlashOverlay.color = c;
            }
            else if (_overtakeFlashTimer > 0f)
            {
                // Flash fade-out after mode ends
                _overtakeFlashTimer -= Time.deltaTime;
                overtakeFlashOverlay.gameObject.SetActive(true);
                var c = overtakeFlashOverlay.color;
                c.a = _overtakeFlashTimer / 0.25f * 0.35f;
                overtakeFlashOverlay.color = c;
            }
            else
            {
                overtakeFlashOverlay.gameObject.SetActive(false);
            }
        }

        IEnumerator OvertakeBloomBurst()
        {
            if (_bloom == null) yield break;
            // Spike bloom on activation, then settle to high baseline
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                _bloom.intensity.value = Mathf.Lerp(4.5f, 2.0f, t / 0.3f);
                yield return null;
            }
            // Sustain elevated bloom while overtake is active
            while (_overtakeModeActive)
            {
                float pulse = 1.8f + 0.3f * Mathf.Sin(Time.time * 3.5f * Mathf.PI * 2f);
                _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, pulse, 6f * Time.deltaTime);
                yield return null;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: VSC BLUE FLICKER
        // ═════════════════════════════════════════════════════════════════

        public void SetVSCActive(bool active)
        {
            _vscActive = active;
            if (!active && vscOverlay != null)
                vscOverlay.gameObject.SetActive(false);
        }

        void TickVSCFlicker()
        {
            if (!_vscActive || vscOverlay == null) return;
            vscOverlay.gameObject.SetActive(true);

            _vscFlickerTimer += Time.deltaTime;
            // Slow blue pulse (1 Hz) — mimics blue marshal lights
            float pulse = 0.5f + 0.5f * Mathf.Sin(_vscFlickerTimer * Mathf.PI * 2f * 1.0f);
            var c = vscOverlay.color;
            c.a = pulse * 0.10f;
            vscOverlay.color = c;
        }

        // ═════════════════════════════════════════════════════════════════
        //  ERS BLOOM BURST  (called by VFXManager on heavy deploy)
        // ═════════════════════════════════════════════════════════════════

        public void TriggerERSBloomBurst(float deployLevel)
        {
            if (_bloom == null || deployLevel < 0.7f) return;
            StartCoroutine(ERSBloomPulse(deployLevel));
        }

        IEnumerator ERSBloomPulse(float level)
        {
            if (_bloom == null) yield break;
            float peak = Mathf.Lerp(1.0f, 2.2f, level);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                _bloom.intensity.value = Mathf.Lerp(peak, 0.5f, t / 0.2f);
                yield return null;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  ACTIVE AERO TRANSITION WARP
        // ═════════════════════════════════════════════════════════════════

        public void TriggerAeroWarp(bool openingWing)
        {
            if (_lensDist == null) return;
            StartCoroutine(AeroLensFlick(openingWing));
        }

        IEnumerator AeroLensFlick(bool open)
        {
            if (_lensDist == null) yield break;
            float peak = open ? 0.08f : -0.08f;
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                _lensDist.intensity.value = Mathf.Lerp(peak, 0f, t / 0.12f);
                _lensDist.active = true;
                yield return null;
            }
            _lensDist.active = false;
        }

        // ═════════════════════════════════════════════════════════════════
        //  ORIGINAL METHODS  (unchanged)
        // ═════════════════════════════════════════════════════════════════

        void TickBloom(float speed)
        {
            if (_bloom == null || _overtakeModeActive) return;
            float target = Mathf.Lerp(0.25f, 1.2f, Mathf.Clamp01(speed / 350f));
            _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, target, 3f * Time.deltaTime);
        }

        void TickSpeedLines(float speed)
        {
            if (speedLinesOverlay == null) return;
            float str = Mathf.Clamp01((speed - 250f) / 80f);
            speedLinesOverlay.gameObject.SetActive(str > 0.02f);
            if (str <= 0.02f) return;
            _speedLineOffset += str * 0.8f * Time.deltaTime;
            speedLinesOverlay.uvRect = new Rect(0f, _speedLineOffset, 1f, 1f);
            var c = speedLinesOverlay.color;
            c.a = str * 0.35f;
            speedLinesOverlay.color = c;
        }

        void TickVignette(float gLat)
        {
            if (_vignette == null) return;
            float target = Mathf.Lerp(0.28f, 0.58f, Mathf.Clamp01(Mathf.Abs(gLat) / 4f));
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, target, 5f * Time.deltaTime);
        }

        void TickMotionBlur(float steer, float speed)
        {
            if (_motionBlur == null) return;
            bool highTier = GraphicsSettingsManager.Instance == null ||
                            GraphicsSettingsManager.Instance.CurrentTier >= GraphicsTier.High;
            _motionBlur.active = highTier;
            if (!highTier) return;
            float target = Mathf.Abs(steer) * Mathf.Clamp01(speed / 200f) * 0.4f;
            _motionBlur.intensity.value = Mathf.Lerp(_motionBlur.intensity.value, target, 6f * Time.deltaTime);
        }

        void TickChromaSmooth()
        {
            if (_chroma == null) return;
            _chroma.intensity.value = Mathf.Lerp(_chroma.intensity.value, _chromaTarget, 8f * Time.deltaTime);
            _chromaTarget = Mathf.MoveTowards(_chromaTarget, 0f, Time.deltaTime * 0.9f);
        }

        public void TriggerImpact(float forceG)
        {
            _chromaTarget = Mathf.Max(_chromaTarget, Mathf.Clamp01(forceG / 50f));
            StartCoroutine(VignetteFlash(forceG / 50f));
        }

        IEnumerator VignetteFlash(float str)
        {
            if (_vignette == null) yield break;
            for (float t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, 0.8f * str, 20f * Time.deltaTime);
                yield return null;
            }
        }

        public void SetWetLens(bool wet, float intensity = 0.5f)
        {
            if (wetLensOverlay != null) wetLensOverlay.gameObject.SetActive(wet);
            if (_lensDist != null)
            {
                _lensDist.active = wet;
                _lensDist.intensity.value = wet ? -intensity * 0.15f : 0f;
            }
        }

        public void TriggerCelebrationBloom() => StartCoroutine(CelebBurst());

        IEnumerator CelebBurst()
        {
            if (_bloom == null) yield break;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                _bloom.intensity.value = Mathf.Lerp(2.8f, 0.5f, t / 0.5f);
                yield return null;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ORIGINAL — Car Livery System (unchanged)
    // ═══════════════════════════════════════════════════════════════════════
    public class CarLiverySystem : MonoBehaviour
    {
        public Renderer[] bodyRenderers;
        public Renderer[] accentRenderers;

        static readonly int _primary    = Shader.PropertyToID("_PrimaryColor");
        static readonly int _secondary  = Shader.PropertyToID("_SecondaryColor");
        static readonly int _metallic   = Shader.PropertyToID("_Metallic");
        static readonly int _smoothness = Shader.PropertyToID("_Smoothness");

        MaterialPropertyBlock _mpb;
        void Awake() => _mpb = new MaterialPropertyBlock();

        public void Apply(Color primary, Color secondary, float metallic = 0.7f, float smooth = 0.85f)
        {
            SetBlock(bodyRenderers,   primary,   secondary, metallic, smooth);
            SetBlock(accentRenderers, secondary, primary,   metallic, smooth);
        }

        public void SetWetSheen(float wetness)
        {
            if (bodyRenderers == null) return;
            foreach (var r in bodyRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(_smoothness, Mathf.Lerp(0.82f, 0.98f, wetness));
                r.SetPropertyBlock(_mpb);
            }
        }

        void SetBlock(Renderer[] rends, Color p, Color s, float m, float sm)
        {
            if (rends == null) return;
            foreach (var r in rends)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(_primary,    p);
                _mpb.SetColor(_secondary,  s);
                _mpb.SetFloat(_metallic,   m);
                _mpb.SetFloat(_smoothness, sm);
                r.SetPropertyBlock(_mpb);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ORIGINAL — Track Surface Renderer (unchanged)
    // ═══════════════════════════════════════════════════════════════════════
    public class TrackSurfaceRenderer : MonoBehaviour
    {
        public Renderer[] trackSections;

        static readonly int _wetness = Shader.PropertyToID("_Wetness");
        static readonly int _puddle  = Shader.PropertyToID("_PuddleIntensity");

        MaterialPropertyBlock _mpb;
        void Awake() => _mpb = new MaterialPropertyBlock();

        public void SetWetness(float wetness, float puddle)
        {
            if (trackSections == null) return;
            foreach (var r in trackSections)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(_wetness, wetness);
                _mpb.SetFloat(_puddle,  puddle);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}