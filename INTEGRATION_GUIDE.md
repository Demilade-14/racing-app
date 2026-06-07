# Integration Guide - Advanced Racing Systems

## ARCHITECTURAL OVERVIEW

```
┌─────────────────────────────────────────────────────────────────┐
│                        GAME BOOTSTRAP                           │
│                  (GameBootstrap.cs or Main)                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│   Physics    │  │    Race      │  │    Career    │
│   Engine     │  │   Director   │  │   Systems    │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       ├─→ Track Surface Manager
       ├─→ Weather Evolution  ◄─────────────┤
       ├─→ Telemetry Recorder
       ├─→ Damage Manager
       ├─→ Audio Engine Manager
       ├─→ Haptic Manager
       │
       ▼
    Car Physics
     (Updated)
```

---

## STEP 1: PHYSICS ENGINE INTEGRATION

### File: Assets/Scripts/Physics/PhysicsEngine.cs

Add these member variables at class level:

```csharp
private TrackSurfaceManager _trackSurface;
private WeatherEvolution _weatherSystem;
private DamageManager _damageManager;
private AudioEngineManager _audioManager;
private HapticManager _hapticManager;
private TelemetryRecorder _telemetryRecorder;
```

Add to Start() method:

```csharp
void Start()
{
    _trackSurface = TrackSurfaceManager.Instance;
    _weatherSystem = FindObjectOfType<WeatherEvolution>();
    _damageManager = DamageManager.Instance;
    _audioManager = AudioEngineManager.Instance;
    _hapticManager = HapticManager.Instance;
    _telemetryRecorder = GetComponent<TelemetryRecorder>();
}
```

### FixedUpdate Integration:

Replace grip calculation with this:

```csharp
// Original grip calculation
float baseGrip = GetBaseGrip();  // Your existing method

// Apply track surface
float trackGrip = _trackSurface?.GetGripCoefficient(
    transform.position, 
    currentTireTemp
) ?? 1f;

// Apply weather
float weatherGrip = _weatherSystem?.GetGripModifier() ?? 1f;

// Apply damage
float damageGrip = _damageManager?.GetSuspensionMultiplier() ?? 1f;

// Final grip
float finalGrip = baseGrip * trackGrip * weatherGrip * damageGrip;
```

### Top Speed Modification:

```csharp
// In your top speed calculation
float baseTopSpeed = 320f;  // km/h

// Apply wind and weather
float weatherModifier = _weatherSystem?.GetTopSpeedModifier() ?? 1f;

// Apply damage
float damageModifier = _damageManager?.GetEngineMultiplier() ?? 1f;

// Apply wind tunnel bonus
float rndModifier = WindTunnelRnD.Instance?.GetAerodynamicsBonus() ?? 1f;

float finalTopSpeed = baseTopSpeed * weatherModifier * damageModifier * rndModifier;
```

### Downforce Calculation:

```csharp
// In downforce calculation
float baseDownforce = 1.5f;

// Apply damage
float damageDownforce = _damageManager?.GetDownforceMultiplier() ?? 1f;

// Apply car setup
float setupDownforce = CarSetupManager.Instance?.GetDownforceFromWings() ?? 1f;

// Apply wind tunnel
float rndDownforce = WindTunnelRnD.Instance?.GetDownforceBonus() ?? 1f;

float finalDownforce = baseDownforce * damageDownforce * setupDownforce * rndDownforce;
```

### Audio/Haptic Updates:

```csharp
void FixedUpdate()
{
    // ... existing physics updates ...

    // Update audio
    _audioManager?.SetEngineRPM(CurrentRPM);
    _audioManager?.SetSpeed(CurrentSpeed);
    _audioManager?.SetTireSlip(CurrentSlipAngle);

    // Update haptic feedback
    if (Mathf.Abs(CurrentSlipAngle) > 15f)
        _hapticManager?.TriggerTireSlipFeedback(CurrentSlipAngle);

    // Update telemetry
    _telemetryRecorder?.RecordFrame(
        position, rotation, CurrentSpeed, throttle, brake, 
        steering, CurrentGear, CurrentRPM
    );
}
```

### Collision Handling:

```csharp
void OnCollisionEnter(Collision collision)
{
    // Existing collision code...
    float impactForce = collision.relativeVelocity.magnitude;

    // Apply damage
    if (collision.gameObject.CompareTag("AI"))
    {
        _damageManager?.TakeSideCollisionDamage(impactForce);
        
        // Get AI driver name
        AIDriver aiDriver = collision.gameObject.GetComponent<AIDriver>();
        RivalrySystem.Instance?.OnCollisionWithDriver(aiDriver.driverName);
        
        // Radio call
        TeamRadioManager.Instance?.BroadcastDamageAlert("Front wing");
    }
    else if (collision.gameObject.CompareTag("Barrier"))
    {
        _damageManager?.TakeFrontCollisionDamage(impactForce);
    }

    // Haptic feedback
    _hapticManager?.TriggerCollisionThud(impactForce);
}
```

---

## STEP 2: RACE DIRECTOR INTEGRATION

### File: Assets/Scripts/Race/RaceDirector.cs

Add at class level:

```csharp
private TelemetryRecorder _telemetryRecorder;
private RecordManager _recordManager;
private RivalrySystem _rivalrySystem;
private TeamRadioManager _teamRadioManager;
```

### OnRaceStart():

```csharp
public void OnRaceStart()
{
    // Initialize telemetry recording
    _telemetryRecorder = FindObjectOfType<TelemetryRecorder>();
    _telemetryRecorder?.StartRecording(trackName);
    
    // Register AI drivers with rivalry system
    _rivalrySystem = RivalrySystem.Instance;
    foreach (var aiDriver in GetComponentsInChildren<AIDriver>())
    {
        _rivalrySystem?.RegisterAIDriver(aiDriver.driverName);
    }

    Debug.Log("[RaceDirector] Race started with telemetry & rivalry tracking");
}
```

### OnLapComplete():

```csharp
public void OnLapComplete(int lapNumber, float lapTime)
{
    if (lapNumber == 1)  // Don't record formation lap
        return;

    // Record telemetry for this lap
    var currentRecording = _telemetryRecorder?.EndLap(lapTime);
    
    if (currentRecording != null)
    {
        // Check personal best
        _recordManager?.SaveLapTime(trackName, currentRecording);
        
        // Trigger radio call if it's a pb
        if (_recordManager?.IsPersonalBest(trackName, lapTime) ?? false)
        {
            TeamRadioManager.Instance?.BroadcastGreatOvertake();
        }
    }
}
```

### OnRaceFinish():

```csharp
public void OnRaceFinish(int finalPosition)
{
    // End telemetry recording
    var finalRecording = _telemetryRecorder?.EndRecording();
    
    if (finalRecording != null)
    {
        // Save final lap data
        _recordManager?.SaveLapTime(trackName, finalRecording);
        
        // Generate telemetry UI data
        Debug.Log($"[Telemetry] Saved {finalRecording.frames.Count} frames");
    }

    // Career integration
    DriverCareerManager.Instance?.ProcessRaceWeekend(
        currentWeekend, 
        new RaceResult { finalPosition = finalPosition }
    );

    Debug.Log("[RaceDirector] Race finished, telemetry saved");
}
```

### Overtake Detection:

```csharp
void CheckOvertakes()
{
    // Compare player and AI positions each frame
    AIDriver[] aiDrivers = FindObjectsOfType<AIDriver>();
    
    foreach (var ai in aiDrivers)
    {
        bool wasAhead = _previousAIPositions[ai].z > playerPos.z;
        bool isNowBehind = ai.transform.position.z < playerPos.z;
        
        if (wasAhead && isNowBehind)
        {
            // Player overtook this AI
            RivalrySystem.Instance?.OnOvertakeDriver(ai.driverName);
            TeamRadioManager.Instance?.BroadcastGreatOvertake();
        }
        
        _previousAIPositions[ai] = ai.transform.position;
    }
}
```

---

## STEP 3: UI WIRING

### Create HUD Panel Hierarchy:

```
HUD (Canvas)
├─ DeltaTimer (Text - TMP)
│  └─ Display: "+0.532" or "-0.110"
├─ TeamRadio (Image + Text)
│  ├─ Panel (background)
│  └─ Message (TMP)
├─ DamageIndicator (Panel)
│  ├─ EngineHealth (Slider)
│  ├─ SuspensionHealth (Slider)
│  └─ TireTemp (Slider)
└─ RivalryBadge (Image + Text)
   └─ Display: rival name + tension level
```

### File: Assets/Scripts/UI/DeltaTimerUI.cs

```csharp
using UnityEngine;
using TMPro;

namespace RacingGame.UI
{
    public class DeltaTimerUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI deltaText;
        [SerializeField] Image deltaBackground;

        private RecordManager _recordManager;
        private float _lastUpdateTime;

        void Start()
        {
            _recordManager = RecordManager.Instance;
        }

        void Update()
        {
            if (Time.time - _lastUpdateTime < 0.1f) return;
            _lastUpdateTime = Time.time;

            UpdateDeltaDisplay();
        }

        void UpdateDeltaDisplay()
        {
            float delta = _recordManager?.GetLiveDelta() ?? 0;

            if (delta == 0)
            {
                deltaText.text = "---";
                deltaBackground.color = Color.gray;
            }
            else if (delta > 0)
            {
                deltaText.text = $"+{delta:F3}";
                deltaBackground.color = Color.red;  // Behind
            }
            else
            {
                deltaText.text = $"{delta:F3}";
                deltaBackground.color = Color.green;  // Ahead
            }
        }
    }
}
```

### File: Assets/Scripts/UI/DamageDisplayUI.cs

```csharp
public class DamageDisplayUI : MonoBehaviour
{
    [SerializeField] Slider engineHealthSlider;
    [SerializeField] Slider suspensionHealthSlider;
    [SerializeField] TextMeshProUGUI damageAlertText;

    void Start()
    {
        DamageManager.Instance.OnPartDamaged += OnPartDamaged;
        DamageManager.Instance.OnMechanicalFailure += OnFailure;
    }

    void OnPartDamaged(CarPart part)
    {
        switch (part.type)
        {
            case CarPart.PartType.Engine:
                engineHealthSlider.value = part.GetHealthPercent() / 100f;
                break;
            case CarPart.PartType.Suspension:
                suspensionHealthSlider.value = part.GetHealthPercent() / 100f;
                break;
        }
    }

    void OnFailure(string failureMessage)
    {
        damageAlertText.text = failureMessage;
        StartCoroutine(FadeDamageAlert());
    }

    IEnumerator FadeDamageAlert()
    {
        yield return new WaitForSeconds(3f);
        damageAlertText.text = "";
    }
}
```

---

## STEP 4: MANAGER INITIALIZATION

### Create Singleton Setup Script:

File: Assets/Scripts/Core/SystemsInitializer.cs

```csharp
using UnityEngine;
using RacingGame.Physics;
using RacingGame.Narrative;
using RacingGame.Immersion;
using RacingGame.Garage;

public class SystemsInitializer : MonoBehaviour
{
    [SerializeField] GameObject trackSurfaceManagerPrefab;
    [SerializeField] GameObject weatherSystemPrefab;
    [SerializeField] GameObject damageManagerPrefab;
    [SerializeField] GameObject telemetryRecorderPrefab;
    [SerializeField] GameObject rivalrySystemPrefab;
    [SerializeField] GameObject teamRadioManagerPrefab;
    [SerializeField] GameObject hapticManagerPrefab;
    [SerializeField] GameObject audioManagerPrefab;
    [SerializeField] GameObject carSetupManagerPrefab;
    [SerializeField] GameObject windTunnelRnDPrefab;

    void Awake()
    {
        // Initialize all systems
        if (TrackSurfaceManager.Instance == null)
            Instantiate(trackSurfaceManagerPrefab);

        if (FindObjectOfType<WeatherEvolution>() == null)
            Instantiate(weatherSystemPrefab);

        if (DamageManager.Instance == null)
            Instantiate(damageManagerPrefab);

        if (FindObjectOfType<TelemetryRecorder>() == null)
            Instantiate(telemetryRecorderPrefab);

        if (RivalrySystem.Instance == null)
            Instantiate(rivalrySystemPrefab);

        if (TeamRadioManager.Instance == null)
            Instantiate(teamRadioManagerPrefab);

        if (HapticManager.Instance == null)
            Instantiate(hapticManagerPrefab);

        if (AudioEngineManager.Instance == null)
            Instantiate(audioManagerPrefab);

        if (CarSetupManager.Instance == null)
            Instantiate(carSetupManagerPrefab);

        if (WindTunnelRnD.Instance == null)
            Instantiate(windTunnelRnDPrefab);

        Debug.Log("[SystemsInitializer] All racing systems initialized");
    }
}
```

### Add to Scene:

1. Create empty GameObject: `Systems`
2. Add component: `SystemsInitializer`
3. Drag all system prefabs into serialized fields

---

## STEP 5: EVENT FLOW

### Race Sequence:

```
OnRaceStart
  ├─ Create weather conditions
  ├─ Initialize track surface
  ├─ Start telemetry recording
  └─ Register AI drivers with rivalry

EveryFrame (FixedUpdate)
  ├─ Update grip based on track/weather/damage
  ├─ Check for marbles/aquaplaning
  ├─ Record telemetry frame
  ├─ Update audio/haptic
  ├─ Check overtakes
  └─ Check collisions → damage/rivalry/radio

OnLapComplete
  ├─ Check personal best
  ├─ Save sector times
  ├─ Broadcast radio callout if PB

OnRaceFinish
  ├─ Save all telemetry data
  ├─ Check career milestones
  ├─ Award career XP
  ├─ Update rivalry tensions
  └─ Display post-race telemetry UI
```

---

## STEP 6: CAREER INTEGRATION

### Update DriverCareerManager.cs:

```csharp
public void ProcessRaceWeekend(RaceWeekend weekend, RaceResult result)
{
    // Existing career code...

    // Update damage (affects R&D strategy)
    if (!DamageManager.Instance.IsCarDrivable())
    {
        result.dnf = true;
        AwardRaceXP(result);
        return;
    }

    // Update telemetry milestone tracking
    var telemetry = RecordManager.Instance?.GetLastLapTelemetry();
    if (telemetry != null && telemetry.maxSpeed > 320)
    {
        // Speed demon milestone
        mediaEventSystem.OnMilestoneAchieved("Speed Demon", 320f);
    }

    AwardRaceXP(result);
    ScheduleNextRace();
}
```

---

## TESTING CHECKLIST

- [ ] Physics responds to grip changes (rubber buildup visible)
- [ ] Weather transitions smooth (puddle visibility)
- [ ] Damage affects performance (slowing with major damage)
- [ ] Telemetry records full lap data
- [ ] Delta timer shows correct deltas
- [ ] Radio callouts trigger appropriately
- [ ] Rivalry tensions escalate with collisions
- [ ] Haptic feedback triggers on collisions/ABS
- [ ] Audio updates with RPM changes
- [ ] Car setup changes affect physics

---

*End of Integration Guide*
