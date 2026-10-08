using System;
using System.IO;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Art-direction numbers for the couch menu. The spring, the hop, and the
    /// font files live here so the headless walk can hold them. Nothing here
    /// is a movement feel number.
    /// </summary>
    public static class MenuPolish
    {
        public const float SpringK = 80f;
        public const float SpringDamp = 6f;
        public const float HotScale = 1.05f;
        public const float HopHeight = 0.28f;
        public const string DisplayResource = "UI/Fonts/Bangers-Regular";
        public const string BodyResource = "UI/Fonts/LiberationSans-Bold";

        public static void Spring(ref float x, ref float v, float target, float dt)
        {
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            float force = (target - x) * SpringK;
            v += force * dt;
            v *= (float)Math.Exp(-SpringDamp * dt);
            x += v * dt;
        }

        public static float Hop(float life)
        {
            if (life <= 0f) return 0f;
            if (life > 1f) life = 1f;
            float t = 1f - life;
            return (float)Math.Sin(t * Math.PI) * HopHeight;
        }

        public static bool Holds(string root)
        {
            if (string.IsNullOrEmpty(root)) return false;
            string font = Path.Combine(root, "Assets", "UI", "Fonts", "Bangers-Regular.ttf");
            string ofl = Path.Combine(root, "Assets", "UI", "Fonts", "OFL-Bangers.txt");
            string body = Path.Combine(root, "Assets", "UI", "Fonts", "LiberationSans-Bold.ttf");
            if (!File.Exists(font) || !File.Exists(ofl) || !File.Exists(body)) return false;
            string license = File.ReadAllText(ofl);
            if (license.IndexOf("SIL Open Font License", StringComparison.Ordinal) < 0) return false;
            string credits = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "UI", "Menu", "MenuCatalog.cs"));
            if (credits.IndexOf("Bangers", StringComparison.Ordinal) < 0) return false;
            if (credits.IndexOf("Liberation Sans", StringComparison.Ordinal) < 0) return false;
            if (credits.IndexOf("SIL Open Font License", StringComparison.Ordinal) < 0) return false;
            if (!SpringSettles()) return false;
            if (Hop(1f) > 0.001f) return false;
            if (Hop(0.5f) < HopHeight * 0.9f) return false;
            if (Hop(0f) != 0f) return false;
            return true;
        }

        static bool SpringSettles()
        {
            float x = 1f;
            float v = 0f;
            float target = HotScale;
            float peak = x;
            float dt = 1f / 60f;
            for (int i = 0; i < 90; i++)
            {
                Spring(ref x, ref v, target, dt);
                if (x > peak) peak = x;
            }
            if (peak < target + 0.012f) return false;
            if (Math.Abs(x - target) > 0.02f) return false;
            return true;
        }
    }
}
