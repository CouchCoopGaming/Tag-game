using System;
using System.Globalization;
using System.Text;

namespace Tag.Settings
{
    /// <summary>
    /// Line-oriented blob. PlayerPrefs and tag-settings.json store the same text.
    /// Missing lines keep the defaults the caller started from.
    /// </summary>
    public static class SettingsFile
    {
        public static string Write(GameSettings settings, ActionBinds binds)
        {
            var s = settings ?? GameSettings.Defaults();
            var b = binds ?? ActionBinds.Defaults();
            var text = new StringBuilder();
            text.Append("v=1\n");
            Line(text, "mouse", s.MouseSensitivity);
            Line(text, "padLook", s.GamepadLook);
            Line(text, "invertY", s.InvertY ? 1f : 0f);
            Line(text, "fov", s.Fov);
            Line(text, "master", s.Master);
            Line(text, "sfx", s.Sfx);
            Line(text, "ui", s.Ui);
            Line(text, "music", s.Music);
            Line(text, "mute", s.Muted ? 1f : 0f);
            Line(text, "hud", s.HudScale);
            Line(text, "colorblind", s.Colorblind ? 1f : 0f);
            Line(text, "minimap", s.Minimap ? 1f : 0f);
            Line(text, "arena", s.Arena);
            Line(text, "ai", s.AiOpponents);
            Line(text, "diff", s.DifficultyTier);
            Line(text, "roundLen", s.RoundLengthIndex);
            Line(text, "rounds", s.RoundsPerMatch);
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                var action = (PlayAction)i;
                text.Append("kb.");
                text.Append(action.ToString());
                text.Append('=');
                text.Append(b.Keyboard[i] ?? "");
                text.Append('\n');
                text.Append("pad.");
                text.Append(action.ToString());
                text.Append('=');
                text.Append(b.Gamepad[i] ?? "");
                text.Append('\n');
            }
            return text.ToString();
        }

        public static void Read(string blob, GameSettings settings, ActionBinds binds)
        {
            if (settings == null || binds == null || string.IsNullOrEmpty(blob)) return;
            string[] lines = blob.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                Apply(settings, binds, key, value);
            }
            settings.Clamp();
        }

        static void Apply(GameSettings settings, ActionBinds binds, string key, string value)
        {
            if (key == "mouse") settings.MouseSensitivity = Num(value, settings.MouseSensitivity);
            else if (key == "padLook") settings.GamepadLook = Num(value, settings.GamepadLook);
            else if (key == "invertY") settings.InvertY = Flag(value);
            else if (key == "fov") settings.Fov = Num(value, settings.Fov);
            else if (key == "master") settings.Master = Num(value, settings.Master);
            else if (key == "sfx") settings.Sfx = Num(value, settings.Sfx);
            else if (key == "ui") settings.Ui = Num(value, settings.Ui);
            else if (key == "music") settings.Music = Num(value, settings.Music);
            else if (key == "mute") settings.Muted = Flag(value);
            else if (key == "hud") settings.HudScale = Num(value, settings.HudScale);
            else if (key == "colorblind") settings.Colorblind = Flag(value);
            else if (key == "minimap") settings.Minimap = Flag(value);
            else if (key == "arena") settings.Arena = (int)Num(value, settings.Arena);
            else if (key == "ai") settings.AiOpponents = (int)Num(value, settings.AiOpponents);
            else if (key == "diff") settings.DifficultyTier = (int)Num(value, settings.DifficultyTier);
            else if (key == "roundLen") settings.RoundLengthIndex = (int)Num(value, settings.RoundLengthIndex);
            else if (key == "rounds") settings.RoundsPerMatch = (int)Num(value, settings.RoundsPerMatch);
            else if (key.StartsWith("kb.", StringComparison.Ordinal))
                Assign(binds, key.Substring(3), value, true);
            else if (key.StartsWith("pad.", StringComparison.Ordinal))
                Assign(binds, key.Substring(4), value, false);
        }

        static void Assign(ActionBinds binds, string name, string value, bool keyboard)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (!Enum.TryParse(name, false, out PlayAction action)) return;
            if (action < 0 || action >= PlayAction.Count) return;
            if (keyboard) binds.SetKeyboard(action, value);
            else binds.SetGamepad(action, value);
        }

        static float Num(string value, float fallback)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float n))
                return n;
            return fallback;
        }

        static bool Flag(string value)
        {
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        static void Line(StringBuilder text, string key, float value)
        {
            text.Append(key);
            text.Append('=');
            text.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            text.Append('\n');
        }
    }
}
