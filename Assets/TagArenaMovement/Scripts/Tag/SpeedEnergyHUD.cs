using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;

namespace TagArena.Movement
{
    /// <summary>
    /// Cave-man OnGUI: speed, move state, jet fuel, ski on/off + P0 controls cheat-sheet
    /// + nearest mega-park zone + It / mode / Hot Potato fuse / Least It times
    /// + bearing/distance to CurrentIt when you are not It
    /// + bearing/distance to nearest non-It when you ARE It (Prey)
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

        // Labels mirror PlayerInputReader defaults (skiKey/jetKey/crouchKey/punchKey/lungeKey + hard-coded alts).
        const string Controls =
            "WASD move\n" +
            "Shift ski\n" +
            "RMB jet\n" +
            "Space jump\n" +
            "Ctrl/C crouch\n" +
            "LMB/E punch\n" +
            "MMB lunge\n" +
            "F1 Hot Potato\n" +
            "F2 Least It\n" +
            "F3 Trail Tag";

        // Flash full Least-It standings briefly every few seconds.
        const float AllStandingsShowSec = 3.5f;
        const float AllStandingsCycleSec = 8f;

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

            float y = 110f;
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

            GUI.Label(new Rect(24, y, 480, 22), "Mode " + modeName, _status);
            y += 22f;
            GUI.Label(new Rect(24, y, 480, 22), "It: " + itLabel, _status);
            y += 22f;

            // Compass: hunt It when not It; hunt nearest prey when you are It.
            if (it != null && IsLocalPlayer(it))
                y = DrawPreyBearing(modes, y);
            else if (it != null)
                y = DrawItBearing(it, y);

            if (fuseLine != null)
            {
                GUI.Label(new Rect(24, y, 480, 22), fuseLine, _status);
                y += 22f;
            }

            if (modes != null && modes.SelectedMode == TagModeId.LeastIt)
                y = DrawLeastItTimes(modes, y);
        }

        /// <summary>
        /// Cave-man compass toward It: camera-relative 8-way + flat meters.
        /// </summary>
        float DrawItBearing(ItController it, float y)
        {
            return DrawCompassBearing("It", it.transform.position, y);
        }

        /// <summary>
        /// Mirror of It compass when local is It: nearest alive non-It (Prey).
        /// </summary>
        float DrawPreyBearing(TagModeController modes, float y)
        {
            var prey = FindNearestPrey(modes);
            if (prey == null)
            {
                GUI.Label(new Rect(24, y, 520, 22), "Prey  none", _status);
                return y + 22f;
            }
            return DrawCompassBearing("Prey", prey.transform.position, y);
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
        /// </summary>
        float DrawCompassBearing(string label, Vector3 to, float y)
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
            GUI.Label(new Rect(24, y, 520, 22), line, _status);
            return y + 22f;
        }

        float DrawLeastItTimes(TagModeController modes, float y)
        {
            ItController self = null;
            ItController leader = null;
            float best = float.MaxValue;
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
                }
            }

            if (self == null)
            {
                var onMotor = motor != null ? motor.GetComponent<ItController>() : null;
                if (onMotor != null && onMotor.IsAlive)
                    self = onMotor;
            }

            float youT = self != null ? self.TimeAsIt : 0f;
            GUI.Label(new Rect(24, y, 520, 22),
                "You " + youT.ToString("0.0") + "s as It", _status);
            y += 22f;

            if (leader != null)
            {
                string leadName = IsLocalPlayer(leader) ? "YOU" : FormatIt(leader);
                string leadMark = (self != null && leader == self) ? "  (you)" : "";
                GUI.Label(new Rect(24, y, 520, 22),
                    "Lead " + leadName + " " + leader.TimeAsIt.ToString("0.0") + "s" + leadMark,
                    _status);
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
