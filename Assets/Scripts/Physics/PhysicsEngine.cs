using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Physics
{
    // ═══════════════════════════════════════════════════════════════════════
    //  TIRE THERMODYNAMICS  –  dual-layer surface / core model
    // ═══════════════════════════════════════════════════════════════════════
    public static class TirePhysics
    {
        // ── Heat-transfer constants ────────────────────────────────────────
        // Surface layer heats ~10× faster than carcass; they exchange heat
        // with each other and both lose heat to ambient air.
        const float SURFACE_HEAT_CAPACITY  = 1.0f;    // relative (normalised)
        const float CORE_HEAT_CAPACITY     = 8.0f;    // carcass is much more massive
        const float SURFACE_TO_CORE_XFER   = 0.18f;   // W fraction per °C delta per s
        const float SURFACE_AIR_COOLING    = 0.35f;   // convective loss coefficient
        const float CORE_AIR_COOLING       = 0.04f;   // insulated from airflow

        // ── Graining / blistering thresholds ──────────────────────────────
        const float GRAINING_HEAL_RATE     = 0.002f;  // per second once in window
        const float BLISTERING_HEAL_RATE   = 0.0003f; // blisters don't really heal

        // ── Pressure ──────────────────────────────────────────────────────
        // Charles's Law approximation: pressure rises ~0.4 PSI per 10 °C above cold
        const float PRESSURE_RISE_PER_10C  = 0.4f;
        const float COLD_BASELINE_TEMP     = 20f;     // °C at which coldPressure is set

        // ─────────────────────────────────────────────────────────────────
        //  PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Master grip formula. Uses surface temp for the temperature factor
        /// (that is what contacts the track), and applies graining/blistering
        /// and dirty-air slip penalty on top.
        /// </summary>
        public static float CalculateGrip(TireState tire, WeatherData weather)
        {
            float compoundWeatherMatch = CompoundWeatherMatch(tire, weather);
            float tempFactor           = TemperatureFactor(tire);
            float wearFactor           = WearFactor(tire.wearPercent);
            float grainingPenalty      = 1f - tire.grainingLevel   * 0.18f;
            float blisterPenalty       = 1f - tire.blisteringLevel * 0.25f;

            // High pressure → smaller contact patch → less grip
            float pressurePenalty = PressureGripFactor(tire);

            // Dirty air increases effective slip angle → less lateral grip
            float dirtyAirPenalty = 1f - Mathf.Clamp01(tire.dirtyAirSlipDelta / 4f) * 0.08f;

            return Mathf.Clamp01(
                tire.BaseGrip
              * tempFactor
              * wearFactor
              * weather.GripMultiplier
              * compoundWeatherMatch
              * Mathf.Lerp(0.90f, 1.00f, tire.surfaceGrip)
              * grainingPenalty
              * blisterPenalty
              * pressurePenalty
              * dirtyAirPenalty);
        }

        /// <summary>Full tick: advance both thermal layers, pressure, graining, blistering.</summary>
        public static void UpdateTire(TireState tire, VehicleState car,
                                      WeatherData weather, float dt)
        {
            UpdateDualLayerTemps(tire, car, weather, dt);
            UpdatePressure(tire);
            UpdateGraining(tire, car, dt);
            UpdateBlistering(tire, dt);
            UpdateWear(tire, car, weather, dt);
        }

        // ─────────────────────────────────────────────────────────────────
        //  TEMPERATURE (dual-layer)
        // ─────────────────────────────────────────────────────────────────

        static void UpdateDualLayerTemps(TireState tire, VehicleState car,
                                          WeatherData weather, float dt)
        {
            // ── Friction heat generated at surface ────────────────────────
            // Lateral slip, braking slip, wheel-spin all generate surface heat.
            float effectiveSlip = Mathf.Abs(tire.slipAngle) / 12f        // lateral
                                + Mathf.Abs(tire.slipRatio)               // longitudinal
                                + tire.dirtyAirSlipDelta / 6f;            // turbulence extra

            float frictionHeat = effectiveSlip
                               * (0.6f + car.speedMs * 0.005f)            // speed-scaled
                               * Mathf.Lerp(1f, 0.6f, weather.rainIntensity); // rain cools

            // Braking and aggressive steering also add heat directly
            float drivingHeat = Mathf.Abs(car.steering) * 30f
                              + car.brake               * 28f
                              + car.throttle            * 12f
                              + tire.dirtyAirHeatDelta;

            float totalSurfaceHeat = (frictionHeat + drivingHeat) * 5f;   // scale to °C/s

            // ── Surface-layer cooling ─────────────────────────────────────
            float ambientCooling  = (tire.surfaceTemp - weather.ambientTemp) * SURFACE_AIR_COOLING;
            float rainCooling     = weather.rainIntensity * 45f;
            float speedCooling    = car.speedMs * 0.08f;                   // airflow

            // ── Surface ↔ Core heat exchange ──────────────────────────────
            float surfaceToCoreXfer = (tire.surfaceTemp - tire.coreTemp)
                                     * SURFACE_TO_CORE_XFER;

            // ── Integrate surface temp ─────────────────────────────────────
            float dSurface = (totalSurfaceHeat - ambientCooling - rainCooling
                              - speedCooling   - surfaceToCoreXfer)
                            / SURFACE_HEAT_CAPACITY;

            tire.surfaceTemp = Mathf.Clamp(tire.surfaceTemp + dSurface * dt, 0f, 160f);

            // ── Core heated by surface, cooled slowly by structure ─────────
            float coreAmbientLoss = (tire.coreTemp - weather.ambientTemp) * CORE_AIR_COOLING;
            float dCore           = (surfaceToCoreXfer - coreAmbientLoss) / CORE_HEAT_CAPACITY;

            tire.coreTemp = Mathf.Clamp(tire.coreTemp + dCore * dt, 0f, 200f);
        }

        // ─────────────────────────────────────────────────────────────────
        //  DYNAMIC PRESSURE
        // ─────────────────────────────────────────────────────────────────

        static void UpdatePressure(TireState tire)
        {
            float tempRise     = Mathf.Max(0f, tire.coreTemp - COLD_BASELINE_TEMP);
            tire.currentPressurePSI = tire.coldPressurePSI
                                    + tempRise / 10f * PRESSURE_RISE_PER_10C;
        }

        /// <summary>
        /// Grip penalty from over/under pressure.
        /// Nominal ~23 PSI → factor 1.0.  Too high or too low = penalty.
        /// </summary>
        static float PressureGripFactor(TireState tire)
        {
            float delta = Mathf.Abs(tire.currentPressurePSI - tire.coldPressurePSI);
            return Mathf.Clamp01(1f - delta * 0.012f);
        }

        // ─────────────────────────────────────────────────────────────────
        //  GRAINING  (cold surface + high lateral load → micro-tears)
        // ─────────────────────────────────────────────────────────────────

        static void UpdateGraining(TireState tire, VehicleState car, float dt)
        {
            bool surfaceCold       = tire.surfaceTemp < tire.OptimalTempMin;
            float lateralStress    = Mathf.Abs(tire.slipAngle) / 8f; // 0-1ish

            if (surfaceCold && lateralStress > 0.3f)
            {
                // Graining forms when cold rubber shears under lateral load
                float grainRate = lateralStress
                                * (1f - tire.surfaceTemp / tire.OptimalTempMin)
                                * tire.WearRate * 0.04f;
                tire.grainingLevel = Mathf.Clamp01(tire.grainingLevel + grainRate * dt);
            }
            else if (!surfaceCold && tire.grainingLevel > 0f)
            {
                // Graining "burns off" once tires are in window
                tire.grainingLevel = Mathf.Max(0f,
                    tire.grainingLevel - GRAINING_HEAL_RATE * dt);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  BLISTERING  (overheated core cooking rubber from inside)
        // ─────────────────────────────────────────────────────────────────

        static void UpdateBlistering(TireState tire, float dt)
        {
            if (tire.coreTemp > tire.BlisterThreshold)
            {
                float excess      = tire.coreTemp - tire.BlisterThreshold;
                float blisterRate = excess * 0.002f * tire.WearRate;
                tire.blisteringLevel = Mathf.Clamp01(
                    tire.blisteringLevel + blisterRate * dt);
            }
            else if (tire.blisteringLevel > 0f)
            {
                // Blisters barely heal; tiny decay only
                tire.blisteringLevel = Mathf.Max(0f,
                    tire.blisteringLevel - BLISTERING_HEAL_RATE * dt);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  WEAR
        // ─────────────────────────────────────────────────────────────────

        static void UpdateWear(TireState tire, VehicleState car,
                               WeatherData weather, float dt)
        {
            float stress = 1f
                + Mathf.Abs(car.steering)         * 1.8f
                + car.brake                        * 1.4f
                + Mathf.Abs(tire.slipRatio)        * 2.0f
                + tire.grainingLevel               * 1.2f  // grain accelerates wear
                + tire.blisteringLevel             * 2.5f  // blisters catastrophic
                + (tire.coreTemp > tire.BlisterThreshold ? 1.5f : 0f);

            stress *= Mathf.Lerp(1f, 0.55f, weather.rainIntensity);

            float wearPerSec = tire.WearRate * 0.007f * stress;
            tire.wearPercent = Mathf.Clamp(tire.wearPercent + wearPerSec * dt, 0f, 100f);
        }

        // ─────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Grip factor 0-1 based on surface temperature vs optimal window.</summary>
        public static float TemperatureFactor(TireState tire)
        {
            float t    = tire.surfaceTemp;
            float tMin = tire.OptimalTempMin;
            float tMax = tire.OptimalTempMax;

            if (t < tMin * 0.5f)
                return Mathf.Lerp(0.38f, 0.72f, t / (tMin * 0.5f));
            if (t < tMin)
                return Mathf.Lerp(0.72f, 1.00f, (t - tMin * 0.5f) / (tMin * 0.5f));
            if (t <= tMax)
                return 1.00f;
            return Mathf.Lerp(1.00f, 0.50f, (t - tMax) / 35f);
        }

        public static float WearFactor(float wearPercent) =>
            Mathf.Clamp01(1f - wearPercent / 100f);

        static float CompoundWeatherMatch(TireState tire, WeatherData weather)
        {
            if (weather.RequiresWetTires && tire.compound <= TireCompound.Hard)
                return Mathf.Lerp(1f, 0.42f, weather.rainIntensity);
            if (weather.RequiresInterTires && tire.compound <= TireCompound.Hard)
                return Mathf.Lerp(1f, 0.68f, weather.rainIntensity);
            if (tire.compound == TireCompound.Wet && weather.condition == WeatherCondition.Dry)
                return 0.58f;
            return 1f;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ERS  –  State-of-Charge battery model with 4 deployment modes
    // ═══════════════════════════════════════════════════════════════════════
    public static class ERSSystem
    {
        // ── Battery / hardware constants (2026-era F1 approximation) ──────
        const float BATTERY_CAPACITY_KJ    = 4000f;   // kJ total usable
        const float MGU_K_MAX_DEPLOY_KW    = 120f;    // kW max motor deployment
        const float MGU_K_MAX_HARVEST_KW   = 50f;     // kW max brake regen
        const float MGU_H_HARVEST_KW       = 30f;     // kW from exhaust gas turbine
        const float MAX_DEPLOY_PER_LAP_KJ  = 4000f;   // can deploy full battery per lap
        const float OVERTAKE_BURST_SECS    = 10f;     // max duration of Overtake mode

        // ── Deployment power by mode ───────────────────────────────────────
        static float DeployKw(ERSMode mode) => mode switch
        {
            ERSMode.Qualifying => 120f,   // flat-out drain, no harvest priority
            ERSMode.Overtake   =>  80f,   // burst; limited to OVERTAKE_BURST_SECS
            ERSMode.Balanced   =>  40f,   // normal racing
            ERSMode.Harvest    =>   0f,   // no deployment, maximise harvest
            _ => 40f
        };

        /// <summary>
        /// Additional traction force from ERS motor (N).
        /// </summary>
        public static float DeployForce(VehicleState car)
        {
            if (car.ersSoC <= 0f) return 0f;
            if (car.speedMs < 0.5f) return 0f;

            // Overtake burst degrades after timer expires
            float kw = DeployKw(car.ersMode);
            if (car.ersMode == ERSMode.Overtake && car.ersOvertakeTimer <= 0f) kw = 0f;

            float powerW = kw * 1000f;
            return powerW / car.speedMs;
        }

        /// <summary>
        /// Tick the battery SoC each physics step. Returns updated SoC (0-100 %).
        /// Also updates the overtake timer inside the VehicleState.
        /// </summary>
        public static float TickBattery(VehicleState car, float dt)
        {
            // ── Deployment drain ──────────────────────────────────────────
            float deployKw = DeployKw(car.ersMode);
            if (car.ersMode == ERSMode.Overtake)
            {
                if (car.ersOvertakeTimer > 0f)
                    car.ersOvertakeTimer -= dt;
                else
                    deployKw = 0f;   // burst expired
            }

            float deployKj = deployKw * 1000f * dt / 1000f;  // kJ used this tick
            float drainPct = deployKj / BATTERY_CAPACITY_KJ * 100f;

            // ── Harvest from MGU-K (braking) ──────────────────────────────
            float brakingHarvestKw = car.brake * MGU_K_MAX_HARVEST_KW;

            // ── Harvest from MGU-H (exhaust) always trickles ──────────────
            float exhaustHarvestKw = car.rpm > 9000f ? MGU_H_HARVEST_KW * 0.5f : MGU_H_HARVEST_KW * 0.15f;

            float harvestKj  = (brakingHarvestKw + exhaustHarvestKw) * 1000f * dt / 1000f;
            float harvestPct = harvestKj / BATTERY_CAPACITY_KJ * 100f;

            // In Harvest mode boost harvest efficiency
            if (car.ersMode == ERSMode.Harvest) harvestPct *= 1.4f;

            float newSoC = Mathf.Clamp(car.ersSoC - drainPct + harvestPct, 0f, 100f);

            // Keep legacy 0-1 fields in sync for backward-compat
            car.ersEnergy = newSoC / 100f;
            car.ersDeploy = deployKw / MGU_K_MAX_DEPLOY_KW;

            return newSoC;
        }

        /// <summary>
        /// Activate Overtake mode. Resets burst timer. No-op if battery too low.
        /// </summary>
        public static void ActivateOvertake(VehicleState car)
        {
            if (car.ersSoC < 10f) return;   // need minimum charge
            car.ersMode            = ERSMode.Overtake;
            car.ersOvertakeTimer   = OVERTAKE_BURST_SECS;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AERODYNAMIC WAKE  –  dirty air & slipstream
    // ═══════════════════════════════════════════════════════════════════════
    public static class AerodynamicWakeSystem
    {
        // ── Dirty-air constants ────────────────────────────────────────────
        const float DIRTY_AIR_GAP_SEC       = 1.5f;   // gap threshold (seconds)
        const float DIRTY_AIR_DOWNFORCE_LOSS = 0.10f; // 10% downforce reduction
        const float DIRTY_AIR_SLIP_INCREASE  = 2.5f;  // extra degrees of slip angle
        const float DIRTY_AIR_SURFACE_HEAT   = 8f;    // °C/s extra surface heat

        // ── Slipstream constants ───────────────────────────────────────────
        const float SLIPSTREAM_GAP_MAX_SEC   = 1.5f;  // effective range
        const float SLIPSTREAM_GAP_MIN_SEC   = 0.05f; // must be behind, not alongside
        const float SLIPSTREAM_SPEED_BONUS   = 5f;    // m/s top-speed bonus (≈18 km/h)

        /// <summary>
        /// Call once per physics tick for Car B that is behind Car A.
        /// Updates Car B's VehicleState dirty-air and slipstream fields,
        /// and injects extra slip angle / heat into each of Car B's 4 tires.
        /// </summary>
        public static void ApplyWake(VehicleState behind, VehicleState ahead,
                                     float gapSeconds)
        {
            // Clear last frame's values
            behind.inDirtyAir            = false;
            behind.dirtyAirDownforceLoss = 0f;
            behind.slipstreamSpeedBonus  = 0f;

            foreach (var t in behind.tires)
            {
                t.dirtyAirSlipDelta = 0f;
                t.dirtyAirHeatDelta = 0f;
            }

            if (gapSeconds <= 0f || gapSeconds > DIRTY_AIR_GAP_SEC) return;

            // Intensity: 1.0 right behind, 0.0 at the gap threshold
            float intensity = 1f - gapSeconds / DIRTY_AIR_GAP_SEC;

            // ── Dirty air ─────────────────────────────────────────────────
            behind.inDirtyAir            = true;
            behind.dirtyAirDownforceLoss = DIRTY_AIR_DOWNFORCE_LOSS * intensity;
            behind.gapToCarAheadSec      = gapSeconds;

            foreach (var t in behind.tires)
            {
                t.dirtyAirSlipDelta = DIRTY_AIR_SLIP_INCREASE * intensity;
                t.dirtyAirHeatDelta = DIRTY_AIR_SURFACE_HEAT  * intensity;
            }

            // ── Slipstream (low-pressure wake reduces drag → speed boost) ─
            // Only effective when closely behind on a straight (low steering)
            float steerFactor = 1f - Mathf.Clamp01(Mathf.Abs(behind.steering) * 3f);
            behind.slipstreamSpeedBonus = SLIPSTREAM_SPEED_BONUS * intensity * steerFactor;
        }

        /// <summary>
        /// Adjusts the raw drag force of Car B based on its slipstream / dirty-air state.
        /// Call after AeroPhysics.CalculateDrag to get the net effective drag.
        /// </summary>
        public static float AdjustDrag(float rawDrag, VehicleState car)
        {
            if (!car.inDirtyAir) return rawDrag;

            // Slipstream cuts drag (good)
            float slipDragReduction = (car.slipstreamSpeedBonus / 80f) * rawDrag;

            // Dirty-air turbulence adds a small parasitic drag (bad)
            float turbulenceDrag = rawDrag * car.dirtyAirDownforceLoss * 0.15f;

            return Mathf.Max(0f, rawDrag - slipDragReduction + turbulenceDrag);
        }

        /// <summary>
        /// Adjusts the downforce of Car B.
        /// </summary>
        public static float AdjustDownforce(float rawDownforce, VehicleState car)
        {
            return rawDownforce * (1f - car.dirtyAirDownforceLoss);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AERODYNAMICS
    // ═══════════════════════════════════════════════════════════════════════
    public static class AeroPhysics
    {
        const float AIR_DENSITY     = 1.225f;
        const float DRS_DRAG_FACTOR = 0.75f;
        const float REF_AREA        = 1.5f;

        public static float CalculateDownforce(float speedMs, CarSetup setup,
                                               ComponentDamage dmg, VehicleState car = null)
        {
            float wingLoad = (setup.frontWingAngle + setup.rearWingAngle) / 22f;
            float cl       = 2.8f * wingLoad;
            float floorDmg = 1f - dmg.floor      / 100f * 0.30f;
            float bargeDmg = 1f - dmg.bargeboard / 100f * 0.15f;

            float raw = 0.5f * cl * AIR_DENSITY * REF_AREA * speedMs * speedMs
                      * floorDmg * bargeDmg;

            return car != null ? AerodynamicWakeSystem.AdjustDownforce(raw, car) : raw;
        }

        public static float CalculateDrag(float speedMs, CarSetup setup,
                                          bool drsActive, ComponentDamage dmg,
                                          VehicleState car = null)
        {
            float wingLoad = (setup.frontWingAngle + setup.rearWingAngle) / 22f;
            float cd       = 0.85f + wingLoad * 0.45f;
            if (drsActive) cd *= DRS_DRAG_FACTOR;

            float bodyDmg = 1f + (dmg.frontWing + dmg.rearWing) / 100f * 0.15f;
            float raw     = 0.5f * cd * AIR_DENSITY * REF_AREA * speedMs * speedMs * bodyDmg;

            return car != null ? AerodynamicWakeSystem.AdjustDrag(raw, car) : raw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ENGINE / POWERTRAIN
    // ═══════════════════════════════════════════════════════════════════════
    public static class PowertrainPhysics
    {
        static readonly float[] GEAR_RATIOS = { 0f, 3.20f, 2.35f, 1.83f, 1.47f,
                                                 1.22f, 1.04f, 0.90f, 0.79f };
        const float FINAL_DRIVE  = 3.07f;
        const float WHEEL_RADIUS = 0.33f;
        const float MAX_RPM      = 15000f;
        const float IDLE_RPM     = 5000f;

        public static float EngineTorque(float rpm, EngineMode mode, ComponentDamage dmg)
        {
            float n     = rpm / MAX_RPM;
            float curve = Mathf.Clamp01(Mathf.Sin(n * Mathf.PI) * 0.9f + n * 0.1f);

            float modeM = mode switch
            {
                EngineMode.Eco      => 0.82f,
                EngineMode.Push     => 1.10f,
                EngineMode.Overtake => 1.20f,
                _                   => 1.00f
            };
            return 650f * curve * modeM * DamageSystem.EnginePowerMultiplier(dmg);
        }

        public static float DriveForce(float rpm, int gear, EngineMode mode, ComponentDamage dmg)
        {
            if (gear <= 0 || gear >= GEAR_RATIOS.Length) return 0f;
            return EngineTorque(rpm, mode, dmg) * GEAR_RATIOS[gear] * FINAL_DRIVE / WHEEL_RADIUS;
        }

        public static float CalculateRPM(float speedMs, int gear)
        {
            if (gear <= 0 || gear >= GEAR_RATIOS.Length) return IDLE_RPM;
            float wheelRPM  = speedMs / WHEEL_RADIUS * (60f / (2f * Mathf.PI));
            return Mathf.Clamp(wheelRPM * GEAR_RATIOS[gear] * FINAL_DRIVE, IDLE_RPM, MAX_RPM);
        }

        public static int AutoShift(int gear, float rpm, float throttle)
        {
            if (rpm > 13500f && gear < 8 && throttle > 0.3f) return gear + 1;
            if (rpm <  6500f && gear > 1)                     return gear - 1;
            return gear;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FUEL SYSTEM
    // ═══════════════════════════════════════════════════════════════════════
    public static class FuelSystem
    {
        const float BASE_PER_LAP = 1.7f;

        public static float CalculateBurnRate(VehicleState car, float trackLenKm, float dt)
        {
            float modeM = car.engineMode switch
            {
                EngineMode.Eco      => 0.78f,
                EngineMode.Push     => 1.18f,
                EngineMode.Overtake => 1.30f,
                _                   => 1.00f
            };
            float perLap    = BASE_PER_LAP * (1f + car.throttle * 0.35f + car.speedMs * 0.0015f) * modeM;
            float lapTimeSec = trackLenKm * 1000f / Mathf.Max(car.speedMs, 1f);
            return perLap / lapTimeSec * dt;
        }

        public static float FuelWeightPenalty(float fuelLoad) => fuelLoad * 0.033f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LAP TIME CALCULATOR
    // ═══════════════════════════════════════════════════════════════════════
    public static class LapTimeCalculator
    {
        public static float Estimate(CircuitData circuit, DriverStats driver,
                                     CarSetup setup, TireState[] tires,
                                     float fuelLoad, WeatherData weather,
                                     float trackEvolution = 0f)
        {
            float driverMult = 1.06f - driver.OverallRating * 0.10f;

            if (weather.rainIntensity > 0.3f)
            {
                float wetSkill = driver.wetWeather / 100f;
                driverMult *= Mathf.Lerp(1.05f, 0.97f, wetSkill) * weather.rainIntensity
                            + (1f - weather.rainIntensity);
            }

            float avgGrip = 0f;
            foreach (var t in tires) avgGrip += TirePhysics.CalculateGrip(t, weather);
            avgGrip /= tires.Length;
            float tireMult = 2f - avgGrip;

            float wingFactor = (setup.frontWingAngle + setup.rearWingAngle) / 22f;
            float aeroMult   = 1f + (0.5f - wingFactor) * 0.04f;

            float evolutionBonus = -trackEvolution * circuit.trackEvolutionRate * 0.005f;

            return circuit.baseLapTimeSeconds
                 * driverMult * tireMult * aeroMult
                 * (1f / Mathf.Max(weather.GripMultiplier, 0.1f) * 0.25f + 0.75f)
                 + FuelSystem.FuelWeightPenalty(fuelLoad)
                 + evolutionBonus;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DAMAGE SYSTEM
    // ═══════════════════════════════════════════════════════════════════════
    public static class DamageSystem
    {
        public static void ApplyCollisionDamage(ComponentDamage dmg,
                                                float impactForceG, bool isFrontal)
        {
            if (impactForceG < 5f) return;

            if (isFrontal)
            {
                dmg.frontWing  += impactForceG * 0.9f;
                dmg.suspension += impactForceG * 0.25f;
                dmg.bargeboard += impactForceG * 0.20f;
            }
            else
            {
                dmg.rearWing   += impactForceG * 0.6f;
                dmg.suspension += impactForceG * 0.45f;
                dmg.floor      += impactForceG * 0.30f;
            }

            dmg.brakes += impactForceG * 0.08f;
            dmg.engine += impactForceG * 0.04f;

            if (impactForceG > 40f && Random.value < 0.30f) dmg.hasPuncture = true;

            Clamp(dmg);
        }

        static void Clamp(ComponentDamage d)
        {
            d.frontWing  = Mathf.Clamp(d.frontWing,  0f, 100f);
            d.rearWing   = Mathf.Clamp(d.rearWing,   0f, 100f);
            d.suspension = Mathf.Clamp(d.suspension, 0f, 100f);
            d.engine     = Mathf.Clamp(d.engine,     0f, 100f);
            d.brakes     = Mathf.Clamp(d.brakes,     0f, 100f);
            d.floor      = Mathf.Clamp(d.floor,      0f, 100f);
            d.bargeboard = Mathf.Clamp(d.bargeboard, 0f, 100f);
        }

        public static float FrontWingDownforceMultiplier(ComponentDamage d) =>
            1f - d.frontWing / 100f * 0.25f;

        public static float BrakeEffectivenessMultiplier(ComponentDamage d) =>
            1f - d.brakes / 100f * 0.45f;

        public static float EnginePowerMultiplier(ComponentDamage d) =>
            1f - d.engine / 100f * 0.55f;

        public static float SuspensionHandlingMultiplier(ComponentDamage d) =>
            1f - d.suspension / 100f * 0.60f;

        public static void RepairInPit(ComponentDamage d, float budget)
        {
            if (d.frontWing  > 20f) { d.frontWing  = Mathf.Max(0f, d.frontWing  - budget); return; }
            if (d.suspension > 15f) { d.suspension = Mathf.Max(0f, d.suspension - budget); return; }
            if (d.brakes     > 20f) { d.brakes     = Mathf.Max(0f, d.brakes     - budget); return; }
            d.hasPuncture = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  WEATHER SYSTEM
    // ═══════════════════════════════════════════════════════════════════════
    public static class WeatherSystem
    {
        public static void TickWeather(WeatherData w, float dt)
        {
            if (w.condition <= WeatherCondition.Cloudy)
                w.trackWetness = Mathf.MoveTowards(w.trackWetness, 0f, 0.005f * dt);
            else
                w.trackWetness = Mathf.MoveTowards(w.trackWetness, w.rainIntensity, 0.003f * dt);
        }

        public static WeatherCondition TransitionWeather(WeatherCondition cur, float prob)
        {
            if (Random.value > prob) return cur;
            return cur switch
            {
                WeatherCondition.Dry       => WeatherCondition.Cloudy,
                WeatherCondition.Cloudy    => Random.value > 0.5f ? WeatherCondition.LightRain : WeatherCondition.Dry,
                WeatherCondition.LightRain => Random.value > 0.4f ? WeatherCondition.HeavyRain : WeatherCondition.Cloudy,
                WeatherCondition.HeavyRain => Random.value > 0.5f ? WeatherCondition.Storm : WeatherCondition.LightRain,
                WeatherCondition.Storm     => WeatherCondition.HeavyRain,
                _ => cur
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PHYSICS INTEGRATOR  –  FixedUpdate entry point
    // ═══════════════════════════════════════════════════════════════════════
    public class PhysicsIntegrator : MonoBehaviour
    {
        [Header("Config")]
        public CarSetup    setup   = new();
        public WeatherData weather = new();
        public CircuitData circuit;

        VehicleState _state = new();
        public VehicleState State => _state;

        float _shiftCooldown;

        void Awake()
        {
            _state.ersSoC    = 80f;      // start at 80 % charge
            _state.ersMode   = ERSMode.Balanced;
            _state.fuelLoad  = 110f;
            _state.gear      = 1;
            _state.engineMode = EngineMode.Normal;

            for (int i = 0; i < 4; i++)
            {
                _state.tires[i] = new TireState
                {
                    compound      = TireCompound.Medium,
                    wearPercent   = 0f,
                    coldPressurePSI = 23f
                };
                _state.tires[i].InitTemps(weather.ambientTemp);
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // 1 ── Tires (thermal, pressure, graining, blistering, wear)
            foreach (var tire in _state.tires)
                TirePhysics.UpdateTire(tire, _state, weather, dt);

            float avgGrip = 0f;
            foreach (var t in _state.tires)
                avgGrip += TirePhysics.CalculateGrip(t, weather);
            avgGrip /= 4f;

            // 2 ── RPM & auto-shift
            _state.rpm = PowertrainPhysics.CalculateRPM(_state.speedMs, _state.gear);
            _shiftCooldown -= dt;
            if (_shiftCooldown <= 0f)
            {
                int ng = PowertrainPhysics.AutoShift(_state.gear, _state.rpm, _state.throttle);
                if (ng != _state.gear) { _state.gear = ng; _shiftCooldown = 0.08f; }
            }

            // 3 ── ERS (SoC model)
            _state.ersSoC = ERSSystem.TickBattery(_state, dt);
            float ersForce = ERSSystem.DeployForce(_state);

            // 4 ── Fuel
            if (circuit != null)
                _state.fuelLoad = Mathf.Max(0f,
                    _state.fuelLoad - FuelSystem.CalculateBurnRate(_state, circuit.trackLengthKm, dt));

            // 5 ── Aero + ride-height effects
            float carMass = 798f + _state.fuelLoad * 0.745f;
            float driveForce = PowertrainPhysics.DriveForce(_state.rpm, _state.gear,
                                   _state.engineMode, _state.damage);
            float brakeForce = _state.brake * 18000f
                             * DamageSystem.BrakeEffectivenessMultiplier(_state.damage);

            float predictedDrag = Aerodynamics.CalculateDrag(_state.speedMs, setup,
                                   _state.drsActive, _state.damage, _state);
            float previewForce = _state.throttle * (driveForce + ersForce) - brakeForce - predictedDrag;
            float previewAccel = previewForce / carMass;
            float rideHeight = CarPhysics.CalculateEffectiveRideHeight(_state, setup, previewAccel);
            bool bottomedOut = Aerodynamics.IsBottomedOut(rideHeight);

            _state.downforce = Aerodynamics.CalculateDownforce(_state.speedMs, setup,
                _state.damage, rideHeight, bottomedOut);
            _state.dragForce = predictedDrag;

            // 6 ── Longitudinal dynamics + weight transfer
            _state.gLongitudinal = previewAccel / 9.81f;
            _state.gLateral      = _state.speedMs * Mathf.Abs(_state.steering) * 0.15f / 9.81f;
            CarPhysics.ApplyWeightTransfer(_state, previewAccel, _state.gLateral);
            CarPhysics.UpdateTireGripFromLoad(_state, weather, bottomedOut);

            float avgGrip2 = CarPhysics.CalculateAverageGrip(_state, weather);
            float traction   = Mathf.Clamp01(avgGrip2 * DamageSystem.SuspensionHandlingMultiplier(_state.damage));

            float propulsive = _state.throttle * (driveForce + ersForce) * traction;
            float effectiveBrake = brakeForce * traction;

            float netForce = propulsive - effectiveBrake - _state.dragForce;
            float accel    = netForce / carMass;

            // 7 ── G-force telemetry
            _state.gLongitudinal = accel / 9.81f;
            _state.gLateral      = _state.speedMs * Mathf.Abs(_state.steering) * 0.15f / 9.81f;

            // 8 ── Integrate speed & position
            _state.speedMs = Mathf.Max(0f, _state.speedMs + accel * dt);
            _state.speed   = _state.speedMs * 3.6f;
            _state.velocity = transform.forward * _state.speedMs;

            if (_state.speedMs > 0.5f)
            {
                float steerAngle = _state.steering * 25f * (traction * 0.7f + 0.3f);
                float turnRadius = Mathf.Max(5f, _state.speedMs /
                    Mathf.Tan(steerAngle * Mathf.Deg2Rad + 0.001f));
                transform.Rotate(Vector3.up, _state.speedMs / turnRadius * dt * Mathf.Rad2Deg);
            }

            transform.position += transform.forward * _state.speedMs * dt;
            _state.position     = transform.position;
            _state.rotation     = transform.rotation;

            if (_state.damage.IsDNF) _state.throttle = 0f;
        }

        // ── Public control API ────────────────────────────────────────────
        public void SetERSMode(ERSMode mode)           => _state.ersMode = mode;
        public void ActivateOvertake()                 => ERSSystem.ActivateOvertake(_state);
        public void SetWakeData(float gapSec, VehicleState carAhead)
        {
            AerodynamicWakeSystem.ApplyWake(_state, carAhead, gapSec);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  WAKE MANAGER  –  MonoBehaviour that pairs all cars each frame
    // ═══════════════════════════════════════════════════════════════════════
    public class WakeManager : MonoBehaviour
    {
        [Tooltip("All PhysicsIntegrators in the race, ordered by position (1st = index 0).")]
        public List<PhysicsIntegrator> raceCars = new();

        // Approximate seconds gap from distance gap
        const float AVG_SPEED_MS = 55f;   // ~200 km/h average

        void FixedUpdate()
        {
            if (raceCars == null || raceCars.Count < 2) return;

            for (int i = 1; i < raceCars.Count; i++)
            {
                var behind = raceCars[i];
                var ahead  = raceCars[i - 1];
                if (behind == null || ahead == null) continue;

                float distM  = Vector3.Distance(behind.State.position, ahead.State.position);
                float gapSec = distM / Mathf.Max(AVG_SPEED_MS, ahead.State.speedMs);

                behind.SetWakeData(gapSec, ahead.State);
            }

            // Car in P1 is never in dirty air
            if (raceCars[0] != null)
                AerodynamicWakeSystem.ApplyWake(raceCars[0].State, null, 999f);
        }
    }
}
