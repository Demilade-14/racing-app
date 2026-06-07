# F1 25-Style Player Career Mode Documentation

## Overview

This is a comprehensive F1-authentic player career system for the mobile racing game. The system manages:
- **Driver creation & attributes** (0-99 scale, F1 25 style)
- **Season progression** (race weekends, qualifying, races)
- **Contract negotiations** (salary, bonuses, team offers)
- **Car R&D** (development tokens, upgrades for 4 categories)
- **Media & PR** (press conferences, team relationships)
- **Career progression** (promotion through F2 → F1)

---

## ARCHITECTURE OVERVIEW

### Core Managers

1. **DriverCareerManager.cs**
   - Main entry point for career lifecycle
   - Handles race result processing, season progression
   - Awards XP and skill points
   - Manages contracts

2. **RnDManager.cs**
   - Car upgrade system (Aerodynamics, PowerUnit, Chassis, Reliability)
   - Development token budget (500 tokens/season like F1)
   - Performance multipliers per category (1.5% per level, max 5 levels)

3. **MediaEventSystem.cs**
   - Post-race press conferences with player choices
   - Fan popularity & sponsor happiness tracking
   - Impacts team relationships and R&D budget

4. **ContractNegotiationEngine.cs**
   - Evaluates team offers based on driver rating, championship position
   - Generates counter-offers
   - Calculates market value

5. **CareerIntegrationLayer.cs**
   - Orchestrates all managers
   - Handles season loop flow
   - Saves/loads career progress

---

## DATA STRUCTURES

### DriverProfile
```
- Identity: profileId, driverName, driverCode (3-letter), nationality, raceNumber
- Attributes: cornering, braking, overtaking, defending, awareness, consistency (all 0-99)
- Season Stats: seasonPoints, seasonWins, seasonPodiums, seasonDNFs
- Career Totals: careerRaces, careerWins, careerPoints, careerChampionships
- Relationships: teamRelationship (0-100), teammateSatisfaction
- Collections: accolades, contractHistory, weekendHistory
```

### DriverAttributes
```
- Primary (0-99): cornering, braking, overtaking, defending, awareness, consistency
- Secondary (0-99): wetWeatherSkill, tireManagement
- XP Pools: corneringXP, brakingXP, etc. (fill to 1000 = +1 attribute)
- OverallRating: weighted average (15% cornering, 15% braking, 15% overtaking, 15% defending, 20% awareness, 20% consistency)
```

### RnDResearch
```
- categoryName: "Aerodynamics" | "PowerUnit" | "Chassis" | "Reliability"
- level: 1-5
- performanceGain: +0.5% per level (1.005^level)
- performanceMultiplier: applied to car telemetry
```

### DriverContract
```
- teamName, tier (F3/F2/F1)
- annualSalary, podiumBonus, winBonus
- durationSeasons, seasonsRemaining
- hasNumberOneClause (true = #1 driver status)
```

### DriverRaceWeekend
```
- round, circuitName
- fp1Done, fp2Done, fp3Done, qualiDone
- gridPosition (qualifying result)
- result (RaceResult object)
- overtakesThisRace, achievedFastestLap
- practicePrograms (PracticeProgram list)
```

---

## CAREER FLOW

### 1. CAREER START
```csharp
CareerIntegrationLayer.Instance.StartNewPlayerCareer("Lewis", "British", 1); // 0=F3, 1=F2, 2=F1

// Creates:
// - DriverProfile with attributes initialized to 50 (average)
// - Empty DriverCareerSave for the season
// - 20-24 race calendar
// - 3-4 season objectives
```

### 2. PRE-SEASON
```
- Player creates driver profile (avatar, helmet, suit design — future)
- Player selects starting team or free agent option
- Contract offer arrives with base salary
```

### 3. RACE WEEKEND FLOW
```
FP1 Practice Program → 5 XP
FP2 Setup Tuning → adjusts car setup (saved for race)
FP3 Race Simulation → 3 XP
Qualifying → player races, gridPosition set
Race → player races, RaceResult generated

Post-Race:
- XP awarded (50 for 1st place, 45 for 2nd, etc.)
- Press Conference triggered
  - Player chooses response (affects team/fan/sponsor morale)
  - Outcome determined
- R&D tokens awarded (8 for win, 6 for 2nd, etc.)
- Season stats updated
```

### 4. MID-SEASON
- Dynamic weather and mechanical failures (random)
- Teammate AI simulated (influences head-to-head stats)
- Sponsor notifications (income/expectations)

### 5. END OF SEASON
```
- Championship points finalized
- If top 3: promotion to next tier (F2→F1)
- Contract offers generated from teams
- Player negotiates (salary, number 1 status, duration)
- Season stats reset, next season begins
```

---

## XP & PROGRESSION SYSTEM

### Race XP Calculation
```
Position XP: 1st=50, 2nd=45, 3rd=40, 4-6=30, 7-10=20, DNF=0
Qualifying XP: P1-3=20, P4-6=15, P7-10=10
Fastest Lap: +15
Overtakes: +5 per overtake (max 50)
Total range: 0-150 XP per race
```

### XP → Attribute Conversion
```
1000 XP fills one attribute point (max 99)
Distribution strategy:
- Overtakes/Defending: from overtaking XP
- Consistency: from finishing position XP
- Racecraft: from position and quali XP
- Awareness: always a percentage of total
```

### Example: Player finishes P2 with 4 overtakes, P3 qualify
```
XP breakdown:
- 45 (P2) + 20 (P3 quali) + 20 (overtakes 4x5) + 15 (awareness base) = 100 total
- Consistency: +50 XP (45/2)
- Racecraft: +30 XP (45/3 + 15/3)
- Overtaking: +20 XP (overtakes weighted)
- Awareness: +remaining
```

---

## R&D SYSTEM

### Budget & Tokens
```
F1: 500 tokens/season (like real cost cap)
F2: 300 tokens/season
F3: 150 tokens/season

Token awards per race:
- 1st place: 8 tokens
- 2nd place: 6 tokens
- 3rd place: 4 tokens
- 4-6: 2 tokens
- 7-10: 1 token
- DNF: 0 tokens

Max budget cap: 500 (can't earn more than cap)
```

### Upgrade Costs & Performance Gains
```
Level 1→2: 100 tokens, +1.5% performance
Level 2→3: 200 tokens, +1.5% performance
Level 3→4: 300 tokens, +1.5% performance
Level 4→5: 400 tokens, +1.5% performance

Per-category multiplier:
Aero Level 3 = 1.045x (1 + 0.015*3)
PowerUnit Level 2 = 1.030x

Overall car performance:
Final Multiplier = Product of all categories
= 1.045 * 1.030 * 1.020 * 1.015 (example)
≈ 1.110 = +11% pace improvement
```

### Usage in Physics
```csharp
// In race telemetry:
carMaxSpeed *= RnD.GetCategoryMultiplier("PowerUnit");
downforceCoefficient *= RnD.GetCategoryMultiplier("Aerodynamics");
mechanicalGrip *= RnD.GetCategoryMultiplier("Chassis");
reliabilityFactor *= RnD.GetCategoryMultiplier("Reliability"); // reduces DNF chance
```

---

## CONTRACT NEGOTIATION

### Offer Generation Algorithm
```csharp
// Team creates offer based on:
// 1. Player's OVR rating
// 2. Championship position
// 3. Team's constructor budget
// 4. Seat availability

baseSalary = (OVR/99) * teamBudget * 0.15
// Example: OVR 85, Budget 150M → 85/99 * 150M * 0.15 = 19.3M/year

Number 1 offered if: OVR >= 80 AND team position <= 5

Contract length: top teams offer 2-3 years, backmarkers 1-2 years
```

### Evaluation Score (out of 100)
```
Salary factor:
  20%+ raise = +40 points
  Match salary = +30 points
  10% cut = +10 points
  >20% cut = -20 points

Team competitiveness: +30 (for top teams)
Number 1 status: +25 (if player OVR >=75)
Contract length: +15 (if preferred duration)
Series tier: +20 (for F1 offers)
Personality: +10 (lower-rated drivers are hungry)
Random: ±15 points

Decision logic:
- Score >= 70: ACCEPT immediately
- Score 40-70: COUNTER with +10-25% wage demand
- Score < 40: REJECT, seek other offers
```

### Example Negotiation
```
Player: OVR 78, currently Ferrari (€18M/year), finished P2

Mercedes offer:
- Salary: €22M/year (+22% raise) = +40 pts
- Team: top team = +30 pts
- Number 1: offered = +25 pts (OVR 78 > 75)
- Series: F1 = +20 pts
- Contract: 2 years (preferred) = +15 pts
- Random: +5 pts
Total: 135 pts → ACCEPT

Alternative: Aston Martin
- Salary: €16M/year (-11% cut) = +5 pts
- Team: mid-field = +10 pts
- Number 1: not offered = 0 pts
- Series: F1 = +20 pts
- Contract: 1 year = +5 pts
Total: 40 pts → COUNTER with €18M request
```

---

## MEDIA SYSTEM

### Press Conference Scenarios

#### Victory (P1 finish)
```
Q: "You dominated today. How does it feel?"

Option A: "Great team effort!"
  Team Relationship: +15
  Fan Popularity: +5
  Sponsor Happiness: +5
  Outcome: "Team morale boosted"

Option B: "I just drove hard. I made the difference."
  Team Relationship: -5
  Fan Popularity: +15
  Sponsor Happiness: 0
  Outcome: "Fans love confidence, team concerned about ego"

Option C: "Competition wasn't tough today"
  Team Relationship: +2
  Fan Popularity: -5
  Sponsor Happiness: -3
  Outcome: "Humble but media criticizes overconfidence"
```

#### Mid-field (P5-10)
```
Q: "Struggled with pace. What went wrong?"

Option A: "Car setup wasn't ideal"
  Team Relationship: +8
  Fan Popularity: 0
  Sponsor Happiness: +5

Option B: "I gave everything, car held me back"
  Team Relationship: -10
  Fan Popularity: +8
  Sponsor Happiness: -7

Option C: "Strategy and pit stop were slow"
  Team Relationship: -8
  Fan Popularity: +5
  Sponsor Happiness: -5
```

#### DNF (Did Not Finish)
```
Q: "You retired. What happened?"

Option A: "Mechanical failure"
  Team Relationship: +10
  Fan Popularity: +2
  Sponsor Happiness: +3

Option B: "I was pushing hard to catch leader"
  Team Relationship: -5
  Fan Popularity: +12
  Sponsor Happiness: -8

Option C: "These things happen"
  Team Relationship: 0
  Fan Popularity: -3
  Sponsor Happiness: 0
```

### Morale Impact on Career
```
Team Relationship = 0-100
- < 25: Risk of contract termination mid-season
- 25-50: Standard treatment
- 50-75: Preferred status (better car setup priority)
- > 75: #1 driver status benefits

Fan Popularity = 0-100
- Affects sponsorship value
- Sponsor bonus formula: baseBonus * (fanPopularity / 100)
- Example: 50% fan popularity = 50% sponsor income reduction

Sponsor Happiness = 0-100
- Unlocks R&D token bonus
- Bonus = (sponsorHappiness / 100) * 50 tokens/season
- Example: 80% happiness = +40 bonus tokens
```

---

## JSON SAVE SCHEMA

```json
{
  "saveId": "driver_career_001",
  "slotName": "Lewis Hamilton 2026",
  "slotType": "Driver",
  "career": {
    "saveId": "driver_career_001",
    "modeType": "Driver",
    "phase": "RaceWeekend",
    "currentRound": 5,
    
    "profile": {
      "profileId": "player_uuid",
      "driverName": "Lewis",
      "driverCode": "LH",
      "nationality": "British",
      "raceNumber": 44,
      "tier": "Formula1",
      "season": 1,
      "currentTeam": "Mercedes",
      
      "attributes": {
        "cornering": 82,
        "braking": 85,
        "overtaking": 78,
        "defending": 80,
        "awareness": 86,
        "consistency": 83,
        "overallRating": 82
      },
      
      "seasonStats": {
        "seasonPoints": 68,
        "seasonWins": 2,
        "seasonPodiums": 4,
        "seasonPoles": 2,
        "seasonFastestLaps": 3,
        "seasonDNFs": 0,
        "seasonOvertakes": 12
      },
      
      "careerTotals": {
        "careerRaces": 45,
        "careerWins": 15,
        "careerPodiums": 35,
        "careerPoints": 1850,
        "careerChampionships": 1
      },
      
      "activeContract": {
        "teamName": "Mercedes",
        "tier": "Formula1",
        "annualSalary": 25000000,
        "podiumBonus": 2500000,
        "winBonus": 10000000,
        "durationSeasons": 2,
        "seasonsRemaining": 1,
        "hasNumberOneClause": true
      },
      
      "teamRelationship": 82,
      "rnDBudget": {
        "tokensAvailable": 340,
        "tokensBudgeted": 160,
        "research": [
          {
            "categoryName": "Aerodynamics",
            "level": 3,
            "performanceGain": 0.045
          }
        ]
      }
    },
    
    "calendar": [
      {
        "round": 1,
        "circuitName": "Bahrain",
        "gridPosition": 1,
        "result": {
          "finishPosition": 1,
          "pointsEarned": 25,
          "hasFastestLap": true,
          "retired": false
        },
        "overtakesThisRace": 0,
        "achievedFastestLap": true
      }
    ],
    
    "objectives": [
      {
        "description": "Score 3 wins this season",
        "status": "Pending",
        "reputationReward": 20,
        "skillPointReward": 50,
        "isPrimary": true
      }
    ],
    
    "pendingEvents": [
      {
        "id": "post_race_interview_5",
        "type": "PressConference",
        "questionsAsked": 3,
        "choicesMade": 2
      }
    ]
  },
  
  "savedAt": "2026-06-07T14:32:00Z",
  "totalPlaySeconds": 3600
}
```

---

## UI/UX FLOW

### Main Menu
```
┌─ New Game
│   └─ Select Series (F3 / F2 / F1)
│       └─ Driver Creation (Name, Nationality, Number, Helmet, Suit)
│           └─ Team Selection / Free Agent
│               └─ Career begins!
│
├─ Continue Career
│   └─ Load last save slot
│       └─ Career Hub
│
└─ Career Slot Manager
    ├─ List all saves
    ├─ Create new slot
    ├─ Delete slot
    └─ Select slot to continue
```

### Career Hub (Main Loop)
```
┌─ [Driver Card] Lewis | P2 | 68 pts | OVR 82
│   └─ Attributes breakdown
│   └─ Contract details
│   └─ Market value
│
├─ [Season Progress]
│   ├─ Race Calendar
│   │   ├─ Completed races (with results)
│   │   ├─ Upcoming races
│   │   └─ [START RACE BUTTON]
│   └─ Championship Standings
│       └─ P1-20 driver grid
│
├─ [R&D Garage]
│   ├─ Aerodynamics (Lv3 | +4.5% | 200/300 tokens needed)
│   ├─ PowerUnit (Lv1 | +1.5% | 100 tokens needed)
│   ├─ Chassis (Lv2 | +3% | 200 tokens needed)
│   ├─ Reliability (Lv1 | +1.5% | 100 tokens needed)
│   └─ Budget Display: 340/500 tokens available
│
├─ [Team & Media]
│   ├─ Team Relationship: 82/100 ⭐⭐⭐⭐
│   ├─ Fan Popularity: 65/100
│   ├─ Sponsor Happiness: 71/100
│   └─ Recent Headlines
│
├─ [Contracts]
│   ├─ Current: Mercedes €25M/year, 1 year remaining
│   └─ [Upcoming offers from teams]
│
└─ [Settings & Save]
    ├─ Settings (Graphics, Audio, etc.)
    ├─ Save Game
    └─ Load Game / Exit
```

### Race Weekend View
```
FP1/FP2/FP3
├─ [Practice Program Selector]
│   ├─ "Tyre Management" → +20 Consistency XP
│   ├─ "Quali Simulation" → +15 Awareness XP
│   └─ "Race Pace" → +20 Racecraft XP
│
├─ [Car Setup Editor] (optional depth)
│   ├─ Front Wing Angle
│   ├─ Brake Bias
│   ├─ Tire Pressure
│   └─ [Auto Setup by AI]
│
└─ [Run Practice / Save Setup]

QUALIFYING
├─ [Driver on track]
├─ [Lap times display]
└─ Q1/Q2/Q3 progression

RACE
├─ [Full race sim / highlight moments]
├─ [Weather & safety cars]
├─ [DRS / ERS deployment]
└─ [Finish with result & XP summary]

POST-RACE SCREEN
├─ [Result: P2, +18pts, +68 XP]
├─ [XP Breakdown]
│   ├─ Consistency +30 XP
│   ├─ Racecraft +20 XP
│   ├─ Overtaking +10 XP
│   └─ Awareness +8 XP
├─ [R&D Tokens +6]
└─ [PRESS CONFERENCE BUTTON]
    └─ Leads to media choice dialog
```

### Press Conference Dialog
```
┌─ [Question headline]
│
├─ [Choice A] "Team effort was key" [+15 Team, +5 Fans, +5 Sponsors]
├─ [Choice B] "I drove best today" [-5 Team, +15 Fans, 0 Sponsors]
└─ [Choice C] "Competition wasn't tough" [+2 Team, -5 Fans, -3 Sponsors]

[SELECT CHOICE]
↓
[Outcome modal with reputation impact & headline]
```

### End of Season
```
┌─ [Season Summary]
│   ├─ Champion: P1 - Max Verstappen (405 pts)
│   ├─ You: P2 - Lewis Hamilton (380 pts)
│   └─ Championship Progression Chart
│
├─ [Personal Stats]
│   ├─ Wins: 2 / Podiums: 4 / Poles: 2 / DNFs: 0
│   ├─ Overtakes: 34
│   └─ vs Teammate: +45 pts
│
├─ [Achievements]
│   ├─ "Consistent Performer" ⭐
│   ├─ "Overtake Master" ⭐⭐
│   └─ "Podium Finisher" ⭐
│
├─ [Contract Offers]
│   ├─ Ferrari: €28M/year, 2yrs, #1 Driver [NEGOTIATE]
│   ├─ Red Bull: €26M/year, 2yrs, #2 Driver [NEGOTIATE]
│   └─ McLaren: €20M/year, 1yr, #1 Driver [NEGOTIATE]
│
└─ [Next Season Preview]
    └─ Promotion to F1? (if applicable)
```

---

## INTEGRATION WITH EXISTING SYSTEMS

### Physics Engine Integration
```csharp
// In PhysicsEngine.cs OnFixedUpdate():
float rndMultiplier = RnDManager.Instance.GetOverallPerformanceMultiplier();
float topSpeedModified = baseTopSpeed * rndMultiplier;
float downforceModified = baseDownforce * 
    RnDManager.Instance.GetCategoryMultiplier("Aerodynamics");

// Driver attributes affect driving
float cornersSpeedMultiplier = 1.0f + (driverOVR - 50) * 0.002f; // elite drivers corner faster
```

### Championship Manager Integration
```csharp
// In GameBootstrap.cs after race:
Championship.RegisterPlayer(
    DriverCareerManager.Instance.PlayerProfile.driverName,
    DriverCareerManager.Instance.PlayerProfile.currentTeam,
    1  // seatNumber
);

Championship.ProcessRound(new List<RaceResult> { raceResult });
int finalPos = Championship.GetPlayerPosition(driverName);
DriverCareerManager.Instance.OnSeasonComplete?.Invoke(finalPos);
```

### Save System Integration
```csharp
// In SaveSystem.cs LoadCareer():
public void LoadCareer()
{
    if (Slots.Count > 0)
    {
        Current = Slots[SelectedSlotIndex];
        
        if (Current.career?.careerType == CareerType.Driver)
        {
            // Load driver career into DriverCareerManager
            var careerData = Current.career;
            DriverCareerManager.Instance.CurrentSeason = 
                JsonUtility.FromJson<DriverCareerSave>(
                    JsonUtility.ToJson(careerData));
        }
    }
}
```

---

## QUICK SETUP CHECKLIST

- [x] Create DriverCareerManager.cs
- [x] Create RnDManager.cs
- [x] Create MediaEventSystem.cs
- [x] Create ContractNegotiationEngine.cs
- [x] Create CareerIntegrationLayer.cs
- [ ] Create UI prefabs (Career Hub, Press Conference, R&D Garage)
- [ ] Wire CareerIntegrationLayer to GameBootstrap
- [ ] Test race result → XP → attribute progression
- [ ] Test R&D token awards and upgrades
- [ ] Test press conference choices
- [ ] Test contract negotiation flow
- [ ] Test season end promotion logic
- [ ] Integrate save/load with SaveManager

---

## NOTES ON F1 AUTHENTICITY

1. **Attribute System:** Mirrors F1 25 (0-99 scale, weighted OVR)
2. **R&D Tokens:** Real F1 uses cost cap; this mimics token budget system
3. **Contract Dynamics:** Real F1 features salary-based team tier hierarchy
4. **Press System:** Real F1 25 has post-race media questions with outcomes
5. **Championship:** F1 points system (25-18-15-12-10-8-6-4-2-1)
6. **Series Progression:** Real drivers move F2→F1 after strong seasons

This system creates the **depth and realism** of F1 25 My Career adapted for mobile with touch-friendly UX.
