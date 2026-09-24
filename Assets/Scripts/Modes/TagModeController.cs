using System.Collections.Generic;
using Tag.Core;
using Tag.Gameplay;
using Tag.Trail;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        bool _resultsActionTaken;
        float _resultsInputReadyAt;
        MatchPhase _phase = MatchPhase.Idle;
        float _phaseTimer;
        string _resultMessage = "";
        string _resultTitle = "";
        string _resultDetail = "";
        bool _firstCountdownHint = true;
        float _roundStartGuard;
        bool _localPaused;
        bool _localHelp;
        int _resultsFocus;
        GUIStyle _countStyle;

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
        public bool SuddenDeath => _ctx.SuddenDeath;
        /// <summary>False during the short results arm so Esc/R/Q ignore the round-end click.</summary>
        public bool ResultsInputReady =>
            _phase == MatchPhase.Results
            && !_resultsActionTaken
            && Time.unscaledTime >= _resultsInputReadyAt;
        public string ResultMessage => _resultMessage;
        /// <summary>Last punch/round handoff, for the local TAG flash.</summary>
        public string LastFromId { get; private set; }
        public string LastToId { get; private set; }

        /// <summary>Living players' TimeAsIt (already on ItController); empty if no context players.</summary>
        public IReadOnlyList<ItController> PlayersForHud => _ctx.Players;

        void Awake()
        {
            Tag.Audio.AudioMaster.Load();
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
            // Same-frame double R (this controller and GameFlow) must not restart twice.
            if (Time.unscaledTime < _roundStartGuard)
                return;
            _roundStartGuard = Time.unscaledTime + 0.05f;
            if (_localPaused)
                SetLocalPause(false);
            if (GameFlow.Instance != null)
                GameFlow.Instance.ReturnToPlay();
            // Direct Play has no flow to lock the cursor after the results card.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
            SetMode(id);
            RefreshPlayers();
            _endedNotified = false;
            _resultMessage = "";
            // F1-F4 / rematch leave Results: drop the arm latch so the next card is live.
            _resultsActionTaken = false;
            _resultsFocus = 0;
            _resultsInputReadyAt = 0f;
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
                var punch = p.GetComponent<PunchHitbox>();
                if (punch != null) punch.ForceEnd();
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
            PollLocalPause();
            if (_localPaused)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.H))
                    _localHelp = !_localHelp;
                if (_localHelp)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                        TagArena.Movement.ControlBinds.CycleDash(-1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                        TagArena.Movement.ControlBinds.CycleDash(1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                        TagArena.Movement.ControlBinds.CyclePunch(-1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                        TagArena.Movement.ControlBinds.CyclePunch(1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Minus) || UnityEngine.Input.GetKeyDown(KeyCode.LeftBracket))
                        Tag.Audio.AudioMaster.CycleVolume(-1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Equals) || UnityEngine.Input.GetKeyDown(KeyCode.RightBracket))
                        Tag.Audio.AudioMaster.CycleVolume(1);
                }
                else
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                        TagArena.Movement.LookSensitivity.Cycle(-1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                        TagArena.Movement.LookSensitivity.Cycle(1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                        Tag.Audio.AudioMaster.CycleMusic(1);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                        Tag.Audio.AudioMaster.CycleMusic(-1);
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
                    LoadBootMenu();
                if (UnityEngine.Input.GetKeyDown(KeyCode.M))
                    Tag.Audio.AudioMaster.ToggleMute();
                if (UnityEngine.Input.GetKeyDown(KeyCode.N))
                    Tag.Audio.AudioMaster.ToggleMusicMute();
                return;
            }
            PollPlaytestModeHotkeys();
            PollResultsKeys();

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
            // Same-frame Results R/Enter must not also rematch after an F-key restart.
            bool fromResults = _phase == MatchPhase.Results;
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("[TagMode] Playtest hotkey F1 -> Hot Potato");
                if (fromResults) _resultsActionTaken = true;
                StartRound(TagModeId.HotPotato);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log("[TagMode] Playtest hotkey F2 -> Least It");
                if (fromResults) _resultsActionTaken = true;
                StartRound(TagModeId.LeastIt);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
            {
                Debug.Log("[TagMode] Playtest hotkey F3 -> Trail Tag");
                if (fromResults) _resultsActionTaken = true;
                StartRound(TagModeId.TrailTag);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F4))
            {
                Debug.Log("[TagMode] Playtest hotkey F4 -> Free play");
                if (fromResults) _resultsActionTaken = true;
                StartRound(TagModeId.FreePlay);
            }
        }

        /// <summary>
        /// One listener for the results card (R rematch, Q/Esc menu). GameFlow skips those
        /// keys while Phase is Results so the arm window and one-shot latch stay single-owner.
        /// Direct Play has no GameFlow, so Q/Esc load Boot.
        /// </summary>
        void PollResultsKeys()
        {
            if (_phase != MatchPhase.Results) return;
            if (_resultsActionTaken) return;
            if (Time.unscaledTime < _resultsInputReadyAt) return;
            var flow = GameFlow.Instance;
            // Ends stay put. Left on Rematch and Right on Menu do not wrap or leak.
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) && _resultsFocus != 0)
            {
                _resultsFocus = 0;
                TagSfx.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) && _resultsFocus != 1)
            {
                _resultsFocus = 1;
                TagSfx.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                ActivateResultsFocus();
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                _resultsActionTaken = true;
                if (flow != null) flow.Rematch();
                else Rematch();
                return;
            }
            // Esc mirrors Q (menu). Same arm/latch as Rematch so the round-end Esc is not sticky.
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                _resultsActionTaken = true;
                if (flow != null) flow.QuitToMenu();
                else LoadBootMenu();
            }
        }

        void ActivateResultsFocus()
        {
            _resultsActionTaken = true;
            if (_resultsFocus == 0)
            {
                var flow = GameFlow.Instance;
                if (flow != null) flow.Rematch();
                else
                {
                    TagSfx.UiConfirm();
                    Rematch();
                }
                return;
            }
            if (GameFlow.Instance != null) GameFlow.Instance.QuitToMenu();
            else LoadBootMenu();
        }

        void LoadBootMenu()
        {
            Time.timeScale = 1f;
            Tag.Audio.TagSfx.UiClick();
            SceneManager.LoadScene("Boot");
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
        /// F1-F4 start a round from the pads, not from wherever bodies stopped.
        /// Slot follows P1/P2/... when the id parses; everyone else fills a free pad.
        /// Yaw is left alone - the chase camera owns it.
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
            _resultsActionTaken = false;
            _resultsFocus = 0;
            // Ignore the same click/key that ended the round (unscaled: results keep timeScale 1).
            _resultsInputReadyAt = Time.unscaledTime + 0.25f;

            var winners = _mode != null ? _mode.GetWinnerIds(_ctx) : new List<string>();
            bool anyWinner = winners != null && winners.Count > 0;
            string names = anyWinner ? string.Join(", ", winners) : "nobody";
            _resultTitle = HeadlineFor(winners);
            _resultDetail = ModeTitle(selectedMode) + (anyWinner ? "\nWinners: " + names : "\nNo winner");
            if (_resultTitle == "YOU LOSE" || _resultTitle == "BOT WINS")
                _resultMessage = $"[{_mode?.Id}] Lose";
            else if (!anyWinner)
                _resultMessage = $"[{_mode?.Id}] No winners";
            else
                _resultMessage = $"[{_mode?.Id}] Winner(s): " + names;
            Debug.Log($"[TagMode] END -- {_resultTitle} {_resultMessage}");

            foreach (var p in players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null) e.SetEmitting(false);
                var punch = p.GetComponent<PunchHitbox>();
                if (punch != null) punch.ForceEnd();
            }

            if (_endedNotified) return;
            _endedNotified = true;
            var flow = GameFlow.Instance != null ? GameFlow.Instance : FindFirstObjectByType<GameFlow>();
            if (flow != null) flow.OnRoundEnded(_resultMessage);
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Time.timeScale = 1f;
            }
        }

        void PollLocalPause()
        {
            // Boot's GameFlow already owns Esc. Direct Play has no menu object.
            if (GameFlow.Instance != null) return;
            if (_phase == MatchPhase.Results || _phase == MatchPhase.Idle) return;
            if (!UnityEngine.Input.GetKeyDown(KeyCode.Escape)) return;
            SetLocalPause(!_localPaused);
        }

        void SetLocalPause(bool paused)
        {
            _localPaused = paused;
            if (!paused) _localHelp = false;
            Time.timeScale = paused ? 0f : 1f;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
            if (paused)
            {
                foreach (var motor in Object.FindObjectsByType<TagArena.Movement.PlayerMotor>(FindObjectsSortMode.None))
                {
                    if (motor == null) continue;
                    var loco = motor.GetComponentInChildren<Tag.Art.DummyLocomotor>();
                    loco?.CancelPunchTelegraph();
                    var punch = motor.GetComponent<PunchHitbox>();
                    if (punch != null) punch.ForceEnd();
                }
            }
            Tag.Audio.TagSfx.UiClick();
        }

        void DrawLocalPause()
        {
            float w = 320f;
            float h = 120f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.38f;
            if (_localHelp)
            {
                GUI.Box(new Rect(x - 40f, y, w + 80f, 340f), "Controls");
                GUI.Label(new Rect(x - 24f, y + 28, w + 48f, 290),
                    TagArena.Movement.ControlBinds.Help +
                    "\n\nH close\nLeft / Right dash    Up / Down punch\n- / + volume    M mute    N music");
                return;
            }
            string extra = _phase == MatchPhase.Countdown ? "\nCountdown frozen" : "";
            GUI.Box(new Rect(x, y, w, h + 72f), "Paused");
            GUI.Label(new Rect(x + 16, y + 28, w - 32, 150),
                "Esc resume\nQ  Boot menu\nH  controls\nM mute (" + Tag.Audio.AudioMaster.Label +
                ")   N music\nUp / Down bed (" + Tag.Audio.AudioMaster.MusicLabel +
                ")\nLeft / Right  " + TagArena.Movement.LookSensitivity.Label + extra);
        }

        static string ModeTitle(TagModeId id)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return "Hot Potato";
                case TagModeId.TrailTag: return "Trail Tag";
                case TagModeId.FreePlay: return "Free play";
                default: return "Least It";
            }
        }

        void OnGUI()
        {
            if (_localPaused)
            {
                DrawLocalPause();
                return;
            }

            DrawItBanner();

            if (_phase == MatchPhase.Countdown)
            {
                DrawCountdownCard();
                return;
            }

            string body = _mode != null ? _mode.GetHud(_ctx) : $"Mode {selectedMode}";
            if (_phase == MatchPhase.Results)
                DrawResultsCard();
            else if (_phase == MatchPhase.PostRound)
            {
                body += $"\nNext round {_phaseTimer:0.0}s";
                DrawPostRoundCard();
            }
            GUI.Box(new Rect(12, Screen.height - 168, 480, 156), "");
            GUI.Label(new Rect(20, Screen.height - 162, 464, 148), body);
        }

        void DrawCountdownCard()
        {
            if (_countStyle == null)
            {
                _countStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 54,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            _countStyle.fontSize = 54;
            _countStyle.alignment = TextAnchor.MiddleCenter;
            float w = 440f;
            float h = 168f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.28f;
            int show = Mathf.Max(1, Mathf.CeilToInt(_phaseTimer));
            GUI.Box(new Rect(x, y, w, h), ModeTitle(selectedMode));
            _countStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 28, w, 70), show.ToString(), _countStyle);
            string hint = _firstCountdownHint
                ? "WASD sprint   Ctrl slide   " + TagArena.Movement.ControlBinds.DashName + " dash\n" +
                  TagArena.Movement.ControlBinds.PunchName + " punch transfers It"
                : "Punch the dummy with the orange hat";
            GUI.Label(new Rect(x + 16, y + 104, w - 32, 48), hint);
        }

        string HeadlineFor(System.Collections.Generic.IReadOnlyList<string> winners)
        {
            bool any = winners != null && winners.Count > 0;
            if (!any) return "DRAW";
            int humans = 0;
            int humanWins = 0;
            foreach (var p in players)
            {
                if (p == null || p.GetComponent<DummyPatrol>() != null) continue;
                if (p.GetComponent<TagArena.Movement.PlayerInputReader>() == null) continue;
                humans++;
                if (winners == null) continue;
                for (int i = 0; i < winners.Count; i++)
                {
                    if (winners[i] == p.PlayerId) humanWins++;
                }
            }
            if (humans == 0) return "ROUND OVER";
            if (humans == 1) return humanWins > 0 ? "YOU WIN" : "YOU LOSE";
            if (humanWins <= 0) return "BOT WINS";
            return "ROUND OVER";
        }

        void DrawResultsCard()
        {
            float w = 520f;
            float h = 248f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.26f;
            if (_countStyle == null)
            {
                _countStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 54,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            string title = string.IsNullOrEmpty(_resultTitle) ? "ROUND OVER" : _resultTitle;
            GUI.Box(new Rect(x, y, w, h), "");
            _countStyle.fontSize = 46;
            _countStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 12, w, 56), title, _countStyle);
            _countStyle.fontSize = 54;
            GUI.Label(new Rect(x + 16, y + 72, w - 32, 96),
                (_resultDetail ?? "") + "\n\nLeft / Right picks. Enter uses it.\nR rematch    Q / Esc menu");
            float bw = 140f;
            float by = y + h - 44f;
            bool canAct = !_resultsActionTaken && Time.unscaledTime >= _resultsInputReadyAt;
            var remRect = new Rect(x + w * 0.5f - bw - 8f, by, bw, 32f);
            var menuRect = new Rect(x + w * 0.5f + 8f, by, bw, 32f);
            if (_resultsFocus == 0)
                GUI.Box(new Rect(remRect.x - 4f, remRect.y - 4f, remRect.width + 8f, remRect.height + 8f), "");
            else
                GUI.Box(new Rect(menuRect.x - 4f, menuRect.y - 4f, menuRect.width + 8f, menuRect.height + 8f), "");
            if (canAct && GUI.Button(remRect, _resultsFocus == 0 ? "> Rematch" : "Rematch"))
            {
                _resultsActionTaken = true;
                var flow = GameFlow.Instance;
                if (flow != null) flow.Rematch();
                else
                {
                    TagSfx.UiConfirm();
                    Rematch();
                }
            }
            if (canAct && GUI.Button(menuRect, _resultsFocus == 1 ? "> Menu" : "Menu"))
            {
                _resultsActionTaken = true;
                if (GameFlow.Instance != null) GameFlow.Instance.QuitToMenu();
                else LoadBootMenu();
            }
        }

        void DrawPostRoundCard()
        {
            float w = 360f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.22f, w, 36f);
            GUI.Box(r, "");
            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 22), $"Next round  {_phaseTimer:0.0}s");
        }

        void DrawItBanner()
        {
            if (_phase != MatchPhase.Playing && _phase != MatchPhase.PostRound) return;
            var it = _ctx.CurrentIt;
            float w = 420f;
            float h = SuddenDeath ? 64f : 46f;
            var r = new Rect((Screen.width - w) * 0.5f, 16f, w, h);
            GUI.Box(r, "");
            string text;
            if (it == null)
                text = "No one is It";
            else if (it.GetComponent<TagArena.Movement.PlayerInputReader>() != null && it.GetComponent<DummyPatrol>() == null)
                text = "YOU ARE IT    punch to dump it";
            else
                text = $"IT: {it.PlayerId}    orange hat    punch to tag";
            if (SuddenDeath)
                text += "\nSD - next trail hit eliminates";
            GUI.Label(new Rect(r.x + 12, r.y + 10, w - 24, h - 16), text);
        }
    }
}



