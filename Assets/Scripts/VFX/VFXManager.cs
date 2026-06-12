using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using RacingGame.Data;
using RacingGame.Physics;
using RacingGame.Graphics;

namespace RacingGame.VFX
{
    // ═══════════════════════════════════════════════════════════════════════
    //  VFX MANAGER  –  2026 Extension
    //  Adds: ERS energy VFX, OvertakeMode glow & pulse, active aero shimmer,
    //        Madrid heat distortion, Audi/Cadillac livery FX hooks.
    //  All original systems retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════

    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        // ── Tier-aware caps ───────────────────────────────────────────────
        int _maxParticles = 300;

        // ── Original refs ─────────────────────────────────────────────────
        [Header("Tire Smoke")]
        public ParticleSystem[] tireSmokeFL;
        public ParticleSystem[] tireSmokeFR;
        public ParticleSystem[] tireSmokeRL;
        public ParticleSystem[] tireSmokeRR;

        [Header("Tire Marks")]
        public TireMarkRenderer tireMarkRenderer;

        [Header("Sparks")]
        public ParticleSystem bottomOutSparks;
        public ParticleSystem brakeSparks;

        [Header("Exhaust")]
        public ParticleSystem exhaustFlame;
        public ParticleSystem exhaustSmoke;

        [Header("DRS")]
        public ParticleSystem drsVapor;

        [Header("Rain")]
        public ParticleSystem   rainParticles;
        public ParticleSystem   sprayBehindCar;
        public RainScreenEffect rainScreen;

        [Header("Collision")]
        public ParticleSystem crashSparks;
        public ParticleSystem dustCloud;
        public ParticleSystem debrisParticles;

        [Header("Track Surface")]
        public ParticleSystem dustRoadsideLeft;
        public ParticleSystem dustRoadsideRight;

        [Header("Heat Haze")]
        public HeatHazeEffect heatHaze;

        [Header("Damage Visuals")]
        public DamageVisuals damageVisuals;

        // ── 2026 new refs ─────────────────────────────────────────────────
        [Header("— 2026: ERS VFX —")]
        public ParticleSystem  ersHarvestParticles;    // blue sparks during braking
        public ParticleSystem  ersDeployParticles;     // orange glow from sidepods
        public Light[]         ersGlowLights;          // point lights on sidepods

        [Header("— 2026: Overtake Mode —")]
        public ParticleSystem  overtakeBoostJet;       // rear exhaust plasma jet
        public ParticleSystem  overtakeFrontAura;      // front diffuser glow
        public Light           overtakeGlowLight;      // main body rim light
        public Renderer[]      overtakeGlowRenderers;  // car body emissive
        public float           overtakeGlowIntensity = 3.5f;

        [Header("— 2026: Active Aero —")]
        public ParticleSystem  aeroTransitionVapor;    // condensation on wing flip
        public Light           aeroStatusLight;        // cockpit indicator

        [Header("— 2026: Madrid Heat —")]
        public ParticleSystem  madridTarmacHeat;       // heat shimmer off track
        public Light           madridSunLight;

        [Header("— 2026: Tyre Cliff —")]
        public ParticleSystem  tyreCliffSmoke;         // heavy smoke when tyre cliffs
        public Light           tyreWarningLight;

        // ── Internal ──────────────────────────────────────────────────────
        PhysicsIntegrator _physics;
        GraphicsTier      _tier = GraphicsTier.High;

        Vector3[] _lastTirePos  = new Vector3[4];
        const float MARK_INTERVAL = 0.08f;
        float _markTimer;

        // Overtake mode state
        bool  _overtakeModeActive;
        float _overtakeGlowTimer;
        float _overtakePulseFreq = 3.5f;   // Hz

        // ERS glow target
        float _ersGlowTarget;
        float _ersGlowCurrent;

        // Aero transition flash timer
        float _aeroFlashTimer;

        // Tyre cliff per wheel
        bool[] _tyreInCliff = new bool[4];

        // Madrid flag
        bool _isMadrid;

        // Emissive shader IDs
        static readonly int _emissionColorId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _physics = GetComponentInParent<PhysicsIntegrator>();

            if (GraphicsSettingsManager.Instance != null)
            {
                _tier         = GraphicsSettingsManager.Instance.CurrentTier;
                _maxParticles = GraphicsSettingsManager.Instance.GetCurrentSettings().particleMaxCount;
            }

            CapAllParticleSystems(_maxParticles);
        }

        void Update()
        {
            if (_physics == null) return;
            var s = _physics.State;

            // Original systems
            UpdateTireSmoke(s);
            UpdateExhaust(s);
            UpdateDRS(s);
            UpdateTireMarks(s);
            UpdateRoadsideDust(s);
            UpdateDamageVisuals(s);

            // 2026 systems
            UpdateERSVFX(s);
            UpdateOvertakeModeVFX(s);
            UpdateAeroTransitionVFX(s);
            UpdateMadridHeat(s);
            UpdateTyreCliffVFX(s);
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: ERS VFX
        // ═════════════════════════════════════════════════════════════════

        void UpdateERSVFX(VehicleState s)
        {
            // ── Harvest (blue sparks during heavy braking) ────────────────
            if (ersHarvestParticles != null)
            {
                bool harvesting = s.brake > 0.4f && s.speedMs > 30f;
                var em = ersHarvestParticles.emission;
                em.rateOverTime = harvesting
                    ? s.brake * s.speedMs * 0.8f * (_tier >= GraphicsTier.High ? 1f : 0.5f)
                    : 0f;

                if (harvesting && !ersHarvestParticles.isPlaying) ersHarvestParticles.Play();
                else if (!harvesting)                              ersHarvestParticles.Stop();

                // Blue tint on harvest particles
                if (harvesting && _tier >= GraphicsTier.High)
                {
                    var main = ersHarvestParticles.main;
                    main.startColor = new Color(0.2f, 0.5f, 1.0f, 0.9f);
                }
            }

            // ── Deploy (orange sidepod glow) ──────────────────────────────
            float deployLevel = Mathf.Clamp01(s.ersBoostForce / 8000f);
            _ersGlowTarget  = deployLevel;
            _ersGlowCurrent = Mathf.Lerp(_ersGlowCurrent, _ersGlowTarget, 10f * Time.deltaTime);

            if (ersDeployParticles != null)
            {
                var em = ersDeployParticles.emission;
                em.rateOverTime = deployLevel > 0.1f
                    ? deployLevel * 40f * (_tier >= GraphicsTier.Medium ? 1f : 0.4f)
                    : 0f;

                var main = ersDeployParticles.main;
                // Orange deploy → red in Overtake mode
                main.startColor = _overtakeModeActive
                    ? new Color(1.0f, 0.2f, 0.05f, 0.85f)
                    : new Color(1.0f, 0.55f, 0.1f, 0.75f);
            }

            // ── Sidepod point lights ──────────────────────────────────────
            if (ersGlowLights != null && _tier >= GraphicsTier.Medium)
            {
                foreach (var light in ersGlowLights)
                {
                    if (light == null) continue;
                    light.intensity = _ersGlowCurrent * 2.8f;
                    light.color     = _overtakeModeActive
                        ? new Color(1f, 0.2f, 0.05f)
                        : new Color(1f, 0.55f, 0.1f);
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: OVERTAKE MODE GLOW & PULSE
        // ═════════════════════════════════════════════════════════════════

        public void SetOvertakeMode(bool active)
        {
            if (_overtakeModeActive == active) return;
            _overtakeModeActive = active;

            if (active)
            {
                _overtakeGlowTimer = 0f;
                overtakeBoostJet?.Play();
                overtakeFrontAura?.Play();
                StartCoroutine(OvertakeGlowPulse());
            }
            else
            {
                overtakeBoostJet?.Stop();
                overtakeFrontAura?.Stop();
                ClearOvertakeGlow();
            }
        }

        IEnumerator OvertakeGlowPulse()
        {
            while (_overtakeModeActive)
            {
                _overtakeGlowTimer += Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(_overtakeGlowTimer * _overtakePulseFreq * Mathf.PI * 2f);
                float glowValue = overtakeGlowIntensity * pulse;

                // Rim light
                if (overtakeGlowLight != null)
                {
                    overtakeGlowLight.intensity = glowValue * 1.5f;
                    overtakeGlowLight.color     = new Color(1f, 0.15f, 0.0f);
                }

                // Emissive on body panels
                if (overtakeGlowRenderers != null && _tier >= GraphicsTier.High)
                {
                    Color emissive = new Color(1f, 0.15f, 0f) * glowValue;
                    foreach (var r in overtakeGlowRenderers)
                    {
                        if (r == null) continue;
                        var mpb = new MaterialPropertyBlock();
                        r.GetPropertyBlock(mpb);
                        mpb.SetColor(_emissionColorId, emissive);
                        r.SetPropertyBlock(mpb);
                    }
                }

                yield return null;
            }
            ClearOvertakeGlow();
        }

        void ClearOvertakeGlow()
        {
            if (overtakeGlowLight != null)
                overtakeGlowLight.intensity = 0f;

            if (overtakeGlowRenderers != null)
            {
                foreach (var r in overtakeGlowRenderers)
                {
                    if (r == null) continue;
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor(_emissionColorId, Color.black);
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: ACTIVE AERO TRANSITION VFX
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Call when active aero wing angle changes by >8°.</summary>
        public void TriggerAeroTransition(bool openingWing)
        {
            if (aeroTransitionVapor == null || _tier < GraphicsTier.Medium) return;

            aeroTransitionVapor.Emit(openingWing ? 12 : 6);

            // Cockpit indicator light: green = low drag, amber = high downforce
            if (aeroStatusLight != null)
            {
                aeroStatusLight.color     = openingWing ? COL_AERO_OPEN : COL_AERO_CLOSED;
                aeroStatusLight.intensity = 1.8f;
                _aeroFlashTimer           = 0.4f;
            }
        }

        void UpdateAeroTransitionVFX(VehicleState s)
        {
            if (_aeroFlashTimer > 0f)
            {
                _aeroFlashTimer -= Time.deltaTime;
                if (aeroStatusLight != null)
                    aeroStatusLight.intensity = Mathf.Lerp(0f, 1.8f, _aeroFlashTimer / 0.4f);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: MADRID HEAT SHIMMER
        // ═════════════════════════════════════════════════════════════════

        public void SetMadridMode(bool active)
        {
            _isMadrid = active;
            if (madridTarmacHeat != null)
            {
                if (active) madridTarmacHeat.Play();
                else        madridTarmacHeat.Stop();
            }
            if (madridSunLight != null)
                madridSunLight.colorTemperature = active ? 5500f : 6500f;  // warmer Madrid sun
        }

        void UpdateMadridHeat(VehicleState s)
        {
            if (!_isMadrid || madridTarmacHeat == null) return;
            // Shimmer more intense at low speeds (more time over hot tarmac)
            var em = madridTarmacHeat.emission;
            em.rateOverTime = Mathf.Lerp(40f, 8f, Mathf.Clamp01(s.speed / 200f));
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: TYRE CLIFF VFX
        // ═════════════════════════════════════════════════════════════════

        public void SetTyreCliff(int tyreIndex, bool inCliff)
        {
            if (tyreIndex < 0 || tyreIndex >= 4) return;
            if (_tyreInCliff[tyreIndex] == inCliff) return;
            _tyreInCliff[tyreIndex] = inCliff;

            bool anyCliff = System.Array.Exists(_tyreInCliff, b => b);

            if (tyreWarningLight != null)
            {
                tyreWarningLight.intensity = anyCliff ? 2.5f : 0f;
                tyreWarningLight.color     = Color.red;
            }
        }

        void UpdateTyreCliffVFX(VehicleState s)
        {
            bool anyCliff = System.Array.Exists(_tyreInCliff, b => b);
            if (tyreCliffSmoke == null) return;

            var em = tyreCliffSmoke.emission;
            em.rateOverTime = anyCliff ? 35f : 0f;
            if (anyCliff && !tyreCliffSmoke.isPlaying) tyreCliffSmoke.Play();
            else if (!anyCliff && tyreCliffSmoke.isPlaying) tyreCliffSmoke.Stop();

            if (anyCliff)
            {
                var main = tyreCliffSmoke.main;
                main.startColor = new Color(0.7f, 0.65f, 0.6f, 0.9f);  // grey rubber smoke
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  ORIGINAL SYSTEMS  (unchanged)
        // ═════════════════════════════════════════════════════════════════

        void UpdateTireSmoke(VehicleState s)
        {
            float brakeSmoke = s.brake > 0.65f ? (s.brake - 0.65f) * 3.5f * s.speedMs * 0.4f : 0f;
            float rearSlip   = s.throttle > 0.75f && s.speed < 130f
                             ? (s.throttle - 0.70f) * 2.5f * 25f : 0f;

            SetEmission(tireSmokeFL, brakeSmoke);
            SetEmission(tireSmokeFR, brakeSmoke);
            SetEmission(tireSmokeRL, rearSlip);
            SetEmission(tireSmokeRR, rearSlip);

            float avgSurface = 0f;
            foreach (var t in s.tires) avgSurface += t.surfaceTemp;
            avgSurface /= 4f;
            float heat = Mathf.Clamp01((avgSurface - 80f) / 60f);
            TintSmoke(tireSmokeFL, heat);
            TintSmoke(tireSmokeFR, heat);
        }

        static void TintSmoke(ParticleSystem[] systems, float heatFactor)
        {
            if (systems == null) return;
            Color cold = new(0.95f, 0.95f, 0.95f, 0.6f);
            Color hot  = new(0.35f, 0.30f, 0.28f, 0.8f);
            Color col  = Color.Lerp(cold, hot, heatFactor);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.startColor = col;
            }
        }

        void UpdateExhaust(VehicleState s)
        {
            if (exhaustFlame != null)
            {
                var em = exhaustFlame.emission;
                bool overrun = s.throttle < 0.05f && s.rpm > 11500f;
                em.rateOverTime = overrun ? 60f : 0f;
                var main = exhaustFlame.main;
                main.startColor = s.engineMode == EngineMode.Overtake
                    ? new Color(1f, 0.55f, 0.1f)
                    : new Color(0.85f, 0.25f, 0.05f);
            }
            if (exhaustSmoke != null)
            {
                var em = exhaustSmoke.emission;
                em.rateOverTime = s.engineMode == EngineMode.Push ? 12f : 3f;
            }
        }

        void UpdateDRS(VehicleState s)
        {
            if (drsVapor == null) return;
            var em = drsVapor.emission;
            em.rateOverTime = s.drsActive ? 18f : 0f;
            if (s.drsActive && !drsVapor.isPlaying) drsVapor.Play();
            else if (!s.drsActive && drsVapor.isPlaying) drsVapor.Stop();
        }

        void UpdateTireMarks(VehicleState s)
        {
            if (tireMarkRenderer == null || _tier < GraphicsTier.Medium) return;
            _markTimer += Time.deltaTime;
            if (_markTimer < MARK_INTERVAL) return;
            _markTimer = 0f;

            bool heavyBrake = s.brake > 0.7f;
            bool wheelspin  = s.throttle > 0.8f && s.speed < 100f;
            bool hardCorner = Mathf.Abs(s.steering) > 0.6f && s.speed > 80f;

            if (heavyBrake || wheelspin || hardCorner)
            {
                Vector3 right = Vector3.Cross(Vector3.up, s.velocity.normalized);
                tireMarkRenderer.AddMark(s.position + right *  0.75f);
                tireMarkRenderer.AddMark(s.position - right *  0.75f);
            }
        }

        void UpdateRoadsideDust(VehicleState s) { }

        public void TriggerCurbDust(bool leftSide)
        {
            var ps = leftSide ? dustRoadsideLeft : dustRoadsideRight;
            if (ps == null) return;
            ps.Emit(Mathf.RoundToInt(20f * (_tier >= GraphicsTier.High ? 2f : 1f)));
        }

        void UpdateDamageVisuals(VehicleState s) =>
            damageVisuals?.UpdateFrontWingVisual(s.damage.frontWing);

        public void SetRainIntensity(float intensity)
        {
            if (rainParticles != null)
            {
                var em = rainParticles.emission;
                em.rateOverTime = intensity * (_tier >= GraphicsTier.High ? 900f : 400f);
                if (intensity > 0.05f && !rainParticles.isPlaying) rainParticles.Play();
                else if (intensity <= 0.05f) rainParticles.Stop();
            }
            if (sprayBehindCar != null && _physics != null)
            {
                var em = sprayBehindCar.emission;
                em.rateOverTime = intensity * _physics.State.speed * 0.45f;
            }
            rainScreen?.SetIntensity(intensity);
        }

        public void PlayCollisionVFX(Vector3 position, float forceG)
        {
            if (forceG < 5f) return;
            if (crashSparks != null)
            {
                crashSparks.transform.position = position;
                crashSparks.Emit(Mathf.Min(Mathf.RoundToInt(forceG * 3f), _maxParticles / 4));
            }
            if (dustCloud != null && forceG > 18f) { dustCloud.transform.position = position; dustCloud.Emit(30); }
            if (debrisParticles != null && forceG > 30f && _tier >= GraphicsTier.High)
            { debrisParticles.transform.position = position; debrisParticles.Emit(15); }
            if (brakeSparks != null && forceG > 25f) brakeSparks.Play();
        }

        public void PlayBottomOutSparks() => bottomOutSparks?.Play();

        static void SetEmission(ParticleSystem[] systems, float rate)
        {
            if (systems == null) return;
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var em = ps.emission;
                em.rateOverTime = Mathf.Max(0f, rate);
                if (rate > 0.5f && !ps.isPlaying) ps.Play();
                else if (rate <= 0.5f && ps.isPlaying) ps.Stop();
            }
        }

        void CapAllParticleSystems(int maxCount)
        {
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                if (main.maxParticles > maxCount) main.maxParticles = maxCount;
            }
        }

        // ── Color constants ───────────────────────────────────────────────
        static readonly Color COL_AERO_OPEN   = new(0.2f, 1.0f, 0.3f);   // green = low drag
        static readonly Color COL_AERO_CLOSED = new(1.0f, 0.6f, 0.1f);   // amber = max downforce
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ORIGINAL CLASSES  (retained verbatim)
    // ═══════════════════════════════════════════════════════════════════════

    public class TireMarkRenderer : MonoBehaviour
    {
        [Header("Decal")]
        public Material  tireMarkMaterial;
        public float     markWidth    = 0.22f;
        public float     markFadeTime = 18f;
        public int       maxMarks     = 64;

        struct Mark { public Vector3 position; public float opacity; public float birthTime; }

        readonly List<Mark>      _marks    = new();
        readonly List<Matrix4x4> _matrices = new();
        readonly List<Vector4>   _colors   = new();

        static readonly int ColorProp = Shader.PropertyToID("_Color");
        MaterialPropertyBlock _mpb;

        void Awake() => _mpb = new MaterialPropertyBlock();

        public void AddMark(Vector3 worldPos)
        {
            if (_marks.Count >= maxMarks) _marks.RemoveAt(0);
            _marks.Add(new Mark { position = worldPos, opacity = 1f, birthTime = Time.time });
        }

        void Update()
        {
            if (tireMarkMaterial == null) return;
            _matrices.Clear(); _colors.Clear();

            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                var m = _marks[i];
                float t = (Time.time - m.birthTime) / markFadeTime;
                if (t >= 1f) { _marks.RemoveAt(i); continue; }
                float op = 1f - t;
                _matrices.Add(Matrix4x4.TRS(m.position + Vector3.up * 0.01f, Quaternion.identity, new Vector3(markWidth, 0.001f, markWidth)));
                _colors.Add(new Vector4(0.05f, 0.05f, 0.05f, op));
                _marks[i] = new Mark { position = m.position, opacity = op, birthTime = m.birthTime };
            }

            if (_matrices.Count == 0) return;
            _mpb.SetVectorArray(ColorProp, _colors);
            Graphics.DrawMeshInstanced(GetComponent<MeshFilter>()?.sharedMesh, 0, tireMarkMaterial, _matrices, _mpb);
        }
    }

    public class HeatHazeEffect : MonoBehaviour
    {
        public Renderer hazeRenderer;
        public float    maxDistortion = 0.04f;
        public float    minSpeed      = 180f;

        PhysicsIntegrator _physics;
        static readonly int DistortProp = Shader.PropertyToID("_DistortAmount");

        void Start() => _physics = GetComponentInParent<PhysicsIntegrator>();

        void Update()
        {
            if (hazeRenderer == null || _physics == null) return;
            float t = Mathf.Clamp01((_physics.State.speed - minSpeed) / 120f);
            hazeRenderer.enabled = t > 0.05f;
            hazeRenderer.material.SetFloat(DistortProp, t * maxDistortion);
        }
    }

    public class RainScreenEffect : MonoBehaviour
    {
        public UnityEngine.UI.RawImage screenOverlay;
        public Texture2D               rainTexture;
        public float scrollSpeed = 0.25f;

        float _rainIntensity;
        float _offsetY;

        public void SetIntensity(float intensity) => _rainIntensity = intensity;

        void Update()
        {
            if (screenOverlay == null) return;
            screenOverlay.gameObject.SetActive(_rainIntensity > 0.05f);
            _offsetY += scrollSpeed * _rainIntensity * Time.deltaTime;
            screenOverlay.uvRect = new Rect(0f, _offsetY, 1f, 1f);
            var c = screenOverlay.color;
            c.a = Mathf.Lerp(c.a, _rainIntensity * 0.65f, 4f * Time.deltaTime);
            screenOverlay.color = c;
        }
    }

    public class CrowdBillboardSystem : MonoBehaviour
    {
        public Mesh     billboardMesh;
        public Material crowdMaterial;
        public int      crowdCount   = 500;
        public Vector3  spawnCenter;
        public Vector3  spawnExtents = new(80f, 0f, 5f);
        public float    billboardHeight = 1.8f;

        Matrix4x4[] _matrices;
        Camera      _cam;

        void Start()
        {
            _cam = Camera.main;
            _matrices = new Matrix4x4[crowdCount];
            var rng = new System.Random(42);
            for (int i = 0; i < crowdCount; i++)
            {
                float x = spawnCenter.x + (float)(rng.NextDouble() * 2 - 1) * spawnExtents.x;
                float z = spawnCenter.z + (float)(rng.NextDouble() * 2 - 1) * spawnExtents.z;
                _matrices[i] = Matrix4x4.TRS(new Vector3(x, spawnCenter.y, z), Quaternion.identity, new Vector3(0.6f, billboardHeight, 1f));
            }
        }

        void Update()
        {
            if (billboardMesh == null || crowdMaterial == null || _cam == null) return;
            Vector3 camPos = _cam.transform.position;
            for (int i = 0; i < _matrices.Length; i++)
            {
                Vector3 pos = _matrices[i].GetColumn(3);
                Vector3 dir = new Vector3(camPos.x - pos.x, 0f, camPos.z - pos.z).normalized;
                _matrices[i] = Matrix4x4.TRS(pos, Quaternion.LookRotation(dir), new Vector3(0.6f, billboardHeight, 1f));
            }
            Graphics.DrawMeshInstanced(billboardMesh, 0, crowdMaterial, _matrices);
        }
    }

    public class LensFlareController : MonoBehaviour
    {
        public LensFlareComponentSRP   sunFlare;
        public LensFlareComponentSRP[] brakeLightFlares;

        PhysicsIntegrator _physics;
        void Start() => _physics = GetComponentInParent<PhysicsIntegrator>();

        void Update()
        {
            if (_physics != null && brakeLightFlares != null)
            {
                float brakeIntensity = _physics.State.brake;
                foreach (var fl in brakeLightFlares)
                    if (fl != null) fl.intensity = brakeIntensity * 2.5f;
            }
        }
    }

    public class DamageVisuals : MonoBehaviour
    {
        [Header("Front Wing")]
        public GameObject frontWingNormal;
        public GameObject frontWingDamaged;
        public GameObject frontWingBroken;
        [Header("Rear Wing")]
        public GameObject rearWingNormal;
        public GameObject rearWingDamaged;
        [Header("Bodywork")]
        public GameObject bodyworkClean;
        public GameObject bodyworkScratched;
        [Header("Thresholds")]
        public float damagedThreshold = 30f;
        public float brokenThreshold  = 70f;

        public void UpdateFrontWingVisual(float pct)
        {
            if (frontWingNormal)  frontWingNormal.SetActive(pct < damagedThreshold);
            if (frontWingDamaged) frontWingDamaged.SetActive(pct >= damagedThreshold && pct < brokenThreshold);
            if (frontWingBroken)  frontWingBroken.SetActive(pct >= brokenThreshold);
        }

        public void UpdateRearWingVisual(float pct)
        {
            if (rearWingNormal)  rearWingNormal.SetActive(pct < brokenThreshold);
            if (rearWingDamaged) rearWingDamaged.SetActive(pct >= brokenThreshold);
        }

        public void UpdateBodywork(float frontWingPct, float rearWingPct)
        {
            float combined = (frontWingPct + rearWingPct) * 0.5f;
            if (bodyworkClean)     bodyworkClean.SetActive(combined < damagedThreshold);
            if (bodyworkScratched) bodyworkScratched.SetActive(combined >= damagedThreshold);
        }
    }
}