using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using RacingGame.Data;
using RacingGame.Physics;
using RacingGame.Graphics;

namespace RacingGame.VFX
{
    // ═══════════════════════════════════════════════════════════════════════
    //  TIRE MARK RENDERER  (GPU-instanced decals on track surface)
    // ═══════════════════════════════════════════════════════════════════════
    public class TireMarkRenderer : MonoBehaviour
    {
        [Header("Decal")]
        public Material  tireMarkMaterial;
        public float     markWidth    = 0.22f;
        public float     markFadeTime = 18f;
        public int       maxMarks     = 64;

        struct Mark
        {
            public Vector3 position;
            public float   opacity;
            public float   birthTime;
        }

        readonly List<Mark>    _marks     = new();
        readonly List<Matrix4x4> _matrices = new();
        readonly List<Vector4>   _colors   = new();

        static readonly int ColorProp = Shader.PropertyToID("_Color");

        MaterialPropertyBlock _mpb;

        void Awake() => _mpb = new MaterialPropertyBlock();

        public void AddMark(Vector3 worldPos)
        {
            if (_marks.Count >= maxMarks) _marks.RemoveAt(0);
            _marks.Add(new Mark
            { position = worldPos, opacity = 1f, birthTime = Time.time });
        }

        void Update()
        {
            if (tireMarkMaterial == null) return;
            _matrices.Clear();
            _colors.Clear();

            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                var m   = _marks[i];
                float t = (Time.time - m.birthTime) / markFadeTime;
                if (t >= 1f) { _marks.RemoveAt(i); continue; }

                float op = 1f - t;
                _matrices.Add(Matrix4x4.TRS(
                    m.position + Vector3.up * 0.01f,
                    Quaternion.identity,
                    new Vector3(markWidth, 0.001f, markWidth)));
                _colors.Add(new Vector4(0.05f, 0.05f, 0.05f, op));
                _marks[i] = new Mark { position = m.position, opacity = op, birthTime = m.birthTime };
            }

            if (_matrices.Count == 0) return;

            // Draw instanced
            _mpb.SetVectorArray(ColorProp, _colors);
            Graphics.DrawMeshInstanced(
                GetComponent<MeshFilter>()?.sharedMesh,
                0, tireMarkMaterial, _matrices, _mpb);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HEAT HAZE  (screen-space distortion behind exhaust)
    // ═══════════════════════════════════════════════════════════════════════
    public class HeatHazeEffect : MonoBehaviour
    {
        public Renderer hazeRenderer;
        public float    maxDistortion = 0.04f;
        public float    minSpeed      = 180f;   // km/h before haze appears

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

    // ═══════════════════════════════════════════════════════════════════════
    //  RAIN SCREEN EFFECT  (drip UI overlay + windscreen water)
    // ═══════════════════════════════════════════════════════════════════════
    public class RainScreenEffect : MonoBehaviour
    {
        [Header("Canvas overlay")]
        public UnityEngine.UI.RawImage screenOverlay;
        public Texture2D               rainTexture;

        [Header("Speed of drip scroll")]
        public float scrollSpeed = 0.25f;

        float _rainIntensity;
        float _offsetY;

        static readonly int AlphaProp = Shader.PropertyToID("_Alpha");

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

    // ═══════════════════════════════════════════════════════════════════════
    //  CROWD BILLBOARD SYSTEM  (GPU-instanced impostors in grandstands)
    // ═══════════════════════════════════════════════════════════════════════
    public class CrowdBillboardSystem : MonoBehaviour
    {
        public Mesh      billboardMesh;
        public Material  crowdMaterial;
        public int       crowdCount   = 500;
        public Vector3   spawnCenter;
        public Vector3   spawnExtents = new(80f, 0f, 5f);
        public float     billboardHeight = 1.8f;

        Matrix4x4[]  _matrices;
        Camera       _cam;

        void Start()
        {
            _cam     = Camera.main;
            _matrices = new Matrix4x4[crowdCount];
            var rng  = new System.Random(42);

            for (int i = 0; i < crowdCount; i++)
            {
                float x = spawnCenter.x + (float)(rng.NextDouble() * 2 - 1) * spawnExtents.x;
                float z = spawnCenter.z + (float)(rng.NextDouble() * 2 - 1) * spawnExtents.z;
                _matrices[i] = Matrix4x4.TRS(
                    new Vector3(x, spawnCenter.y, z),
                    Quaternion.identity,
                    new Vector3(0.6f, billboardHeight, 1f));
            }
        }

        void Update()
        {
            if (billboardMesh == null || crowdMaterial == null || _cam == null) return;

            // Face billboards toward camera (Y-axis only)
            Vector3 camPos = _cam.transform.position;
            for (int i = 0; i < _matrices.Length; i++)
            {
                Vector3 pos = _matrices[i].GetColumn(3);
                Vector3 dir = new Vector3(camPos.x - pos.x, 0f, camPos.z - pos.z).normalized;
                _matrices[i] = Matrix4x4.TRS(pos,
                    Quaternion.LookRotation(dir),
                    new Vector3(0.6f, billboardHeight, 1f));
            }

            Graphics.DrawMeshInstanced(billboardMesh, 0, crowdMaterial, _matrices);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LENS FLARE CONTROLLER  (URP lens flare on sun + headlights)
    // ═══════════════════════════════════════════════════════════════════════
    public class LensFlareController : MonoBehaviour
    {
        public LensFlareComponentSRP sunFlare;
        public LensFlareComponentSRP[] brakeLightFlares;

        PhysicsIntegrator _physics;

        void Start() => _physics = GetComponentInParent<PhysicsIntegrator>();

        void Update()
        {
            // Brake lights intensity from brake input
            if (_physics != null && brakeLightFlares != null)
            {
                float brakeIntensity = _physics.State.brake;
                foreach (var fl in brakeLightFlares)
                    if (fl != null) fl.intensity = brakeIntensity * 2.5f;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FULL VFX MANAGER (rebuilt)
    // ═══════════════════════════════════════════════════════════════════════
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        // ── Tier-aware caps ───────────────────────────────────────────────
        int _maxParticles = 300;

        [Header("Tire Smoke (FL/FR/RL/RR arrays)")]
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
        public ParticleSystem rainParticles;
        public ParticleSystem sprayBehindCar;
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

        PhysicsIntegrator _physics;
        GraphicsTier      _tier = GraphicsTier.High;

        // Per-tire mark tracking
        Vector3[] _lastTirePos = new Vector3[4];
        const float MARK_INTERVAL = 0.08f;
        float _markTimer;

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
                _tier = GraphicsSettingsManager.Instance.CurrentTier;
                _maxParticles = GraphicsSettingsManager.Instance.GetCurrentSettings().particleMaxCount;
            }

            CapAllParticleSystems(_maxParticles);
        }

        void Update()
        {
            if (_physics == null) return;
            var s = _physics.State;

            UpdateTireSmoke(s);
            UpdateExhaust(s);
            UpdateDRS(s);
            UpdateTireMarks(s);
            UpdateRoadsideDust(s);
            UpdateDamageVisuals(s);
        }

        // ── Tire smoke ────────────────────────────────────────────────────
        void UpdateTireSmoke(VehicleState s)
        {
            float brakeSmoke = s.brake > 0.65f ? (s.brake - 0.65f) * 3.5f * s.speedMs * 0.4f : 0f;
            float rearSlip   = s.throttle > 0.75f && s.speed < 130f
                             ? (s.throttle - 0.70f) * 2.5f * 25f : 0f;

            // Front: brake smoke
            SetEmission(tireSmokeFL, brakeSmoke);
            SetEmission(tireSmokeFR, brakeSmoke);

            // Rear: wheelspin + light oversteer smoke
            SetEmission(tireSmokeRL, rearSlip);
            SetEmission(tireSmokeRR, rearSlip);

            // Color shift: white cold → grey hot
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
            Color cold = new Color(0.95f, 0.95f, 0.95f, 0.6f);
            Color hot  = new Color(0.35f, 0.30f, 0.28f, 0.8f);
            Color col  = Color.Lerp(cold, hot, heatFactor);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.startColor = col;
            }
        }

        // ── Exhaust flames & smoke ────────────────────────────────────────
        void UpdateExhaust(VehicleState s)
        {
            // Overrun flames: throttle cut at high RPM (upshift)
            if (exhaustFlame != null)
            {
                var em = exhaustFlame.emission;
                bool overrun = s.throttle < 0.05f && s.rpm > 11500f;
                em.rateOverTime = overrun ? 60f : 0f;

                // Color: blue lean at high RPM → orange rich
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

        // ── DRS vapor ─────────────────────────────────────────────────────
        void UpdateDRS(VehicleState s)
        {
            if (drsVapor == null) return;
            var em = drsVapor.emission;
            em.rateOverTime = s.drsActive ? 18f : 0f;
            if (s.drsActive && !drsVapor.isPlaying) drsVapor.Play();
            else if (!s.drsActive && drsVapor.isPlaying) drsVapor.Stop();
        }

        // ── Tire marks on track ───────────────────────────────────────────
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
                // Place marks at front-left and front-right wheel positions
                Vector3 right = Vector3.Cross(Vector3.up, s.velocity.normalized);
                tireMarkRenderer.AddMark(s.position + right *  0.75f);
                tireMarkRenderer.AddMark(s.position - right *  0.75f);
            }
        }

        // ── Roadside dust when going wide ─────────────────────────────────
        void UpdateRoadsideDust(VehicleState s)
        {
            // Triggered externally when car goes over curb — emit burst
        }

        public void TriggerCurbDust(bool leftSide)
        {
            var ps = leftSide ? dustRoadsideLeft : dustRoadsideRight;
            if (ps == null) return;
            ps.Emit(Mathf.RoundToInt(20f * (_tier >= GraphicsTier.High ? 2f : 1f)));
        }

        // ── Damage visuals sync ───────────────────────────────────────────
        void UpdateDamageVisuals(VehicleState s)
        {
            damageVisuals?.UpdateFrontWingVisual(s.damage.frontWing);
        }

        // ── Rain ──────────────────────────────────────────────────────────
        public void SetRainIntensity(float intensity)
        {
            // World rain
            if (rainParticles != null)
            {
                var em = rainParticles.emission;
                em.rateOverTime = intensity * (_tier >= GraphicsTier.High ? 900f : 400f);
                if (intensity > 0.05f && !rainParticles.isPlaying) rainParticles.Play();
                else if (intensity <= 0.05f) rainParticles.Stop();
            }

            // Car spray
            if (sprayBehindCar != null && _physics != null)
            {
                var em = sprayBehindCar.emission;
                em.rateOverTime = intensity * _physics.State.speed * 0.45f;
            }

            // Screen drops
            rainScreen?.SetIntensity(intensity);
        }

        // ── Collision ─────────────────────────────────────────────────────
        public void PlayCollisionVFX(Vector3 position, float forceG)
        {
            if (forceG < 5f) return;

            if (crashSparks != null)
            {
                crashSparks.transform.position = position;
                int count = Mathf.RoundToInt(forceG * 3f);
                crashSparks.Emit(Mathf.Min(count, _maxParticles / 4));
            }

            if (dustCloud != null && forceG > 18f)
            {
                dustCloud.transform.position = position;
                dustCloud.Emit(30);
            }

            if (debrisParticles != null && forceG > 30f && _tier >= GraphicsTier.High)
            {
                debrisParticles.transform.position = position;
                debrisParticles.Emit(15);
            }

            if (brakeSparks != null && forceG > 25f) brakeSparks.Play();
        }

        public void PlayBottomOutSparks() => bottomOutSparks?.Play();

        // ── Helpers ───────────────────────────────────────────────────────
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
                if (main.maxParticles > maxCount)
                    main.maxParticles = maxCount;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DAMAGE VISUALS
    // ═══════════════════════════════════════════════════════════════════════
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
            bool normal  = pct < damagedThreshold;
            bool damaged = pct >= damagedThreshold && pct < brokenThreshold;
            bool broken  = pct >= brokenThreshold;
            if (frontWingNormal)  frontWingNormal.SetActive(normal);
            if (frontWingDamaged) frontWingDamaged.SetActive(damaged);
            if (frontWingBroken)  frontWingBroken.SetActive(broken);
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
