using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tag.Level
{
    /// <summary>
    /// Saves the arena pick and reloads Play so the bootstrap builds that park.
    /// </summary>
    public static class ParkArenaHost
    {
        public static void Choose(int id)
        {
            int next = id == ParkArena.Pocket ? ParkArena.Pocket : ParkArena.Mega;
            if (ParkArena.Id == next)
            {
                Remember(next);
                return;
            }
            ParkArena.Select(next);
            Remember(next);
            Time.timeScale = 1f;
            SceneManager.LoadScene("Play");
        }

        public static void Toggle()
        {
            Choose(ParkArena.IsPocket ? ParkArena.Mega : ParkArena.Pocket);
        }

        static void Remember(int id)
        {
            ParkArena.HasExplicitChoice = true;
            PlayerPrefs.SetInt(ParkArena.PrefsKey, id);
            PlayerPrefs.Save();
        }
    }
}
