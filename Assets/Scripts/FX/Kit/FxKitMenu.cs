using Tag.Audio;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pause-only toggles. Number keys 1 through 7, or a click on the row.
    /// The 21-row settings list is not changed.
    /// </summary>
    public sealed class FxKitMenu : MonoBehaviour
    {
        Rect _title;
        Rect _row;

        void Awake()
        {
            _title = new Rect(24f, 64f, 300f, 22f);
            _row = new Rect(24f, 92f, 300f, 24f);
        }

        static void Flip(int slot)
        {
            FxKitOptions.Toggle(slot);
            FxKitStore.Save();
        }

        void OnGUI()
        {
            if (!AudioMix.WorldPaused) return;
            float y0 = Screen.height - 280f;
            if (y0 < 64f) y0 = 64f;
            _title.y = y0;
            GUI.Label(_title, FxKitOptions.Title);
            for (int i = 0; i < FxKitOptions.Count; i++)
            {
                _row.y = y0 + 28f + i * 26f;
                if (GUI.Button(_row, FxKitOptions.Label(i)))
                    Flip(i);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) Flip(0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) Flip(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) Flip(2);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) Flip(3);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) Flip(4);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha6)) Flip(5);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha7)) Flip(6);
        }
    }
}
