using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RacingGame.Garage
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAR SETUP PRESET – saves/loads car configuration
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class CarSetup
    {
        public string setupName = "Default";
        public float frontWingAngle = 5f;        // 0-10 degrees
        public float rearWingAngle = 8f;         // 0-15 degrees
        public float differentialOnThrottle = 60f;  // 0-100% lockup
        public float differentialOffThrottle = 40f;
        public float suspensionStiffness = 50f;  // 0-100 soft to stiff
        public float brakeBiasFront = 55f;       // 0-100% front bias
        public float brakeBiasRear = 45f;
        public int drsFlapMode = 0;              // 0=off, 1=auto, 2=manual

        public void ApplyToPhysics(PhysicsEngine physics)
        {
            if (physics == null) return;

            // Apply wing angles to downforce calculation
            physics.SetAerodynamicsAngle(frontWingAngle, rearWingAngle);

            // Apply differential settings
            physics.SetDifferential(differentialOnThrottle, differentialOffThrottle);

            // Apply suspension stiffness
            physics.SetSuspensionStiffness(suspensionStiffness);

            // Apply brake bias
            physics.SetBrakeBias(brakeBiasFront);

            Debug.Log($"[Setup] Applied config: {setupName}");
        }

        public string GetDescription()
        {
            return $"Wing: {frontWingAngle}°/{rearWingAngle}° | " +
                   $"Diff: {differentialOnThrottle:F0}%/{differentialOffThrottle:F0}% | " +
                   $"Suspension: {suspensionStiffness:F0}% | " +
                   $"Brakes: {brakeBiasFront:F0}%";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CAR SETUP MANAGER  – garage setup adjustments
    // ═══════════════════════════════════════════════════════════════════════
    public class CarSetupManager : MonoBehaviour
    {
        public static CarSetupManager Instance { get; private set; }

        public CarSetup currentSetup = new();
        public List<CarSetup> savedSetups = new();
        public PhysicsEngine physicsEngine;

        public event Action<CarSetup> OnSetupChanged;
        public event Action<string> OnSetupLoaded;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            physicsEngine = FindObjectOfType<PhysicsEngine>();

            // Load default setups
            CreateDefaultSetups();
        }

        void CreateDefaultSetups()
        {
            savedSetups.Clear();

            savedSetups.Add(new CarSetup 
            { 
                setupName = "Balanced",
                frontWingAngle = 5f,
                rearWingAngle = 8f
            });

            savedSetups.Add(new CarSetup 
            { 
                setupName = "Low Drag",
                frontWingAngle = 2f,
                rearWingAngle = 3f,
                suspensionStiffness = 30f
            });

            savedSetups.Add(new CarSetup 
            { 
                setupName = "High Downforce",
                frontWingAngle = 9f,
                rearWingAngle = 14f,
                suspensionStiffness = 80f
            });

            Debug.Log($"[CarSetup] Loaded {savedSetups.Count} default setups");
        }

        // ── FRONT WING ─────────────────────────────────────────────────────
        public void SetFrontWingAngle(float angle)
        {
            currentSetup.frontWingAngle = Mathf.Clamp(angle, 0, 10);

            // Effect: Higher angle = more downforce but more drag
            // Front grip increases, top speed decreases
            UpdateSetup();
        }

        // ── REAR WING ──────────────────────────────────────────────────────
        public void SetRearWingAngle(float angle)
        {
            currentSetup.rearWingAngle = Mathf.Clamp(angle, 0, 15);

            // Effect: Higher angle = more rear downforce, better stability
            UpdateSetup();
        }

        // ── DIFFERENTIAL ───────────────────────────────────────────────────
        public void SetDifferentialOnThrottle(float percent)
        {
            currentSetup.differentialOnThrottle = Mathf.Clamp(percent, 0, 100);

            // Effect: Higher = more locked, faster out of corners, harder to control
            UpdateSetup();
        }

        public void SetDifferentialOffThrottle(float percent)
        {
            currentSetup.differentialOffThrottle = Mathf.Clamp(percent, 0, 100);

            // Effect: Affects turn-in stability
            UpdateSetup();
        }

        // ── SUSPENSION ─────────────────────────────────────────────────────
        public void SetSuspensionStiffness(float stiffness)
        {
            currentSetup.suspensionStiffness = Mathf.Clamp(stiffness, 0, 100);

            // Effect: Stiffer = faster response but less grip over bumps
            UpdateSetup();
        }

        // ── BRAKE BIAS ─────────────────────────────────────────────────────
        public void SetBrakeBias(float frontPercent)
        {
            float rearPercent = 100 - frontPercent;

            currentSetup.brakeBiasFront = Mathf.Clamp(frontPercent, 40, 60);  // Realistic range
            currentSetup.brakeBiasRear = rearPercent;

            // Effect: More front bias = better stopping but risk of locking
            UpdateSetup();
        }

        void UpdateSetup()
        {
            currentSetup.ApplyToPhysics(physicsEngine);
            OnSetupChanged?.Invoke(currentSetup);
        }

        public void SaveCurrentSetup(string name)
        {
            var setup = new CarSetup
            {
                setupName = name,
                frontWingAngle = currentSetup.frontWingAngle,
                rearWingAngle = currentSetup.rearWingAngle,
                differentialOnThrottle = currentSetup.differentialOnThrottle,
                differentialOffThrottle = currentSetup.differentialOffThrottle,
                suspensionStiffness = currentSetup.suspensionStiffness,
                brakeBiasFront = currentSetup.brakeBiasFront
            };

            savedSetups.Add(setup);
            Debug.Log($"[CarSetup] Saved setup: {name}");
        }

        public void LoadSetup(string name)
        {
            var setup = savedSetups.Find(s => s.setupName == name);
            if (setup != null)
            {
                currentSetup = setup;
                UpdateSetup();
                OnSetupLoaded?.Invoke(name);
                Debug.Log($"[CarSetup] Loaded setup: {name}");
            }
        }

        public string GetSetupDescription() => currentSetup.GetDescription();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  WIND TUNNEL R&D SYSTEM  – seasonal car development
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class R&DUpgrade
    {
        public enum UpgradeType { FrontWingDrag, RearWingDownforce, Suspension, Stability }

        public UpgradeType type;
        public string description;
        public float progressPercent = 0f;  // 0-100
        public float maxPerformanceGain = 0.15f;  // 0-15% improvement
        public float currentGain = 0f;

        public bool IsComplete => progressPercent >= 100f;
    }

    public class WindTunnelRnD : MonoBehaviour
    {
        public static WindTunnelRnD Instance { get; private set; }

        public List<R&DUpgrade> activeUpgrades = new();
        public float windTunnelHoursPerSeason = 100f;
        public float windTunnelHoursUsed = 0f;
        public int currentSeason = 1;

        public event Action<R&DUpgrade> OnUpgradeProgressed;
        public event Action<R&DUpgrade> OnUpgradeCompleted;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            InitializeSeasonalUpgrades();
        }

        void FixedUpdate()
        {
            ProgressUpgrades();
        }

        void InitializeSeasonalUpgrades()
        {
            activeUpgrades.Clear();

            activeUpgrades.Add(new R&DUpgrade
            {
                type = R&DUpgrade.UpgradeType.FrontWingDrag,
                description = "Front Wing Drag Reduction",
                maxPerformanceGain = 0.08f  // 8% drag reduction = ~2 km/h top speed
            });

            activeUpgrades.Add(new R&DUpgrade
            {
                type = R&DUpgrade.UpgradeType.RearWingDownforce,
                description = "Rear Wing Downforce",
                maxPerformanceGain = 0.12f  // 12% downforce = better cornering
            });

            activeUpgrades.Add(new R&DUpgrade
            {
                type = R&DUpgrade.UpgradeType.Suspension,
                description = "Suspension Optimization",
                maxPerformanceGain = 0.10f
            });

            Debug.Log($"[WindTunnel] Season {currentSeason} upgrades available");
        }

        void ProgressUpgrades()
        {
            if (windTunnelHoursUsed >= windTunnelHoursPerSeason)
                return;

            // Each frame = small progress
            float progressRate = 0.5f;  // 0.5% per second
            windTunnelHoursUsed += Time.deltaTime / 3600f;

            foreach (var upgrade in activeUpgrades)
            {
                if (!upgrade.IsComplete)
                {
                    upgrade.progressPercent += progressRate * Time.deltaTime;

                    if (upgrade.progressPercent >= 100f)
                    {
                        upgrade.progressPercent = 100f;
                        upgrade.currentGain = upgrade.maxPerformanceGain;
                        OnUpgradeCompleted?.Invoke(upgrade);

                        Debug.Log($"[WindTunnel] Upgrade complete: {upgrade.description}");
                    }
                    else
                    {
                        OnUpgradeProgressed?.Invoke(upgrade);
                    }
                }
            }
        }

        public void FocusUpgradeResearch(R&DUpgrade.UpgradeType type)
        {
            // Increase progress rate for focused upgrade
            var upgrade = activeUpgrades.Find(u => u.type == type);
            if (upgrade != null)
            {
                upgrade.progressPercent += 2f;  // Boost progress
                Debug.Log($"[WindTunnel] Focused research on {upgrade.description}");
            }
        }

        public float GetAerodynamicsBonus()
        {
            float bonus = 1f;

            var dragUpgrade = activeUpgrades.Find(u => u.type == R&DUpgrade.UpgradeType.FrontWingDrag);
            if (dragUpgrade != null)
                bonus *= (1f - dragUpgrade.currentGain);  // Drag reduction

            return bonus;
        }

        public float GetDownforceBonus()
        {
            var downforceUpgrade = activeUpgrades.Find(u => u.type == R&DUpgrade.UpgradeType.RearWingDownforce);
            return downforceUpgrade != null ? (1f + downforceUpgrade.currentGain) : 1f;
        }

        public void ResetSeason()
        {
            windTunnelHoursUsed = 0f;
            currentSeason++;

            foreach (var upgrade in activeUpgrades)
            {
                upgrade.progressPercent = 0f;
                upgrade.currentGain = 0f;
            }

            InitializeSeasonalUpgrades();
        }

        public string GetWindTunnelStatus()
        {
            float hoursRemaining = windTunnelHoursPerSeason - windTunnelHoursUsed;
            return $"Season {currentSeason}: {hoursRemaining:F1}h remaining";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CAR SETUP UI  – garage interface
    // ═══════════════════════════════════════════════════════════════════════
    public class CarSetupUI : MonoBehaviour
    {
        [SerializeField] Slider frontWingSlider;
        [SerializeField] Slider rearWingSlider;
        [SerializeField] Slider diffOnSlider;
        [SerializeField] Slider brakeBiasSlider;
        [SerializeField] TextMeshProUGUI setupDescriptionText;
        [SerializeField] Button saveSetupButton;

        void Start()
        {
            frontWingSlider.onValueChanged.AddListener(CarSetupManager.Instance.SetFrontWingAngle);
            rearWingSlider.onValueChanged.AddListener(CarSetupManager.Instance.SetRearWingAngle);
            diffOnSlider.onValueChanged.AddListener(CarSetupManager.Instance.SetDifferentialOnThrottle);
            brakeBiasSlider.onValueChanged.AddListener(CarSetupManager.Instance.SetBrakeBias);

            CarSetupManager.Instance.OnSetupChanged += UpdateDisplay;
            saveSetupButton.onClick.AddListener(() => OnSaveSetup());

            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            frontWingSlider.value = CarSetupManager.Instance.currentSetup.frontWingAngle;
            rearWingSlider.value = CarSetupManager.Instance.currentSetup.rearWingAngle;
            diffOnSlider.value = CarSetupManager.Instance.currentSetup.differentialOnThrottle;
            brakeBiasSlider.value = CarSetupManager.Instance.currentSetup.brakeBiasFront;

            UpdateDisplay(CarSetupManager.Instance.currentSetup);
        }

        void UpdateDisplay(CarSetup setup)
        {
            setupDescriptionText.text = 
                $"🏎️ {setup.setupName}\n" +
                $"Frontend: {setup.frontWingAngle:F1}°/{setup.rearWingAngle:F1}°\n" +
                $"Diff: {setup.differentialOnThrottle:F0}%\n" +
                $"Brakes: {setup.brakeBiasFront:F0}% front";
        }

        void OnSaveSetup()
        {
            // Show dialog to save current setup with custom name
            // For now, just save as timestamped setup
            string setupName = $"Setup {System.DateTime.Now:HHmm}";
            CarSetupManager.Instance.SaveCurrentSetup(setupName);
        }

        void OnDestroy()
        {
            if (CarSetupManager.Instance)
                CarSetupManager.Instance.OnSetupChanged -= UpdateDisplay;
        }
    }
}
