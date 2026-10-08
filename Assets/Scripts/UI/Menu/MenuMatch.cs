using Tag.Core;
using Tag.Couch;
using Tag.Front;
using Tag.Level;
using Tag.Modes;
using Tag.Practice;
using Tag.Settings;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Writes the lobby into the settings the match already reads, then uses
    /// GameFlow.BeginFromMenu so TagModeController's start path stays the one
    /// that counts down and runs the round.
    /// </summary>
    public static class MenuMatch
    {
        public static void RememberRules()
        {
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = s;
            s.Clamp();
            SettingsRuntime.Apply();
            SettingsRuntime.Save();
            PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)MenuSession.Mode);
            PlayerPrefs.Save();
            GameFlow flow = GameFlow.Instance;
            if (flow != null) flow.SyncSelectedMode(MenuSession.Mode);
        }

        public static int ResolveArena()
        {
            if (!MenuSession.RandomArena)
            {
                int id = MenuSession.Arena;
                if (id < 0) id = 0;
                if (id >= ParkArena.Count) id = 0;
                return id;
            }
            return Random.Range(0, ParkArena.Count);
        }

        public static void StartMatch()
        {
            MenuSession.CommitLooks();
            int arena = ResolveArena();
            MenuSession.Arena = arena;
            ParkArena.Select(arena);
            ParkArena.HasExplicitChoice = true;
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = s;
            s.Arena = arena;
            s.Clamp();
            RememberRules();
            PlayerPrefs.SetInt(ParkArena.PrefsKey, arena);
            PlayerPrefs.Save();
            GameFlow flow = GameFlow.Instance;
            if (flow == null) return;
            flow.BeginFromMenu(true);
        }

        public static void StartPractice()
        {
            PracticeSession.Arm();
            if (CouchPlay.Humans < 1)
                CouchPlay.Join(CouchPlay.DeviceKeyboard);
            MenuSession.Mode = TagModeId.FreePlay;
            RememberRules();
            FrontSession.Armed = true;
            FrontSession.Screen = FrontScreen.Playing;
            GameFlow flow = GameFlow.Instance;
            if (flow == null) return;
            flow.BeginFromMenu(false);
        }
    }
}
