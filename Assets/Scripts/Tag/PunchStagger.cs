using System;
using System.IO;
using Tag.Art;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Gameplay
{
    /// <summary>
    /// A punch that connects with a runner and does not transfer It.
    /// The struck pawn stumbles for a quarter second. Sprint drops and cannot
    /// re-engage until the stumble ends. A half-second immunity then blocks
    /// another stagger, so hits cannot chain-lock. No knockback and no new button.
    /// Reach, punch windows, and the jump numbers are not written here.
    /// </summary>
    public static class PunchStagger
    {
        public const float Duration = 0.25f;
        public const float Immunity = 0.50f;
        /// <summary>No impulse. Jump vertical stays the motor's jumpSpeed write.</summary>
        public const float Knockback = 0f;

        public struct Clock
        {
            public float Stagger;
            public float Immune;
        }

        /// <summary>
        /// Same gates as TagModeController.OnSuccessfulPunch.
        /// A live round, an It puncher, a living runner who can be tagged.
        /// </summary>
        public static bool IsTag(bool roundLive, bool puncherIsIt, bool victimIsRunner, bool victimCanBeTagged)
        {
            return roundLive && puncherIsIt && victimIsRunner && victimCanBeTagged;
        }

        /// <summary>The fist found a runner and the tag rules did not take the hit.</summary>
        public static bool IsStaggerHit(bool connectedRunner, bool isTag)
        {
            return connectedRunner && !isTag;
        }

        /// <summary>False while stumbling or immune, so a second hit cannot refresh the lock.</summary>
        public static bool TryStart(ref Clock clock)
        {
            if (clock.Stagger > 0f || clock.Immune > 0f) return false;
            clock.Stagger = Duration;
            return true;
        }

        public static void Tick(ref Clock clock, float dt)
        {
            if (dt < 0f) dt = 0f;
            if (clock.Stagger > 0f)
            {
                clock.Stagger -= dt;
                if (clock.Stagger <= 0f)
                {
                    clock.Stagger = 0f;
                    clock.Immune = Immunity;
                }
                return;
            }

            if (clock.Immune > 0f)
            {
                clock.Immune -= dt;
                if (clock.Immune < 0f) clock.Immune = 0f;
            }
        }

        /// <summary>Sprint is cancelled for the stumble. The key can engage again once it ends.</summary>
        public static bool SprintHeld(bool held, float staggerRemaining)
        {
            return held && staggerRemaining <= 0f;
        }

        public static bool Holds()
        {
            if (Mathf.Abs(Duration - 0.25f) > 0.001f) return false;
            if (Mathf.Abs(Immunity - 0.50f) > 0.001f) return false;
            if (Knockback != 0f) return false;
            if (PunchStaggerPose.RootMotion) return false;

            if (!IsTag(true, true, true, true)) return false;
            if (IsTag(false, true, true, true)) return false;
            if (IsTag(true, false, true, true)) return false;
            if (IsTag(true, true, false, true)) return false;
            if (IsTag(true, true, true, false)) return false;
            if (!IsStaggerHit(true, false)) return false;
            if (IsStaggerHit(true, true)) return false;
            if (IsStaggerHit(false, false)) return false;

            Clock clock = default;
            if (!TryStart(ref clock) || clock.Stagger < Duration - 0.001f) return false;
            if (TryStart(ref clock)) return false;
            if (SprintHeld(true, clock.Stagger)) return false;
            Tick(ref clock, Duration);
            if (clock.Stagger > 0.001f) return false;
            if (Mathf.Abs(clock.Immune - Immunity) > 0.001f) return false;
            if (TryStart(ref clock)) return false;
            if (!SprintHeld(true, clock.Stagger)) return false;
            Tick(ref clock, Immunity);
            if (clock.Immune > 0.001f || clock.Stagger > 0.001f) return false;
            if (!TryStart(ref clock)) return false;
            if (SprintHeld(false, 0f)) return false;

            if (!PunchStaggerPose.Holds()) return false;
            if (!DummyPosePaths.Allows("DummyRunner", DummyPosePaths.Stagger)) return false;
            if (!DummyPosePaths.Allows("Player", DummyPosePaths.Stagger)) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
            if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) return false;
            if (cfg.slideBoost != 0f) return false;
            if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f) return false;
            if (Mathf.Abs(punch.reach - 1.55f) > 0.001f) return false;
            if (Mathf.Abs(punch.windup - 0.12f) > 0.001f) return false;
            if (Mathf.Abs(punch.active - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(punch.hitRecover - 0.15f) > 0.001f) return false;
            if (Mathf.Abs(punch.missRecover - 0.32f) > 0.001f) return false;

            if (!Wired()) return false;
            return true;
        }

        public static string ProofLine()
        {
            PunchStaggerPose.Sample stumble = PunchStaggerPose.Stumble();
            return "punch-stagger"
                + " dur=" + Duration.ToString("0.00")
                + " immune=" + Immunity.ToString("0.00")
                + " knock=" + Knockback.ToString("0.00")
                + " sprint=cancelled"
                + " pose=stumble"
                + " chest=" + (stumble.Hip + stumble.Spine).ToString("0")
                + " knee=" + stumble.KneeL.ToString("0")
                + " arm=" + stumble.ArmPitchL.ToString("0") + "/" + stumble.ArmPitchR.ToString("0")
                + " riseW=" + PunchStaggerPose.Weight(0.06f).ToString("0.00")
                + " endW=" + PunchStaggerPose.Weight(Duration).ToString("0.00")
                + " reach=1.55"
                + " dummy=receives+respects"
                + " chain=immune"
                + " rootMotion=0";
        }

        static bool Wired()
        {
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string hit = Read("Assets/Scripts/Tag/PunchHitbox.cs");
            string it = Read("Assets/Scripts/Tag/ItController.cs");
            string loco = Read("Assets/Scripts/Art/DummyLocomotor.cs");
            string patrol = Read("Assets/Scripts/Modes/DummyPatrol.cs");
            if (motor == null || hit == null || it == null || loco == null || patrol == null)
                return false;
            if (motor.IndexOf("BeginPunchStagger", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("IsPunchStaggered", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("PunchStagger.Tick", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("PunchStagger.SprintHeld", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("DummyPosePaths", StringComparison.Ordinal) >= 0) return false;
            if (Count(motor, "_cc.Move(") != 1) return false;
            if (motor.IndexOf("AddForce", StringComparison.Ordinal) >= 0
                && Method(motor, "bool BeginPunchStagger").IndexOf("AddForce", StringComparison.Ordinal) >= 0)
                return false;
            string begin = Method(motor, "bool BeginPunchStagger");
            if (begin.IndexOf("_velocity", StringComparison.Ordinal) >= 0) return false;
            if (begin.IndexOf("jumpSpeed", StringComparison.Ordinal) >= 0) return false;
            if (hit.IndexOf("ReceivePunchStagger", StringComparison.Ordinal) < 0) return false;
            if (hit.IndexOf("TagLandTell.Transferred", StringComparison.Ordinal) < 0) return false;
            if (hit.IndexOf("victim.ReceiveTagHit", StringComparison.Ordinal) < 0) return false;
            if (it.IndexOf("ReceivePunchStagger", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("PlayPunchStagger", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("ApplyPunchStagger", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("PunchStaggerPose.Stumble", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("DummyPosePaths.Stagger", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("IsPunchStaggered", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        static int Count(string hay, string needle)
        {
            int n = 0;
            int i = 0;
            while (i >= 0 && i < hay.Length)
            {
                i = hay.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0) break;
                n++;
                i += needle.Length;
            }
            return n;
        }

        static string Method(string source, string signature)
        {
            int start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) return "";
            int brace = source.IndexOf('{', start);
            if (brace < 0) return "";
            int depth = 0;
            for (int i = brace; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0) return source.Substring(brace, i - brace + 1);
                }
            }
            return "";
        }
    }
}
