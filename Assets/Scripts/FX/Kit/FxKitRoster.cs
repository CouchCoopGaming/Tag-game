using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Finds pawns that do not have the kit yet. The scan is not every frame.
    /// </summary>
    public static class FxKitRoster
    {
        public static int AttachMissing()
        {
            PlayerMotor[] motors = Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None);
            int added = 0;
            for (int i = 0; i < motors.Length; i++)
            {
                PlayerMotor motor = motors[i];
                if (motor == null) continue;
                FxKitHost host = motor.GetComponent<FxKitHost>();
                if (host == null)
                {
                    host = motor.gameObject.AddComponent<FxKitHost>();
                    added++;
                }
                host.BindCamera();
            }
            return motors.Length;
        }
    }
}
