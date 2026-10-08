namespace Tag.FX
{
    /// <summary>
    /// Spawn, peak, and fade for the owned verb FX. Alpha and scale both
    /// ease, so a card does not pop on or off. Nothing here writes the motor.
    /// </summary>
    public static class VerbFxEase
    {
        public static float Smooth(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }

        public static float Out(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            float k = 1f - u;
            return 1f - k * k;
        }

        /// <summary>0 at birth, full by the peak, 0 at the end.</summary>
        public static float Alpha(float age, float life)
        {
            if (life <= 0.0001f || age <= 0f || age >= life) return 0f;
            float u = age / life;
            const float peak = 0.18f;
            if (u < peak)
                return Smooth(u / peak);
            float f = (u - peak) / (1f - peak);
            return 1f - Out(f);
        }

        public static float Scale(float age, float life, float from, float to)
        {
            if (life <= 0.0001f) return to;
            float u = age / life;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return from + (to - from) * Out(u);
        }
    }
}
