using Tag.Core;
using Tag.Couch;
using Tag.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Highlight sweep on a tile, and a short pad pulse on join and confirm.
    /// Reduce motion and a rumble of 0 leave the motors alone.
    /// Keyboard seats never write a motor.
    /// </summary>
    public static class MenuJuice
    {
        public static void AddSweep(MenuTile tile)
        {
            if (tile == null || tile.Sweep != null) return;
            RectTransform root = tile.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 400f;
            float h = root != null ? root.sizeDelta.y : 88f;
            float sw = w * 0.22f;
            if (sw < 28f) sw = 28f;
            RectTransform rt = MenuWidgets.Place(root, "Sweep", -sw, h * 0.22f, sw, h * 0.5f);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 0.96f, 0.75f, 0.22f);
            img.raycastTarget = false;
            tile.Sweep = rt;
            tile.SweepSpan = w * 0.55f;
            rt.gameObject.SetActive(false);
        }

        public static void Tick(float dt)
        {
            PadRumbleOutput.Tick(dt);
        }

        public static void Buzz(int ev)
        {
            if (MenuVideo.ReduceMotion) return;
            int device = MenuInput.LastDevice;
            int seat = -1;
            for (int s = 0; s < CouchPlay.Max; s++)
            {
                if (!CouchPlay.HumanAt(s)) continue;
                if (CouchPlay.DeviceOf(s) != device) continue;
                seat = s;
                break;
            }
            if (seat < 0) return;
            Pulse(seat, ev);
        }

        public static void Pulse(int seat, int ev)
        {
            if (MenuVideo.ReduceMotion) return;
            if (seat < 0 || seat >= PadRumble.Seats) return;
            GameSettings settings = GameSettings.Current;
            int intensity = settings != null ? settings.RumbleOf(seat) : 0;
            if (intensity <= 0) return;
            int device = CouchPlay.DeviceOf(seat);
            if (device <= CouchPlay.DeviceKeyboard) return;
            if (!InMatch())
                PadRumble.Bind(seat, 9100 + seat, device);
            PadRumble.Fire(seat, ev, intensity);
        }

        static bool InMatch()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null) return false;
            GameFlowState state = flow.State;
            return state == GameFlowState.Play
                || state == GameFlowState.Paused
                || state == GameFlowState.RoundEnd
                || state == GameFlowState.Rematch;
        }
    }
}
