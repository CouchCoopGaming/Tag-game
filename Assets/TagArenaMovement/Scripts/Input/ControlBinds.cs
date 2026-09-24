using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Air-dash key stub. Default stays Q. Left Alt still dashes either way.
    /// The choice is saved in PlayerPrefs, same pattern as look sensitivity.
    /// </summary>
    public static class ControlBinds
    {
        public const string DashPrefsKey = "Tag.AirDashKey";

        static readonly KeyCode[] DashSteps = { KeyCode.Q, KeyCode.V, KeyCode.Mouse4 };

        public static KeyCode AirDash { get; private set; } = KeyCode.Q;

        public static string DashName => AirDash == KeyCode.Mouse4 ? "Mouse4" : AirDash.ToString();

        public static string Help =>
            "WASD move\n" +
            "Shift ski, or sprint when ski does not catch\n" +
            "Space jump\n" +
            "Ctrl or C slide (hold with speed)\n" +
            "Ctrl in air falls faster\n" +
            "LMB or E punch (passes It)\n" +
            DashName + " or Left Alt air dash (0.1 s, then 30 s)\n" +
            "MMB lunge when you are It, on the ground\n" +
            "RMB does not jet\n" +
            "F1 Hot Potato   F2 Least It   F3 Trail Tag   F4 Free play\n" +
            "Esc pause\n" +
            "Air dash key: " + DashName;

        public static void Load()
        {
            int raw = PlayerPrefs.GetInt(DashPrefsKey, (int)KeyCode.Q);
            AirDash = KeyCode.Q;
            for (int i = 0; i < DashSteps.Length; i++)
            {
                if ((int)DashSteps[i] == raw)
                {
                    AirDash = DashSteps[i];
                    break;
                }
            }
        }

        public static void CycleDash(int dir)
        {
            Load();
            int i = 0;
            for (int n = 0; n < DashSteps.Length; n++)
            {
                if (DashSteps[n] == AirDash) i = n;
            }
            int next = Mathf.Clamp(i + dir, 0, DashSteps.Length - 1);
            if (next == i) return;
            AirDash = DashSteps[next];
            PlayerPrefs.SetInt(DashPrefsKey, (int)AirDash);
            PlayerPrefs.Save();
            Apply();
            Tag.Audio.TagSfx.UiClick();
        }

        public static void Apply()
        {
            Load();
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))
            {
                if (reader == null || reader.ExternalControl) continue;
                reader.airDashKey = AirDash;
            }
        }
    }
}
