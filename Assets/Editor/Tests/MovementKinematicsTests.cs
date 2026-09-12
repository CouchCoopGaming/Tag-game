using NUnit.Framework;
using Tag.Art;
using Tag.Movement;
using UnityEngine;

namespace Tag.EditorTests
{
    public class MovementKinematicsTests
    {
        [Test]
        public void PartyDefaults_MatchDraftTargets()
        {
            var t = MovementTuning.CreateRuntimeDefaults();
            Assert.AreEqual(5.5f, t.walkSpeed, 0.01f);
            Assert.AreEqual(9.0f, t.sprintSpeed, 0.01f);
            Assert.AreEqual(12.0f, t.slidePeakSpeed, 0.01f);
            Assert.AreEqual(1, t.airDodgeCharges);
            Assert.IsTrue(t.autoSprint);
            Assert.IsTrue(t.slideFromSpeed);
            Assert.IsTrue(t.slideBoostToPeak);
            Assert.IsTrue(t.airDodgeRefreshOnLand);
            Assert.GreaterOrEqual(t.airDodgeSpeed, 14f);
            Assert.LessOrEqual(t.airDodgeSpeed, 16f);
            Assert.GreaterOrEqual(t.airDodgeLock, 0.12f);
            Assert.LessOrEqual(t.airDodgeLock, 0.18f);
            Assert.GreaterOrEqual(t.airDodgeIFrames, 0.10f);
            Assert.LessOrEqual(t.airDodgeIFrames, 0.15f);
        }

        [Test]
        public void AirDashDistance_IsInPartyBand()
        {
            var t = MovementTuning.CreateRuntimeDefaults();
            float d = MovementKinematics.EffectiveAirDashDistance(t);
            Assert.GreaterOrEqual(d, 2.0f);
            Assert.LessOrEqual(d, 2.5f);
        }

        [Test]
        public void AirDodgeSpeed_ClampsToMaxDistance()
        {
            float speed = MovementKinematics.EffectiveAirDodgeSpeed(20f, 0.15f, 2.5f);
            Assert.AreEqual(2.5f / 0.15f, speed, 0.01f);
        }

        [Test]
        public void SlideEnter_BoostsSprintToPeak()
        {
            float enter = MovementKinematics.SlideEnterSpeed(9f, 12f, 6.5f, boostToPeak: true, wipeToGate: false);
            Assert.AreEqual(12f, enter, 0.01f);
        }

        [Test]
        public void SlideEnter_KeepsFasterThanPeak()
        {
            float enter = MovementKinematics.SlideEnterSpeed(14f, 12f, 6.5f, boostToPeak: true, wipeToGate: false);
            Assert.AreEqual(14f, enter, 0.01f);
        }

        [Test]
        public void AutoSprint_FullStickSprints_LightStickWalks()
        {
            Assert.IsTrue(MovementKinematics.WantsSprint(true, 0.55f, 1f, false));
            Assert.IsFalse(MovementKinematics.WantsSprint(true, 0.55f, 0.3f, false));
            Assert.IsTrue(MovementKinematics.WantsSprint(false, 0.55f, 1f, true));
            Assert.IsFalse(MovementKinematics.WantsSprint(false, 0.55f, 1f, false));
        }

        [Test]
        public void SlideFromSpeed_RequiresGateAndHold()
        {
            Assert.IsTrue(MovementKinematics.CanStartSlide(true, false, 0f, 9f, 6.5f, false, true, true));
            Assert.IsFalse(MovementKinematics.CanStartSlide(true, false, 0f, 5f, 6.5f, false, true, true));
            Assert.IsFalse(MovementKinematics.CanStartSlide(false, false, 0f, 9f, 6.5f, true, true, true));
            Assert.IsTrue(MovementKinematics.CanStartSlide(true, false, 0f, 9f, 6.5f, true, false, false));
        }

        [Test]
        public void JumpLaunch_KeepsAuthoredWhenCloseToDerived()
        {
            float derived = Mathf.Sqrt(2f * 28f * 1.15f);
            float launch = MovementKinematics.JumpLaunchSpeed(28f, 1.15f, 8f);
            Assert.AreEqual(8f, launch, 0.01f);
            Assert.Less(Mathf.Abs(8f - derived), 0.5f);
        }

        [Test]
        public void EmptyDummyPrefab_HasNoRenderer()
        {
            Assert.IsFalse(DummyPrimitiveFactory.PrefabHasRenderer(null));
        }

        [Test]
        public void JumpLaunch_RewritesWhenAuthoredIsOff()
        {
            float derived = Mathf.Sqrt(2f * 28f * 1.15f);
            float launch = MovementKinematics.JumpLaunchSpeed(28f, 1.15f, 20f);
            Assert.AreEqual(derived, launch, 0.01f);
        }

        [Test]
        public void ParkRamp20_IsWalkable_SteepIsNot()
        {
            Vector3 ramp20 = Quaternion.Euler(20f, 0f, 0f) * Vector3.up;
            Vector3 cliff = Quaternion.Euler(60f, 0f, 0f) * Vector3.up;
            Assert.IsTrue(MovementKinematics.IsWalkableSlope(ramp20, 45f));
            Assert.IsFalse(MovementKinematics.IsWalkableSlope(cliff, 45f));
            Assert.AreEqual(20f, MovementKinematics.SlopeAngle(ramp20), 0.1f);
        }

        [Test]
        public void ProjectWishOnSlope_KeepsPathSpeed()
        {
            Vector3 wish = new Vector3(9f, 0f, 0f);
            Vector3 n = Quaternion.Euler(0f, 0f, -20f) * Vector3.up;
            Vector3 along = MovementKinematics.ProjectWishOnSlope(wish, n);
            Assert.AreEqual(9f, along.magnitude, 0.02f);
            Assert.Greater(Mathf.Abs(along.y), 0.01f);
        }

        [Test]
        public void SteepSlope_SlidesDownhill()
        {
            Vector3 n = Quaternion.Euler(0f, 0f, -55f) * Vector3.up;
            Vector3 v = MovementKinematics.SteepSlopeSlideVelocity(n, 8f);
            Assert.AreEqual(8f, v.magnitude, 0.05f);
            Assert.Less(v.y, 0f);
        }
    }
}
