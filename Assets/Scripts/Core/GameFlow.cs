using UnityEngine;
using UnityEngine.SceneManagement;
using Tag.Couch;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Local;
using Tag.Audio;
using Tag.Front;
using Tag.Level;
using Tag.Practice;
using Tag.Settings;
using TagArena.Movement;

namespace Tag.Core
{
    public enum GameFlowState
    {
        Boot,
        PlayerCount,
        ModeSelect,
        Play,
        Paused,
        RoundEnd,
        Rematch,
        Setup
    }

    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        public static void ResetStatics()
        {
            Instance = null;
        }

        [SerializeField] string bootSceneName = "Boot";
        [SerializeField] string playSceneName = "Play";
        AsyncOperation _bootLoad;

        public TagModeController modeController;
        public TagRoundController round;

        public GameFlowState State { get; private set; } = GameFlowState.Boot;
        public TagModeId SelectedMode { get; private set; } = TagModeId.LeastIt;
        public string LastResultMessage { get; private set; } = "";

        int _menuCursor = 1;
        int _playerCountCursor;
        int _bootFocus;
        bool _modeFromWhoPlays;
        int _pauseFocus;
        int _looseResultsFocus;
        float _looseResultsReadyAt;
        int _controlsFocus;
        int _lookFocus;
        int _audioFocus;
        bool _settingsOpen;
        bool _controlsOpen;
        bool _audioOpen;
        bool _firstBoot;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            LocalPlayerRoster.Load();
            LookSensitivity.Load();
            _playerCountCursor = Mathf.Clamp(LocalPlayerRoster.PlayerCount - 1, 0, 3);
            _firstBoot = PlayerPrefs.GetInt("Tag.BootSeen", 0) == 0;
            LookSensitivity.Load();
            ControlBinds.Load();
            AudioMaster.Load();
            SettingsRuntime.Load();
            AudioCuePlayer.Ensure();
            FrontHooks.Act = ApplyFront;
            FrontHooks.Sound = PlayFrontSound;
            if (PlayerPrefs.HasKey(TagModeController.PrefsModeKey))
            {
                SelectedMode = (TagModeId)PlayerPrefs.GetInt(TagModeController.PrefsModeKey, (int)TagModeId.LeastIt);
                _menuCursor = (int)SelectedMode;
            }
            ParkArena.ApplySaved(
                PlayerPrefs.HasKey(ParkArena.PrefsKey),
                PlayerPrefs.GetInt(ParkArena.PrefsKey, ParkArena.Mega),
                ParkArena.Mega);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            FrontHooks.Act = ApplyFront;
            FrontHooks.Sound = PlayFrontSound;
            var scene = SceneManager.GetActiveScene().name;
            bool boot = scene == bootSceneName || scene == "Boot";
            if (boot || !FrontSession.Armed)
            {
                if (FrontSession.Screen == FrontScreen.Setup || FrontSession.Screen == FrontScreen.Join)
                    State = GameFlowState.Setup;
                else
                {
                    State = GameFlowState.Boot;
                    if (!FrontSession.Armed && FrontSession.Screen != FrontScreen.Practice)
                        FrontSession.ShowTitle();
                }
                HoldMenuClock();
                return;
            }
            State = GameFlowState.Play;
            MarkBootSeen();
            EnsurePlayHelpers();
            LookSensitivity.Load();
            LookSensitivity.Apply();
            EnsureRoundStarted();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != playSceneName && scene.name != "Play")
                return;
            if (!FrontSession.Armed)
            {
                State = GameFlowState.Boot;
                if (FrontSession.Screen != FrontScreen.Practice)
                    FrontSession.ShowTitle();
                HoldMenuClock();
                return;
            }
            State = GameFlowState.Play;
            EnsurePlayHelpers();
            LookSensitivity.Load();
            LookSensitivity.Apply();
            EnsureRoundStarted();
        }

        void HoldMenuClock()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == playSceneName || scene == "Play")
                Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void EnsurePlayHelpers()
        {
            if (FindFirstObjectByType<LocalPlayerSpawner>() == null)
            {
                var go = new GameObject("LocalMultiplayer");
                go.AddComponent<LocalPlayerSpawner>();
                go.AddComponent<LocalSplitCamera>();
            }
            else
                FindFirstObjectByType<LocalSplitCamera>()?.Apply();
        }

        public void PlayLeastItSlice()
        {
            MarkBootSeen();
            CouchPlay.Release();
            LocalPlayerRoster.SetCount(1);
            SelectedMode = TagModeId.LeastIt;
            _menuCursor = (int)TagModeId.LeastIt;
            PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)TagModeId.LeastIt);
            PlayerPrefs.Save();
            AudioCuePlayer.Ensure()?.UiConfirm();
            FrontSession.Arm();
            GoToPlay();
        }

        public void GoToPlayerCount()
        {
            _playerCountCursor = Mathf.Clamp(LocalPlayerRoster.PlayerCount - 1, 0, 3);
            State = GameFlowState.PlayerCount;
            AudioCuePlayer.Ensure()?.UiClick();
        }

        void MarkBootSeen()
        {
            if (PlayerPrefs.GetInt("Tag.BootSeen", 0) != 0)
            {
                _firstBoot = false;
                return;
            }
            PlayerPrefs.SetInt("Tag.BootSeen", 1);
            PlayerPrefs.Save();
            _firstBoot = false;
        }
        public void SyncSelectedMode(TagModeId id)
        {
            SelectedMode = id;
            _menuCursor = (int)id;
        }

        public void GoToModeSelect()
        {
            _menuCursor = Mathf.Clamp((int)SelectedMode, 0, 3);
            State = GameFlowState.ModeSelect;
            AudioCuePlayer.Ensure()?.UiClick();
        }

        public void ConfirmModeAndPlay()
        {
            SelectedMode = (TagModeId)_menuCursor;
            PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)SelectedMode);
            PlayerPrefs.Save();
            AudioCuePlayer.Ensure()?.UiConfirm();
            FrontSession.Arm();
            GoToPlay();
        }

        public void GoToPlay()
        {
            MarkBootSeen();
            int humans = CouchPlay.Humans;
            if (humans < 1) humans = 1;
            if (humans > 4) humans = 4;
            LocalPlayerRoster.SetCount(humans);
            CloseMenuPanels();
            State = GameFlowState.Play;
            LookSensitivity.Load();
            LookSensitivity.Apply();
            Time.timeScale = 1f;
            if (SceneManager.GetActiveScene().name != playSceneName)
                BeginAsyncLoad(playSceneName);
            else
            {
                EnsurePlayHelpers();
                EnsureRoundStarted();
            }
        }

        /// <summary>
        /// Same hold as the menu boot load. Progress stalls at 0.9 until
        /// AdvanceBootLoad allows activation, so the bar can reach 1.
        /// </summary>
        void BeginAsyncLoad(string sceneName)
        {
            _bootLoad = SceneManager.LoadSceneAsync(sceneName);
            if (_bootLoad == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            _bootLoad.allowSceneActivation = false;
        }

        /// <summary>
        /// Menu confirm. Reloads Play so the roster, looks, and arena spawn clean.
        /// The countdown still belongs to TagModeController.StartRound.
        /// </summary>
        public void BeginFromMenu(bool fillRoster)
        {
            MarkBootSeen();
            if (fillRoster)
                FrontSession.Arm();
            else if (!FrontSession.Armed)
                FrontSession.Armed = true;
            int humans = CouchPlay.Humans;
            if (humans < 1) humans = 1;
            if (humans > 4) humans = 4;
            LocalPlayerRoster.SetCount(humans);
            CloseMenuPanels();
            State = GameFlowState.Play;
            LookSensitivity.Load();
            LookSensitivity.Apply();
            Time.timeScale = 1f;
            BeginAsyncLoad(playSceneName);
        }

        /// <summary>
        /// Scene load progress while the menu is still up. -1 before the load
        /// starts. AsyncOperation stalls at 0.9 until activation, then reaches 1.
        /// </summary>
        public float BootLoad
        {
            get
            {
                if (_bootLoad == null) return -1f;
                if (_bootLoad.isDone) return 1f;
                return _bootLoad.progress;
            }
        }

        public void ClearBootLoad()
        {
            _bootLoad = null;
        }

        public void AdvanceBootLoad()
        {
            if (_bootLoad == null || _bootLoad.allowSceneActivation) return;
            if (_bootLoad.progress >= 0.9f)
                _bootLoad.allowSceneActivation = true;
        }

        public void OnRoundEnded(string result = "")
        {
            CloseMenuPanels();
            LastResultMessage = result ?? "";
            State = GameFlowState.RoundEnd;
            _looseResultsFocus = 0;
            _looseResultsReadyAt = Time.unscaledTime + 0.25f;
            Time.timeScale = 1f;
            // Unlock so Rematch/Menu clicks on the results card work (pause already unlocks).
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            AudioBus.RaiseRoundEnd(LastResultMessage);
        }

        // Compat for older callers
        public void OnRoundEnded() => OnRoundEnded(LastResultMessage);

        public void Rematch()
        {
            FrontSession.NoteRematch();
            FrontLive.BeginMatch();
            AudioCuePlayer.Ensure()?.UiConfirm();
            ClearPauseEdges();
            ReturnToPlay();
            if (modeController == null) modeController = FindFirstObjectByType<TagModeController>();
            if (modeController != null) modeController.Rematch();
            else BeginAsyncLoad(playSceneName);
        }

        /// <summary>
        /// F1-F4 and rematch leave RoundEnd / Pause. Otherwise R still rematches
        /// the new round, and a pause leaves timeScale at 0 so the countdown never finishes.
        /// </summary>
        public void ReturnToPlay()
        {
            // F-keys call this even mid-round, when the state guard below no-ops.
            CloseMenuPanels();
            if (State != GameFlowState.Paused && State != GameFlowState.RoundEnd)
                return;
            State = GameFlowState.Play;
            Time.timeScale = 1f;
            ResumeInputGate.LockPlayCursor();
            ArmLocalLookPunchGate();
        }

        public void QuitToMenu()
        {
            MarkBootSeen();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            TagSfx.UiBack();
            AudioCuePlayer.Ensure()?.StopMusic();
            CloseMenuPanels();
            FrontLive.ReleaseScene();
            StaticLifecycle.ReleaseMatch();
            FrontHooks.Act = ApplyFront;
            FrontHooks.Sound = PlayFrontSound;
            State = GameFlowState.Boot;
            _bootFocus = 0;
            if (SceneManager.GetActiveScene().name != bootSceneName)
                BeginAsyncLoad(bootSceneName);
        }

        public void OpenSetup()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            TagSfx.UiConfirm();
            AudioCuePlayer.Ensure()?.StopMusic();
            CloseMenuPanels();
            FrontLive.ReleaseScene();
            FrontSession.ShowSetup();
            FrontLive.Reset();
            State = GameFlowState.Setup;
            if (SceneManager.GetActiveScene().name != bootSceneName)
                SceneManager.LoadScene(bootSceneName);
        }

        /// <summary>
        /// Controls / Look / Audio are drawn before the pause card. F-keys and results
        /// leave that card without TogglePause, so the panel would stay up over play,
        /// steal results arrows, and hide Boot after Q.
        /// </summary>
        static string CouchRoster(int count)
        {
            if (count <= 1) return "1 human, bot off";
            if (count == 2) return "2 humans, bot off";
            if (count == 3) return "3 humans, bot off";
            return "4 humans, bot off";
        }

        string _looseLine;
        string _looseArm;
        string _looseMenu;
        string _looseMsg;

        string LooseResultsLine(string arm, string menu)
        {
            if (_looseLine != null && _looseArm == arm && _looseMenu == menu && _looseMsg == LastResultMessage)
                return _looseLine;
            _looseArm = arm;
            _looseMenu = menu;
            _looseMsg = LastResultMessage;
            _looseLine = LastResultMessage + "\n\n" + arm + "    " + menu
                + "\n1-2 or Left / Right    Enter / Space    R    Q";
            return _looseLine;
        }

        void CloseMenuPanels()
        {
            _controlsOpen = false;
            _settingsOpen = false;
            _audioOpen = false;
            SettingsMenuUi.Close();
        }


        static void ArmLocalLookPunchGate()
        {
            foreach (var reader in Object.FindObjectsByType<TagArena.Movement.PlayerInputReader>(FindObjectsSortMode.None))
                reader?.ArmLookPunchGate(2);
        }

        static bool CouchPause()
        {
            if (CouchPlay.Humans < 2) return false;
            for (int i = 0; i < CouchPlay.Max; i++)
            {
                if (!CouchPlay.HumanAt(i)) continue;
                int device = CouchPlay.DeviceOf(i);
                ActionBinds binds = CouchPlay.BindsFor(device);
                if (binds != null && BindSampler.PressedDevice(binds, PlayAction.Pause, device))
                    return true;
            }
            return false;
        }

        void TogglePause()
        {
            if (State == GameFlowState.Play)
            {
                State = GameFlowState.Paused;
                _pauseFocus = 0;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                PadRumble.Silence();
                AudioMix.SetWorldPaused(true);
                if (PadNav.StartDevice > 0)
                    CouchPlay.OpenPauseFrom(PadNav.StartDevice);
                ClearPauseEdges();
                AudioCuePlayer.Ensure()?.UiClick();
            }
            else if (State == GameFlowState.Paused)
            {
                State = GameFlowState.Play;
                Time.timeScale = 1f;
                AudioMix.SetWorldPaused(false);
                ResumeInputGate.LockPlayCursor();
                Cursor.visible = false;
                CloseMenuPanels();
                ArmLocalLookPunchGate();
                AudioCuePlayer.Ensure()?.UiClick();
            }
        }

        void ClearPauseEdges()
        {
            foreach (var motor in Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None))
            {
                if (motor == null) continue;
                var loco = motor.GetComponentInChildren<Tag.Art.DummyLocomotor>();
                loco?.CancelPunchTelegraph();
            }
        }

        void EnsureRoundStarted()
        {
            if (modeController == null)
                modeController = FindFirstObjectByType<TagModeController>();
            if (modeController == null)
            {
                var legacy = FindFirstObjectByType<TagRoundController>();
                if (legacy != null)
                {
                    modeController = legacy.GetComponent<TagModeController>();
                    if (modeController == null)
                        modeController = legacy.gameObject.AddComponent<TagModeController>();
                }
            }
            if (modeController == null) return;
            FrontLive.Apply();
            modeController.SelectedMode = SelectedMode;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
                modeController.RegisterPlayer(p);
            if (!modeController.RoundActive)
                modeController.StartRound();
            AudioCuePlayer.Ensure()?.PlaygroundMusic();
        }

        void Update()
        {
            AdvanceBootLoad();
            PadNav.Poll();
            SettingsRuntime.PollHotkeys();
            // Before panel returns, so Comma / N still work on Controls, Look, Boot, and results.
            AudioMaster.PollMuteHotkeys();

            // Subpanels belong on Boot and the pause card. Any other state drops them
            // before Esc/Enter can hit both the panel and results or play.
            if (_audioOpen || _controlsOpen || _settingsOpen)
            {
                if (State != GameFlowState.Boot && State != GameFlowState.Paused)
                    CloseMenuPanels();
            }

            if (_audioOpen)
            {
                PollAudioKeys();
                return;
            }

            if (_controlsOpen)
            {
                PollControlsKeys();
                return;
            }

            if (_settingsOpen)
            {
                PollLookKeys();
                return;
            }

            if (SettingsMenuUi.Blocks)
            {
                SettingsMenuUi.Poll();
                return;
            }

            CouchDevices.PollHotplug();
            bool pauseEdge = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Start || CouchPause();
            if (State == GameFlowState.Paused && PadNav.Back)
                pauseEdge = true;
            if (CouchPlay.NeedsRejoin && State == GameFlowState.Play)
                TogglePause();
            else if (pauseEdge && !Tag.Ui.Menu.MenuHost.EatPause && (State == GameFlowState.Play || State == GameFlowState.Paused))
                TogglePause();

            if ((State == GameFlowState.Boot || State == GameFlowState.Setup) && !Tag.Ui.Menu.MenuHost.CoversFront)
                PollFront();
            else if (State == GameFlowState.PlayerCount)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    AudioCuePlayer.Ensure()?.UiClick();
                    _bootFocus = 5;
                    State = GameFlowState.Boot;
                    return;
                }
                // Rows are 1..4. Keys used to highlight row 0 while Enter started 2 players.
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _playerCountCursor, 0);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _playerCountCursor, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetFocus(ref _playerCountCursor, 2);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetFocus(ref _playerCountCursor, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) Nudge(ref _playerCountCursor, -1, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) Nudge(ref _playerCountCursor, 1, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    UnityEngine.Input.GetKeyDown(KeyCode.Space))
                {
                    LocalPlayerRoster.SetCount(_playerCountCursor + 1);
                    _modeFromWhoPlays = true;
                    GoToModeSelect();
                }
            }
            else if (State == GameFlowState.ModeSelect)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    AudioCuePlayer.Ensure()?.UiClick();
                    if (_modeFromWhoPlays)
                        State = GameFlowState.PlayerCount;
                    else
                    {
                        _bootFocus = 4;
                        State = GameFlowState.Boot;
                    }
                    return;
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _menuCursor, 0);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _menuCursor, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetFocus(ref _menuCursor, 2);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetFocus(ref _menuCursor, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) Nudge(ref _menuCursor, -1, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) Nudge(ref _menuCursor, 1, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    UnityEngine.Input.GetKeyDown(KeyCode.Space))
                    ConfirmModeAndPlay();
            }
            else if (State == GameFlowState.RoundEnd)
            {
                // TagModeController owns R/Q/Esc while Results (arm + one-shot latch).
                var modes = TagModeController.Instance;
                if (modes != null && modes.Phase == MatchPhase.Results)
                    return;
                // Fallback card when no mode controller is showing results.
                // Same arm as the main card: highlight can move, activate waits.
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) Nudge(ref _looseResultsFocus, -1, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) Nudge(ref _looseResultsFocus, 1, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _looseResultsFocus, 0);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _looseResultsFocus, 1);
                if (Time.unscaledTime < _looseResultsReadyAt) return;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
                {
                    if (_looseResultsFocus == 0) Rematch();
                    else QuitToMenu();
                    return;
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                {
                    Rematch();
                    return;
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
                    QuitToMenu();
            }
            else if (State == GameFlowState.Paused && !Tag.Ui.Menu.MenuHost.CoversPause)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left || PadNav.Up)
                    Nudge(ref _pauseFocus, -1, MenuGraph.PauseRows - 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right || PadNav.Down)
                    Nudge(ref _pauseFocus, 1, MenuGraph.PauseRows - 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _pauseFocus, 0);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _pauseFocus, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetFocus(ref _pauseFocus, 2);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetFocus(ref _pauseFocus, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) SetFocus(ref _pauseFocus, 4);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha6)) SetFocus(ref _pauseFocus, 5);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha7)) SetFocus(ref _pauseFocus, 6);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha8)) SetFocus(ref _pauseFocus, 7);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha9)) SetFocus(ref _pauseFocus, 8);
                if (UnityEngine.Input.GetKeyDown(KeyCode.H))
                {
                    OpenControls();
                    AudioCuePlayer.Ensure()?.UiClick();
                    return;
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
                    ActivatePause();
                else if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) QuitToMenu();
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) AudioMaster.CycleMusic(1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) AudioMaster.CycleMusic(-1);
            }
        }

        void OnGUI()
        {
            MinimapHud.Draw();
            if (SettingsMenuUi.Blocks)
            {
                SettingsMenuUi.Draw();
                return;
            }

            if (_audioOpen)
            {
                DrawAudioSettings();
                return;
            }

            if (_controlsOpen)
            {
                DrawControls();
                return;
            }

            if (_settingsOpen)
            {
                DrawLookSettings();
                return;
            }

            if (Tag.Ui.Menu.MenuHost.CoversFront &&
                (State == GameFlowState.Boot || State == GameFlowState.Setup
                    || State == GameFlowState.PlayerCount || State == GameFlowState.ModeSelect))
                return;
            if (Tag.Ui.Menu.MenuHost.CoversPause && State == GameFlowState.Paused)
                return;
            if (Tag.Ui.Menu.MenuHost.CoversResults && State == GameFlowState.RoundEnd)
                return;

            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            if (State == GameFlowState.Boot || State == GameFlowState.Setup)
                FrontEndView.Draw();
            else if (State == GameFlowState.PlayerCount)
            {
                GUI.Box(new Rect(cx - 180, cy - 140, 360, 280), "Who plays");
                DrawRow(cx, cy - 80, 0, "You + 1 bot");
                DrawRow(cx, cy - 45, 1, "2 humans (bot off)");
                DrawRow(cx, cy - 10, 2, "3 humans (bot off)");
                DrawRow(cx, cy + 25, 3, "4 humans (bot off)");
                GUI.Label(new Rect(cx - 170, cy + 62, 340, 64),
                    "1 is solo versus the bot.\n2-4 is couch and the bot stays off.\n1-4 highlights. Enter / Space next. Esc to Boot.");
            }
            else if (State == GameFlowState.ModeSelect)
            {
                string roster = LocalPlayerRoster.IsCouch
                    ? CouchRoster(LocalPlayerRoster.PlayerCount)
                    : "you + 1 bot";
                GUI.Box(new Rect(cx - 220, cy - 150, 440, 300), "Mode  " + roster);
                DrawMode(cx, cy - 100, 0, "1  Hot Potato  (first to 2 - fuse 45/40/35s)");
                DrawMode(cx, cy - 60, 1, "2  Least It    (120s + next-punch tiebreak)");
                DrawMode(cx, cy - 20, 2, "3  Trail Tag   (ribbons eliminate - last standing)");
                DrawMode(cx, cy + 20, 3, "4  Free play   (punch transfers It - no timer)");
                GUI.Label(new Rect(cx - 180, cy + 70, 360, 48),
                    "1-4 highlights. Enter / Space plays.\nEsc steps back. Up / Down stops at the ends.");
            }
            else if (State == GameFlowState.Paused)
            {
                GUI.Box(new Rect(cx - 170, cy - 204, 340, 500), "Paused");
                string rejoin = CouchPlay.RejoinPrompt;
                if (rejoin.Length > 0)
                    GUI.Label(new Rect(cx - 100, cy - 200, 220, 22), rejoin);
                if (FocusButton(new Rect(cx - 100, cy - 176, 200, 26), 0, ref _pauseFocus, "Resume")) TogglePause();
                if (FocusButton(new Rect(cx - 100, cy - 146, 200, 26), 1, ref _pauseFocus, "Controls"))
                    OpenControls();
                if (FocusButton(new Rect(cx - 100, cy - 116, 200, 26), 2, ref _pauseFocus, "Look sensitivity"))
                    OpenLook();
                if (FocusButton(new Rect(cx - 100, cy - 86, 200, 26), 3, ref _pauseFocus, "Audio"))
                    OpenAudio();
                if (FocusButton(new Rect(cx - 100, cy - 56, 200, 26), 4, ref _pauseFocus, "Quit to title"))
                    QuitToMenu();
                if (FocusButton(new Rect(cx - 100, cy - 26, 200, 26), 5, ref _pauseFocus, "Settings"))
                    SettingsMenuUi.Open(SettingsMenuUi.Panel.Settings);
                if (FocusButton(new Rect(cx - 100, cy + 4, 200, 26), 6, ref _pauseFocus, "Rebind"))
                    SettingsMenuUi.Open(SettingsMenuUi.Panel.Rebind);
                if (FocusButton(new Rect(cx - 100, cy + 34, 200, 26), 7, ref _pauseFocus, "Arena"))
                    SettingsMenuUi.Open(SettingsMenuUi.Panel.Arena);
                if (FocusButton(new Rect(cx - 100, cy + 64, 200, 26), 8, ref _pauseFocus, "How to play"))
                    SettingsMenuUi.Open(SettingsMenuUi.Panel.HowTo);
                GUI.Label(new Rect(cx - 160, cy + 100, 320, 80),
                    "Left / Right or stick    1-9 picks    Enter / South\nEsc or East resume    Start pauses    Q title\nComma mute    M minimap    N music    Up / Down bed");
            }
            else if (State == GameFlowState.RoundEnd)
            {
                // TagModeController draws the center card when a round is running.
                if (TagModeController.Instance == null)
                {
                    GUI.Box(new Rect(cx - 220, cy - 70, 440, 140), "Round over");
                    string arm = _looseResultsFocus == 0 ? "> Rematch" : "Rematch";
                    string menu = _looseResultsFocus == 1 ? "> Menu" : "Menu";
                    GUI.Label(new Rect(cx - 200, cy - 36, 400, 70),
                        LooseResultsLine(arm, menu));
                }
            }
        }

        void PollControlsKeys()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
            {
                _controlsOpen = false;
                AudioCuePlayer.Ensure()?.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up) Nudge(ref _controlsFocus, -1, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down) Nudge(ref _controlsFocus, 1, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _controlsFocus, 0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _controlsFocus, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetFocus(ref _controlsFocus, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) StepControls(-1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) StepControls(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
            {
                if (_controlsFocus >= 2)
                {
                    _controlsOpen = false;
                    AudioCuePlayer.Ensure()?.UiClick();
                }
                else StepControls(1);
            }
        }

        void StepControls(int dir)
        {
            if (_controlsFocus == 0) ControlBinds.CycleDash(dir);
            else if (_controlsFocus == 1) ControlBinds.CyclePunch(dir);
        }

        void PollLookKeys()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
            {
                _settingsOpen = false;
                AudioCuePlayer.Ensure()?.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up) Nudge(ref _lookFocus, -1, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down) Nudge(ref _lookFocus, 1, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _lookFocus, 0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _lookFocus, 1);
            if ((UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) && _lookFocus == 0)
                LookSensitivity.Cycle(-1);
            if ((UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) && _lookFocus == 0)
                LookSensitivity.Cycle(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
            {
                if (_lookFocus >= 1)
                {
                    _settingsOpen = false;
                    AudioCuePlayer.Ensure()?.UiClick();
                }
                else LookSensitivity.Cycle(1);
            }
        }

        void PollAudioKeys()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
            {
                _audioOpen = false;
                AudioCuePlayer.Ensure()?.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up) Nudge(ref _audioFocus, -1, 4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down) Nudge(ref _audioFocus, 1, 4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _audioFocus, 0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _audioFocus, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetFocus(ref _audioFocus, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetFocus(ref _audioFocus, 3);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) SetFocus(ref _audioFocus, 4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) StepAudio(-1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) StepAudio(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
            {
                if (_audioFocus >= 4)
                {
                    _audioOpen = false;
                    AudioCuePlayer.Ensure()?.UiClick();
                }
                else StepAudio(1);
            }
        }

        void StepAudio(int dir)
        {
            switch (_audioFocus)
            {
                case 0: AudioMaster.CycleVolume(dir); break;
                case 1: AudioMaster.CycleMusic(dir); break;
                case 2: AudioMaster.ToggleMute(); break;
                case 3: AudioMaster.ToggleMusicMute(); break;
            }
        }

        void DrawControls()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 240, cy - 210, 480, 430), "Controls");
            GUI.Label(new Rect(cx - 220, cy - 180, 440, 200), ControlBinds.Help);
            SubRow(cx, cy + 28, 0, ref _controlsFocus, "Dash   " + ControlBinds.DashName);
            if (SideStep(cx, cy + 60))
            {
                _controlsFocus = 0;
                ControlBinds.CycleDash(SideDir());
            }
            SubRow(cx, cy + 96, 1, ref _controlsFocus, "Punch  " + ControlBinds.PunchName);
            if (SideStep(cx, cy + 128))
            {
                _controlsFocus = 1;
                ControlBinds.CyclePunch(SideDir());
            }
            if (SubRow(cx, cy + 164, 2, ref _controlsFocus, "Back"))
            {
                _controlsOpen = false;
                AudioCuePlayer.Ensure()?.UiClick();
            }
            GUI.Label(new Rect(cx - 220, cy + 198, 440, 36),
                "Up / Down picks. Left / Right steps it. 1-3 highlight.\nEnter uses the row. Esc back. E punches. Alt dashes.");
        }

        void DrawLookSettings()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 200, cy - 110, 400, 230), "Look sensitivity");
            SubRow(cx, cy - 70, 0, ref _lookFocus, LookSensitivity.Label);
            if (SideStep(cx, cy - 36))
            {
                _lookFocus = 0;
                LookSensitivity.Cycle(SideDir());
            }
            if (SubRow(cx, cy + 8, 1, ref _lookFocus, "Back"))
            {
                _settingsOpen = false;
                AudioCuePlayer.Ensure()?.UiClick();
            }
            GUI.Label(new Rect(cx - 180, cy + 44, 360, 48),
                "Up / Down picks. Left / Right steps look.\n1-2 highlight. Enter uses the row. Esc back.");
        }

        void DrawAudioSettings()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 210, cy - 170, 420, 360), "Audio");
            SubRow(cx, cy - 130, 0, ref _audioFocus, "SFX   " + AudioMaster.Label);
            if (SideStep(cx, cy - 96))
            {
                _audioFocus = 0;
                AudioMaster.CycleVolume(SideDir());
            }
            SubRow(cx, cy - 60, 1, ref _audioFocus, "Music   " + AudioMaster.MusicLabel);
            if (SideStep(cx, cy - 26))
            {
                _audioFocus = 1;
                AudioMaster.CycleMusic(SideDir());
            }
            string muteLabel = AudioMaster.Muted ? "Unmute (Comma)" : "Mute (Comma)";
            if (SubRow(cx, cy + 12, 2, ref _audioFocus, muteLabel))
                AudioMaster.ToggleMute();
            string musicLabel = AudioMaster.MusicMuted ? "Music on (N)" : "Music off (N)";
            if (SubRow(cx, cy + 46, 3, ref _audioFocus, musicLabel))
                AudioMaster.ToggleMusicMute();
            if (SubRow(cx, cy + 80, 4, ref _audioFocus, "Back"))
            {
                _audioOpen = false;
                AudioCuePlayer.Ensure()?.UiClick();
            }
            GUI.Label(new Rect(cx - 190, cy + 116, 380, 48),
                "Up / Down picks. Left / Right steps the row.\n1-5 highlight. Comma mute. N music. Enter uses it. Esc back.");
        }

        static int _sideDir;

        static bool SideStep(float cx, float y)
        {
            _sideDir = 0;
            if (MenuClick.Button(new Rect(cx - 150, y, 80, 26), "<")) { _sideDir = -1; return true; }
            if (MenuClick.Button(new Rect(cx + 70, y, 80, 26), ">")) { _sideDir = 1; return true; }
            return false;
        }

        static int SideDir() => _sideDir == 0 ? 1 : _sideDir;

        static bool SubRow(float cx, float y, int index, ref int cursor, string label)
        {
            var r = new Rect(cx - 170, y, 340, 28);
            bool sel = cursor == index;
            if (sel) GUI.Box(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), "");
            if (!MenuClick.Button(r, (sel ? "> " : "  ") + label)) return false;
            cursor = index;
            return true;
        }

        static void Nudge(ref int cursor, int dir, int maxInclusive)
        {
            int next = Mathf.Clamp(cursor + dir, 0, maxInclusive);
            if (next == cursor) return;
            cursor = next;
            AudioCuePlayer.Ensure()?.UiClick();
        }

        static void SetFocus(ref int cursor, int index)
        {
            if (cursor == index) return;
            cursor = index;
            AudioCuePlayer.Ensure()?.UiClick();
        }

        void PollFront()
        {
            if ((FrontSession.Screen == FrontScreen.Settings || FrontSession.Screen == FrontScreen.HowTo)
                && !SettingsMenuUi.Blocks)
                FrontSession.CloseOverlay();
            if (FrontSession.Screen == FrontScreen.Settings || FrontSession.Screen == FrontScreen.HowTo)
                return;

            CouchDevices.Poll();

            bool up = UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up;
            bool down = UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down;
            bool left = UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left;
            bool right = UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right;
            bool confirm = UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)
                || UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm;
            bool back = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back;
            if (up) FrontSession.Nudge(-1);
            if (down) FrontSession.Nudge(1);
            if (left) FrontSession.Step(-1);
            if (right) FrontSession.Step(1);

            int act = FrontSession.ActNone;
            if (CouchDevices.EatBack) back = false;
            if (CouchDevices.EatConfirm) confirm = false;
            if (back) act = FrontSession.Back();
            else if (confirm) act = FrontSession.Confirm();
            else
            {
                int rows = FrontSession.RowCount;
                for (int i = 0; i < rows && i < 9; i++)
                {
                    if (UnityEngine.Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                        FrontSession.Highlight(i);
                }
            }
            if (act != FrontSession.ActNone)
                ApplyFront(act);
            PlayFrontSound();
            if (FrontSession.ConsumeDirty())
                SettingsRuntime.Save();
        }

        void ApplyFront(int act)
        {
            if (act == FrontSession.ActSetup)
                State = GameFlowState.Setup;
            else if (act == FrontSession.ActSettings)
                SettingsMenuUi.Open(SettingsMenuUi.Panel.Settings);
            else if (act == FrontSession.ActHowTo)
                SettingsMenuUi.Open(SettingsMenuUi.Panel.HowTo);
            else if (act == FrontSession.ActJoin)
                State = GameFlowState.Setup;
            else if (act == FrontSession.ActStart)
                StartFromSetup();
            else if (act == FrontSession.ActQuit)
                Application.Quit();
            else if (act == FrontSession.ActTitle)
            {
                State = GameFlowState.Boot;
                PracticeArena.Restore();
                HoldMenuClock();
            }
            else if (act == FrontSession.ActRematch)
                Rematch();
        }

        void StartFromSetup()
        {
            if (GameSettings.Current != null)
                GameSettings.Current.Clamp();
            if (PracticeSession.Active)
                SyncSelectedMode(TagModeId.FreePlay);
            SettingsRuntime.Save();
            State = GameFlowState.Play;
            Time.timeScale = 1f;
            GoToPlay();
        }

        void PlayFrontSound()
        {
            int sound = FrontSession.ConsumeSound();
            if (sound == FrontSession.SoundMove) TagSfx.UiMove();
            else if (sound == FrontSession.SoundConfirm) TagSfx.UiConfirm();
            else if (sound == FrontSession.SoundBack) TagSfx.UiBack();
        }

        void ActivateBoot()
        {
            switch (_bootFocus)
            {
                case 1: OpenControls(); break;
                case 2: OpenLook(); break;
                case 3: OpenAudio(); break;
                case 4:
                    _modeFromWhoPlays = false;
                    GoToModeSelect();
                    break;
                case 5: GoToPlayerCount(); break;
                default: PlayLeastItSlice(); break;
            }
        }

        void ActivatePause()
        {
            switch (_pauseFocus)
            {
                case 1: OpenControls(); break;
                case 2: OpenLook(); break;
                case 3: OpenAudio(); break;
                case 4: QuitToMenu(); break;
                case 5: SettingsMenuUi.Open(SettingsMenuUi.Panel.Settings); break;
                case 6: SettingsMenuUi.Open(SettingsMenuUi.Panel.Rebind); break;
                case 7: SettingsMenuUi.Open(SettingsMenuUi.Panel.Arena); break;
                case 8: SettingsMenuUi.Open(SettingsMenuUi.Panel.HowTo); break;
                default: TogglePause(); break;
            }
        }

        void OpenControls()
        {
            _settingsOpen = false;
            _audioOpen = false;
            _controlsOpen = true;
            _controlsFocus = 0;
        }

        void OpenLook()
        {
            _controlsOpen = false;
            _audioOpen = false;
            _settingsOpen = true;
            _lookFocus = 0;
        }

        void OpenAudio()
        {
            _controlsOpen = false;
            _settingsOpen = false;
            _audioOpen = true;
            _audioFocus = 0;
        }

        static bool FocusButton(Rect r, int index, ref int cursor, string label)
        {
            bool sel = cursor == index;
            if (sel) GUI.Box(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), "");
            // Mouse only. Enter/Space is handled in Update from the highlight, so a
            // different IMGUI focus cannot fire a second row on the same key.
            if (!MenuClick.Button(r, (sel ? "> " : "  ") + label)) return false;
            cursor = index;
            return true;
        }

        void DrawRow(float cx, float y, int index, string label)
        {
            bool sel = _playerCountCursor == index;
            var r = new Rect(cx - 120, y, 240, 28);
            if (sel) GUI.Box(r, "");
            if (MenuClick.Button(r, (sel ? "> " : "  ") + label))
            {
                _playerCountCursor = index;
                LocalPlayerRoster.SetCount(index + 1);
                _modeFromWhoPlays = true;
                GoToModeSelect();
            }
        }

        void DrawMode(float cx, float y, int index, string label)
        {
            bool sel = _menuCursor == index;
            var r = new Rect(cx - 200, y, 400, 28);
            if (sel) GUI.Box(r, "");
            if (MenuClick.Button(r, (sel ? "> " : "  ") + label))
            {
                _menuCursor = index;
                ConfirmModeAndPlay();
            }
        }
    }

    /// <summary>
    /// IMGUI's GUI.Button activates on Enter/Space when a control has keyboard focus,
    /// and that Use() eats the key before jump can see it. Menu rows are mouse-only.
    /// Enter/Space still follows the highlight from Update.
    /// </summary>
    public static class MenuClick
    {
        static readonly int ButtonHint = "Tag.MenuClick".GetHashCode();

        public static bool Button(Rect r, string label)
        {
            int id = GUIUtility.GetControlID(ButtonHint, FocusType.Passive, r);
            var e = Event.current;
            if (e == null)
                return false;

            switch (e.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    GUI.skin.button.Draw(
                        r,
                        new GUIContent(label),
                        r.Contains(e.mousePosition),
                        GUIUtility.hotControl == id,
                        false,
                        false);
                    break;
                case EventType.MouseDown:
                    if (e.button == 0 && r.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                        return e.button == 0 && r.Contains(e.mousePosition);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                        e.Use();
                    break;
            }

            return false;
        }
    }
}

