using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Air-dash and punch key stubs. Defaults stay Q and LMB.
    /// Left Alt still dashes. E still punches. Choices are saved in PlayerPrefs.
    /// </summary>
    public static class ControlBinds
    {
        public const string DashPrefsKey = "Tag.AirDashKey";
        public const string PunchPrefsKey = "Tag.PunchKey";

        static readonly KeyCode[] DashSteps = { KeyCode.Q, KeyCode.V, KeyCode.Mouse4 };
        static readonly KeyCode[] PunchSteps = { KeyCode.Mouse0, KeyCode.F, KeyCode.Mouse3 };

        public static KeyCode AirDash { get; private set; } = KeyCode.Q;
        public static KeyCode Punch { get; private set; } = KeyCode.Mouse0;

        public static string DashName => AirDash == KeyCode.Mouse4 ? "Mouse4" : AirDash.ToString();

        public static string PunchName =>
            Punch == KeyCode.Mouse0 ? "LMB" :
            Punch == KeyCode.Mouse3 ? "Mouse3" :
            Punch.ToString();

        public static string Help =>
            "WASD move\n" +
            "Shift ski, or sprint when ski does not catch\n" +
            "Space jump\n" +
            "Ctrl or C slide (hold with speed)\n" +
            "Ctrl in air falls faster\n" +
            PunchName + " or E punch (passes It)\n" +
            DashName + " or Left Alt air dash (0.1 s, then 30 s)\n" +
            "MMB lunge when you are It, on the ground\n" +
            "RMB does not jet\n" +
            "F1 Hot Potato   F2 Least It   F3 Trail Tag   F4 Free play\n" +
            "Esc pause\n" +
            "Air dash key: " + DashName + "    Punch key: " + PunchName + "\n" +
            "Volume: " + Tag.Audio.AudioMaster.Label;

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

            int punchRaw = PlayerPrefs.GetInt(PunchPrefsKey, (int)KeyCode.Mouse0);
            Punch = KeyCode.Mouse0;
            for (int i = 0; i < PunchSteps.Length; i++)
            {
                if ((int)PunchSteps[i] == punchRaw)
                {
                    Punch = PunchSteps[i];
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

        public static void CyclePunch(int dir)
        {
            Load();
            int i = 0;
            for (int n = 0; n < PunchSteps.Length; n++)
            {
                if (PunchSteps[n] == Punch) i = n;
            }
            int next = Mathf.Clamp(i + dir, 0, PunchSteps.Length - 1);
            if (next == i) return;
            Punch = PunchSteps[next];
            PlayerPrefs.SetInt(PunchPrefsKey, (int)Punch);
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
                reader.punchKey = Punch;
            }
        }
    }
}
