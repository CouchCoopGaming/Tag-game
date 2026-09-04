using System.Collections.Generic;
using Tag.Core;
using Tag.Gameplay;
using Tag.Trail;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Selects / instantiates an ITagMode, owns shared It-transfer, end conditions, HUD.
    /// Replaces the LeastIt-only TagRoundController path.
    /// </summary>
    public class TagModeController : MonoBehaviour
    {
        public static TagModeController Instance { get; private set; }

        public const string PrefsModeKey = "Tag.SelectedMode";

        [SerializeField] TagModeId selectedMode = TagModeId.LeastIt;
        [SerializeField] bool autoFindPlayers = true;
        [SerializeField] List<ItController> players = new List<ItController>();
        [SerializeField] LeastItTuning leastItTuning;
        [SerializeField] HotPotatoTuning hotPotatoTuning;
        [SerializeField] TrailTagTuning trailTagTuning;

        readonly TagModeContext _ctx = new TagModeContext();
        ITagMode _mode;
        bool _endedNotified;

        public TagModeId SelectedMode
        {
            get => selectedMode;
            set => selectedMode = value;
        }

        public float Remaining => _ctx.RemainingTime;
        public bool IsRunning => _ctx.RoundRunning;
        public bool RoundActive => _ctx.RoundRunning;
        public ItController CurrentIt => _ctx.CurrentIt;
        public ITagMode ActiveMode => _mode;
        public TagModeContext Context => _ctx;

        void Awake()
        {
            Instance = this;
            if (GameFlow.SelectedMode.HasValue)
                selectedMode = GameFlow.SelectedMode.Value;
            else if (PlayerPrefs.HasKey(PrefsModeKey))
                selectedMode = (TagModeId)PlayerPrefs.GetInt(PrefsModeKey, (int)TagModeId.LeastIt);

            if (leastItTuning == null) leastItTuning = LeastItTuning.CreateRuntimeDefaults();
            if (hotPotatoTuning == null) hotPotatoTuning = HotPotatoTuning.CreateRuntimeDefaults();
            if (trailTagTuning == null) trailTagTuning = TrailTagTuning.CreateRuntimeDefaults();

            _ctx.Eliminate = p => EliminatePlayer(p, "mode");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (autoFindPlayers)
                RefreshPlayers();
            // If no GameFlow, auto-start (Play scene opened directly).
            if (FindFirstObjectByType<GameFlow>() == null)
                StartRound();
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
                case TagModeId.HotPotato:
                    return new HotPotatoMode(hotPotatoTuning);
                case TagModeId.TrailTag:
                    return new TrailTagMode(trailTagTuning);
                case TagModeId.LeastIt:
                default:
                    return new LeastItMode(leastItTuning);
            }
        }

        public void StartRound()
        {
            RefreshPlayers();
            _endedNotified = false;
            selectedMode = GameFlow.SelectedMode ?? selectedMode;
            _mode = CreateMode(selectedMode);

            _ctx.Players.Clear();
            _ctx.Players.AddRange(players);
            _ctx.CurrentIt = null;
            _ctx.Elapsed = 0f;
            _ctx.RoundRunning = true;

            // Disable trail emit by default; TrailTagMode enables.
            foreach (var p in players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null)
                {
                    e.ClearTrail();
                    e.SetEmitting(false);
                }
            }

            _mode.OnRoundStart(_ctx);

            if (_ctx.Players.Count > 0)
            {
                var living = new List<ItController>();
                foreach (var p in _ctx.LivingPlayers()) living.Add(p);
                if (living.Count > 0)
                {
                    int idx = Random.Range(0, living.Count);
                    TransferIt(null, living[idx]);
                }
            }

            Debug.Log($"[TagMode] Started {_mode.Id} — {_ctx.Players.Count} players.");
        }

        public void Rematch() => StartRound();

        void Update()
        {
            if (!_ctx.RoundRunning || _mode == null) return;

            float dt = Time.deltaTime;
            _ctx.Elapsed += dt;
            _mode.Tick(_ctx, dt);

            if (_mode.ShouldEndRound(_ctx))
                EndRound();
        }

        public void OnSuccessfulPunch(ItController puncher, ItController target)
        {
            if (!_ctx.RoundRunning || puncher == null || target == null) return;
            if (!puncher.IsIt) return;
            if (!target.IsAlive || !target.CanBeTagged) return;
            TransferIt(puncher, target);
            _mode?.OnPunchTransfer(_ctx, puncher, target);
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
            else if (to == null)
            {
                _ctx.CurrentIt = null;
            }
        }

        public void EliminatePlayer(ItController player, string reason = "")
        {
            if (player == null || !player.IsAlive) return;
            player.Eliminate();
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
                EndRound();
        }

        void EndRound()
        {
            if (!_ctx.RoundRunning) return;
            _ctx.RoundRunning = false;
            if (_ctx.RemainingTime < 0f) _ctx.RemainingTime = 0f;

            var winners = _mode != null ? _mode.GetWinnerIds(_ctx) : new List<string>();
            string msg = winners != null && winners.Count > 0
                ? "Winner(s): " + string.Join(", ", winners)
                : "No winners";
            Debug.Log($"[TagMode] END {_mode?.Id} — {msg}");

            if (_endedNotified) return;
            _endedNotified = true;

            var flow = GameFlow.Instance != null ? GameFlow.Instance : FindFirstObjectByType<GameFlow>();
            if (flow != null)
                flow.OnRoundEnded();
        }

        void OnGUI()
        {
            string body = _mode != null ? _mode.GetHud(_ctx) : $"Mode {selectedMode}";
            if (!_ctx.RoundRunning)
                body += "\n(R = Rematch)";
            GUI.Box(new Rect(12, Screen.height - 140, 420, 128), "");
            GUI.Label(new Rect(20, Screen.height - 134, 400, 120), body);
        }
    }
}
