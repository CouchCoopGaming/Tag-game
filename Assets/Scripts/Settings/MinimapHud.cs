using Tag.Core;
using Tag.Modes;
using UnityEngine;

namespace Tag.Settings
{
    /// <summary>
    /// Bottom-right map in the box the verb cluster already clears.
    /// M and gamepad Select toggle it. Scale 1 leaves the box where it is.
    /// </summary>
    public static class MinimapHud
    {
        static int _frame = -1;

        public static void ResetStatics()
        {
            _frame = -1;
        }

        public static void Draw()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            if (GameSettings.Current == null || !GameSettings.Current.Minimap) return;
            if (!ShowNow()) return;

            float sw = Screen.width;
            float sh = Screen.height;
            VerbHudLayout.Box box = VerbHudLayout.Minimap(sw, sh);
            float scale = GameSettings.Current.HudScale;
            Matrix4x4 prev = GUI.matrix;
            bool scaled = scale < 0.999f || scale > 1.001f;
            if (scaled)
            {
                var pivot = new Vector2(box.X + box.W, box.Y + box.H);
                GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
            }

            Color prevColor = GUI.color;
            GUI.color = new Color(0.08f, 0.12f, 0.16f, 0.82f);
            GUI.DrawTexture(new Rect(box.X, box.Y, box.W, box.H), Texture2D.whiteTexture);
            GUI.color = new Color(0.35f, 0.62f, 0.72f, 0.95f);
            float m = 8f;
            GUI.DrawTexture(new Rect(box.X + m, box.Y + m, box.W - m * 2f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.X + m, box.Y + box.H - m - 3f, box.W - m * 2f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.X + m, box.Y + m, 3f, box.H - m * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.X + box.W - m - 3f, box.Y + m, 3f, box.H - m * 2f), Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.85f, 0.35f, 1f);
            float dot = 8f;
            int drawn = 0;
            for (int s = 0; s < Tag.Couch.CouchPlay.Max; s++)
            {
                if (!Tag.Couch.CouchPlay.HumanAt(s) && !Tag.Couch.CouchPlay.AiAt(s)) continue;
                Tag.Couch.CouchPlay.Tint(s, out float pr, out float pg, out float pb);
                float u = Tag.Couch.CouchPlay.X(s) * 0.02f;
                float v = Tag.Couch.CouchPlay.Z(s) * 0.02f;
                if (u < -0.35f) u = -0.35f;
                if (u > 0.35f) u = 0.35f;
                if (v < -0.35f) v = -0.35f;
                if (v > 0.35f) v = 0.35f;
                if (u == 0f && v == 0f)
                {
                    u = (s - 1.5f) * 0.18f;
                    v = 0.05f;
                }
                float dx = box.X + box.W * (0.5f + u) - dot * 0.5f;
                float dy = box.Y + box.H * (0.55f + v);
                GUI.color = new Color(pr, pg, pb, 1f);
                GUI.DrawTexture(new Rect(dx, dy, dot, dot), Texture2D.whiteTexture);
                GUI.Label(new Rect(dx + dot, dy - 2f, box.W * 0.4f, 16f), Tag.Couch.CouchPlay.Name(s));
                drawn++;
            }
            if (drawn == 0)
            {
                GUI.color = new Color(0.95f, 0.85f, 0.35f, 1f);
                GUI.DrawTexture(new Rect(box.X + box.W * 0.5f - dot * 0.5f, box.Y + box.H * 0.55f, dot, dot), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            string name = GameSettings.ArenaName(GameSettings.Current.Arena);
            GUI.Label(new Rect(box.X + 10f, box.Y + 8f, box.W - 20f, 22f), name);
            GUI.color = prevColor;
            if (scaled) GUI.matrix = prev;
        }

        static bool ShowNow()
        {
            if (GameFlow.Instance != null)
                return GameFlow.Instance.State == GameFlowState.Play;
            if (TagModeController.Instance != null)
                return TagModeController.Instance.Phase == MatchPhase.Playing;
            return false;
        }
    }
}
