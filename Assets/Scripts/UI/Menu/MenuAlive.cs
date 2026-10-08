using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Menu-only poses. The hip shell already sits in the thigh at rest.
    /// Life is a whole-body lean, a look, a breath, and a small weight shift.
    /// The march keeps the chest up. A raised arm, a clapped elbow, and a
    /// vault knee sink on this rig.
    /// </summary>
    public static class MenuAlive
    {
        public struct Angles
        {
            public float RootPitch, RootYaw, RootRoll;
            public float Hip, HipYaw, HipRoll;
            public float Spine, SpineYaw, SpineRoll;
            public float Head, HeadYaw;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float ThighL, ThighR, KneeL, KneeR;
        }

        /// <summary>Breath on the chest, a look, and a shift onto one leg.</summary>
        public static Angles Idle(float shift, float breath)
        {
            float s = Mathf.Sin(shift);
            float b = Mathf.Sin(breath);
            float loadL = s > 0f ? s : 0f;
            float loadR = s < 0f ? -s : 0f;
            var a = new Angles();
            a.RootRoll = 8f * s;
            a.Spine = 2.2f * b;
            a.HeadYaw = 12f * s;
            a.ArmPitchL = 1.2f * b;
            a.ArmPitchR = 1.2f * b;
            a.ThighL = 1.6f * loadL;
            a.ThighR = 1.6f * loadR;
            a.KneeL = -2.2f - loadL;
            a.KneeR = -2.2f - loadR;
            return a;
        }

        /// <summary>Alert settle. Knees soft, arms a little out, chest open.</summary>
        public static Angles Ready()
        {
            var a = new Angles();
            a.RootPitch = -10f;
            a.Spine = -2f;
            a.KneeL = -3f;
            a.KneeR = -3f;
            a.ThighL = 1.5f;
            a.ThighR = 1.5f;
            a.ElbowL = -3f;
            a.ElbowR = -3f;
            a.ArmYawL = -5f;
            a.ArmYawR = 5f;
            a.ArmPitchL = -1.5f;
            a.ArmPitchR = -1.5f;
            return a;
        }

        public static Angles Lerp(Angles a, Angles b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            float u = 1f - t;
            var o = new Angles();
            o.RootPitch = a.RootPitch * u + b.RootPitch * t;
            o.RootYaw = a.RootYaw * u + b.RootYaw * t;
            o.RootRoll = a.RootRoll * u + b.RootRoll * t;
            o.Hip = a.Hip * u + b.Hip * t;
            o.HipYaw = a.HipYaw * u + b.HipYaw * t;
            o.HipRoll = a.HipRoll * u + b.HipRoll * t;
            o.Spine = a.Spine * u + b.Spine * t;
            o.SpineYaw = a.SpineYaw * u + b.SpineYaw * t;
            o.SpineRoll = a.SpineRoll * u + b.SpineRoll * t;
            o.Head = a.Head * u + b.Head * t;
            o.HeadYaw = a.HeadYaw * u + b.HeadYaw * t;
            o.ArmPitchL = a.ArmPitchL * u + b.ArmPitchL * t;
            o.ArmPitchR = a.ArmPitchR * u + b.ArmPitchR * t;
            o.ArmYawL = a.ArmYawL * u + b.ArmYawL * t;
            o.ArmYawR = a.ArmYawR * u + b.ArmYawR * t;
            o.ArmRollL = a.ArmRollL * u + b.ArmRollL * t;
            o.ArmRollR = a.ArmRollR * u + b.ArmRollR * t;
            o.ElbowL = a.ElbowL * u + b.ElbowL * t;
            o.ElbowR = a.ElbowR * u + b.ElbowR * t;
            o.ThighL = a.ThighL * u + b.ThighL * t;
            o.ThighR = a.ThighR * u + b.ThighR * t;
            o.KneeL = a.KneeL * u + b.KneeL * t;
            o.KneeR = a.KneeR * u + b.KneeR * t;
            return o;
        }

        /// <summary>
        /// March. The chest stays up and the chin stays back over it.
        /// The body still rocks into the step. The thighs stay small.
        /// </summary>
        public static Angles Run(float age)
        {
            float s = Mathf.Sin(age * 2.4f);
            float loadL = s > 0f ? s : 0f;
            float loadR = s < 0f ? -s : 0f;
            var a = new Angles();
            a.RootPitch = 4f + 4f * s;
            a.RootRoll = 5f * s;
            a.ThighL = 2.2f * s;
            a.ThighR = -2.2f * s;
            a.KneeL = -2.2f - loadL;
            a.KneeR = -2.2f - loadR;
            a.ArmPitchL = -1.8f * s;
            a.ArmPitchR = 1.8f * s;
            a.ElbowL = -2f;
            a.ElbowR = -2f;
            a.HeadYaw = 6f * s;
            a.Head = -1.2f;
            a.Spine = -3f;
            return a;
        }

        /// <summary>
        /// A step in place of a vault. The lead knee cannot tuck on this rig.
        /// </summary>
        public static Angles Step(float age)
        {
            float u = age * 0.28f;
            u = u - (float)System.Math.Floor(u);
            if (u < 0f) u += 1f;
            float s = Mathf.Sin(u * 3.14159265f);
            var a = new Angles();
            a.RootPitch = -14f * s;
            a.ThighL = 2.2f * s;
            a.ThighR = -1.5f * s;
            a.KneeL = -2f - s;
            a.KneeR = -2f;
            a.ArmPitchL = -1.5f * s;
            a.ArmPitchR = -1.5f * s;
            a.Spine = -1.5f * s;
            return a;
        }

        /// <summary>
        /// Winner. The whole body leans back. An arm raise sinks the shoulder
        /// into the chest on this rig, so the arms only open a little.
        /// lean is 1 for the winner and smaller for the places beside them.
        /// </summary>
        public static Angles Cheer(float t, float lean)
        {
            if (lean < 0f) lean = 0f;
            if (lean > 1f) lean = 1f;
            float s = Mathf.Sin(t * 2.1f);
            var a = new Angles();
            a.RootPitch = -16f * lean;
            a.HeadYaw = 10f * s * lean;
            a.Spine = -2f * lean;
            a.ArmPitchL = -1.5f * lean;
            a.ArmPitchR = -1.5f * lean;
            a.ArmYawL = -4f * lean;
            a.ArmYawR = 4f * lean;
            a.ElbowL = -2f * lean;
            a.ElbowR = -2f * lean;
            return a;
        }

        /// <summary>Last place. A bow. A chin tuck past this sinks the head.</summary>
        public static Angles Slump(float t)
        {
            float s = Mathf.Sin(t * 1.1f);
            var a = new Angles();
            a.RootPitch = 16f;
            a.RootRoll = 3f * s;
            a.Spine = 2f;
            a.Head = 1f;
            a.HeadYaw = -8f;
            a.KneeL = -3f;
            a.KneeR = -3f;
            a.ArmPitchL = 1.5f;
            a.ArmPitchR = 1.5f;
            return a;
        }
    }
}
