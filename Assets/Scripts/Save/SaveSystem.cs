using System;
using System.IO;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Career;
using RacingGame.Manager;

namespace RacingGame.Save
{
    // ═══════════════════════════════════════════════════════════════════════
    //  SAVE SLOT
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class SaveSlot
    {
        public string     slotId        = Guid.NewGuid().ToString("N");
        public string     slotName      = "New Save";
        public CareerType slotType      = CareerType.Driver;
        public CareerSave career        = new CareerSave();
        public AppSettings settings     = new AppSettings();
        public string     saveVersion   = "1.0.0";
        public string     savedAt;
        public int        totalPlaySeconds;

        public bool IsDriverCareer => slotType == CareerType.Driver;
        public bool IsManagerCareer => slotType == CareerType.Manager;
    }

    [Serializable]
    public class SaveSlotCollection
    {
        public int selectedSlotIndex;
        public SaveSlot[] slots = new SaveSlot[0];
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  APP SETTINGS
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class AppSettings
    {
        // Graphics
        public int    graphicsQuality      = 2;    // 0=low, 1=med, 2=high, 3=ultra
        public bool   motionBlur           = true;
        public bool   antialias            = true;
        public int    targetFrameRate      = 60;
        public bool   showFPS              = false;

        // Audio
        public float  masterVolume         = 1.0f;
        public float  engineVolume         = 0.8f;
        public float  effectsVolume        = 0.7f;
        public float  musicVolume          = 0.4f;

        // Gameplay
        public float  steeringSensitivity  = 0.6f;
        public bool   tractionControl      = true;
        public bool   abs                  = true;
        public bool   steeringAssist       = true;
        public bool   pitAssist            = false;
        public int    defaultDifficulty    = 1;     // 0=easy,1=med,2=hard,3=expert
        public int    cameraMode           = 1;     // CameraMode enum index
        public bool   vibration            = true;

        // HUD
        public bool   showMinimap          = true;
        public bool   showTelemetry        = false;
        public bool   showGForce           = false;
        public bool   showInputTrace       = false;
        public int    hudScale             = 1;    // 0=small,1=medium,2=large
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SAVE MANAGER
    // ═══════════════════════════════════════════════════════════════════════
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public SaveSlot Current { get; private set; } = new();
        public List<SaveSlot> Slots { get; private set; } = new();
        public int SelectedSlotIndex { get; private set; }

        // Paths
        static string SaveDir      => Path.Combine(Application.persistentDataPath, "saves");
        static string SlotsPath    => Path.Combine(SaveDir, "slots.json");
        static string CareerPath   => Path.Combine(SaveDir, "career.json");
        static string SettingsPath => Path.Combine(SaveDir, "settings.json");

        // Auto-save
        float _autoSaveTimer;
        const float AUTO_SAVE_INTERVAL = 60f;

        // Session timer
        DateTime _sessionStart;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance       = this;
            DontDestroyOnLoad(gameObject);
            _sessionStart  = DateTime.UtcNow;

            if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);

            LoadSettings();
            LoadSlots();
            LoadCareer();
        }

        void Update()
        {
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
            {
                _autoSaveTimer = 0f;
                SaveAll();
            }
        }

        void OnApplicationPause(bool paused) { if (paused) SaveAll(); }
        void OnApplicationFocus(bool focus)  { if (!focus) SaveAll(); }

        // ── Save ──────────────────────────────────────────────────────────
        public void SaveAll()
        {
            SaveCareer();
            SaveSlots();
            SaveSettings();
        }

        public void SaveCareer()
        {
            if (Current.career == null) return;
            Current.savedAt = DateTime.UtcNow.ToString("O");
            Current.totalPlaySeconds += (int)(DateTime.UtcNow - _sessionStart).TotalSeconds;
            _sessionStart = DateTime.UtcNow;

            WriteJson(CareerPath, Current.career);
            SaveSlots();
            Debug.Log($"[Save] Career saved → {CareerPath}");
        }

        public void SaveSlots()
        {
            var collection = new SaveSlotCollection
            {
                selectedSlotIndex = SelectedSlotIndex,
                slots = Slots.ToArray()
            };
            WriteJson(SlotsPath, collection);
            Debug.Log($"[Save] Slots saved → {SlotsPath}");
        }

        public void SaveSettings()
        {
            WriteJson(SettingsPath, Current.settings);
        }

        // ── Load ──────────────────────────────────────────────────────────
        public void LoadCareer()
        {
            if (Slots.Count > 0 && SelectedSlotIndex >= 0 && SelectedSlotIndex < Slots.Count)
            {
                Current = Slots[SelectedSlotIndex];
            }
            else if (Slots.Count > 0)
            {
                SelectedSlotIndex = 0;
                Current = Slots[0];
            }
            else
            {
                Current = new SaveSlot { slotName = "Default", career = ReadJson<CareerSave>(CareerPath) ?? new CareerSave() };
                Slots.Add(Current);
                SelectedSlotIndex = 0;
            }
        }

        public void LoadSettings()
        {
            Current.settings = ReadJson<AppSettings>(SettingsPath) ?? new AppSettings();
            ApplySettings(Current.settings);
        }

        // ── Reset ─────────────────────────────────────────────────────────
        public void NewGame(string driverName)
        {
            Current.career            = new CareerSave();
            Current.career.driver.driverName = driverName;
            Current.career.reputation = 10f;
            Current.slotName = driverName;
            Current.slotType = CareerType.Driver;
            Current.career.careerType = CareerType.Driver;
            if (!Slots.Contains(Current)) Slots.Add(Current);
            SelectedSlotIndex = Slots.IndexOf(Current);
            SaveCareer();
            SaveSlots();
        }

        public void NewManagerGame(string teamName)
        {
            Current.career              = new CareerSave();
            Current.career.careerType   = CareerType.Manager;
            Current.slotName           = teamName;
            Current.slotType           = CareerType.Manager;
            Current.career.managerCareer = new MyTeamSave { teamName = teamName };
            if (!Slots.Contains(Current)) Slots.Add(Current);
            SelectedSlotIndex = Slots.IndexOf(Current);
            SaveCareer();
            SaveSlots();
        }

        public void ResetSettings()
        {
            Current.settings = new AppSettings();
            SaveSettings();
            ApplySettings(Current.settings);
        }

        public void LoadSlots()
        {
            var loadedCollection = ReadJson<SaveSlotCollection>(SlotsPath);
            if (loadedCollection != null && loadedCollection.slots.Length > 0)
            {
                Slots = new List<SaveSlot>(loadedCollection.slots);
                SelectedSlotIndex = Mathf.Clamp(loadedCollection.selectedSlotIndex, 0, Slots.Count - 1);
            }
            else
            {
                Slots = new List<SaveSlot> { Current };
                SelectedSlotIndex = 0;
                SaveSlots();
            }
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= Slots.Count) return;
            SelectedSlotIndex = index;
            Current = Slots[index];
            SaveSlots();
            LoadCareer();
        }

        public void CreateSlot(string slotName, CareerType slotType)
        {
            var slot = new SaveSlot
            {
                slotName = slotName,
                slotType = slotType,
                career = new CareerSave
                {
                    careerType = slotType,
                    managerCareer = slotType == CareerType.Manager ? new MyTeamSave { teamName = slotName } : new MyTeamSave()
                }
            };
            Slots.Add(slot);
            SelectedSlotIndex = Slots.Count - 1;
            Current = slot;
            SaveSlots();
            SaveCareer();
        }

        // ── Apply settings to Unity ───────────────────────────────────────
        public void ApplySettings(AppSettings s)
        {
            Application.targetFrameRate  = s.targetFrameRate;
            QualitySettings.masterTextureLimit
                = s.graphicsQuality < 2 ? 2 - s.graphicsQuality : 0;
            AudioListener.volume = s.masterVolume;
        }

        // ── JSON helpers ──────────────────────────────────────────────────
        static void WriteJson<T>(string path, T obj)
        {
            try   { File.WriteAllText(path, JsonUtility.ToJson(obj, true)); }
            catch (Exception e) { Debug.LogError($"[Save] Write failed: {e.Message}"); }
        }

        static T ReadJson<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Read failed: {e.Message}");
                return null;
            }
        }

        // ── Cloud save stub (AWS S3 / Cognito) ───────────────────────────
        public void UploadCloudSave()
        {
            // TODO: implement AWS S3 upload via UnityWebRequest
            // POST https://<api-gateway>.amazonaws.com/save
            // Body: JsonUtility.ToJson(Current.career)
            // Headers: Authorization: Bearer <cognito_token>
            Debug.Log("[CloudSave] Upload queued (stub)");
        }

        public void DownloadCloudSave()
        {
            // TODO: implement AWS S3 download
            Debug.Log("[CloudSave] Download queued (stub)");
        }
    }
}
