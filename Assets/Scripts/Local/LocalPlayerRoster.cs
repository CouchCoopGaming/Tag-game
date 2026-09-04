using UnityEngine;

namespace Tag.Local
{
    /// <summary>Couch lobby: player count 2–4 persisted for Play spawn.</summary>
    public static class LocalPlayerRoster
    {
        public const string PrefsCountKey = "Tag.LocalPlayerCount";
        public static int PlayerCount { get; private set; } = 2;

        public static void SetCount(int n)
        {
            PlayerCount = Mathf.Clamp(n, 2, 4);
            PlayerPrefs.SetInt(PrefsCountKey, PlayerCount);
            PlayerPrefs.Save();
        }

        public static void Load()
        {
            PlayerCount = Mathf.Clamp(PlayerPrefs.GetInt(PrefsCountKey, 2), 2, 4);
        }
    }
}
