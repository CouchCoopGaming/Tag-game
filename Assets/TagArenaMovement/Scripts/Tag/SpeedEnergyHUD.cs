using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using Tag.Trail;

namespace TagArena.Movement
{
    /// <summary>
    /// Cave-man OnGUI: speed, move state, jet fuel, ski on/off + P0 controls cheat-sheet
    /// + nearest mega-park zone + It / mode / Hot Potato fuse / Least It times (LEAD/LAG tint)
    /// + bearing/distance to CurrentIt when you are not It
    /// + bearing/distance to nearest non-It when you ARE It (Prey)
    /// + brief YOU'RE IT / YOU'RE FREE OnGUI flash on local It handoff
    /// + Trail Tag soft near-miss TRAIL! edge pulse when near a foreign ribbon
    /// + brief OUT! / TRAIL HIT OnGUI flash when local IsAlive drops (trail eliminate)
    /// (reads TagModeController, falls back to ItController scan).
    /// Local human only (wired by LocalPlayerSpawner for index 0).
    /// </summary>
    public class SpeedEnergyHUD : MonoBehaviour
    {
        public PlayerMotor motor;
        GUIStyle _big;
        GUIStyle _small;
        GUIStyle _keys;
        GUIStyle _status;
        GUIStyle _flash;

        // Brief center flash when local gains/loses It (SetIt / TransferIt / punch).
        const float ItFlashSec = 0.5f;
        bool _itFlashPrimed;
        bool _prevLocalIsIt;
        float _itFlashUntil;
        bool _itFlashGained;

        // Trail Tag near-miss (foreign ribbon) — soft edge warn before eliminate contact.
        const float TrailNearMissWarnM = 4.5f;
        readonly List<TrailSegment> _trailNearScratch = new List<TrailSegment>();
        float _trailNearDist = float.MaxValue;
        bool _trailNearActive;

        // Trail Tag eliminate — brief center flash when local IsAlive drops (trail hit).
        const float TrailOutFlashSec = 0.65f;
        bool _aliveFlashPrimed;
        bool _prevLocalAlive = true;
        float _trailOutFlashUntil;

        // Brief center flash when F1/F2/F3 (or menu) changes SelectedMode.
        const float ModeFlashSec = 0.85f;
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
            "Ctrl/C crouch+slide\n" +
            "LMB/E punch\n" +
            "Q/Alt air dash 30s\n" +
            "MMB lunge\n" +
            "F1 Hot Potato\n" +
            "F2 Least It\n" +
            "F3 Trail Tag";

        // Flash full Least-It standings briefly every few seconds.
        const float AllStandingsShowSec = 3.5f;
        const float AllStandingsCycleSec = 8f;

        // Compass close-range pulse (Prey hunt / It flee), flat meters.
        const float CompassPulseDistM = 12f;

        // Hot Potato fuse HUD warn fallback (matches ItMarker / DummyPatrol when tuning missing).
        const float HotPotatoWarnSecFallback = 10f;

        // Relative to camera: forward = N, right = E (hunt direction, not world north).
        static readonly string[] Compass8 = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        void OnGUI()
        {
            if (!motor) return;
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 18 };
                _keys = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                _status = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                _big.normal.textColor = Color.white;
                _small.normal.textColor = new Color(0.85f, 0.9f, 1f);
                _keys.normal.textColor = new Color(0.75f, 0.82f, 0.95f);
                _status.normal.textColor = new Color(1f, 0.92f, 0.55f);
            }

            float hs = motor.HorizSpeed;
            string kph = (hs * 3.6f).ToString("0");
            GUI.Label(new Rect(24, 16, 520, 36), kph + " km/h   " + motor.State, _big);

            float maxE = 100f;
            if (motor.cfg != null)
                maxE = motor.cfg.jetEnergyMax + 0.001f;
            float e = Mathf.Clamp01(motor.Energy / maxE);
            GUI.Box(new Rect(24, 56, 240, 20), GUIContent.none);
            GUI.Box(new Rect(24, 56, 240 * e, 20), GUIContent.none);

            string jet = motor.Jetting ? "JETTING" : "jet";
            string ski = motor.Skiing ? "SKI ON" : "ski off";
            GUI.Label(
                new Rect(24, 80, 480, 26),
                "JET " + motor.Energy.ToString("0") + "/" + maxE.ToString("0") + "  " + jet + "   " + ski,
                _small);

            float dashCd = motor.AirDashCooldownRemaining;
            string dashLine = dashCd > 0.05f
                ? ("DASH CD " + dashCd.ToString("0.0") + "s")
                : (motor.IsAirDashing ? "DASH!" : "DASH ready");
            GUI.Label(new Rect(24, 102, 480, 22), dashLine, _small);

            float y = 124f;
            string zone = ZoneNameMarkers.GetNearestZoneName(motor.transform.position);
            GUI.Label(new Rect(24, y, 480, 22), "Zone: " + zone, _small);
            y += 24f;

            if (motor.SuperGlideT >= 0f)
            {
                GUI.Label(new Rect(24, y, 280, 28), "GLIDE WINDOW", _big);
                y += 32f;
            }

            GUI.Label(new Rect(24, y, 220, 200), Controls, _keys);
            y += 178f;

            DrawMatchStatus(y);
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
        /// Watch local ItController.IsIt — same flag SetIt / TransferIt / PunchHitbox mutate.
        /// Skip first sample so spawn / HUD enable does not false-flash.
        /// </summary>
        void TickItHandoffFlash()
        {
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
            _itFlashUntil = Time.unscaledTime + ItFlashSec;
            _prevLocalIsIt = localIsIt;
        }

        bool ResolveLocalIsIt()
        {
            ItController self = null;
            if (motor != null)
                self = motor.GetComponent<ItController>();
            if (self == null)
                self = GetComponent<ItController>();
            if (self != null)
                return self.IsAlive && self.IsIt;
            return false;
        }

        /// <summary>
        /// Cave-man center flash ~0.5s: YOU'RE IT / YOU'RE FREE (TAG! handoff beat).
        /// </summary>
        void DrawItHandoffFlash()
        {
            float rem = _itFlashUntil - Time.unscaledTime;
            if (rem <= 0f) return;

            if (_flash == null)
            {
                _flash = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 64,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }

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
                var sub = new Rect(r.x, r.yMax - 8f, r.width, 28f);
                Color prevStatus = _status.normal.textColor;
                _status.normal.textColor = new Color(1f, 0.92f, 0.55f, a * 0.9f);
                var prevAlign = _status.alignment;
                _status.alignment = TextAnchor.MiddleCenter;
                GUI.Label(sub, "TAG!", _status);
                _status.alignment = prevAlign;
                _status.normal.textColor = prevStatus;
            }
            GUI.matrix = prevM;
            GUI.color = prev;
        }


        /// <summary>
        /// Trail Tag only: closest foreign TrailSegment via CopyActive + ClosestPointOnSegment
        /// (same helpers DummyPatrol trail avoid uses). Warn under TrailNearMissWarnM — soft
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

            ItController self = motor.GetComponent<ItController>();
            if (self == null) self = GetComponent<ItController>();
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
                // Foreign only — own ribbon is self-grace / separate fairness case.
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
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
            float pulse = Mathf.Lerp(0.3f, 1f, wave);
            float a = Mathf.Lerp(0.12f, 0.42f, urgency * pulse);

            Color prev = GUI.color;
            // Cyan edge → hot warn as you close in (same family as It flee compass).
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

            if (_flash == null)
            {
                _flash = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 64,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
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
                GUI.Label(new Rect(r.x, r.yMax - 6f, r.width, 24f),
                    _trailNearDist.ToString("0.0") + "m", _status);
                _status.alignment = prevAlign;
                _status.normal.textColor = prevStatus;
            }
            GUI.matrix = prevM;
            GUI.color = prev;
        }


        /// <summary>
        /// Watch local ItController.IsAlive — same flag TrailSegment hit / EliminatePlayer mutate.
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
                _trailOutFlashUntil = Time.unscaledTime + TrailOutFlashSec;

            _prevLocalAlive = alive;
        }

        bool ResolveLocalAlive()
        {
            ItController self = null;
            if (motor != null)
                self = motor.GetComponent<ItController>();
            if (self == null)
                self = GetComponent<ItController>();
            if (self != null)
                return self.IsAlive;
            return true;
        }

        /// <summary>
        /// Cave-man center flash ~0.65s on trail eliminate: OUT! + TRAIL HIT.
        /// Distinct from TAG handoff (YOU'RE IT / FREE) and near-miss TRAIL! edge pulse —
        /// lower screen, hot red, no edge bars.
        /// </summary>
        void DrawTrailEliminateFlash()
        {
            float rem = _trailOutFlashUntil - Time.unscaledTime;
            if (rem <= 0f) return;

            if (_flash == null)
            {
                _flash = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 64,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }

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

        void DrawMatchStatus(float y)
        {
            string modeName = "";
            string itLabel = "";
            string fuseLine = null;
            ItController it = null;

            var modes = TagModeController.Instance;
            if (modes != null)
            {
                modeName = FriendlyModeName(modes.SelectedMode);
                it = modes.CurrentIt;
                if (it == null)
                    it = ScanItControllers();
                itLabel = FormatIt(it);

                if (modes.SelectedMode == TagModeId.HotPotato)
                {
                    float rem = modes.Remaining;
                    fuseLine = rem > 0f
                        ? "Fuse " + rem.ToString("0.0") + "s"
                        : "Fuse ";
                }
            }
            else
            {
                it = ScanItControllers();
                itLabel = FormatIt(it);
                modeName = "default";
            }

            // Hot Potato + local is It: pulse fuse/It lines as Remaining approaches warnSec
            // (same urgency curve as ItMarker / DummyPatrol).
            bool localIsIt = it != null && IsLocalPlayer(it);
            float fuseUrgency = (localIsIt && modes != null && modes.SelectedMode == TagModeId.HotPotato)
                ? HotPotatoFuseUrgency(modes)
                : 0f;
            bool pulseFuse = fuseUrgency > 0.01f;

            GUI.Label(new Rect(24, y, 480, 22), "Mode " + modeName, _status);
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
        /// Pulse fuse/It status text: scale + amber→hot tint, faster as urgency rises.
        /// </summary>
        void DrawFuseUrgencyLabel(Rect r, string text, float urgency)
        {
            Color prevColor = GUI.color;
            Matrix4x4 prevMatrix = GUI.matrix;

            float hz = Mathf.Lerp(4f, 11f, urgency);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
            float pulse = Mathf.Lerp(0.35f, 1f, wave);

            // Status amber → hot-potato warn (magenta-orange), same family as It flee compass.
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
        /// Pulses under ~12m (cyan/white → hot-potato warn) so close flee reads distinct from Prey hunt.
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
            Vector3 from = motor.transform.position;
            ItController best = null;
            float bestSq = float.MaxValue;

            void Consider(ItController p)
            {
                if (p == null || !p.IsAlive || p.IsIt) return;
                if (IsLocalPlayer(p)) return;
                Vector3 d = p.transform.position - from;
                d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = p;
                }
            }

            if (modes != null && modes.PlayersForHud != null)
            {
                var list = modes.PlayersForHud;
                for (int i = 0; i < list.Count; i++)
                    Consider(list[i]);
            }

            // Empty / no-candidate PlayersForHud: same scan as when modes is null.
            if (best == null)
            {
                var all = Object.FindObjectsByType<ItController>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                    Consider(all[i]);
            }

            return best;
        }

        /// <summary>
        /// Shared cave-man compass: camera-relative 8-way + flat meters.
        /// Label examples: "It ->  SW  18m" / "Prey ->  SW  18m".
        /// When pulseClose and dist &lt; CompassPulseDistM: scale/alpha/color urgency pulse.
        /// itWarnTint: cyan/white → magenta-orange (flee); else amber → red (hunt).
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

            string line = label + " ->  " + Compass8[relIdx] + "  " + dist.ToString("0") + "m";

            Color prevColor = GUI.color;
            Matrix4x4 prevMatrix = GUI.matrix;
            if (pulseClose && dist < CompassPulseDistM)
            {
                // 0 at threshold, 1 at contact — closer = hotter / bigger / faster pulse.
                float urgency = 1f - Mathf.Clamp01(dist / CompassPulseDistM);
                float hz = Mathf.Lerp(3.5f, 9f, urgency);
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
                float pulse = Mathf.Lerp(0.35f, 1f, wave);

                Color calm;
                Color hot;
                if (itWarnTint)
                {
                    // Fleeing It: cyan/white → hot-potato warn (magenta-orange), distinct from Prey hunt.
                    calm = new Color(0.55f, 0.95f, 1f, 1f);
                    hot = new Color(1f, 0.35f, 0.55f, 1f);
                }
                else
                {
                    // Hunting Prey: amber → red.
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
            var living = new List<ItController>(8);

            var list = modes.PlayersForHud;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    if (p == null || !p.IsAlive) continue;
                    living.Add(p);
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

            if (self == null)
            {
                var onMotor = motor != null ? motor.GetComponent<ItController>() : null;
                if (onMotor != null && onMotor.IsAlive)
                    self = onMotor;
            }

            float youT = self != null ? self.TimeAsIt : 0f;
            // Soft standings cue: lowest It-time = LEAD (mint), highest/near-highest = LAG (coral).
            bool leading = false;
            bool lagging = false;
            if (self != null && living.Count > 0)
            {
                const float eps = 0.05f;
                leading = youT <= best + eps;
                if (!leading && living.Count >= 2)
                {
                    if (youT >= worst - eps)
                        lagging = true;
                    else if (living.Count >= 3)
                    {
                        // Near-highest: 2nd-from-bottom by TimeAsIt rank.
                        living.Sort((a, b) => a.TimeAsIt.CompareTo(b.TimeAsIt));
                        int idx = living.IndexOf(self);
                        if (idx >= living.Count - 2)
                            lagging = true;
                    }
                }
            }

            Color prev = GUI.color;
            string youTag = " as It";
            if (leading)
            {
                GUI.color = new Color(0.45f, 1f, 0.7f, 1f);
                youTag = "  LEAD";
            }
            else if (lagging)
            {
                GUI.color = new Color(1f, 0.55f, 0.42f, 1f);
                youTag = "  LAG";
            }
            GUI.Label(new Rect(24, y, 520, 22),
                "You " + youT.ToString("0.0") + "s" + youTag, _status);
            GUI.color = prev;
            y += 22f;

            if (leader != null)
            {
                string leadName = IsLocalPlayer(leader) ? "YOU" : FormatIt(leader);
                string leadMark = (self != null && leader == self) ? "  (you)" : "";
                if (leading)
                    GUI.color = new Color(0.45f, 1f, 0.7f, 1f);
                GUI.Label(new Rect(24, y, 520, 22),
                    "Lead " + leadName + " " + leader.TimeAsIt.ToString("0.0") + "s" + leadMark,
                    _status);
                GUI.color = prev;
                y += 22f;
            }

            // Briefly show all players' times on a compact line (cycle).
            float cycle = Mathf.Repeat(Time.unscaledTime, AllStandingsCycleSec);
            if (living.Count > 0 && cycle < AllStandingsShowSec)
            {
                var sb = new StringBuilder(64);
                sb.Append("All ");
                living.Sort((a, b) => a.TimeAsIt.CompareTo(b.TimeAsIt));
                for (int i = 0; i < living.Count; i++)
                {
                    if (i > 0) sb.Append(" | ");
                    var p = living[i];
                    string n = IsLocalPlayer(p) ? "YOU" : (string.IsNullOrEmpty(p.PlayerId) ? p.name : p.PlayerId);
                    sb.Append(n);
                    sb.Append(' ');
                    sb.Append(p.TimeAsIt.ToString("0.0"));
                }
                GUI.Label(new Rect(24, y, 640, 22), sb.ToString(), _status);
                y += 22f;
            }

            return y;
        }

        bool IsLocalPlayer(ItController it)
        {
            if (it == null) return false;
            if (it.gameObject == gameObject) return true;
            if (motor != null && it.GetComponent<PlayerMotor>() == motor) return true;
            if (it.GetComponent<PlayerInputReader>() != null && it.GetComponent<DummyPatrol>() == null)
                return true;
            return false;
        }


        void TickModeChangeFlash()
        {
            var modes = TagModeController.Instance;
            if (modes == null) return;
            var cur = modes.SelectedMode;
            if (!_modeFlashPrimed)
            {
                _prevMode = cur;
                _modeFlashPrimed = true;
                return;
            }
            if (cur == _prevMode) return;
            _prevMode = cur;
            _modeFlashLabel = FriendlyModeName(cur);
            _modeFlashUntil = Time.unscaledTime + ModeFlashSec;
        }

        void DrawModeChangeFlash()
        {
            float rem = _modeFlashUntil - Time.unscaledTime;
            if (rem <= 0f || string.IsNullOrEmpty(_modeFlashLabel)) return;

            if (_flash == null)
            {
                _flash = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 52,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }

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
                GUI.Label(new Rect(r.x, r.yMax - 4f, r.width, 24f), "F1 Hot Potato  |  F2 Least It  |  F3 Trail Tag", _status);
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
                default: return id.ToString();
            }
        }

        string FormatIt(ItController it)
        {
            if (it == null) return "none";
            if (IsLocalPlayer(it))
                return "YOU";
            return string.IsNullOrEmpty(it.PlayerId) ? it.gameObject.name : it.PlayerId;
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
    }
}
