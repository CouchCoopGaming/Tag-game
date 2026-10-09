using UnityEngine;

namespace Tag.Core
{
    /// <summary>
    /// F6 shows FPS, frame time, and the frame-budget buckets. Off until that key.
    /// F3 stays Trail Tag. The key is not a gameplay bind, so rebind does not sample it.
    /// </summary>
    public class FrameBudgetOverlay : MonoBehaviour
    {
        GUIStyle _label;
        int _seenFrame = -1;

        void WarmStyle()
        {
            if (_label == null) BootStyle();
        }

        void BootStyle()
        {
            _label = new GUIStyle(GUI.skin.label);
            _label.fontSize = 14;
            _label.fontStyle = FontStyle.Bold;
            _label.normal.textColor = Color.white;
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F6))
                FrameMeter.Overlay = !FrameMeter.Overlay;
        }

        void OnGUI()
        {
            WarmStyle();
            if (Time.frameCount != _seenFrame)
            {
                _seenFrame = Time.frameCount;
                if (FrameMeter.Overlay)
                    FrameMeter.Close(Time.unscaledDeltaTime);
                else
                    FrameMeter.Discard();
            }
            if (!FrameMeter.Overlay || _label == null) return;
            if (Event.current != null && Event.current.type != EventType.Repaint) return;

            float x = Screen.width - 292f;
            float y = 16f;
            GUI.Box(new Rect(x, y, 276f, 196f), "Frame");
            _label.normal.textColor = Color.white;
            Row(x, y + 28f, "FPS", HudDigits.Whole0(FrameMeter.Fps));
            Row(x, y + 50f, "MS", HudDigits.Tenth0(FrameMeter.LastMs));
            Row(x, y + 72f, "MOVE", HudDigits.Whole0(FrameMeter.LastMove));
            Row(x, y + 94f, "AI", HudDigits.Whole0(FrameMeter.LastAi));
            Row(x, y + 116f, "POSE", HudDigits.Whole0(FrameMeter.LastPose));
            Row(x, y + 138f, "HUD", HudDigits.Whole0(FrameMeter.LastHud));
            Row(x, y + 160f, "AUDIO", HudDigits.Whole0(FrameMeter.LastAudio));
            Row(x, y + 182f, "ROUND", HudDigits.Whole0(FrameMeter.LastRound));
        }

        void Row(float x, float y, string name, string value)
        {
            GUI.Label(new Rect(x + 12f, y, 90f, 20f), name, _label);
            GUI.Label(new Rect(x + 110f, y, 150f, 20f), value, _label);
        }
    }
}
