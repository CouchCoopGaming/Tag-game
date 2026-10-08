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
            TagSfx.UiMove();
        }

        public static void Confirm()
        {
            TagSfx.UiConfirm();
            MenuJuice.Buzz(PadRumble.PunchHit);
        }

        public static void Back()
        {
            TagSfx.UiBack();
        }

        public static void Join()
        {
            TagSfx.UiClick();
            MenuJuice.Buzz(PadRumble.PadLaunch);
        }

        public static void Error()
        {
            TagSfx.RoundLose();
        }

        public static void Ready()
        {
            TagSfx.RoundWin();
        }

        public static void StartMatch()
        {
            TagSfx.RoundStart();
        }

        public static void EnsureBed()
        {
            AudioCuePlayer.Ensure()?.PlaygroundMusic();
        }

        public static void NoteDevice(InputDeviceKind kind)
        {
            ControlGlyphs.Note(kind);
        }
    }
}
