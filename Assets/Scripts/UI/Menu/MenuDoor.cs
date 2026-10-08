using Tag.Core;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Drop on Boot or MainMenu. Play reaches the same door from the
    /// after-scene hook when a match has not been armed.
    /// </summary>
    public sealed class MenuDoor : MonoBehaviour
    {
        void Awake()
        {
            if (GameFlow.Instance != null) return;
            var go = new GameObject("GameFlow");
            go.AddComponent<GameFlow>();
        }

        void Start()
        {
            if (MenuHost.Legacy) return;
            MenuHost host = MenuHost.Ensure();
            if (host == null) return;
            if (host.Screen == MenuScreenId.Hidden)
                host.ShowTitle();
        }
    }
}
