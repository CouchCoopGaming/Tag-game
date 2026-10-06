using Tag.Art;

namespace Tag.Practice
{
    /// <summary>
    /// Ghost pose weights. Each clip is one the pawn already plays.
    /// Nothing here writes a root or a velocity.
    /// </summary>
    public static class PracticePose
    {
        public const bool RootMotion = false;

        public static string Clip(byte verb)
        {
            switch (verb)
            {
                case PracticeVerb.WallRun:
                case PracticeVerb.Cling: return "WallPose";
                case PracticeVerb.WallJump: return "WallJumpPose";
                case PracticeVerb.AirDash: return "AirDashPose";
                case PracticeVerb.Zip: return "ZipPose";
                case PracticeVerb.Pad: return "LaunchPose";
                case PracticeVerb.Jump: return "JumpPose";
                case PracticeVerb.Slide: return "CrouchPose";
                default: return "IdlePose";
            }
        }

        public static float Weight(byte verb, float age, float vertical, float speed)
        {
            switch (verb)
            {
                case PracticeVerb.WallRun:
                case PracticeVerb.Cling:
                    return WallPose.RunRate(speed);
                case PracticeVerb.WallJump:
                    return WallJumpPose.JumpWeight(age);
                case PracticeVerb.AirDash:
                    return AirDashPose.DashWeight(age);
                case PracticeVerb.Zip:
                    return ZipPose.CatchWeight(age);
                case PracticeVerb.Pad:
                    return LaunchPose.OpenAmount(vertical);
                case PracticeVerb.Jump:
                    return JumpPose.TakeoffWeight(age);
                case PracticeVerb.Slide:
                    return CrouchPose.BlendSeconds > 0f ? 1f : 0f;
                default:
                    return IdlePose.Weight(speed, 0f, 0f, 0f);
            }
        }

        public static bool ClipsHold()
        {
            if (IdlePose.RootMotion || JumpPose.RootMotion || WallPose.RootMotion) return false;
            if (WallJumpPose.RootMotion || AirDashPose.RootMotion || ZipPose.RootMotion) return false;
            if (LaunchPose.RootMotion || CrouchPose.RootMotion) return false;
            if (RootMotion) return false;
            float idle = Weight(PracticeVerb.None, 0f, 0f, 0f);
            float dash = Weight(PracticeVerb.AirDash, 0.02f, 0f, 15f);
            float wall = Weight(PracticeVerb.WallRun, 0.1f, 0f, 9.5f);
            float zip = Weight(PracticeVerb.Zip, 0.05f, 0f, 14f);
            if (float.IsNaN(idle) || float.IsNaN(dash) || float.IsNaN(wall) || float.IsNaN(zip)) return false;
            if (Clip(PracticeVerb.WallRun) != "WallPose") return false;
            if (Clip(PracticeVerb.AirDash) != "AirDashPose") return false;
            if (Clip(PracticeVerb.Zip) != "ZipPose") return false;
            if (Clip(PracticeVerb.Pad) != "LaunchPose") return false;
            return true;
        }
    }
}
