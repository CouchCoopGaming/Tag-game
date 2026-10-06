using Tag.Local;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Play opens on Mega Park with the solo pawn, one DummyRunner, and the HUD.
    /// The campus graybox is built only when Mega Park does not come up.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class PlayEntry : MonoBehaviour
    {
        void Awake()
        {
            EnsureMap();
        }

        void Start()
        {
            EnsureSlice();
        }

        void EnsureMap()
        {
            MegaParkP1Bootstrap park = FindFirstObjectByType<MegaParkP1Bootstrap>();
            if (park != null && park.Built)
                return;

            if (park != null)
                park.ClearBuilt();

            if (FindFirstObjectByType<CutArenaBootstrap>() == null)
            {
                var go = new GameObject("CampusFallback");
                go.AddComponent<CutArenaBootstrap>();
            }
        }

        void EnsureSlice()
        {
            if (FindFirstObjectByType<ParkMinimap>() == null)
                gameObject.AddComponent<ParkMinimap>();

            if (FindFirstObjectByType<LocalPlayerSpawner>() == null)
            {
                var go = new GameObject("LocalMultiplayer");
                go.AddComponent<LocalPlayerSpawner>();
                go.AddComponent<LocalSplitCamera>();
            }

            GameObject player = GameObject.Find(LocalPlayerSpawner.SoloPawnName);
            if (player != null)
            {
                if (!player.activeSelf)
                    player.SetActive(true);
                SpeedEnergyHUD hud = player.GetComponent<SpeedEnergyHUD>();
                if (hud == null)
                    hud = player.AddComponent<SpeedEnergyHUD>();
                if (hud.motor == null)
                    hud.motor = player.GetComponent<PlayerMotor>();
            }

            if (LocalPlayerRoster.IsCouch)
                return;

            GameObject dummy = GameObject.Find(LocalPlayerSpawner.OpponentPawnName);
            if (dummy != null && !dummy.activeSelf)
                dummy.SetActive(true);
        }
    }
}
