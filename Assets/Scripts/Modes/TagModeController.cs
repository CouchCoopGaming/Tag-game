using System.Collections.Generic;
using Tag.Core;
using Tag.Gameplay;
using Tag.Trail;
using UnityEngine;
using TagArena.Movement;
using UnityEngine.SceneManagement;
using Tag.Audio;
using Tag.Couch;
using Tag.Onboard;
using Tag.Settings;
using Tag.Front;
using Tag.Practice;
using Tag.MatchStats;

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

        public static void ResetStatics()
        {
            Instance = null;
        }
        public const string PrefsModeKey = "Tag.SelectedMode";
        const string CountdownHintKey = "Tag.CountdownHintSeen";

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
        bool _localLook;
        bool _localAudio;
        int _localPauseFocus;
        int _localControlsFocus;
        int _localLookFocus;
        int _localAudioFocus;
        int _resultsFocus;
        GUIStyle _countStyle;
        GUIStyle _bannerStyle;
        readonly List<ItController> _livingScratch = new List<ItController>(8);
        static readonly List<string> NoWinners = new List<string>(0);
        PlayerTrailEmitter[] _trailCache = System.Array.Empty<PlayerTrailEmitter>();
        PunchHitbox[] _punchCache = System.Array.Empty<PunchHitbox>();
        string _bannerLine = "";
        string _bannerWho = "";
        string _bannerTagged = "";
        int _bannerKey = -1;
        string _clockLine = "";
        int _clockSec = int.MinValue;
        int _transferFrame = -1;
        int _beepSec = -1;
        float _chase;
        float _longestChase;
        string _taggedId = "";
        float _taggedUntil;
        string[] _scoreIds = System.Array.Empty<string>();
        string _scoreCard = "";
        readonly System.Text.StringBuilder _scoreSb = new System.Text.StringBuilder(128);
        float[] _scoreTimes = System.Array.Empty<float>();
        int[] _scoreTags = System.Array.Empty<int>();
        int _scoreCount;
        int _scoreTagsTotal;
        float _scoreLongest;

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
            TagArena.Movement.LookSensitivity.Load();
            Tag.Audio.AudioMaster.Load();
            SettingsRuntime.Load();
            Instance = this;
            if (GetComponent<FrameBudgetOverlay>() == null)
                gameObject.AddComponent<FrameBudgetOverlay>();
            ApplyPersistedMode();
            if (matchTuning == null) matchTuning = MatchTuning.CreateRuntimeDefaults();
            if (leastItTuning == null) leastItTuning = LeastItTuning.CreateRuntimeDefaults();
            if (hotPotatoTuning == null) hotPotatoTuning = HotPotatoTuning.CreateRuntimeDefaults();
            if (trailTagTuning == null) trailTagTuning = TrailTagTuning.CreateRuntimeDefaults();
            _ctx.Eliminate = p => EliminatePlayer(p, "mode");
            _ctx.EnterPostRound = EnterPostRound;
            _ctx.MatchTuning = matchTuning;
            // One controls blurb on the first countdown ever. Rematch and later Boot visits stay short.
            _firstCountdownHint = PlayerPrefs.GetInt(CountdownHintKey, 0) == 0;
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
            EnsurePromptHud();
            if (FindFirstObjectByType<GameFlow>() == null)
            {
                if (!FrontSession.Armed)
                {
                    var go = new GameObject("GameFlow");
                    go.AddComponent<GameFlow>();
                }
                else
                    StartRound();
            }
        }

        public void ApplyRound(float duration, int rounds)
        {
            if (leastItTuning == null) leastItTuning = LeastItTuning.CreateRuntimeDefaults();
            if (duration < 1f) duration = 120f;
            leastItTuning.roundDuration = duration;
            if (rounds < 1) rounds = 1;
            leastItTuning.roundCount = rounds;
        }

        void EnsurePromptHud()
        {
            if (PlayPromptHud.Instance != null) return;
            for (int i = 0; i < players.Count; i++)
            {
                ItController p = players[i];
                if (p == null) continue;
                if (p.GetComponent<DummyPatrol>() != null) continue;
                if (p.GetComponent<PlayerInputReader>() == null) continue;
                if (p.GetComponent<PlayPromptHud>() == null)
                    p.gameObject.AddComponent<PlayPromptHud>();
                return;
            }
        }

        void WarmHudStyles()
        {
            if (_countStyle == null) BootHudStyles();
        }

        void BootHudStyles()
        {
            _countStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 54,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _bannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        static string ModeBody(TagModeId id)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return "Mode Hot Potato";
                case TagModeId.TrailTag: return "Mode Trail Tag";
                case TagModeId.FreePlay: return "Mode Free play";
                case TagModeId.LeastIt: return "Mode Least It";
                default: return "Mode";
            }
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
            RememberPawns();
        }

        public void RegisterPlayer(ItController p)
        {
            if (p != null && !players.Contains(p))
                players.Add(p);
            EnsureTrailEmitters();
            RememberPawns();
        }

        void RememberPawns()
        {
            int n = players.Count;
            if (_trailCache.Length < n)
            {
                _trailCache = new PlayerTrailEmitter[n];
                _punchCache = new PunchHitbox[n];
            }
            for (int i = 0; i < n; i++)
            {
                ItController p = players[i];
                if (p == null)
                {
                    _trailCache[i] = null;
                    _punchCache[i] = null;
                    continue;
                }
                _trailCache[i] = p.GetComponent<PlayerTrailEmitter>();
                _punchCache[i] = p.GetComponent<PunchHitbox>();
            }
        }

        static string ModeIdName(TagModeId id)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return "HotPotato";
                case TagModeId.LeastIt: return "LeastIt";
                case TagModeId.TrailTag: return "TrailTag";
                default: return "FreePlay";
            }
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

        public bool RoundLive =>
            _phase == MatchPhase.Playing || _phase == MatchPhase.PostRound;

        public void StartRound(TagModeId id)
        {
            // Same-frame double R (this controller and GameFlow) must not restart twice.
            if (Time.unscaledTime < _roundStartGuard)
                return;
            MatchLive.OnRoundStarting();
            _roundStartGuard = Time.unscaledTime + 0.05f;
            SetMode(id);
            // Queued pause-map swap: results already showed, now teardown and countdown on the new arena.
            if (Tag.Level.ParkArenaHost.ConsumePending())
                return;
            FrontLive.OnRoundStarted();
            if (_localPaused)
                SetLocalPause(false);
            if (GameFlow.Instance != null)
                GameFlow.Instance.ReturnToPlay();
            // Direct Play has no flow to lock the cursor after the results card.
            ResumeInputGate.LockPlayCursor();
            Time.timeScale = 1f;
            // Same Update as rematch click / R: swallow look+punch (rising-edge gate also covers this).
            foreach (var reader in Object.FindObjectsByType<TagArena.Movement.PlayerInputReader>(FindObjectsSortMode.None))
                reader?.ArmLookPunchGate(2);
            RefreshPlayers();
            _endedNotified = false;
            _resultMessage = "";
            // F1-F4 / rematch leave Results: drop the arm latch so the next card is live.
            _resultsActionTaken = false;
            _resultsFocus = 0;
            _resultsInputReadyAt = 0f;
            _transferFrame = -1;
            _beepSec = -1;
            _chase = 0f;
            _longestChase = 0f;
            _taggedId = "";
            _taggedUntil = 0f;
            _scoreCount = 0;
            _scoreTagsTotal = 0;
            _scoreLongest = 0f;
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

            ClearRoleTagBack();
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
                var loco = p.GetComponentInChildren<Tag.Art.DummyLocomotor>();
                loco?.CancelPunchTelegraph();
            }

            PlacePlayersOnPads();

            _phase = MatchPhase.Countdown;
            SessionRules.RoundPlay = false;
            PadRumble.Silence();
            AudioMix.SetWorldPaused(false);
            CouchPlay.ClearResidue();
            _phaseTimer = Mathf.Max(0.01f, matchTuning.countdownSec);
            Debug.Log($"[TagMode] Countdown {_phaseTimer:0}s -> {selectedMode} ({_ctx.Players.Count}p)");
        }

        public void Rematch()
        {
            FrontLive.BeginMatch();
            StartRound(selectedMode);
        }

        void BeginPlaying()
        {
            if (_phase == MatchPhase.Playing && _ctx.RoundRunning) return;
            if (_firstCountdownHint)
            {
                PlayerPrefs.SetInt(CountdownHintKey, 1);
                PlayerPrefs.Save();
            }
            _firstCountdownHint = false;
            _phase = MatchPhase.Playing;
            _ctx.RoundRunning = true;
            AudioCuePlayer.Ensure()?.RoundStart();
            _mode.OnRoundStart(_ctx);

            if (!PracticeSession.Active && _ctx.CurrentIt == null)
            {
                _livingScratch.Clear();
                var roster = _ctx.Players;
                for (int i = 0; i < roster.Count; i++)
                {
                    ItController p = roster[i];
                    if (p != null && p.IsAlive) _livingScratch.Add(p);
                }
                if (_livingScratch.Count > 0)
                    TransferIt(null, _livingScratch[Random.Range(0, _livingScratch.Count)]);
            }
            EnforceSpawnSafety();
            if (!PracticeSession.Active)
                MatchLive.Arm();
            Debug.Log("[TagMode] Playing " + ModeIdName(selectedMode));
        }

        public int ExportRoster(ItController[] into)
        {
            if (into == null) return 0;
            int n = 0;
            int c = players.Count;
            for (int i = 0; i < c; i++)
            {
                ItController p = players[i];
                if (p == null) continue;
                if (n >= into.Length) break;
                into[n] = p;
                n++;
            }
            return n;
        }

        /// <summary>
        /// Round start. Every living runner must be 20 m from It, and not in sight
        /// inside a 2 s sprint. Pads already satisfy that; this moves anyone who does not.
        /// </summary>
        void EnforceSpawnSafety()
        {
            ItController it = _ctx.CurrentIt;
            if (it == null) return;
            Vector3 ip = it.transform.position;
            foreach (var p in players)
            {
                if (p == null || p == it || !p.IsAlive) continue;
                Vector3 pos = p.transform.position;
                if (Tag.Level.ParkArena.SpawnIsSafe(pos.x, pos.z, ip.x, ip.z))
                    continue;
                Tag.Level.ParkArena.PickRespawn(pos.x, pos.z, ip.x, ip.z, true, out float x, out float y, out float z);
                Vector3 pad = new Vector3(x, y, z);
                var motor = p.GetComponent<PlayerMotor>();
                if (motor != null)
                    motor.Place(pad, "spawn-safe");
                else
                    p.transform.position = pad;
            }
        }

        public void EnterPostRound(float seconds)
        {
            _phase = MatchPhase.PostRound;
            _phaseTimer = Mathf.Max(0.01f, seconds);
            _ctx.RoundRunning = false;
        }

        void Update()
        {
            FrameMeter.AddRound(FrameMeter.RoundOps);
            FrameMeter.AddAudio(FrameMeter.AudioOps);
            SessionRules.RoundPlay = _phase == MatchPhase.Playing;
            PadNav.Poll();
            SettingsRuntime.PollHotkeys();
            // GameFlow owns Comma/N when Boot is in the session. Direct Play has no flow.
            if (GameFlow.Instance == null)
                Tag.Audio.AudioMaster.PollMuteHotkeys();
            PracticeRuntime.Tick();
            PollLocalPause();
            if (_localPaused) return;
            if (!PracticeSession.Active)
                PollPlaytestModeHotkeys();
            PollResultsKeys();
            if (_phase == MatchPhase.Results)
                MatchGhostView.Tick(Time.unscaledDeltaTime);

            float dt = Time.deltaTime;

            if (_phase == MatchPhase.Countdown)
            {
                bool menuUp = GameFlow.Instance != null && GameFlow.Instance.State == GameFlowState.Paused;
                if (!menuUp)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
                        Tag.Level.ParkArenaHost.Request(Tag.Level.ParkArena.Mega, false);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
                        Tag.Level.ParkArenaHost.Request(Tag.Level.ParkArena.Pocket, false);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
                        Tag.Level.ParkArenaHost.Request(Tag.Level.ParkArena.Stack, false);
                }
                _phaseTimer -= dt;
                int sec = Mathf.CeilToInt(Mathf.Max(0f, _phaseTimer));
                if (_phaseTimer > 0f && sec >= 1 && sec != _beepSec)
                {
                    _beepSec = sec;
                    AudioBus.Raise(AudioBus.Hook.CountdownBeep, Vector3.zero);
                }
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
                    if (_mode != null && _mode.ShouldEndRound(_ctx))
                        EndMatch();
                }
                return;
            }

            if (_phase != MatchPhase.Playing || !_ctx.RoundRunning || _mode == null)
                return;

            _ctx.Elapsed += dt;
            if (_ctx.CurrentIt != null) _chase += dt;
            if (!PracticeSession.Active)
                MatchLive.Sample(dt);
            _mode.Tick(_ctx, dt);
            RoundChime.Tick(_ctx.RemainingTime);
            if (_mode.ShouldEndRound(_ctx))
                EndMatch();
        }

        void LateUpdate()
        {
            if (PracticeSession.Active) return;
            if (_phase != MatchPhase.Playing) return;
            MatchLive.CloseFrame();
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
            // Highlight can move during the arm. Activate still waits.
            // Ends stay put. Left on Rematch and Right on Menu do not wrap or leak.
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) NudgeResults(-1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) NudgeResults(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetResultsFocus(0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetResultsFocus(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetResultsFocus(2);
            if (Time.unscaledTime < _resultsInputReadyAt) return;
            if (MatchHighlight.Playing && (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) || UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm || UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back))
            {
                MatchHighlight.Skip();
                MatchGhostView.Release();
                return;
            }
            var flow = GameFlow.Instance;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
            {
                ActivateResultsFocus();
                return;
            }
            // Re-check latch: Enter above may have already rematched this frame.
            if (_resultsActionTaken) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                _resultsActionTaken = true;
                if (flow != null) flow.Rematch();
                else Rematch();
                return;
            }
            // Esc mirrors Q (menu). Same arm/latch as Rematch so the round-end Esc is not sticky.
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
            {
                _resultsActionTaken = true;
                if (flow != null) flow.QuitToMenu();
                else LoadBootMenu();
            }
        }

        void NudgeResults(int dir)
        {
            int next = _resultsFocus + dir;
            if (next < 0) next = 0;
            if (next > 2) next = 2;
            SetResultsFocus(next);
        }

        void SetResultsFocus(int index)
        {
            if (_resultsFocus == index) return;
            _resultsFocus = index;
            TagSfx.UiMove();
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
            if (_resultsFocus == 1)
            {
                TagSfx.UiConfirm();
                if (GameFlow.Instance != null) GameFlow.Instance.OpenSetup();
                else LoadBootMenu();
                return;
            }
            TagSfx.UiConfirm();
            if (GameFlow.Instance != null) GameFlow.Instance.QuitToMenu();
            else LoadBootMenu();
        }

        void LoadBootMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            TagSfx.UiClick();
            AudioCuePlayer.Ensure()?.StopMusic();
            PlayerPrefs.SetInt("Tag.BootSeen", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Boot");
        }

        public void OnSuccessfulPunch(ItController puncher, ItController target)
        {
            if (_phase != MatchPhase.Playing || !_ctx.RoundRunning) return;
            if (puncher == null || target == null) return;
            if (!puncher.IsIt || puncher.IsEliminated) return;
            if (!target.IsAlive || !target.CanBeTagged) return;
            if (Time.frameCount == _transferFrame) return;
            _transferFrame = Time.frameCount;
            TransferIt(puncher, target);
            _mode?.OnPunchTransfer(_ctx, puncher, target);
            if (_mode != null && _mode.ShouldEndRound(_ctx))
                EndMatch();
        }

        void ClearRoleTagBack()
        {
            TagRole[] roles = Object.FindObjectsByType<TagRole>(FindObjectsSortMode.None);
            for (int i = 0; i < roles.Length; i++)
            {
                if (roles[i] != null) roles[i].ClearTagBackImmunity();
            }
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null) players[i].ClearTagBackImmunity();
            }
        }

        public void TransferIt(ItController from, ItController to)
        {
            if (PracticeSession.Active)
            {
                PracticeSession.ItAssigned = false;
                return;
            }
            LastFromId = from != null ? from.PlayerId : "";
            float tagBackSeconds = TagBackSeconds(from, to);
            if (from != null)
            {
                from.NoteTagLanded();
                if (to != null)
                    MatchLive.NoteTag(from, to);
                if (_chase > _longestChase) _longestChase = _chase;
                _chase = 0f;
                if (IsLocalHuman(from) && to != null)
                {
                    _taggedId = string.IsNullOrEmpty(to.PlayerId) ? to.name : to.PlayerId;
                    _taggedUntil = Time.time + RoundFlow.TaggedBannerSeconds;
                }
            }
            if (to != null && IsLocalHuman(to))
                _taggedUntil = 0f;
            if (from != null)
            {
                Tag.Settings.PadRumble.PulseId(from.gameObject.GetInstanceID(), Tag.Settings.PadRumble.Tagged);
                from.SetIt(false);
            }
            if (to != null && to.IsAlive)
            {
                PlayerMotor victimMotor = to.GetComponent<PlayerMotor>();
                if (victimMotor != null) victimMotor.ReleaseCarriers();
                to.SetIt(true);
                _ctx.CurrentIt = to;
                LastToId = to.PlayerId;
                Debug.Log($"[TagMode] It -> {to.PlayerId}");
                // A is safe from B only. A fresh It (from == null) does not open a window.
                if (from != null)
                    from.BeginTagBackImmunity(to, tagBackSeconds);
            }
            else
            {
                _ctx.CurrentIt = null;
                LastToId = "";
            }
        }

        static float TagBackSeconds(ItController from, ItController to)
        {
            PunchHitbox box = null;
            if (from != null) box = from.GetComponent<PunchHitbox>();
            if (box == null && to != null) box = to.GetComponent<PunchHitbox>();
            return box != null ? box.TagBackImmunitySeconds : TagBackImmunity.DefaultSeconds;
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
                var motor = p.GetComponent<PlayerMotor>();
                if (motor != null)
                {
                    motor.Place(pad, "round-start");
                    continue;
                }
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
            if (_chase > _longestChase) _longestChase = _chase;
            SnapshotScores();
            if (FrontLive.KeepGoing(_scoreIds, _scoreTimes, _scoreTags, _scoreCount, _scoreLongest))
            {
                MatchLive.Hold();
                StartRound(selectedMode);
                return;
            }
            MatchLive.Seal();
            _ctx.RoundRunning = false;
            if (_ctx.RemainingTime < 0f) _ctx.RemainingTime = 0f;
            _phase = MatchPhase.Results;
            SessionRules.RoundPlay = false;
            PadRumble.Silence();
            AudioMix.SetWorldPaused(false);
            ClearRoleTagBack();
            _resultsActionTaken = false;
            _resultsFocus = 0;
            // A pause subpanel must not stay over the results card or eat Left/Right.
            _localHelp = false;
            _localLook = false;
            _localAudio = false;
            SettingsMenuUi.Close();
            if (_localPaused)
            {
                _localPaused = false;
                Time.timeScale = 1f;
            }
            // Ignore the same click/key that ended the round (unscaled: results keep timeScale 1).
            _resultsInputReadyAt = Time.unscaledTime + 0.25f;

            var winners = _mode != null ? _mode.GetWinnerIds(_ctx) : NoWinners;
            bool anyWinner = winners != null && winners.Count > 0;
            string names = anyWinner ? string.Join(", ", winners) : "nobody";
            _resultTitle = HeadlineFor(winners);
            _resultDetail = ModeTitle(selectedMode) + (anyWinner ? "\nWinners: " + names : "\nNo winner");
            string idName = ModeIdName(selectedMode);
            if (_resultTitle == "YOU LOSE" || _resultTitle == "BOT WINS")
                _resultMessage = "[" + idName + "] Lose";
            else if (!anyWinner)
                _resultMessage = "[" + idName + "] No winners";
            else
                _resultMessage = "[" + idName + "] Winner(s): " + names;
            Debug.Log("[TagMode] END -- " + _resultTitle + " " + _resultMessage);
            RebuildScoreCard();

            int n = players.Count;
            for (int i = 0; i < n; i++)
            {
                if (players[i] == null) continue;
                if (i < _trailCache.Length && _trailCache[i] != null) _trailCache[i].SetEmitting(false);
                if (i < _punchCache.Length && _punchCache[i] != null) _punchCache[i].ForceEnd();
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
                AudioBus.RaiseRoundEnd(_resultMessage);
            }
        }

        static bool CouchPause()
        {
            if (CouchPlay.Humans < 2) return false;
            for (int i = 0; i < CouchPlay.Max; i++)
            {
                if (!CouchPlay.HumanAt(i)) continue;
                int device = CouchPlay.DeviceOf(i);
                ActionBinds binds = CouchPlay.BindsFor(device);
                if (binds != null && BindSampler.PressedDevice(binds, PlayAction.Pause, device))
                    return true;
            }
            return false;
        }

        void PollLocalPause()
        {
            // Boot's GameFlow already owns Esc. Direct Play has no menu object.
            if (GameFlow.Instance != null) return;
            if (_phase == MatchPhase.Results || _phase == MatchPhase.Idle) return;
            if (!_localPaused)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Start || CouchPause())
                    SetLocalPause(true);
                return;
            }
            // Esc inside Controls / Look / Audio closes that panel and stays paused.
            if (PollLocalPauseOverlay()) return;
            PollLocalPauseRoot();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back || PadNav.Start)
                SetLocalPause(false);
        }

        void SetLocalPause(bool paused)
        {
            _localPaused = paused;
            _localHelp = false;
            _localLook = false;
            _localAudio = false;
            SettingsMenuUi.Close();
            if (paused) _localPauseFocus = 0;
            Time.timeScale = paused ? 0f : 1f;
            if (paused)
            {
                PadRumble.Silence();
                AudioMix.SetWorldPaused(true);
                if (PadNav.StartDevice > 0)
                    CouchPlay.OpenPauseFrom(PadNav.StartDevice);
            }
            else
                AudioMix.SetWorldPaused(false);
            if (paused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } else ResumeInputGate.LockPlayCursor();
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
            else
            {
                foreach (var reader in Object.FindObjectsByType<TagArena.Movement.PlayerInputReader>(FindObjectsSortMode.None))
                    reader?.ArmLookPunchGate(2);
            }
            TagSfx.UiClick();
        }

        bool PollLocalPauseOverlay()
        {
            if (SettingsMenuUi.Blocks) { SettingsMenuUi.Poll(); return true; }
            if (_localHelp) { PollLocalControls(); return true; }
            if (_localLook) { PollLocalLook(); return true; }
            if (_localAudio) { PollLocalAudio(); return true; }
            return false;
        }

        void PollLocalControls()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.H) || PadNav.Back)
            {
                _localHelp = false;
                TagSfx.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up) NudgeLocal(ref _localControlsFocus, -1, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down) NudgeLocal(ref _localControlsFocus, 1, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetLocal(ref _localControlsFocus, 0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetLocal(ref _localControlsFocus, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetLocal(ref _localControlsFocus, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) StepLocalControls(-1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) StepLocalControls(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
            {
                if (_localControlsFocus >= 2) { _localHelp = false; TagSfx.UiClick(); }
                else StepLocalControls(1);
            }
        }

        void StepLocalControls(int dir)
        {
            if (_localControlsFocus == 0) TagArena.Movement.ControlBinds.CycleDash(dir);
            else if (_localControlsFocus == 1) TagArena.Movement.ControlBinds.CyclePunch(dir);
        }

        void PollLocalLook()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
            {
                _localLook = false;
                TagSfx.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up) NudgeLocal(ref _localLookFocus, -1, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down) NudgeLocal(ref _localLookFocus, 1, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetLocal(ref _localLookFocus, 0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetLocal(ref _localLookFocus, 1);
            if ((UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) && _localLookFocus == 0)
                TagArena.Movement.LookSensitivity.Cycle(-1);
            if ((UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) && _localLookFocus == 0)
                TagArena.Movement.LookSensitivity.Cycle(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
            {
                if (_localLookFocus >= 1) { _localLook = false; TagSfx.UiClick(); }
                else TagArena.Movement.LookSensitivity.Cycle(1);
            }
        }

        void PollLocalAudio()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadNav.Back)
            {
                _localAudio = false;
                TagSfx.UiClick();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || PadNav.Up) NudgeLocal(ref _localAudioFocus, -1, 4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || PadNav.Down) NudgeLocal(ref _localAudioFocus, 1, 4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetLocal(ref _localAudioFocus, 0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetLocal(ref _localAudioFocus, 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetLocal(ref _localAudioFocus, 2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetLocal(ref _localAudioFocus, 3);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) SetLocal(ref _localAudioFocus, 4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left) StepLocalAudio(-1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right) StepLocalAudio(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
            {
                if (_localAudioFocus >= 4) { _localAudio = false; TagSfx.UiClick(); }
                else StepLocalAudio(1);
            }
        }

        void StepLocalAudio(int dir)
        {
            switch (_localAudioFocus)
            {
                case 0: Tag.Audio.AudioMaster.CycleVolume(dir); break;
                case 1: Tag.Audio.AudioMaster.CycleMusic(dir); break;
                case 2: Tag.Audio.AudioMaster.ToggleMute(); break;
                case 3: Tag.Audio.AudioMaster.ToggleMusicMute(); break;
            }
        }

        static void NudgeLocal(ref int cursor, int dir, int maxInclusive)
        {
            int next = Mathf.Clamp(cursor + dir, 0, maxInclusive);
            if (next == cursor) return;
            cursor = next;
            TagSfx.UiClick();
        }

        static void SetLocal(ref int cursor, int index)
        {
            if (cursor == index) return;
            cursor = index;
            TagSfx.UiClick();
        }

        void PollLocalPauseRoot()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                _localHelp = true;
                _localControlsFocus = 0;
                TagSfx.UiClick();
                return;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || PadNav.Left || PadNav.Up) NudgeLocalPause(-1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || PadNav.Right || PadNav.Down) NudgeLocalPause(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SetLocalPauseFocus(0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SetLocalPauseFocus(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SetLocalPauseFocus(2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SetLocalPauseFocus(3);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) SetLocalPauseFocus(4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha6)) SetLocalPauseFocus(5);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha7)) SetLocalPauseFocus(6);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha8)) SetLocalPauseFocus(7);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha9)) SetLocalPauseFocus(8);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) || PadNav.Confirm)
                ActivateLocalPause();
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
                LoadBootMenu();
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                Tag.Audio.AudioMaster.CycleMusic(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                Tag.Audio.AudioMaster.CycleMusic(-1);
        }

        void NudgeLocalPause(int dir)
        {
            int next = Mathf.Clamp(_localPauseFocus + dir, 0, MenuGraph.PauseRows - 1);
            if (next == _localPauseFocus) return;
            _localPauseFocus = next;
            TagSfx.UiClick();
        }

        void SetLocalPauseFocus(int index)
        {
            if (_localPauseFocus == index) return;
            _localPauseFocus = index;
            TagSfx.UiClick();
        }

        void ActivateLocalPause()
        {
            switch (_localPauseFocus)
            {
                case 1: _localHelp = true; _localControlsFocus = 0; TagSfx.UiClick(); break;
                case 2: _localLook = true; _localLookFocus = 0; TagSfx.UiClick(); break;
                case 3: _localAudio = true; _localAudioFocus = 0; TagSfx.UiClick(); break;
                case 4: LoadBootMenu(); break;
                case 5: SettingsMenuUi.Open(SettingsMenuUi.Panel.Settings); TagSfx.UiClick(); break;
                case 6: SettingsMenuUi.Open(SettingsMenuUi.Panel.Rebind); TagSfx.UiClick(); break;
                case 7: SettingsMenuUi.Open(SettingsMenuUi.Panel.Arena); TagSfx.UiClick(); break;
                case 8: SettingsMenuUi.Open(SettingsMenuUi.Panel.HowTo); TagSfx.UiClick(); break;
                default: SetLocalPause(false); break;
            }
        }

        void DrawLocalPause()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            if (_localHelp)
            {
                DrawLocalControls(cx, cy);
                return;
            }
            if (_localLook)
            {
                DrawLocalLook(cx, cy);
                return;
            }
            if (_localAudio)
            {
                DrawLocalAudio(cx, cy);
                return;
            }

            if (SettingsMenuUi.Blocks)
            {
                SettingsMenuUi.Draw();
                return;
            }
            string extra = _phase == MatchPhase.Countdown ? "\nCountdown frozen" : "";
            GUI.Box(new Rect(cx - 170, cy - 204, 340, 500), "Paused");
            if (LocalPauseButton(cx, cy - 176, 0, "Resume")) SetLocalPause(false);
            if (LocalPauseButton(cx, cy - 146, 1, "Controls"))
            {
                _localHelp = true;
                _localControlsFocus = 0;
                TagSfx.UiClick();
            }
            if (LocalPauseButton(cx, cy - 116, 2, "Look sensitivity"))
            {
                _localLook = true;
                _localLookFocus = 0;
                TagSfx.UiClick();
            }
            if (LocalPauseButton(cx, cy - 86, 3, "Audio"))
            {
                _localAudio = true;
                _localAudioFocus = 0;
                TagSfx.UiClick();
            }
            if (LocalPauseButton(cx, cy - 56, 4, "Quit to title")) LoadBootMenu();
            if (LocalPauseButton(cx, cy - 26, 5, "Settings"))
                SettingsMenuUi.Open(SettingsMenuUi.Panel.Settings);
            if (LocalPauseButton(cx, cy + 4, 6, "Rebind"))
                SettingsMenuUi.Open(SettingsMenuUi.Panel.Rebind);
            if (LocalPauseButton(cx, cy + 34, 7, "Arena"))
                SettingsMenuUi.Open(SettingsMenuUi.Panel.Arena);
            if (LocalPauseButton(cx, cy + 64, 8, "How to play"))
                SettingsMenuUi.Open(SettingsMenuUi.Panel.HowTo);
            GUI.Label(new Rect(cx - 160, cy + 100, 320, 96),
                "Left / Right or stick    1-9    Enter / South\nEsc or East resume    Start pauses\nComma mute    M minimap    N music" + extra);
        }

        void DrawLocalControls(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 240, cy - 210, 480, 430), "Controls");
            GUI.Label(new Rect(cx - 220, cy - 180, 440, 200), TagArena.Movement.ControlBinds.Help);
            LocalSubRow(cx, cy + 28, 0, ref _localControlsFocus, "Dash   " + TagArena.Movement.ControlBinds.DashName);
            if (LocalSideStep(cx, cy + 60))
            {
                _localControlsFocus = 0;
                TagArena.Movement.ControlBinds.CycleDash(_localSideDir);
            }
            LocalSubRow(cx, cy + 96, 1, ref _localControlsFocus, "Punch  " + TagArena.Movement.ControlBinds.PunchName);
            if (LocalSideStep(cx, cy + 128))
            {
                _localControlsFocus = 1;
                TagArena.Movement.ControlBinds.CyclePunch(_localSideDir);
            }
            if (LocalSubRow(cx, cy + 164, 2, ref _localControlsFocus, "Back"))
            {
                _localHelp = false;
                TagSfx.UiClick();
            }
            GUI.Label(new Rect(cx - 220, cy + 198, 440, 36),
                "Up / Down picks. Left / Right steps it. 1-3 highlight.\nEnter uses the row. Esc or H back.");
        }

        void DrawLocalLook(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 200, cy - 110, 400, 230), "Look sensitivity");
            LocalSubRow(cx, cy - 70, 0, ref _localLookFocus, TagArena.Movement.LookSensitivity.Label);
            if (LocalSideStep(cx, cy - 36))
            {
                _localLookFocus = 0;
                TagArena.Movement.LookSensitivity.Cycle(_localSideDir);
            }
            if (LocalSubRow(cx, cy + 8, 1, ref _localLookFocus, "Back"))
            {
                _localLook = false;
                TagSfx.UiClick();
            }
            GUI.Label(new Rect(cx - 180, cy + 44, 360, 48),
                "Up / Down picks. Left / Right steps look.\n1-2 highlight. Enter uses the row. Esc back.");
        }

        void DrawLocalAudio(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 210, cy - 170, 420, 360), "Audio");
            LocalSubRow(cx, cy - 130, 0, ref _localAudioFocus, "SFX   " + Tag.Audio.AudioMaster.Label);
            if (LocalSideStep(cx, cy - 96))
            {
                _localAudioFocus = 0;
                Tag.Audio.AudioMaster.CycleVolume(_localSideDir);
            }
            LocalSubRow(cx, cy - 60, 1, ref _localAudioFocus, "Music   " + Tag.Audio.AudioMaster.MusicLabel);
            if (LocalSideStep(cx, cy - 26))
            {
                _localAudioFocus = 1;
                Tag.Audio.AudioMaster.CycleMusic(_localSideDir);
            }
            string muteLabel = Tag.Audio.AudioMaster.Muted ? "Unmute (Comma)" : "Mute (Comma)";
            if (LocalSubRow(cx, cy + 12, 2, ref _localAudioFocus, muteLabel))
                Tag.Audio.AudioMaster.ToggleMute();
            string musicLabel = Tag.Audio.AudioMaster.MusicMuted ? "Music on (N)" : "Music off (N)";
            if (LocalSubRow(cx, cy + 46, 3, ref _localAudioFocus, musicLabel))
                Tag.Audio.AudioMaster.ToggleMusicMute();
            if (LocalSubRow(cx, cy + 80, 4, ref _localAudioFocus, "Back"))
            {
                _localAudio = false;
                TagSfx.UiClick();
            }
            GUI.Label(new Rect(cx - 190, cy + 116, 380, 48),
                "Up / Down picks. Left / Right steps the row.\n1-5 highlight. Comma mute. N music. Enter uses it. Esc back.");
        }

        int _localSideDir;

        bool LocalSideStep(float cx, float y)
        {
            _localSideDir = 1;
            if (MenuClick.Button(new Rect(cx - 150, y, 80, 26), "<")) { _localSideDir = -1; return true; }
            if (MenuClick.Button(new Rect(cx + 70, y, 80, 26), ">")) { _localSideDir = 1; return true; }
            return false;
        }

        static bool LocalSubRow(float cx, float y, int index, ref int cursor, string label)
        {
            var r = new Rect(cx - 170, y, 340, 28);
            bool sel = cursor == index;
            if (sel) GUI.Box(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), "");
            if (!MenuClick.Button(r, (sel ? "> " : "  ") + label)) return false;
            cursor = index;
            return true;
        }

        bool LocalPauseButton(float cx, float y, int index, string label)
        {
            var r = new Rect(cx - 70, y, 140, 28);
            bool sel = _localPauseFocus == index;
            if (sel) GUI.Box(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), "");
            if (!MenuClick.Button(r, (sel ? "> " : "  ") + label)) return false;
            _localPauseFocus = index;
            return true;
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
            WarmHudStyles();
            MinimapHud.Draw();
            if (_localPaused)
            {
                DrawLocalPause();
                return;
            }

            PracticeHud.Draw();
            DrawItBanner();

            if (_phase == MatchPhase.Countdown)
            {
                DrawCountdownCard();
                return;
            }

            string body = _mode != null ? _mode.GetHud(_ctx) : ModeBody(selectedMode);
            if (_phase == MatchPhase.Results)
                DrawResultsCard();
            else if (_phase == MatchPhase.PostRound)
            {
                body += "\nNext round " + HudDigits.TenthSeconds(_phaseTimer);
                DrawPostRoundCard();
            }
            GUI.Box(new Rect(12, Screen.height - 168, 480, 156), "");
            GUI.Label(new Rect(20, Screen.height - 162, 464, 148), body);
        }

        void DrawCountdownCard()
        {
            if (_countStyle == null) return;
            float hud = GameSettings.Current != null ? GameSettings.Current.HudScale : 1f;
            _countStyle.fontSize = (int)(54f * hud);
            _countStyle.alignment = TextAnchor.MiddleCenter;
            VerbHudLayout.Box card = VerbHudLayout.Picker(Screen.width, Screen.height);
            float w = card.W;
            float h = card.H;
            float x = card.X;
            float y = card.Y;
            int show = Mathf.Max(1, Mathf.CeilToInt(_phaseTimer));
            GUI.Box(new Rect(x, y, w, h), ModeTitle(selectedMode));
            float flash = GameSettings.Current != null ? GameSettings.Current.CountdownFlash(_phaseTimer) : 1f;
            _countStyle.normal.textColor = new Color(flash, flash, flash, 1f);
            GUI.Label(new Rect(x, y + 28, w, 70), HudDigits.Whole0(show), _countStyle);
            string hint = _firstCountdownHint
                ? "WASD move   Shift sprint   Ctrl slide   " + TagArena.Movement.ControlBinds.DashName + " dash\n" +
                  TagArena.Movement.ControlBinds.PunchName + " or E tags"
                : "Punch the dummy with the orange hat";
            GUI.Label(new Rect(x + 16, y + 104, w - 32, 48), hint);
            GUI.Label(new Rect(x + 16, y + 156, w - 32, 44),
                "1 Mega Park   2 Pocket Park   3 Stack Yard\nnow " + Tag.Level.ParkArena.DisplayName);
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
            float hud = GameSettings.Current != null ? GameSettings.Current.HudScale : 1f;
            bool story = MatchBook.Sealed && MatchBook.Count > 0;
            int seats = story ? MatchBook.Count : 0;
            int cols = seats < 1 ? 1 : (seats < 4 ? seats : 4);
            float w = 560f;
            float h = 300f + _scoreCount * 22f;
            if (story)
            {
                w = cols * 260f * hud + 48f;
                if (w < 640f) w = 640f;
                if (w > Screen.width - 24f) w = Screen.width - 24f;
                h = Screen.height - 32f;
                if (h > 840f) h = 840f;
                if (h < 420f) h = 420f;
            }
            float x = (Screen.width - w) * 0.5f;
            float y = story ? Mathf.Max(12f, (Screen.height - h) * 0.5f) : Mathf.Max(24f, Screen.height * 0.18f);
            if (_countStyle == null) return;
            string title = string.IsNullOrEmpty(_resultTitle) ? "ROUND OVER" : _resultTitle;
            GUI.Box(new Rect(x, y, w, h), "");
            _countStyle.fontSize = story ? MatchBook.CouchFont(46, hud) : 46;
            _countStyle.alignment = TextAnchor.MiddleCenter;
            _countStyle.normal.textColor = Color.white;
            if (story)
                GUI.Label(new Rect(x, y + 8, w, MatchBook.CouchFont(52, hud)), title, _countStyle);
            else
                GUI.Label(new Rect(x, y + 12, w, 56), title, _countStyle);
            if (story)
            {
                int scoreSize = MatchBook.CouchFont(20, hud);
                _countStyle.fontSize = scoreSize;
                _countStyle.alignment = TextAnchor.UpperLeft;
                int lines = 8 + (seats < 4 ? seats : 4);
                float scoreH = scoreSize * lines * 0.9f;
                float top = y + MatchBook.CouchFont(52, hud) + 4f;
                GUI.Label(new Rect(x + 16, top, w - 32, scoreH), _scoreCard, _countStyle);
                _countStyle.alignment = TextAnchor.MiddleCenter;
                MatchResults.Paint(x, top + scoreH, w, h - (top - y) - scoreH - 56f, hud);
            }
            else
            {
                _countStyle.fontSize = 54;
                GUI.Label(new Rect(x + 16, y + 68, w - 32, h - 120f), _scoreCard);
            }
            float bw = 128f;
            float gap = 8f;
            float by = y + h - 44f;
            float x0 = x + (w - (bw * 3f + gap * 2f)) * 0.5f;
            bool canAct = !_resultsActionTaken && Time.unscaledTime >= _resultsInputReadyAt;
            var remRect = new Rect(x0, by, bw, 32f);
            var setupRect = new Rect(x0 + bw + gap, by, bw, 32f);
            var titleRect = new Rect(x0 + (bw + gap) * 2f, by, bw, 32f);
            Rect focus = _resultsFocus == 1 ? setupRect : (_resultsFocus == 2 ? titleRect : remRect);
            GUI.Box(new Rect(focus.x - 4f, focus.y - 4f, focus.width + 8f, focus.height + 8f), "");
            // Mouse only, so Enter does not also fire whichever IMGUI control is focused.
            // A click during the arm moves the highlight and does not activate.
            if (MenuClick.Button(remRect, _resultsFocus == 0 ? "> Rematch" : "Rematch"))
            {
                _resultsFocus = 0;
                if (!canAct) return;
                if (MatchHighlight.Playing)
                {
                    MatchHighlight.Skip();
                    MatchGhostView.Release();
                    return;
                }
                _resultsActionTaken = true;
                var flow = GameFlow.Instance;
                if (flow != null) flow.Rematch();
                else
                {
                    TagSfx.UiConfirm();
                    Rematch();
                }
            }
            if (MenuClick.Button(setupRect, _resultsFocus == 1 ? "> Change setup" : "Change setup"))
            {
                _resultsFocus = 1;
                if (!canAct) return;
                if (MatchHighlight.Playing)
                {
                    MatchHighlight.Skip();
                    MatchGhostView.Release();
                    return;
                }
                _resultsActionTaken = true;
                TagSfx.UiConfirm();
                if (GameFlow.Instance != null) GameFlow.Instance.OpenSetup();
                else LoadBootMenu();
            }
            if (MenuClick.Button(titleRect, _resultsFocus == 2 ? "> Title" : "Title"))
            {
                _resultsFocus = 2;
                if (!canAct) return;
                if (MatchHighlight.Playing)
                {
                    MatchHighlight.Skip();
                    MatchGhostView.Release();
                    return;
                }
                _resultsActionTaken = true;
                TagSfx.UiConfirm();
                if (GameFlow.Instance != null) GameFlow.Instance.QuitToMenu();
                else LoadBootMenu();
            }
        }

        void DrawPostRoundCard()
        {
            float w = 360f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.22f, w, 36f);
            GUI.Box(r, "");
            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 22), "Next round  " + HudDigits.TenthSeconds(_phaseTimer));
        }

        static bool IsLocalHuman(ItController p)
        {
            if (p == null) return false;
            if (p.GetComponent<DummyPatrol>() != null) return false;
            return p.GetComponent<TagArena.Movement.PlayerInputReader>() != null;
        }

        void SnapshotScores()
        {
            int n = 0;
            for (int i = 0; i < players.Count; i++)
                if (players[i] != null) n++;
            _scoreIds = new string[n];
            _scoreTimes = new float[n];
            _scoreTags = new int[n];
            _scoreCount = 0;
            _scoreTagsTotal = 0;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null) continue;
                _scoreIds[_scoreCount] = string.IsNullOrEmpty(p.PlayerId) ? p.name : p.PlayerId;
                _scoreTimes[_scoreCount] = p.TimeAsIt;
                _scoreTags[_scoreCount] = p.TagsLanded;
                _scoreTagsTotal += p.TagsLanded;
                _scoreCount++;
            }
            _scoreLongest = _longestChase;
        }

        void RebuildScoreCard()
        {
            string match = FrontLive.Card();
            if (!string.IsNullOrEmpty(match))
            {
                _scoreCard = match;
                return;
            }
            _scoreSb.Clear();
            _scoreSb.Append(_resultDetail ?? "");
            _scoreSb.Append("\n\nTime as It");
            for (int i = 0; i < _scoreCount; i++)
            {
                _scoreSb.Append("\n").Append(_scoreIds[i]).Append("  ");
                _scoreSb.Append(HudDigits.Tenth0(_scoreTimes[i])).Append("s");
            }
            _scoreSb.Append("\nTags  ").Append(_scoreTagsTotal);
            _scoreSb.Append("\nLongest chase  ").Append(HudDigits.Tenth0(_scoreLongest)).Append("s");
            _scoreSb.Append("\n\n1-2 or Left / Right picks. Enter / Space uses it.\nR rematch    Q / Esc menu");
            _scoreCard = _scoreSb.ToString();
        }

        void DrawItBanner()
        {
            if (_phase != MatchPhase.Playing && _phase != MatchPhase.PostRound) return;
            var it = _ctx.CurrentIt;
            bool showClock = selectedMode != TagModeId.FreePlay;
            VerbHudLayout.Box plate = VerbHudLayout.Banner(Screen.width);
            float w = plate.W;
            float h = plate.H;
            var r = new Rect(plate.X, plate.Y, w, h);
            GUI.Box(r, "");
            if (_bannerStyle == null) return;
            _bannerStyle.fontSize = Screen.height >= 1000 ? 26 : 20;
            _bannerStyle.normal.textColor = Color.white;
            bool localIsIt = IsLocalHuman(it);
            bool taggedFlash = Time.time < _taggedUntil && !string.IsNullOrEmpty(_taggedId);
            string who = it != null ? it.PlayerId : "";
            int key = (localIsIt ? 1 : 0) + (taggedFlash ? 2 : 0) + (SuddenDeath ? 4 : 0);
            if (key != _bannerKey || who != _bannerWho || _taggedId != _bannerTagged)
            {
                _bannerKey = key;
                _bannerWho = who;
                _bannerTagged = _taggedId;
                _bannerLine = RoundFlow.BannerLine(localIsIt, taggedFlash, _taggedId, who);
                if (SuddenDeath)
                    _bannerLine = _bannerLine + "\nSD - next trail hit eliminates";
            }
            float labelH = showClock ? h - 30f : h - 8f;
            GUI.Label(new Rect(r.x + 8, r.y + 4, w - 16, labelH), _bannerLine, _bannerStyle);
            if (!showClock) return;
            float left = _ctx.RemainingTime;
            int sec = left <= 0f ? 0 : (int)left;
            if (left > sec) sec++;
            if (sec != _clockSec)
            {
                _clockSec = sec;
                _clockLine = RoundFlow.Clock(_ctx.RemainingTime);
            }
            _bannerStyle.fontSize = Screen.height >= 1000 ? 18 : 16;
            _bannerStyle.normal.textColor = new Color(0.75f, 0.86f, 1f, 1f);
            GUI.Label(new Rect(r.x + 8, r.y + h - 30f, w - 16, 26f), _clockLine, _bannerStyle);
            _bannerStyle.normal.textColor = Color.white;
        }
    }
}



