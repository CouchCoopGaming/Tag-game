using UnityEngine;
using UnityEngine.SceneManagement;
using Tag.Gameplay;
using Tag.Modes;

namespace Tag.Core
{
    public enum GameFlowState
    {
        Boot,
        ModeSelect,
        Play,
        RoundEnd,
        Rematch
    }

    /// <summary>Boot → ModeSelect (1/2/3 + Enter) → Play → Rematch (keeps mode).</summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] string bootSceneName = "Boot";
        [SerializeField] string playSceneName = "Play";
        [SerializeField] float bootDelay = 0.35f;
        [SerializeField] bool autoLoadPlay = false;

        public TagModeController modeController;
        public TagRoundController round;

        public GameFlowState State { get; private set; } = GameFlowState.Boot;
        public TagModeId SelectedMode { get; private set; } = TagModeId.LeastIt;
        public string LastResultMessage { get; private set; } = "";

        int _menuCursor = 1;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (PlayerPrefs.HasKey(TagModeController.PrefsModeKey))
            {
                SelectedMode = (TagModeId)PlayerPrefs.GetInt(TagModeController.PrefsModeKey, (int)TagModeId.LeastIt);
                _menuCursor = (int)SelectedMode;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == bootSceneName || scene == "Boot")
            {
                State = GameFlowState.Boot;
                if (autoLoadPlay)
                    Invoke(nameof(GoToModeSelect), bootDelay);
            }
            else
            {
                State = GameFlowState.Play;
                EnsureRoundStarted();
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == playSceneName || scene.name == "Play")
            {
                State = GameFlowState.Play;
                EnsureRoundStarted();
            }
        }

        public void StartPlay() => GoToModeSelect();
        public void GoToModeSelect() => State = GameFlowState.ModeSelect;

        public void ConfirmModeAndPlay()
        {
            SelectedMode = (TagModeId)_menuCursor;
            PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)SelectedMode);
            PlayerPrefs.Save();
            GoToPlay();
        }

        public void ConfirmModeAndPlay(TagModeId mode)
        {
            _menuCursor = (int)mode;
            ConfirmModeAndPlay();
        }

        public void GoToPlay()
        {
            State = GameFlowState.Play;
            if (SceneManager.GetActiveScene().name != playSceneName)
                SceneManager.LoadScene(playSceneName);
            else
                EnsureRoundStarted();
        }

        public void OnRoundEnded() => OnRoundEnded(LastResultMessage);

        public void OnRoundEnded(string resultMessage)
        {
            LastResultMessage = resultMessage ?? "";
            State = GameFlowState.RoundEnd;
        }

        public void Rematch()
        {
            State = GameFlowState.Rematch;
            ResolveControllers();
            if (modeController != null)
            {
                modeController.SetMode(SelectedMode);
                modeController.Rematch();
            }
            else if (round != null)
            {
                round.SetMode(SelectedMode);
                round.Rematch();
            }
            else
                SceneManager.LoadScene(playSceneName);
            State = GameFlowState.Play;
        }

        void ResolveControllers()
        {
            if (modeController == null)
                modeController = TagModeController.Instance ?? FindFirstObjectByType<TagModeController>();
            if (round == null)
                round = FindFirstObjectByType<TagRoundController>();
            if (modeController == null && round != null)
                modeController = round;
        }

        void EnsureRoundStarted()
        {
            ResolveControllers();
            if (modeController == null) return;
            modeController.SetMode(SelectedMode);
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
                modeController.RegisterPlayer(p);
            if (!modeController.RoundActive)
                modeController.StartRound(SelectedMode);
        }

        void Update()
        {
            if (State == GameFlowState.Boot)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    GoToModeSelect();
            }
            else if (State == GameFlowState.ModeSelect)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) _menuCursor = 0;
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) _menuCursor = 1;
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) _menuCursor = 2;
                if (Input.GetKeyDown(KeyCode.UpArrow)) _menuCursor = (_menuCursor + 2) % 3;
                if (Input.GetKeyDown(KeyCode.DownArrow)) _menuCursor = (_menuCursor + 1) % 3;
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    ConfirmModeAndPlay();
            }
            else if (State == GameFlowState.RoundEnd ||
                     (State == GameFlowState.Play && modeController != null && !modeController.RoundActive))
            {
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
                    Rematch();
                if (Input.GetKeyDown(KeyCode.M))
                {
                    State = GameFlowState.ModeSelect;
                    if (SceneManager.GetActiveScene().name != bootSceneName)
                        SceneManager.LoadScene(bootSceneName);
                }
            }
        }

        void OnGUI()
        {
            if (State == GameFlowState.Boot)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 120, Screen.height * 0.5f - 40, 240, 80), "TAG — Boot");
                if (GUI.Button(new Rect(Screen.width * 0.5f - 60, Screen.height * 0.5f, 120, 28), "Start"))
                    GoToModeSelect();
            }
            else if (State == GameFlowState.ModeSelect)
            {
                float cx = Screen.width * 0.5f;
                float cy = Screen.height * 0.5f;
                GUI.Box(new Rect(cx - 180, cy - 120, 360, 240), "Select Mode");
                DrawModeRow(cx, cy - 70, 0, "1  Hot Potato  (fuse ~75s — It loses at 0)");
                DrawModeRow(cx, cy - 30, 1, "2  Least It    (105s — least time-as-It)");
                DrawModeRow(cx, cy + 10, 2, "3  Trail Tag   (ribbons eliminate · last standing)");
                GUI.Label(new Rect(cx - 160, cy + 60, 320, 40), "↑↓ or 1/2/3 · Enter to play");
                if (GUI.Button(new Rect(cx - 60, cy + 95, 120, 28), "Play"))
                    ConfirmModeAndPlay();
            }
            else if (State == GameFlowState.RoundEnd)
            {
                string msg = string.IsNullOrEmpty(LastResultMessage) ? "Round over" : LastResultMessage;
                GUI.Box(new Rect(Screen.width * 0.5f - 200, 40, 400, 70), "");
                GUI.Label(new Rect(Screen.width * 0.5f - 190, 48, 380, 54),
                    $"{msg}\nR / Enter Rematch (keeps {SelectedMode})");
            }
        }

        void DrawModeRow(float cx, float y, int index, string label)
        {
            bool sel = _menuCursor == index;
            var r = new Rect(cx - 160, y, 320, 28);
            if (sel) GUI.Box(r, "");
            if (GUI.Button(r, (sel ? "> " : "  ") + label))
            {
                _menuCursor = index;
                ConfirmModeAndPlay();
            }
        }
    }
}
