using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tag.Level
{
    /// <summary>
    /// Saves the arena pick and reloads Play so the bootstrap builds that park.
    /// </summary>
    public static class ParkArenaHost
    {
        /// <summary>Arena queued by the pause Map toggle. Applied on the next countdown, not mid-round.</summary>
        public static int Pending = -1;

        public static string MapButtonLabel()
        {
            if (Pending < 0)
                return "Map: " + ParkArena.DisplayName;
            string name = Pending == ParkArena.Pocket ? "Pocket Park" : "Mega Park";
            return "Next: " + name;
        }

        public static void Choose(int id)
        {
            Request(id, false);
        }

        /// <summary>
        /// Countdown keys apply now: teardown, then a fresh countdown on that arena.
        /// A live round only queues. Results then teardown then the next countdown.
        /// </summary>
        public static void Request(int id, bool roundLive)
        {
            int next = id == ParkArena.Pocket ? ParkArena.Pocket : ParkArena.Mega;
            if (roundLive)
            {
                Pending = next == ParkArena.Id ? -1 : next;
                return;
            }
            Pending = -1;
            Apply(next);
        }

        public static void Toggle(bool roundLive)
        {
            int shown = Pending >= 0 ? Pending : ParkArena.Id;
            int next = shown == ParkArena.Pocket ? ParkArena.Mega : ParkArena.Pocket;
            Request(next, roundLive);
        }

        public static void Toggle()
        {
            Toggle(false);
        }

        /// <summary>True when the scene is reloading onto the queued arena.</summary>
        public static bool ConsumePending()
        {
            if (Pending < 0)
                return false;
            int next = Pending;
            Pending = -1;
            if (next == ParkArena.Id)
            {
                Remember(next);
                return false;
            }
            Apply(next);
            return true;
        }

        static void Apply(int next)
        {
            if (ParkArena.Id == next)
            {
                Remember(next);
                return;
            }
            ParkArena.Select(next);
            Remember(next);
            Time.timeScale = 1f;
            MegaParkP1Bootstrap[] live = UnityEngine.Object.FindObjectsByType<MegaParkP1Bootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    live[i].TearDownArena();
            }
            SceneManager.LoadScene("Play");
        }

        static void Remember(int id)
        {
            ParkArena.HasExplicitChoice = true;
            PlayerPrefs.SetInt(ParkArena.PrefsKey, id);
            PlayerPrefs.Save();
        }
    }
}
