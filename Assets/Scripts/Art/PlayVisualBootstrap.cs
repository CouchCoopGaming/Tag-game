using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>Ensures DummyAvatarBinder exists on every ItController at runtime.</summary>
    public static class PlayVisualBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            foreach (var it in Object.FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (it.GetComponent<DummyAvatarBinder>() == null)
                    it.gameObject.AddComponent<DummyAvatarBinder>();
            }
        }
    }
}
