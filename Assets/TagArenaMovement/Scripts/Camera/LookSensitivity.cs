using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Look-speed stub. Default 1.8 matches the camera field, so an untouched install feels the same.
    /// </summary>
    public static class LookSensitivity
    {
        public const string PrefsKey = "Tag.LookSensitivity";
        public const float Default = 1.8f;

        static readonly float[] Steps = { 1.0f, 1.4f, 1.8f, 2.4f, 3.2f };
        static readonly string[] Names = { "Low", "Lower", "Default", "Higher", "High" };

        public static float Current { get; private set; } = Default;

        public static string Label
        {
            get
            {
                Load();
                int i = IndexOf(Current);
                return Names[i] + "  " + Current.ToString("0.0");
            }
        }

        public static void Load()
        {
            Current = Nearest(PlayerPrefs.GetFloat(PrefsKey, Default));
        }

        public static void Cycle(int dir)
        {
            Load();
            int i = IndexOf(Current);
            int next = Mathf.Clamp(i + dir, 0, Steps.Length - 1);
            if (next == i) return;
            Current = Steps[next];
            PlayerPrefs.SetFloat(PrefsKey, Current);
            PlayerPrefs.Save();
            Apply();
            Tag.Audio.TagSfx.UiClick();
        }

        public static void Apply()
        {
            Load();
            foreach (var cam in Object.FindObjectsByType<TpsMoveCamera>(FindObjectsSortMode.None))
                cam.sensitivity = Current;
            foreach (var cam in Object.FindObjectsByType<FpsMoveCamera>(FindObjectsSortMode.None))
                cam.sensitivity = Current;
        }

        static int IndexOf(float v)
        {
            int best = 0;
            float bestD = Mathf.Abs(Steps[0] - v);
            for (int i = 1; i < Steps.Length; i++)
            {
                float d = Mathf.Abs(Steps[i] - v);
                if (d < bestD)
                {
                    best = i;
                    bestD = d;
                }
            }
            return best;
        }

        static float Nearest(float v) => Steps[IndexOf(v)];
    }
}
