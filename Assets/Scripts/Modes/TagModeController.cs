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
    /// Shared shell: Countdown -> Round(s) -> Results. Delegates rules to ITagMode.
    /// TagRoundController on the same GO wraps this for scene GUID back-compat.
    /// Playtest: F1 Hot Potato, F2 Least It, F3 Trail Tag, F4 Free play -> StartRound(mode).
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
        bool _firstCountdownHint = true;

        public TagModeId SelectedMode { get => selectedMode; set => selectedMode = value; }
        public MatchTuning MatchTuningAsset => matchTuning;
        public HotPotatoTuning HotPotatoTuningAsset => hotPotatoTuning;
        public float Remaining => _ctx.RemainingTime;
        public bool IsRunning => _ctx.RoundRunning;
        public bool RoundActive =>
            _phase == MatchPhase.Countdown || _phase == MatchPhase.Playing || _phase == MatchPhase.PostRound;
        public ItController CurrentIt => _ctx.CurrentIt;
        public ITagMode ActiveMode => _mode;
        public TagModeContext Context => _ctx;
        public MatchPhase Phase => _phase;
        public string ResultMessage => _resultMessage;
        /// <summary>Last punch/round handoff, for the local TAG flash.</summary>
        public string LastFromId { get; private set; }
        public string LastToId { get; private set; }

        /// <summary>Living players' TimeAsIt (already on ItController); empty if no context players.</summary>
        public IReadOnlyList<ItController> PlayersForHud => _ctx.Players;

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
            // Keep hub menu cursor in sync when F1-F4 restart a round in-play.
            if (GameFlow.Instance != null)
                GameFlow.Instance.SyncSelectedMode(id);
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
                case TagModeId.FreePlay: return new FreePlayMode();
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
                // Stop a live ragdoll coroutine before Revive unlocks the motor, or it locks them again.
                var rag = p.GetComponent<Tag.Gameplay.PlayerRagdoll>();
                if (rag != null) rag.ForceRecover();
                var motor = p.GetComponent<TagArena.Movement.PlayerMotor>();
                if (motor != null) motor.ClearStun();
                p.Revive();
                p.ResetScore();
                p.SetIt(false);
                p.ApplySpawnIFrames(matchTuning.spawnIFramesSec);
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null) { e.ClearTrail(); e.SetEmitting(false); }
            }

            PlacePlayersOnPads();

            _phase = MatchPhase.Countdown;
            _phaseTimer = Mathf.Max(0.01f, matchTuning.countdownSec);
            Debug.Log($"[TagMode] Countdown {_phaseTimer:0}s -> {selectedMode} ({_ctx.Players.Count}p)");
        }

        public void Rematch() => StartRound(selectedMode);

        void BeginPlaying()
        {
            _firstCountdownHint = false;
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
            PollPlaytestModeHotkeys();

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

        /// <summary>
        /// Playtest: F1 Hot Potato / F2 Least It / F3 Trail Tag / F4 Free play - SetMode + StartRound cleanly.
        /// Works in any phase (Idle/Countdown/Playing/Results).
        /// </summary>
        void PollPlaytestModeHotkeys()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("[TagMode] Playtest hotkey F1 -> Hot Potato");
                StartRound(TagModeId.HotPotato);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log("[TagMode] Playtest hotkey F2 -> Least It");
                StartRound(TagModeId.LeastIt);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
            {
                Debug.Log("[TagMode] Playtest hotkey F3 -> Trail Tag");
                StartRound(TagModeId.TrailTag);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F4))
            {
                Debug.Log("[TagMode] Playtest hotkey F4 -> Free play");
                StartRound(TagModeId.FreePlay);
            }
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
            LastFromId = from != null ? from.PlayerId : "";
            if (from != null) from.SetIt(false);
            if (to != null && to.IsAlive)
            {
                to.SetIt(true);
                _ctx.CurrentIt = to;
                LastToId = to.PlayerId;
                Debug.Log($"[TagMode] It -> {to.PlayerId}");
            }
            else
            {
                _ctx.CurrentIt = null;
                LastToId = "";
            }
        }

        /// <summary>
        /// F1–F4 start a round from the pads, not from wherever the last ragdoll stopped.
        /// Slot follows P1/P2/… when the id parses; everyone else fills the next free pad.
        /// Yaw is left alone — the chase camera owns it.
        /// </summary>
        void PlacePlayersOnPads()
        {
            var pads = Tag.Local.LocalPlayerSpawner.Spawns;
            if (pads == null || pads.Length == 0) return;
            var used = new bool[pads.Length];
            int next = 0;
            foreach (var p in players)
            {
                if (p == null) continue;
                int idx = -1;
                string id = p.PlayerId;
                if (!string.IsNullOrEmpty(id) && id.Length > 1 && (id[0] == 'P' || id[0] == 'p')
                    && int.TryParse(id.Substring(1), out int n)
                    && n >= 1 && n <= pads.Length)
                    idx = n - 1;
                if (idx < 0 || used[idx])
                {
                    while (next < used.Length && used[next]) next++;
                    idx = next < used.Length ? next : 0;
                    if (next < used.Length) next++;
                }
                used[idx] = true;

                Vector3 pad = pads[idx];
                var rb = p.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity = false;
                    rb.position = pad;
                }
                p.transform.position = pad;
            }
            Physics.SyncTransforms();
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
            Debug.Log($"[TagMode] END -- {_resultMessage}");

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
            DrawItBanner();

            if (_phase == MatchPhase.Countdown)
            {
                float cx = Screen.width * 0.5f;
                float cy = Screen.height * 0.35f;
                GUI.Box(new Rect(cx - 140, cy, 280, 88), "");
                GUI.Label(new Rect(cx - 130, cy + 10, 260, 28), $"Get ready  {_phaseTimer:0}");
                GUI.Label(new Rect(cx - 130, cy + 36, 260, 40),
                    _firstCountdownHint
                        ? "WASD sprint  Ctrl slide  Q dash\nLMB punch transfers It"
                        : "Punch the dummy with the orange hat");
                return;
            }

            string body = _mode != null ? _mode.GetHud(_ctx) : $"Mode {selectedMode}";
            if (_phase == MatchPhase.Results)
                body += $"\n{_resultMessage}\n(R = Rematch  Q = Menu)";
            else if (_phase == MatchPhase.PostRound)
                body += $"\nPost-round {_phaseTimer:0.0}s";
            GUI.Box(new Rect(12, Screen.height - 168, 480, 156), "");
            GUI.Label(new Rect(20, Screen.height - 162, 464, 148), body);
        }

        void DrawItBanner()
        {
            if (_phase != MatchPhase.Playing && _phase != MatchPhase.PostRound) return;
            var it = _ctx.CurrentIt;
            float w = 420f;
            var r = new Rect((Screen.width - w) * 0.5f, 16f, w, 46f);
            GUI.Box(r, "");
            string text;
            if (it == null)
                text = "No one is It";
            else if (it.GetComponent<TagArena.Movement.PlayerInputReader>() != null && it.GetComponent<DummyPatrol>() == null)
                text = "YOU ARE IT    punch to dump it";
            else
                text = $"IT: {it.PlayerId}    orange hat    punch to tag";
            GUI.Label(new Rect(r.x + 12, r.y + 12, w - 24, 24), text);
        }
    }
}
