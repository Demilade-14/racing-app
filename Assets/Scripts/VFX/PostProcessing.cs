using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RacingGame.Physics;
using RacingGame.Graphics;

namespace RacingGame.VFX
{
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

        // Cached effects
        Bloom              _bloom;
        ChromaticAberration _chroma;
        Vignette           _vignette;
        MotionBlur         _motionBlur;
        ColorAdjustments   _colorAdj;
        LensDistortion     _lensDist;
        FilmGrain          _grain;

        float _chromaTarget;
        float _speedLineOffset;

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
            }
        }

        void Update()
        {
            if (playerPhysics == null) return;
            var s = playerPhysics.State;

            TickBloom(s.speed);
            TickSpeedLines(s.speed);
            TickVignette(s.gLateral);
            TickMotionBlur(s.steering, s.speed);
            TickChromaSmooth();
        }

        void TickBloom(float speed)
        {
            if (_bloom == null) return;
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

        // ── Public API ────────────────────────────────────────────────────

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

        public void SetNightMode(bool night)
        {
            if (_colorAdj == null) return;
            _colorAdj.colorFilter.value  = night ? new Color(0.70f, 0.82f, 1.0f)
                                                  : new Color(1.00f, 0.97f, 0.9f);
            _colorAdj.postExposure.value = night ? -0.5f : 0f;
            if (_grain != null) { _grain.active = night; _grain.intensity.value = 0.22f; }
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
    //  CAR LIVERY SYSTEM
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
            SetBlock(bodyRenderers,   primary, secondary, metallic, smooth);
            SetBlock(accentRenderers, secondary, primary, metallic, smooth);
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
                _mpb.SetColor(_primary, p);
                _mpb.SetColor(_secondary, s);
                _mpb.SetFloat(_metallic,   m);
                _mpb.SetFloat(_smoothness, sm);
                r.SetPropertyBlock(_mpb);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TRACK SURFACE (wetness shader driver)
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
