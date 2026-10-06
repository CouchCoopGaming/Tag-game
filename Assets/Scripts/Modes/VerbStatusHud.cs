using Tag.Core;
using Tag.Couch;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Air-dash ring, tag-back timer, stagger, and cling-held.
    /// Words and shapes carry the state. Blue, amber, purple, and white
    /// are not a red/green pair. The cluster stays off the minimap and the mute chip.
    /// </summary>
    public class VerbStatusHud : MonoBehaviour
    {
        public PlayerMotor motor;
        public Camera View;
        public int Seat = -1;
        public int DriveDevice = -1;
        ItController _self;
        GUIStyle _label;
        GUIStyle _caption;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }

        void Start()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (motor != null) _self = motor.GetComponent<ItController>();
            if (_self == null) _self = GetComponent<ItController>();
            BootStyles();
        }

        void BootStyles()
        {
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _label.normal.textColor = Color.white;
            _caption = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
        }

        void OnGUI()
        {
            if (motor == null || _label == null) return;
            if (DriveDevice >= 0 && View != null)
            {
                DrawCouch();
                return;
            }
            var modes = TagModeController.Instance;
            if (modes != null && modes.Phase != MatchPhase.Playing && modes.Phase != MatchPhase.PostRound)
                return;

            int size = Screen.height >= 1000 ? 16 : 14;
            _label.fontSize = size;
            _caption.fontSize = size;
            float sw = Screen.width;
            float sh = Screen.height;
            VerbHudLayout.Box cluster = VerbHudLayout.Cluster(sw, sh);
            float hudScale = GameSettings.Current != null ? GameSettings.Current.HudScale : 1f;
            bool scaled = hudScale < 0.999f || hudScale > 1.001f;
            Matrix4x4 prevMatrix = GUI.matrix;
            if (scaled)
                GUIUtility.ScaleAroundPivot(new Vector2(hudScale, hudScale), new Vector2(cluster.Right, cluster.Y));
            int palette = PaletteFor(Seat);
            ItController self = _self;
            float dashMax = motor.cfg != null ? Mathf.Max(0.01f, motor.cfg.airDashCooldown) : 30f;
            float dashRem = motor.AirDashCooldownRemaining;
            float dashFill = motor.IsAirDashing ? 1f : 1f - Mathf.Clamp01(dashRem / dashMax);
            string dashText = motor.IsAirDashing ? "GO" : (dashRem <= 0.05f ? "READY" : HudDigits.Tenth0(dashRem));
            DrawRing(VerbHudLayout.Row(cluster, 0), dashFill, VerbColor(palette, 0), "DASH", dashText, false);

            float safe = self != null ? self.TagBackRemaining : 0f;
            float safeFill = safe > 0.001f ? Mathf.Clamp01(safe / TagBackImmunity.DefaultSeconds) : 0f;
            string safeText = safe > 0.001f ? HudDigits.Tenth0(safe) : "—";
            DrawRing(VerbHudLayout.Row(cluster, 1), safeFill, VerbColor(palette, 1), "SAFE", safeText, true);

            float stag = motor.StaggerRemaining;
            float stagFill = stag > 0.001f ? Mathf.Clamp01(stag / PunchStagger.Duration) : 0f;
            string stagText = stag > 0.001f ? HudDigits.Hundredth0(stag) : "—";
            DrawRing(VerbHudLayout.Row(cluster, 2), stagFill, VerbColor(palette, 2), "STAGGER", stagText, false);

            bool cling = motor.ClingHeldActive;
            DrawRing(VerbHudLayout.Row(cluster, 3), cling ? 1f : 0f, VerbColor(palette, 3), "CLING", cling ? "HELD" : "—", false);
            DrawCaptions(Seat < 0 ? 0 : Seat, cluster.X, cluster.Y, sh);
            if (scaled) GUI.matrix = prevMatrix;
        }

        void DrawCouch()
        {
            var modes = TagModeController.Instance;
            if (modes != null && modes.Phase != MatchPhase.Playing && modes.Phase != MatchPhase.PostRound)
                return;
            Rect area = View.pixelRect;
            if (area.width < 8f || area.height < 8f) return;
            float gx = area.x;
            float gy = Screen.height - (area.y + area.height);
            float gw = area.width;
            float gh = area.height;
            float hudScale = GameSettings.Current != null ? GameSettings.Current.HudScale : 1f;
            int seat = Seat < 0 ? 0 : Seat;
            int palette = PaletteFor(seat);
            _label.fontSize = (int)((gh >= 500f ? 16 : 13) * hudScale);
            _caption.fontSize = _label.fontSize;
            ItController self = _self;
            CouchPlay.HudBox(gx, gy, gw, gh, 0, out float tx, out float ty, out float tw, out float th);
            CouchPlay.HudBox(gx, gy, gw, gh, 1, out float nx, out float ny, out float nw, out float nh);
            CouchPlay.HudBox(gx, gy, gw, gh, 2, out float ix, out float iy, out float iw, out float ih);
            CouchPlay.HudBox(gx, gy, gw, gh, 3, out float cx, out float cy, out float cw, out float ch);

            var prev = GUI.color;
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            GUI.DrawTexture(new Rect(tx, ty, tw, th), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float remain = modes != null ? modes.Remaining : 0f;
            GUI.Label(new Rect(tx, ty, tw, th), HudDigits.Tenth0(remain), _label);

            AccessibilityPalette.Player(palette, seat, out float r, out float g, out float b);
            _label.normal.textColor = new Color(r, g, b, 1f);
            GUI.Label(new Rect(nx, ny, 18f, nh), AccessibilityPalette.Glyph(seat), _label);
            GUI.Label(new Rect(nx + 18f, ny, nw - 18f, nh), CouchPlay.Name(seat), _label);
            _label.normal.textColor = Color.white;

            if (self != null && self.IsIt)
            {
                AccessibilityPalette.It(palette, out float ir, out float ig, out float ib);
                GUI.color = new Color(ir, ig, ib, 1f);
                GUI.DrawTexture(new Rect(ix, iy, iw, ih), Texture2D.whiteTexture);
                GUI.color = Color.white;
                _label.normal.textColor = new Color(0.08f, 0.08f, 0.1f, 1f);
                GUI.Label(new Rect(ix, iy, iw, ih * 0.5f), AccessibilityPalette.ItGlyph, _label);
                GUI.Label(new Rect(ix, iy + ih * 0.42f, iw, ih * 0.58f), "IT", _label);
                _label.normal.textColor = Color.white;
            }

            float dashMax = motor.cfg != null ? Mathf.Max(0.01f, motor.cfg.airDashCooldown) : 30f;
            float dashRem = motor.AirDashCooldownRemaining;
            float dashFill = motor.IsAirDashing ? 1f : 1f - Mathf.Clamp01(dashRem / dashMax);
            string dashText = motor.IsAirDashing ? "GO" : (dashRem <= 0.05f ? "READY" : HudDigits.Tenth0(dashRem));
            float row = ch / 4f;
            DrawRing(new VerbHudLayout.Box { X = cx, Y = cy, W = cw, H = row - 4f }, dashFill, VerbColor(palette, 0), "DASH", dashText, false);

            float safe = self != null ? self.TagBackRemaining : 0f;
            float safeFill = safe > 0.001f ? Mathf.Clamp01(safe / TagBackImmunity.DefaultSeconds) : 0f;
            string safeText = safe > 0.001f ? HudDigits.Tenth0(safe) : "—";
            DrawRing(new VerbHudLayout.Box { X = cx, Y = cy + row, W = cw, H = row - 4f }, safeFill, VerbColor(palette, 1), "SAFE", safeText, true);

            float stag = motor.StaggerRemaining;
            float stagFill = stag > 0.001f ? Mathf.Clamp01(stag / PunchStagger.Duration) : 0f;
            string stagText = stag > 0.001f ? HudDigits.Hundredth0(stag) : "—";
            DrawRing(new VerbHudLayout.Box { X = cx, Y = cy + row * 2f, W = cw, H = row - 4f }, stagFill, VerbColor(palette, 2), "STAGGER", stagText, false);

            bool cling = motor.ClingHeldActive;
            DrawRing(new VerbHudLayout.Box { X = cx, Y = cy + row * 3f, W = cw, H = row - 4f }, cling ? 1f : 0f, VerbColor(palette, 3), "CLING", cling ? "HELD" : "—", false);
            DrawCaptions(seat, cx, cy + ch + 4f, gy + gh);
            GUI.color = prev;
        }

        static int PaletteFor(int seat)
        {
            if (GameSettings.Current == null) return 0;
            return GameSettings.Current.PaletteOf(seat < 0 ? 0 : seat);
        }

        static Color VerbColor(int palette, int index)
        {
            AccessibilityPalette.Verb(palette, index, out float r, out float g, out float b);
            return new Color(r, g, b, 1f);
        }

        void DrawCaptions(int seat, float x, float y, float yMax)
        {
            GameSettings settings = GameSettings.Current;
            if (settings == null || !settings.CaptionsOf(seat)) return;
            GUI.color = Color.white;
            float camX = 0f;
            float camZ = 0f;
            if (View != null)
            {
                Vector3 p = View.transform.position;
                camX = p.x;
                camZ = p.z;
            }
            else if (motor != null)
            {
                Vector3 p = motor.transform.position;
                camX = p.x;
                camZ = p.z;
            }
            float yy = y;
            for (int k = 0; k < CaptionFeed.Count; k++)
            {
                if (!CaptionFeed.Shows(settings, seat, k, camX, camZ)) continue;
                if (yy + 18f > yMax) break;
                GUI.Label(new Rect(x, yy, 18f, 18f), CaptionFeed.Icon(k), _label);
                if (CaptionFeed.HasDirection(k))
                    GUI.Label(new Rect(x + 16f, yy, 18f, 18f), CaptionFeed.Arrow(CaptionFeed.Direction(k, camX, camZ)), _label);
                yy += 18f;
            }
        }

        void DrawRing(VerbHudLayout.Box row, float fill, Color mark, string name, string value, bool dotted)
        {
            float side = Mathf.Min(row.H, 52f);
            float cx = row.X + side * 0.5f + 4f;
            float cy = row.Y + row.H * 0.5f;
            var prev = GUI.color;
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            GUI.DrawTexture(new Rect(row.X, row.Y, row.W, row.H), Texture2D.whiteTexture);
            DrawArc(cx, cy, side * 0.42f, 1f, new Color(0.2f, 0.24f, 0.3f, 1f), false);
            DrawArc(cx, cy, side * 0.42f, Mathf.Clamp01(fill), mark, dotted);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - 1.5f, cy - side * 0.42f - 3f, 3f, 6f), Texture2D.whiteTexture);
            _label.normal.textColor = Color.white;
            GUI.Label(new Rect(row.X + side + 10f, row.Y, row.W * 0.46f, row.H), name, _label);
            _caption.normal.textColor = mark;
            GUI.Label(new Rect(row.X + side + 8f, row.Y, row.W - side - 16f, row.H), value, _caption);
            GUI.color = prev;
        }

        static void DrawArc(float cx, float cy, float radius, float fill, Color color, bool dotted)
        {
            const int seg = 20;
            int n = fill >= 0.999f ? seg : Mathf.Clamp(Mathf.RoundToInt(fill * seg), 0, seg);
            var prev = GUI.color;
            GUI.color = color;
            for (int i = 0; i < n; i++)
            {
                if (dotted && (i & 1) == 1) continue;
                float a = -Mathf.PI * 0.5f + (i + 0.5f) / seg * Mathf.PI * 2f;
                float px = cx + Mathf.Cos(a) * radius;
                float py = cy + Mathf.Sin(a) * radius;
                GUI.DrawTexture(new Rect(px - 2.5f, py - 2.5f, 5f, 5f), Texture2D.whiteTexture);
            }
            GUI.color = prev;
        }
    }
}
