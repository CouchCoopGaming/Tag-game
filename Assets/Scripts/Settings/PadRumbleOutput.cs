using UnityEngine;

namespace Tag.Settings
{
    /// <summary>
    /// Applies the rumble slots once per frame. Keyboard seats are skipped.
    /// Motor writes only happen when the stored speeds change.
    /// </summary>
    public static class PadRumbleOutput
    {
        static int _frame = -1;
        static readonly float[] SentLow = new float[PadRumble.Seats];
        static readonly float[] SentHigh = new float[PadRumble.Seats];

        public static void Tick(float dt)
        {
            int frame = Time.frameCount;
            if (frame == _frame) return;
            _frame = frame;
            if (dt < 0f) dt = 0f;
            if (dt > 0.1f) dt = 0.1f;
            PadRumble.Decay(dt);
            CaptionFeed.Decay(dt);
            Apply();
        }

        static void Apply()
        {
#if ENABLE_INPUT_SYSTEM
            var all = UnityEngine.InputSystem.Gamepad.all;
            if (all.Count == 0) return;
            int n = all.Count;
            for (int i = 0; i < PadRumble.Seats; i++)
            {
                int pad = PadRumble.PadIndex(i);
                PadRumble.Motor(i, out float low, out float high);
                if (low == SentLow[i] && high == SentHigh[i]) continue;
                SentLow[i] = low;
                SentHigh[i] = high;
                if (pad < 0 || pad >= n) continue;
                all[pad].SetMotorSpeeds(low, high);
            }
#endif
        }
    }
}
