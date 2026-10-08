using System;
using Tag.Art;
using Tag.Local;
using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Chase boom and look-ahead. A wall pulls the boom in on the same frame.
    /// Pose gates catch the look up inside a short window. A normal turn keeps the slow slew.
    /// The timer lives on each camera. Couch rigs and DummyRunner do not write the solo timer.
    /// No field-of-view pop, no shake, no slow-motion.
    /// </summary>
    public static class ChaseCam
    {
        public const float AheadRate = 4.5f;
        public const float CatchRate = 22f;
        /// <summary>Same length as the wall-jump and slide pose windows.</summary>
        public const float CatchSeconds = 0.10f;
        /// <summary>
        /// Re-extension only. Pull-in stays the same frame so the lens never sits inside a wall.
        /// Slower than the old 18, which closed about a meter of a full boom on frame one.
        /// </summary>
        public const float BoomRate = 6f;
        public const float LookRate = 8f;
        /// <summary>Catch-up does not change field of view.</summary>
        public const float FovPop = 0f;
        /// <summary>Catch-up does not add a shake.</summary>
        public const float Shake = 0f;
        /// <summary>Catch-up does not scale time.</summary>
        public const float SlowMo = 0f;
        /// <summary>No static timer. Each rig passes its own remaining time.</summary>
        public const bool SharedTimer = false;

        public static float Remaining(float rate, float seconds)
        {
            if (seconds <= 0f || rate <= 0f) return 1f;
            return (float)Math.Exp(-rate * seconds);
        }

        public static bool WantsCatchup(bool enteredSlide, bool wallToAir)
        {
            return enteredSlide || wallToAir;
        }

        public static bool WantsCatchup(bool enteredSlide, bool wallToAir, bool mantle, bool becomeIt, bool lungeBurst, bool aim)
        {
            return enteredSlide || wallToAir || mantle || becomeIt || lungeBurst || aim;
        }

        /// <summary>
        /// DummyRunner has no solo chase rig, so it does not arm one.
        /// A couch pawn may arm only the timer it passes in.
        /// </summary>
        public static bool Arms(string pawnName)
        {
            if (string.IsNullOrEmpty(pawnName)) return false;
            if (pawnName == SoloGrappleGate.OpponentPawnName) return false;
            return true;
        }

        /// <summary>
        /// Short window for the gates that just opened. 0 keeps the slow slew.
        /// Slide and wall jump stay on their 0.10s pose windows.
        /// Mantle uses the vault enter. Become-It uses the claim/give-up open, not the hold.
        /// Lunge uses the tell-to-burst handoff, not the 0.20s motor burst.
        /// Aim uses the chest blend for punch telegraph/windup and grapple aim/latch.
        /// </summary>
        public static float Seconds(bool enteredSlide, bool wallToAir, bool mantle, bool becomeIt, bool lungeBurst, bool aim)
        {
            float s = 0f;
            if (enteredSlide) s = Mathf.Max(s, VerbPoseClips.SlideBlendSeconds);
            if (wallToAir) s = Mathf.Max(s, WallPose.PushBlendSeconds);
            if (mantle) s = Mathf.Max(s, MantlePose.EnterBlendSeconds);
            if (becomeIt) s = Mathf.Max(s, PoseHandoff.PunchBecomeSeconds);
            if (lungeBurst) s = Mathf.Max(s, PoseHandoff.LungeBurstSeconds);
            if (aim) s = Mathf.Max(s, AimTorsoPose.BlendSeconds);
            return s;
        }

        /// <summary>Raise one rig's timer. Another pawn's timer is a different variable.</summary>
        public static void Arm(ref float catchRemaining, bool enteredSlide, bool wallToAir, bool mantle, bool becomeIt, bool lungeBurst, bool aim, string pawnName)
        {
            if (!Arms(pawnName)) return;
            float window = Seconds(enteredSlide, wallToAir, mantle, becomeIt, lungeBurst, aim);
            if (window > catchRemaining)
                catchRemaining = window;
        }

        public static float AheadRateFor(float catchRemaining)
        {
            return catchRemaining > 0f ? CatchRate : AheadRate;
        }

        public static float LookRateFor(float catchRemaining)
        {
            return catchRemaining > 0f ? CatchRate : LookRate;
        }

        /// <summary>Shorter than the current boom snaps. Longer eases, faster during a catch-up.</summary>
        public static float BoomDistance(float current, float desired, float dt, bool catchup)
        {
            if (desired < current) return desired;
            float rate = catchup ? CatchRate : BoomRate;
            float t = 1f - Remaining(rate, dt);
            return current + (desired - current) * t;
        }

        public static bool Holds()
        {
            if (Mathf.Abs(CatchSeconds - 0.10f) > 0.001f) return false;
            if (SharedTimer || FovPop != 0f || Shake != 0f || SlowMo != 0f) return false;
            float slow = Remaining(AheadRate, CatchSeconds);
            float fast = Remaining(CatchRate, CatchSeconds);
            if (slow < 0.50f) return false;
            if (fast > 0.15f) return false;
            if (!WantsCatchup(true, false) || !WantsCatchup(false, true)) return false;
            if (WantsCatchup(false, false)) return false;
            if (Mathf.Abs(AheadRateFor(0f) - AheadRate) > 0.001f) return false;
            if (Mathf.Abs(AheadRateFor(0.05f) - CatchRate) > 0.001f) return false;
            if (Mathf.Abs(LookRateFor(0f) - LookRate) > 0.001f) return false;
            if (Mathf.Abs(LookRateFor(0.05f) - CatchRate) > 0.001f) return false;
            if (Mathf.Abs(BoomDistance(5.2f, 0.55f, 0.016f, false) - 0.55f) > 0.001f) return false;
            float outSlow = BoomDistance(0.55f, 5.2f, CatchSeconds, false);
            float outFast = BoomDistance(0.55f, 5.2f, CatchSeconds, true);
            if (!(outFast > outSlow)) return false;

            float slide = Seconds(true, false, false, false, false, false);
            float wall = Seconds(false, true, false, false, false, false);
            float mantle = Seconds(false, false, true, false, false, false);
            float become = Seconds(false, false, false, true, false, false);
            float lunge = Seconds(false, false, false, false, true, false);
            float aim = Seconds(false, false, false, false, false, true);
            if (Seconds(false, false, false, false, false, false) != 0f) return false;
            if (Mathf.Abs(slide - CatchSeconds) > 0.001f || Mathf.Abs(wall - CatchSeconds) > 0.001f) return false;
            if (Mathf.Abs(slide - VerbPoseClips.SlideBlendSeconds) > 0.001f) return false;
            if (Mathf.Abs(slide - CrouchPose.SlideHandoffSeconds) > 0.001f) return false;
            if (Mathf.Abs(wall - WallPose.PushBlendSeconds) > 0.001f) return false;
            if (Mathf.Abs(mantle - MantlePose.EnterBlendSeconds) > 0.001f) return false;
            if (Mathf.Abs(become - PoseHandoff.PunchBecomeSeconds) > 0.001f) return false;
            if (become >= BecomeItPose.HoldSeconds - 0.05f) return false;
            if (Mathf.Abs(lunge - PoseHandoff.LungeBurstSeconds) > 0.001f) return false;
            if (lunge >= LungePose.BurstSeconds - 0.05f) return false;
            if (Mathf.Abs(lunge - LungePose.LeadSeconds) < 0.05f) return false;
            if (Mathf.Abs(aim - AimTorsoPose.BlendSeconds) > 0.001f) return false;
            if (aim < 0.08f || aim > 0.12f) return false;
            if (slide < 0.08f || slide > 0.12f) return false;
            if (wall < 0.08f || wall > 0.12f) return false;
            if (mantle < 0.08f || mantle > 0.12f) return false;
            if (become < 0.08f || become > 0.12f) return false;
            if (lunge < 0.08f || lunge > 0.12f) return false;

            if (!WantsCatchup(false, false, true, false, false, false)) return false;
            if (!WantsCatchup(false, false, false, true, false, false)) return false;
            if (!WantsCatchup(false, false, false, false, true, false)) return false;
            if (!WantsCatchup(false, false, false, false, false, true)) return false;
            if (WantsCatchup(false, false, false, false, false, false)) return false;

            float lungeFast = Remaining(CatchRate, lunge);
            float lungeSlow = Remaining(AheadRate, lunge);
            if (!(lungeFast < lungeSlow) || lungeFast > 0.20f) return false;
            if (!(Remaining(CatchRate, mantle) < Remaining(AheadRate, mantle))) return false;
            if (!(Remaining(CatchRate, aim) < Remaining(AheadRate, aim))) return false;
            float both = Seconds(false, false, true, false, true, false);
            if (Mathf.Abs(both - Mathf.Max(mantle, lunge)) > 0.001f) return false;

            if (!Arms(SoloGrappleGate.SoloPawnName) || !Arms("Player_P1") || !Arms("Player_P2")) return false;
            if (Arms(SoloGrappleGate.OpponentPawnName) || Arms("DummyRunner") || Arms("")) return false;

            float solo = 0f;
            float couch = 0f;
            Arm(ref solo, false, false, true, false, false, false, SoloGrappleGate.SoloPawnName);
            Arm(ref couch, false, false, false, false, true, false, "Player_P2");
            Arm(ref solo, true, false, false, false, false, false, "DummyRunner");
            if (Mathf.Abs(solo - mantle) > 0.001f) return false;
            if (Mathf.Abs(couch - lunge) > 0.001f) return false;
            float ignored = 0.04f;
            Arm(ref ignored, false, false, false, true, false, false, "DummyRunner");
            if (Mathf.Abs(ignored - 0.04f) > 0.001f) return false;
            return true;
        }

        public static string ProofLine()
        {
            float slow = Remaining(AheadRate, CatchSeconds);
            float fast = Remaining(CatchRate, CatchSeconds);
            float slide = Seconds(true, false, false, false, false, false);
            float wall = Seconds(false, true, false, false, false, false);
            float mantle = Seconds(false, false, true, false, false, false);
            float become = Seconds(false, false, false, true, false, false);
            float lunge = Seconds(false, false, false, false, true, false);
            float aim = Seconds(false, false, false, false, false, true);
            return "chase cam"
                + " aheadLag@" + CatchSeconds.ToString("0.00") + "s=" + slow.ToString("0.00")
                + " catchLag=" + fast.ToString("0.00")
                + " boomSnapIn=yes"
                + " slowTurn=" + AheadRate.ToString("0.0")
                + " fovPop=0 shake=0 slowMo=0 sharedTimer=0"
                + " wallJump+slide=" + CatchSeconds.ToString("0.00") + "s"
                + " gates="
                + "slide " + slide.ToString("0.00") + "s enter"
                + ", wallJump " + wall.ToString("0.00") + "s wall-to-air"
                + ", mantle " + mantle.ToString("0.00") + "s enter"
                + ", becomeIt " + become.ToString("0.00") + "s claim/give-up open"
                + ", lungeBurst " + lunge.ToString("0.00") + "s tell-to-burst"
                + ", aim " + aim.ToString("0.00") + "s punch telegraph/windup + grapple aim/attached";
        }
    }
}
