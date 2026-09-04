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
        Playing,
        Rematch
    }

    /// <summary>Boot → Mode Select → Play → Rematch (mode retained).</summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] string bootSceneName = "Boot";
        [SerializeField] string playSceneName = "Play";
        [SerializeField] float bootDelay = 0.35f;
        [SerializeField] bool autoLoadPlay = false;

        public TagRoundController round;
        public GameFlowState State { get; private set; } = GameFlowState.Boot;
        public TagModeId SelectedMode { get; private set; } = TagModeId.LeastIt;
        public string LastResultMessage { get; private set; } = "";

        ModeSelectUI _modeSelect;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Play.unity mounts GameFlow on Systems with TagModeRunner — never destroy the whole GO.
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            _modeSelect = GetComponent<ModeSelectUI>();
            if (_modeSelect == null)
                _modeSelect = gameObject.AddComponent<ModeSelectUI>();
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
                // Opened Play directly — skip select, use SelectedMode / editor default
                State = GameFlowState.Playing;
                EnsureRoundStarted();
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == playSceneName || scene.name == "Play")
            {
                State = GameFlowState.Playing;
                EnsureRoundStarted();
            }
            else if (scene.name == bootSceneName || scene.name == "Boot")
            {
                if (State != GameFlowState.ModeSelect)
                    State = GameFlowState.Boot;
            }
        }

        public void StartPlay() => GoToModeSelect();

        public void GoToModeSelect()
        {
            State = GameFlowState.ModeSelect;
        }

        public void ConfirmModeAndPlay(TagModeId mode)
        {
            SelectedMode = mode;
            GoToPlay();
        }

        public void GoToPlay()
        {
            State = GameFlowState.Playing;
            if (SceneManager.GetActiveScene().name != playSceneName)
                SceneManager.LoadScene(playSceneName);
            else
                EnsureRoundStarted();
        }

        public void OnRoundEnded()
        {
            OnRoundEnded(LastResultMessage);
        }

        public void OnRoundEnded(string resultMessage)
        {
            LastResultMessage = resultMessage ?? "";
            // Rematch prompt while still on Play scene
            State = GameFlowState.Rematch;
        }

        public void Rematch()
        {
            State = GameFlowState.Rematch;
            if (round == null)
                round = FindFirstObjectByType<TagRoundController>();
            var runner = round != null ? (TagModeRunner)round : FindFirstObjectByType<TagModeRunner>();
            if (runner != null)
            {
                runner.SetMode(SelectedMode);
                runner.Rematch();
            }
            else
                SceneManager.LoadScene(playSceneName);
            State = GameFlowState.Playing;
        }

        public void BackToModeSelect()
        {
            State = GameFlowState.ModeSelect;
            if (SceneManager.GetActiveScene().name != bootSceneName)
                SceneManager.LoadScene(bootSceneName);
        }

        void EnsureRoundStarted()
        {
            if (round == null)
                round = FindFirstObjectByType<TagRoundController>();
            var runner = round != null ? (TagModeRunner)round : FindFirstObjectByType<TagModeRunner>();
            if (runner == null) return;

            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
                runner.RegisterPlayer(p);

            runner.SetMode(SelectedMode);
            if (!runner.RoundActive)
                runner.StartRound(SelectedMode);
        }

        void Update()
        {
            if (State == GameFlowState.Boot)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    GoToModeSelect();
            }
            else if (State == GameFlowState.Rematch ||
                     (State == GameFlowState.Playing && round != null && !round.RoundActive))
            {
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
                    Rematch();
                if (Input.GetKeyDown(KeyCode.M))
                    BackToModeSelect();
            }
        }

        void OnGUI()
        {
            if (State == GameFlowState.Boot)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 140, Screen.height * 0.5f - 50, 280, 100), "TAG — Boot");
                if (GUI.Button(new Rect(Screen.width * 0.5f - 70, Screen.height * 0.5f, 140, 32), "Mode Select"))
                    GoToModeSelect();
                GUI.Label(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 40, 200, 24), "Enter / Space");
            }
            else if (State == GameFlowState.Rematch)
            {
                string msg = string.IsNullOrEmpty(LastResultMessage) ? "Round over" : LastResultMessage;
                GUI.Box(new Rect(Screen.width * 0.5f - 220, 40, 440, 70), "");
                GUI.Label(new Rect(Screen.width * 0.5f - 210, 48, 420, 50),
                    $"{msg}\nR/Enter Rematch (keep mode) · M Mode Select");
            }
        }
    }
}
