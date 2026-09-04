using UnityEngine;
using UnityEngine.SceneManagement;
using Tag.Gameplay;

namespace Tag.Core
{
    public enum GameFlowState
    {
        Boot,
        Play,
        RoundEnd,
        Rematch
    }

    /// <summary>Boot → Play → Rematch stub flow.</summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] string bootSceneName = "Boot";
        [SerializeField] string playSceneName = "Play";
        [SerializeField] float bootDelay = 0.35f;
        [SerializeField] bool autoLoadPlay = true;

        public TagRoundController round;
        public GameFlowState State { get; private set; } = GameFlowState.Boot;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == bootSceneName || scene == "Boot")
            {
                State = GameFlowState.Boot;
                if (autoLoadPlay)
                    Invoke(nameof(GoToPlay), bootDelay);
            }
            else
            {
                State = GameFlowState.Play;
                EnsureRoundStarted();
            }
        }

        public void GoToPlay()
        {
            State = GameFlowState.Play;
            if (SceneManager.GetActiveScene().name != playSceneName)
                SceneManager.LoadScene(playSceneName);
            else
                EnsureRoundStarted();
        }

        public void OnRoundEnded()
        {
            State = GameFlowState.RoundEnd;
        }

        public void Rematch()
        {
            State = GameFlowState.Rematch;
            if (round == null)
                round = FindFirstObjectByType<TagRoundController>();
            if (round != null)
                round.Rematch();
            else
                SceneManager.LoadScene(playSceneName);
            State = GameFlowState.Play;
        }

        void EnsureRoundStarted()
        {
            if (round == null)
                round = FindFirstObjectByType<TagRoundController>();
            if (round == null) return;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
                round.RegisterPlayer(p);
            if (!round.RoundActive)
                round.StartRound();
        }

        void Update()
        {
            if (State == GameFlowState.RoundEnd || (State == GameFlowState.Play && round != null && !round.RoundActive))
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                    Rematch();
            }
        }

        void OnGUI()
        {
            if (State == GameFlowState.Boot)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 120, Screen.height * 0.5f - 40, 240, 80), "TAG — Boot");
                if (GUI.Button(new Rect(Screen.width * 0.5f - 60, Screen.height * 0.5f, 120, 28), "Start"))
                    GoToPlay();
            }
            else if (State == GameFlowState.RoundEnd)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 140, 40, 280, 60), "Round over — press R to Rematch");
            }
        }
    }
}
