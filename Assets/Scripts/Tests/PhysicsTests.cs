using NUnit.Framework;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Physics;
using RacingGame.Multiplayer;

namespace RacingGame.Tests
{
    // ═══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════════
    static class TestFactory
    {
        public static WeatherData Dry() => new()
        { condition = WeatherCondition.Dry, ambientTemp = 25f, trackTemp = 35f, rainIntensity = 0f };

        public static WeatherData HeavyRain() => new()
        { condition = WeatherCondition.HeavyRain, ambientTemp = 15f, trackTemp = 18f, rainIntensity = 0.9f };

        public static TireState NewMedium(float surfaceTemp = 80f, float coreTemp = 75f) =>
            new()
            {
                compound     = TireCompound.Medium,
                surfaceTemp  = surfaceTemp,
                coreTemp     = coreTemp,
                wearPercent  = 0f,
                coldPressurePSI = 23f,
                currentPressurePSI = 23f
            };

        public static TireState NewSoft(float surfaceTemp = 90f, float coreTemp = 85f) =>
            new()
            {
                compound    = TireCompound.Soft,
                surfaceTemp = surfaceTemp,
                coreTemp    = coreTemp,
                wearPercent = 0f,
                coldPressurePSI = 23f,
                currentPressurePSI = 23f
            };

        public static VehicleState Coasting() => new()
        {
            speedMs  = 60f, speed = 216f,
            throttle = 0f,  brake = 0f, steering = 0f,
            gear     = 5,   rpm   = 8000f,
            ersSoC   = 80f, ersMode = ERSMode.Balanced,
            tires    = new[] { NewMedium(), NewMedium(), NewMedium(), NewMedium() }
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DUAL-LAYER TIRE THERMAL TESTS
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class DualLayerThermalTests
    {
        [Test]
        public void ColdSurface_GivesReducedGrip()
        {
            var tire    = TestFactory.NewMedium(surfaceTemp: 30f, coreTemp: 30f);
            var weather = TestFactory.Dry();
            float grip  = TirePhysics.CalculateGrip(tire, weather);
            Assert.Less(grip, 0.70f, "Cold surface should give grip well below 0.70");
        }

        [Test]
        public void OptimalSurface_GivesMaxGrip()
        {
            var tire    = TestFactory.NewMedium(surfaceTemp: 82f, coreTemp: 78f);
            var weather = TestFactory.Dry();
            float grip  = TirePhysics.CalculateGrip(tire, weather);
            Assert.GreaterOrEqual(grip, 0.86f, "Optimal medium should give grip >= 0.86");
        }

        [Test]
        public void OverheatedSurface_ReducesGrip()
        {
            var tireHot  = TestFactory.NewMedium(surfaceTemp: 130f, coreTemp: 80f);
            var tireGood = TestFactory.NewMedium(surfaceTemp:  82f, coreTemp: 78f);
            var weather  = TestFactory.Dry();
            Assert.Less(TirePhysics.CalculateGrip(tireHot, weather),
                        TirePhysics.CalculateGrip(tireGood, weather),
                        "Overheated surface should have less grip than optimal");
        }

        [Test]
        public void SurfaceCoolsFasterThanCore()
        {
            // Simulate 5 seconds of coasting from hot state
            var tire    = TestFactory.NewMedium(surfaceTemp: 120f, coreTemp: 120f);
            var car     = TestFactory.Coasting();
            var weather = TestFactory.Dry();

            for (int i = 0; i < 250; i++)   // 250 × 0.02 s = 5 s
                TirePhysics.UpdateTire(tire, car, weather, 0.02f);

            Assert.Less(tire.surfaceTemp, tire.coreTemp,
                "Surface layer should cool faster than carcass (lower thermal mass)");
        }

        [Test]
        public void CoreHeatsSlowerThanSurface()
        {
            // Both start cold, simulate 3 s of aggressive driving
            var tire    = TestFactory.NewMedium(surfaceTemp: 25f, coreTemp: 25f);
            var car     = TestFactory.Coasting();
            car.steering = 0.9f;
            car.throttle = 1.0f;
            var weather = TestFactory.Dry();

            for (int i = 0; i < 150; i++)
                TirePhysics.UpdateTire(tire, car, weather, 0.02f);

            Assert.Greater(tire.surfaceTemp, tire.coreTemp,
                "Surface should heat faster than core during aggressive driving");
        }

        [Test]
        public void RainCoolsSurfaceSignificantly()
        {
            var tire    = TestFactory.NewMedium(surfaceTemp: 90f, coreTemp: 85f);
            var car     = TestFactory.Coasting();
            var dry     = TestFactory.Dry();
            var rain    = TestFactory.HeavyRain();

            var tireDry  = TestFactory.NewMedium(surfaceTemp: 90f, coreTemp: 85f);
            var tireRain = TestFactory.NewMedium(surfaceTemp: 90f, coreTemp: 85f);

            for (int i = 0; i < 200; i++)
            {
                TirePhysics.UpdateTire(tireDry,  car, dry,  0.02f);
                TirePhysics.UpdateTire(tireRain, car, rain, 0.02f);
            }

            Assert.Less(tireRain.surfaceTemp, tireDry.surfaceTemp - 15f,
                "Rain should cool surface significantly more than dry conditions");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GRAINING TESTS
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class GrainingTests
    {
        [Test]
        public void ColdTireHighSlip_BuildsGraining()
        {
            var tire    = TestFactory.NewSoft(surfaceTemp: 45f, coreTemp: 42f); // cold soft
            var car     = TestFactory.Coasting();
            car.steering = 0.85f;

            var weather = TestFactory.Dry();

            for (int i = 0; i < 300; i++)   // 6 s
                TirePhysics.UpdateTire(tire, car, weather, 0.02f);

            Assert.Greater(tire.grainingLevel, 0.01f,
                "Cold soft tire under lateral load should grain");
        }

        [Test]
        public void WarmTire_NoGraining()
        {
            var tire    = TestFactory.NewSoft(surfaceTemp: 92f, coreTemp: 88f); // in window
            var car     = TestFactory.Coasting();
            car.steering = 0.85f;
            var weather = TestFactory.Dry();

            for (int i = 0; i < 300; i++)
                TirePhysics.UpdateTire(tire, car, weather, 0.02f);

            Assert.Less(tire.grainingLevel, 0.001f,
                "Warm tire in optimal window should not grain");
        }

        [Test]
        public void Graining_BurnsOffOnceWarm()
        {
            // Start with pre-grained tire, then warm it up
            var tire    = TestFactory.NewSoft(surfaceTemp: 45f, coreTemp: 42f);
            tire.grainingLevel = 0.30f;

            var car     = TestFactory.Coasting();
            car.throttle = 0.5f;
            car.steering = 0f;
            var weather = TestFactory.Dry();

            // Simulate warming – increase surface temp manually
            tire.surfaceTemp = 95f;

            float initialGraining = tire.grainingLevel;
            for (int i = 0; i < 500; i++)
                TirePhysics.UpdateTire(tire, car, weather, 0.02f);

            Assert.Less(tire.grainingLevel, initialGraining,
                "Graining should decrease once tire is in optimal temp window");
        }

        [Test]
        public void Graining_ReducesGrip()
        {
            var tireClean   = TestFactory.NewSoft(92f, 88f);
            var tireGrained = TestFactory.NewSoft(92f, 88f);
            tireGrained.grainingLevel = 0.80f;

            var weather = TestFactory.Dry();
            Assert.Less(TirePhysics.CalculateGrip(tireGrained, weather),
                        TirePhysics.CalculateGrip(tireClean,   weather),
                        "Grained tire should have lower grip");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  BLISTERING TESTS
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class BlisteringTests
    {
        [Test]
        public void OverheatedCore_BuildsBlistering()
        {
            var tire = TestFactory.NewSoft(surfaceTemp: 90f, coreTemp: 130f); // core over threshold
            var car  = TestFactory.Coasting();
            var w    = TestFactory.Dry();

            for (int i = 0; i < 300; i++)
                TirePhysics.UpdateTire(tire, car, w, 0.02f);

            Assert.Greater(tire.blisteringLevel, 0f,
                "Core above blister threshold should create blistering");
        }

        [Test]
        public void CoolCore_NoBlistering()
        {
            var tire = TestFactory.NewSoft(surfaceTemp: 90f, coreTemp: 75f);
            var car  = TestFactory.Coasting();
            var w    = TestFactory.Dry();

            for (int i = 0; i < 300; i++)
                TirePhysics.UpdateTire(tire, car, w, 0.02f);

            Assert.AreEqual(0f, tire.blisteringLevel, 0.001f,
                "Core below threshold should not blister");
        }

        [Test]
        public void Blistering_ReducesGrip()
        {
            var tireClean    = TestFactory.NewSoft(92f, 80f);
            var tireBlistered = TestFactory.NewSoft(92f, 80f);
            tireBlistered.blisteringLevel = 0.85f;

            var w = TestFactory.Dry();
            Assert.Less(TirePhysics.CalculateGrip(tireBlistered, w),
                        TirePhysics.CalculateGrip(tireClean,     w),
                        "Blistered tire should have lower grip");
        }

        [Test]
        public void Blistering_AcceleratesWear()
        {
            var tireClean    = TestFactory.NewSoft(92f, 80f);
            var tireBlistered = TestFactory.NewSoft(92f, 80f);
            tireBlistered.blisteringLevel = 0.90f;

            var car = TestFactory.Coasting();
            var w   = TestFactory.Dry();

            for (int i = 0; i < 200; i++)
            {
                TirePhysics.UpdateTire(tireClean,    car, w, 0.02f);
                TirePhysics.UpdateTire(tireBlistered, car, w, 0.02f);
            }

            Assert.Greater(tireBlistered.wearPercent, tireClean.wearPercent,
                "Blistered tire should wear faster");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TYRE PRESSURE TESTS
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class TyrePressureTests
    {
        [Test]
        public void HotCore_IncreasesLivePressure()
        {
            var tire = TestFactory.NewMedium(surfaceTemp: 85f, coreTemp: 25f);

            // Simulate warming core
            tire.coreTemp = 95f;

            // Manually call pressure update via UpdateTire for one tick
            var car = TestFactory.Coasting();
            var w   = TestFactory.Dry();
            TirePhysics.UpdateTire(tire, car, w, 0.02f);

            Assert.Greater(tire.currentPressurePSI, tire.coldPressurePSI,
                "Hot core should raise live pressure above cold baseline");
        }

        [Test]
        public void HighPressure_PenalisesGrip()
        {
            var tireNormal = TestFactory.NewMedium(82f, 75f);
            tireNormal.currentPressurePSI = 23f;

            var tireHigh = TestFactory.NewMedium(82f, 75f);
            tireHigh.currentPressurePSI = 33f;  // 10 PSI over

            var w = TestFactory.Dry();
            Assert.Less(TirePhysics.CalculateGrip(tireHigh, w),
                        TirePhysics.CalculateGrip(tireNormal, w),
                        "High pressure should reduce grip (smaller contact patch)");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ERS STATE-OF-CHARGE TESTS
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class ERSTests
    {
        [Test]
        public void QualifyingMode_DrainsQuickest()
        {
            var carQual = TestFactory.Coasting(); carQual.ersSoC = 100f; carQual.ersMode = ERSMode.Qualifying;
            var carBal  = TestFactory.Coasting(); carBal.ersSoC  = 100f; carBal.ersMode  = ERSMode.Balanced;
            carQual.tires = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                                    TestFactory.NewMedium(), TestFactory.NewMedium() };
            carBal.tires  = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                                    TestFactory.NewMedium(), TestFactory.NewMedium() };

            for (int i = 0; i < 100; i++)
            {
                carQual.ersSoC = ERSSystem.TickBattery(carQual, 0.02f);
                carBal.ersSoC  = ERSSystem.TickBattery(carBal,  0.02f);
            }

            Assert.Less(carQual.ersSoC, carBal.ersSoC,
                "Qualifying mode should drain battery faster than Balanced");
        }

        [Test]
        public void HarvestMode_DoesNotDeploy()
        {
            var car = TestFactory.Coasting();
            car.ersSoC  = 50f;
            car.ersMode = ERSMode.Harvest;
            car.tires   = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                                   TestFactory.NewMedium(), TestFactory.NewMedium() };

            float initial = car.ersSoC;
            // No braking, moderate RPM — should not drain
            for (int i = 0; i < 50; i++)
                car.ersSoC = ERSSystem.TickBattery(car, 0.02f);

            Assert.GreaterOrEqual(car.ersSoC, initial - 0.1f,
                "Harvest mode should not deplete battery");
        }

        [Test]
        public void HeavyBraking_Harvests_InAnyMode()
        {
            var car = TestFactory.Coasting();
            car.ersSoC  = 30f;
            car.brake   = 1f;
            car.ersMode = ERSMode.Balanced;
            car.tires   = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                                   TestFactory.NewMedium(), TestFactory.NewMedium() };

            float initial = car.ersSoC;
            for (int i = 0; i < 100; i++)
                car.ersSoC = ERSSystem.TickBattery(car, 0.02f);

            Assert.Greater(car.ersSoC, initial,
                "Heavy braking should harvest and raise SoC in Balanced mode");
        }

        [Test]
        public void OvertakeMode_TimerExpires()
        {
            var car = TestFactory.Coasting();
            car.ersSoC           = 100f;
            car.ersMode          = ERSMode.Overtake;
            car.ersOvertakeTimer = 10f;
            car.tires            = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                                            TestFactory.NewMedium(), TestFactory.NewMedium() };

            // Simulate 12 s (timer should expire after 10 s)
            for (int i = 0; i < 600; i++)
                car.ersSoC = ERSSystem.TickBattery(car, 0.02f);

            Assert.LessOrEqual(car.ersOvertakeTimer, 0f,
                "Overtake burst timer should have expired after 10 s");
        }

        [Test]
        public void SoC_ClampsTo100()
        {
            var car = TestFactory.Coasting();
            car.ersSoC  = 98f;
            car.ersMode = ERSMode.Harvest;
            car.brake   = 1f;
            car.tires   = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                                   TestFactory.NewMedium(), TestFactory.NewMedium() };

            for (int i = 0; i < 500; i++)
                car.ersSoC = ERSSystem.TickBattery(car, 0.02f);

            Assert.LessOrEqual(car.ersSoC, 100f, "SoC must never exceed 100 %");
        }

        [Test]
        public void SoC_NeverGoesNegative()
        {
            var car = TestFactory.Coasting();
            car.ersSoC  = 0f;
            car.ersMode = ERSMode.Qualifying;
            car.tires   = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                                   TestFactory.NewMedium(), TestFactory.NewMedium() };

            for (int i = 0; i < 500; i++)
                car.ersSoC = ERSSystem.TickBattery(car, 0.02f);

            Assert.GreaterOrEqual(car.ersSoC, 0f, "SoC must never go below 0 %");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DIRTY AIR & SLIPSTREAM TESTS
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class AerodynamicWakeTests
    {
        VehicleState MakeCar(float speed = 200f) => new VehicleState
        {
            speedMs  = speed / 3.6f,
            speed    = speed,
            steering = 0f,
            tires    = new[] { TestFactory.NewMedium(), TestFactory.NewMedium(),
                               TestFactory.NewMedium(), TestFactory.NewMedium() }
        };

        [Test]
        public void Within1s_CarInDirtyAir()
        {
            var behind = MakeCar();
            var ahead  = MakeCar();
            AerodynamicWakeSystem.ApplyWake(behind, ahead, 0.8f);
            Assert.IsTrue(behind.inDirtyAir, "Car within 0.8 s should be in dirty air");
        }

        [Test]
        public void Beyond1s5_NotInDirtyAir()
        {
            var behind = MakeCar();
            var ahead  = MakeCar();
            AerodynamicWakeSystem.ApplyWake(behind, ahead, 2.0f);
            Assert.IsFalse(behind.inDirtyAir, "Car beyond 1.5 s gap should not be in dirty air");
        }

        [Test]
        public void DirtyAir_ReducesDownforce()
        {
            var car    = MakeCar();
            var ahead  = MakeCar();
            AerodynamicWakeSystem.ApplyWake(car, ahead, 0.5f);

            var setup = new CarSetup { frontWingAngle = 5, rearWingAngle = 5 };
            var dmg   = new ComponentDamage();

            float cleanDf = AeroPhysics.CalculateDownforce(car.speedMs, setup, dmg, null);
            float dirtyDf = AeroPhysics.CalculateDownforce(car.speedMs, setup, dmg, car);

            Assert.Less(dirtyDf, cleanDf,
                "Dirty air should reduce downforce vs clean air");

            float lossPercent = (cleanDf - dirtyDf) / cleanDf * 100f;
            Assert.That(lossPercent, Is.InRange(8f, 12f),
                "Downforce loss should be approximately 10 %");
        }

        [Test]
        public void Slipstream_ReducesDrag()
        {
            var car   = MakeCar();
            var ahead = MakeCar();
            AerodynamicWakeSystem.ApplyWake(car, ahead, 0.4f);

            var setup = new CarSetup { frontWingAngle = 5, rearWingAngle = 5 };
            var dmg   = new ComponentDamage();

            float cleanDrag = AeroPhysics.CalculateDrag(car.speedMs, setup, false, dmg, null);
            float dirtyDrag = AeroPhysics.CalculateDrag(car.speedMs, setup, false, dmg, car);

            Assert.Less(dirtyDrag, cleanDrag,
                "Slipstream should reduce net drag vs clean air");
        }

        [Test]
        public void DirtyAir_InjectsHeatIntoTires()
        {
            var behind = MakeCar();
            var ahead  = MakeCar();
            AerodynamicWakeSystem.ApplyWake(behind, ahead, 0.5f);

            foreach (var tire in behind.tires)
                Assert.Greater(tire.dirtyAirHeatDelta, 0f,
                    "Dirty air turbulence should add heat to each tire");
        }

        [Test]
        public void DirtyAir_IncreasesSlipAngleOnTires()
        {
            var behind = MakeCar();
            var ahead  = MakeCar();
            AerodynamicWakeSystem.ApplyWake(behind, ahead, 0.3f);

            foreach (var tire in behind.tires)
                Assert.Greater(tire.dirtyAirSlipDelta, 0f,
                    "Dirty air should increase effective slip angle on all tires");
        }

        [Test]
        public void Slipstream_StrongerWhenCloser()
        {
            var behindClose = MakeCar();
            var behindFar   = MakeCar();
            var ahead       = MakeCar();

            AerodynamicWakeSystem.ApplyWake(behindClose, ahead, 0.2f);
            AerodynamicWakeSystem.ApplyWake(behindFar,   ahead, 1.2f);

            Assert.Greater(behindClose.slipstreamSpeedBonus, behindFar.slipstreamSpeedBonus,
                "Closer car should get bigger slipstream bonus");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ELO  (existing, unchanged)
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class EloTests
    {
        [Test] public void WinnerGainsRating()
        {
            var (a, b) = EloRating.Calculate(1500, 1500, 1f);
            Assert.Greater(a, 1500); Assert.Less(b, 1500);
        }

        [Test] public void Draw_BetweenEquals_NoChange()
        {
            var (a, b) = EloRating.Calculate(1500, 1500, 0.5f);
            Assert.AreEqual(1500, a); Assert.AreEqual(1500, b);
        }

        [Test] public void UpsetWin_GivesMorePoints()
        {
            var (weak,  _) = EloRating.Calculate(1200, 1800, 1f);
            var (strong, _) = EloRating.Calculate(1800, 1200, 1f);
            Assert.Greater(weak - 1200, strong - 1800);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DAMAGE  (existing, unchanged)
    // ═══════════════════════════════════════════════════════════════════════
    [TestFixture]
    public class DamageTests
    {
        [Test] public void LowImpact_NoDamage()
        {
            var d = new ComponentDamage();
            DamageSystem.ApplyCollisionDamage(d, 3f, true);
            Assert.AreEqual(0f, d.frontWing);
        }

        [Test] public void FrontalImpact_DamagesFrontWing()
        {
            var d = new ComponentDamage();
            DamageSystem.ApplyCollisionDamage(d, 20f, true);
            Assert.Greater(d.frontWing, 0f);
        }

        [Test] public void SevereDamage_TriggersDNF()
        {
            var d = new ComponentDamage { engine = 100f };
            Assert.IsTrue(d.IsDNF);
        }
    }
}
