using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Physics
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAR PHYSICS  –  2026 Regulations Extension
    //  Adds: ActiveAeroSystem, ERSSystem2026, OvertakeMode,
    //        TyreStrategy2026, Madrid circuit behaviour.
    //  All original static methods retained unchanged below the new code.
    // ═══════════════════════════════════════════════════════════════════════

    public static class CarPhysics
    {
        // ── Original constants ────────────────────────────────────────────
        const float GRAVITY            = 9.81f;
        const float FUEL_DENSITY       = 0.745f;
        const float BASE_CAR_MASS      = 748f;
        const float WHEELBASE          = 3.25f;
        const float TRACK_WIDTH        = 1.78f;
        const float CG_HEIGHT          = 0.33f;
        const float FRONT_WEIGHT_RATIO = 0.45f;
        const float REAR_WEIGHT_RATIO  = 0.55f;
        const float MIN_RIDE_HEIGHT    = 0.032f;
        const float MAX_RIDE_HEIGHT    = 0.095f;
        const float BOTTOM_OUT_HEIGHT  = 0.015f;
        const float MAX_FUEL_LOAD      = 110f;

        // ── 2026 constants ────────────────────────────────────────────────
        const float ERS_MAX_DEPLOY_KJ      = 4000f;   // kJ per lap (2026 cap raised)
        const float ERS_MAX_HARVEST_KJ     = 2000f;   // kJ per lap from MGU-K + MGU-H
        const float ERS_OVERTAKE_BOOST_KW  = 350f;    // kW extra in OvertakeMode
        const float ACTIVE_AERO_MIN_ANGLE  = -2.5f;   // degrees (low drag)
        const float ACTIVE_AERO_MAX_ANGLE  = 18.0f;   // degrees (max downforce)
        const float AERO_TRANSITION_RATE   = 12.0f;   // degrees/second

        // ═════════════════════════════════════════════════════════════════
        //  ACTIVE AERO SYSTEM (2026)
        //  Replaces fixed DRS with a continuously variable rear wing.
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ticks the active aero each frame. Targets are set by racing mode.
        /// </summary>
        public static void TickActiveAero(ActiveAeroState aero, VehicleState vehicle,
                                          ActiveAeroMode mode, float dt)
        {
            float targetAngle = ResolveTargetAngle(mode, vehicle);

            // Smooth transition toward target
            float delta = targetAngle - aero.currentAngleDeg;
            float step  = AERO_TRANSITION_RATE * dt;
            aero.currentAngleDeg = Mathf.MoveTowards(aero.currentAngleDeg, targetAngle, step);

            // Override: active aero cannot open in wet conditions (safety rule)
            if (vehicle.surfaceIsWet)
                aero.currentAngleDeg = Mathf.Max(aero.currentAngleDeg, 8.0f);

            aero.isTransitioning = Mathf.Abs(delta) > 0.5f;
        }

        static float ResolveTargetAngle(ActiveAeroMode mode, VehicleState vehicle)
        {
            return mode switch
            {
                ActiveAeroMode.LowDrag       => ACTIVE_AERO_MIN_ANGLE,   // straights
                ActiveAeroMode.MaxDownforce  => ACTIVE_AERO_MAX_ANGLE,   // slow corners
                ActiveAeroMode.Balanced      => 6.0f,
                ActiveAeroMode.Auto          => AutoAeroAngle(vehicle),
                _                            => 6.0f
            };
        }

        /// <summary>
        /// Auto mode: opens on straights, closes in corners.
        /// Driven by lateral G and speed.
        /// </summary>
        static float AutoAeroAngle(VehicleState vehicle)
        {
            float cornerDemand = Mathf.Abs(vehicle.gLateral) / 3.0f;      // 0-1
            float speedFactor  = Mathf.InverseLerp(200f, 340f, vehicle.speed); // fast = open

            float t = Mathf.Clamp01(cornerDemand - speedFactor * 0.4f);
            return Mathf.Lerp(ACTIVE_AERO_MIN_ANGLE, ACTIVE_AERO_MAX_ANGLE, t);
        }

        /// <summary>
        /// Returns downforce multiplier from active aero angle (0° = minimum, 18° = maximum).
        /// </summary>
        public static float ActiveAeroDownforceMultiplier(float angleDeg)
        {
            float t = Mathf.InverseLerp(ACTIVE_AERO_MIN_ANGLE, ACTIVE_AERO_MAX_ANGLE, angleDeg);
            return Mathf.Lerp(0.72f, 1.35f, t);
        }

        /// <summary>
        /// Returns drag multiplier from active aero angle.
        /// </summary>
        public static float ActiveAeroDragMultiplier(float angleDeg)
        {
            float t = Mathf.InverseLerp(ACTIVE_AERO_MIN_ANGLE, ACTIVE_AERO_MAX_ANGLE, angleDeg);
            return Mathf.Lerp(0.78f, 1.20f, t);
        }

        // ═════════════════════════════════════════════════════════════════
        //  ERS SYSTEM 2026
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ticks ERS per frame: harvests energy, deploys on demand,
        /// enforces lap limits, applies OvertakeMode boost.
        /// </summary>
        public static void TickERS(ERSState2026 ers, VehicleState vehicle,
                                   DeploymentMode deployMode, float dt)
        {
            // ── Harvesting ────────────────────────────────────────────────
            float harvestKw = 0f;

            // MGU-K: braking energy
            if (vehicle.brake > 0.05f)
                harvestKw += 120f * vehicle.brake;

            // MGU-H: exhaust heat (simplified — proportional to throttle + speed)
            harvestKw += Mathf.Lerp(0f, 80f, vehicle.throttle) *
                         Mathf.InverseLerp(100f, 300f, vehicle.speed);

            float harvestedKJ = harvestKw * dt / 1000f;
            ers.storedEnergyKJ = Mathf.Min(ers.storedEnergyKJ + harvestedKJ,
                                           ers.maxStorageKJ);
            ers.lapHarvestKJ  += harvestedKJ;

            // ── Deployment ────────────────────────────────────────────────
            float deployKw = ResolveDeploymentPower(deployMode, ers, vehicle);

            if (deployKw > 0f && ers.storedEnergyKJ > 0f)
            {
                float usedKJ = deployKw * dt / 1000f;
                usedKJ = Mathf.Min(usedKJ, ers.storedEnergyKJ);

                ers.storedEnergyKJ -= usedKJ;
                ers.lapDeployKJ    += usedKJ;
                ers.currentDeployKw = deployKw;

                // Apply thrust to vehicle
                float extraForce = deployKw * 1000f / Mathf.Max(vehicle.speedMs, 5f);
                vehicle.ersBoostForce = extraForce;
            }
            else
            {
                ers.currentDeployKw = 0f;
                vehicle.ersBoostForce = 0f;
            }

            // ── Lap limit enforcement ─────────────────────────────────────
            if (ers.lapDeployKJ >= ERS_MAX_DEPLOY_KJ)
            {
                ers.deployLimitReached = true;
                vehicle.ersBoostForce  = 0f;
            }

            // ── OvertakeMode ──────────────────────────────────────────────
            if (ers.overtakeModeActive)
                TickOvertakeMode(ers, vehicle, dt);
        }

        static float ResolveDeploymentPower(DeploymentMode mode, ERSState2026 ers,
                                            VehicleState vehicle)
        {
            if (ers.deployLimitReached) return 0f;

            float batteryPct = ers.storedEnergyKJ / ers.maxStorageKJ;

            return mode switch
            {
                DeploymentMode.Hotlap       => 350f,               // max all lap
                DeploymentMode.Race_High    => 280f * batteryPct,  // scales with battery
                DeploymentMode.Race_Medium  => 180f * batteryPct,
                DeploymentMode.Race_Low     => 80f,                // minimal, conserve
                DeploymentMode.Harvest      => 0f,                 // no deploy
                DeploymentMode.Overtake     => ERS_OVERTAKE_BOOST_KW,
                _                           => 120f
            };
        }

        // ═════════════════════════════════════════════════════════════════
        //  OVERTAKE MODE
        // ═════════════════════════════════════════════════════════════════

        public static bool TryActivateOvertakeMode(ERSState2026 ers, VehicleState vehicle,
                                                   float gapAheadSeconds)
        {
            // Cannot activate if: not enough energy, already active,
            // wet conditions, or gap too large
            if (ers.overtakeModeActive)    return false;
            if (ers.overtakeCooldown > 0f) return false;
            if (gapAheadSeconds > 2.0f)    return false;   // only within 2 s
            if (vehicle.surfaceIsWet)       return false;   // wet = no overtake mode
            if (ers.storedEnergyKJ < 300f) return false;   // needs min 300 kJ

            ers.overtakeModeActive   = true;
            ers.overtakeTimeLeft     = 10.0f;   // 10-second burst
            ers.overtakeCooldown     = 25.0f;   // 25-second cooldown after
            return true;
        }

        static void TickOvertakeMode(ERSState2026 ers, VehicleState vehicle, float dt)
        {
            ers.overtakeTimeLeft -= dt;

            float boost = ERS_OVERTAKE_BOOST_KW * 1000f / Mathf.Max(vehicle.speedMs, 5f);
            vehicle.ersBoostForce += boost * 0.5f;  // additional on top of base deploy

            // Visual flag for VFX system
            vehicle.overtakeModeVFX = true;

            if (ers.overtakeTimeLeft <= 0f)
            {
                ers.overtakeModeActive  = false;
                vehicle.overtakeModeVFX = false;
            }
        }

        public static void TickOvertakeCooldown(ERSState2026 ers, float dt)
        {
            if (ers.overtakeCooldown > 0f)
                ers.overtakeCooldown = Mathf.Max(0f, ers.overtakeCooldown - dt);
        }

        // ═════════════════════════════════════════════════════════════════
        //  TYRE STRATEGY 2026
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tick tyre degradation per frame with 2026 compound profiles.
        /// 2026 rules: mandatory use of at least two different compounds.
        /// </summary>
        public static void TickTyreDeg2026(TyreState2026 tyre, VehicleState vehicle,
                                           WeatherData weather, float dt)
        {
            if (tyre.currentCompound == TyreCompound2026.Intermediate ||
                tyre.currentCompound == TyreCompound2026.Wet)
            {
                TickWetTyre(tyre, vehicle, weather, dt);
                return;
            }

            float baseDeg   = GetBaseDegRate(tyre.currentCompound);
            float thermalMod = CalculateThermalDeg(tyre, vehicle);
            float trackMod   = weather.trackTemp > 45f ? 1.4f : 1.0f;  // hot track degrades faster
            float loadMod    = Mathf.Lerp(0.8f, 1.5f,
                               Mathf.Abs(vehicle.gLateral) / 3.0f);    // cornering load

            float degradationRate = baseDeg * thermalMod * trackMod * loadMod;

            tyre.lifePercent = Mathf.Max(0f, tyre.lifePercent - degradationRate * dt);
            tyre.age        += dt;
            tyre.overheating = tyre.tyreTemp > GetTempWindow(tyre.currentCompound).y;

            // Cliff behaviour: below 15% life, grip drops sharply
            tyre.inCliff = tyre.lifePercent < 15f;
        }

        static void TickWetTyre(TyreState2026 tyre, VehicleState vehicle,
                                 WeatherData weather, float dt)
        {
            // Wet tyres degrade rapidly in dry conditions (overheating)
            float dryTrackPenalty = (1f - weather.wetness) * 3.0f;
            tyre.lifePercent = Mathf.Max(0f, tyre.lifePercent - (0.002f + dryTrackPenalty) * dt);
            tyre.tyreTemp   += (1f - weather.wetness) * 15f * dt;
        }

        static float GetBaseDegRate(TyreCompound2026 compound) =>
            compound switch
            {
                TyreCompound2026.Soft     => 0.035f,   // %/second
                TyreCompound2026.Medium   => 0.020f,
                TyreCompound2026.Hard     => 0.012f,
                _                         => 0.020f
            };

        static float CalculateThermalDeg(TyreState2026 tyre, VehicleState vehicle)
        {
            Vector2 window = GetTempWindow(tyre.currentCompound);
            float targetTemp = (window.x + window.y) * 0.5f;

            // Heat tyres from speed + lateral load
            float heatInput = vehicle.speed * 0.012f +
                              Mathf.Abs(vehicle.gLateral) * 8f;
            tyre.tyreTemp = Mathf.Lerp(tyre.tyreTemp,
                                        targetTemp + heatInput, 0.02f);

            // Over-temp or under-temp multiplier
            if (tyre.tyreTemp > window.y) return Mathf.Lerp(1f, 3f, (tyre.tyreTemp - window.y) / 30f);
            if (tyre.tyreTemp < window.x) return Mathf.Lerp(1.5f, 1f, (tyre.tyreTemp - window.x) / 20f + 1f);
            return 1f;
        }

        /// <summary>Returns (minTemp, maxTemp) optimal window for a compound.</summary>
        public static Vector2 GetTempWindow(TyreCompound2026 compound) =>
            compound switch
            {
                TyreCompound2026.Soft         => new Vector2(90f,  115f),
                TyreCompound2026.Medium       => new Vector2(85f,  110f),
                TyreCompound2026.Hard         => new Vector2(80f,  105f),
                TyreCompound2026.Intermediate => new Vector2(30f,   65f),
                TyreCompound2026.Wet          => new Vector2(15f,   45f),
                _                             => new Vector2(85f,  110f)
            };

        /// <summary>
        /// Grip scalar from tyre life and temperature.
        /// </summary>
        public static float TyreGripScalar2026(TyreState2026 tyre)
        {
            if (tyre.inCliff)
            {
                // Cliff: rapid non-linear grip loss
                float cliffFactor = Mathf.InverseLerp(15f, 0f, tyre.lifePercent);
                return Mathf.Lerp(0.85f, 0.55f, cliffFactor);
            }

            float lifeGrip = Mathf.Lerp(0.88f, 1.0f,
                             Mathf.InverseLerp(0f, 100f, tyre.lifePercent));

            Vector2 window = GetTempWindow(tyre.currentCompound);
            float tempRatio = Mathf.InverseLerp(window.x, window.y, tyre.tyreTemp);
            float tempGrip  = Mathf.Sin(tempRatio * Mathf.PI) * 0.12f + 0.88f; // peak in mid-window

            return Mathf.Clamp(lifeGrip * tempGrip, 0.5f, 1.05f);
        }

        // ═════════════════════════════════════════════════════════════════
        //  MADRID CIRCUIT BEHAVIOUR
        // ═════════════════════════════════════════════════════════════════
        // IFEMA Madrid (fictional MADRING layout used in game)
        // Key traits: long back straight, abrasive surface, high ambient temp,
        //             technical sector 2, three DRS zones.

        static readonly MadridCircuitProfile MADRID = new MadridCircuitProfile
        {
            trackAbrasion    = 1.45f,   // 45% higher tyre wear vs baseline
            ambientTempBase  = 32f,     // °C
            trackTempBase    = 52f,
            altitudeM        = 667f,    // Madrid altitude — affects aero
            airDensityFactor = 0.921f,  // lower than sea level
            mainStraightMs   = 95f,     // top speed zone
            drsZoneCount     = 3,
            sector2TechnicalRating = 0.88f   // how technical (0 = fast, 1 = technical)
        };

        /// <summary>
        /// Returns modified tyre degradation multiplier for Madrid.
        /// </summary>
        public static float MadridTyreDegMultiplier(TyreCompound2026 compound)
        {
            float base_ = MADRID.trackAbrasion;
            // Softs suffer extra on abrasive Madrid tarmac
            return compound switch
            {
                TyreCompound2026.Soft   => base_ * 1.20f,
                TyreCompound2026.Medium => base_ * 1.10f,
                TyreCompound2026.Hard   => base_ * 1.00f,
                _                      => base_
            };
        }

        /// <summary>
        /// Returns downforce multiplier adjusted for Madrid altitude.
        /// Lower air density = less downforce.
        /// </summary>
        public static float MadridAeroScalar() => MADRID.airDensityFactor;

        /// <summary>
        /// Returns recommended strategy for Madrid based on driver skill / pace.
        /// </summary>
        public static MadridStrategy RecommendMadridStrategy(float driverTyreSkill,
                                                              float carPace,
                                                              int totalLaps)
        {
            // Madrid typically favours 2-stop due to high deg
            bool canOneStop = driverTyreSkill >= 75f && carPace >= 70f;

            if (canOneStop)
            {
                return new MadridStrategy
                {
                    stops  = 1,
                    pit1Lap = Mathf.RoundToInt(totalLaps * 0.52f),
                    stint1  = TyreCompound2026.Medium,
                    stint2  = TyreCompound2026.Hard,
                    note    = "One-stop: Medium → Hard. Requires strong tyre management."
                };
            }

            return new MadridStrategy
            {
                stops   = 2,
                pit1Lap = Mathf.RoundToInt(totalLaps * 0.33f),
                pit2Lap = Mathf.RoundToInt(totalLaps * 0.65f),
                stint1  = TyreCompound2026.Soft,
                stint2  = TyreCompound2026.Medium,
                stint3  = TyreCompound2026.Hard,
                note    = "Two-stop: Soft → Medium → Hard. Optimal for mid-field runners."
            };
        }

        // ═════════════════════════════════════════════════════════════════
        //  EXTENDED SimulateVehicleDynamics  (2026-aware)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Full 2026 dynamics tick — includes active aero, ERS boost, tyre deg.
        /// Call this instead of the original SimulateVehicleDynamics when
        /// using 2026 regulation cars.
        /// </summary>
        public static void SimulateVehicleDynamics2026(
            VehicleState      vehicle,
            CarSetup          setup,
            WeatherData       weather,
            ActiveAeroState   activeAero,
            ERSState2026      ers,
            TyreState2026[]   tyres,        // 4 tyres
            DeploymentMode    deployMode,
            ActiveAeroMode    aeroMode,
            bool              isMadrid,
            float             dt)
        {
            // ── Active aero ───────────────────────────────────────────────
            TickActiveAero(activeAero, vehicle, aeroMode, dt);
            float aeroDownMult = ActiveAeroDownforceMultiplier(activeAero.currentAngleDeg);
            float aeroDragMult = ActiveAeroDragMultiplier(activeAero.currentAngleDeg);

            // ── Madrid altitude aero penalty ──────────────────────────────
            if (isMadrid) { aeroDownMult *= MadridAeroScalar(); aeroDragMult *= MadridAeroScalar(); }

            // ── ERS ───────────────────────────────────────────────────────
            TickERS(ers, vehicle, deployMode, dt);
            TickOvertakeCooldown(ers, dt);

            // ── Tyre deg (all four) ───────────────────────────────────────
            float tyreDegMult = isMadrid ? MadridTyreDegMultiplier(tyres[0].currentCompound) : 1f;
            for (int i = 0; i < 4; i++)
            {
                TickTyreDeg2026(tyres[i], vehicle, weather, dt * tyreDegMult);
                vehicle.tires[i].surfaceGrip = TyreGripScalar2026(tyres[i]);
            }

            // ── Mass & forces ─────────────────────────────────────────────
            float mass = TotalMass(vehicle);

            float engineForce = PowertrainPhysics.DriveForce(
                vehicle.rpm, vehicle.gear, vehicle.engineMode, vehicle.damage)
                * vehicle.throttle;

            engineForce += vehicle.ersBoostForce;   // ERS contribution

            float brakeForce = vehicle.brake * 18000f *
                               DamageSystem.BrakeEffectivenessMultiplier(vehicle.damage);

            float drag = Aerodynamics.CalculateDrag(vehicle.speedMs, setup,
                         vehicle.drsActive, vehicle.damage) * aeroDragMult;
            vehicle.dragForce = drag;

            float netForce = engineForce - brakeForce - drag;
            float longAccel = netForce / mass;

            vehicle.gLongitudinal = longAccel / GRAVITY;
            vehicle.gLateral      = Mathf.Clamp(
                vehicle.steering * vehicle.speedMs * 0.18f, -3.0f, 3.0f);

            ApplyWeightTransfer(vehicle, longAccel, vehicle.gLateral);

            float rideHeight  = CalculateEffectiveRideHeight(vehicle, setup, longAccel);
            bool  bottomedOut = Aerodynamics.IsBottomedOut(rideHeight);

            float baseDownforce = Aerodynamics.CalculateDownforce(
                vehicle.speedMs, setup, vehicle.damage, rideHeight, bottomedOut);
            vehicle.downforce = baseDownforce * aeroDownMult;

            // ── Traction ──────────────────────────────────────────────────
            float avgGrip  = CalculateAverageGrip(vehicle, weather);
            float traction = Mathf.Clamp01(avgGrip *
                             DamageSystem.SuspensionHandlingMultiplier(vehicle.damage));

            // ── Integrate speed ───────────────────────────────────────────
            float effectiveAccel = netForce / mass * traction;
            vehicle.speedMs = Mathf.Max(0f, vehicle.speedMs + effectiveAccel * dt);
            vehicle.speed   = vehicle.speedMs * 3.6f;
        }

        // ═════════════════════════════════════════════════════════════════
        //  ORIGINAL STATIC METHODS  (unchanged)
        // ═════════════════════════════════════════════════════════════════

        public static float TotalMass(VehicleState state) =>
            BASE_CAR_MASS + state.fuelLoad * FUEL_DENSITY;

        public static float FuelMass(VehicleState state)   => state.fuelLoad * FUEL_DENSITY;
        public static float FuelWeight(VehicleState state) => FuelMass(state) * GRAVITY;

        public static void BurnFuel(VehicleState state, float burnRateLitersPerSecond, float dt)
        {
            state.fuelLoad = Mathf.Max(0f, state.fuelLoad - burnRateLitersPerSecond * dt);
        }

        public static float CalculateEffectiveRideHeight(VehicleState state, CarSetup setup, float longitudinalAccel)
        {
            float rideFront  = Mathf.Lerp(MIN_RIDE_HEIGHT, MAX_RIDE_HEIGHT, (setup.rideHeightFront - 1f) / 10f);
            float rideRear   = Mathf.Lerp(MIN_RIDE_HEIGHT, MAX_RIDE_HEIGHT, (setup.rideHeightRear  - 1f) / 10f);
            float frontSpring = Mathf.Lerp(145000f, 225000f, (setup.suspensionStiffnessFront - 1f) / 10f);
            float rearSpring  = Mathf.Lerp(138000f, 215000f, (setup.suspensionStiffnessRear  - 1f) / 10f);

            float mass = TotalMass(state);
            float tf   = mass * longitudinalAccel * CG_HEIGHT / WHEELBASE;

            float fc = Mathf.Clamp(tf  / frontSpring, -0.03f, 0.05f);
            float rc = Mathf.Clamp(-tf / rearSpring,  -0.03f, 0.05f);

            return Mathf.Max(BOTTOM_OUT_HEIGHT, Mathf.Min(rideFront - fc, rideRear - rc));
        }

        public static float AdjustDownforceForRideHeight(float baseDownforce, float rideHeight)
        {
            if (rideHeight <= BOTTOM_OUT_HEIGHT) return baseDownforce * 0.30f;
            float heightFactor = Mathf.InverseLerp(MAX_RIDE_HEIGHT, MIN_RIDE_HEIGHT, rideHeight);
            float groundEffect = Mathf.Lerp(0.86f, 1.28f, heightFactor);
            return baseDownforce * groundEffect;
        }

        public static void ApplyWeightTransfer(VehicleState state,
                                               float longitudinalAccel, float lateralAccel)
        {
            float mass      = TotalMass(state);
            float sFront    = mass * GRAVITY * FRONT_WEIGHT_RATIO;
            float sRear     = mass * GRAVITY * REAR_WEIGHT_RATIO;
            float longTrans = mass * longitudinalAccel * CG_HEIGHT / WHEELBASE;
            float latTrans  = mass * lateralAccel * CG_HEIGHT / TRACK_WIDTH;

            state.tires[0].load = Mathf.Max(0f, sFront - longTrans + latTrans * 0.5f);
            state.tires[1].load = Mathf.Max(0f, sFront - longTrans - latTrans * 0.5f);
            state.tires[2].load = Mathf.Max(0f, sRear  + longTrans + latTrans * 0.5f);
            state.tires[3].load = Mathf.Max(0f, sRear  + longTrans - latTrans * 0.5f);
        }

        public static void UpdateTireGripFromLoad(VehicleState state, WeatherData weather, bool bottomedOut)
        {
            float baseline    = TotalMass(state) * GRAVITY * 0.25f;
            float fuelPenalty = Mathf.Clamp01(1f - state.fuelLoad / MAX_FUEL_LOAD) * state.throttle * 0.18f;

            for (int i = 0; i < 4; i++)
            {
                TireState tire     = state.tires[i];
                float loadRatio    = tire.load / Mathf.Max(100f, baseline);
                float loadGrip     = Mathf.Lerp(0.90f, 1.08f, Mathf.Clamp01(loadRatio * 0.95f));
                float lateralPen   = Mathf.Abs(state.gLateral) * 0.04f;
                tire.surfaceGrip   = Mathf.Clamp01(loadGrip - fuelPenalty - lateralPen);
                if (bottomedOut) tire.surfaceGrip *= 0.78f;
            }
        }

        public static float CalculateAverageGrip(VehicleState state, WeatherData weather)
        {
            float total = 0f;
            for (int i = 0; i < 4; i++) total += TirePhysics.CalculateGrip(state.tires[i], weather);
            return total * 0.25f;
        }

        // Legacy original method — still works for non-2026 cars
        public static void SimulateVehicleDynamics(VehicleState state, CarSetup setup,
            WeatherData weather, float dt)
        {
            float mass        = TotalMass(state);
            float engineForce = PowertrainPhysics.DriveForce(state.rpm, state.gear,
                                    state.engineMode, state.damage) * state.throttle;
            float brakeForce  = state.brake * 18000f *
                                DamageSystem.BrakeEffectivenessMultiplier(state.damage);

            state.dragForce = Aerodynamics.CalculateDrag(state.speedMs, setup,
                                  state.drsActive, state.damage);

            float netForce    = engineForce - brakeForce - state.dragForce;
            float longAccel   = netForce / mass;

            state.gLongitudinal = longAccel / GRAVITY;
            state.gLateral      = Mathf.Clamp(state.steering * state.speedMs * 0.18f, -3.0f, 3.0f);

            ApplyWeightTransfer(state, longAccel, state.gLateral);

            float rideHeight  = CalculateEffectiveRideHeight(state, setup, longAccel);
            bool  bottomedOut = Aerodynamics.IsBottomedOut(rideHeight);

            state.downforce = Aerodynamics.CalculateDownforce(state.speedMs, setup,
                                  state.damage, rideHeight, bottomedOut);

            UpdateTireGripFromLoad(state, weather, bottomedOut);

            float avgGrip  = CalculateAverageGrip(state, weather);
            float traction = Mathf.Clamp01(avgGrip *
                             DamageSystem.SuspensionHandlingMultiplier(state.damage));

            float effectiveAccel = netForce / mass * traction;
            state.speedMs = Mathf.Max(0f, state.speedMs + effectiveAccel * dt);
            state.speed   = state.speedMs * 3.6f;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NEW STATE & DATA TYPES  (Compartment 4)
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class ActiveAeroState
    {
        public float currentAngleDeg = 6.0f;
        public bool  isTransitioning;
        public bool  lockedForWet;           // set by race director in rain
    }

    [System.Serializable]
    public class ERSState2026
    {
        public float storedEnergyKJ   = 2000f;  // starts at 50% of 4000 kJ max
        public float maxStorageKJ     = 4000f;
        public float lapDeployKJ      = 0f;
        public float lapHarvestKJ     = 0f;
        public float currentDeployKw  = 0f;
        public bool  deployLimitReached = false;
        public bool  overtakeModeActive = false;
        public float overtakeTimeLeft   = 0f;
        public float overtakeCooldown   = 0f;

        public float BatteryPercent => storedEnergyKJ / maxStorageKJ * 100f;

        /// <summary>Call at start of each new lap to reset lap counters.</summary>
        public void ResetLapCounters()
        {
            lapDeployKJ       = 0f;
            lapHarvestKJ      = 0f;
            deployLimitReached = false;
        }
    }

    [System.Serializable]
    public class TyreState2026
    {
        public TyreCompound2026 currentCompound = TyreCompound2026.Medium;
        public float lifePercent  = 100f;
        public float tyreTemp     = 90f;
        public float age          = 0f;    // seconds on tyre
        public bool  overheating  = false;
        public bool  inCliff      = false;

        public float GripScalar => CarPhysics.TyreGripScalar2026(this);
    }

    [System.Serializable]
    public class MadridCircuitProfile
    {
        public float trackAbrasion;
        public float ambientTempBase;
        public float trackTempBase;
        public float altitudeM;
        public float airDensityFactor;
        public float mainStraightMs;
        public int   drsZoneCount;
        public float sector2TechnicalRating;
    }

    [System.Serializable]
    public class MadridStrategy
    {
        public int              stops;
        public int              pit1Lap;
        public int              pit2Lap;
        public TyreCompound2026 stint1;
        public TyreCompound2026 stint2;
        public TyreCompound2026 stint3;
        public string           note;
    }

    // ── New enums ─────────────────────────────────────────────────────────
    public enum ActiveAeroMode   { Auto, LowDrag, Balanced, MaxDownforce }
    public enum TyreCompound2026 { Soft, Medium, Hard, Intermediate, Wet }

    // DeploymentMode already defined in DriverCareerData.cs (Compartment 1)
    // Re-declared here as partial fallback if not imported:
    // public enum DeploymentMode { Hotlap, Race_High, Race_Medium, Race_Low, Harvest, Overtake }
}