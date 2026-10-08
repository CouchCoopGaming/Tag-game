using Tag.Audio;
using Tag.Onboard;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Move, confirm, and back use the existing UI bus. No new clips.
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
        }

        public static void Back()
        {
            TagSfx.UiBack();
        }

        public static void NoteDevice(InputDeviceKind kind)
        {
            ControlGlyphs.Note(kind);
        }
    }
}
