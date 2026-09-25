using UnityEngine;
using UnityEngine.SceneManagement;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Local;
using Tag.Audio;
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
        Rematch
    }

    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] string bootSceneName = "Boot";
        [SerializeField] string playSceneName = "Play";

        public TagModeController modeController;
        public TagRoundController round;

        public GameFlowState State { get; private set; } = GameFlowState.Boot;
        public TagModeId SelectedMode { get; private set; } = TagModeId.LeastIt;
        public string LastResultMessage { get; private set; } = "";

        int _menuCursor = 1;
        int _playerCountCursor;
        int _bootFocus;
        int _pauseFocus;
        int _looseResultsFocus;
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
            AudioCuePlayer.Ensure();
            if (PlayerPrefs.HasKey(TagModeController.PrefsModeKey))
            {
                SelectedMode = (TagModeId)PlayerPrefs.GetInt(TagModeController.PrefsModeKey, (int)TagModeId.LeastIt);
                _menuCursor = (int)SelectedMode;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == bootSceneName || scene == "Boot")
                State = GameFlowState.Boot;
            else
            {
                State = GameFlowState.Play;
                EnsurePlayHelpers();
                LookSensitivity.Load();
                LookSensitivity.Apply();
                EnsureRoundStarted();
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == playSceneName || scene.name == "Play")
            {
                State = GameFlowState.Play;
                EnsurePlayHelpers();
                LookSensitivity.Load();
                LookSensitivity.Apply();
                EnsureRoundStarted();
            }
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
            LocalPlayerRoster.SetCount(1);
            SelectedMode = TagModeId.LeastIt;
            _menuCursor = (int)TagModeId.LeastIt;
            PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)TagModeId.LeastIt);
            PlayerPrefs.Save();
            AudioCuePlayer.Ensure()?.UiConfirm();
            GoToPlay();
        }

        public void GoToPlayerCount()
        {
            MarkBootSeen();
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
            MarkBootSeen();
            State = GameFlowState.ModeSelect;
            AudioCuePlayer.Ensure()?.UiClick();
        }

        public void ConfirmModeAndPlay()
        {
            SelectedMode = (TagModeId)_menuCursor;
            PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)SelectedMode);
            PlayerPrefs.Save();
            AudioCuePlayer.Ensure()?.UiConfirm();
            GoToPlay();
        }

        public void GoToPlay()
        {
            State = GameFlowState.Play;
            LookSensitivity.Load();
            LookSensitivity.Apply();
            Time.timeScale = 1f;
            if (SceneManager.GetActiveScene().name != playSceneName)
                SceneManager.LoadScene(playSceneName);
            else
            {
                EnsurePlayHelpers();
                EnsureRoundStarted();
            }
        }

        public void OnRoundEnded(string result = "")
        {
            LastResultMessage = result ?? "";
            State = GameFlowState.RoundEnd;
            _looseResultsFocus = 0;
            Time.timeScale = 1f;
            // Unlock so Rematch/Menu clicks on the results card work (pause already unlocks).
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var msg = LastResultMessage.ToLowerInvariant();
            // "No winners" contains "win"; check that before the win sting.
            if (msg.Contains("no winner"))
                AudioCuePlayer.Ensure()?.RoundEnd();
            else if (msg.Contains("winner") || msg.Contains(" win"))
                AudioCuePlayer.Ensure()?.RoundWin();
            else if (msg.Contains("lose") || msg.Contains("loss"))
                AudioCuePlayer.Ensure()?.RoundLose();
            else
                AudioCuePlayer.Ensure()?.RoundEnd();
        }

        // Compat for older callers
        public void OnRoundEnded() => OnRoundEnded(LastResultMessage);

        public void Rematch()
        {
            AudioCuePlayer.Ensure()?.UiConfirm();
            ClearPauseEdges();
            ReturnToPlay();
            if (modeController == null) modeController = FindFirstObjectByType<TagModeController>();
            if (modeController != null) modeController.Rematch();
            else SceneManager.LoadScene(playSceneName);
        }

        /// <summary>
        /// F1-F4 and rematch leave RoundEnd / Pause. Otherwise R still rematches
        /// the new round, and a pause leaves timeScale at 0 so the countdown never finishes.
        /// </summary>
        public void ReturnToPlay()
        {
            if (State != GameFlowState.Paused && State != GameFlowState.RoundEnd)
                return;
            State = GameFlowState.Play;
            Time.timeScale = 1f;
            ResumeInputGate.LockPlayCursor();
            ArmLocalLookPunchGate();
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            AudioCuePlayer.Ensure()?.UiClick();
            AudioCuePlayer.Ensure()?.StopMusic();
            SceneManager.LoadScene(bootSceneName);
            State = GameFlowState.Boot;
            _bootFocus = 0;
        }


        static void ArmLocalLookPunchGate()
        {
            foreach (var reader in Object.FindObjectsByType<TagArena.Movement.PlayerInputReader>(FindObjectsSortMode.None))
                reader?.ArmLookPunchGate(2);
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
                ClearPauseEdges();
                AudioCuePlayer.Ensure()?.UiClick();
            }
            else if (State == GameFlowState.Paused)
            {
                State = GameFlowState.Play;
                Time.timeScale = 1f;
                ResumeInputGate.LockPlayCursor();
                Cursor.visible = false;
                _controlsOpen = false;
                _settingsOpen = false;
                _audioOpen = false;
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
            modeController.SelectedMode = SelectedMode;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
                modeController.RegisterPlayer(p);
            if (!modeController.RoundActive)
                modeController.StartRound();
            AudioCuePlayer.Ensure()?.PlaygroundMusic();
        }

        void Update()
        {
            if (_audioOpen)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    _audioOpen = false;
                    AudioCuePlayer.Ensure()?.UiClick();
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                    AudioMaster.CycleVolume(-1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                    AudioMaster.CycleVolume(1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.M))
                    AudioMaster.ToggleMute();
                if (UnityEngine.Input.GetKeyDown(KeyCode.N))
                    AudioMaster.ToggleMusicMute();
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                    AudioMaster.CycleMusic(1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                    AudioMaster.CycleMusic(-1);
                return;
            }

            if (_controlsOpen)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    _controlsOpen = false;
                    AudioCuePlayer.Ensure()?.UiClick();
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                    ControlBinds.CycleDash(-1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                    ControlBinds.CycleDash(1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                    ControlBinds.CyclePunch(-1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                    ControlBinds.CyclePunch(1);
                return;
            }

            if (_settingsOpen)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    _settingsOpen = false;
                    AudioCuePlayer.Ensure()?.UiClick();
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                    LookSensitivity.Cycle(-1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                    LookSensitivity.Cycle(1);
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) &&
                (State == GameFlowState.Play || State == GameFlowState.Paused))
                TogglePause();

            if (State == GameFlowState.Boot)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) Nudge(ref _bootFocus, -1, 5);
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) Nudge(ref _bootFocus, 1, 5);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _bootFocus, 0);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _bootFocus, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetFocus(ref _bootFocus, 2);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetFocus(ref _bootFocus, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) SetFocus(ref _bootFocus, 4);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha6)) SetFocus(ref _bootFocus, 5);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    UnityEngine.Input.GetKeyDown(KeyCode.Space))
                    ActivateBoot();
            }
            else if (State == GameFlowState.PlayerCount)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    AudioCuePlayer.Ensure()?.UiClick();
                    State = GameFlowState.Boot;
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
                    GoToModeSelect();
                }
            }
            else if (State == GameFlowState.ModeSelect)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    AudioCuePlayer.Ensure()?.UiClick();
                    State = GameFlowState.Boot;
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
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) Nudge(ref _looseResultsFocus, -1, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) Nudge(ref _looseResultsFocus, 1, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (_looseResultsFocus == 0) Rematch();
                    else QuitToMenu();
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.R)) Rematch();
                else if (UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                    QuitToMenu();
            }
            else if (State == GameFlowState.Paused)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) Nudge(ref _pauseFocus, -1, 4);
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) Nudge(ref _pauseFocus, 1, 4);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetFocus(ref _pauseFocus, 0);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetFocus(ref _pauseFocus, 1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetFocus(ref _pauseFocus, 2);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetFocus(ref _pauseFocus, 3);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) SetFocus(ref _pauseFocus, 4);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    UnityEngine.Input.GetKeyDown(KeyCode.Space))
                    ActivatePause();
                else if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) QuitToMenu();
                if (UnityEngine.Input.GetKeyDown(KeyCode.M)) AudioMaster.ToggleMute();
                if (UnityEngine.Input.GetKeyDown(KeyCode.N)) AudioMaster.ToggleMusicMute();
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) AudioMaster.CycleMusic(1);
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) AudioMaster.CycleMusic(-1);
            }
        }

        void OnGUI()
        {
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

            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            if (State == GameFlowState.Boot)
            {
                GUI.Box(new Rect(cx - 210, cy - 170, 420, 360), "TAG - party slice");
                string hello = _firstBoot
                    ? "First run: you + 1 bot, Least It. LMB/F punch passes It.\nEsc pauses. Audio / M mute. R rematch after a round."
                    : "Play is you and one bot. Couch is local humans.";
                GUI.Label(new Rect(cx - 190, cy - 128, 380, 44), hello);
                if (FocusButton(new Rect(cx - 90, cy - 76, 180, 32), 0, ref _bootFocus, "Play Tag (Least It)"))
                    PlayLeastItSlice();
                if (FocusButton(new Rect(cx - 90, cy - 38, 180, 28), 1, ref _bootFocus, "Controls"))
                    OpenControls();
                if (FocusButton(new Rect(cx - 90, cy - 4, 180, 28), 2, ref _bootFocus, "Look sensitivity"))
                    OpenLook();
                if (FocusButton(new Rect(cx - 90, cy + 30, 180, 28), 3, ref _bootFocus, "Audio"))
                    OpenAudio();
                if (FocusButton(new Rect(cx - 90, cy + 64, 180, 28), 4, ref _bootFocus, "Mode select..."))
                {
                    LocalPlayerRoster.SetCount(1);
                    GoToModeSelect();
                }
                if (FocusButton(new Rect(cx - 90, cy + 98, 180, 28), 5, ref _bootFocus, "Couch..."))
                    GoToPlayerCount();
                GUI.Label(new Rect(cx - 190, cy + 132, 380, 36),
                    "Up / Down picks. Enter uses it. Stops at the ends.");
            }
            else if (State == GameFlowState.PlayerCount)
            {
                GUI.Box(new Rect(cx - 180, cy - 140, 360, 280), "Who plays");
                DrawRow(cx, cy - 80, 0, "You + 1 bot");
                DrawRow(cx, cy - 45, 1, "2 humans (bot off)");
                DrawRow(cx, cy - 10, 2, "3 humans (bot off)");
                DrawRow(cx, cy + 25, 3, "4 humans (bot off)");
                GUI.Label(new Rect(cx - 170, cy + 62, 340, 64),
                    "1 is solo versus the bot.\n2-4 is couch and the bot stays off.\n1-4  Enter    Esc back    Up/Down stop");
            }
            else if (State == GameFlowState.ModeSelect)
            {
                string roster = LocalPlayerRoster.IsCouch
                    ? $"{LocalPlayerRoster.PlayerCount} humans, bot off"
                    : "you + 1 bot";
                GUI.Box(new Rect(cx - 220, cy - 150, 440, 300), "Mode  " + roster);
                DrawMode(cx, cy - 100, 0, "1  Hot Potato  (first to 2 - fuse 45/40/35s)");
                DrawMode(cx, cy - 60, 1, "2  Least It    (120s + next-punch tiebreak)");
                DrawMode(cx, cy - 20, 2, "3  Trail Tag   (ribbons eliminate - last standing)");
                DrawMode(cx, cy + 20, 3, "4  Free play   (punch transfers It - no timer)");
                GUI.Label(new Rect(cx - 180, cy + 70, 360, 48),
                    "1/2/3/4  Enter to play    Esc back\nUp / Down stops at the ends.");
            }
            else if (State == GameFlowState.Paused)
            {
                GUI.Box(new Rect(cx - 150, cy - 130, 300, 280), "Paused");
                if (FocusButton(new Rect(cx - 70, cy - 90, 140, 28), 0, ref _pauseFocus, "Resume")) TogglePause();
                if (FocusButton(new Rect(cx - 70, cy - 56, 140, 28), 1, ref _pauseFocus, "Controls"))
                    OpenControls();
                if (FocusButton(new Rect(cx - 70, cy - 22, 140, 28), 2, ref _pauseFocus, "Look sensitivity"))
                    OpenLook();
                if (FocusButton(new Rect(cx - 70, cy + 12, 140, 28), 3, ref _pauseFocus, "Audio"))
                    OpenAudio();
                if (FocusButton(new Rect(cx - 70, cy + 46, 140, 28), 4, ref _pauseFocus, "Quit to Menu"))
                    QuitToMenu();
                GUI.Label(new Rect(cx - 140, cy + 78, 280, 64),
                    "Left / Right or 1-5 picks    Enter / Space\nEsc resume    Q menu\nM mute    N music    Up / Down bed");
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
                        $"{LastResultMessage}\n\n{arm}    {menu}\nLeft / Right    Enter    R    Q");
                }
            }
        }

        void DrawControls()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 240, cy - 200, 480, 390), "Controls");
            GUI.Label(new Rect(cx - 220, cy - 170, 440, 250), ControlBinds.Help);
            if (GUI.Button(new Rect(cx - 220, cy + 88, 100, 26), "Dash <"))
                ControlBinds.CycleDash(-1);
            if (GUI.Button(new Rect(cx - 112, cy + 88, 100, 26), "Dash >"))
                ControlBinds.CycleDash(1);
            if (GUI.Button(new Rect(cx + 4, cy + 88, 100, 26), "Punch <"))
                ControlBinds.CyclePunch(-1);
            if (GUI.Button(new Rect(cx + 112, cy + 88, 100, 26), "Punch >"))
                ControlBinds.CyclePunch(1);
            GUI.Label(new Rect(cx - 220, cy + 122, 440, 48),
                "Left / Right dash. Up / Down punch. E still punches.\nAlt still dashes. Volume is the Audio card. Esc back.");
        }

        void DrawLookSettings()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 200, cy - 90, 400, 180), "Look sensitivity");
            GUI.Label(new Rect(cx - 180, cy - 48, 360, 28), LookSensitivity.Label);
            if (GUI.Button(new Rect(cx - 150, cy - 10, 80, 28), "<"))
                LookSensitivity.Cycle(-1);
            if (GUI.Button(new Rect(cx + 70, cy - 10, 80, 28), ">"))
                LookSensitivity.Cycle(1);
            GUI.Label(new Rect(cx - 180, cy + 28, 360, 48),
                "Left / Right    Esc back\nDefault is the current camera feel");
        }

        void DrawAudioSettings()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 200, cy - 140, 400, 280), "Audio");
            GUI.Label(new Rect(cx - 180, cy - 108, 360, 28), "SFX  " + AudioMaster.Label);
            if (GUI.Button(new Rect(cx - 150, cy - 74, 80, 28), "<"))
                AudioMaster.CycleVolume(-1);
            if (GUI.Button(new Rect(cx + 70, cy - 74, 80, 28), ">"))
                AudioMaster.CycleVolume(1);
            GUI.Label(new Rect(cx - 180, cy - 36, 360, 28), "Music  " + AudioMaster.MusicLabel);
            if (GUI.Button(new Rect(cx - 150, cy - 4, 80, 28), "<"))
                AudioMaster.CycleMusic(-1);
            if (GUI.Button(new Rect(cx + 70, cy - 4, 80, 28), ">"))
                AudioMaster.CycleMusic(1);
            string muteLabel = AudioMaster.Muted ? "Unmute (M)" : "Mute (M)";
            if (GUI.Button(new Rect(cx - 150, cy + 36, 140, 28), muteLabel))
                AudioMaster.ToggleMute();
            string musicLabel = AudioMaster.MusicMuted ? "Music on (N)" : "Music off (N)";
            if (GUI.Button(new Rect(cx + 10, cy + 36, 140, 28), musicLabel))
                AudioMaster.ToggleMusicMute();
            GUI.Label(new Rect(cx - 180, cy + 74, 360, 48),
                "Left / Right SFX    Up / Down music\nM mute all    N music    Esc back");
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

        void ActivateBoot()
        {
            switch (_bootFocus)
            {
                case 1: OpenControls(); break;
                case 2: OpenLook(); break;
                case 3: OpenAudio(); break;
                case 4:
                    LocalPlayerRoster.SetCount(1);
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
                default: TogglePause(); break;
            }
        }

        void OpenControls()
        {
            _settingsOpen = false;
            _audioOpen = false;
            _controlsOpen = true;
        }

        void OpenLook()
        {
            _controlsOpen = false;
            _audioOpen = false;
            _settingsOpen = true;
        }

        void OpenAudio()
        {
            _controlsOpen = false;
            _settingsOpen = false;
            _audioOpen = true;
        }

        static bool FocusButton(Rect r, int index, ref int cursor, string label)
        {
            bool sel = cursor == index;
            if (sel) GUI.Box(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), "");
            if (!GUI.Button(r, (sel ? "> " : "  ") + label)) return false;
            cursor = index;
            return true;
        }

        void DrawRow(float cx, float y, int index, string label)
        {
            bool sel = _playerCountCursor == index;
            var r = new Rect(cx - 120, y, 240, 28);
            if (sel) GUI.Box(r, "");
            if (GUI.Button(r, (sel ? "> " : "  ") + label))
            {
                _playerCountCursor = index;
                LocalPlayerRoster.SetCount(index + 1);
                GoToModeSelect();
            }
        }

        void DrawMode(float cx, float y, int index, string label)
        {
            bool sel = _menuCursor == index;
            var r = new Rect(cx - 200, y, 400, 28);
            if (sel) GUI.Box(r, "");
            if (GUI.Button(r, (sel ? "> " : "  ") + label))
            {
                _menuCursor = index;
                ConfirmModeAndPlay();
            }
        }
    }
}

