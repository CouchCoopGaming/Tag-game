using System.Collections.Generic;
using Tag.Core;
using Tag.Gameplay;
using Tag.Trail;
using UnityEngine;
using Tag.Audio;

namespace Tag.Modes
{
    public enum MatchPhase
    {
        Idle,
        Countdown,
        Playing,
        PostRound,
        Results
    }

    /// <summary>
    /// Shared shell: Countdown → Round(s) → Results. Delegates rules to ITagMode.
    /// TagRoundController on the same GO wraps this for scene GUID back-compat.
    /// </summary>
    public class TagModeController : MonoBehaviour
    {
        public static TagModeController Instance { get; private set; }
        public const string PrefsModeKey = "Tag.SelectedMode";

        [SerializeField] TagModeId selectedMode = TagModeId.LeastIt;
        [SerializeField] bool autoFindPlayers = true;
        [SerializeField] List<ItController> players = new List<ItController>();
        [SerializeField] MatchTuning matchTuning;
        [SerializeField] LeastItTuning leastItTuning;
        [SerializeField] HotPotatoTuning hotPotatoTuning;
        [SerializeField] TrailTagTuning trailTagTuning;

        readonly TagModeContext _ctx = new TagModeContext();
        ITagMode _mode;
        bool _endedNotified;
        MatchPhase _phase = MatchPhase.Idle;
        float _phaseTimer;
        string _resultMessage = "";

        public TagModeId SelectedMode { get => selectedMode; set => selectedMode = value; }
        public MatchTuning MatchTuningAsset => matchTuning;
        public float Remaining => _ctx.RemainingTime;
        public bool IsRunning => _ctx.RoundRunning;
        public bool RoundActive =>
            _phase == MatchPhase.Countdown || _phase == MatchPhase.Playing || _phase == MatchPhase.PostRound;
        public ItController CurrentIt => _ctx.CurrentIt;
        public ITagMode ActiveMode => _mode;
        public TagModeContext Context => _ctx;
        public MatchPhase Phase => _phase;
        public string ResultMessage => _resultMessage;

        void Awake()
        {
            Instance = this;
            ApplyPersistedMode();
            if (matchTuning == null) matchTuning = MatchTuning.CreateRuntimeDefaults();
            if (leastItTuning == null) leastItTuning = LeastItTuning.CreateRuntimeDefaults();
            if (hotPotatoTuning == null) hotPotatoTuning = HotPotatoTuning.CreateRuntimeDefaults();
            if (trailTagTuning == null) trailTagTuning = TrailTagTuning.CreateRuntimeDefaults();
            _ctx.Eliminate = p => EliminatePlayer(p, "mode");
            _ctx.EnterPostRound = EnterPostRound;
            _ctx.MatchTuning = matchTuning;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void ApplyPersistedMode()
        {
            if (GameFlow.Instance != null)
                selectedMode = GameFlow.Instance.SelectedMode;
            else if (PlayerPrefs.HasKey(PrefsModeKey))
                selectedMode = (TagModeId)PlayerPrefs.GetInt(PrefsModeKey, (int)TagModeId.LeastIt);
        }

        void Start()
        {
            if (autoFindPlayers) RefreshPlayers();
            if (FindFirstObjectByType<GameFlow>() == null)
                StartRound();
        }

        public void SetMode(TagModeId id)
        {
            selectedMode = id;
            PlayerPrefs.SetInt(PrefsModeKey, (int)id);
        }

        public void RefreshPlayers()
        {
            players.Clear();
            players.AddRange(FindObjectsByType<ItController>(FindObjectsSortMode.None));
            EnsureTrailEmitters();
        }

        public void RegisterPlayer(ItController p)
        {
            if (p != null && !players.Contains(p))
                players.Add(p);
            EnsureTrailEmitters();
        }

        void EnsureTrailEmitters()
        {
            foreach (var p in players)
            {
                if (p == null) continue;
                if (p.GetComponent<PlayerTrailEmitter>() == null)
                    p.gameObject.AddComponent<PlayerTrailEmitter>();
            }
        }

        ITagMode CreateMode(TagModeId id)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return new HotPotatoMode(hotPotatoTuning);
                case TagModeId.TrailTag: return new TrailTagMode(trailTagTuning);
                case TagModeId.LeastIt:
                default: return new LeastItMode(leastItTuning);
            }
        }

        public void StartRound() => StartRound(selectedMode);

        public void StartRound(TagModeId id)
        {
            SetMode(id);
            RefreshPlayers();
            _endedNotified = false;
            _resultMessage = "";
            _mode = CreateMode(selectedMode);

            _ctx.Players.Clear();
            _ctx.Players.AddRange(players);
            _ctx.CurrentIt = null;
            _ctx.Elapsed = 0f;
            _ctx.RemainingTime = 0f;
            _ctx.RoundRunning = false;
            _ctx.SuddenDeath = false;
            _ctx.MatchTuning = matchTuning;
            _ctx.EnterPostRound = EnterPostRound;
            _ctx.Eliminate = p => EliminatePlayer(p, "mode");

            foreach (var p in players)
            {
                if (p == null) continue;
                p.Revive();
                p.ResetScore();
                p.SetIt(false);
                p.ApplySpawnIFrames(matchTuning.spawnIFramesSec);
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null) { e.ClearTrail(); e.SetEmitting(false); }
            }

            _phase = MatchPhase.Countdown;
            _phaseTimer = Mathf.Max(0.01f, matchTuning.countdownSec);
            Debug.Log($"[TagMode] Countdown {_phaseTimer:0}s → {selectedMode} ({_ctx.Players.Count}p)");
        }

        public void Rematch() => StartRound(selectedMode);

        void BeginPlaying()
        {
            _phase = MatchPhase.Playing;
            _ctx.RoundRunning = true;
            AudioCuePlayer.Ensure()?.RoundStart();
            _mode.OnRoundStart(_ctx);

            if (_ctx.CurrentIt == null)
            {
                var living = new List<ItController>();
                foreach (var p in _ctx.LivingPlayers()) living.Add(p);
                if (living.Count > 0)
                    TransferIt(null, living[Random.Range(0, living.Count)]);
            }
            Debug.Log($"[TagMode] Playing {_mode.Id}");
        }

        public void EnterPostRound(float seconds)
        {
            _phase = MatchPhase.PostRound;
            _phaseTimer = Mathf.Max(0.01f, seconds);
            _ctx.RoundRunning = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (_phase == MatchPhase.Countdown)
            {
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f) BeginPlaying();
                return;
            }

            if (_phase == MatchPhase.PostRound)
            {
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f)
                {
                    _phase = MatchPhase.Playing;
                    _ctx.RoundRunning = true;
                }
                return;
            }

            if (_phase != MatchPhase.Playing || !_ctx.RoundRunning || _mode == null)
                return;

            _ctx.Elapsed += dt;
            _mode.Tick(_ctx, dt);
            if (_mode.ShouldEndRound(_ctx))
                EndMatch();
        }

        public void OnSuccessfulPunch(ItController puncher, ItController target)
        {
            if (_phase != MatchPhase.Playing || !_ctx.RoundRunning) return;
            if (puncher == null || target == null) return;
            if (!puncher.IsIt || puncher.IsEliminated) return;
            if (!target.IsAlive || !target.CanBeTagged) return;
            TransferIt(puncher, target);
            _mode?.OnPunchTransfer(_ctx, puncher, target);
            if (_mode != null && _mode.ShouldEndRound(_ctx))
                EndMatch();
        }

        public void TransferIt(ItController from, ItController to)
        {
            if (from != null) from.SetIt(false);
            if (to != null && to.IsAlive)
            {
                to.SetIt(true);
                _ctx.CurrentIt = to;
                Debug.Log($"[TagMode] It → {to.PlayerId}");
            }
            else
                _ctx.CurrentIt = null;
        }

        public void EliminatePlayer(ItController player, string reason = "")
        {
            if (player == null || !player.IsAlive) return;
            player.Eliminate(string.IsNullOrEmpty(reason) ? "eliminated" : reason);
            if (_ctx.CurrentIt == player)
            {
                player.SetIt(false);
                _ctx.CurrentIt = null;
            }
            var e = player.GetComponent<PlayerTrailEmitter>();
            if (e != null) e.SetEmitting(false);
            _mode?.OnPlayerEliminated(_ctx, player);
            Debug.Log($"[TagMode] Eliminated {player.PlayerId} ({reason})");
            if (_mode != null && _mode.ShouldEndRound(_ctx))
                EndMatch();
        }

        void EndMatch()
        {
            if (_phase == MatchPhase.Results) return;
            _ctx.RoundRunning = false;
            if (_ctx.RemainingTime < 0f) _ctx.RemainingTime = 0f;
            _phase = MatchPhase.Results;

            var winners = _mode != null ? _mode.GetWinnerIds(_ctx) : new List<string>();
            _resultMessage = winners != null && winners.Count > 0
                ? $"[{_mode?.Id}] Winner(s): " + string.Join(", ", winners)
                : $"[{_mode?.Id}] No winners";
            Debug.Log($"[TagMode] END — {_resultMessage}");

            foreach (var p in players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null) e.SetEmitting(false);
            }

            if (_endedNotified) return;
            _endedNotified = true;
            var flow = GameFlow.Instance != null ? GameFlow.Instance : FindFirstObjectByType<GameFlow>();
            if (flow != null) flow.OnRoundEnded(_resultMessage);
        }

        void OnGUI()
        {
            if (_phase == MatchPhase.Countdown)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 80, Screen.height * 0.35f, 160, 60), "");
                GUI.Label(new Rect(Screen.width * 0.5f - 70, Screen.height * 0.35f + 18, 140, 30),
                    $"Countdown {_phaseTimer:0.0}");
                return;
            }

            string body = _mode != null ? _mode.GetHud(_ctx) : $"Mode {selectedMode}";
            if (_phase == MatchPhase.Results)
                body += $"\n{_resultMessage}\n(R = Rematch)";
            else if (_phase == MatchPhase.PostRound)
                body += $"\nPost-round {_phaseTimer:0.0}s";
            GUI.Box(new Rect(12, Screen.height - 150, 460, 138), "");
            GUI.Label(new Rect(20, Screen.height - 144, 440, 130), body);
        }
    }
}
