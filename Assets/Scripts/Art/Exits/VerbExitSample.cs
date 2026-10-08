namespace Tag.Art
{
    /// <summary>
    /// Euler offsets from the bind pose, degrees. The rider multiplies these
    /// onto the rest rotation. Nothing here moves the capsule.
    /// Positive hip and spine pitch the chest forward. Positive thigh reaches +Z.
    /// Negative knee bends. Negative arm pitch reaches forward.
    /// </summary>
    public struct VerbExitSample
    {
        public float Hip, HipYaw, HipRoll;
        public float Spine, SpineYaw, SpineRoll;
        public float Head, HeadYaw;
        public float ThighL, ThighR, KneeL, KneeR;
        public float ThighYawL, ThighYawR;
        public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
        public float ElbowL, ElbowR;
        public float FootL, FootR;
        public float RootPitch, RootRoll, Drop;

        public static VerbExitSample Lerp(VerbExitSample a, VerbExitSample b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            float u = 1f - t;
            VerbExitSample s;
            s.Hip = a.Hip * u + b.Hip * t;
            s.HipYaw = a.HipYaw * u + b.HipYaw * t;
            s.HipRoll = a.HipRoll * u + b.HipRoll * t;
            s.Spine = a.Spine * u + b.Spine * t;
            s.SpineYaw = a.SpineYaw * u + b.SpineYaw * t;
            s.SpineRoll = a.SpineRoll * u + b.SpineRoll * t;
            s.Head = a.Head * u + b.Head * t;
            s.HeadYaw = a.HeadYaw * u + b.HeadYaw * t;
            s.ThighL = a.ThighL * u + b.ThighL * t;
            s.ThighR = a.ThighR * u + b.ThighR * t;
            s.KneeL = a.KneeL * u + b.KneeL * t;
            s.KneeR = a.KneeR * u + b.KneeR * t;
            s.ThighYawL = a.ThighYawL * u + b.ThighYawL * t;
            s.ThighYawR = a.ThighYawR * u + b.ThighYawR * t;
            s.ArmPitchL = a.ArmPitchL * u + b.ArmPitchL * t;
            s.ArmPitchR = a.ArmPitchR * u + b.ArmPitchR * t;
            s.ArmYawL = a.ArmYawL * u + b.ArmYawL * t;
            s.ArmYawR = a.ArmYawR * u + b.ArmYawR * t;
            s.ArmRollL = a.ArmRollL * u + b.ArmRollL * t;
            s.ArmRollR = a.ArmRollR * u + b.ArmRollR * t;
            s.ElbowL = a.ElbowL * u + b.ElbowL * t;
            s.ElbowR = a.ElbowR * u + b.ElbowR * t;
            s.FootL = a.FootL * u + b.FootL * t;
            s.FootR = a.FootR * u + b.FootR * t;
            s.RootPitch = a.RootPitch * u + b.RootPitch * t;
            s.RootRoll = a.RootRoll * u + b.RootRoll * t;
            s.Drop = a.Drop * u + b.Drop * t;
            return s;
        }

        /// <summary>Left shoulder, or the left hand leading. Yaw and roll flip. Pitch stays.</summary>
        public static VerbExitSample Mirror(VerbExitSample s)
        {
            float thighL = s.ThighL;
            float kneeL = s.KneeL;
            float thighYawL = s.ThighYawL;
            float armPitchL = s.ArmPitchL;
            float armYawL = s.ArmYawL;
            float armRollL = s.ArmRollL;
            float elbowL = s.ElbowL;
            float footL = s.FootL;
            s.ThighL = s.ThighR;
            s.ThighR = thighL;
            s.KneeL = s.KneeR;
            s.KneeR = kneeL;
            s.ThighYawL = -s.ThighYawR;
            s.ThighYawR = -thighYawL;
            s.ArmPitchL = s.ArmPitchR;
            s.ArmPitchR = armPitchL;
            s.ArmYawL = -s.ArmYawR;
            s.ArmYawR = -armYawL;
            s.ArmRollL = -s.ArmRollR;
            s.ArmRollR = -armRollL;
            s.ElbowL = s.ElbowR;
            s.ElbowR = elbowL;
            s.FootL = s.FootR;
            s.FootR = footL;
            s.HipYaw = -s.HipYaw;
            s.HipRoll = -s.HipRoll;
            s.SpineYaw = -s.SpineYaw;
            s.SpineRoll = -s.SpineRoll;
            s.HeadYaw = -s.HeadYaw;
            s.RootRoll = -s.RootRoll;
            return s;
        }

        /// <summary>Soft-land depth. Arms stay readable. 1 is the full knee bend.</summary>
        public static VerbExitSample ScaleBend(VerbExitSample s, float scale)
        {
            if (scale < 0f) scale = 0f;
            if (scale > 1f) scale = 1f;
            s.Hip *= scale;
            s.Spine *= scale;
            s.Head *= scale;
            s.ThighL *= scale;
            s.ThighR *= scale;
            s.KneeL *= scale;
            s.KneeR *= scale;
            s.Drop *= scale;
            s.RootPitch *= scale;
            return s;
        }

        /// <summary>Sum of absolute channel gaps. Used to prove two exits are not the same pose.</summary>
        public static float Gap(VerbExitSample a, VerbExitSample b)
        {
            float d = 0f;
            d += Abs(a.Hip - b.Hip);
            d += Abs(a.Spine - b.Spine);
            d += Abs(a.Head - b.Head);
            d += Abs(a.HipYaw - b.HipYaw);
            d += Abs(a.SpineYaw - b.SpineYaw);
            d += Abs(a.SpineRoll - b.SpineRoll);
            d += Abs(a.ThighL - b.ThighL);
            d += Abs(a.ThighR - b.ThighR);
            d += Abs(a.KneeL - b.KneeL);
            d += Abs(a.KneeR - b.KneeR);
            d += Abs(a.ArmPitchL - b.ArmPitchL);
            d += Abs(a.ArmPitchR - b.ArmPitchR);
            d += Abs(a.ArmYawL - b.ArmYawL);
            d += Abs(a.ArmYawR - b.ArmYawR);
            d += Abs(a.ElbowL - b.ElbowL);
            d += Abs(a.ElbowR - b.ElbowR);
            d += Abs(a.RootPitch - b.RootPitch);
            d += Abs(a.RootRoll - b.RootRoll);
            return d;
        }

        static float Abs(float v)
        {
            return v < 0f ? -v : v;
        }
    }
}
