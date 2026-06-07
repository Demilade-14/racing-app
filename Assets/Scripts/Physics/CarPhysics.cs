using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Physics
{
    public static class CarPhysics
    {
        const float GRAVITY = 9.81f;
        const float FUEL_DENSITY = 0.745f;
        const float BASE_CAR_MASS = 748f;          // kg dry car mass approximation
        const float WHEELBASE = 3.25f;             // meters
        const float TRACK_WIDTH = 1.78f;           // meters
        const float CG_HEIGHT = 0.33f;             // meters
        const float FRONT_WEIGHT_RATIO = 0.45f;
        const float REAR_WEIGHT_RATIO = 0.55f;
        const float MIN_RIDE_HEIGHT = 0.032f;      // meters
        const float MAX_RIDE_HEIGHT = 0.095f;      // meters
        const float BOTTOM_OUT_HEIGHT = 0.015f;    // meters
        const float MAX_FUEL_LOAD = 110f;          // liters for traction scaling

        public static float TotalMass(VehicleState state)
        {
            return BASE_CAR_MASS + state.fuelLoad * FUEL_DENSITY;
        }

        public static float FuelMass(VehicleState state)
        {
            return state.fuelLoad * FUEL_DENSITY;
        }

        public static float FuelWeight(VehicleState state)
        {
            return FuelMass(state) * GRAVITY;
        }

        public static void BurnFuel(VehicleState state, float burnRateLitersPerSecond, float dt)
        {
            state.fuelLoad = Mathf.Max(0f, state.fuelLoad - burnRateLitersPerSecond * dt);
        }

        public static float CalculateEffectiveRideHeight(VehicleState state, CarSetup setup, float longitudinalAccel)
        {
            float rideFront = Mathf.Lerp(MIN_RIDE_HEIGHT, MAX_RIDE_HEIGHT,
                (setup.rideHeightFront - 1f) / 10f);
            float rideRear = Mathf.Lerp(MIN_RIDE_HEIGHT, MAX_RIDE_HEIGHT,
                (setup.rideHeightRear - 1f) / 10f);

            float frontSpring = Mathf.Lerp(145000f, 225000f,
                (setup.suspensionStiffnessFront - 1f) / 10f);
            float rearSpring = Mathf.Lerp(138000f, 215000f,
                (setup.suspensionStiffnessRear - 1f) / 10f);

            float mass = TotalMass(state);
            float transferForce = mass * longitudinalAccel * CG_HEIGHT / WHEELBASE;
            float frontCompression = Mathf.Clamp(transferForce / frontSpring, -0.03f, 0.05f);
            float rearCompression = Mathf.Clamp(-transferForce / rearSpring, -0.03f, 0.05f);

            float effectiveFront = rideFront - frontCompression;
            float effectiveRear = rideRear - rearCompression;

            return Mathf.Max(BOTTOM_OUT_HEIGHT, Mathf.Min(effectiveFront, effectiveRear));
        }

        public static float AdjustDownforceForRideHeight(float baseDownforce, float rideHeight)
        {
            if (rideHeight <= BOTTOM_OUT_HEIGHT)
            {
                return baseDownforce * 0.30f;
            }

            float heightFactor = Mathf.InverseLerp(MAX_RIDE_HEIGHT, MIN_RIDE_HEIGHT, rideHeight);
            float groundEffect = Mathf.Lerp(0.86f, 1.28f, heightFactor);
            return baseDownforce * groundEffect;
        }

        public static void ApplyWeightTransfer(VehicleState state, float longitudinalAccel, float lateralAccel)
        {
            float mass = TotalMass(state);
            float staticFront = mass * GRAVITY * FRONT_WEIGHT_RATIO;
            float staticRear  = mass * GRAVITY * REAR_WEIGHT_RATIO;

            float longitudinalTransfer = mass * longitudinalAccel * CG_HEIGHT / WHEELBASE;
            float lateralTransfer = mass * lateralAccel * CG_HEIGHT / TRACK_WIDTH;

            float frontLeft  = staticFront - longitudinalTransfer + lateralTransfer * 0.5f;
            float frontRight = staticFront - longitudinalTransfer - lateralTransfer * 0.5f;
            float rearLeft   = staticRear  + longitudinalTransfer + lateralTransfer * 0.5f;
            float rearRight  = staticRear  + longitudinalTransfer - lateralTransfer * 0.5f;

            state.tires[0].load = Mathf.Max(0f, frontLeft);
            state.tires[1].load = Mathf.Max(0f, frontRight);
            state.tires[2].load = Mathf.Max(0f, rearLeft);
            state.tires[3].load = Mathf.Max(0f, rearRight);
        }

        public static void UpdateTireGripFromLoad(VehicleState state, WeatherData weather, bool bottomedOut)
        {
            float baseline = TotalMass(state) * GRAVITY * 0.25f;
            float fuelPenalty = Mathf.Clamp01(1f - state.fuelLoad / MAX_FUEL_LOAD) * state.throttle * 0.18f;

            for (int i = 0; i < 4; i++)
            {
                TireState tire = state.tires[i];
                float loadRatio = tire.load / Mathf.Max(100f, baseline);
                float loadGrip = Mathf.Lerp(0.90f, 1.08f, Mathf.Clamp01(loadRatio * 0.95f));
                float lateralPenalty = Mathf.Abs(state.gLateral) * 0.04f;

                tire.surfaceGrip = Mathf.Clamp01(loadGrip - fuelPenalty - lateralPenalty);

                if (bottomedOut)
                {
                    tire.surfaceGrip *= 0.78f;
                }
            }
        }

        public static float CalculateAverageGrip(VehicleState state, WeatherData weather)
        {
            float totalGrip = 0f;
            for (int i = 0; i < 4; i++)
            {
                totalGrip += TirePhysics.CalculateGrip(state.tires[i], weather);
            }
            return totalGrip * 0.25f;
        }

        public static void SimulateVehicleDynamics(VehicleState state, CarSetup setup,
            WeatherData weather, float dt)
        {
            float mass = TotalMass(state);
            float engineForce = PowertrainPhysics.DriveForce(state.rpm, state.gear,
                state.engineMode, state.damage) * state.throttle;
            float brakeForce = state.brake * 18000f * DamageSystem.BrakeEffectivenessMultiplier(state.damage);

            state.dragForce = Aerodynamics.CalculateDrag(state.speedMs, setup,
                state.drsActive, state.damage);

            float netForce = engineForce - brakeForce - state.dragForce;
            float longitudinalAccel = netForce / mass;

            state.gLongitudinal = longitudinalAccel / GRAVITY;
            state.gLateral = Mathf.Clamp(state.steering * state.speedMs * 0.18f, -3.0f, 3.0f);

            ApplyWeightTransfer(state, longitudinalAccel, state.gLateral);

            float rideHeight = CalculateEffectiveRideHeight(state, setup, longitudinalAccel);
            bool bottomedOut = Aerodynamics.IsBottomedOut(rideHeight);

            state.downforce = Aerodynamics.CalculateDownforce(state.speedMs, setup,
                state.damage, rideHeight, bottomedOut);

            UpdateTireGripFromLoad(state, weather, bottomedOut);

            float avgGrip = CalculateAverageGrip(state, weather);
            float traction = Mathf.Clamp01(avgGrip * DamageSystem.SuspensionHandlingMultiplier(state.damage));

            float effectiveAccel = netForce / mass * traction;
            state.speedMs = Mathf.Max(0f, state.speedMs + effectiveAccel * dt);
            state.speed = state.speedMs * 3.6f;
        }
    }
}
