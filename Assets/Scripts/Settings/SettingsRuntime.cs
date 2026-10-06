using System;
using System.IO;
using Tag.Audio;
using Tag.Core;
using Tag.Level;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Settings
{
    /// <summary>
    /// Loads the settings blob from PlayerPrefs, then from tag-settings.json,
    /// and writes both on every change. Boot calls Load.
    /// </summary>
    public static class SettingsRuntime
    {
        public const string PrefsKey = "Tag.GameSettingsJson";
        public const string FileName = "tag-settings.json";

        static int _hotkeyFrame = -1;
        static bool _applying;

        public static void Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json))
                json = ReadFile();
            bool stored = !string.IsNullOrEmpty(json);
            var settings = GameSettings.Defaults();
            var binds = ActionBinds.Defaults();
            if (stored)
                SettingsFile.Read(json, settings, binds);
            else
            {
                settings.MouseSensitivity = LookSensitivity.Current;
                settings.Master = AudioMaster.Volume;
                settings.Muted = AudioMaster.Muted;
                settings.Music = AudioMaster.MusicVolume;
            }
            if (PlayerPrefs.HasKey(ParkArena.PrefsKey))
                settings.Arena = PlayerPrefs.GetInt(ParkArena.PrefsKey, settings.Arena);
            settings.Clamp();
            bool repaired = false;
            if (binds.AnyConflict(out _, out _))
            {
                binds.ResetToDefaults();
                repaired = true;
            }
            GameSettings.Current = settings;
            ActionBinds.Current = binds;
            Apply();
            if (stored || repaired)
                Save();
        }

        public static void Save()
        {
            if (GameSettings.Current == null) GameSettings.Current = GameSettings.Defaults();
            if (ActionBinds.Current == null) ActionBinds.Current = ActionBinds.Defaults();
            GameSettings.Current.Clamp();
            string json = SettingsFile.Write(GameSettings.Current, ActionBinds.Current);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
            try
            {
                string dir = Application.persistentDataPath;
                if (!string.IsNullOrEmpty(dir))
                    SettingsFile.CommitText(Path.Combine(dir, FileName), json);
            }
            catch (Exception)
            {
                // PlayerPrefs already has the blob.
            }
        }

        public static void Apply()
        {
            if (_applying) return;
            _applying = true;
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            LookSensitivity.Assign(s.MouseSensitivity);
            AudioMaster.ApplyFromSettings(s.Master, s.Muted);
            AudioMaster.ApplyMusicFromSettings(s.Music);
            _applying = false;
        }

        public static void NoteAudio(float master, bool muted)
        {
            if (_applying) return;
            if (GameSettings.Current == null) GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.Master = master;
            GameSettings.Current.Muted = muted;
            GameSettings.Current.Clamp();
            Save();
        }

        public static void NoteMusic(float music)
        {
            if (_applying) return;
            if (GameSettings.Current == null) GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.Music = music;
            GameSettings.Current.Clamp();
            Save();
        }

        public static void NoteMouse(float sensitivity)
        {
            if (_applying) return;
            if (GameSettings.Current == null) GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.MouseSensitivity = sensitivity;
            GameSettings.Current.Clamp();
            Save();
        }

        public static void ToggleMinimap()
        {
            if (GameSettings.Current == null) GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.Minimap = !GameSettings.Current.Minimap;
            Save();
            TagSfx.UiClick();
        }

        public static void SelectArena(int arena)
        {
            bool live = false;
            if (TagModeController.Instance != null)
            {
                MatchPhase phase = TagModeController.Instance.Phase;
                live = phase == MatchPhase.Playing || phase == MatchPhase.PostRound;
            }
            ParkArenaHost.Request(arena, live);
            TagSfx.UiClick();
        }

        public static void PollHotkeys()
        {
            if (_hotkeyFrame == Time.frameCount) return;
            _hotkeyFrame = Time.frameCount;
            if (SettingsMenuUi.Capturing) return;
            if (ActionBinds.Current == null) ActionBinds.Current = ActionBinds.Defaults();
            if (BindSampler.Pressed(PlayAction.Minimap))
                ToggleMinimap();
            if (!PlayHotkeys()) return;
            if (BindSampler.Pressed(PlayAction.Arena1)) SelectArena(ParkArena.Mega);
            else if (BindSampler.Pressed(PlayAction.Arena2)) SelectArena(ParkArena.Pocket);
            else if (BindSampler.Pressed(PlayAction.Arena3)) SelectArena(ParkArena.Stack);
        }

        static bool PlayHotkeys()
        {
            if (SettingsMenuUi.Blocks) return false;
            if (GameFlow.Instance != null && GameFlow.Instance.State != GameFlowState.Play)
                return false;
            if (TagModeController.Instance != null && TagModeController.Instance.Phase == MatchPhase.Results)
                return false;
            return true;
        }

        static string ReadFile()
        {
            try
            {
                string dir = Application.persistentDataPath;
                if (string.IsNullOrEmpty(dir)) return "";
                string path = Path.Combine(dir, FileName);
                return SettingsFile.ReadStable(path);
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
