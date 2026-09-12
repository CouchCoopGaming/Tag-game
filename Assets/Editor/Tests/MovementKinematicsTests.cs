#if UNITY_EDITOR
using Tag.Art;
using Tag.Movement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tag.EditorTests
{
    /// <summary>
    /// Editor smoke for party movement numbers. Lives in Assembly-CSharp-Editor
    /// (no isolated test asmdef) so Tag.Movement / Tag.Art resolve and Safe Mode stays off.
    /// Menu: Tag → Run Movement Kinematics Smoke
    /// </summary>
    public static class MovementKinematicsTests
    {
        [MenuItem("Tag/Run Movement Kinematics Smoke")]
        public static void RunAll()
        {
            PartyDefaults_MatchDraftTargets();
            AirDashDistance_IsInPartyBand();
            AirDodgeSpeed_ClampsToMaxDistance();
            SlideEnter_BoostsSprintToPeak();
            SlideEnter_KeepsFasterThanPeak();
            AutoSprint_FullStickSprints_LightStickWalks();
            SlideFromSpeed_RequiresGateAndHold();
            JumpLaunch_KeepsAuthoredWhenCloseToDerived();
            EmptyDummyPrefab_HasNoRenderer();
            UrpSetupScript_IsPresent();
            DummyArtFbx_ArePresent();
            JumpLaunch_RewritesWhenAuthoredIsOff();
            ParkRamp20_IsWalkable_SteepIsNot();
            ProjectWishOnSlope_KeepsPathSpeed();
            SteepSlope_SlidesDownhill();
            Debug.Log("[Tag] Movement kinematics smoke OK");
        }

        public static void PartyDefaults_MatchDraftTargets()
        {
            var t = MovementTuning.CreateRuntimeDefaults();
            Assert.AreApproximatelyEqual(5.5f, t.walkSpeed);
            Assert.AreApproximatelyEqual(9.0f, t.sprintSpeed);
            Assert.AreApproximatelyEqual(12.0f, t.slidePeakSpeed);
            Assert.AreEqual(1, t.airDodgeCharges);
            Assert.IsTrue(t.autoSprint);
            Assert.IsTrue(t.slideFromSpeed);
            Assert.IsTrue(t.slideBoostToPeak);
            Assert.IsTrue(t.airDodgeRefreshOnLand);
            Assert.IsTrue(t.airDodgeSpeed >= 14f && t.airDodgeSpeed <= 16f);
            Assert.IsTrue(t.airDodgeLock >= 0.12f && t.airDodgeLock <= 0.18f);
            Assert.IsTrue(t.airDodgeIFrames >= 0.10f && t.airDodgeIFrames <= 0.15f);
        }

        public static void AirDashDistance_IsInPartyBand()
        {
            var t = MovementTuning.CreateRuntimeDefaults();
            float d = MovementKinematics.EffectiveAirDashDistance(t);
            Assert.IsTrue(d >= 2.0f && d <= 2.5f);
        }

        public static void AirDodgeSpeed_ClampsToMaxDistance()
        {
            float speed = MovementKinematics.EffectiveAirDodgeSpeed(20f, 0.15f, 2.5f);
            Assert.AreApproximatelyEqual(2.5f / 0.15f, speed);
        }

        public static void SlideEnter_BoostsSprintToPeak()
        {
            float enter = MovementKinematics.SlideEnterSpeed(9f, 12f, 6.5f, boostToPeak: true, wipeToGate: false);
            Assert.AreApproximatelyEqual(12f, enter);
        }

        public static void SlideEnter_KeepsFasterThanPeak()
        {
            float enter = MovementKinematics.SlideEnterSpeed(14f, 12f, 6.5f, boostToPeak: true, wipeToGate: false);
            Assert.AreApproximatelyEqual(14f, enter);
        }

        public static void AutoSprint_FullStickSprints_LightStickWalks()
        {
            Assert.IsTrue(MovementKinematics.WantsSprint(true, 0.55f, 1f, false));
            Assert.IsFalse(MovementKinematics.WantsSprint(true, 0.55f, 0.3f, false));
            Assert.IsTrue(MovementKinematics.WantsSprint(false, 0.55f, 1f, true));
            Assert.IsFalse(MovementKinematics.WantsSprint(false, 0.55f, 1f, false));
        }

        public static void SlideFromSpeed_RequiresGateAndHold()
        {
            Assert.IsTrue(MovementKinematics.CanStartSlide(true, false, 0f, 9f, 6.5f, false, true, true));
            Assert.IsFalse(MovementKinematics.CanStartSlide(true, false, 0f, 5f, 6.5f, false, true, true));
            Assert.IsFalse(MovementKinematics.CanStartSlide(false, false, 0f, 9f, 6.5f, true, true, true));
            Assert.IsTrue(MovementKinematics.CanStartSlide(true, false, 0f, 9f, 6.5f, true, false, false));
        }

        public static void JumpLaunch_KeepsAuthoredWhenCloseToDerived()
        {
            float derived = Mathf.Sqrt(2f * 28f * 1.15f);
            float launch = MovementKinematics.JumpLaunchSpeed(28f, 1.15f, 8f);
            Assert.AreApproximatelyEqual(8f, launch);
            Assert.IsTrue(Mathf.Abs(8f - derived) < 0.5f);
        }

        public static void EmptyDummyPrefab_HasNoRenderer()
        {
            Assert.IsFalse(DummyPrimitiveFactory.PrefabHasRenderer(null));
        }

        public static void UrpSetupScript_IsPresent()
        {
            Assert.IsTrue(System.IO.File.Exists("Assets/Editor/TagUrpSetup.cs"));
            Assert.IsTrue(System.IO.File.Exists("Assets/Scripts/Art/TagUrpBootstrap.cs"));
            Assert.IsFalse(System.IO.File.Exists("Assets/Settings/TagURPAsset.asset"));
        }

        public static void DummyArtFbx_ArePresent()
        {
            Assert.IsTrue(System.IO.File.Exists("Assets/Art/Characters/Dummy_Runner.fbx"));
            Assert.IsTrue(System.IO.File.Exists("Assets/Art/Characters/Dummy_It.fbx"));
            Assert.IsTrue(System.IO.File.Exists("Assets/Art/Props/Playground/Toy_Bench.fbx"));
            Assert.IsFalse(System.IO.Directory.Exists("Assets/Resources/Characters/Fbx"));
            Assert.IsFalse(System.IO.Directory.Exists("Assets/Resources/Props/Fbx"));
        }

        public static void JumpLaunch_RewritesWhenAuthoredIsOff()
        {
            float derived = Mathf.Sqrt(2f * 28f * 1.15f);
            float launch = MovementKinematics.JumpLaunchSpeed(28f, 1.15f, 20f);
            Assert.AreApproximatelyEqual(derived, launch);
        }

        public static void ParkRamp20_IsWalkable_SteepIsNot()
        {
            Vector3 ramp20 = Quaternion.Euler(20f, 0f, 0f) * Vector3.up;
            Vector3 cliff = Quaternion.Euler(60f, 0f, 0f) * Vector3.up;
            Assert.IsTrue(MovementKinematics.IsWalkableSlope(ramp20, 45f));
            Assert.IsFalse(MovementKinematics.IsWalkableSlope(cliff, 45f));
            Assert.AreApproximatelyEqual(20f, MovementKinematics.SlopeAngle(ramp20));
        }

        public static void ProjectWishOnSlope_KeepsPathSpeed()
        {
            Vector3 wish = new Vector3(9f, 0f, 0f);
            Vector3 n = Quaternion.Euler(0f, 0f, -20f) * Vector3.up;
            Vector3 along = MovementKinematics.ProjectWishOnSlope(wish, n);
            Assert.AreApproximatelyEqual(9f, along.magnitude);
            Assert.IsTrue(Mathf.Abs(along.y) > 0.01f);
        }

        public static void SteepSlope_SlidesDownhill()
        {
            Vector3 n = Quaternion.Euler(0f, 0f, -55f) * Vector3.up;
            Vector3 v = MovementKinematics.SteepSlopeSlideVelocity(n, 8f);
            Assert.AreApproximatelyEqual(8f, v.magnitude);
            Assert.IsTrue(v.y < 0f);
        }
    }
}
#endif
