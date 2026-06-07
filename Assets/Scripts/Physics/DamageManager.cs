using System;
using System.Collections.Generic;
using UnityEngine;

namespace RacingGame.Physics
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAR PART – individual component with damage state
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class CarPart
    {
        public enum PartType { FrontWing, RearWing, Engine, Gearbox, Suspension, Tires }

        public PartType type;
        public string partName;
        public float health = 100f;  // 0-100
        public float maxHealth = 100f;
        public GameObject visualMesh;  // Reference to 3D model
        public float performanceMultiplier = 1f;  // 1.0 = intact, 0.3 = damaged

        public bool IsDestroyed => health <= 0;
        public bool IsMinor => health > 50;
        public bool IsMajor => health <= 50 && health > 0;

        public float GetHealthPercent() => (health / maxHealth) * 100f;

        public void TakeDamage(float amount)
        {
            health -= amount;
            health = Mathf.Max(0, health);

            // Update performance as damage increases
            performanceMultiplier = Mathf.Clamp(health / maxHealth, 0.3f, 1.0f);
        }

        public void Repair()
        {
            health = maxHealth;
            performanceMultiplier = 1f;
        }

        public string GetDamageStatus()
        {
            if (IsDestroyed) return "DESTROYED";
            if (IsMajor) return "MAJOR DAMAGE";
            if (!IsMinor) return "MINOR DAMAGE";
            return "INTACT";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DAMAGE MANAGER  – track all car part damage
    // ═══════════════════════════════════════════════════════════════════════
    public class DamageManager : MonoBehaviour
    {
        public static DamageManager Instance { get; private set; }

        [SerializeField] List<CarPart> carParts = new();
        public float totalDamage = 0f;  // 0-1 scale

        // Stress tracking
        public float engineStress = 0f;      // 0-100%
        public float gearboxStress = 0f;     // 0-100%
        public float fuelSystemHealth = 100f;

        // Events
        public event Action<CarPart> OnPartDamaged;
        public event Action<CarPart> OnPartDestroyed;
        public event Action<string> OnMechanicalFailure;  // "Engine blowup!"

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            InitializeCarParts();
        }

        void FixedUpdate()
        {
            UpdateStress();
            CheckFailures();
        }

        // ── INITIALIZATION ────────────────────────────────────────────────
        void InitializeCarParts()
        {
            carParts.Clear();

            carParts.Add(new CarPart 
            { 
                type = CarPart.PartType.FrontWing, 
                partName = "Front Wing",
                maxHealth = 100f
            });

            carParts.Add(new CarPart 
            { 
                type = CarPart.PartType.RearWing, 
                partName = "Rear Wing",
                maxHealth = 100f
            });

            carParts.Add(new CarPart 
            { 
                type = CarPart.PartType.Engine, 
                partName = "Engine",
                maxHealth = 150f  // More robust
            });

            carParts.Add(new CarPart 
            { 
                type = CarPart.PartType.Gearbox, 
                partName = "Gearbox",
                maxHealth = 120f
            });

            carParts.Add(new CarPart 
            { 
                type = CarPart.PartType.Suspension, 
                partName = "Suspension",
                maxHealth = 100f
            });

            Debug.Log($"[DamageManager] Initialized {carParts.Count} car parts");
        }

        // ── COLLISION DAMAGE ─────────────────────────────────────────────
        public void TakeSideCollisionDamage(float impactForce)
        {
            // Side impact damages wings
            float damage = Mathf.Clamp(impactForce * 0.5f, 0, 40);

            GetPart(CarPart.PartType.FrontWing)?.TakeDamage(damage * 0.7f);
            GetPart(CarPart.PartType.RearWing)?.TakeDamage(damage * 0.3f);

            Debug.Log($"[Damage] Side collision: {damage:F1} damage");
        }

        public void TakeFrontCollisionDamage(float impactForce)
        {
            float damage = Mathf.Clamp(impactForce * 0.6f, 0, 50);

            GetPart(CarPart.PartType.FrontWing)?.TakeDamage(damage);
            GetPart(CarPart.PartType.Engine)?.TakeDamage(damage * 0.3f);

            OnPartDamaged?.Invoke(GetPart(CarPart.PartType.FrontWing));

            Debug.Log($"[Damage] Front collision: {damage:F1} damage");
        }

        public void TakeCurbDamage(float curveHeight)
        {
            // Curbs damage suspension and wings
            float damage = curveHeight * 5f;  // Higher curbs = more damage

            GetPart(CarPart.PartType.Suspension)?.TakeDamage(damage * 0.8f);
            GetPart(CarPart.PartType.RearWing)?.TakeDamage(damage * 0.3f);
        }

        // ── STRESS SYSTEM ─────────────────────────────────────────────────
        public void IncreaseEngineStress(float engineRPM, float throttle)
        {
            // High RPM + max throttle = stress
            float rpmStress = (engineRPM / 15000f) * 100f;  // 0-100 based on RPM
            float throttleStress = throttle * 50f;

            engineStress += (rpmStress + throttleStress) * Time.deltaTime * 0.1f;
            engineStress = Mathf.Clamp(engineStress, 0, 100);
        }

        public void IncreaseGearboxStress(float downshiftCount)
        {
            // Multiple downshifts = stress
            gearboxStress += downshiftCount * 5f;
            gearboxStress = Mathf.Clamp(gearboxStress, 0, 100);

            // Gradually cool down
            gearboxStress -= Time.deltaTime * 2f;
        }

        void UpdateStress()
        {
            PhysicsEngine physics = FindObjectOfType<PhysicsEngine>();
            if (physics != null)
            {
                IncreaseEngineStress(physics.GetCurrentRPM(), Input.GetAxis("Throttle"));
            }

            // Cool down engine gradually
            engineStress -= Time.deltaTime * 1f;
            engineStress = Mathf.Max(0, engineStress);
        }

        // ── MECHANICAL FAILURES ───────────────────────────────────────────
        void CheckFailures()
        {
            // Engine failure if stressed too long
            if (engineStress > 90f && Random.value < 0.001f)
            {
                TriggerEngineFailure();
                return;
            }

            // Gearbox failure if abused
            if (gearboxStress > 85f && Random.value < 0.0005f)
            {
                TriggerGearboxFailure();
                return;
            }

            // Random suspension failure if damaged
            var suspension = GetPart(CarPart.PartType.Suspension);
            if (suspension != null && suspension.health < 20 && Random.value < 0.002f)
            {
                TriggerSuspensionFailure();
            }
        }

        void TriggerEngineFailure()
        {
            GetPart(CarPart.PartType.Engine)?.TakeDamage(100);
            OnMechanicalFailure?.Invoke("🔥 Engine failure! Pull over!");
            Debug.Log("[Damage] ENGINE FAILURE");
        }

        void TriggerGearboxFailure()
        {
            GetPart(CarPart.PartType.Gearbox)?.TakeDamage(100);
            OnMechanicalFailure?.Invoke("⚙️ Gearbox failure!");
            Debug.Log("[Damage] GEARBOX FAILURE");
        }

        void TriggerSuspensionFailure()
        {
            GetPart(CarPart.PartType.Suspension)?.TakeDamage(100);
            OnMechanicalFailure?.Invoke("🚗 Suspension failure - car disabled!");
            Debug.Log("[Damage] SUSPENSION FAILURE");
        }

        // ── PERFORMANCE IMPACT ───────────────────────────────────────────
        public float GetDownforceMultiplier()
        {
            var frontWing = GetPart(CarPart.PartType.FrontWing);
            var rearWing = GetPart(CarPart.PartType.RearWing);

            float multiplier = 1f;
            if (frontWing != null) multiplier *= frontWing.performanceMultiplier;
            if (rearWing != null) multiplier *= rearWing.performanceMultiplier;

            return multiplier;
        }

        public float GetEngineMultiplier()
        {
            var engine = GetPart(CarPart.PartType.Engine);
            return engine?.performanceMultiplier ?? 1f;
        }

        public float GetSuspensionMultiplier()
        {
            var suspension = GetPart(CarPart.PartType.Suspension);
            return suspension?.performanceMultiplier ?? 1f;
        }

        // ── PIT STOP REPAIR ───────────────────────────────────────────────
        public float RepairPart(CarPart.PartType type)
        {
            var part = GetPart(type);
            if (part == null) return 0;

            float repairTime = 0;

            switch (type)
            {
                case CarPart.PartType.FrontWing:
                    repairTime = part.GetHealthPercent() < 20 ? 3.5f : 1.5f;
                    break;
                case CarPart.PartType.RearWing:
                    repairTime = 2f;
                    break;
                case CarPart.PartType.Suspension:
                    repairTime = 4f;
                    break;
                case CarPart.PartType.Engine:
                    repairTime = 6f;
                    break;
                default:
                    repairTime = 1f;
                    break;
            }

            part.Repair();
            Debug.Log($"[Pit] Repaired {part.partName} (+{repairTime}s)");

            return repairTime;
        }

        public void FullRepair()
        {
            foreach (var part in carParts)
                part.Repair();

            engineStress = 0;
            gearboxStress = 0;
            fuelSystemHealth = 100f;
        }

        // ── HELPERS ───────────────────────────────────────────────────────
        CarPart GetPart(CarPart.PartType type) => carParts.Find(p => p.type == type);

        public string GetDamageReport()
        {
            string report = "DAMAGE REPORT\n";
            foreach (var part in carParts)
                report += $"{part.partName}: {part.GetDamageStatus()} ({part.GetHealthPercent():F0}%)\n";

            report += $"\nEngine Stress: {engineStress:F0}%\n";
            report += $"Gearbox Stress: {gearboxStress:F0}%";

            return report;
        }

        public bool IsCarDrivable()
        {
            var engine = GetPart(CarPart.PartType.Engine);
            var suspension = GetPart(CarPart.PartType.Suspension);

            return engine?.health > 0 && suspension?.health > 0;
        }
    }
}
