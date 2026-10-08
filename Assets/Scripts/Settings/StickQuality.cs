using System;
using System.Globalization;
using UnityEngine;

namespace Tag.Settings
{
    /// <summary>
    /// Radial gamepad stick. The inner radius matches the old axis deadzone (0.19)
    /// so a light touch still drops out, then the stick ramps to full at the outer
    /// radius. Keyboard axes are already -1, 0, or 1, so a full press stays 1.
    /// Look acceleration is off unless LookAccel is above 0, and it never runs for the mouse.
    /// </summary>
    public static class StickQuality
    {
        public static Vector2 Shape(Vector2 v)
        {
            GameSettings s = GameSettings.Current;
            float inner = s != null ? s.StickInner : GameSettings.StickInnerDefault;
            float outer = s != null ? s.StickOuter : GameSettings.StickOuterDefault;
            float curve = s != null ? s.StickCurve : GameSettings.StickCurveDefault;
            return Shape(v.x, v.y, inner, outer, curve);
        }

        public static Vector2 Shape(float x, float y, float inner, float outer, float curve)
        {
            float mag = (float)Math.Sqrt(x * x + y * y);
            if (mag <= inner || mag < 0.000001f)
                return new Vector2(0f, 0f);
            float span = outer - inner;
            if (span < 0.05f) span = 0.05f;
            float t = (mag - inner) / span;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            if (curve != 1f)
            {
                if (curve < 0.5f) curve = 0.5f;
                if (curve > 3f) curve = 3f;
                t = (float)Math.Pow(t, curve);
            }
            float scale = t / mag;
            return new Vector2(x * scale, y * scale);
        }

        /// <summary>Accel 0 returns the axis unchanged, including mouse-sized deltas.</summary>
        public static float LookAxis(float axis, float accel)
        {
            if (accel <= 0f) return axis;
            float sign = axis < 0f ? -1f : 1f;
            float m = axis < 0f ? -axis : axis;
            if (m > 1f) m = 1f;
            float a = accel > 1f ? 1f : accel;
            return sign * (float)Math.Pow(m, 1f + a * 2f);
        }

        /// <summary>Same curve on the stick length, so a diagonal does not snap to a cardinal.</summary>
        public static void LookStick(float x, float y, float accel, out float ox, out float oy)
        {
            if (accel <= 0f)
            {
                ox = x;
                oy = y;
                return;
            }
            float mag = (float)Math.Sqrt(x * x + y * y);
            if (mag <= 0.000001f)
            {
                ox = 0f;
                oy = 0f;
                return;
            }
            float m = mag > 1f ? 1f : mag;
            float a = accel > 1f ? 1f : accel;
            float curved = (float)Math.Pow(m, 1f + a * 2f);
            float scale = curved / mag;
            ox = x * scale;
            oy = y * scale;
        }

        public static float Magnitude(float x, float y)
        {
            return (float)Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// Extra stick travel a 45° push used to need before either axis cleared an axial deadzone.
        /// A radial deadzone reports 0.
        /// </summary>
        public static float AxialHole(float inner, float outer, float curve)
        {
            float card = FirstMotion(1f, 0f, inner, outer, curve);
            float diag = FirstMotion(1f, 1f, inner, outer, curve);
            float hole = diag - card;
            if (hole < 0.0005f) hole = 0f;
            return hole;
        }

        public static string ProofLine()
        {
            const float inner = GameSettings.StickInnerDefault;
            const float outer = GameSettings.StickOuterDefault;
            const float curve = GameSettings.StickCurveDefault;
            Vector2 diag = Shape(0.70710678118f, 0.70710678118f, inner, outer, curve);
            Vector2 card = Shape(1f, 0f, inner, outer, curve);
            float ramp = Magnitude(Shape(0.20f, 0f, inner, outer, curve).x, Shape(0.20f, 0f, inner, outer, curve).y);
            float hole = AxialHole(inner, outer, curve);
            string holeText = hole < 0.0005f ? "0" : hole.ToString("0.000", CultureInfo.InvariantCulture);
            return "stick-quality"
                + " diag=" + Num(Magnitude(diag.x, diag.y))
                + " card=" + Num(Magnitude(card.x, card.y))
                + " ramp=" + Num(ramp)
                + " axialHole=" + holeText
                + " inner=" + GameSettings.StickInnerDefault.ToString("0.##", CultureInfo.InvariantCulture)
                + " outer=" + GameSettings.StickOuterDefault.ToString("0.##", CultureInfo.InvariantCulture)
                + " curve=" + GameSettings.StickCurveDefault.ToString("0.##", CultureInfo.InvariantCulture)
                + " lookAccel=" + GameSettings.LookAccelDefault.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static bool Holds()
        {
            const float inner = GameSettings.StickInnerDefault;
            const float outer = GameSettings.StickOuterDefault;
            const float curve = GameSettings.StickCurveDefault;
            if (GameSettings.RowCount != 19) return false;
            if (GameSettings.LookAccelDefault != 0f) return false;
            Vector2 full = Shape(1f, 0f, inner, outer, curve);
            if (Math.Abs(full.x - 1f) > 0.001f || Math.Abs(full.y) > 0.001f) return false;
            Vector2 unitDiag = Shape(0.70710678118f, 0.70710678118f, inner, outer, curve);
            if (Math.Abs(Magnitude(unitDiag.x, unitDiag.y) - 1f) > 0.002f) return false;
            if (Magnitude(Shape(inner, 0f, inner, outer, curve).x, 0f) > 0.0001f) return false;
            float ramp = Magnitude(Shape(0.20f, 0f, inner, outer, curve).x, Shape(0.20f, 0f, inner, outer, curve).y);
            if (ramp < 0.005f || ramp > 0.02f) return false;
            float edge = Magnitude(Shape(0.191f, 0f, inner, outer, curve).x, 0f);
            if (edge <= 0f || edge > 0.01f) return false;
            if (Magnitude(Shape(0.15f, 0f, inner, outer, curve).x, 0f) > 0.0001f) return false;
            Vector2 snag = Shape(0.15f, 0.15f, inner, outer, curve);
            if (Magnitude(snag.x, snag.y) <= 0f) return false;
            Vector2 angled = Shape(0.6f, 0.3f, inner, outer, curve);
            float cross = 0.6f * angled.y - 0.3f * angled.x;
            if (Math.Abs(cross) > 0.0001f) return false;
            if (Math.Abs(LookAxis(0.4f, 0f) - 0.4f) > 0.0001f) return false;
            if (Math.Abs(LookAxis(-1.5f, 0f) + 1.5f) > 0.0001f) return false;
            float curved = LookAxis(0.5f, 1f);
            if (!(curved > 0f && curved < 0.5f)) return false;
            if (Math.Abs(LookAxis(1f, 1f) - 1f) > 0.001f) return false;
            LookStick(0.4f, -0.2f, 0f, out float ox, out float oy);
            if (Math.Abs(ox - 0.4f) > 0.0001f || Math.Abs(oy + 0.2f) > 0.0001f) return false;
            if (AxialHole(inner, outer, curve) > 0.0005f) return false;

            GameSettings edited = GameSettings.Defaults();
            edited.StickInner = 0.25f;
            edited.StickOuter = 0.9f;
            edited.StickCurve = 1.4f;
            edited.LookAccel = 0.5f;
            string blob = SettingsFile.Write(edited, ActionBinds.Defaults());
            GameSettings loaded = GameSettings.Defaults();
            SettingsFile.Read(blob, loaded, ActionBinds.Defaults());
            if (Math.Abs(loaded.StickInner - 0.25f) > 0.001f) return false;
            if (Math.Abs(loaded.StickOuter - 0.9f) > 0.001f) return false;
            if (Math.Abs(loaded.StickCurve - 1.4f) > 0.001f) return false;
            if (Math.Abs(loaded.LookAccel - 0.5f) > 0.001f) return false;

            GameSettings kept = GameSettings.Defaults();
            SettingsFile.Read("v=2\nmouse=1.8\n", kept, ActionBinds.Defaults());
            if (Math.Abs(kept.StickInner - inner) > 0.001f) return false;
            if (Math.Abs(kept.StickOuter - outer) > 0.001f) return false;
            if (Math.Abs(kept.StickCurve - curve) > 0.001f) return false;
            if (kept.LookAccel != 0f) return false;

            GameSettings clamped = GameSettings.Defaults();
            clamped.StickInner = 9f;
            clamped.StickOuter = 0f;
            clamped.StickCurve = 9f;
            clamped.LookAccel = -2f;
            clamped.Clamp();
            if (Math.Abs(clamped.StickInner - 0.5f) > 0.001f) return false;
            if (Math.Abs(clamped.StickOuter - 0.55f) > 0.001f) return false;
            if (Math.Abs(clamped.StickCurve - 3f) > 0.001f) return false;
            if (clamped.LookAccel != 0f) return false;
            return true;
        }

        static float FirstMotion(float x, float y, float inner, float outer, float curve)
        {
            float mag = (float)Math.Sqrt(x * x + y * y);
            if (mag < 0.000001f) return 0f;
            float ux = x / mag;
            float uy = y / mag;
            float lo = 0f;
            float hi = 1f;
            for (int i = 0; i < 16; i++)
            {
                float mid = (lo + hi) * 0.5f;
                Vector2 s = Shape(ux * mid, uy * mid, inner, outer, curve);
                if (Magnitude(s.x, s.y) > 0.0001f) hi = mid;
                else lo = mid;
            }
            return hi;
        }

        static string Num(float v)
        {
            return v.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Input-to-first-motion, in frames. Keyboard and pad move were already sampled
    /// in the same Update as the motor. Mouse look already reached the picture
    /// before render. The extra frame was the body yaw: the camera wrote it in
    /// LateUpdate, so the first Move of a look still used the previous heading.
    /// </summary>
    public static class ResponseLatency
    {
        public const int ReadOrder = -200;
        public const int LookOrder = -100;
        public const int MotorOrder = 0;
        public const int Kb = 0;
        public const int Mouse = 0;
        public const int Pad = 0;
        public const int Look = 0;
        public const int BeforeKb = 0;
        public const int BeforeMouse = 1;
        public const int BeforePad = 0;
        public const int BeforeLook = 1;

        public static string ProofLine()
        {
            return "response-latency"
                + " kb=" + Kb
                + " mouse=" + Mouse
                + " pad=" + Pad
                + " look=" + Look
                + " beforeKb=" + BeforeKb
                + " beforeMouse=" + BeforeMouse
                + " beforePad=" + BeforePad
                + " beforeLook=" + BeforeLook
                + " read=" + ReadOrder
                + " cam=" + LookOrder
                + " motor=" + MotorOrder
                + " vsync=" + FramePace.VsyncDefault
                + " rate=" + FramePace.TargetFor(FramePace.VsyncDefault)
                + " fixed=" + FramePace.FixedStep.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static bool Holds()
        {
            if (Kb != 0 || Mouse != 0 || Pad != 0 || Look != 0) return false;
            if (BeforeKb != 0 || BeforePad != 0) return false;
            if (BeforeMouse != 1 || BeforeLook != 1) return false;
            if (ReadOrder != -200 || LookOrder != -100 || MotorOrder != 0) return false;
            if (FramePace.VsyncDefault != 1) return false;
            if (FramePace.TargetFor(1) != -1) return false;
            if (FramePace.TargetFor(0) != 60) return false;
            if (Math.Abs(FramePace.FixedStep - 0.02f) > 0.0001f) return false;
            return true;
        }
    }

    /// <summary>
    /// Vsync on leaves the frame cap to the display. Vsync off falls back to 60.
    /// The fixed step stays 0.02. The motor still steps in Update.
    /// </summary>
    public static class FramePace
    {
        public const int VsyncDefault = 1;
        public const int Uncapped = -1;
        public const int FallbackRate = 60;
        public const float FixedStep = 0.02f;

        public static int TargetFor(int vsyncCount)
        {
            return vsyncCount > 0 ? Uncapped : FallbackRate;
        }
    }
}
