namespace Tag.Settings
{
    /// <summary>
    /// Camera multipliers. At the defaults (mouse 1.8, invert off, FOV 78)
    /// yaw, pitch, and field of view match the motor config.
    /// </summary>
    public static class LookFeel
    {
        public static float YawDelta(float lookX, bool gamepad)
        {
            return lookX * Sens(gamepad);
        }

        public static float PitchDelta(float lookY, bool gamepad)
        {
            if (GameSettings.Current != null && GameSettings.Current.InvertY)
                lookY = -lookY;
            return lookY * Sens(gamepad);
        }

        public static float ScaleFov(float target)
        {
            float fov = GameSettings.Current != null ? GameSettings.Current.Fov : GameSettings.FovDefault;
            if (fov < 1f) fov = GameSettings.FovDefault;
            return target * (fov / GameSettings.FovDefault);
        }

        static float Sens(bool gamepad)
        {
            GameSettings s = GameSettings.Current;
            if (s == null) return gamepad ? GameSettings.PadLookDefault : GameSettings.MouseDefault;
            return gamepad ? s.GamepadLook : s.MouseSensitivity;
        }
    }
}
