using System;
using System.Globalization;
using System.IO;
using Tag.Art;
using UnityEngine;

/// <summary>
/// Local pose keys the game applies, sampled at 30 fps from the same
/// static clip classes DummyLocomotor reads. Not the still-renderer poses.
/// </summary>
static class PoseKeyDump
{
    const float Dt = 1f / 30f;

    public static void Write(TextWriter w)
    {
        CultureInfo c = CultureInfo.InvariantCulture;
        w.WriteLine("# ingame pose keys dt=1/30 source=Tag.Art");
        Vault(w, c);
        Climb(w, c);
        Slide(w, c);
        Wall(w, c);
        Roll(w, c);
        Pad(w, c);
        Zip(w, c);
        Grapple(w, c);
        Punch(w, c);
        TagCatch(w, c);
        Stagger(w, c);
        Idle(w, c);
        Loco(w, c, "loco", LocoFeel.Walk);
        Loco(w, c, "sprint", LocoFeel.Sprint);
        Exits(w, c);
    }

    static void Exits(TextWriter w, CultureInfo c)
    {
        for (int k = 0; k < VerbExitClock.Catalog.Length; k++)
        {
            VerbExitId id = VerbExitClock.Catalog[k];
            float dur = VerbExitClock.Duration(id);
            int n = Frames(dur);
            for (int i = 0; i <= n; i++)
            {
                float t = i * Dt;
                if (t > dur) t = dur;
                float u = dur > 0.0001f ? t / dur : 1f;
                if (u > 1f) u = 1f;
                VerbExitSample s = VerbExitClips.At(id, u, 1f, false, false);
                Emit(w, c, "exit-" + id, t,
                    s.ThighL, s.ThighR, s.KneeL, s.KneeR, s.ThighYawL, s.ThighYawR,
                    s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, s.ArmRollL, s.ArmRollR,
                    s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, s.HipRoll, s.HipYaw, s.SpineYaw,
                    s.FootL, s.FootR, 0f, 0f, 0f, 0f, s.ThighRollL, s.ThighRollR, s.Drop);
                if (t >= dur) break;
            }
        }
    }

    static void Vault(TextWriter w, CultureInfo c)
    {
        int n = Frames(Handoff2Feel.MantleWindow);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            float u = Handoff2Feel.MantleWindow > 0f ? t / Handoff2Feel.MantleWindow : 1f;
            if (u > 1f) u = 1f;
            MantlePose.Sample s = MantlePose.Cleared(u, true);
            Emit(w, c, "vault", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, s.ThighYawL, s.ThighYawR,
                s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, 0f, 0f,
                s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, 0f, 0f, 0f, 0f, 0f, 0f,
                hipDrop: s.PelvisDrop);
            if (u >= 1f) break;
        }
    }

    static void Climb(TextWriter w, CultureInfo c)
    {
        float rate = WallPose.ClimbRate(WallPose.ClimbSpeedRef);
        float dur = rate > 0.01f ? (6.2831853f / rate) : 0.4f;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            float phase = Mathf.Sin(rate * t);
            WallPose.Sample s = WallPose.Climb(phase, WallPose.ClimbSpeedRef);
            Emit(w, c, "climb", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, 0f, 0f,
                s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, 0f, 0f,
                s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, s.LeanZ, 0f, 0f, s.FootL, s.FootR, 0f);
            if (t >= dur) break;
        }
    }

    static void Slide(TextWriter w, CultureInfo c)
    {
        float inn = VerbPoseClips.SlideBlendSeconds;
        float hold = 0.20f;
        float dur = inn + hold + inn;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            float wgt;
            if (t < inn)
                wgt = Mathf.SmoothStep(0f, 1f, inn > 0f ? t / inn : 1f);
            else if (t < inn + hold)
                wgt = 1f;
            else
                wgt = 1f - Mathf.SmoothStep(0f, 1f, (t - inn - hold) / inn);
            VerbPoseClips.SlideSample s = VerbPoseClips.SlideAt(wgt, true);
            Emit(w, c, "slide", t, s.ThL, s.ThR, s.KnL, s.KnR, s.YawL, s.YawR,
                s.ArmL, s.ArmR, s.ArmYawL, s.ArmYawR, s.RollL, s.RollR,
                s.ElbL, s.ElbR, s.Hip, s.Spine, s.Head,
                0f, 0f, 0f,
                s.FootL, s.FootR, s.Drop,
                0f, 0f, 0f, s.ThRollL, s.ThRollR);
            if (t >= dur) break;
        }
    }

    static void Wall(TextWriter w, CultureInfo c)
    {
        float rate = WallPose.RunRate(WallPose.WallRunSpeedRef);
        float cycle = rate > 0.01f ? (6.2831853f / rate) : 0.4f;
        float push = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
        float dur = cycle + push;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            if (t <= cycle)
            {
                WallPose.Sample s = WallPose.RunCycle(rate * t, true);
                Emit(w, c, "wall", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, 0f, 0f,
                    s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, 0f, 0f,
                    s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, s.LeanZ, 0f, 0f, s.FootL, s.FootR, 0f);
            }
            else
            {
                WallJumpPose.Sample s = WallJumpPose.At(t - cycle, 8f, true, WallPose.WallRunSpeedRef);
                Emit(w, c, "wall", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, 0f, 0f,
                    s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, 0f, 0f,
                    s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, s.LeanZ, 0f, 0f, 0f, 0f, 0f);
            }
            if (t >= dur) break;
        }
    }

    static void Roll(TextWriter w, CultureInfo c)
    {
        float dur = 1f / HandoffFeel.SquashRate;
        GaitBlend.Legs legs = GaitBlend.At(1.5707963f, LocoFeel.Sprint);
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            float u = dur > 0f ? t / dur : 1f;
            float weight = HandoffFeel.RollWeight(u);
            float thigh = Mathf.Lerp(LandPose.HardThigh, legs.ThighL, u);
            float thL = Mathf.Lerp(thigh, HandoffFeel.RollThigh, weight);
            float thR = Mathf.Lerp(thigh, HandoffFeel.RollThigh - 18f, weight);
            float knL = Mathf.Lerp(Mathf.Lerp(LandPose.HardKnee, legs.KneeL, u), HandoffFeel.RollKnee, weight);
            float knR = Mathf.Lerp(Mathf.Lerp(LandPose.HardKnee, legs.KneeR, u), HandoffFeel.RollKnee + 20f, weight);
            float hip = Mathf.Lerp(Mathf.Lerp(LandPose.HardHip, HandoffFeel.RunHip, u), HandoffFeel.RollHip, weight);
            float spine = Mathf.Lerp(Mathf.Lerp(LandPose.HardSpine, HandoffFeel.RunSpine, u), HandoffFeel.RollSpine, weight);
            float head = Mathf.Lerp(LandPose.HardHead, HandoffFeel.RollHead, weight);
            HandoffFeel.RollAdd add = HandoffFeel.RollClear(u);
            Emit(w, c, "roll", t, thL, thR, knL, knR, add.YawL, add.YawR,
                Mathf.Lerp(-36f, HandoffFeel.RollArm, weight) + add.ArmL,
                Mathf.Lerp(28f, HandoffFeel.RollArm, weight) + add.ArmR,
                add.ArmYawL, add.ArmYawR, 0f, 0f,
                Mathf.Lerp(-18f, HandoffFeel.RollElbow, weight) + add.ElbL,
                Mathf.Lerp(-24f, HandoffFeel.RollElbow + 16f, weight) + add.ElbR,
                hip + add.Hip, spine + add.Spine, head + add.Head, 22f * weight, 0f, 0f, 0f, 0f, 0f);
            if (t >= dur) break;
        }
    }

    static void Pad(TextWriter w, CultureInfo c)
    {
        float vy = 24.7f;
        float t = 0f;
        int guard = 0;
        while (guard < 240)
        {
            LaunchPose.Sample s = LaunchPose.At(vy);
            Emit(w, c, "pad", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, 0f, 0f,
                s.ArmPitchL, s.ArmPitchR, s.ArmYawL, -s.ArmYawR, 0f, 0f,
                s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, 0f, 0f, 0f, 0f, 0f, 0f);
            if (vy < -16f) break;
            float g = vy > 0f ? 22f : 22f * 1.62f;
            vy -= g * Dt;
            t += Dt;
            guard++;
        }
    }

    static void Zip(TextWriter w, CultureInfo c)
    {
        float grab = Handoff2Feel.ZipGrabSeconds;
        float drop = Handoff2Feel.ZipDropSeconds;
        float dur = grab + 0.12f + drop;
        ZipPose.Sample from = ZipPose.JumpDrop();
        ZipPose.Sample hang = ZipPose.Hang();
        ZipPose.Sample to = ZipPose.Release();
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            ZipPose.Sample s;
            if (t <= grab)
            {
                float u = Handoff2Feel.ZipGrab(t);
                s = ZipLerp(from, hang, u);
            }
            else if (t <= grab + 0.12f)
                s = hang;
            else
            {
                float age = t - grab - 0.12f;
                s = ZipLerp(hang, to, Handoff2Feel.ZipDrop(age));
            }
            Emit(w, c, "zip", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, 0f, 0f,
                s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, 0f, 0f,
                s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, s.LeanZ, 0f, 0f, s.FootL, s.FootR, 0f);
            if (t >= dur) break;
        }
    }

    static ZipPose.Sample ZipLerp(ZipPose.Sample a, ZipPose.Sample b, float u)
    {
        return new ZipPose.Sample
        {
            ThighL = Mathf.Lerp(a.ThighL, b.ThighL, u),
            ThighR = Mathf.Lerp(a.ThighR, b.ThighR, u),
            KneeL = Mathf.Lerp(a.KneeL, b.KneeL, u),
            KneeR = Mathf.Lerp(a.KneeR, b.KneeR, u),
            ArmPitchL = Mathf.Lerp(a.ArmPitchL, b.ArmPitchL, u),
            ArmPitchR = Mathf.Lerp(a.ArmPitchR, b.ArmPitchR, u),
            ArmYawL = Mathf.Lerp(a.ArmYawL, b.ArmYawL, u),
            ArmYawR = Mathf.Lerp(a.ArmYawR, b.ArmYawR, u),
            ElbowL = Mathf.Lerp(a.ElbowL, b.ElbowL, u),
            ElbowR = Mathf.Lerp(a.ElbowR, b.ElbowR, u),
            Hip = Mathf.Lerp(a.Hip, b.Hip, u),
            Spine = Mathf.Lerp(a.Spine, b.Spine, u),
            Head = Mathf.Lerp(a.Head, b.Head, u),
            LeanZ = Mathf.Lerp(a.LeanZ, b.LeanZ, u),
            FootL = Mathf.Lerp(a.FootL, b.FootL, u),
            FootR = Mathf.Lerp(a.FootR, b.FootR, u),
        };
    }

    static void Grapple(TextWriter w, CultureInfo c)
    {
        float aim = GrapplePose.AimBlendSeconds;
        float latch = GrapplePose.LatchSnapSeconds;
        float pull = 0.30f;
        float rel = Handoff2Feel.ReleaseSeconds;
        float dur = aim + latch + pull + rel;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            GrapplePose.Sample s;
            if (t <= aim)
                s = GrapplePose.ForBody(GrapplePose.Aim(10f, 0f));
            else if (t <= aim + latch)
                s = GrapplePose.ForBody(GrapplePose.Latch(0f));
            else if (t <= aim + latch + pull)
            {
                float phase = Mathf.Sin((t - aim - latch) * 6f);
                s = GrapplePose.ForBody(GrapplePose.Pull(phase, 4f, 0f));
            }
            else
            {
                float age = t - aim - latch - pull;
                s = GrapplePose.ForBody(GrapplePose.Release(0f, -4f, 0f, age));
            }
            Emit(w, c, "grapple", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, 0f, 0f,
                s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, GrapplePose.HangRollL, 0f,
                s.ElbowL, s.ElbowR, s.Hip, s.Spine, s.Head, GrapplePose.HangLean, s.HipYaw, s.SpineYaw, 0f, 0f, 0f,
                0f, 0f, GrapplePose.HangShoulder);
            if (t >= dur) break;
        }
    }

    static void Punch(TextWriter w, CultureInfo c)
    {
        float dur = 0.37f;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            float sample = dur > 0f ? t / dur : 1f;
            if (sample > 1f) sample = 1f;
            float u = Mathf.SmoothStep(0f, 1f, sample);
            Emit(w, c, "punch", t,
                Mathf.Lerp(VerbPoseClips.PunchCockLeadThigh, VerbPoseClips.PunchStrikeLeadThigh, u),
                Mathf.Lerp(VerbPoseClips.PunchCockTrailThigh, VerbPoseClips.PunchStrikeTrailThigh, u),
                Mathf.Lerp(VerbPoseClips.PunchCockLeadKnee, VerbPoseClips.PunchStrikeLeadKnee, u),
                Mathf.Lerp(VerbPoseClips.PunchCockTrailKnee, VerbPoseClips.PunchStrikeTrailKnee, u),
                0f, 0f,
                Mathf.Lerp(VerbPoseClips.PunchGuardPitchCock, VerbPoseClips.PunchGuardPitchStrike, u),
                Mathf.Lerp(VerbPoseClips.PunchCockPitch, VerbPoseClips.PunchStrikePitch, u),
                Mathf.Lerp(VerbPoseClips.PunchGuardYawCock, VerbPoseClips.PunchGuardYawStrike, u),
                Mathf.Lerp(VerbPoseClips.PunchCockYaw, VerbPoseClips.PunchStrikeYaw, u),
                Mathf.Lerp(VerbPoseClips.PunchGuardRoll, VerbPoseClips.PunchGuardRoll, u),
                Mathf.Lerp(VerbPoseClips.PunchCockRoll, VerbPoseClips.PunchStrikeRoll, u),
                Mathf.Lerp(VerbPoseClips.PunchGuardElbowCock, VerbPoseClips.PunchGuardElbowStrike, u),
                Mathf.Lerp(VerbPoseClips.PunchCockElbow, VerbPoseClips.PunchStrikeElbow, u),
                VerbPoseClips.PunchHipPitch,
                VerbPoseClips.PunchSpinePitch,
                Mathf.Lerp(VerbPoseClips.PunchHeadPitch, VerbPoseClips.PunchHeadPitch, u),
                0f,
                Mathf.Lerp(VerbPoseClips.PunchCockHipYaw, VerbPoseClips.PunchStrikeHipYaw, u),
                Mathf.Lerp(VerbPoseClips.PunchCockSpineYaw, VerbPoseClips.PunchStrikeSpineYaw, u),
                0f, 0f, 0f,
                0f, Mathf.Lerp(VerbPoseClips.PunchFistYaw, 0f, u));
            if (t >= dur) break;
        }
    }

    static void TagCatch(TextWriter w, CultureInfo c)
    {
        float dur = 0.45f;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            float sample = dur > 0f ? t / dur : 1f;
            float u = Mathf.SmoothStep(0f, 1f, sample > 1f ? 1f : sample);
            Emit(w, c, "tag", t,
                Mathf.Lerp(VerbPoseClips.TagWindupThigh, VerbPoseClips.TagThigh, u),
                Mathf.Lerp(VerbPoseClips.TagWindupThigh, VerbPoseClips.TagThigh, u),
                Mathf.Lerp(VerbPoseClips.TagWindupKnee, VerbPoseClips.TagKnee, u),
                Mathf.Lerp(VerbPoseClips.TagWindupKnee, VerbPoseClips.TagKnee, u),
                0f, 0f,
                Mathf.Lerp(VerbPoseClips.TagWindupArmPitch, VerbPoseClips.TagArmPitch, u) + VerbPoseClips.TagClearPitch * u,
                Mathf.Lerp(VerbPoseClips.TagWindupArmPitch, VerbPoseClips.TagArmPitch, u) + VerbPoseClips.TagClearPitch * u,
                Mathf.Lerp(VerbPoseClips.TagWindupArmYaw, VerbPoseClips.TagArmYaw, u) + VerbPoseClips.TagClearYaw * u,
                Mathf.Lerp(-VerbPoseClips.TagWindupArmYaw, -VerbPoseClips.TagArmYaw, u) - VerbPoseClips.TagClearYaw * u,
                Mathf.Lerp(VerbPoseClips.TagWindupArmRoll, VerbPoseClips.TagArmRoll, u),
                Mathf.Lerp(-VerbPoseClips.TagWindupArmRoll, -VerbPoseClips.TagArmRoll, u),
                Mathf.Lerp(VerbPoseClips.TagWindupElbow, VerbPoseClips.TagElbow, u) + VerbPoseClips.TagClearElbow * u,
                Mathf.Lerp(VerbPoseClips.TagWindupElbow, VerbPoseClips.TagElbow, u) + VerbPoseClips.TagClearElbow * u,
                Mathf.Lerp(VerbPoseClips.TagWindupHip, VerbPoseClips.TagHip, u),
                Mathf.Lerp(VerbPoseClips.TagWindupSpine, VerbPoseClips.TagSpine, u),
                Mathf.Lerp(VerbPoseClips.TagWindupHead, VerbPoseClips.TagHead, u),
                0f, 0f, 0f,
                VerbPoseClips.TagClearFoot, VerbPoseClips.TagClearFoot, 0f);
            if (t >= dur) break;
        }
    }

    static void Stagger(TextWriter w, CultureInfo c)
    {
        PunchStaggerPose.Sample s = PunchStaggerPose.Stumble();
        int n = Frames(PunchStaggerPose.Duration);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > PunchStaggerPose.Duration) t = PunchStaggerPose.Duration;
            float weight = PunchStaggerPose.Weight(t);
            Emit(w, c, "stagger", t,
                s.ThighL * weight, s.ThighR * weight, s.KneeL * weight, s.KneeR * weight, 0f, 0f,
                s.ArmPitchL * weight, s.ArmPitchR * weight, s.ArmYawL * weight, s.ArmYawR * weight,
                s.ArmRollL * weight, s.ArmRollR * weight,
                s.ElbowL * weight, s.ElbowR * weight,
                s.Hip * weight, s.Spine * weight, s.Head * weight, 0f,
                s.HipYaw * weight, s.SpineYaw * weight, 0f, 0f, 0f);
            if (t >= PunchStaggerPose.Duration) break;
        }
    }

    static void Idle(TextWriter w, CultureInfo c)
    {
        float dur = 6.2831853f / IdlePose.ShiftRate;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            IdlePose.Sample s = IdlePose.At(IdlePose.ShiftRate * t, IdlePose.BreathRate * t);
            Emit(w, c, "idle", t, s.ThighL, s.ThighR, s.KneeL, s.KneeR, 0f, 0f,
                VerbPoseClips.IdleArmPitch, VerbPoseClips.IdleArmPitch,
                VerbPoseClips.IdleArmYaw, -VerbPoseClips.IdleArmYaw, 0f, 0f,
                VerbPoseClips.IdleElbow, VerbPoseClips.IdleElbow,
                0f, s.ChestPitch, s.HeadPitch, s.HipRoll, 0f, s.ChestRoll, s.FootL, s.FootR, 0f);
            if (t >= dur) break;
        }
    }

    static void Loco(TextWriter w, CultureInfo c, string name, float speed)
    {
        float rate = GaitBlend.CadenceAt(speed);
        float dur = rate > 0.01f ? (6.2831853f / rate) : 0.5f;
        int n = Frames(dur);
        for (int i = 0; i <= n; i++)
        {
            float t = i * Dt;
            if (t > dur) t = dur;
            float phase = rate * t;
            GaitBlend.Legs legs = GaitBlend.At(phase, speed);
            float sin = Mathf.Sin(phase);
            Emit(w, c, name, t, legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR,
                -LocoFeel.ThighSpread, LocoFeel.ThighSpread,
                LocoFeel.ArmPitch(-sin, speed), LocoFeel.ArmPitch(sin, speed), 0f, 0f, 0f, 0f,
                -16f, -20f,
                0f, LocoFeel.CruiseTarget(speed), -LocoFeel.CruiseTarget(speed) * 0.35f, 0f, 0f, 0f,
                legs.FootL, legs.FootR, 0f);
            if (t >= dur) break;
        }
    }

    static int Frames(float seconds)
    {
        return (int)(seconds / Dt + 0.5f);
    }

    static void Emit(TextWriter w, CultureInfo c, string clip, float t,
        float thL, float thR, float knL, float knR, float yawL, float yawR,
        float armL, float armR, float armYawL, float armYawR, float rollL, float rollR,
        float elbL, float elbR, float hip, float spine, float head, float lean,
        float hipYaw, float spineYaw,         float footL, float footR, float drop,
        float elbYawL = 0f, float elbYawR = 0f, float shoulderL = 0f,
        float thRollL = 0f, float thRollR = 0f, float hipDrop = 0f)
    {
        w.Write(clip);
        w.Write('\t');
        w.Write(t.ToString("0.000", c));
        Write(w, c, thL); Write(w, c, thR); Write(w, c, knL); Write(w, c, knR);
        Write(w, c, yawL); Write(w, c, yawR);
        Write(w, c, armL); Write(w, c, armR); Write(w, c, armYawL); Write(w, c, armYawR);
        Write(w, c, rollL); Write(w, c, rollR);
        Write(w, c, elbL); Write(w, c, elbR);
        Write(w, c, hip); Write(w, c, spine); Write(w, c, head); Write(w, c, lean);
        Write(w, c, hipYaw); Write(w, c, spineYaw);
        Write(w, c, footL); Write(w, c, footR); Write(w, c, drop);
        Write(w, c, elbYawL); Write(w, c, elbYawR); Write(w, c, shoulderL);
        Write(w, c, thRollL); Write(w, c, thRollR);
        Write(w, c, hipDrop);
        w.WriteLine();
    }

    static void Write(TextWriter w, CultureInfo c, float v)
    {
        w.Write('\t');
        w.Write(v.ToString("0.000", c));
    }
}
