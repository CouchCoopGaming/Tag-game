using Tag.Settings;

namespace Tag.Onboard
{
    public enum InputDeviceKind
    {
        Keyboard = 0,
        Gamepad = 1
    }

    /// <summary>
    /// One glyph lookup. The token is the current bind for the last-used device.
    /// Show() returns an existing string, so a prompt can read it every frame.
    /// </summary>
    public static class ControlGlyphs
    {
        public static InputDeviceKind Device;

        public static void Note(InputDeviceKind kind)
        {
            Device = kind;
        }

        public static void ResetStatics()
        {
            Device = InputDeviceKind.Keyboard;
        }

        public static string Glyph(PlayAction action)
        {
            return GlyphOf(action, ActionBinds.Current, Device);
        }

        public static string GlyphOf(PlayAction action, ActionBinds binds, InputDeviceKind device)
        {
            if (binds == null) return "";
            int i = (int)action;
            int n = (int)PlayAction.Count;
            if (i < 0 || i >= n) return "";
            string token = device == InputDeviceKind.Gamepad ? binds.Gamepad[i] : binds.Keyboard[i];
            return ActionBinds.Show(token);
        }
    }
}
