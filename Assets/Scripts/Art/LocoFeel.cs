using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual feel on the run, the start, the stop, the turn, the idle, the
    /// crouch walk, and the slide. Speeds, slide decay, and the capsule are
    /// unchanged. Nothing here runs a root motion or delays a verb.
    /// </summary>
    public static class LocoFeel
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;
        public const float Dt = 1f / 60f;

        public const float Walk = 6.9f;
        public const float Sprint = 13.8f;
        public const float Crouch = 3.68f;

        public const float ArmWalk = 32f;
        public const float ArmSprint = 46f;
        public const float CruiseDeg = 6.5f;
        public const float CruiseSeconds = 0.12f;
        public const float HeadShare = 0.65f;

        public const float StartSeconds = 0.16f;
        public const float StepInSeconds = 0.32f;
        public const float StopSeconds = 0.14f;
        public const float PivotSeconds = 0.10f;
        public const float PivotRate = 140f;
        public const float SlideSeconds = 0.22f;
        public const float SoleWindow = 0.05f;

        public struct Shot
        {
            public float ArmL, ArmR, ElbL, ElbR, ThL, ThR, KnL, KnR, Spine, Hip, Head, Lean;
        }

        public static float ArmPitch(float phase, float speed)
        {
            return -phase * ArmAmp(speed);
        }

        public static float CruiseTarget(float speed)
        {
            return Inv(Walk, Sprint, speed) * CruiseDeg;
        }

        public static float HeadHold(float bob)
        {
            return bob * HeadShare;
        }

        public static float StartChest(float stepIn, float authored)
        {
            float u = stepIn < 0f ? 0f : (stepIn > 1f ? 1f : stepIn);
            float age = u * StepInSeconds;
            float rise = age >= StartSeconds ? 1f : Raised(age / StartSeconds);
            return authored * rise;
        }

        public static float StopOpen(float age)
        {
            if (age < 0f) age = 0f;
            if (age >= StopSeconds) return 1f;
            return Raised(age / StopSeconds);
        }

        public static float IdleLook(float time)
        {
            return BodyLife.LookYawAt(time);
        }

        public static float CrouchThigh(float sinPhase, float baseDeg, float reach, float trail)
        {
            float raw = ThighOffset(sinPhase, reach, trail);
            const float span = 0.55f;
            float lo = sinPhase - span;
            float hi = sinPhase + span;
            if (lo < -1f) lo = -1f;
            if (hi > 1f) hi = 1f;
            float a = ThighOffset(lo, reach, trail);
            float b = ThighOffset(hi, reach, trail);
            return baseDeg + (a + 2f * raw + b) * 0.25f;
        }

        public static float SlideWeight(float age, bool entering)
        {
            float u = SlideSeconds > 0.0001f ? age / SlideSeconds : 1f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float e = Raised(u);
            return entering ? e : 1f - e;
        }

        public static float Sole(float phase, float speed, bool left)
        {
            float leg = left ? phase : phase + 3.14159265f;
            return SoleAt(leg, speed, true);
        }

        public static void NoteWalkPivot(ref float weight, ref float vel, ref float sign, float yawRate, float speed, bool canTurn, float dt)
        {
            bool gate = canTurn
                && speed > PivotPose.SpeedOff
                && speed <= Walk + 0.05f
                && (yawRate < 0f ? -yawRate : yawRate) >= PivotRate;
            float target = gate ? 1f : 0f;
            weight = SmoothMotion.Smooth(weight, target, ref vel, PivotSeconds, dt);
            if (gate)
                sign = yawRate < 0f ? -1f : 1f;
        }

        public static Shot ShotAt(int kind, int frame, bool after)
        {
            float u = frame / 7f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            Shot s = new Shot();
            s.ElbL = -14f;
            s.ElbR = -14f;
            s.KnL = -8f;
            s.KnR = -8f;
            if (kind == 0)
            {
                float phase = u * 6.2831853f;
                float sin = Mathf.Sin(phase);
                float speed = Sprint;
                GaitBlend.Legs legs = GaitBlend.At(phase, speed);
                s.ArmL = after ? ArmPitch(-sin, speed) : ArmBefore(-sin, speed);
                s.ArmR = after ? ArmPitch(sin, speed) : ArmBefore(sin, speed);
                s.ThL = legs.ThighL;
                s.ThR = legs.ThighR;
                s.KnL = legs.KneeL;
                s.KnR = legs.KneeR;
                s.Spine = after ? CruiseTarget(speed) * u : (u < 0.2f ? 0f : CruiseDeg);
                s.Head = -s.Spine * 0.35f;
            }
            else if (kind == 1)
            {
                float stepIn = u;
                float authored = LocomotionPolish.StartLean(1f - Mathf.SmoothStep(0f, 1f, stepIn), 0f);
                s.Spine = after ? StartChest(stepIn, authored) : authored;
                s.ThL = 18f;
                s.ThR = -8f;
                s.KnL = -20f;
                s.KnR = -12f;
            }
            else if (kind == 2)
            {
                float from = GaitBlend.FrontReach(GaitBlend.PoseWeight(Sprint));
                float to = StopPlantPose.LeadThigh;
                float open = after ? StopOpen(u * StopSeconds) : (u < 0.15f ? 0f : 1f);
                s.ThL = Mathf.Lerp(from, to, open);
                s.ThR = Mathf.Lerp(-GaitBlend.BackReach(GaitBlend.PoseWeight(Sprint)), StopPlantPose.TrailThigh, open);
                s.KnL = Mathf.Lerp(-8f, StopPlantPose.LeadKnee, open);
                s.KnR = Mathf.Lerp(-6f, StopPlantPose.TrailKnee, open);
                s.Spine = Mathf.Lerp(CruiseDeg, StopPlantPose.ChestPitch, open);
                s.Hip = Mathf.Lerp(0f, StopPlantPose.HipPitch, open);
            }
            else if (kind == 3)
            {
                float turnAbs = u;
                float inn = TurnIn(turnAbs, after);
                s.Lean = inn * GaitBlend.TurnLeanSprint;
                float plant = after ? StopOpen(u * PivotSeconds) : (u < 0.15f ? 0f : 1f);
                float front = GaitBlend.FrontReach(GaitBlend.PoseWeight(Walk));
                s.ThL = Mathf.Lerp(front, PivotPose.PlantThigh, plant);
                s.ThR = Mathf.Lerp(-8f, PivotPose.LeadThigh, plant);
                s.KnL = Mathf.Lerp(-6f, PivotPose.PlantKnee, plant);
            }
            else if (kind == 4)
            {
                float time = BodyLife.LookPeriod * (1f - BodyLife.LookWindow) + u * BodyLife.LookPeriod * BodyLife.LookWindow;
                s.Head = after ? IdleLook(time) : (u < 0.2f ? 0f : (u > 0.8f ? 0f : BodyLife.LookYaw));
                IdlePose.Sample idle = IdlePose.At(u * 6.2831853f, u * 6.2831853f);
                s.Spine = idle.ChestPitch;
                s.Hip = idle.HipRoll;
                s.ThL = idle.ThighL;
                s.ThR = idle.ThighR;
            }
            else if (kind == 5)
            {
                float phase = u * 6.2831853f;
                float sin = Mathf.Sin(phase);
                float reach = CrouchPose.WalkThighReach;
                float trail = CrouchPose.WalkThighTrail;
                float basis = CrouchPose.WalkThighBase;
                s.ThL = after ? CrouchThigh(sin, basis, reach, trail) : basis + ThighOffset(sin, reach, trail);
                s.ThR = after ? CrouchThigh(-sin, basis, reach, trail) : basis + ThighOffset(-sin, reach, trail);
                s.KnL = CrouchPose.WalkKnee(Mathf.Max(0f, sin));
                s.KnR = CrouchPose.WalkKnee(Mathf.Max(0f, -sin));
                s.Spine = CrouchPose.Spine;
                s.Hip = CrouchPose.Hip;
                s.Head = CrouchPose.Head;
            }
            else
            {
                float w = after ? Raised(u) : PoseHandoff.Ease(u < 0.15f ? 0f : (u > 0.3f ? 1f : (u - 0.15f) / 0.15f));
                if (!after && u >= 0.3f) w = 1f;
                s.Head = VerbPoseClips.SlideHead * w;
                s.Hip = VerbPoseClips.SlideHip * w;
                s.Spine = VerbPoseClips.SlideSpine * w;
                s.ThL = Mathf.Lerp(20f, VerbPoseClips.SlideLeadThigh, w);
                s.ThR = Mathf.Lerp(8f, VerbPoseClips.SlideTrailThigh, w);
                s.KnL = Mathf.Lerp(-12f, VerbPoseClips.SlideLeadKnee, w);
                s.KnR = Mathf.Lerp(-8f, VerbPoseClips.SlideTrailKnee, w);
            }
            return s;
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "loco-feel"
                + " stride=" + Pair(ArmStep(false), ArmStep(true), c)
                + " foot=" + Pair(SoleStep(false), SoleStep(true), c)
                + " slideCm=" + Pair(FootSlide.SprintBefore(), FootSlide.SprintAfter(), c)
                + " lean=" + Pair(LeanStep(false), LeanStep(true), c)
                + " start=" + Pair(StartStep(false), StartStep(true), c)
                + " stop=" + Pair(StopStep(false), StopStep(true), c)
                + " turn=" + Pair(TurnStep(false), TurnStep(true), c)
                + " idle=" + Pair(IdleStep(false), IdleStep(true), c)
                + " crouch=" + Pair(CrouchStep(false), CrouchStep(true), c)
                + " drop=" + Pair(DropStep(false), DropStep(true), c)
                + " head=" + Pair(HeadStep(false), HeadStep(true), c)
                + " gameplayDelay=0 rootMotion=0";
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (Mathf.Abs(Walk - 6.9f) > 0.001f || Mathf.Abs(Sprint - 13.8f) > 0.001f) return false;
            if (Mathf.Abs(Crouch - 3.68f) > 0.001f) return false;
            if (!FootSlide.Holds()) return false;
            if (VerbPoseClips.SlideBlendSeconds < 0.08f || VerbPoseClips.SlideBlendSeconds > 0.12f) return false;
            if (ArmStep(true) >= ArmStep(false) * 0.75f) return false;
            if (SoleStep(true) >= SoleStep(false) * 0.5f) return false;
            if (FootSlide.SprintAfter() > 1f || FootSlide.WalkAfter() > 1f || FootSlide.CrouchAfter() > 1f) return false;
            if (LeanStep(true) >= LeanStep(false) * 0.55f) return false;
            if (StartStep(true) >= StartStep(false) * 0.75f) return false;
            if (StopStep(true) >= StopStep(false) * 0.45f) return false;
            if (TurnStep(true) >= TurnStep(false) * 0.55f) return false;
            if (IdleStep(true) >= IdleStep(false) * 0.25f) return false;
            if (CrouchStep(true) >= CrouchStep(false) * 0.9f) return false;
            if (DropStep(true) >= DropStep(false) * 0.7f) return false;
            if (HeadStep(true) >= HeadStep(false) * 0.5f) return false;
            if (ArmStep(true) <= 0.05f || SoleStep(true) <= 0.05f) return false;
            return true;
        }

        static string Pair(float before, float after, CultureInfo c)
        {
            return before.ToString("0.0", c) + ">" + after.ToString("0.0", c);
        }

        static float ArmAmp(float speed)
        {
            return Mathf.Lerp(ArmWalk, ArmSprint, Inv(Walk, Sprint, speed));
        }

        static float ArmBefore(float phase, float speed)
        {
            float gait = Mathf.Clamp01(GaitBlend.PoseWeight(speed));
            float amp = Mathf.Lerp(36f, 64f, gait) * BodyLife.ArmPump(speed);
            float fwd = Mathf.Max(0f, phase) * amp;
            float back = Mathf.Max(0f, -phase) * amp * 0.22f;
            return -(fwd - back);
        }

        static float ArmStep(bool eased)
        {
            float speed = Sprint;
            float cadence = LocomotionPolish.PlayCadence(speed);
            float phase = 0f;
            float prev = eased ? ArmPitch(0f, speed) : ArmBefore(0f, speed);
            float peak = 0f;
            int n = Steps(cadence);
            for (int i = 0; i < n; i++)
            {
                phase += cadence * Dt;
                float s = Mathf.Sin(phase);
                float now = eased ? ArmPitch(s, speed) : ArmBefore(s, speed);
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
            }
            return peak;
        }

        static float SoleAt(float phase, float speed, bool eased)
        {
            float raw = RawSole(phase, speed);
            if (!eased) return raw;
            float cadence = LocomotionPolish.PlayCadence(speed);
            if (cadence < 0.05f) return raw;
            float half = SoleWindow * cadence;
            float dist = ContactDist(phase);
            if (dist >= half || dist <= -half) return raw;
            float u = (dist + half) / (2f * half);
            float edge = RawSole(phase + (half - dist), speed);
            return Mathf.Lerp(0f, edge, PoseHandoff.Ease(u));
        }

        static float RawSole(float phase, float speed)
        {
            float weight = GaitBlend.PoseWeight(speed);
            float p = Wrap(phase);
            float s = Mathf.Sin(p);
            float c = Mathf.Cos(p);
            float front = GaitBlend.FrontReach(weight);
            float back = GaitBlend.BackReach(weight);
            float thigh = s >= 0f ? s * front : s * back;
            if (c > 0f) return 0f;
            return -(thigh - 5f);
        }

        static float SoleStep(bool eased)
        {
            float speed = Sprint;
            float cadence = LocomotionPolish.PlayCadence(speed);
            float phase = 0f;
            float prev = SoleAt(0f, speed, eased);
            float peak = 0f;
            int n = Steps(cadence);
            for (int i = 0; i < n; i++)
            {
                phase += cadence * Dt;
                float now = SoleAt(phase, speed, eased);
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
            }
            return peak;
        }

        static float LeanStep(bool eased)
        {
            if (!eased) return CruiseDeg;
            float cur = 0f;
            float vel = 0f;
            float peak = 0f;
            int n = (int)(CruiseSeconds / Dt) + 8;
            for (int i = 0; i < n; i++)
            {
                float next = SmoothMotion.Smooth(cur, CruiseDeg, ref vel, CruiseSeconds, Dt);
                peak = Max(peak, Mathf.Abs(next - cur));
                cur = next;
            }
            return peak;
        }

        static float StartStep(bool eased)
        {
            if (!eased) return LocomotionPolish.StartLean(1f, 0f);
            float prev = 0f;
            float peak = 0f;
            float stepIn = 0f;
            int n = (int)(StepInSeconds / Dt) + 2;
            for (int i = 0; i < n; i++)
            {
                float authored = LocomotionPolish.StartLean(1f - Mathf.SmoothStep(0f, 1f, stepIn), 0f);
                float now = StartChest(stepIn, authored);
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
                stepIn += Dt / StepInSeconds;
            }
            return peak;
        }

        static float StopStep(bool eased)
        {
            float gap = Mathf.Abs(GaitBlend.FrontReach(GaitBlend.PoseWeight(Sprint)) - StopPlantPose.LeadThigh);
            if (!eased) return gap;
            float prev = 0f;
            float peak = 0f;
            float age = 0f;
            int n = (int)(StopSeconds / Dt) + 3;
            for (int i = 0; i < n; i++)
            {
                float now = gap * StopOpen(age);
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
                age += Dt;
            }
            return peak;
        }

        static float TurnStep(bool eased)
        {
            float gap = Mathf.Abs(GaitBlend.FrontReach(GaitBlend.PoseWeight(Walk)) - PivotPose.PlantThigh);
            if (!eased) return gap;
            float prev = 0f;
            float peak = 0f;
            float age = 0f;
            int n = (int)(PivotSeconds / Dt) + 3;
            for (int i = 0; i < n; i++)
            {
                float now = gap * Raised(age / PivotSeconds);
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
                age += Dt;
            }
            return peak;
        }

        static float TurnIn(float turnAbs, bool eased)
        {
            float a = turnAbs < 0f ? 0f : (turnAbs > 1f ? 1f : turnAbs);
            if (!eased) return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a / 0.55f));
            return a;
        }

        static float IdleStep(bool eased)
        {
            if (!eased) return BodyLife.LookYaw;
            float prev = 0f;
            float peak = 0f;
            float span = BodyLife.LookPeriod * 2f;
            for (float t = 0f; t <= span; t += Dt)
            {
                float now = BodyLife.LookYawAt(t);
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
            }
            return peak;
        }

        static float CrouchStep(bool eased)
        {
            float cadence = BodyLife.CrouchCadence(Crouch);
            float reach = CrouchPose.WalkThighReach;
            float trail = CrouchPose.WalkThighTrail;
            float basis = CrouchPose.WalkThighBase;
            float phase = 0f;
            float prev = eased
                ? CrouchThigh(0f, basis, reach, trail)
                : basis + ThighOffset(0f, reach, trail);
            float peak = 0f;
            int n = Steps(cadence);
            for (int i = 0; i < n; i++)
            {
                phase += cadence * Dt;
                float sin = Mathf.Sin(phase);
                float now = eased
                    ? CrouchThigh(sin, basis, reach, trail)
                    : basis + ThighOffset(sin, reach, trail);
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
            }
            return peak;
        }

        static float DropStep(bool eased)
        {
            float seconds = eased ? SlideSeconds : VerbPoseClips.SlideBlendSeconds;
            float prev = 0f;
            float peak = 0f;
            float age = 0f;
            int n = (int)(seconds / Dt) + 4;
            for (int i = 0; i < n; i++)
            {
                float u = seconds > 0.0001f ? age / seconds : 1f;
                float w = eased ? Raised(u > 1f ? 1f : u) : PoseHandoff.Ease(u);
                float now = w * VerbPoseClips.SlideHead;
                peak = Max(peak, Mathf.Abs(now - prev));
                prev = now;
                age += Dt;
            }
            return peak;
        }

        static float HeadStep(bool eased)
        {
            float cadence = LocomotionPolish.PlayCadence(Sprint);
            float phase = 0f;
            float prev = 0f;
            float peak = 0f;
            int n = Steps(cadence);
            for (int i = 0; i < n; i++)
            {
                phase += cadence * Dt;
                float bob = (float)System.Math.Pow(System.Math.Abs(System.Math.Sin(phase)), 1.7) * 0.085f;
                float now = eased ? bob - HeadHold(bob) : bob;
                peak = Max(peak, Mathf.Abs(now - prev) * 100f);
                prev = now;
            }
            return peak;
        }

        static float ThighOffset(float sinPhase, float reach, float trail)
        {
            return sinPhase >= 0f ? sinPhase * reach : sinPhase * trail;
        }

        static float ContactDist(float phase)
        {
            float p = Wrap(phase);
            const float pi = 3.14159265f;
            float a = AngleTo(p, pi * 0.5f);
            float b = AngleTo(p, pi * 1.5f);
            float dist = Mathf.Abs(a) < Mathf.Abs(b) ? Mathf.Abs(a) : Mathf.Abs(b);
            if (Mathf.Cos(p) <= 0f) return dist;
            return -dist;
        }

        static float AngleTo(float a, float b)
        {
            float d = a - b;
            const float pi = 3.14159265f;
            if (d > pi) d -= pi * 2f;
            if (d < -pi) d += pi * 2f;
            return d;
        }

        static float Wrap(float phase)
        {
            const float tau = 6.2831853f;
            float p = phase % tau;
            if (p < 0f) p += tau;
            return p;
        }

        static float Raised(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return 0.5f * (1f - Mathf.Cos(u * 3.14159265f));
        }

        static float Inv(float a, float b, float v)
        {
            if (b <= a) return v >= b ? 1f : 0f;
            float t = (v - a) / (b - a);
            if (t < 0f) return 0f;
            if (t > 1f) return 1f;
            return t;
        }

        static int Steps(float cadence)
        {
            if (cadence < 0.05f) return 8;
            int n = (int)(6.2831853f / (cadence * Dt)) + 3;
            if (n < 8) n = 8;
            if (n > 400) n = 400;
            return n;
        }

        static float Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}
