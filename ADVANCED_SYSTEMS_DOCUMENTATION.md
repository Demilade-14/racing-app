# Advanced Racing Systems Documentation

## SYSTEM OVERVIEW

Six major advanced systems have been implemented to bring F1-level depth to the mobile racing game:

1. **Track Evolution & Dynamic Weather** – Rubber, marbles, puddles, wind
2. **Telemetry & Ghost System** – Lap recording, delta timer, personal records
3. **Rivalry & Team Radio** – AI aggression, contextual messages, drama events
4. **Visual & Mechanical Damage** – Part tracking, failures, pit repairs
5. **Haptic & Audio Immersion** – Device vibration, 3D audio, Doppler effect
6. **Car Setup & Wind Tunnel R&D** – Garage tuning, seasonal upgrades

---

## 1. TRACK EVOLUTION & DYNAMIC WEATHER

### TrackSurfaceManager.cs

Simulates realistic track surface evolution throughout a race session.

#### Key Features:
- **Rubber In**: Racing line gains grip as laps progress (0-100 rubber level)
- **Marble Buildup**: Outside of track accumulates loose rubber
- **Grip Coefficients**: Dynamic based on rubber, marbles, and temperature
- **Session Reset**: Track state resets between practice/qualifying/race

#### Usage:
```csharp
// In PhysicsEngine:
float baseGrip = 1.2f;
float trackGrip = TrackSurfaceManager.Instance.GetGripCoefficient(playerPos, tireTemp);
float finalGrip = baseGrip * trackGrip;
```

#### Configuration:
```csharp
trackSections[0].rubberLevel = 0-100    // 0=fresh, 100=max grip
trackSections[0].marbleLevel = 0-100    // 0=clean, 100=full marbles

// Marbles cause:
- 0-40% grip loss when driven over
- Tire temperature increase
- Potential spin-out if speed > threshold
```

### WeatherEvolution.cs

Dynamic weather system with puddles, wind, and drying line mechanics.

#### Weather States:
```
DRY (sunny)
  ├─ No puddles
  ├─ No aquaplaning risk
  └─ Wind effects minor

INTERMEDIATE (light rain)
  ├─ Some puddles active
  ├─ Drying line visible (best grip)
  ├─ +15% grip on drying line
  └─ Aquaplane risk at high speed

WET (heavy rain)
  ├─ 5 puddles generated
  ├─ Aquaplaning risk high
  ├─ Wind effects significant
  └─ Tire temps lower, slip higher
```

#### Puddle System:
```csharp
if (CheckAquaplane(playerPos, speed))
{
    // Aquaplane triggered!
    // Temporary loss of steering (1-2 seconds)
    // Speed-dependent: faster = worse
    
    float speedThreshold = 150f - (puddle.waterLevel * 0.5f);
    if (speed > speedThreshold)
        steeringInput *= 0.3f;  // 70% loss of control
}
```

#### Wind Effects:
```
Wind Strength: 0-40 km/h
Wind Direction: Random bearing

Top Speed Modifier = 1.0 - (headwind_component * 0.05)
  // Headwind: -5% top speed
  // Tailwind: +5% top speed

Stability Modifier = 1.0 - (crosswind * 0.001 * strength)
  // Crosswind: up to -5% stability
```

---

## 2. TELEMETRY & GHOST SYSTEM

### TelemetrySystem.cs

Records and playbacks lap telemetry data for ghost racing.

#### LapRecording Data:
```csharp
TelemetryFrame
├─ lapDistance (0-1)
├─ speed
├─ throttle (0-1)
├─ brake (0-1)
├─ steering (-1 to 1)
├─ position
├─ rotation
├─ timestamp
├─ gear
└─ engineRPM

Recorded at 60 Hz (every frame)
~60 frames per minute lap = 3,600 data points per lap
```

#### Ghost Car Rendering:
```csharp
GhostSystem
├─ Load best lap: SetGhostLap(recording)
├─ Render 40% opacity ghost car
├─ Play ghost 1-lap behind player
├─ Update position every frame from telemetry
└─ Compare position for delta calculation
```

### DeltaTimer & RecordManager

#### Live Delta Timer:
```
Format: [+0.432] or [-0.110]
  Red (+) = behind personal best
  Green (-) = ahead of personal best

Updated every 100ms
Compares lap distance positions between laps
```

#### Track Records Database:
```csharp
TrackRecord
├─ trackName
├─ lapTime (float seconds)
├─ driverName
├─ recordDate
├─ maxSpeed
└─ sectorTimes[] (F1: 3 sectors)

Milestones tracked:
├─ Fastest lap all-time
├─ Highest speed achieved
├─ Most wins in season
├─ Consistency (low variance)
└─ Racecraft (overtakes/race)
```

#### Post-Race Telemetry UI:
```
Display three graphs:
1. Speed vs Lap Distance
   └─ Line graph, colored by speed (red=slow, green=fast)

2. Throttle Application
   └─ Yellow bar graph, height = throttle %

3. Brake Application
   └─ Red bar graph, height = brake %

All normalized 0-1 scale, full lap distance
```

---

## 3. RIVALRY & TEAM RADIO

### RivalrySystem.cs

Tracks player interactions with specific AI drivers.

#### Rivalry Metrics:
```csharp
DriverRivalry
├─ rivalryTension (0-100)
├─ collisionsWithDriver (count)
├─ overtakesAgainstDriver (count)
├─ beatenByDriver (count)
└─ status: None | Rival | Bitter | Dangerous

Tension Escalation:
  ├─ Collision: +15
  ├─ Overtake: +5
  ├─ Lost race: -3
  ├─ At 60+: "Bitter" rival status
  └─ At 80+: "Dangerous" mode activated
```

#### Aggression Multiplier:
```csharp
AI_Aggression = 1.0 + (rivalryTension / 100) * 0.5
  // 0 tension = 1.0x normal
  // 100 tension = 1.5x aggression

Results:
  ├─ Closer following distance
  ├─ More aggressive overtake attempts
  ├─ Less yielding in tight corners
  └─ Higher collision risk
```

### TeamRadioManager.cs

Context-aware radio messages and driver communication.

#### Message Types:
```
Engineer Messages (from team):
  ├─ "Box, box, next lap" (pit call)
  ├─ "Great pace! Keep pushing" (encouragement)
  ├─ "Car damage, check temperatures" (damage alert)
  ├─ "Tire temps are high" (tire warning)
  ├─ "Position P2, gap 1.5 seconds" (position update)
  └─ "That overtake was beautiful!" (praise)

Driver Acknowledgments:
  ├─ "Copy that"
  ├─ "Understood"
  └─ "Push push!"
```

#### Context-Triggered Events:
```csharp
BroadcastEngineFailure()     // "Engine failure! Pull over!"
BroadcastPitWindow()         // "Pit window is open"
BroadcastGreatOvertake()     // "Excellent overtake!"
BroadcastGapUpdate()         // Position and gap info
BroadcastRivalry()           // "Careful with [Driver], pushing hard"
BroadcastWeatherAlert()      // Weather warnings
BroadcastDamageAlert()       // Specific part damage
```

### Team Principal Meeting

Drama event triggered when underperforming:

```
Condition: Player championship position < 50% of leader
  
Dialog Options:
1. Defend: "I'll prove myself next race"
   └─ Grant 3-race reprieve
2. Accept Demotion: Contract terminated
   └─ Free agent status, seek new team
3. Negotiate: "3 races to deliver or resign"
   └─ High-pressure comeback mode

Affects career progression and contract negotiations
```

---

## 4. DAMAGE SYSTEM

### DamageManager.cs

Tracks individual car part damage and performance impact.

#### Car Parts:
```csharp
CarPart (health 0-100%)
├─ FrontWing (100 max health)
├─ RearWing (100 max health)
├─ Engine (150 max health)
├─ Gearbox (120 max health)
├─ Suspension (100 max health)
└─ Tires (tracked separately)

Damage Status:
  ├─ INTACT (100%)
  ├─ MINOR DAMAGE (50-99%)
  ├─ MAJOR DAMAGE (1-49%)
  └─ DESTROYED (0%)
```

#### Damage Sources:
```
Collision Damage:
  ├─ Front collision: 50% → FrontWing, 15% → Engine
  ├─ Side collision: 35% → Wings, 5% → Suspension
  └─ Rear collision: 40% → RearWing, 10% → Gearbox

Curb Damage:
  ├─ Height-based: damage = height * 5
  └─ Primarily Suspension & RearWing

Marbles:
  └─ Causes tire stress, not mechanical damage
```

#### Stress System:
```csharp
Engine Stress (0-100)
  ├─ Increases with high RPM + max throttle
  ├─ At >90: Random engine failure chance (0.1%)
  └─ Cools gradually (-1% per second)

Gearbox Stress (0-100)
  ├─ Increases per downshift (count * 5)
  ├─ At >85: Random gearbox failure (0.05%)
  └─ Cools at -2% per second
```

#### Performance Impact:
```csharp
Downforce Multiplier = FrontWing.health * RearWing.health
  // Both at 50% = 0.25x downforce (massive loss)

Engine Multiplier = 0.3-1.0 based on health
  // 0% health = 0.3x power (limp home)

Suspension Multiplier = 0.3-1.0
  // Affects grip and handling

Failure = DNF (Did Not Finish)
  // Engine destroyed or Suspension destroyed
```

### Pit Stop Repairs:
```csharp
RepairPart(type) returns time cost:

  FrontWing:
    ├─ <20% health: +3.5s (full replacement)
    └─ >20% health: +1.5s (patch)

  RearWing: +2.0s
  Suspension: +4.0s
  Engine: +6.0s (full rebuild)

Tire change: +2.0-2.5s per compound change
```

---

## 5. HAPTIC & AUDIO IMMERSION

### HapticManager.cs

Device vibration feedback for enhanced immersion.

#### Haptic Triggers:
```
ABS Pulsing (braking hard):
  └─ 4 pulses of 50ms @ 50% intensity

Curb Impact:
  └─ Sharp jolt, 150ms @ 100% intensity

Collision:
  └─ Duration-based: 50-300ms
  └─ Intensity scales with impact force

Traction Loss:
  └─ Rumble 300ms @ 60% intensity

Engine Failure:
  └─ 3 heavy pulses, 200ms each @ 100%

Wind Gust:
  └─ 100ms @ (gustStrength / 40) * 100%
```

### AudioEngineManager.cs

3D positional audio with RPM-based engine sound.

#### Engine Audio:
```
RPM to Pitch Mapping:
  ├─ 0 RPM = 0.5 pitch
  ├─ 7,500 RPM = 1.25 pitch
  └─ 15,000 RPM = 2.0 pitch

Volume = 0.3 + (throttle * 0.7)
  // Idle = 0.3, WOT = 1.0
```

#### Tire Noise:
```
Volume based on slip angle:
  ├─ 0° slip = 0% tire noise
  ├─ 15° slip = 40% noise
  └─ 30° slip = 80% noise

Pitch: 1.0 + (slip / 30) * 0.3
  // Higher slip = higher pitched screech

Added warble effect when slipping heavily
```

#### Wind Noise:
```
Volume = (speed / 300 km/h) * 0.5
  // 300 km/h = 50% volume max

Pitch = 0.9 + (speed / 300) * 0.2
  // Higher speed = higher pitch

Continuous at speed > 150 km/h
```

#### AI Car Audio:
```
Pan Stereo: AI horizontal distance / 50
  // ±1.0 for full left/right

Volume: 1.0 / (distance / 10 + 1)
  // Closer = louder

Doppler Effect:
  ├─ Approaching = +10% pitch
  └─ Receding = -10% pitch
```

---

## 6. CAR SETUP & WIND TUNNEL R&D

### CarSetupManager.cs

Garage adjustments for real-time tuning.

#### Setup Parameters:
```csharp
CarSetup
├─ frontWingAngle (0-10°)
│  └─ Effect: More angle = more downforce + more drag
├─ rearWingAngle (0-15°)
│  └─ Effect: Stability and rear grip
├─ differentialOnThrottle (0-100% lock)
│  └─ Effect: Exit traction vs mid-corner control
├─ differentialOffThrottle (0-100% lock)
│  └─ Effect: Turn-in precision
├─ suspensionStiffness (0-100)
│  └─ Effect: Response vs bumps
└─ brakeBiasFront (40-60%)
   └─ Effect: Stopping distance distribution
```

#### Impact on Physics:
```
Front Wing +1°:
  ├─ +0.5% downforce
  ├─ +0.3% drag
  ├─ -0.5 km/h top speed
  └─ +1% corner speed

Rear Wing +2°:
  ├─ +1.5% downforce
  ├─ Improves rear grip
  ├─ +0.8% drag
  └─ Better stability

Diff On-Throttle +10%:
  ├─ +5% exit acceleration
  ├─ -3% mid-corner feel
  └─ Higher DNF risk in wet

Suspension Stiffness +10:
  ├─ +2% response time
  ├─ +1% cornering speed
  ├─ -3% grip over bumps
  └─ Harsher ride feedback
```

### WindTunnelR&D.cs

Seasonal car development through wind tunnel research.

#### Upgrade Paths:
```csharp
Season R&D (100 hours budget)

1. Front Wing Drag Reduction
   ├─ +0-8% top speed (drag reduction)
   ├─ Time: ~25 hours to complete
   └─ Applied to all tracks

2. Rear Wing Downforce
   ├─ +0-12% cornering speed
   ├─ Time: ~30 hours
   └─ Best for technical circuits

3. Suspension Optimization
   ├─ +0-10% overall handling
   ├─ Time: ~35 hours
   └─ Affects all track types

4. Stability Improvement
   ├─ +0-5% wind resistance
   ├─ Time: ~20 hours
   └─ Helps on fast straights
```

#### Focus Research:
```csharp
Player can focus development on one upgrade:
  ├─ Speeds up progress by 2% per second
  ├─ One focus per season
  └─ Multiple focuses slow overall progress

Bonus = 1.0 + (upgrade.progressPercent / 100) * upgrade.maxPerformanceGain
  // If 50% done: gain 50% of max bonus
```

#### Integration with Physics:
```
After upgrade completes:

Aero Bonus = GetAerodynamicsBonus()
  └─ Applied to drag coefficient

Downforce Bonus = GetDownforceBonus()
  └─ Applied to corner speed calculations

Season Progression:
  ├─ Upgrade 1 complete: +3% pace
  ├─ Upgrade 2 complete: +5% pace
  ├─ All 4 complete: +15-20% total pace improvement
  └─ ResetSeason() at end-of-year
```

---

## INTEGRATION CHECKLIST

### Physics Engine:
- [ ] Add TrackSurfaceManager grip calculation
- [ ] Add WeatherEvolution puddle/wind effects
- [ ] Add DamageManager performance multipliers
- [ ] Add audio input to AudioEngineManager

### Game Loop:
- [ ] Call TelemetryRecorder.StartRecording() on race start
- [ ] Call TelemetryRecorder.EndRecording() on race end
- [ ] Call RivalrySystem.OnCollisionWithDriver() on collision
- [ ] Call TeamRadioManager contextual methods

### UI:
- [ ] Create DeltaTimer HUD element
- [ ] Create TeamRadio HUD panel
- [ ] Create CarSetupUI garage menu
- [ ] Create WindTunnelR&D progress display

---

## COMPILATION STATUS

✅ All 13 scripts compile without errors:
- TrackSurfaceManager.cs
- WeatherEvolution.cs
- TelemetrySystem.cs
- RecordManager.cs (includes DeltaTimerUI, TelemetryUI)
- DamageManager.cs
- RivalryAndTeamRadio.cs (includes 4 classes)
- HapticAndAudioManager.cs (includes HapticManager, AudioEngineManager)
- CarSetupManager.cs (includes 4 classes)

---

## NEXT STEPS

**Priority 1:**
- Integrate TrackSurfaceManager into PhysicsEngine
- Wire DamageManager to collision system
- Test haptic feedback on target devices

**Priority 2:**
- Implement visual damage models
- Create ghost car 3D model
- Add voice lines to team radio

**Priority 3:**
- Advanced AI adaptation to rivalry
- Weather forecast UI
- Live telemetry streaming

---

*End of Advanced Systems Documentation*
