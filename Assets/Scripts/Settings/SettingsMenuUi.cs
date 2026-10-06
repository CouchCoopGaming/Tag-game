using Tag.Audio;
using Tag.Core;
using Tag.Onboard;
using UnityEngine;

namespace Tag.Settings
{
    /// <summary>
    /// Settings, rebind, and arena picker drawn over pause. Keyboard and the
    /// gamepad cursor use the same rows the menu-graph proof walks.
    /// </summary>
    public static class SettingsMenuUi
    {
        public enum Panel
        {
            None,
            Settings,
            Rebind,
            Arena,
            HowTo
        }

        public static Panel Which { get; private set; }
        public static bool Blocks => Which != Panel.None;
        public static bool Capturing { get; private set; }

        static int _focus;
        static int _captureAction = -1;
        static int _captureFrame = -1;
        static string _conflict = "";

        public static void Open(Panel panel)
        {
            Which = panel;
            _focus = 0;
            Capturing = false;
            _captureAction = -1;
            _conflict = "";
            if (panel == Panel.HowTo)
                HowToPlay.Ensure(ControlGlyphs.Device, ArenaRegistry.Count);
        }

        public static void ResetStatics()
        {
            Which = Panel.None;
            Capturing = false;
            _focus = 0;
            _captureAction = -1;
            _captureFrame = -1;
            _conflict = "";
        }

        public static void Close()
        {
            Which = Panel.None;
            Capturing = false;
            _captureAction = -1;
            _conflict = "";
        }

        public static void Poll()
        {
            if (!Blocks) return;
            if (Capturing)
            {
                PollCapture();
                return;
            }

            int rows = Rows();
            if (Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
            {
                Close();
                TagSfx.UiBack();
                return;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up)
                Nudge(-1, rows - 1);
            if (Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down)
                Nudge(1, rows - 1);
            if (Which == Panel.Arena)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Pressed(PlayAction.Arena1))
                {
                    _focus = 0;
                    SettingsRuntime.SelectArena(0);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Alpha2) || Pressed(PlayAction.Arena2))
                {
                    _focus = 1;
                    SettingsRuntime.SelectArena(1);
                    return;
                }
            }
            else
            {
                for (int i = 0; i < rows && i < 9; i++)
                {
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                        SetFocus(i);
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left)
                Step(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right)
                Step(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
                Activate();
        }

        public static void Draw()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            if (Which == Panel.Settings) DrawSettings(cx, cy);
            else if (Which == Panel.Rebind) DrawRebind(cx, cy);
            else if (Which == Panel.Arena) DrawArena(cx, cy);
            else if (Which == Panel.HowTo) DrawHowTo(cx, cy);
        }

        static void DrawSettings(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 250, cy - 280, 500, 600), "Settings");
            var s = GameSettings.Current ?? GameSettings.Defaults();
            float y = cy - 248f;
            for (int i = 0; i < MenuGraph.SettingsRows; i++)
            {
                if (Row(cx, y, i, s.RowLabel(i)))
                    Activate();
                y += 28f;
            }
            GUI.Label(new Rect(cx - 230, cy + 200, 460, 64),
                "Up / Down or the stick picks. Left / Right steps.\nEnter or South uses the row. East or Esc back.\nReplay tips restarts the first-run hints.\nComma mutes. M or Select toggles the minimap.");
        }

        static void DrawHowTo(float cx, float cy)
        {
            HowToPlay.Ensure(ControlGlyphs.Device, ArenaRegistry.Count);
            GUI.Box(new Rect(cx - 280, cy - 260, 560, 540), "How to play");
            float y = cy - 228f;
            for (int i = 0; i < HowToPlay.Count; i++)
            {
                GUI.Label(new Rect(cx - 260, y, 520, 22), HowToPlay.Line(i));
                y += 22f;
            }
            if (Row(cx, y + 6f, 0, "Back"))
                Activate();
        }

        static void DrawRebind(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 280, cy - 260, 560, 540), "Rebind");
            var binds = ActionBinds.Current ?? ActionBinds.Defaults();
            float y = cy - 230f;
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                string label = binds.RowLabel((PlayAction)i);
                if (Capturing && _captureAction == i) label = "Press a key or button…";
                if (Row(cx, y, i, label))
                    Activate();
                y += 26f;
            }
            if (Row(cx, y, (int)PlayAction.Count, "Reset bindings"))
                Activate();
            y += 26f;
            if (Row(cx, y, (int)PlayAction.Count + 1, "Back"))
                Activate();
            string foot = string.IsNullOrEmpty(_conflict)
                ? "Enter or South listens. Esc cancels a listen.\nJump stays Space until you change it. Reset puts it back."
                : _conflict;
            GUI.Label(new Rect(cx - 260, cy + 200, 520, 48), foot);
        }

        static void DrawArena(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 220, cy - 120, 440, 260), "Arena");
            int arena = GameSettings.Current != null ? GameSettings.Current.Arena : 0;
            string park = (arena == 0 ? "* " : "") + "1  PARK";
            string mega = (arena == 1 ? "* " : "") + "2  Mega Park";
            if (Row(cx, cy - 70, 0, park)) Activate();
            if (Row(cx, cy - 34, 1, mega)) Activate();
            if (Row(cx, cy + 10, 2, "Back")) Activate();
            GUI.Label(new Rect(cx - 200, cy + 52, 400, 64),
                "1 and 2 pick during play and in this list.\nD-pad left / right do the same on a pad.\nThe choice is saved. Esc or East back.");
        }

        static bool Row(float cx, float y, int index, string label)
        {
            var r = new Rect(cx - 210, y, 420, 26);
            bool sel = _focus == index;
            if (sel) GUI.Box(new Rect(r.x - 4f, r.y - 2f, r.width + 8f, r.height + 4f), "");
            if (!MenuClick.Button(r, (sel ? "> " : "  ") + label)) return false;
            _focus = index;
            return true;
        }

        static int Rows()
        {
            if (Which == Panel.Settings) return MenuGraph.SettingsRows;
            if (Which == Panel.Rebind) return MenuGraph.RebindRows;
            if (Which == Panel.Arena) return MenuGraph.ArenaRows;
            if (Which == Panel.HowTo) return MenuGraph.HowToRows;
            return 1;
        }

        static void Nudge(int dir, int max)
        {
            int next = _focus + dir;
            if (next < 0) next = 0;
            if (next > max) next = max;
            if (next == _focus) return;
            _focus = next;
            TagSfx.UiMove();
        }

        static void SetFocus(int index)
        {
            if (_focus == index) return;
            _focus = index;
            TagSfx.UiMove();
        }

        static void Step(int dir)
        {
            if (Which == Panel.Settings && _focus <= GameSettings.RowMinimap)
            {
                var s = GameSettings.Current ?? GameSettings.Defaults();
                GameSettings.Current = s;
                s.Nudge(_focus, dir);
                SettingsRuntime.Apply();
                SettingsRuntime.Save();
                AudioCuePlayer.Ensure()?.UiClick();
            }
            else if (Which == Panel.Rebind && _focus < (int)PlayAction.Count)
                CyclePreset((PlayAction)_focus, dir);
        }

        static void CyclePreset(PlayAction action, int dir)
        {
            var binds = ActionBinds.Current ?? ActionBinds.Defaults();
            ActionBinds.Current = binds;
            if (action == PlayAction.Move)
            {
                string next = binds.Keyboard[(int)action] == "wasd" ? "arrows" : "wasd";
                if (dir < 0) next = next == "wasd" ? "arrows" : "wasd";
                TryKeyboard(binds, action, next);
            }
            else if (action == PlayAction.Look)
            {
                string next = binds.Gamepad[(int)action] == "rightStick" ? "leftStick" : "rightStick";
                TryGamepad(binds, action, next);
            }
        }

        static void Activate()
        {
            if (Which == Panel.Settings)
            {
                if (_focus == GameSettings.RowReset)
                {
                    GameSettings.Current.ResetToDefaults();
                    SettingsRuntime.Apply();
                    SettingsRuntime.Save();
                    TagSfx.UiConfirm();
                    return;
                }
                if (_focus == GameSettings.RowReplay)
                {
                    OnboardingSession.Live.Replay();
                    OnboardingStore.Save(OnboardingSession.Live);
                    TagSfx.UiConfirm();
                    return;
                }
                if (_focus >= GameSettings.RowBack)
                {
                    Close();
                    TagSfx.UiBack();
                    return;
                }
                if (_focus == GameSettings.RowInvert || _focus == GameSettings.RowMute
                    || _focus == GameSettings.RowColorblind || _focus == GameSettings.RowMinimap)
                    Step(1);
                return;
            }
            if (Which == Panel.Rebind)
            {
                if (_focus == (int)PlayAction.Count)
                {
                    ActionBinds.Current.ResetToDefaults();
                    _conflict = "";
                    SettingsRuntime.Save();
                    AudioCuePlayer.Ensure()?.UiClick();
                    return;
                }
                if (_focus >= (int)PlayAction.Count + 1)
                {
                    Close();
                    AudioCuePlayer.Ensure()?.UiClick();
                    return;
                }
                Capturing = true;
                _captureAction = _focus;
                _captureFrame = Time.frameCount;
                _conflict = "";
                return;
            }
            if (Which == Panel.Arena)
            {
                if (_focus == 0) SettingsRuntime.SelectArena(0);
                else if (_focus == 1) SettingsRuntime.SelectArena(1);
                else
                {
                    Close();
                    AudioCuePlayer.Ensure()?.UiClick();
                }
                return;
            }
            if (Which == Panel.HowTo)
            {
                Close();
                AudioCuePlayer.Ensure()?.UiClick();
            }
        }

        static void PollCapture()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Capturing = false;
                _captureAction = -1;
                return;
            }
            if (Time.frameCount <= _captureFrame) return;
            string token = BindSampler.AnyPressedToken();
            if (string.IsNullOrEmpty(token) || token == "escape") return;
            var binds = (ActionBinds.Current ?? ActionBinds.Defaults()).Clone();
            var action = (PlayAction)_captureAction;
            bool pad = IsPad(token);
            if (pad) binds.SetGamepad(action, token);
            else binds.SetKeyboard(action, token);
            if (binds.Conflict(action, out PlayAction other))
            {
                _conflict = ActionBinds.Name(action) + " conflicts with " + ActionBinds.Name(other);
                Capturing = false;
                _captureAction = -1;
                AudioCuePlayer.Ensure()?.UiClick();
                return;
            }
            ActionBinds.Current = binds;
            SettingsRuntime.Save();
            Capturing = false;
            _captureAction = -1;
            _conflict = "";
            AudioCuePlayer.Ensure()?.UiClick();
        }

        static bool IsPad(string token)
        {
            return token == "buttonSouth" || token == "buttonEast" || token == "buttonWest"
                || token == "buttonNorth" || token == "leftShoulder" || token == "rightShoulder"
                || token == "leftStickPress" || token == "rightStickPress"
                || token == "start" || token == "select"
                || token == "dpadLeft" || token == "dpadRight" || token == "dpadUp" || token == "dpadDown";
        }

        static void TryKeyboard(ActionBinds binds, PlayAction action, string token)
        {
            var trial = binds.Clone();
            trial.SetKeyboard(action, token);
            if (trial.Conflict(action, out PlayAction other))
            {
                _conflict = ActionBinds.Name(action) + " conflicts with " + ActionBinds.Name(other);
                return;
            }
            ActionBinds.Current = trial;
            _conflict = "";
            SettingsRuntime.Save();
        }

        static void TryGamepad(ActionBinds binds, PlayAction action, string token)
        {
            var trial = binds.Clone();
            trial.SetGamepad(action, token);
            if (trial.Conflict(action, out PlayAction other))
            {
                _conflict = ActionBinds.Name(action) + " conflicts with " + ActionBinds.Name(other);
                return;
            }
            ActionBinds.Current = trial;
            _conflict = "";
            SettingsRuntime.Save();
        }

        static bool Pressed(PlayAction action)
        {
            return BindSampler.Pressed(action);
        }
    }
}
