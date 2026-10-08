namespace Tag.Audio
{
    /// <summary>
    /// Menu music and stinger slots. Each name points at a clip that is
    /// already in the project. No new paid audio.
    /// </summary>
    public static class MenuCue
    {
        public const int Count = 9;

        public static readonly string[] Name =
        {
            "menu music",
            "menu move",
            "menu confirm",
            "menu back",
            "menu join",
            "menu error",
            "menu ready",
            "menu start",
            "menu results"
        };

        public static readonly string[] Slot =
        {
            "Music/music_playground_bed_loop.wav",
            "UI/ui_move.wav",
            "UI/ui_confirm.wav",
            "UI/ui_back.wav",
            "UI/ui_click.wav",
            "SFX/sfx_round_lose.wav",
            "SFX/sfx_round_win.wav",
            "SFX/sfx_round_start.wav",
            "SFX/sfx_round_win.wav"
        };

        public static int Present(string root)
        {
            if (string.IsNullOrEmpty(root) || Slot.Length != Count || Name.Length != Count) return 0;
            int n = 0;
            for (int i = 0; i < Count; i++)
            {
                if (string.IsNullOrEmpty(Slot[i])) continue;
                string path = System.IO.Path.Combine(root, "Assets", "Audio", Slot[i].Replace('/', System.IO.Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(path)) n++;
            }
            return n;
        }
    }
}
