using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Attaches the kit without editing the pawn spawn or the other FX hosts.
    /// The roster scan waits between passes.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class FxKitDriver : MonoBehaviour
    {
        static FxKitDriver _live;
        float _fastUntil;
        int _wait;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_live != null) return;
            var go = new GameObject("FxKit");
            DontDestroyOnLoad(go);
            _live = go.AddComponent<FxKitDriver>();
        }

        void Awake()
        {
            FxKitStore.Load();
            if (GetComponent<FxKitMenu>() == null)
                gameObject.AddComponent<FxKitMenu>();
            _fastUntil = Time.unscaledTime + 4f;
            _wait = 0;
        }

        void LateUpdate()
        {
            _wait--;
            if (_wait > 0) return;
            int found = FxKitRoster.AttachMissing();
            float now = Time.unscaledTime;
            _wait = now < _fastUntil || found <= 0 ? 10 : 120;
        }
    }
}
