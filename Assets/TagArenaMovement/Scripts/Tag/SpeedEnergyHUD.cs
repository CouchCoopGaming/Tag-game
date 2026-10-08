using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Tag.Core;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using Tag.Trail;

namespace TagArena.Movement
{
    /// <summary>
    /// Cave-man OnGUI: speed, move state, jet fuel, ski on/off + P0 controls cheat-sheet
    /// + nearest mega-park zone + It / mode / Hot Potato fuse / Least It times (lowest time wins)
    /// + bearing/distance to CurrentIt when you are not It
    /// + bearing/distance to nearest non-It when you ARE It (Prey)
    /// + brief YOU'RE IT / YOU'RE FREE OnGUI flash on local It handoff
    /// + tag-back window on the It line (safe / no tag-back) while it is open
    /// + Trail Tag soft near-miss TRAIL! edge pulse when near a foreign ribbon
    /// + brief OUT! / TRAIL HIT OnGUI flash when local IsAlive drops (trail eliminate)
    /// (reads TagModeController, falls back to ItController scan).
    /// Local human only (wired by LocalPlayerSpawner for index 0).
    /// </summary>
    public class SpeedEnergyHUD : MonoBehaviour
    {
        public PlayerMotor motor;
        ItController _self;
        string _controlLine;
        string _punchSeen;
        string _dashSeen;
        int _kphKey = int.MinValue;
        int _verbKey = int.MinValue;
        string _kphLine;
        int _jetKey = int.MinValue;
        bool _jettingSeen;
        bool _skiSeen;
        string _jetLine;
        int _dashKey = int.MinValue;
        string _dashToken;
        bool _skiOn;
        string _dashLine;
        string _zoneName;
        string _zoneLine;
        string _jetDashToken;
        string _jetDashLine;
        int _nearKey = int.MinValue;
        string _nearLine;
        int _compassMeters = int.MinValue;
        int _compassDir = -1;
        string _compassWho;
        string _compassLine;
        ItController _preyBest;
        float _preyBestSq;
        ItController _preyHunter;
        Vector3 _preyFrom;
        readonly List<ItController> _living = new List<ItController>(8);
        readonly StringBuilder _standings = new StringBuilder(64);
        static readonly System.Comparison<ItController> ByItTime = CompareItTime;
        GUIStyle _big;
        GUIStyle _small;
        GUIStyle _keys;
        GUIStyle _status;
        GUIStyle _flash;

        // Brief center flash when local gains/loses It (SetIt / TransferIt / punch).
        const float ItFlashSec = 1.0f; // handoff beat: a hair longer so YOU ARE IT / FREE reads
        bool _itFlashPrimed;
        bool _prevLocalIsIt;
        float _itFlashUntil;
        bool _itFlashGained;

        // Trail Tag near-miss (foreign ribbon) - soft edge warn before eliminate contact.
        const float TrailNearMissWarnM = 6.6f; // earlier soft edge so TRAIL! reads before contact (AI peels ~9.4 m)
        readonly List<TrailSegment> _trailNearScratch = new List<TrailSegment>();
        float _trailNearDist = float.MaxValue;
        bool _trailNearActive;

        // Trail Tag eliminate - brief center flash when local IsAlive drops (trail hit).
        const float TrailOutFlashSec = 1.05f; // match It/Mode/SD flash beat so OUT! / TRAIL HIT reads
        bool _aliveFlashPrimed;
        bool _prevLocalAlive = true;
        float _trailOutFlashUntil;
        // Trail Tag sudden-death rising edge (mode line alone is easy to miss).
        const float SdFlashSec = 1.0f; // match It/Mode flash beat so SD reads on rising edge
        bool _sdFlashPrimed;
        bool _prevSuddenDeath;
        float _sdFlashUntil;

        // Brief center flash when F1/F2/F3 (or menu) changes SelectedMode.
        const float ModeFlashSec = 1.0f; // match It handoff beat so F1-F4 mode name reads
        bool _modeFlashPrimed;
        TagModeId _prevMode;
        float _modeFlashUntil;
        string _modeFlashLabel = "";

        // Labels mirror PlayerInputReader defaults (skiKey/jetKey/crouchKey/punchKey/lungeKey + hard-coded alts).
        const string Controls =
            "WASD move\n" +
            "Shift ski\n" +
            "RMB jet (off)\n" +
            "Space jump\n" +
            "Ctrl/C slide (hold + speed)\n" +
            "Ctrl in air = fast fall\n" +
            "LMB/E punch\n" +
            "Q/Alt air dash\n" +
            "MMB lunge (It, ground)\n" +
            "F1 Hot Potato\n" +
            "F2 Least It\n" +
            "F3 Trail Tag\n" +
            "F4 Free play\n" +
            "Esc pause\n" +
            "Comma mute   M minimap   N music";

        // Flash full Least-It standings briefly every few seconds.
        const float AllStandingsShowSec = 4.4f; // slightly longer Least It board read
        const float AllStandingsCycleSec = 8.7f; // slightly slower board flip

        // Compass close-range pulse (Prey hunt / It flee), flat meters.
        const float CompassPulseDistM = 14f; // earlier It-hunt compass pulse

        // Hot Potato fuse HUD warn fallback (matches ItMarker / DummyPatrol when tuning missing).
        const float HotPotatoWarnSecFallback = 11.5f; // earlier fuse urgency fallback

        // Relative to camera: forward = N, right = E (hunt direction, not world north).
        static readonly string[] Compass8 = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        void Start()
        {
            if (motor != null) _self = motor.GetComponent<ItController>();
            if (_self == null) _self = GetComponent<ItController>();
        }

        void WarmStyles()
        {
            if (_big == null) BootStyles();
        }

        void BootStyles()
        {
            _big = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            _keys = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            _status = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            _flash = new GUIStyle(GUI.skin.label)
            {
                fontSize = 64,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _big.normal.textColor = Color.white;
            _small.normal.textColor = new Color(0.85f, 0.9f, 1f);
            _keys.normal.textColor = new Color(0.75f, 0.82f, 0.95f);
            _status.normal.textColor = new Color(1f, 0.92f, 0.55f);
        }

        static int CompareItTime(ItController a, ItController b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return a.TimeAsIt.CompareTo(b.TimeAsIt);
        }

        string ControlLine()
        {
            string punch = ControlBinds.PunchName;
            string dash = ControlBinds.DashName;
            if (_controlLine != null && punch == _punchSeen && dash == _dashSeen)
                return _controlLine;
            _punchSeen = punch;
            _dashSeen = dash;
            _controlLine = Controls
                .Replace("LMB/E punch", punch + "/E punch")
                .Replace("Q/Alt air dash", dash + "/Alt air dash");
            return _controlLine;
        }

        void OnGUI()
        {
            if (Tag.Ui.Hud.MatchHud.Active) return;
            if (!motor) return;
            WarmStyles();
            if (_big == null) return;

            float hs = motor.HorizSpeed;
            int verbKey = motor.IsAirDashing ? 64 : (int)motor.State;
            int kphKey = (int)(hs * 3.6f + 0.5f);
            if (kphKey < 0) kphKey = 0;
            if (kphKey != _kphKey || verbKey != _verbKey || _kphLine == null)
            {
                _kphKey = kphKey;
                _verbKey = verbKey;
                _kphLine = HudDigits.Kph(hs) + " km/h   " + LocoVerb(motor);
            }
            GUI.Label(new Rect(24, 16, 560, 36), _kphLine, _big);
            DrawMuteChip();

            bool jetOn = motor.cfg != null && motor.cfg.enableJet;
            float maxE = 100f;
            if (motor.cfg != null)
                maxE = motor.cfg.jetEnergyMax + 0.001f;
            string ski = motor.Skiing ? "SKI ON" : "ski off";
            if (jetOn)
            {
                float e = Mathf.Clamp01(motor.Energy / maxE);
                GUI.Box(new Rect(24, 56, 240, 20), GUIContent.none);
                GUI.Box(new Rect(24, 56, 240 * e, 20), GUIContent.none);
                int jetKey = (int)(motor.Energy + 0.5f);
                if (jetKey != _jetKey || _jetLine == null || motor.Jetting != _jettingSeen || motor.Skiing != _skiSeen)
                {
                    _jetKey = jetKey;
                    _jettingSeen = motor.Jetting;
                    _skiSeen = motor.Skiing;
                    string jet = motor.Jetting ? "JETTING" : "jet";
                    _jetLine = "JET " + HudDigits.Whole0(motor.Energy) + "/" + HudDigits.Whole0(maxE) + "  " + jet + "   " + ski;
                }
                GUI.Label(new Rect(24, 80, 480, 26), _jetLine, _small);
            }
            else
            {
                float cdMax = motor.cfg != null ? Mathf.Max(0.01f, motor.cfg.airDashCooldown) : 30f;
                float rem = motor.AirDashCooldownRemaining;
                // Burst reads full. A cooling bar grows from empty. Track stays dark so the fill is visible.
                bool bursting = motor.IsAirDashing;
                float ready = bursting ? 1f : 1f - Mathf.Clamp01(rem / cdMax);
                Color prev = GUI.color;
                GUI.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);
                GUI.Box(new Rect(24, 56, 240, 20), GUIContent.none);
                GUI.color = bursting || rem <= 0.05f
                    ? new Color(0.45f, 1f, 0.72f, 1f)
                    : new Color(0.35f, 0.82f, 1f, 1f);
                GUI.Box(new Rect(24, 56, 240 * ready, 20), GUIContent.none);
                GUI.color = prev;
                // Active burst wins the label; otherwise ready / CD (no second DASH line below).
                string dashTok = motor.IsAirDashing
                    ? "DASH!"
                    : (rem <= 0.05f ? "DASH ready" : FormatDashCd(rem));
                if (dashTok != _dashToken || motor.Skiing != _skiOn || _dashLine == null)
                {
                    _dashToken = dashTok;
                    _skiOn = motor.Skiing;
                    _dashLine = (motor.IsAirDashing || rem <= 0.05f ? dashTok : "DASH " + dashTok) + "   " + ski;
                }
                GUI.Label(new Rect(24, 80, 480, 26), _dashLine, _small);
            }

            // When jet is on, the primary row is JET - keep a dedicated dash CD / active line.
            // When jet is off, dash already owns the primary row; skip the duplicate.
            float y = 102f;
            if (jetOn)
            {
                float dashCd = motor.AirDashCooldownRemaining;
                string jetDashTok = dashCd > 0.05f
                    ? FormatDashCd(dashCd)
                    : (motor.IsAirDashing ? "DASH!" : "DASH ready");
                if (jetDashTok != _jetDashToken || _jetDashLine == null)
                {
                    _jetDashToken = jetDashTok;
                    _jetDashLine = dashCd > 0.05f ? "DASH CD " + jetDashTok : jetDashTok;
                }
                GUI.Label(new Rect(24, 102, 480, 22), _jetDashLine, _small);
                y = 124f;
            }
            string zone = ZoneNameMarkers.GetNearestZoneName(motor.transform.position);
            if (zone != _zoneName || _zoneLine == null)
            {
                _zoneName = zone;
                _zoneLine = "Zone: " + (zone ?? "");
            }
            GUI.Label(new Rect(24, y, 480, 22), _zoneLine, _small);
            y += 24f;

            if (motor.SuperGlideT >= 0f)
            {
                GUI.Label(new Rect(24, y, 280, 28), "GLIDE WINDOW", _big);
                y += 32f;
            }

            GUI.Label(new Rect(24, y, 300, 300), ControlLine(), _keys);
            y += 292f;

            DrawMatchStatus(y);
            DrawFuseBanner();
            DrawWaitingBanner();
            TickSuddenDeathFlash();
            DrawSuddenDeathFlash();
            DrawItHandoffFlash();
            DrawTrailNearMissWarn();
            DrawTrailEliminateFlash();
            DrawModeChangeFlash();
        }

        void Update()
        {
            TickItHandoffFlash();
            TickTrailNearMiss();
            TickTrailEliminateFlash();
            TickModeChangeFlash();
        }

        /// <summary>
        /// Watch local ItController.IsIt - same flag SetIt / TransferIt / PunchHitbox mutate.
        /// Skip first sample so spawn / HUD enable does not false-flash.
        /// </summary>
        void TickItHandoffFlash()
        {
            var modes = TagModeController.Instance;
            // Rematch / menu leave Playing: clear edge so spawn-as-It and first handoff flash again.
            if (modes == null || modes.Phase != MatchPhase.Playing)
            {
                _prevLocalIsIt = false;
                _itFlashPrimed = true;
                return;
            }
            bool localIsIt = ResolveLocalIsIt();
            if (!_itFlashPrimed)
            {
                _prevLocalIsIt = localIsIt;
                _itFlashPrimed = true;
                return;
            }
            if (localIsIt == _prevLocalIsIt)
                return;
            _itFlashGained = localIsIt;
            _itFlashUntil = Time.time + ItFlashSec;
            _prevLocalIsIt = localIsIt;
        }

        bool ResolveLocalIsIt()
        {
            ItController self = _self;
            if (self != null)
                return self.IsAlive && self.IsIt;
            return false;
        }

        /// <summary>
        /// Center flash ~1.0s: YOU'RE IT / YOU'RE FREE (TAG! handoff beat). Rematch re-arms.
        /// </summary>
        void DrawItHandoffFlash()
        {
            float rem = _itFlashUntil - Time.time;
            if (rem <= 0f) return;

            if (_flash == null) return;
            _flash.fontSize = 64;

            float elapsed = ItFlashSec - rem;
            float fadeIn = 0.08f;
            float fadeOut = 0.18f;
            float a;
            if (elapsed < fadeIn)
                a = elapsed / fadeIn;
            else if (rem < fadeOut)
                a = rem / fadeOut;
            else
                a = 1f;

            string msg = _itFlashGained ? "YOU'RE IT" : "YOU'RE FREE";
            Color tint = _itFlashGained
                ? new Color(1f, 0.35f, 0.28f, a)
                : new Color(0.45f, 0.95f, 1f, a);
            _flash.normal.textColor = tint;

            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            float w = 720f;
            float h = 90f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.28f, w, h);
            Matrix4x4 prevM = GUI.matrix;
            float peak = 1f - Mathf.Abs((elapsed / ItFlashSec) - 0.35f) * 0.5f;
            float scale = Mathf.Lerp(0.92f, 1.08f, Mathf.Clamp01(peak));
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), r.center);
            GUI.Label(r, msg, _flash);
            if (_status != null)
            {
                var sub = new Rect(r.x, r.yMax - 4f, r.width, 28f);
                Color prevStatus = _status.normal.textColor;
                _status.normal.textColor = new Color(1f, 0.92f, 0.55f, a * 0.9f);
                var prevAlign = _status.alignment;
                _status.alignment = TextAnchor.MiddleCenter;
                GUI.Label(sub, HandoffSubtitle(), _status);
                _status.alignment = prevAlign;
                _status.normal.textColor = prevStatus;
            }
            GUI.matrix = prevM;
            GUI.color = prev;
        }

        /// <summary>
        /// Trail eliminate locks the motor and the OUT flash lasts under a second.
        /// Keep a waiting line until the round ends so the freeze is explained.
        /// </summary>
        void DrawWaitingBanner()
        {
            ItController self = _self;
            if (self == null || self.IsAlive) return;
            var modes = TagModeController.Instance;
            if (modes == null || modes.Phase != MatchPhase.Playing) return;

            float w = 420f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.62f, w, 52f);
            GUI.Box(r, "");
            if (_status != null)
            {
                var prevA = _status.alignment;
                var prevC = GUI.color;
                _status.alignment = TextAnchor.MiddleCenter;
                GUI.color = new Color(1f, 0.58f, 0.32f, 1f); // warm so OUT waiting reads vs other status
                GUI.Label(r, "OUT    waiting for the round", _status);
                GUI.color = prevC;
                _status.alignment = prevA;
            }
            else
            {
                var prevC = GUI.color;
                GUI.color = new Color(1f, 0.58f, 0.32f, 1f);
                GUI.Label(r, "OUT    waiting for the round");
                GUI.color = prevC;
            }
        }

        string HandoffSubtitle()
        {
            var modes = TagModeController.Instance;
            if (modes == null) return "TAG!";
            if (_itFlashGained)
            {
                string from = modes.LastFromId;
                return string.IsNullOrEmpty(from) ? "TAG!" : "from " + from;
            }
            string to = modes.LastToId;
            return string.IsNullOrEmpty(to) ? "TAG!" : to + " is It";
        }

        /// <summary>
        /// Top-center fuse so a runner sees the potato even when they are not It.
        /// Stays below the It banner (y=16, h=78). Hidden while Remaining is 0 (countdown).
        /// </summary>
        void DrawFuseBanner()
        {
            var modes = TagModeController.Instance;
            if (modes == null || modes.SelectedMode != TagModeId.HotPotato || modes.Phase != MatchPhase.Playing)
                return;
            float remain = modes.Remaining;
            float urgency = HotPotatoFuseUrgency(modes);
            if (remain <= 0f || urgency <= 0.02f) return;

            float w = 280f;
            var r = new Rect((Screen.width - w) * 0.5f, 102f, w, 36f);
            DrawFuseUrgencyLabel(r, "FUSE  " + HudDigits.Tenth0(remain), urgency);
        }

        /// <summary>
        /// Trail Tag only: closest foreign TrailSegment via CopyActive + ClosestPointOnSegment
        /// (same helpers DummyPatrol trail avoid uses). Warn under TrailNearMissWarnM - soft
        /// readability cue before BoxCollider eliminate; does not change hit rules.
        /// </summary>
        void TickTrailNearMiss()
        {
            _trailNearActive = false;
            _trailNearDist = float.MaxValue;

            var modes = TagModeController.Instance;
            if (modes == null || modes.SelectedMode != TagModeId.TrailTag)
                return;
            if (motor == null) return;

            ItController self = _self;
            if (self == null || !self.IsAlive || self.IsEliminated)
                return;

            Vector3 pos = motor.transform.position;
            float warn = TrailNearMissWarnM;
            float warnSq = warn * warn;
            float bestSq = warnSq;

            TrailSegment.CopyActive(_trailNearScratch);
            for (int i = 0; i < _trailNearScratch.Count; i++)
            {
                var seg = _trailNearScratch[i];
                if (seg == null) continue;
                // Foreign only - own ribbon is self-grace / separate fairness case.
                if (seg.Owner != null && seg.Owner == self) continue;
                if (seg.Owner == null && !string.IsNullOrEmpty(seg.OwnerId)
                    && seg.OwnerId == self.PlayerId) continue;

                Vector3 closest = seg.ClosestPointOnSegment(pos);
                Vector3 delta = pos - closest;
                delta.y = 0f;
                float dsq = delta.sqrMagnitude;
                if (dsq >= bestSq) continue;
                bestSq = dsq;
                _trailNearDist = Mathf.Sqrt(dsq);
                _trailNearActive = true;
            }
        }

        /// <summary>
        /// Soft screen-edge pulse + TRAIL! label when near a foreign ribbon.
        /// Urgency rises toward contact; cave-man OnGUI only (local human HUD).
        /// </summary>
        void DrawTrailNearMissWarn()
        {
            if (!_trailNearActive || _trailNearDist >= TrailNearMissWarnM)
                return;

            float urgency = 1f - Mathf.Clamp01(_trailNearDist / TrailNearMissWarnM);
            float hz = Mathf.Lerp(2.5f, 8f, urgency);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * hz * Mathf.PI * 2f);
            float pulse = Mathf.Lerp(0.3f, 1f, wave);
            float a = Mathf.Lerp(0.12f, 0.42f, urgency * pulse);

            Color prev = GUI.color;
            // Cyan edge - hot warn as you close in (same family as It flee compass).
            Color calm = new Color(0.2f, 0.95f, 1f, a);
            Color hot = new Color(1f, 0.35f, 0.45f, a);
            GUI.color = Color.Lerp(calm, hot, urgency * pulse);

            float edge = Mathf.Lerp(10f, 28f, urgency * pulse);
            float w = Screen.width;
            float h = Screen.height;
            GUI.DrawTexture(new Rect(0f, 0f, w, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, h - edge, w, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 0f, edge, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(w - edge, 0f, edge, h), Texture2D.whiteTexture);

            if (_flash == null) return;
            _flash.fontSize = 64;
            float textA = Mathf.Lerp(0.45f, 1f, urgency * pulse);
            _flash.normal.textColor = Color.Lerp(
                new Color(0.45f, 0.95f, 1f, textA),
                new Color(1f, 0.4f, 0.5f, textA),
                urgency * pulse);

            float tw = 420f;
            float th = 70f;
            var r = new Rect((w - tw) * 0.5f, h * 0.12f, tw, th);
            Matrix4x4 prevM = GUI.matrix;
            float scale = 1f + urgency * 0.12f * pulse;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), r.center);
            GUI.Label(r, "TRAIL!", _flash);
            if (_status != null)
            {
                Color prevStatus = _status.normal.textColor;
                var prevAlign = _status.alignment;
                _status.alignment = TextAnchor.MiddleCenter;
                _status.normal.textColor = new Color(1f, 0.92f, 0.55f, textA * 0.85f);
                int nearKey = (int)(_trailNearDist * 10f + 0.5f);
                if (nearKey != _nearKey || _nearLine == null)
                {
                    _nearKey = nearKey;
                    _nearLine = HudDigits.Tenth0(_trailNearDist) + "m";
                }
                GUI.Label(new Rect(r.x, r.yMax - 6f, r.width, 24f), _nearLine, _status);
                _status.alignment = prevAlign;
                _status.normal.textColor = prevStatus;
            }
            GUI.matrix = prevM;
            GUI.color = prev;
        }


        /// <summary>
        /// Watch local ItController.IsAlive - same flag TrailSegment hit / EliminatePlayer mutate.
        /// Trail Tag only. Skip first sample so spawn / HUD enable does not false-flash.
        /// Does not invent trail rules; mirrors IsAlive edge after existing eliminate path.
        /// </summary>
        void TickTrailEliminateFlash()
        {
            var modes = TagModeController.Instance;
            bool trailMode = modes != null && modes.SelectedMode == TagModeId.TrailTag;

            bool alive = ResolveLocalAlive();
            if (!_aliveFlashPrimed)
            {
                _prevLocalAlive = alive;
                _aliveFlashPrimed = true;
                return;
            }

            // Rising edge of eliminate: was alive, now dead, while Trail Tag is selected.
            if (trailMode && _prevLocalAlive && !alive)
                _trailOutFlashUntil = Time.time + TrailOutFlashSec;

            _prevLocalAlive = alive;
        }

        bool ResolveLocalAlive()
        {
            ItController self = _self;
            if (self != null)
                return self.IsAlive;
            return true;
        }

        /// <summary>
        /// Center flash ~1.0s on trail eliminate: OUT! + TRAIL HIT.
        /// Distinct from TAG handoff (YOU'RE IT / FREE) and near-miss TRAIL! edge pulse -
        /// lower screen, hot red, no edge bars.
        /// </summary>
        void DrawTrailEliminateFlash()
        {
            float rem = _trailOutFlashUntil - Time.time;
            if (rem <= 0f) return;

            if (_flash == null) return;
            _flash.fontSize = 64;

            float elapsed = TrailOutFlashSec - rem;
            float fadeIn = 0.06f;
            float fadeOut = 0.2f;
            float a;
            if (elapsed < fadeIn)
                a = elapsed / fadeIn;
            else if (rem < fadeOut)
                a = rem / fadeOut;
            else
                a = 1f;

            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);

            // Soft red wash behind text (distinct from near-miss cyan/hot edge bars).
            float washA = a * 0.22f;
            GUI.color = new Color(0.85f, 0.08f, 0.12f, washA);
            GUI.DrawTexture(new Rect(0f, Screen.height * 0.38f, Screen.width, Screen.height * 0.28f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, a);

            _flash.normal.textColor = new Color(1f, 0.2f, 0.18f, a);

            float w = 720f;
            float h = 90f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.42f, w, h);
            Matrix4x4 prevM = GUI.matrix;
            float peak = 1f - Mathf.Abs((elapsed / TrailOutFlashSec) - 0.3f) * 0.55f;
            float scale = Mathf.Lerp(0.9f, 1.12f, Mathf.Clamp01(peak));
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), r.center);
            GUI.Label(r, "OUT!", _flash);
            if (_status != null)
            {
                var sub = new Rect(r.x, r.yMax - 4f, r.width, 28f);
                Color prevStatus = _status.normal.textColor;
                _status.normal.textColor = new Color(1f, 0.75f, 0.35f, a * 0.95f);
                var prevAlign = _status.alignment;
                _status.alignment = TextAnchor.MiddleCenter;
                GUI.Label(sub, "TRAIL HIT", _status);
                _status.alignment = prevAlign;
                _status.normal.textColor = prevStatus;
            }
            GUI.matrix = prevM;
            GUI.color = prev;
        }

        void DrawMuteChip()
        {
            bool muted = Tag.Audio.AudioMaster.Muted;
            bool musicOff = Tag.Audio.AudioMaster.MusicMuted;
            if (!muted && !musicOff) return;
            // Show both when M+N are on so the chips do not hide each other.
            string chip = muted && musicOff ? "MUTED  (,)   MUSIC OFF  (N)"
                : muted ? "MUTED  (,)"
                : "MUSIC OFF  (N)";
            var prev = GUI.color;
            GUI.color = muted ? new Color(1f, 0.45f, 0.4f) : new Color(1f, 0.82f, 0.45f);
            VerbHudLayout.Box chipBox = VerbHudLayout.Mute(Screen.width, Screen.height);
            var align = _big.alignment;
            _big.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(chipBox.X, chipBox.Y, chipBox.W, chipBox.H), chip, _big);
            _big.alignment = align;
            GUI.color = prev;
        }

        void DrawMatchStatus(float y)
        {
            string modeName = "";
            string itLabel = "";
            string fuseLine = null;
            ItController it = null;

            var modes = TagModeController.Instance;
            if (modes != null)
            {
                modeName = FriendlyModeName(modes.SelectedMode) + " | " + PhaseLabel(modes.Phase);
                if (modes.SelectedMode == TagModeId.LeastIt && modes.Phase == MatchPhase.Playing && modes.Remaining > 0f)
                    modeName += "  " + HudDigits.WholeSeconds(modes.Remaining) + "  lowest wins";
                if (modes.SelectedMode == TagModeId.TrailTag && modes.Phase == MatchPhase.Playing && modes.SuddenDeath)
                    modeName += "  SUDDEN DEATH";
                if (modes.SelectedMode == TagModeId.FreePlay && modes.Phase == MatchPhase.Playing)
                    modeName += "  no timer";
                it = modes.CurrentIt;
                if (it == null)
                    it = ScanItControllers();
                itLabel = FormatIt(it) + TagBackHudSuffix();

                if (modes.SelectedMode == TagModeId.HotPotato)
                {
                    float rem = modes.Remaining;
                    fuseLine = rem > 0f
                        ? "Fuse " + HudDigits.TenthSeconds(rem)
                        : "Fuse ";
                }
            }
            else
            {
                it = ScanItControllers();
                itLabel = FormatIt(it) + TagBackHudSuffix();
                modeName = "default";
            }

            // Hot Potato fuse pulses for everyone once Remaining is inside warnSec.
            // (Previously only the local It saw the pulse, so runners missed the pop.)

            float fuseUrgency = (modes != null && modes.SelectedMode == TagModeId.HotPotato && modes.Phase == MatchPhase.Playing)
                ? HotPotatoFuseUrgency(modes)
                : 0f;
            bool pulseFuse = fuseUrgency > 0.01f;

            GUI.Label(new Rect(24, y, 640, 22), "Mode " + modeName, _status);

            y += 22f;

            if (pulseFuse)
            {
                DrawFuseUrgencyLabel(new Rect(24, y, 480, 22), "It: " + itLabel, fuseUrgency);
                y += 22f;
            }
            else
            {
                GUI.Label(new Rect(24, y, 480, 22), "It: " + itLabel, _status);
                y += 22f;
            }

            // Compass: hunt It when not It; hunt nearest prey when you are It.
            if (it != null && IsLocalPlayer(it))
                y = DrawPreyBearing(modes, y);
            else if (it != null)
                y = DrawItBearing(it, y);

            if (fuseLine != null)
            {
                string line = pulseFuse ? fuseLine + " !!" : fuseLine;
                if (pulseFuse)
                    DrawFuseUrgencyLabel(new Rect(24, y, 520, 22), line, fuseUrgency);
                else
                    GUI.Label(new Rect(24, y, 480, 22), line, _status);
                y += 22f;
            }

            if (modes != null && modes.SelectedMode == TagModeId.LeastIt)
                y = DrawLeastItTimes(modes, y);
        }

        /// <summary>
        /// 0 = calm / not in warn window; 1 = fuse about to pop (Remaining near 0).
        /// Matches ItMarker / DummyPatrol: Remaining vs HotPotatoTuning.warnSec (fallback 10s).
        /// </summary>
        /// <summary>Whole seconds above 10s (30s air-dash CD); one decimal under that.</summary>
        static string FormatDashCd(float rem)
        {
            return HudDigits.DashCd(rem);
        }

        static float HotPotatoFuseUrgency(TagModeController modes)
        {
            if (modes == null || modes.SelectedMode != TagModeId.HotPotato)
                return 0f;
            float remain = modes.Remaining;
            if (remain <= 0f)
                return 0f;
            float warnSec = HotPotatoWarnSecFallback;
            var tuning = modes.HotPotatoTuningAsset;
            if (tuning != null && tuning.warnSec > 0f)
                warnSec = tuning.warnSec;
            float warn = Mathf.Max(0.5f, warnSec);
            return 1f - Mathf.Clamp01(remain / warn);
        }

        /// <summary>
        /// Pulse fuse/It status text: scale + amber-hot tint, faster as urgency rises.
        /// </summary>
        void DrawFuseUrgencyLabel(Rect r, string text, float urgency)
        {
            Color prevColor = GUI.color;
            Matrix4x4 prevMatrix = GUI.matrix;

            float hz = Mathf.Lerp(4f, 11f, urgency);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * hz * Mathf.PI * 2f);
            float pulse = Mathf.Lerp(0.35f, 1f, wave);

            // Status amber - hot-potato warn (magenta-orange), same family as It flee compass.
            Color calm = new Color(1f, 0.92f, 0.55f, 1f);
            Color hot = new Color(1f, 0.35f, 0.55f, 1f);
            Color tint = Color.Lerp(calm, hot, urgency * pulse);
            tint.a = Mathf.Lerp(0.6f, 1f, Mathf.Lerp(0.7f, 1f, urgency) * pulse);
            GUI.color = tint;

            float scale = 1f + urgency * 0.22f * pulse;
            var pivot = new Vector2(r.x, r.y + r.height * 0.5f);
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);

            GUI.Label(r, text, _status);
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        /// <summary>
        /// Cave-man compass toward It: camera-relative 8-way + flat meters.
        /// Pulses under ~12m (cyan/white - hot-potato warn) so close flee reads distinct from Prey hunt.
        /// </summary>
        float DrawItBearing(ItController it, float y)
        {
            return DrawCompassBearing("It", it.transform.position, y, pulseClose: true, itWarnTint: true);
        }

        /// <summary>
        /// Mirror of It compass when local is It: nearest alive non-It (Prey).
        /// Pulses under ~12m so close chase reads better.
        /// </summary>
        float DrawPreyBearing(TagModeController modes, float y)
        {
            var prey = FindNearestPrey(modes);
            if (prey == null)
            {
                GUI.Label(new Rect(24, y, 520, 22), "Prey  none", _status);
                return y + 22f;
            }
            return DrawCompassBearing("Prey", prey.transform.position, y, pulseClose: true, itWarnTint: false);
        }

        ItController FindNearestPrey(TagModeController modes)
        {
            _preyFrom = motor.transform.position;
            _preyBest = null;
            _preyBestSq = float.MaxValue;
            _preyHunter = _self;

            if (modes != null && modes.PlayersForHud != null)
            {
                var list = modes.PlayersForHud;
                for (int i = 0; i < list.Count; i++)
                    ConsiderPrey(list[i]);
            }

            // Empty / no-candidate PlayersForHud: same scan as when modes is null.
            if (_preyBest == null)
            {
                var all = Object.FindObjectsByType<ItController>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                    ConsiderPrey(all[i]);
            }

            return _preyBest;
        }

        void ConsiderPrey(ItController p)
        {
            if (p == null || !p.IsAlive || p.IsIt) return;
            if (IsLocalPlayer(p)) return;
            if (_preyHunter != null && p.BlocksTagBackFrom(_preyHunter)) return;
            Vector3 d = p.transform.position - _preyFrom;
            d.y = 0f;
            float sq = d.sqrMagnitude;
            if (sq >= _preyBestSq) return;
            _preyBestSq = sq;
            _preyBest = p;
        }

        /// <summary>
        /// Shared cave-man compass: camera-relative 8-way + flat meters.
        /// Label examples: "It ->  SW  18m" / "Prey ->  SW  18m".
        /// When pulseClose and dist &lt; CompassPulseDistM: scale/alpha/color urgency pulse.
        /// itWarnTint: cyan/white - magenta-orange (flee); else amber - red (hunt).
        /// </summary>
        float DrawCompassBearing(string label, Vector3 to, float y, bool pulseClose, bool itWarnTint = false)
        {
            Vector3 from = motor.transform.position;
            Vector3 flat = to - from;
            flat.y = 0f;
            float dist = flat.magnitude;
            if (dist < 0.05f)
            {
                GUI.Label(new Rect(24, y, 520, 22), label + "  HERE", _status);
                return y + 22f;
            }

            float worldDeg = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

            // Relative to camera yaw (fallback: motor forward). Forward = N for hunt.
            Vector3 face = motor.transform.forward;
            var cam = Camera.main;
            if (cam != null)
                face = cam.transform.forward;
            face.y = 0f;
            if (face.sqrMagnitude < 0.0001f)
                face = Vector3.forward;
            face.Normalize();

            float faceDeg = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;
            float rel = Mathf.DeltaAngle(faceDeg, worldDeg); // -180..180, + = target to the right
            float rel360 = rel < 0f ? rel + 360f : rel;
            int relIdx = Mathf.RoundToInt(rel360 / 45f) & 7;

            int meters = (int)(dist + 0.5f);
            if (meters < 0) meters = 0;
            if (_compassLine == null || meters != _compassMeters || relIdx != _compassDir || label != _compassWho)
            {
                _compassMeters = meters;
                _compassDir = relIdx;
                _compassWho = label;
                _compassLine = label + " ->  " + Compass8[relIdx] + "  " + HudDigits.Meters0(dist);
            }
            string line = _compassLine;

            Color prevColor = GUI.color;
            Matrix4x4 prevMatrix = GUI.matrix;
            if (pulseClose && dist < CompassPulseDistM)
            {
                // 0 at threshold, 1 at contact - closer = hotter / bigger / faster pulse.
                float urgency = 1f - Mathf.Clamp01(dist / CompassPulseDistM);
                float hz = Mathf.Lerp(3.5f, 9f, urgency);
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * hz * Mathf.PI * 2f);
                float pulse = Mathf.Lerp(0.35f, 1f, wave);

                Color calm;
                Color hot;
                if (itWarnTint)
                {
                    // Fleeing It: cyan/white - hot-potato warn (magenta-orange), distinct from Prey hunt.
                    calm = new Color(0.55f, 0.95f, 1f, 1f);
                    hot = new Color(1f, 0.35f, 0.55f, 1f);
                }
                else
                {
                    // Hunting Prey: amber - red.
                    calm = new Color(1f, 0.92f, 0.55f, 1f);
                    hot = new Color(1f, 0.28f, 0.12f, 1f);
                }
                Color tint = Color.Lerp(calm, hot, urgency * pulse);
                tint.a = Mathf.Lerp(0.55f, 1f, Mathf.Lerp(0.65f, 1f, urgency) * pulse);
                GUI.color = tint;

                float scale = 1f + urgency * 0.18f * pulse;
                var pivot = new Vector2(24f, y + 11f);
                GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
            }

            GUI.Label(new Rect(24, y, 520, 22), line, _status);
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
            return y + 22f;
        }

        float DrawLeastItTimes(TagModeController modes, float y)
        {
            ItController self = null;
            ItController leader = null;
            float best = float.MaxValue;
            float worst = float.MinValue;
            _living.Clear();

            var list = modes.PlayersForHud;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    if (p == null || !p.IsAlive) continue;
                    _living.Add(p);
                    if (IsLocalPlayer(p))
                        self = p;
                    if (p.TimeAsIt < best)
                    {
                        best = p.TimeAsIt;
                        leader = p;
                    }
                    if (p.TimeAsIt > worst)
                        worst = p.TimeAsIt;
                }
            }

            if (self == null && _self != null && _self.IsAlive)
                self = _self;

            float youT = self != null ? self.TimeAsIt : 0f;
            // Soft standings cue: lowest It-time is winning (mint); more It-time is behind (coral).
            bool leading = false;
            bool lagging = false;
            if (self != null && _living.Count > 0)
            {
                const float eps = 0.05f;
                leading = youT <= best + eps;
                if (!leading && _living.Count >= 2)
                {
                    if (youT >= worst - eps)
                        lagging = true;
                    else if (_living.Count >= 3)
                    {
                        // Near-highest: 2nd-from-bottom by TimeAsIt rank.
                        _living.Sort(ByItTime);
                        int idx = _living.IndexOf(self);
                        if (idx >= _living.Count - 2)
                            lagging = true;
                    }
                }
            }

            Color prev = GUI.color;
            string youTag = " as It";
            if (leading)
            {
                GUI.color = new Color(0.45f, 1f, 0.7f, 1f);
                youTag = "  WINNING (least)";
            }
            else if (lagging)
            {
                GUI.color = new Color(1f, 0.55f, 0.42f, 1f);
                youTag = "  BEHIND (more It)";
            }
            GUI.Label(new Rect(24, y, 640, 22),
                "You " + HudDigits.TenthSeconds(youT) + youTag, _status);
            GUI.color = prev;
            y += 22f;

            if (leader != null)
            {
                string leadName = IsLocalPlayer(leader) ? "YOU" : FormatIt(leader);
                string leadMark = (self != null && leader == self) ? "  (you)" : "";
                if (leading)
                    GUI.color = new Color(0.45f, 1f, 0.7f, 1f);
                GUI.Label(new Rect(24, y, 520, 22),
                    "Least " + leadName + " " + HudDigits.TenthSeconds(leader.TimeAsIt) + leadMark,
                    _status);
                GUI.color = prev;
                y += 22f;
            }

            // Briefly show all players' times on a compact line (cycle).
            float cycle = Mathf.Repeat(Time.time, AllStandingsCycleSec);
            if (_living.Count > 0 && cycle < AllStandingsShowSec)
            {
                _standings.Clear();
                _standings.Append("All ");
                _living.Sort(ByItTime);
                for (int i = 0; i < _living.Count; i++)
                {
                    if (i > 0) _standings.Append(" | ");
                    var p = _living[i];
                    string n = IsLocalPlayer(p) ? "YOU" : (string.IsNullOrEmpty(p.PlayerId) ? p.name : p.PlayerId);
                    _standings.Append(n);
                    _standings.Append(' ');
                    _standings.Append(HudDigits.Tenth0(p.TimeAsIt));
                }
                GUI.Label(new Rect(24, y, 640, 22), _standings.ToString(), _status);
                y += 22f;
            }

            return y;
        }

        bool IsLocalPlayer(ItController it)
        {
            if (it == null) return false;
            if (_self != null) return it == _self;
            if (it.gameObject == gameObject) return true;
            if (motor != null && it.Motor == motor) return true;
            return it.LooksLocal();
        }


        void TickModeChangeFlash()
        {
            var modes = TagModeController.Instance;
            if (modes == null) return;
            var cur = modes.SelectedMode;
            // Rematch / menu: re-prime so the next F1-F4 change still flashes.
            if (modes.Phase != MatchPhase.Playing && modes.Phase != MatchPhase.Countdown)
            {
                _prevMode = cur;
                _modeFlashPrimed = true;
                return;
            }
            if (!_modeFlashPrimed)
            {
                _prevMode = cur;
                _modeFlashPrimed = true;
                return;
            }
            if (cur == _prevMode) return;
            _prevMode = cur;
            _modeFlashLabel = FriendlyModeName(cur);
            _modeFlashUntil = Time.time + ModeFlashSec;
        }

        void DrawModeChangeFlash()
        {
            float rem = _modeFlashUntil - Time.time;
            if (rem <= 0f || string.IsNullOrEmpty(_modeFlashLabel)) return;

            if (_flash == null) return;
            _flash.fontSize = 52;

            float elapsed = ModeFlashSec - rem;
            float fadeIn = 0.08f;
            float fadeOut = 0.22f;
            float a;
            if (elapsed < fadeIn) a = elapsed / fadeIn;
            else if (rem < fadeOut) a = rem / fadeOut;
            else a = 1f;

            _flash.normal.textColor = new Color(1f, 0.92f, 0.45f, a);
            float w = 720f;
            float h = 70f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.18f, w, h);
            GUI.Label(r, "MODE  " + _modeFlashLabel, _flash);
            if (_status != null)
            {
                var prev = _status.normal.textColor;
                var prevA = _status.alignment;
                _status.alignment = TextAnchor.MiddleCenter;
                _status.normal.textColor = new Color(0.85f, 0.9f, 1f, a * 0.9f);
                GUI.Label(new Rect(r.x, r.yMax - 4f, r.width, 24f), "F1 Hot Potato  |  F2 Least It  |  F3 Trail Tag  |  F4 Free play", _status);
                _status.alignment = prevA;
                _status.normal.textColor = prev;
            }
        }

        static string FriendlyModeName(TagModeId id)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return "Hot Potato";
                case TagModeId.LeastIt: return "Least It";
                case TagModeId.TrailTag: return "Trail Tag";
                case TagModeId.FreePlay: return "Free play";
                default: return id.ToString();
            }
        }

        static string PhaseLabel(MatchPhase phase)
        {
            switch (phase)
            {
                case MatchPhase.Countdown: return "Countdown";
                case MatchPhase.Playing: return "Playing";
                case MatchPhase.PostRound: return "Post-round";
                case MatchPhase.Results: return "Results";
                default: return "Idle";
            }
        }

        static string LocoVerb(PlayerMotor motor)
        {
            if (motor.IsAirDashing) return "DASH";
            switch (motor.State)
            {
                case MoveState.Sprint: return "RUN";
                case MoveState.Slide: return "SLIDE";
                case MoveState.WallRun: return "WALL";
                case MoveState.WallClimb: return "CLIMB";
                case MoveState.LandStun: return "LAND";
                case MoveState.Air: return "AIR";
                case MoveState.Ski: return "SKI";
                case MoveState.Crouch: return "CROUCH";
                case MoveState.Mantle: return "VAULT";
                case MoveState.Jet: return "JET";
                case MoveState.Walk: return "WALK";
                default: return "IDLE";
            }
        }

        string FormatIt(ItController it)
        {
            if (it == null) return "none";
            if (IsLocalPlayer(it))
                return "YOU";
            return string.IsNullOrEmpty(it.PlayerId) ? it.gameObject.name : it.PlayerId;
        }

        /// <summary>It-line note while the tag-back window is open. Empty when it is shut.</summary>
        string TagBackHudSuffix()
        {
            ItController self = _self;
            if (self == null) return "";
            if (!self.IsIt && self.TagBackRemaining > 0.001f)
                return "  safe " + HudDigits.TenthSeconds(self.TagBackRemaining);
            if (!self.IsIt) return "";
            float best = 0f;
            var modes = TagModeController.Instance;
            var roster = modes != null ? modes.PlayersForHud : null;
            if (roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    var p = roster[i];
                    if (p == null || !p.BlocksTagBackFrom(self)) continue;
                    if (p.TagBackRemaining > best) best = p.TagBackRemaining;
                }
            }
            if (best <= 0.001f) return "";
            return "  no tag-back " + HudDigits.TenthSeconds(best);
        }

        static ItController ScanItControllers()
        {
            var all = Object.FindObjectsByType<ItController>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c != null && c.IsIt && c.IsAlive)
                    return c;
            }
            return null;
        }
        void TickSuddenDeathFlash()
        {
            var modes = TagModeController.Instance;
            bool trailPlay = modes != null && modes.SelectedMode == TagModeId.TrailTag && modes.Phase == MatchPhase.Playing;
            bool sd = trailPlay && modes.SuddenDeath;
            if (!_sdFlashPrimed)
            {
                _prevSuddenDeath = sd;
                _sdFlashPrimed = true;
                return;
            }
            // Rematch / menu leave Playing: clear edge so the next SD rising edge flashes again.
            if (!trailPlay)
            {
                _prevSuddenDeath = false;
                return;
            }
            if (sd && !_prevSuddenDeath)
                _sdFlashUntil = Time.time + SdFlashSec;
            _prevSuddenDeath = sd;
        }

        void DrawSuddenDeathFlash()
        {
            float rem = _sdFlashUntil - Time.time;
            if (rem <= 0f) return;
            if (_flash == null) return;
            _flash.fontSize = 48;
            float elapsed = SdFlashSec - rem;
            float a = 1f;
            if (elapsed < 0.08f) a = elapsed / 0.08f;
            else if (rem < 0.22f) a = rem / 0.22f;
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.35f, 0.2f, a);
            float w = 640f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.22f, w, 56f);
            GUI.Label(r, "SUDDEN DEATH", _flash);
            GUI.color = prev;
        }
    }
}
