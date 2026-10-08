using System;
using System.Globalization;
using Tag.Art;

namespace Tag.NoClipDump
{
    /// <summary>
    /// Bone offsets at 30 fps for the clips this animation lane owns.
    /// Channels are degrees on top of the bind pose. No gameplay numbers.
    /// </summary>
    static class Program
    {
        const float Fps = 30f;

        struct Frame
        {
            public float Hip, HipYaw, HipRoll, Spine, SpineYaw, SpineRoll, Head, HeadYaw, HeadRoll;
            public float ThighL, ThighYawL, ThighR, ThighYawR, KneeL, KneeR;
            public float ThighRollL, ThighRollR;
            public float ArmPitchL, ArmYawL, ArmRollL, ArmPitchR, ArmYawR, ArmRollR;
            public float ElbowL, ElbowR, FootL, FootR, Drop;
        }

        static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            foreach (VerbExitId id in VerbExitClock.Catalog)
            {
                float dur = VerbExitClock.Duration(id);
                Emit("exit-" + id, dur, SolidForExit(id), t => FromExit(id, dur > 0.0001f ? t / dur : 0f));
            }

            float runPeriod = (float)(2.0 * Math.PI / WallPose.RunCadenceFull);
            Emit("wall-run", runPeriod, "wall", t => FromWall(WallPose.RunCycle(WallPose.RunCadenceFull * t, true)));

            float jumpDur = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            Emit("wall-jump", jumpDur, "wall", t => FromWall(WallJumpPose.At(t, 8f, true, 6f)));

            Emit("vault", 0.40f, "box", t => FromMantle(MantlePose.At(t / 0.40f, true)));

            Emit("zip-catch", ZipPose.CatchSeconds, "zip", t =>
            {
                float w = ZipPose.CatchWeight(t);
                return LerpZip(FromZip(ZipPose.JumpDrop()), FromZip(ZipPose.Hang()), w);
            });
            Emit("zip-ride", 0.40f, "zip", t =>
            {
                Frame f = FromZip(ZipPose.Hang());
                f.SpineRoll = ZipPose.Sway(t, 8f);
                f.HipRoll = -f.SpineRoll * WallPose.HipRollShare;
                f.HeadRoll = -f.SpineRoll * WallPose.HeadRollShare;
                return f;
            });
            Emit("zip-release", ZipPose.ReleaseSeconds, "zip", t =>
            {
                float w = ZipPose.ReleaseWeight(t);
                return LerpZip(FromZip(ZipPose.Release()), FromZip(ZipPose.Hang()), w);
            });

            Emit("grapple-aim", GrapplePose.AimBlendSeconds, "rope", t =>
            {
                float w = GrapplePose.AimWeight(t);
                return LerpZip(new Frame(), FromGrapple(GrapplePose.ForBody(GrapplePose.Aim(10f, 0f))), w);
            });
            Emit("grapple-latch", GrapplePose.LatchSnapSeconds, "rope", t =>
                FromGrapple(GrapplePose.ForBody(GrapplePose.Latched(0f, -4f, 0f, t))));
            float pullPeriod = 0.45f;
            Emit("grapple-pull", pullPeriod, "rope", t =>
            {
                float phase = (float)Math.Sin(t / pullPeriod * Math.PI * 2.0);
                return FromGrapple(GrapplePose.ForBody(GrapplePose.Pull(phase, -4f, 12f)));
            });
            Emit("grapple-release", GrapplePose.ReleaseBlendSeconds, "rope", t =>
                FromGrapple(GrapplePose.ForBody(GrapplePose.Release(0f, -6f, 0f, t))));
            Emit("grapple-miss", GrappleMissTell.FlashSeconds, "rope", t =>
                FromGrapple(GrapplePose.ForBody(GrapplePose.Miss(8f, 0f, t))));

            Emit("slide", VerbPoseClips.SlideBlendSeconds + 0.30f, "ground", t =>
            {
                float u = t / VerbPoseClips.SlideBlendSeconds;
                if (u > 1f) u = 1f;
                float w = u * u * (3f - 2f * u);
                Frame slide = SlideFrame();
                slide.Drop = VerbPoseClips.SlideBodyDrop * w;
                return Scale(slide, w);
            });

            float punchStrike = 0.22f;
            Emit("punch", punchStrike, "ground", t => PunchFrame(Smooth(t / punchStrike)));
            Emit("punch-recover", 0.15f, "ground", t => PunchRecover(t / 0.15f));
            Emit("tag", 0.20f, "ground", t => TagFrame(Smooth(t / 0.20f)));
            Emit("stagger", PunchStaggerPose.Duration, "ground", t =>
            {
                float w = PunchStaggerPose.Weight(t);
                return Scale(StaggerFrame(), w);
            });
        }

        static string SolidForExit(VerbExitId id)
        {
            switch (id)
            {
                case VerbExitId.WallRun:
                case VerbExitId.WallJump:
                case VerbExitId.ClingDrop:
                    return "wall";
                case VerbExitId.Vault:
                case VerbExitId.Mantle:
                case VerbExitId.ClimbTopOut:
                    return "box";
                case VerbExitId.ZipDrop:
                    return "zip";
                case VerbExitId.GrappleArrive:
                case VerbExitId.GrappleRelease:
                    return "rope";
                default:
                    return "ground";
            }
        }

        static void Emit(string name, float duration, string solid, Func<float, Frame> at)
        {
            if (duration < 1f / Fps) duration = 1f / Fps;
            int steps = (int)Math.Ceiling(duration * Fps - 1e-4f);
            Console.WriteLine("CLIP " + name + " " + duration.ToString("0.000") + " " + (steps + 1) + " " + solid);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / Fps;
                if (t > duration) t = duration;
                Frame f = at(t);
                Console.WriteLine("F " + t.ToString("0.000") + " " + Pack(f));
            }
        }

        static Frame FromExit(VerbExitId id, float u)
        {
            VerbExitSample s = VerbExitClips.At(id, u, 1f, false, false);
            return new Frame
            {
                Hip = s.Hip, HipYaw = s.HipYaw, HipRoll = s.HipRoll,
                Spine = s.Spine, SpineYaw = s.SpineYaw, SpineRoll = s.SpineRoll,
                Head = s.Head, HeadYaw = s.HeadYaw,
                ThighL = s.ThighL, ThighYawL = s.ThighYawL, ThighR = s.ThighR, ThighYawR = s.ThighYawR,
                ThighRollL = s.ThighRollL, ThighRollR = s.ThighRollR,
                KneeL = s.KneeL, KneeR = s.KneeR,
                ArmPitchL = s.ArmPitchL, ArmYawL = s.ArmYawL, ArmRollL = s.ArmRollL,
                ArmPitchR = s.ArmPitchR, ArmYawR = s.ArmYawR, ArmRollR = s.ArmRollR,
                ElbowL = s.ElbowL, ElbowR = s.ElbowR,
                FootL = s.FootL, FootR = s.FootR,
                Drop = s.Drop,
            };
        }

        static Frame FromWall(WallPose.Sample s)
        {
            return new Frame
            {
                Hip = s.Hip,
                HipRoll = -s.LeanZ * WallPose.HipRollShare,
                Spine = s.Spine,
                SpineRoll = s.LeanZ,
                Head = s.Head,
                HeadRoll = -s.LeanZ * WallPose.HeadRollShare,
                ThighL = s.ThighL, ThighR = s.ThighR, KneeL = s.KneeL, KneeR = s.KneeR,
                ThighRollL = s.ThighRollL, ThighRollR = s.ThighRollR,
                ArmPitchL = s.ArmPitchL, ArmYawL = s.ArmYawL, ArmPitchR = s.ArmPitchR, ArmYawR = s.ArmYawR,
                ElbowL = s.ElbowL, ElbowR = s.ElbowR, FootL = s.FootL, FootR = s.FootR,
            };
        }

        static Frame FromWall(WallJumpPose.Sample s)
        {
            return new Frame
            {
                Hip = s.Hip,
                HipRoll = -s.LeanZ * WallPose.HipRollShare,
                Spine = s.Spine,
                SpineRoll = s.LeanZ,
                Head = s.Head,
                HeadRoll = -s.LeanZ * WallPose.HeadRollShare,
                ThighL = s.ThighL, ThighR = s.ThighR, KneeL = s.KneeL, KneeR = s.KneeR,
                ThighRollL = s.ThighRollL, ThighRollR = s.ThighRollR,
                ArmPitchL = s.ArmPitchL, ArmYawL = s.ArmYawL, ArmPitchR = s.ArmPitchR, ArmYawR = s.ArmYawR,
                ElbowL = s.ElbowL, ElbowR = s.ElbowR,
            };
        }

        static Frame FromMantle(MantlePose.Sample s)
        {
            return new Frame
            {
                Hip = s.Hip, Spine = s.Spine, Head = s.Head,
                ThighL = s.ThighL, ThighR = s.ThighR, KneeL = s.KneeL, KneeR = s.KneeR,
                ThighRollL = s.ThighRollL, ThighRollR = s.ThighRollR,
                ArmPitchL = s.ArmPitchL, ArmYawL = s.ArmYawL, ArmPitchR = s.ArmPitchR, ArmYawR = s.ArmYawR,
                ElbowL = s.ElbowL, ElbowR = s.ElbowR,
            };
        }

        static Frame FromZip(ZipPose.Sample s)
        {
            return new Frame
            {
                Hip = s.Hip,
                HipRoll = -s.LeanZ * WallPose.HipRollShare,
                Spine = s.Spine,
                SpineRoll = s.LeanZ,
                Head = s.Head,
                HeadRoll = -s.LeanZ * WallPose.HeadRollShare,
                ThighL = s.ThighL, ThighR = s.ThighR, KneeL = s.KneeL, KneeR = s.KneeR,
                ArmPitchL = s.ArmPitchL, ArmYawL = s.ArmYawL, ArmPitchR = s.ArmPitchR, ArmYawR = s.ArmYawR,
                ElbowL = s.ElbowL, ElbowR = s.ElbowR,
            };
        }

        static Frame FromGrapple(GrapplePose.Sample s)
        {
            return new Frame
            {
                Hip = s.Hip, HipYaw = s.HipYaw, Spine = s.Spine, SpineYaw = s.SpineYaw,
                Head = s.Head, HeadYaw = s.HeadYaw,
                ThighL = s.ThighL, ThighR = s.ThighR, KneeL = s.KneeL, KneeR = s.KneeR,
                ArmPitchL = s.ArmPitchL, ArmYawL = s.ArmYawL, ArmPitchR = s.ArmPitchR, ArmYawR = s.ArmYawR,
                ElbowL = s.ElbowL, ElbowR = s.ElbowR,
            };
        }

        static Frame SlideFrame()
        {
            return new Frame
            {
                Hip = VerbPoseClips.SlideHip,
                Spine = VerbPoseClips.SlideSpine,
                Head = VerbPoseClips.SlideHead,
                ThighL = VerbPoseClips.SlideLeadThigh,
                ThighYawL = VerbPoseClips.SlideLeadYaw,
                ThighRollL = -20f,
                ThighR = VerbPoseClips.SlideTrailThigh,
                ThighYawR = -VerbPoseClips.SlideTrailYaw,
                ThighRollR = 22f,
                KneeL = VerbPoseClips.SlideLeadKnee,
                KneeR = VerbPoseClips.SlideTrailKnee,
                FootL = VerbPoseClips.SlideLeadFoot,
                FootR = VerbPoseClips.SlideTrailFoot,
                ArmPitchL = VerbPoseClips.SlideLeadArmPitch,
                ArmYawL = VerbPoseClips.SlideLeadArmYaw,
                ArmPitchR = VerbPoseClips.SlideBalanceArmPitch,
                ArmYawR = -VerbPoseClips.SlideBalanceArmYaw,
                ElbowL = VerbPoseClips.SlideLeadElbow,
                ElbowR = VerbPoseClips.SlideBalanceElbow,
                Drop = VerbPoseClips.SlideBodyDrop,
            };
        }

        static float Smooth(float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }

        static Frame PunchFrame(float t)
        {
            return new Frame
            {
                Hip = VerbPoseClips.PunchHipPitch,
                HipYaw = Lerp(VerbPoseClips.PunchCockHipYaw, VerbPoseClips.PunchStrikeHipYaw, t),
                Spine = VerbPoseClips.PunchSpinePitch,
                SpineYaw = Lerp(VerbPoseClips.PunchCockSpineYaw, VerbPoseClips.PunchStrikeSpineYaw, t),
                Head = VerbPoseClips.PunchHeadPitch,
                HeadYaw = Lerp(VerbPoseClips.PunchCockHeadYaw, VerbPoseClips.PunchStrikeHeadYaw, t),
                ThighL = Lerp(VerbPoseClips.PunchCockLeadThigh, VerbPoseClips.PunchStrikeLeadThigh, t),
                ThighR = Lerp(VerbPoseClips.PunchCockTrailThigh, VerbPoseClips.PunchStrikeTrailThigh, t),
                KneeL = Lerp(VerbPoseClips.PunchCockLeadKnee, VerbPoseClips.PunchStrikeLeadKnee, t),
                KneeR = Lerp(VerbPoseClips.PunchCockTrailKnee, VerbPoseClips.PunchStrikeTrailKnee, t),
                ArmPitchL = Lerp(VerbPoseClips.PunchGuardPitchCock, VerbPoseClips.PunchGuardPitchStrike, t),
                ArmYawL = Lerp(VerbPoseClips.PunchGuardYawCock, VerbPoseClips.PunchGuardYawStrike, t),
                ArmRollL = VerbPoseClips.PunchGuardRoll,
                ArmPitchR = Lerp(VerbPoseClips.PunchCockPitch, VerbPoseClips.PunchStrikePitch, t),
                ArmYawR = Lerp(VerbPoseClips.PunchCockYaw, VerbPoseClips.PunchStrikeYaw, t),
                ArmRollR = Lerp(VerbPoseClips.PunchCockRoll, VerbPoseClips.PunchStrikeRoll, t),
                ElbowL = Lerp(VerbPoseClips.PunchGuardElbowCock, VerbPoseClips.PunchGuardElbowStrike, t),
                ElbowR = Lerp(VerbPoseClips.PunchCockElbow, VerbPoseClips.PunchStrikeElbow, t),
            };
        }

        static Frame PunchRecover(float progress)
        {
            float t = VerbPoseClips.RecoverOpen(progress);
            return new Frame
            {
                Hip = VerbPoseClips.PunchHipPitch,
                HipYaw = Lerp(VerbPoseClips.PunchStrikeHipYaw, VerbPoseClips.PunchRecoverHipYaw, t),
                Spine = VerbPoseClips.PunchSpinePitch,
                SpineYaw = Lerp(VerbPoseClips.PunchStrikeSpineYaw, VerbPoseClips.PunchRecoverSpineYaw, t),
                Head = VerbPoseClips.PunchHeadPitch,
                HeadYaw = Lerp(VerbPoseClips.PunchStrikeHeadYaw, VerbPoseClips.PunchRecoverHeadYaw, t),
                ThighL = Lerp(VerbPoseClips.PunchStrikeLeadThigh, VerbPoseClips.PunchRecoverLeadThigh, t),
                ThighR = Lerp(VerbPoseClips.PunchStrikeTrailThigh, VerbPoseClips.PunchRecoverTrailThigh, t),
                KneeL = Lerp(VerbPoseClips.PunchStrikeLeadKnee, VerbPoseClips.PunchRecoverLeadKnee, t),
                KneeR = Lerp(VerbPoseClips.PunchStrikeTrailKnee, VerbPoseClips.PunchRecoverTrailKnee, t),
                ArmPitchL = Lerp(VerbPoseClips.PunchGuardPitchStrike, VerbPoseClips.PunchRecoverGuardPitch, t),
                ArmYawL = Lerp(VerbPoseClips.PunchGuardYawStrike, VerbPoseClips.PunchRecoverGuardYaw, t),
                ArmRollL = VerbPoseClips.PunchGuardRoll,
                ArmPitchR = Lerp(VerbPoseClips.PunchStrikePitch, VerbPoseClips.PunchRecoverPitch, t),
                ArmYawR = Lerp(VerbPoseClips.PunchStrikeYaw, VerbPoseClips.PunchRecoverYaw, t),
                ArmRollR = Lerp(VerbPoseClips.PunchStrikeRoll, VerbPoseClips.PunchRecoverRoll, t),
                ElbowL = Lerp(VerbPoseClips.PunchGuardElbowStrike, VerbPoseClips.PunchRecoverGuardElbow, t),
                ElbowR = Lerp(VerbPoseClips.PunchStrikeElbow, VerbPoseClips.PunchRecoverElbow, t),
            };
        }

        static Frame TagFrame(float t)
        {
            float pitch = Lerp(VerbPoseClips.TagWindupArmPitch, VerbPoseClips.TagArmPitch, t);
            float yaw = Lerp(VerbPoseClips.TagWindupArmYaw, VerbPoseClips.TagArmYaw, t);
            float roll = Lerp(VerbPoseClips.TagWindupArmRoll, VerbPoseClips.TagArmRoll, t);
            float elbow = Lerp(VerbPoseClips.TagWindupElbow, VerbPoseClips.TagElbow, t);
            return new Frame
            {
                Hip = Lerp(VerbPoseClips.TagWindupHip, VerbPoseClips.TagHip, t),
                Spine = Lerp(VerbPoseClips.TagWindupSpine, VerbPoseClips.TagSpine, t),
                Head = Lerp(VerbPoseClips.TagWindupHead, VerbPoseClips.TagHead, t),
                ThighL = Lerp(VerbPoseClips.TagWindupThigh, VerbPoseClips.TagThigh, t),
                ThighR = Lerp(VerbPoseClips.TagWindupThigh, VerbPoseClips.TagThigh, t),
                KneeL = Lerp(VerbPoseClips.TagWindupKnee, VerbPoseClips.TagKnee, t),
                KneeR = Lerp(VerbPoseClips.TagWindupKnee, VerbPoseClips.TagKnee, t),
                ArmPitchL = pitch, ArmYawL = yaw, ArmRollL = roll, ElbowL = elbow,
                ArmPitchR = pitch, ArmYawR = -yaw, ArmRollR = -roll, ElbowR = elbow,
            };
        }

        static Frame StaggerFrame()
        {
            return new Frame
            {
                Hip = PunchStaggerPose.Hip, HipYaw = PunchStaggerPose.HipYaw,
                Spine = PunchStaggerPose.Spine, SpineYaw = PunchStaggerPose.SpineYaw,
                Head = PunchStaggerPose.Head, HeadYaw = PunchStaggerPose.HeadYaw,
                ThighL = PunchStaggerPose.ThighL, ThighR = PunchStaggerPose.ThighR,
                KneeL = PunchStaggerPose.KneeL, KneeR = PunchStaggerPose.KneeR,
                ArmPitchL = PunchStaggerPose.ArmPitchL, ArmYawL = PunchStaggerPose.ArmYawL, ArmRollL = PunchStaggerPose.ArmRollL,
                ArmPitchR = PunchStaggerPose.ArmPitchR, ArmYawR = PunchStaggerPose.ArmYawR, ArmRollR = PunchStaggerPose.ArmRollR,
                ElbowL = PunchStaggerPose.ElbowL, ElbowR = PunchStaggerPose.ElbowR,
            };
        }

        static float Lerp(float a, float b, float t) { return a + (b - a) * t; }

        static Frame LerpZip(Frame a, Frame b, float t)
        {
            Frame f = new Frame();
            f.Hip = Lerp(a.Hip, b.Hip, t);
            f.HipYaw = Lerp(a.HipYaw, b.HipYaw, t);
            f.HipRoll = Lerp(a.HipRoll, b.HipRoll, t);
            f.Spine = Lerp(a.Spine, b.Spine, t);
            f.SpineYaw = Lerp(a.SpineYaw, b.SpineYaw, t);
            f.SpineRoll = Lerp(a.SpineRoll, b.SpineRoll, t);
            f.Head = Lerp(a.Head, b.Head, t);
            f.HeadYaw = Lerp(a.HeadYaw, b.HeadYaw, t);
            f.HeadRoll = Lerp(a.HeadRoll, b.HeadRoll, t);
            f.ThighL = Lerp(a.ThighL, b.ThighL, t);
            f.ThighYawL = Lerp(a.ThighYawL, b.ThighYawL, t);
            f.ThighR = Lerp(a.ThighR, b.ThighR, t);
            f.ThighYawR = Lerp(a.ThighYawR, b.ThighYawR, t);
            f.KneeL = Lerp(a.KneeL, b.KneeL, t);
            f.KneeR = Lerp(a.KneeR, b.KneeR, t);
            f.ThighRollL = Lerp(a.ThighRollL, b.ThighRollL, t);
            f.ThighRollR = Lerp(a.ThighRollR, b.ThighRollR, t);
            f.ArmPitchL = Lerp(a.ArmPitchL, b.ArmPitchL, t);
            f.ArmYawL = Lerp(a.ArmYawL, b.ArmYawL, t);
            f.ArmRollL = Lerp(a.ArmRollL, b.ArmRollL, t);
            f.ArmPitchR = Lerp(a.ArmPitchR, b.ArmPitchR, t);
            f.ArmYawR = Lerp(a.ArmYawR, b.ArmYawR, t);
            f.ArmRollR = Lerp(a.ArmRollR, b.ArmRollR, t);
            f.ElbowL = Lerp(a.ElbowL, b.ElbowL, t);
            f.ElbowR = Lerp(a.ElbowR, b.ElbowR, t);
            f.FootL = Lerp(a.FootL, b.FootL, t);
            f.FootR = Lerp(a.FootR, b.FootR, t);
            f.Drop = Lerp(a.Drop, b.Drop, t);
            return f;
        }

        static Frame Scale(Frame a, float w)
        {
            return LerpZip(new Frame(), a, w);
        }

        static string Pack(Frame f)
        {
            float[] v = {
                f.Hip, f.HipYaw, f.HipRoll, f.Spine, f.SpineYaw, f.SpineRoll, f.Head, f.HeadYaw, f.HeadRoll,
                f.ThighL, f.ThighYawL, f.ThighR, f.ThighYawR, f.KneeL, f.KneeR,
                f.ArmPitchL, f.ArmYawL, f.ArmRollL, f.ArmPitchR, f.ArmYawR, f.ArmRollR,
                f.ElbowL, f.ElbowR, f.FootL, f.FootR, f.Drop,
                0f, 0f, 0f, 0f, f.ThighRollL, f.ThighRollR,
            };
            string[] s = new string[v.Length];
            for (int i = 0; i < v.Length; i++) s[i] = v[i].ToString("0.###");
            return string.Join(" ", s);
        }
    }
}
