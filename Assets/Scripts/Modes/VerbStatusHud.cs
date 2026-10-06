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
        GUIStyle _label;
        GUIStyle _caption;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }

        void OnGUI()
        {
            if (motor == null) return;
            var modes = TagModeController.Instance;
            if (modes != null && modes.Phase != MatchPhase.Playing && modes.Phase != MatchPhase.PostRound)
                return;

            EnsureStyles();
            float sw = Screen.width;
            float sh = Screen.height;
            VerbHudLayout.Box cluster = VerbHudLayout.Cluster(sw, sh);
            float hudScale = GameSettings.Current != null ? GameSettings.Current.HudScale : 1f;
            bool scaled = hudScale < 0.999f || hudScale > 1.001f;
            Matrix4x4 prevMatrix = GUI.matrix;
            if (scaled)
                GUIUtility.ScaleAroundPivot(new Vector2(hudScale, hudScale), new Vector2(cluster.Right, cluster.Y));
            bool colorblind = GameSettings.Current != null && GameSettings.Current.Colorblind;
            ItController self = motor.GetComponent<ItController>();
            float dashMax = motor.cfg != null ? Mathf.Max(0.01f, motor.cfg.airDashCooldown) : 30f;
            float dashRem = motor.AirDashCooldownRemaining;
            float dashFill = motor.IsAirDashing ? 1f : 1f - Mathf.Clamp01(dashRem / dashMax);
            string dashText = motor.IsAirDashing ? "GO" : (dashRem <= 0.05f ? "READY" : dashRem.ToString("0.0"));
            DrawRing(VerbHudLayout.Row(cluster, 0), dashFill, VerbColor(colorblind, 0), "DASH", dashText, false);

            float safe = self != null ? self.TagBackRemaining : 0f;
            float safeFill = safe > 0.001f ? Mathf.Clamp01(safe / TagBackImmunity.DefaultSeconds) : 0f;
            string safeText = safe > 0.001f ? safe.ToString("0.0") : "—";
            DrawRing(VerbHudLayout.Row(cluster, 1), safeFill, VerbColor(colorblind, 1), "SAFE", safeText, true);

            float stag = motor.StaggerRemaining;
            float stagFill = stag > 0.001f ? Mathf.Clamp01(stag / PunchStagger.Duration) : 0f;
            string stagText = stag > 0.001f ? stag.ToString("0.00") : "—";
            DrawRing(VerbHudLayout.Row(cluster, 2), stagFill, VerbColor(colorblind, 2), "STAGGER", stagText, false);

            bool cling = motor.ClingHeldActive;
            DrawRing(VerbHudLayout.Row(cluster, 3), cling ? 1f : 0f, VerbColor(colorblind, 3), "CLING", cling ? "HELD" : "—", false);
            if (scaled) GUI.matrix = prevMatrix;
        }

        static Color VerbColor(bool colorblind, int index)
        {
            GameSettings.VerbMark(colorblind, index, out float r, out float g, out float b);
            return new Color(r, g, b, 1f);
        }

        void EnsureStyles()
        {
            int size = Screen.height >= 1000 ? 16 : 14;
            if (_label != null && _label.fontSize == size) return;
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _label.normal.textColor = Color.white;
            _caption = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
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
