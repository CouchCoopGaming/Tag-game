using UnityEngine;

namespace Tag.Local
{
    /// <summary>Couch lobby: 1 = SP+Dummy, 2–4 = local couch (Dummy off).</summary>
    public static class LocalPlayerRoster
    {
        public const string PrefsCountKey = "Tag.LocalPlayerCount";
        public static int PlayerCount { get; private set; } = 1;

        public static void SetCount(int n)
        {
            PlayerCount = Mathf.Clamp(n, 1, 4);
            PlayerPrefs.SetInt(PrefsCountKey, PlayerCount);
            PlayerPrefs.Save();
        }

        public static void Load()
        {
            PlayerCount = Mathf.Clamp(PlayerPrefs.GetInt(PrefsCountKey, 1), 1, 4);
        }

        public static bool IsCouch => PlayerCount >= 2;
    }
}
