# F1-Style Mobile Racing Game

A Formula 1-inspired mobile racing game for iOS & Android built in Unity 2022 LTS with C#.

---

## Project Structure

```
Assets/Scripts/
├── Data/
│   ├── GameData.cs        – All shared data structures & enums
│   └── GameContent.cs     – Fictional teams, drivers, circuits
├── Physics/
│   └── PhysicsEngine.cs   – Tire, aero, fuel, damage, lap time, integrator
├── AI/
│   └── AISystem.cs        – AI driver, brain, state machine, pit strategy
├── Career/
│   └── CareerSystem.cs    – Career save, contracts, sponsors, skill upgrades
├── Multiplayer/
│   └── MultiplayerSystem.cs – ELO, matchmaking, lobby, network sync, anti-cheat
├── UI/
│   └── UISystem.cs        – HUD builder, telemetry, race session, standings
├── Utils/
│   └── Utilities.cs       – Object pool, mobile optimizer, LOD switcher
└── Tests/
    └── PhysicsTests.cs    – NUnit tests: tire, lap time, ELO, damage
```

---

## Tech Stack

| Layer        | Technology                        |
|--------------|-----------------------------------|
| Engine       | Unity 2022 LTS                    |
| Language     | C# (.NET Standard 2.1)            |
| Backend      | AWS GameLift + Lambda + DynamoDB  |
| Real-time    | WebSocket (AWS API Gateway)       |
| Auth / Save  | AWS Cognito + S3 cloud save       |
| Analytics    | AWS Pinpoint / Firebase           |
| CI/CD        | GitHub Actions + Unity Cloud Build|

---

## Core Systems

### Physics Engine (`PhysicsEngine.cs`)
- **TirePhysics** – temperature curve (0–120 °C), compound wear rates, grip formula  
  `grip = baseGrip × tempFactor × wearFactor × weatherFactor`
- **AeroPhysics** – downforce/drag from wing angles, DRS reduces drag 25 %
- **FuelSystem** – per-tick burn based on throttle, engine mode, speed
- **LapTimeCalculator** – sector-based estimate using driver rating, tires, fuel, weather
- **DamageSystem** – collision force thresholds → component degradation → DNF check
- **LongitudinalPhysics** – acceleration and braking force from engine power + grip
- **PhysicsIntegrator** – MonoBehaviour FixedUpdate entry point (0.02 s timestep)

### AI System (`AISystem.cs`)
- **DifficultyProfile** – 4 presets (Easy / Medium / Hard / Expert) with tunable params
- **AIDriver** – personality, form variance (0.88–1.12), race state tracking
- **AIBrain** – 100 ms decision tick, 6-state machine, waypoint line following
- **PitStrategyCalc** – triggers pit on tire wear > 70 %, fuel < 8 L, or undercut window

### Career System (`CareerSystem.cs`)
- 3-tier series: Formula 3 → Formula 2 → Formula 1 equivalent  
- Contract offers filtered by team power vs. driver reputation  
- Skill point economy: earn points per race, spend to upgrade 5 driver stats  
- Sponsor income: per-race + podium bonus, expires after N races  

### Multiplayer (`MultiplayerSystem.cs`)
- **EloRating** – standard K=32 formula, multi-player batch update  
- **MatchmakingService** – coroutine queue, ±200 rating window expanding over time  
- **LobbyController** – Waiting → Loading → Racing → Results state machine  
- **NetworkManager** – 20 Hz vehicle state broadcast, dead-reckoning interpolation  
- **AntiCheat** – server-side speed cap (380 km/h) and position teleport detection  

---

## AWS Backend Architecture

```
Mobile Client
    │
    ├─ REST (HTTPS)  →  API Gateway → Lambda → DynamoDB
    │                   (matchmaking, career save, leaderboards)
    │
    └─ WebSocket     →  API Gateway → GameLift Fleet
                        (real-time race sync 20 Hz)
```

**DynamoDB Tables**
- `Players`      – PK: playerId | rating, wins, region, careerSave (JSON)
- `Matches`      – PK: matchId  | circuit, players[], results[], timestamp
- `Leaderboards` – PK: circuit  | SK: lapTime | playerId, region
- `Lobbies`      – PK: lobbyId  | state, settings, players[] (TTL 1 h)

---

## Development Roadmap

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| 1 – MVP        | 8 weeks  | Physics, 1 circuit, basic AI, career (F3), main menu |
| 2 – Extended   | 6 weeks  | 5 circuits, multiplayer 2–4 players, tire/fuel system |
| 3 – Polish     | 4 weeks  | 10 circuits, 8-player MP, leaderboards, cosmetics     |
| 4 – Post-launch| Ongoing  | Seasons, battle pass, community events                |

---

## Performance Targets

| Metric         | Target                        |
|----------------|-------------------------------|
| Frame rate     | 60 FPS on 2 GB RAM devices    |
| Memory usage   | < 500 MB during gameplay      |
| App size       | < 1 GB compressed             |
| Menu load      | < 3 s                         |
| Circuit load   | < 5 s                         |
| MP latency     | < 200 ms end-to-end           |

---

## Running Tests

Open Unity → **Window → General → Test Runner** → Run All (EditMode).  
Tests cover: tire grip, lap time estimation, ELO calculations, damage thresholds.

---

## Getting Started

1. Install **Unity 2022 LTS** with iOS + Android Build Support modules.
2. Clone this repo into your Unity project's `Assets/` folder.
3. Install **NUnit** via the Unity Package Manager (bundled with Unity Test Framework).
4. Configure AWS credentials in `Assets/Resources/AWSConfig.json` (not committed).
5. Set `Application.targetFrameRate = 60` in Project Settings → Player.
