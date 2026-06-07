using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RacingGame.Graphics
{
    public enum GraphicsTier { Low, Medium, High, Ultra }

    [System.Serializable]
    public class TierSettings
    {
        public GraphicsTier tier;
        public int   targetFPS         = 60;
        public int   shadowResolution  = 512;
        public int   textureLimit      = 1;
        public float renderScale       = 1.0f;
        public bool  msaa              = false;
        public bool  softShadows       = false;
        public bool  bloom             = true;
        public bool  ambientOcclusion  = false;
        public bool  motionBlur        = false;
        public bool  heatHaze          = false;
        public int   particleMaxCount  = 200;
        public float shadowDistance    = 30f;
        public int   maxLODLevel       = 0;
    }

    public class GraphicsSettingsManager : MonoBehaviour
    {
        public static GraphicsSettingsManager Instance { get; private set; }

        public UniversalRenderPipelineAsset urpAsset;
        public Volume postProcessVolume;

        public GraphicsTier CurrentTier { get; private set; } = GraphicsTier.High;

        static readonly TierSettings[] TIERS =
        {
            new() { tier=GraphicsTier.Low,    targetFPS=30, shadowResolution=256,  textureLimit=2, renderScale=0.70f,
                    msaa=false, softShadows=false, bloom=false, ambientOcclusion=false,
                    motionBlur=false, heatHaze=false, particleMaxCount=80,  shadowDistance=15f, maxLODLevel=2 },

            new() { tier=GraphicsTier.Medium, targetFPS=60, shadowResolution=512,  textureLimit=1, renderScale=0.85f,
                    msaa=false, softShadows=false, bloom=true,  ambientOcclusion=false,
                    motionBlur=false, heatHaze=false, particleMaxCount=150, shadowDistance=25f, maxLODLevel=1 },

            new() { tier=GraphicsTier.High,   targetFPS=60, shadowResolution=1024, textureLimit=0, renderScale=1.00f,
                    msaa=false, softShadows=true,  bloom=true,  ambientOcclusion=true,
                    motionBlur=true,  heatHaze=true,  particleMaxCount=300, shadowDistance=50f, maxLODLevel=0 },

            new() { tier=GraphicsTier.Ultra,  targetFPS=60, shadowResolution=2048, textureLimit=0, renderScale=1.00f,
                    msaa=true,  softShadows=true,  bloom=true,  ambientOcclusion=true,
                    motionBlur=true,  heatHaze=true,  particleMaxCount=500, shadowDistance=80f, maxLODLevel=0 }
        };

        float _thermalTimer;
        bool  _throttled;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start() => AutoDetect();

        void Update()
        {
            _thermalTimer += Time.deltaTime;
            if (_thermalTimer < 10f) return;
            _thermalTimer = 0f;
            CheckThermal();
        }

        public void AutoDetect()
        {
            int ram  = SystemInfo.systemMemorySize;
            int gpu  = SystemInfo.graphicsMemorySize;
            bool ios = Application.platform == RuntimePlatform.IPhonePlayer;

            GraphicsTier t = (ios || (ram >= 6144 && gpu >= 4096)) ? GraphicsTier.Ultra
                           : (ram >= 4096 && gpu >= 2048)           ? GraphicsTier.High
                           : (ram >= 3072)                           ? GraphicsTier.Medium
                                                                     : GraphicsTier.Low;
            ApplyTier(t);
        }

        public void ApplyTier(GraphicsTier tier)
        {
            CurrentTier = tier;
            var s = TIERS[(int)tier];

            Application.targetFrameRate         = s.targetFPS;
            QualitySettings.masterTextureLimit  = s.textureLimit;
            QualitySettings.shadowDistance      = s.shadowDistance;
            QualitySettings.maximumLODLevel     = s.maxLODLevel;
            QualitySettings.vSyncCount          = 0;

            if (urpAsset != null)
            {
                urpAsset.renderScale         = s.renderScale;
                urpAsset.shadowDistance      = s.shadowDistance;
                urpAsset.msaaSampleCount     = s.msaa ? 4 : 1;
                urpAsset.supportsSoftShadows = s.softShadows;
            }

            ApplyPostProcess(s);
        }

        void ApplyPostProcess(TierSettings s)
        {
            if (postProcessVolume?.profile == null) return;
            var p = postProcessVolume.profile;
            if (p.TryGet<Bloom>(out var b))              b.active = s.bloom;
            if (p.TryGet<AmbientOcclusion>(out var ao))  ao.active = s.ambientOcclusion;
            if (p.TryGet<MotionBlur>(out var mb))         mb.active = s.motionBlur;
        }

        void CheckThermal()
        {
            bool low = SystemInfo.batteryLevel < 0.15f &&
                       SystemInfo.batteryStatus == BatteryStatus.Discharging;
            if (low && !_throttled)
            {
                _throttled = true;
                int down = Mathf.Max(0, (int)CurrentTier - 1);
                ApplyTier((GraphicsTier)down);
                Application.targetFrameRate = 30;
            }
            else if (!low && _throttled)
            {
                _throttled = false;
                ApplyTier(CurrentTier);
            }
        }

        public IEnumerator RunBenchmark(System.Action<GraphicsTier> done)
        {
            float acc = 0f;
            for (int i = 0; i < 120; i++) { acc += 1f / Time.unscaledDeltaTime; yield return null; }
            float fps = acc / 120f;
            done?.Invoke(fps >= 55f ? GraphicsTier.High : fps >= 40f ? GraphicsTier.Medium : GraphicsTier.Low);
        }

        public TierSettings GetCurrentSettings() => TIERS[(int)CurrentTier];
    }
}
