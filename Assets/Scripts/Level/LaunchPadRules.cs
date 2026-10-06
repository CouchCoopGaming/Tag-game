using System;
using System.IO;
using Tag.Art;
using Tag.Gameplay;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Ballistic rules for a launch pad. The motor writes the velocity.
    /// Apex height is <c>v^2 / (2g)</c> with rise gravity only, so entry speed,
    /// the jump button, and held crouch do not change it.
    /// </summary>
    public static class LaunchPadRules
    {
        public const float DefaultApexMeters = 6f;
        public const float DefaultCooldown = 0.3f;
        /// <summary>A landing this much closer counts as helping the chase.</summary>
        public const float HelpMarginMeters = 0.35f;

        public static float VerticalSpeed(float apexHeight, float gravity)
        {
            if (apexHeight <= 0.001f || gravity <= 0.01f) return 0f;
            return Mathf.Sqrt(2f * gravity * apexHeight);
        }

        public static bool CooldownOpen(float now, float readyAt)
        {
            return now + 0.0001f >= readyAt;
        }

        public static float ArmCooldown(float now, float cooldown)
        {
            float cd = cooldown > 0f ? cooldown : 0f;
            return now + cd;
        }

        /// <summary>
        /// One velocity set. Vertical is replaced. Horizontal is replaced only
        /// when the pad sets a direction and a speed.
        /// </summary>
        public static Vector3 VelocitySet(Vector3 entry, float apexHeight, float gravity, Vector3 horizontal, bool setHorizontal)
        {
            Vector3 v = entry;
            v.y = VerticalSpeed(apexHeight, gravity);
            if (!setHorizontal) return v;
            horizontal.y = 0f;
            v.x = horizontal.x;
            v.z = horizontal.z;
            return v;
        }

        /// <summary>
        /// Where the arc meets pad height again. A pad with no horizontal keeps
        /// the speed the pawn already has. Hang time does not read the jump button.
        /// </summary>
        public static Vector3 Landing(
            Vector3 padPosition,
            Vector3 carriedHorizontal,
            Vector3 padHorizontal,
            bool padSetsHorizontal,
            float apexHeight,
            float gravity,
            float fallGravityMult)
        {
            Vector3 hv = padSetsHorizontal ? padHorizontal : carriedHorizontal;
            hv.y = 0f;
            float hang = OpponentChaseSteer.JumpHangSeconds(VerticalSpeed(apexHeight, gravity), gravity, fallGravityMult);
            return padPosition + hv * hang;
        }

        public static bool LandingHelps(Vector3 pawn, Vector3 landing, Vector3 target)
        {
            float now = (target - pawn).magnitude;
            float after = (target - landing).magnitude;
            return after + HelpMarginMeters < now;
        }

        /// <summary>
        /// Discrete apex of the motor's launch. The launch frame moves the set
        /// velocity. Later frames integrate rise gravity only. A jump-button
        /// vertical on entry is replaced and never written back.
        /// </summary>
        public static float SimulatedApex(float entrySpeed, bool jumpButton, float apexHeight, float gravity, float fallGravityMult, float maxFall, float dt)
        {
            Vector3 entry = new Vector3(0f, jumpButton ? 24.7f : 0f, entrySpeed);
            Vector3 v = VelocitySet(entry, apexHeight, gravity, Vector3.zero, false);
            float y = v.y * dt;
            float peak = y;
            for (int i = 0; i < 800; i++)
            {
                if (v.y <= 0f) break;
                float g = KinematicStep.AirGravity(v.y, gravity, fallGravityMult);
                v.y = KinematicStep.IntegrateVertical(v.y, g, dt, maxFall);
                y += v.y * dt;
                if (y > peak) peak = y;
            }
            return peak;
        }

        public static bool StrafeAddsHorizontalOnly(float gravity, float fallGravityMult, float maxFall, float dt)
        {
            Vector3 v = VelocitySet(new Vector3(0f, 4f, 12f), DefaultApexMeters, gravity, new Vector3(8f, 0f, 0f), true);
            float vy = v.y;
            Vector3 hv = new Vector3(v.x, 0f, v.z);
            Vector3 wish = new Vector3(0f, 0f, 1f);
            Vector3 steered = KinematicStep.AirSteer(hv, wish, 12f, 30f * 1.35f, dt);
            if (Mathf.Abs(steered.y) > 0.0001f) return false;
            if (steered.magnitude <= hv.magnitude + 0.0001f) return false;
            float g = KinematicStep.AirGravity(vy, gravity, fallGravityMult);
            float next = KinematicStep.IntegrateVertical(vy, g, dt, maxFall);
            if (Mathf.Abs((vy - next) - g * dt) > 0.001f) return false;
            if (Mathf.Abs(next - vy) < 0.0001f) return false;
            return true;
        }

        public static bool Holds()
        {
            if (!FeelLocks()) return false;
            if (!ApexFixed()) return false;
            if (!CooldownHolds()) return false;
            if (!DummyUsesPad()) return false;
            if (!PoseAndLand()) return false;
            if (!StrafeAddsHorizontalOnly(22f, 1.5f, 52f, 1f / 60f)) return false;
            if (!Wired()) return false;
            return true;
        }

        public static string ProofLine()
        {
            const float dt = 1f / 60f;
            const float g = 22f;
            const float fall = 1.5f;
            float a0 = SimulatedApex(0f, false, DefaultApexMeters, g, fall, 52f, dt);
            float a12 = SimulatedApex(12f, false, DefaultApexMeters, g, fall, 52f, dt);
            float a168 = SimulatedApex(16.8f, false, DefaultApexMeters, g, fall, 52f, dt);
            float j0 = SimulatedApex(0f, true, DefaultApexMeters, g, fall, 52f, dt);
            float j12 = SimulatedApex(12f, true, DefaultApexMeters, g, fall, 52f, dt);
            float j168 = SimulatedApex(16.8f, true, DefaultApexMeters, g, fall, 52f, dt);
            bool jumpIgnored = Mathf.Abs(j0 - a0) < 0.001f
                && Mathf.Abs(j12 - a12) < 0.001f
                && Mathf.Abs(j168 - a168) < 0.001f;
            bool take = false;
            bool skip = false;
            DummyFlags(out take, out skip);
            float ready = ArmCooldown(0f, DefaultCooldown);
            bool cd = CooldownOpen(0f, 0f) && !CooldownOpen(0.1f, ready) && CooldownOpen(0.3f, ready);
            return "launch pad"
                + " apex0=" + a0.ToString("0.00")
                + " apex12=" + a12.ToString("0.00")
                + " apex16.8=" + a168.ToString("0.00")
                + " jumpIgnored=" + (jumpIgnored ? "yes" : "no")
                + " cooldown=" + (cd ? DefaultCooldown.ToString("0.00") : "no")
                + " dummyTake=" + (take ? "yes" : "no")
                + " dummySkip=" + (skip ? "yes" : "no");
        }

        static bool ApexFixed()
        {
            const float dt = 1f / 60f;
            const float g = 22f;
            const float fall = 1.5f;
            float designed = VerticalSpeed(DefaultApexMeters, g);
            float continuous = designed * designed / (2f * g);
            if (Mathf.Abs(continuous - DefaultApexMeters) > 0.001f) return false;
            float a0 = SimulatedApex(0f, false, DefaultApexMeters, g, fall, 52f, dt);
            float a12 = SimulatedApex(12f, false, DefaultApexMeters, g, fall, 52f, dt);
            float a168 = SimulatedApex(16.8f, false, DefaultApexMeters, g, fall, 52f, dt);
            float j0 = SimulatedApex(0f, true, DefaultApexMeters, g, fall, 52f, dt);
            float j168 = SimulatedApex(16.8f, true, DefaultApexMeters, g, fall, 52f, dt);
            if (Mathf.Abs(a0 - a12) > 0.001f || Mathf.Abs(a0 - a168) > 0.001f) return false;
            if (Mathf.Abs(a0 - j0) > 0.001f || Mathf.Abs(a0 - j168) > 0.001f) return false;
            if (Mathf.Abs(a0 - DefaultApexMeters) > 0.45f) return false;
            float jumpApex = 24.7f * 24.7f / (2f * g);
            if (Mathf.Abs(a0 - jumpApex) < 2f) return false;
            Vector3 kept = VelocitySet(new Vector3(3f, 9f, 4f), DefaultApexMeters, g, Vector3.zero, false);
            if (Mathf.Abs(kept.y - designed) > 0.001f) return false;
            if (Mathf.Abs(kept.x - 3f) > 0.001f || Mathf.Abs(kept.z - 4f) > 0.001f) return false;
            Vector3 set = VelocitySet(new Vector3(16.8f, 24.7f, 0f), DefaultApexMeters, g, new Vector3(0f, 5f, 8f), true);
            if (Mathf.Abs(set.y - designed) > 0.001f) return false;
            if (Mathf.Abs(set.x) > 0.001f || Mathf.Abs(set.z - 8f) > 0.001f) return false;
            return true;
        }

        static bool CooldownHolds()
        {
            if (Mathf.Abs(DefaultCooldown - 0.3f) > 0.001f) return false;
            if (!CooldownOpen(1f, 1f)) return false;
            float ready = ArmCooldown(2f, DefaultCooldown);
            if (Mathf.Abs(ready - 2.3f) > 0.001f) return false;
            if (CooldownOpen(2.1f, ready)) return false;
            if (!CooldownOpen(2.3f, ready)) return false;
            return true;
        }

        static bool DummyUsesPad()
        {
            DummyFlags(out bool take, out bool skip);
            if (!take || !skip) return false;

            Vector3 pawn = Vector3.zero;
            Vector3 pad = new Vector3(0f, 0f, 4f);
            Vector3 carried = new Vector3(0f, 0f, 12f);
            Vector3 near = new Vector3(0f, 0f, 10f);
            Vector3 far = new Vector3(0f, 0f, 40f);
            Vector3 landing = Landing(pad, carried, Vector3.zero, false, DefaultApexMeters, 22f, 1.5f);
            if (LandingHelps(pawn, landing, near)) return false;
            if (!LandingHelps(pawn, landing, far)) return false;
            Vector3 away = Landing(pad, carried, new Vector3(0f, 0f, -10f), true, DefaultApexMeters, 22f, 1.5f);
            if (LandingHelps(pawn, away, far)) return false;
            return true;
        }

        static void DummyFlags(out bool take, out bool skip)
        {
            take = false;
            skip = false;
            OpponentChaseInput go = Blank();
            go.PadAhead = true;
            go.PadHelps = true;
            go.PadDistance = 4f;
            go.PadAim = new Vector3(0f, 0f, 1f);
            go.PlanarDistance = 20f;
            OpponentChaseWish onto = OpponentChaseSteer.Decide(go);
            take = onto.Verb == OpponentChaseVerb.PadTake && !onto.Jump && !onto.Lunge && !onto.AirDash
                && onto.MoveY > 0.2f;

            OpponentChaseInput around = go;
            around.PadHelps = false;
            around.PathStrafe = 1f;
            OpponentChaseWish peel = OpponentChaseSteer.Decide(around);
            Vector3 face = peel.Face;
            face.y = 0f;
            Vector3 aim = new Vector3(0f, 0f, 1f);
            float dot = face.sqrMagnitude > 1e-6f ? Vector3.Dot(face.normalized, aim) : 1f;
            skip = peel.Verb != OpponentChaseVerb.PadTake && !peel.Jump && !peel.Lunge && dot < 0.95f;

            OpponentChaseInput plain = Blank();
            plain.PlanarDistance = 12f;
            OpponentChaseWish sprint = OpponentChaseSteer.Decide(plain);
            if (sprint.Verb != OpponentChaseVerb.Sprint || sprint.Jump)
                take = false;
        }

        static OpponentChaseInput Blank()
        {
            OpponentChaseInput s = default;
            s.Aim = new Vector3(0f, 0f, 1f);
            s.BodyForward = new Vector3(0f, 0f, 1f);
            s.WallDistance = 999f;
            s.Grounded = true;
            s.PlanarDistance = 12f;
            s.FarMeters = 6f;
            s.LipDistance = 999f;
            return s;
        }

        static bool PoseAndLand()
        {
            float vy = VerticalSpeed(DefaultApexMeters, 22f);
            if (!JumpPose.PoseActive(true, false, false, false)) return false;
            if (JumpPose.Extend(vy) > 0.05f) return false;
            if (JumpPose.RootMotion) return false;
            if (JumpPose.ImpulseDelaySeconds != 0f) return false;
            float hang = OpponentChaseSteer.JumpHangSeconds(vy, 22f, 1.5f);
            if (hang < JumpLandTell.MinAirSeconds) return false;
            float age = -1f;
            float air = 0f;
            float weight = 0f;
            JumpLandTell.Note(ref age, ref air, true, false, false, false, false, hang, ref weight);
            if (air + 0.001f < hang) return false;
            JumpLandTell.Note(ref age, ref air, true, true, false, false, false, 1f / 60f, ref weight);
            if (age != 0f || weight <= 0.05f) return false;
            float light = JumpLandTell.Weight(JumpLandTell.MinAirSeconds);
            if (weight <= light) return false;
            return true;
        }

        static bool FeelLocks()
        {
            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (cfg == null) return false;
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
            if (Mathf.Abs(cfg.gravity - 22f) > 0.001f) return false;
            PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
            if (punch == null || Mathf.Abs(punch.reach - 1.55f) > 0.001f) return false;
            return true;
        }

        static bool Wired()
        {
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string pad = Read("Assets/Scripts/Level/LaunchPad.cs");
            string steer = Read("Assets/Scripts/Modes/OpponentChaseSteer.cs");
            string patrol = Read("Assets/Scripts/Modes/DummyPatrol.cs");
            string loco = Read("Assets/Scripts/Art/DummyLocomotor.cs");
            if (motor == null || pad == null || steer == null || patrol == null || loco == null)
                return false;
            if (Count(motor, "_cc.Move(") != 1) return false;
            if (motor.IndexOf("QueueLaunch", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("ApplyQueuedLaunch", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("AirMoveLaunch", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("animator.applyRootMotion = false", StringComparison.Ordinal) < 0) return false;
            string apply = Method(motor, "Vector3 ApplyQueuedLaunch");
            if (apply.Length < 20) return false;
            if (apply.IndexOf("ClearWallBan", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("_cc.Move", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("AddForce", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("jumpSpeed", StringComparison.Ordinal) >= 0) return false;
            string rise = Method(motor, "Vector3 AirMoveLaunch");
            if (rise.IndexOf("AirGravity", StringComparison.Ordinal) < 0) return false;
            if (rise.IndexOf("IntegrateVertical", StringComparison.Ordinal) < 0) return false;
            if (rise.IndexOf("airCrouchFallMult", StringComparison.Ordinal) >= 0) return false;
            if (rise.IndexOf("jumpSpeed", StringComparison.Ordinal) >= 0) return false;
            string jump = Method(motor, "void TryJump");
            if (jump.IndexOf("_launchArc", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("TickWallContactGates", StringComparison.Ordinal) < 0) return false;
            if (Method(motor, "void TickWallContactGates").IndexOf("ClearWallBan", StringComparison.Ordinal) < 0)
                return false;
            if (pad.IndexOf("apexHeight", StringComparison.Ordinal) < 0) return false;
            if (pad.IndexOf("horizontalDir", StringComparison.Ordinal) < 0) return false;
            if (pad.IndexOf("horizontalSpeed", StringComparison.Ordinal) < 0) return false;
            if (pad.IndexOf("cooldown", StringComparison.Ordinal) < 0) return false;
            if (pad.IndexOf("OnTriggerEnter", StringComparison.Ordinal) < 0) return false;
            if (pad.IndexOf("isTrigger = true", StringComparison.Ordinal) < 0) return false;
            if (pad.IndexOf("AddForce", StringComparison.Ordinal) >= 0) return false;
            if (pad.IndexOf("Rigidbody", StringComparison.Ordinal) >= 0) return false;
            if (pad.IndexOf("applyRootMotion", StringComparison.Ordinal) >= 0) return false;
            if (steer.IndexOf("PadTake", StringComparison.Ordinal) < 0) return false;
            if (steer.IndexOf("PadHelps", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("QueryChase", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("PadAhead", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("PadHelps", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("LaunchArc", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("JumpPose", StringComparison.Ordinal) < 0) return false;
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
