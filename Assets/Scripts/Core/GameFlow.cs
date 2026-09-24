using UnityEngine;
using UnityEngine.SceneManagement;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Local;
using Tag.Audio;

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

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            LocalPlayerRoster.Load();
            _playerCountCursor = Mathf.Clamp(LocalPlayerRoster.PlayerCount - 1, 0, 3);
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
                EnsureRoundStarted();
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == playSceneName || scene.name == "Play")
            {
                State = GameFlowState.Play;
                EnsurePlayHelpers();
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
            LocalPlayerRoster.SetCount(1);
            SelectedMode = TagModeId.LeastIt;
            _menuCursor = (int)TagModeId.LeastIt;
            PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)TagModeId.LeastIt);
            PlayerPrefs.Save();
            AudioCuePlayer.Ensure()?.UiConfirm();
            GoToPlay();
        }

        public void GoToPlayerCount() { State = GameFlowState.PlayerCount; AudioCuePlayer.Ensure()?.UiClick(); }
        public void SyncSelectedMode(TagModeId id)
        {
            SelectedMode = id;
            _menuCursor = (int)id;
        }

        public void GoToModeSelect() { State = GameFlowState.ModeSelect; AudioCuePlayer.Ensure()?.UiClick(); }

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
            ReturnToPlay();
            if (modeController == null) modeController = FindFirstObjectByType<TagModeController>();
            if (modeController != null) modeController.Rematch();
            else SceneManager.LoadScene(playSceneName);
        }

        /// <summary>
        /// F1–F4 and rematch leave RoundEnd / Pause. Otherwise R still rematches
        /// the new round, and a pause leaves timeScale at 0 so the countdown never finishes.
        /// </summary>
        public void ReturnToPlay()
        {
            if (State != GameFlowState.Paused && State != GameFlowState.RoundEnd)
                return;
            State = GameFlowState.Play;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            AudioCuePlayer.Ensure()?.UiClick();
            AudioCuePlayer.Ensure()?.StopMusic();
            SceneManager.LoadScene(bootSceneName);
            State = GameFlowState.Boot;
        }

        void TogglePause()
        {
            if (State == GameFlowState.Play)
            {
                State = GameFlowState.Paused;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                AudioCuePlayer.Ensure()?.UiClick();
            }
            else if (State == GameFlowState.Paused)
            {
                State = GameFlowState.Play;
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                AudioCuePlayer.Ensure()?.UiClick();
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
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) &&
                (State == GameFlowState.Play || State == GameFlowState.Paused))
                TogglePause();

            if (State == GameFlowState.Boot)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
                    PlayLeastItSlice();
            }
            else if (State == GameFlowState.PlayerCount)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) _playerCountCursor = 0;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) _playerCountCursor = 1;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) _playerCountCursor = 2;
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) _playerCountCursor = (_playerCountCursor + 2) % 3;
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) _playerCountCursor = (_playerCountCursor + 1) % 3;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
                {
                    LocalPlayerRoster.SetCount(_playerCountCursor + 2);
                    GoToModeSelect();
                }
            }
            else if (State == GameFlowState.ModeSelect)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) _menuCursor = 0;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) _menuCursor = 1;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) _menuCursor = 2;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) _menuCursor = 3;
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) _menuCursor = (_menuCursor + 3) % 4;
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) _menuCursor = (_menuCursor + 1) % 4;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
                    ConfirmModeAndPlay();
            }
            else if (State == GameFlowState.RoundEnd)
            {
                // TagModeController owns R/Q while it is showing results, so one press
                // cannot start the round twice.
                var modes = TagModeController.Instance;
                if (modes != null && modes.Phase == MatchPhase.Results)
                {
                    // Esc mirrors Q when the results card owns the match keys.
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) QuitToMenu();
                    return;
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.R)) Rematch();
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                    QuitToMenu();
            }
            else if (State == GameFlowState.Paused)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) QuitToMenu();
            }
        }

        void OnGUI()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            if (State == GameFlowState.Boot)
            {
                GUI.Box(new Rect(cx - 180, cy - 80, 360, 170), "TAG — party slice");
                GUI.Label(new Rect(cx - 170, cy - 52, 340, 36), "Crash-test dummies · playground · punch-tag");
                if (GUI.Button(new Rect(cx - 90, cy - 10, 180, 32), "Play Tag (Least It)"))
                    PlayLeastItSlice();
                if (GUI.Button(new Rect(cx - 90, cy + 28, 180, 28), "Mode select…"))
                {
                    LocalPlayerRoster.SetCount(1);
                    GoToModeSelect();
                }
                if (GUI.Button(new Rect(cx - 90, cy + 62, 180, 28), "Couch…"))
                    GoToPlayerCount();
            }
            else if (State == GameFlowState.PlayerCount)
            {
                GUI.Box(new Rect(cx - 160, cy - 120, 320, 240), "Players");
                DrawRow(cx, cy - 70, 0, "1 Player (SP + Dummy)");
                DrawRow(cx, cy - 35, 1, "2 Players (couch)");
                DrawRow(cx, cy, 2, "3 Players (couch)");
                DrawRow(cx, cy + 35, 3, "4 Players (couch)");
                GUI.Label(new Rect(cx - 150, cy + 75, 300, 40), "1–4 · Enter");
            }
            else if (State == GameFlowState.ModeSelect)
            {
                GUI.Box(new Rect(cx - 220, cy - 150, 440, 300), LocalPlayerRoster.IsCouch ? $"Mode — {LocalPlayerRoster.PlayerCount}P couch" : "Mode — SP + Dummy");
                DrawMode(cx, cy - 100, 0, "1  Hot Potato  (first to 2 - fuse 45/40/35s)");
                DrawMode(cx, cy - 60, 1, "2  Least It    (120s + next-punch tiebreak)");
                DrawMode(cx, cy - 20, 2, "3  Trail Tag   (ribbons eliminate - last standing)");
                DrawMode(cx, cy + 20, 3, "4  Free play   (punch transfers It - no timer)");
                GUI.Label(new Rect(cx - 180, cy + 70, 360, 40), "1/2/3/4 · Enter to play");
            }
            else if (State == GameFlowState.Paused)
            {
                GUI.Box(new Rect(cx - 120, cy - 60, 240, 120), "Paused");
                if (GUI.Button(new Rect(cx - 60, cy - 10, 120, 28), "Resume")) TogglePause();
                if (GUI.Button(new Rect(cx - 60, cy + 25, 120, 28), "Quit to Menu")) QuitToMenu();
            }
            else if (State == GameFlowState.RoundEnd)
            {
                // TagModeController draws the center card when a round is running.
                if (TagModeController.Instance == null)
                {
                    GUI.Box(new Rect(cx - 220, cy - 70, 440, 140), "Round over");
                    GUI.Label(new Rect(cx - 200, cy - 36, 400, 70),
                        $"{LastResultMessage}\n\nR  Rematch    Q  Menu");
                }
            }
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
