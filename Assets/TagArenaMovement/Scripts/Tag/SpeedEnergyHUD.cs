using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Cave-man OnGUI: speed, move state, jet fuel, ski on/off. Local human only.
    /// </summary>
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
                _small = new GUIStyle(GUI.skin.label) { fontSize = 18 };
                _big.normal.textColor = Color.white;
                _small.normal.textColor = new Color(0.85f, 0.9f, 1f);
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

            if (motor.SuperGlideT >= 0f)
                GUI.Label(new Rect(24, 110, 280, 28), "GLIDE WINDOW", _big);
        }
    }
}
