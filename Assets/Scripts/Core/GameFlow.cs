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

        public void GoToPlayerCount() { State = GameFlowState.PlayerCount; AudioCuePlayer.Ensure()?.UiClick(); }
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
            var msg = LastResultMessage.ToLowerInvariant();
            if (msg.Contains("win"))
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
            if (modeController == null) modeController = FindFirstObjectByType<TagModeController>();
            if (modeController != null) modeController.Rematch();
            else SceneManager.LoadScene(playSceneName);
            State = GameFlowState.Play;
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
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
                    GoToModeSelect();
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
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) _menuCursor = (_menuCursor + 2) % 3;
                if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) _menuCursor = (_menuCursor + 1) % 3;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
                    ConfirmModeAndPlay();
            }
            else if (State == GameFlowState.RoundEnd)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.R)) Rematch();
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) QuitToMenu();
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
                GUI.Box(new Rect(cx - 140, cy - 50, 280, 100), "TAG — Steam MVP");
                if (GUI.Button(new Rect(cx - 60, cy - 5, 120, 28), "Play SP")) { LocalPlayerRoster.SetCount(1); GoToModeSelect(); }
                if (GUI.Button(new Rect(cx - 60, cy + 30, 120, 28), "Couch…")) GoToPlayerCount();
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
                GUI.Box(new Rect(cx - 200, cy - 120, 400, 240), LocalPlayerRoster.IsCouch ? $"Mode — {LocalPlayerRoster.PlayerCount}P couch" : "Mode — SP + Dummy");
                DrawMode(cx, cy - 70, 0, "1  Hot Potato  (first to 2 · fuse 45/40/35s)");
                DrawMode(cx, cy - 30, 1, "2  Least It    (120s + next-punch tiebreak)");
                DrawMode(cx, cy + 10, 2, "3  Trail Tag   (ribbons eliminate · last standing)");
                GUI.Label(new Rect(cx - 160, cy + 55, 320, 40), "1/2/3 · Enter to play");
            }
            else if (State == GameFlowState.Paused)
            {
                GUI.Box(new Rect(cx - 120, cy - 60, 240, 120), "Paused");
                if (GUI.Button(new Rect(cx - 60, cy - 10, 120, 28), "Resume")) TogglePause();
                if (GUI.Button(new Rect(cx - 60, cy + 25, 120, 28), "Quit to Menu")) QuitToMenu();
            }
            else if (State == GameFlowState.RoundEnd)
            {
                GUI.Box(new Rect(cx - 180, 40, 360, 80),
                    $"{LastResultMessage}\nR Rematch · Q Menu");
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
            var r = new Rect(cx - 180, y, 360, 28);
            if (sel) GUI.Box(r, "");
            if (GUI.Button(r, (sel ? "> " : "  ") + label))
            {
                _menuCursor = index;
                ConfirmModeAndPlay();
            }
        }
    }
}
