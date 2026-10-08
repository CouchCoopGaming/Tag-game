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
            if (gamepad)
                lookX = StickQuality.LookAxis(lookX, Accel());
            return lookX * Sens(gamepad);
        }

        public static float PitchDelta(float lookY, bool gamepad)
        {
            if (gamepad)
                lookY = StickQuality.LookAxis(lookY, Accel());
            if (GameSettings.Current != null && GameSettings.Current.InvertY)
                lookY = -lookY;
            return lookY * Sens(gamepad);
        }

        /// <summary>
        /// Yaw and pitch for this frame. Mouse is a straight multiply.
        /// Gamepad uses the accel curve only when LookAccel is above 0.
        /// </summary>
        public static void Deltas(float lookX, float lookY, bool gamepad, out float yaw, out float pitch)
        {
            if (gamepad)
                StickQuality.LookStick(lookX, lookY, Accel(), out lookX, out lookY);
            if (GameSettings.Current != null && GameSettings.Current.InvertY)
                lookY = -lookY;
            float sens = Sens(gamepad);
            yaw = lookX * sens;
            pitch = lookY * sens;
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

        static float Accel()
        {
            GameSettings s = GameSettings.Current;
            if (s == null) return GameSettings.LookAccelDefault;
            return s.LookAccel;
        }
    }
}
