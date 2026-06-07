using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RacingGame.Graphics;

namespace RacingGame.Utils
{
    // ═══════════════════════════════════════════════════════════════════════
    //  OBJECT POOL
    // ═══════════════════════════════════════════════════════════════════════
    public class ObjectPool : MonoBehaviour
    {
        [Serializable]
        public class PoolEntry
        {
            public string     tag;
            public GameObject prefab;
            public int        initialSize;
        }

        public List<PoolEntry> entries = new();

        readonly Dictionary<string, Queue<GameObject>> _pools     = new();
        readonly Dictionary<string, GameObject>        _prefabMap = new();

        void Awake()
        {
            foreach (var e in entries)
            {
                var q = new Queue<GameObject>();
                _prefabMap[e.tag] = e.prefab;
                for (int i = 0; i < e.initialSize; i++)
                    q.Enqueue(Spawn(e.prefab));
                _pools[e.tag] = q;
            }
        }

        public GameObject Get(string tag, Vector3 pos, Quaternion rot)
        {
            if (!_pools.TryGetValue(tag, out var q)) return null;
            var obj = q.Count > 0 ? q.Dequeue() : Spawn(_prefabMap[tag]);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
            return obj;
        }

        public void Return(string tag, GameObject obj, float delay = 0f)
        {
            if (delay > 0f) StartCoroutine(ReturnAfter(tag, obj, delay));
            else            DoReturn(tag, obj);
        }

        void DoReturn(string tag, GameObject obj)
        {
            obj.SetActive(false);
            if (_pools.TryGetValue(tag, out var q)) q.Enqueue(obj);
        }

        IEnumerator ReturnAfter(string tag, GameObject obj, float d)
        {
            yield return new WaitForSeconds(d);
            DoReturn(tag, obj);
        }

        GameObject Spawn(GameObject prefab)
        {
            var o = Instantiate(prefab, transform);
            o.SetActive(false);
            return o;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GPU INSTANCING MANAGER
    // ═══════════════════════════════════════════════════════════════════════
    public class GPUInstancingManager : MonoBehaviour
    {
        [Serializable]
        public class InstanceGroup
        {
            public string   tag;
            public Mesh     mesh;
            public Material material;
        }

        public List<InstanceGroup> groups = new();

        readonly Dictionary<string, List<Matrix4x4>>      _matrices = new();
        readonly Dictionary<string, MaterialPropertyBlock> _mpbs     = new();

        void Awake()
        {
            foreach (var g in groups)
            {
                _matrices[g.tag] = new List<Matrix4x4>();
                _mpbs[g.tag]     = new MaterialPropertyBlock();
            }
        }

        public void Register(string tag, Matrix4x4 m)
        {
            if (_matrices.TryGetValue(tag, out var list)) list.Add(m);
        }

        void LateUpdate()
        {
            foreach (var g in groups)
            {
                if (!_matrices.TryGetValue(g.tag, out var list) || list.Count == 0) continue;
                int drawn = 0;
                while (drawn < list.Count)
                {
                    int n = Mathf.Min(1023, list.Count - drawn);
                    Graphics.DrawMeshInstanced(g.mesh, 0, g.material,
                        list.GetRange(drawn, n).ToArray(), n, _mpbs[g.tag]);
                    drawn += n;
                }
                list.Clear();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FRUSTUM CULLER
    // ═══════════════════════════════════════════════════════════════════════
    public class FrustumCuller : MonoBehaviour
    {
        public Camera mainCamera;
        public float  checkInterval = 0.10f;

        readonly List<(Renderer r, float radius)> _targets = new();
        Plane[] _planes = new Plane[6];
        float   _timer;

        public void Register(Renderer r, float radius = 2f) =>
            _targets.Add((r, radius));

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < checkInterval || mainCamera == null) return;
            _timer = 0f;

            GeometryUtility.CalculateFrustumPlanes(mainCamera, _planes);
            foreach (var (r, radius) in _targets)
            {
                if (r == null) continue;
                var bounds = new Bounds(r.transform.position, Vector3.one * radius * 2f);
                r.enabled = GeometryUtility.TestPlanesAABB(_planes, bounds);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ADAPTIVE LOD
    // ═══════════════════════════════════════════════════════════════════════
    public class AdaptiveLOD : MonoBehaviour
    {
        [Serializable]
        public class LODLevel { public GameObject mesh; public float maxDistance; }

        public List<LODLevel> levels = new();

        Transform _cam;
        int       _active = 0;

        void Start() => _cam = Camera.main?.transform;

        void Update()
        {
            if (_cam == null || levels.Count == 0) return;

            float dist = Vector3.Distance(transform.position, _cam.position);
            float mult = TierMult();
            int   next = levels.Count - 1;

            for (int i = 0; i < levels.Count; i++)
                if (dist <= levels[i].maxDistance * mult) { next = i; break; }

            if (next == _active) return;
            levels[_active].mesh?.SetActive(false);
            levels[next].mesh?.SetActive(true);
            _active = next;
        }

        float TierMult() => GraphicsSettingsManager.Instance?.CurrentTier switch
        {
            GraphicsTier.Low    => 0.50f,
            GraphicsTier.Medium => 0.75f,
            GraphicsTier.Ultra  => 1.30f,
            _                   => 1.00f
        } ?? 1f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FRAME BUDGET CONTROLLER  (dynamic render scale)
    // ═══════════════════════════════════════════════════════════════════════
    public class FrameBudgetController : MonoBehaviour
    {
        public int   targetFPS       = 60;
        public float criticalFPS     = 44f;
        public float sampleWindow    = 1.5f;
        public float minRenderScale  = 0.65f;
        public float maxRenderScale  = 1.00f;

        UniversalRenderPipelineAsset _urp;
        float _accum; int _frames; float _timer;

        public float AverageFPS { get; private set; } = 60f;

        void Start() =>
            _urp = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;

        void Update()
        {
            _accum  += 1f / Time.unscaledDeltaTime;
            _frames++;
            _timer  += Time.unscaledDeltaTime;
            if (_timer < sampleWindow) return;

            AverageFPS = _accum / _frames;
            _accum = 0f; _frames = 0; _timer = 0f;

            if (_urp == null) return;
            float cur = _urp.renderScale;

            if (AverageFPS < criticalFPS)
                _urp.renderScale = Mathf.Max(minRenderScale, Snap(cur - 0.05f));
            else if (AverageFPS > targetFPS * 0.95f)
                _urp.renderScale = Mathf.Min(maxRenderScale, Snap(cur + 0.025f));
        }

        static float Snap(float v) => Mathf.Round(v * 20f) / 20f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ASYNC TEXTURE STREAMER
    // ═══════════════════════════════════════════════════════════════════════
    public class AsyncTextureStreamer : MonoBehaviour
    {
        public static AsyncTextureStreamer Instance { get; private set; }

        readonly Dictionary<string, Texture2D> _cache    = new();
        readonly Dictionary<string, float>     _lastUsed = new();
        const float EVICT_TIME = 30f;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            var keys = new List<string>(_lastUsed.Keys);
            foreach (var k in keys)
            {
                if (Time.time - _lastUsed[k] <= EVICT_TIME || !_cache.ContainsKey(k)) continue;
                Destroy(_cache[k]);
                _cache.Remove(k);
                _lastUsed.Remove(k);
            }
        }

        public IEnumerator LoadAsync(string path, Action<Texture2D> done)
        {
            if (_cache.TryGetValue(path, out var cached))
            { _lastUsed[path] = Time.time; done?.Invoke(cached); yield break; }

            var req = Resources.LoadAsync<Texture2D>(path);
            yield return req;

            if (req.asset is Texture2D tex)
            {
                _cache[path] = tex; _lastUsed[path] = Time.time;
                done?.Invoke(tex);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MOBILE OPTIMIZER
    // ═══════════════════════════════════════════════════════════════════════
    public class MobileOptimizer : MonoBehaviour
    {
        public int targetFps      = 60;
        public int batteryModeFps = 30;
        bool _throttled;

        void Start()
        {
            Application.targetFrameRate = targetFps;
            QualitySettings.vSyncCount  = 0;
            Screen.sleepTimeout         = SleepTimeout.NeverSleep;
            Application.backgroundLoadingPriority = ThreadPriority.Low;
        }

        void Update()
        {
            bool low = SystemInfo.batteryLevel < 0.15f &&
                       SystemInfo.batteryStatus == BatteryStatus.Discharging;
            if (low == _throttled) return;
            _throttled = low;
            Application.targetFrameRate = low ? batteryModeFps : targetFps;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RENDER STATS MONITOR  (F1 key toggle)
    // ═══════════════════════════════════════════════════════════════════════
    public class RenderStatsMonitor : MonoBehaviour
    {
        bool _show;
        FrameBudgetController _budget;
        readonly GUIStyle _style = new();

        void Start()
        {
            _budget = FindObjectOfType<FrameBudgetController>();
            _style.fontSize = 26;
            _style.normal.textColor = Color.white;
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1)) _show = !_show;
        }

        void OnGUI()
        {
            if (!_show) return;
            float fps  = _budget != null ? _budget.AverageFPS : 1f / Time.deltaTime;
            string tier = GraphicsSettingsManager.Instance?.CurrentTier.ToString() ?? "N/A";
            GUI.Label(new Rect(10, 10, 320, 150),
                $"FPS: {fps:F1}\nTier: {tier}\n" +
                $"RAM: {SystemInfo.systemMemorySize}MB\n" +
                $"GPU: {SystemInfo.graphicsMemorySize}MB\n" +
                $"Res: {Screen.width}x{Screen.height}", _style);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LOOKUP TABLE
    // ═══════════════════════════════════════════════════════════════════════
    public static class LookupTable
    {
        const int RES = 2048;
        static readonly float[] _sin = new float[RES + 1];
        static readonly float[] _cos = new float[RES + 1];

        static LookupTable()
        {
            for (int i = 0; i <= RES; i++)
            {
                float a = i / (float)RES * Mathf.PI * 2f;
                _sin[i] = Mathf.Sin(a);
                _cos[i] = Mathf.Cos(a);
            }
        }

        public static float FastSin(float r) => _sin[Idx(r)];
        public static float FastCos(float r) => _cos[Idx(r)];

        static int Idx(float r)
        {
            float n = r / (Mathf.PI * 2f) % 1f;
            if (n < 0f) n += 1f;
            return Mathf.Clamp(Mathf.RoundToInt(n * RES), 0, RES);
        }
    }
}
