# F1 25 Career System – Technical Integration Guide

## Overview
This guide shows how to integrate the new career system with your existing racing game infrastructure.

---

## 1. PHYSICS ENGINE INTEGRATION

### Apply Driver Skill to Performance

**File:** `Assets/Scripts/Physics/PhysicsEngine.cs`

**Location:** In `CalculateMaxSpeed()` method, after base speed calculation:

```csharp
// Original physics calculation
float maxSpeed = 350; // km/h baseline

// Apply R&D research bonuses
RnDManager rndManager = RnDManager.Instance;
if (rndManager != null)
{
    float powerUnitMultiplier = rndManager.GetCategoryMultiplier("PowerUnit");
    maxSpeed *= powerUnitMultiplier;
}

// Apply driver skill (better drivers extract more from same car)
DriverCareerManager careerManager = DriverCareerManager.Instance;
if (careerManager != null && careerManager.PlayerProfile != null)
{
    int driverOvr = careerManager.PlayerProfile.attributes.OverallRating;
    float skillMultiplier = 1.0f + (driverOvr - 50) * 0.001f;  // 50 OVR = baseline, 99 OVR = +0.49%
    maxSpeed *= skillMultiplier;
}

return maxSpeed;
```

### Apply R&D to Cornering & Braking

**Location:** In `CalculateCorneringGrip()` and `CalculateBrakingDistance()`:

```csharp
// Aerodynamics affects downforce and corner speed
float downforceMultiplier = RnDManager.Instance?.GetCategoryMultiplier("Aerodynamics") ?? 1.0f;
corneringGrip *= downforceMultiplier;

// Chassis affects suspension response
float chassisMultiplier = RnDManager.Instance?.GetCategoryMultiplier("Chassis") ?? 1.0f;
suspensionStiffness *= chassisMultiplier;

// Reliability reduces DNF chance in physics engine
float reliabilityMultiplier = RnDManager.Instance?.GetCategoryMultiplier("Reliability") ?? 1.0f;
mechanicalWearRate *= (1.0f / reliabilityMultiplier);  // Lower wear = more reliable
```

### Apply Driver Attribute to Cornering

**Location:** In cornering calculation:

```csharp
// Driver's cornering attribute affects line precision
int corneringSkill = DriverCareerManager.Instance?.PlayerProfile?.attributes.cornering ?? 50;
float corneringMultiplier = 0.95f + (corneringSkill / 99f) * 0.10f;  // 50=0.95x, 99=1.05x
corneringGrip *= corneringMultiplier;
```

### Apply Driver Attribute to Braking

**Location:** In braking calculation:

```csharp
// Driver's braking attribute affects stopping precision
int brakingSkill = DriverCareerManager.Instance?.PlayerProfile?.attributes.braking ?? 50;
float brakingMultiplier = 0.90f + (brakingSkill / 99f) * 0.15f;  // 50=0.90x, 99=1.05x
brakingForce *= brakingMultiplier;
```

### Apply Consistency to Tire Wear

**Location:** Tire degradation calculation:

```csharp
// Consistency affects tire management (smoother inputs = less wear)
int consistencySkill = DriverCareerManager.Instance?.PlayerProfile?.attributes.consistency ?? 50;
float wearMultiplier = 1.2f - (consistencySkill / 99f) * 0.25f;  // 50=1.075x wear, 99=0.95x wear
tireDegradationRate *= wearMultiplier;
```

---

## 2. RACE RESULT PROCESSING INTEGRATION

### Wire Game Bootstrap to Career Manager

**File:** `Assets/Scripts/Race/GameBootstrap.cs`

**In `OnRaceFinished()` method, add after existing race end logic:**

```csharp
void OnRaceFinished()
{
    // ... existing race end code ...
    
    // Determine player result
    int playerPosition = GetPlayerFinishPosition();
    bool playerFastestLap = DidPlayerGetFastestLap();
    bool playerRetired = DidPlayerRetire();

    // Route to career system if in Driver career mode
    SaveManager saveManager = SaveManager.Instance;
    if (saveManager != null && 
        saveManager.Current.career != null && 
        saveManager.Current.career.careerType == CareerType.Driver)
    {
        // Create race result
        var raceResult = new RaceResult
        {
            finishPosition = playerPosition,
            hasFastestLap = playerFastestLap,
            retired = playerRetired,
            penalty = GetPlayerPenalties(),
            avgLapTime = GetPlayerAverageLapTime()
        };

        // Process through career system
        DriverCareerManager careerMgr = DriverCareerManager.Instance;
        if (careerMgr != null && careerMgr.CurrentSeason != null)
        {
            int currentRoundIndex = careerMgr.CurrentSeason.currentRound - 1;
            if (currentRoundIndex >= 0 && currentRoundIndex < careerMgr.CurrentSeason.calendar.Count)
            {
                var raceWeekend = careerMgr.CurrentSeason.calendar[currentRoundIndex];
                careerMgr.ProcessRaceWeekend(raceWeekend, raceResult);
            }
        }
    }
    
    // Continue with normal race end sequence
    ShowRaceResults(playerPosition);
}
```

### Register Player in Championship

**In `SetupPlayerEntry()` method, add:**

```csharp
void SetupPlayerEntry()
{
    // ... existing code ...

    SaveManager saveManager = SaveManager.Instance;
    if (saveManager != null && 
        saveManager.Current.career != null && 
        saveManager.Current.career.careerType == CareerType.Driver)
    {
        DriverCareerManager careerMgr = DriverCareerManager.Instance;
        if (careerMgr != null)
        {
            ChampionshipManager championship = ChampionshipManager.Instance;
            if (championship != null)
            {
                string driverName = careerMgr.PlayerProfile.driverName;
                string teamName = careerMgr.PlayerProfile.currentTeam;
                
                championship.RegisterPlayer(driverName, teamName, seatNumber: 1);
                
                Debug.Log($"[GameBootstrap] Registered {driverName} ({teamName}) in championship");
            }
        }
    }
}
```

---

## 3. CHAMPIONSHIP MANAGER INTEGRATION

### Update Standing After Race

**File:** `Assets/Scripts/Race/ChampionshipManager.cs`

**In `ProcessRound()` method, ensure it's called from GameBootstrap:**

```csharp
// In GameBootstrap.OnRaceFinished():
Championship.ProcessRound(new List<RaceResult> { raceResult });

// ChampionshipManager will automatically:
// 1. Award points based on position
// 2. Update driver standings
// 3. Track win/podium counts
// 4. Calculate head-to-head records
```

### Get Player Position for Season End

**In CareerIntegrationLayer.EndSeason():**

```csharp
public void EndSeason()
{
    if (Championship == null)
    {
        Debug.LogWarning("[CareerIntegration] Championship not found");
        return;
    }

    // Get final standing
    int finalChampionshipPosition = Championship.GetPlayerPosition(
        DriverCareer.PlayerProfile.driverName);
    
    // Trigger career season end
    DriverCareer.EndSeason(finalChampionshipPosition);
    
    // Reset championship for next season
    Championship.Initialise(
        DriverCareer.CurrentSeason.calendar.Count,
        DriverCareer.PlayerProfile.season + 1);
}
```

---

## 4. UI INTEGRATION

### Career Hub in Main Scene

**File:** Create `CareerHub` prefab

**Hierarchy:**
```
CareerHub (Canvas)
├─ DriverCard (Panel)
│  ├─ driverNameText (TextMeshProUGUI)
│  ├─ overallRatingText
│  └─ careerStatsText
├─ RaceButton (Button)
├─ RnDButton (Button)
├─ MediaButton (Button)
└─ ContractButton (Button)
```

**Script:** `CareerHubUI.cs` (already created)

### R&D Garage as Modal

**File:** Create `RnDGarage` prefab

**Hierarchy:**
```
RnDGarage (Canvas)
├─ Panel (dark background)
│  ├─ Title: "Car Development"
│  ├─ tokenBudgetText: "340/500 tokens"
│  ├─ performanceText: "Overall +11%"
│  └─ ScrollView
│     └─ CategoryList
│        ├─ CategoryRow (Prefab)
│        │  ├─ categoryNameText
│        │  ├─ levelText: "Level 3/5"
│        │  ├─ performanceText: "+4.5%"
│        │  ├─ costText: "300 tokens"
│        │  └─ upgradeButton
│        ├─ CategoryRow (repeated)
│        └─ ...
└─ CloseButton
```

**Script:** `RnDGarageUI.cs` + `RnDCategoryRow.cs` (already created)

### Press Conference Modal

**File:** Create `PressConference` prefab

**Hierarchy:**
```
PressConference (Canvas)
├─ Panel
│  ├─ headline: "Victory Press Conference"
│  ├─ questionText: "How do you feel?"
│  └─ ChoicesPanel (Vertical Layout)
│     ├─ ChoiceButton 1
│     │  ├─ choiceText
│     │  ├─ statsText: "+15 Team, +5 Fans"
│     │  └─ Button
│     ├─ ChoiceButton 2
│     └─ ChoiceButton 3
└─ CloseButton
```

**Script:** `PressConferenceUI.cs` (already created)

### Contract Negotiation Modal

**File:** Create `ContractNegotiation` prefab

**Hierarchy:**
```
ContractNegotiation (Canvas)
├─ Panel
│  ├─ Team Details
│  │  ├─ teamNameText: "Mercedes"
│  │  ├─ salaryText: "$25M/year"
│  │  ├─ bonusesText: "Podium: $2.5M | Win: $10M"
│  │  ├─ durationText: "2 years"
│  │  └─ numberOneText: "✅ #1 Driver Status"
│  ├─ statusText
│  └─ ButtonPanel
│     ├─ acceptButton
│     ├─ counterButton
│     └─ rejectButton
└─ CloseButton
```

**Script:** `ContractNegotiationUI.cs` (already created)

---

## 5. SAVE SYSTEM INTEGRATION

### Extend Save Manager

**File:** `Assets/Scripts/Save/SaveManager.cs`

**Add to constructor or Awake():**

```csharp
void Start()
{
    // ... existing code ...

    // Initialize career system
    CareerIntegrationLayer careerLayer = FindObjectOfType<CareerIntegrationLayer>();
    if (careerLayer != null)
    {
        if (Current.career != null && Current.career.careerType == CareerType.Driver)
        {
            careerLayer.LoadCareerProgress();
        }
    }
}
```

### Auto-save After Race

**File:** `GameBootstrap.cs`, in `OnRaceFinished()`:

```csharp
void OnRaceFinished()
{
    // ... career processing ...
    
    // Auto-save career progress
    if (SaveManager.Instance != null)
    {
        CareerIntegrationLayer integration = CareerIntegrationLayer.Instance;
        if (integration != null)
        {
            integration.SaveCareerProgress();
            SaveManager.Instance.SaveCareer();
        }
    }
}
```

---

## 6. EVENT FLOW DIAGRAM

```
GAME START
    ↓
[Load Save or New Career]
    ↓
[GameBootstrap.SetupPlayerEntry()]
    ├─ Check career type = Driver
    ├─ Load DriverProfile into DriverCareerManager
    └─ Register in Championship
    ↓
[Show Career Hub UI]
    ├─ Display driver stats
    ├─ Show next race button
    └─ Display R&D/Contract options
    ↓
[Player clicks "START RACE"]
    ↓
[GameBootstrap loads race scene]
    ├─ Apply R&D multipliers to physics
    ├─ Apply driver attributes to handling
    └─ Apply skill multiplier to speed
    ↓
[Race runs normally]
    ├─ Physics engine uses modified values
    ├─ Player controls car
    └─ AI teammates simulated
    ↓
[OnRaceFinished()]
    ├─ Determine result (position, FL, DNF)
    ├─ DriverCareerManager.ProcessRaceWeekend()
    │  ├─ Calculate XP (position + quali + FL + overtakes)
    │  ├─ Award XP to attributes
    │  ├─ Update season stats
    │  ├─ Award R&D tokens
    │  └─ Update teammate relationship
    ├─ Championship.ProcessRound()
    │  ├─ Award championship points
    │  └─ Update standings
    ├─ MediaEventSystem.TriggerPostRaceConference()
    │  └─ Show press conference UI
    ├─ Player chooses response
    │  ├─ Affects team/fan/sponsor morale
    │  └─ Updates reputation
    └─ SaveCareerProgress()
    ↓
[Show Results Screen]
    ├─ XP breakdown
    ├─ Championship position update
    ├─ R&D token award display
    └─ [Continue to Next Race]
    ↓
[SEASON END - After race 20-24]
    ├─ Calculate final standing
    ├─ Trigger DriverCareerManager.EndSeason()
    │  ├─ Check if promotion eligible (P1-3 in lower series)
    │  ├─ Reset season stats
    │  └─ Generate new calendar
    ├─ Generate contract offers
    ├─ Show contract negotiation UI
    └─ Player selects new team
    ↓
[Season 2 begins]
```

---

## 7. DEBUGGING CHECKLIST

### XP System Not Working?
```csharp
// In DriverCareerManager.ProcessRaceWeekend(), add debug:
Debug.Log($"[Career] XP Calc: P{result.finishPosition} + Quali + FL + Overtakes = {xpEarned} total");
Debug.Log($"[Career] Award: Consistency {xpByAttr["consistency"]} XP");

// Check attributes are incrementing:
Debug.Log($"[Career] Cornering: {PlayerProfile.attributes.cornering} (XP: {PlayerProfile.attributes.corneringXP})");
```

### R&D Not Applying?
```csharp
// In PhysicsEngine, verify multiplier:
Debug.Log($"[Physics] R&D Aero multiplier: {RnDManager.Instance.GetCategoryMultiplier("Aerodynamics")}");
Debug.Log($"[Physics] Overall multiplier: {RnDManager.Instance.GetOverallPerformanceMultiplier()}");
Debug.Log($"[Physics] Max speed before: {baseMaxSpeed}, after: {baseMaxSpeed * overallMultiplier}");
```

### Championship Not Updating?
```csharp
// After ProcessRound:
Debug.Log($"[Championship] Player position: {Championship.GetPlayerPosition(driverName)}");
Debug.Log($"[Championship] Player points: {Championship.GetPlayerPoints(driverName)}");

// Check standings:
var standings = Championship.GetCurrentStandings();
foreach (var driver in standings)
    Debug.Log($"{driver.position}. {driver.driverName}: {driver.points} pts");
```

### Press Conference Not Showing?
```csharp
// In MediaEventSystem.TriggerPostRaceConference:
Debug.Log($"[Media] Triggering conference for P{finishPosition}");
Debug.Log($"[Media] OnPressConferenceTriggered listeners: {OnPressConferenceTriggered?.GetInvocationList().Length}");

// In PressConferenceUI.Show:
Debug.Log($"[PressUI] Conference shown: {conference.headline}");
Debug.Log($"[PressUI] Choices: {conference.choices.Count}");
```

---

## 8. COMMON ISSUES

| Issue | Cause | Fix |
|-------|-------|-----|
| Attributes not increasing | XP not awarded | Check ProcessRaceWeekend is called |
| R&D not affecting speed | Physics not reading multiplier | Add multiplier to max speed calc |
| Press conference modal stuck | Event not firing | Check MediaEventSystem.Instance |
| Contract offers not appearing | Season end not triggered | Verify round count logic |
| Data lost on restart | Save not called | Add SaveCareerProgress() before scene change |

---

## 9. PERFORMANCE OPTIMIZATION

**Keep these operations lightweight:**

```csharp
// GOOD - O(1) operations
float multiplier = RnDManager.Instance.GetOverallPerformanceMultiplier();  // 4x multiply
int xp = CalculateRaceXP(result);  // Single formula

// AVOID - O(n) operations in physics
// Don't loop through calendar every frame
// Don't search through contract history in OnFixedUpdate()
// Don't regenerate statistics every frame

// Cache values at race start
private float _rndMultiplierCache;
void OnRaceStart() => _rndMultiplierCache = RnDManager.Instance.GetOverallPerformanceMultiplier();
void OnFixedUpdate() => speed *= _rndMultiplierCache;  // O(1)
```

---

## 10. NEXT INTEGRATION STEPS

1. ✅ Add manager components to race scene
2. ✅ Create UI prefabs
3. ✅ Wire GameBootstrap race end
4. ✅ Test: new career → race → XP → press conference
5. ⏳ Build driver creation UI
6. ⏳ Implement sponsor system
7. ⏳ Add advanced practice mechanics

**Total integration time:** ~4-6 hours

---

*For detailed code examples, see manager script inline comments and CAREER_SYSTEM_DOCUMENTATION.md*
