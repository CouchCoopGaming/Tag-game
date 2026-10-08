using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Parkour shoulder roll when a landing is near terminal fall speed.
    /// Visual only: the velocity handed back is the velocity handed in.
    /// The camera is not rolled. A small mesh dip is the only extra motion.
    /// </summary>
    public static class LandingRollPose
    {
        public const bool RootMotion = false;
        public const bool AddsStun = false;
        public const bool ChangesVelocity = false;

        public const float Terminal = 56.16f;
        public const float Fraction = 0.65f;
        /// <summary>Downward speed, m/s. 0.65 * terminal fall.</summary>
        public const float Threshold = Terminal * Fraction;
        public const float Gravity = 22f;
        public const float FallMult = 1.62f;
        /// <summary>Planar speed under this plays the crouch absorb instead of the shoulder roll.</summary>
        public const float AbsorbPlanar = 1.25f;
        public const float SoftFloor = 3f;

        public const float Seconds = 0.52f;
        public const float AbsorbSeconds = 0.32f;
        /// <summary>Shoulder-contact beat. Dust and the audio hook fire here.</summary>
        public const float DustAt = 0.16f;
        /// <summary>Mesh dip at the deepest beat, meters. The capsule does not move.</summary>
        public const float DipMeters = 0.06f;

        public static float DropMeters()
        {
            float v = Threshold;
            float g = Gravity * FallMult;
            return (v * v) / (2f * g);
        }

        public static bool Triggered(float downwardSpeed)
        {
            return downwardSpeed >= Threshold;
        }

        public static bool Stationary(float planarSpeed)
        {
            float s = planarSpeed < 0f ? -planarSpeed : planarSpeed;
            return s < AbsorbPlanar;
        }

        /// <summary>0 at the soft floor, 1 as the fall reaches the roll gate.</summary>
        public static float SoftScale(float impact)
        {
            if (impact < SoftFloor) return 0f;
            float span = Threshold - SoftFloor;
            float u = span > 0.0001f ? (impact - SoftFloor) / span : 1f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return 0.35f + 0.65f * u;
        }

        /// <summary>Presentation does not rewrite velocity.</summary>
        public static Vector3 KeepVelocity(Vector3 velocity)
        {
            return velocity;
        }

        public static VerbExitSample RollAt(float u, bool shoulderLeft)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            VerbExitSample s;
            if (u < 0.22f)
                s = VerbExitSample.Lerp(Tuck(), Shoulder(), u / 0.22f);
            else if (u < 0.48f)
                s = VerbExitSample.Lerp(Shoulder(), LegsOver(), (u - 0.22f) / 0.26f);
            else if (u < 0.72f)
                s = VerbExitSample.Lerp(LegsOver(), Plant(), (u - 0.48f) / 0.24f);
            else
                s = VerbExitSample.Lerp(Plant(), Rise(), (u - 0.72f) / 0.28f);
            if (shoulderLeft)
                s = VerbExitSample.Mirror(s);
            return s;
        }

        public static VerbExitSample AbsorbAt(float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            VerbExitSample deep = Crouch();
            VerbExitSample up = Rise();
            up.ThighL = 14f;
            up.ThighR = 10f;
            up.KneeL = -12f;
            up.KneeR = -10f;
            if (u < 0.45f)
                return VerbExitSample.Lerp(deep, deep, 1f);
            return VerbExitSample.Lerp(deep, up, (u - 0.45f) / 0.55f);
        }

        /// <summary>Knees to the chest, arms wrapped, chin in.</summary>
        static VerbExitSample Tuck()
        {
            VerbExitSample s = default;
            s.Hip = 28f;
            s.Spine = 34f;
            s.Head = -18f;
            s.ThighL = 96f;
            s.ThighR = 92f;
            s.KneeL = -128f;
            s.KneeR = -122f;
            s.ArmPitchL = -24f;
            s.ArmPitchR = -18f;
            s.ArmYawL = 10f;
            s.ArmYawR = -14f;
            s.ElbowL = -108f;
            s.ElbowR = -102f;
            s.RootPitch = 8f;
            s.Drop = 0.02f;
            return s;
        }

        /// <summary>Right shoulder to the ground, spine rolled, that arm planted.</summary>
        static VerbExitSample Shoulder()
        {
            VerbExitSample s = default;
            s.Hip = 18f;
            s.HipYaw = 16f;
            s.HipRoll = -10f;
            s.Spine = 16f;
            s.SpineRoll = 48f;
            s.SpineYaw = 12f;
            s.Head = 6f;
            s.HeadYaw = 10f;
            s.ThighL = 88f;
            s.ThighR = 70f;
            s.KneeL = -118f;
            s.KneeR = -100f;
            s.ArmPitchL = -20f;
            s.ArmPitchR = 62f;
            s.ArmYawL = 16f;
            s.ArmYawR = -8f;
            s.ArmRollR = 22f;
            s.ElbowL = -80f;
            s.ElbowR = -16f;
            s.RootPitch = 18f;
            s.RootRoll = 16f;
            s.Drop = 0.05f;
            return s;
        }

        /// <summary>Hips through, both knees still tucked, chest coming over.</summary>
        static VerbExitSample LegsOver()
        {
            VerbExitSample s = default;
            s.Hip = 64f;
            s.HipYaw = -8f;
            s.Spine = -22f;
            s.SpineRoll = 28f;
            s.Head = 8f;
            s.ThighL = 128f;
            s.ThighR = 120f;
            s.KneeL = -108f;
            s.KneeR = -102f;
            s.ThighYawR = 14f;
            s.ArmPitchL = 24f;
            s.ArmPitchR = 48f;
            s.ArmYawL = 20f;
            s.ArmYawR = -12f;
            s.ElbowL = -36f;
            s.ElbowR = -22f;
            s.RootPitch = 26f;
            s.RootRoll = 8f;
            s.Drop = DipMeters;
            return s;
        }

        /// <summary>Lead foot and the free hand find the ground.</summary>
        static VerbExitSample Plant()
        {
            VerbExitSample s = default;
            s.Hip = 16f;
            s.Spine = 8f;
            s.Head = -2f;
            s.ThighL = 46f;
            s.ThighR = 18f;
            s.KneeL = -36f;
            s.KneeR = -22f;
            s.ArmPitchL = 36f;
            s.ArmPitchR = -16f;
            s.ArmYawL = 8f;
            s.ArmYawR = 14f;
            s.ElbowL = -14f;
            s.ElbowR = -28f;
            s.RootPitch = 10f;
            s.Drop = 0.03f;
            return s;
        }

        /// <summary>Up into the stride. Root tip is gone.</summary>
        static VerbExitSample Rise()
        {
            VerbExitSample s = default;
            s.Hip = 4f;
            s.Spine = 2f;
            s.ThighL = 26f;
            s.ThighR = 6f;
            s.KneeL = -14f;
            s.KneeR = -8f;
            s.ArmPitchL = -18f;
            s.ArmPitchR = -8f;
            s.ArmYawL = 14f;
            s.ArmYawR = -12f;
            s.ElbowL = -10f;
            s.ElbowR = -8f;
            return s;
        }

        /// <summary>Nearly stopped at the same fall speed. Knees take it. No shoulder roll.</summary>
        static VerbExitSample Crouch()
        {
            VerbExitSample s = default;
            s.Hip = 42f;
            s.Spine = 26f;
            s.Head = 4f;
            s.ThighL = 78f;
            s.ThighR = 74f;
            s.KneeL = -122f;
            s.KneeR = -118f;
            s.ArmPitchL = 28f;
            s.ArmPitchR = -16f;
            s.ArmYawL = 12f;
            s.ArmYawR = 18f;
            s.ElbowL = -18f;
            s.ElbowR = -36f;
            s.Drop = 0.05f;
            s.RootPitch = 4f;
            return s;
        }

        public static bool Holds()
        {
            if (RootMotion || AddsStun || ChangesVelocity) return false;
            if (Mathf.Abs(Terminal - 56.16f) > 0.001f) return false;
            if (Mathf.Abs(Fraction - 0.65f) > 0.001f) return false;
            if (Mathf.Abs(Threshold - Terminal * Fraction) > 0.001f) return false;
            if (Mathf.Abs(Gravity - 22f) > 0.001f) return false;
            if (Mathf.Abs(FallMult - 1.62f) > 0.001f) return false;
            if (Triggered(Threshold - 0.05f)) return false;
            if (!Triggered(Threshold)) return false;
            if (!Triggered(Terminal)) return false;
            if (Triggered(10f) || Triggered(0f)) return false;
            float drop = DropMeters();
            if (drop < 18.2f || drop > 19.2f) return false;
            if (!Stationary(0f) || !Stationary(1.24f)) return false;
            if (Stationary(AbsorbPlanar) || Stationary(8f)) return false;
            if (SoftScale(SoftFloor - 0.2f) > 0.0001f) return false;
            if (SoftScale(SoftFloor) < 0.34f) return false;
            if (SoftScale(Threshold * 0.5f) <= SoftScale(SoftFloor)) return false;
            if (Seconds < 0.45f || Seconds > 0.60f) return false;
            if (AbsorbSeconds < 0.15f || AbsorbSeconds > 0.35f) return false;
            if (DipMeters > 0.08f) return false;

            Vector3 v = new Vector3(4.5f, -Threshold, 1.25f);
            Vector3 kept = KeepVelocity(v);
            if (kept.x != v.x || kept.y != v.y || kept.z != v.z) return false;

            VerbExitSample tuck = RollAt(0f, false);
            VerbExitSample shoulder = RollAt(0.22f, false);
            VerbExitSample over = RollAt(0.5f, false);
            VerbExitSample plant = RollAt(0.72f, false);
            VerbExitSample rise = RollAt(1f, false);
            if (tuck.KneeL > -100f || tuck.ElbowL > -80f) return false;
            if (shoulder.SpineRoll < 40f || shoulder.ArmPitchR < 40f) return false;
            if (over.ThighL < 110f || over.Hip < 40f) return false;
            if (plant.ThighL < 30f) return false;
            if (rise.RootPitch > 1f || rise.ThighL > 40f) return false;
            VerbExitSample left = RollAt(0.22f, true);
            if (left.SpineRoll > -40f || left.ArmPitchL < 40f) return false;
            VerbExitSample absorb = AbsorbAt(0.2f);
            if (absorb.KneeL > -100f || absorb.SpineRoll > 5f) return false;
            if (VerbExitSample.Gap(over, absorb) < 80f) return false;
            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Vector3 v = new Vector3(4.5f, -Threshold, 1.25f);
            Vector3 kept = KeepVelocity(v);
            float delta = Mathf.Abs(kept.x - v.x) + Mathf.Abs(kept.y - v.y) + Mathf.Abs(kept.z - v.z);
            return "landing-roll"
                + " threshold=" + Threshold.ToString("0.00")
                + " fraction=" + Fraction.ToString("0.00")
                + " terminal=" + Terminal.ToString("0.00")
                + " drop=" + DropMeters().ToString("0.00") + "m"
                + " seconds=" + Seconds.ToString("0.00")
                + " absorb=" + AbsorbSeconds.ToString("0.00")
                + " absorb-planar=" + AbsorbPlanar.ToString("0.00")
                + " velocity-delta=" + delta.ToString("0.00")
                + " stun=0"
                + " camera-roll=0"
                + " dip=" + DipMeters.ToString("0.00") + "m";
        }
    }
}
