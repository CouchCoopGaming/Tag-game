using UnityEngine;
using Tag.Gameplay;
using Tag.Modes;

namespace TagArena.Movement
{
    /// <summary>
    /// Cave-man OnGUI: speed, move state, jet fuel, ski on/off + P0 controls cheat-sheet
    /// + It / mode / Hot Potato fuse (reads TagModeController, falls back to ItController scan).
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
            "MMB lunge";

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
            if (motor.SuperGlideT >= 0f)
            {
                GUI.Label(new Rect(24, y, 280, 28), "GLIDE WINDOW", _big);
                y += 32f;
            }

            GUI.Label(new Rect(24, y, 220, 160), Controls, _keys);
            y += 132f;

            DrawMatchStatus(y);
        }

        void DrawMatchStatus(float y)
        {
            string modeName = "—";
            string itLabel = "—";
            string fuseLine = null;

            var modes = TagModeController.Instance;
            if (modes != null)
            {
                modeName = FriendlyModeName(modes.SelectedMode);
                var it = modes.CurrentIt;
                if (it == null)
                    it = ScanItControllers();
                itLabel = FormatIt(it);

                if (modes.SelectedMode == TagModeId.HotPotato)
                {
                    float rem = modes.Remaining;
                    fuseLine = rem > 0f
                        ? "Fuse " + rem.ToString("0.0") + "s"
                        : "Fuse —";
                }
            }
            else
            {
                var it = ScanItControllers();
                itLabel = FormatIt(it);
                modeName = "default";
            }

            GUI.Label(new Rect(24, y, 480, 22), "Mode " + modeName, _status);
            y += 22f;
            GUI.Label(new Rect(24, y, 480, 22), "It: " + itLabel, _status);
            y += 22f;
            if (fuseLine != null)
                GUI.Label(new Rect(24, y, 480, 22), fuseLine, _status);
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
            // Human P0: same GO as this HUD / motor, or has input reader and no DummyPatrol.
            if (it.gameObject == gameObject ||
                (motor != null && it.GetComponent<PlayerMotor>() == motor))
                return "YOU";
            if (it.GetComponent<PlayerInputReader>() != null && it.GetComponent<DummyPatrol>() == null)
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
