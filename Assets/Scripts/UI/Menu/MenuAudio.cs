using Tag.Audio;
using Tag.Onboard;
using Tag.Settings;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Move, confirm, back, join, and error use clips already on the bus.
    /// Confirm and join also pulse a pad when rumble is on.
    /// </summary>
    public static class MenuAudio
    {
        public static void Move()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Move);
        }

        public static void Confirm()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Confirm);
            MenuJuice.Buzz(PadRumble.PunchHit);
        }

        public static void Back()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Back);
        }

        public static void Join()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Join);
            MenuJuice.Buzz(PadRumble.PadLaunch);
        }

        public static void Error()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Error);
        }

        public static void Ready()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Ready);
        }

        public static void StartMatch()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Start);
        }

        public static void Results()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Results);
        }

        public static void EnsureBed()
        {
            AudioBus.RaiseMenu(AudioBus.MenuHook.Music);
        }

        public static void NoteDevice(InputDeviceKind kind)
        {
            ControlGlyphs.Note(kind);
        }
    }
}
