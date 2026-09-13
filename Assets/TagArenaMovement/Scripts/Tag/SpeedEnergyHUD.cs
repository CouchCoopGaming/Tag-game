using UnityEngine;

namespace TagArena.Movement
{
    public class SpeedEnergyHUD : MonoBehaviour
    {
        public PlayerMotor motor;
        GUIStyle _big;
        GUIStyle _small;

        void OnGUI()
        {
            if (!motor) return;
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _big.normal.textColor = Color.white;
                _small.normal.textColor = new Color(0.85f, 0.9f, 1f);
            }

            float hs = motor.HorizSpeed;
            string kph = (hs * 3.6f).ToString("0");
            GUI.Label(new Rect(24, 20, 480, 36), kph + " km/h   " + motor.State, _big);

            float maxE = motor.cfg.jetEnergyMax + 0.001f;
            float e = Mathf.Clamp01(motor.Energy / maxE);
            GUI.Box(new Rect(24, 62, 220, 18), GUIContent.none);
            GUI.Box(new Rect(24, 62, 220 * e, 18), GUIContent.none);
            GUI.Label(new Rect(24, 82, 420, 24), "JET  " + motor.Energy.ToString("0") + "   ski " + (motor.Skiing ? "ON" : "off"), _small);

            if (motor.SuperGlideT >= 0f)
                GUI.Label(new Rect(24, 108, 280, 24), "GLIDE WINDOW", _big);
        }
    }
}
