using Tag.Settings;
using UnityEngine;

namespace Tag.Couch
{
    /// <summary>
    /// The fourth quadrant when three humans are seated. Minimap and the seat list.
    /// Strings are the cached seat lines. Nothing here is built per repaint.
    /// </summary>
    public class CouchScoreHud : MonoBehaviour
    {
        GUIStyle _style;

        void WarmStyle()
        {
            if (_style == null) BootStyle();
        }

        void BootStyle()
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _style.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            if (Tag.Ui.Hud.MatchHud.Active) return;
            WarmStyle();
            if (CouchPlay.Humans != 3) return;
            int split = GameSettings.Current != null ? GameSettings.Current.SplitAxis : GameSettings.SplitVertical;
            CouchPlay.Norm(3, 3, split, out float nx, out float ny, out float nw, out float nh);
            float sw = Screen.width;
            float sh = Screen.height;
            float x = nx * sw;
            float y = sh - (ny + nh) * sh;
            float w = nw * sw;
            float h = nh * sh;
            Color prev = GUI.color;
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
            GUI.DrawTexture(new Rect(x + 12f, y + 12f, w - 24f, h - 24f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 24f, y + 20f, w - 48f, 24f), "Score", _style);
            float yy = y + 52f;
            for (int i = 0; i < CouchPlay.Max; i++)
            {
                if (!CouchPlay.HumanAt(i) && !CouchPlay.AiAt(i)) continue;
                CouchPlay.Tint(i, out float r, out float g, out float b);
                _style.normal.textColor = new Color(r, g, b, 1f);
                string line = CouchPlay.ScoreText(i);
                if (line.Length == 0) line = CouchPlay.SeatLine(i);
                GUI.Label(new Rect(x + 24f, yy, w - 48f, 22f), line, _style);
                yy += 24f;
            }
            string tie = CouchPlay.TieText;
            if (tie.Length > 0)
            {
                _style.normal.textColor = Color.white;
                GUI.Label(new Rect(x + 24f, yy, w - 48f, 22f), tie, _style);
            }
            _style.normal.textColor = Color.white;
            GUI.color = prev;
        }
    }
}
