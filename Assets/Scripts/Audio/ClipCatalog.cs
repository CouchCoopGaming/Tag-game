namespace Tag.Audio
{
    /// <summary>
    /// The 20 AudioBus hooks and the baked clip each one plays.
    /// File order matches AudioBus.Hook. The synth in Tools/AudioSynth writes the bytes.
    /// </summary>
    public static class ClipCatalog
    {
        public static readonly string[] Names =
        {
            "jump",
            "land soft",
            "land hard",
            "slide start",
            "slide loop",
            "slide end",
            "cling grab",
            "wall jump",
            "air dash",
            "punch whiff",
            "punch hit",
            "tag",
            "tag-back blocked",
            "stagger",
            "pad launch",
            "zip grab",
            "zip loop",
            "zip drop",
            "countdown beep",
            "round end"
        };

        public static readonly string[] Files =
        {
            "SFX/sfx_jump.wav",
            "SFX/sfx_land_soft.wav",
            "SFX/sfx_land_hard.wav",
            "SFX/sfx_slide_start.wav",
            "SFX/sfx_slide_loop.wav",
            "SFX/sfx_slide_end.wav",
            "SFX/sfx_cling.wav",
            "SFX/sfx_wall_jump.wav",
            "SFX/sfx_air_dash.wav",
            "SFX/sfx_punch_whiff.wav",
            "SFX/sfx_punch_hit.wav",
            "SFX/sfx_tag_transfer.wav",
            "SFX/sfx_tagback.wav",
            "SFX/sfx_stagger.wav",
            "SFX/sfx_pad.wav",
            "SFX/sfx_zip_grab.wav",
            "SFX/sfx_zip_loop.wav",
            "SFX/sfx_zip_drop.wav",
            "SFX/sfx_countdown.wav",
            "SFX/sfx_round_end.wav"
        };

        public static readonly int[] Priorities =
        {
            VoiceBudget.PriJump,
            VoiceBudget.PriLandSoft,
            VoiceBudget.PriLandHard,
            VoiceBudget.PriSlide,
            VoiceBudget.PriSlide,
            VoiceBudget.PriLandSoft,
            VoiceBudget.PriCling,
            VoiceBudget.PriWallJump,
            VoiceBudget.PriDash,
            VoiceBudget.PriWhiff,
            VoiceBudget.PriPunch,
            VoiceBudget.PriTag,
            VoiceBudget.PriTag,
            VoiceBudget.PriStagger,
            VoiceBudget.PriPad,
            VoiceBudget.PriZip,
            VoiceBudget.PriZipLoop,
            VoiceBudget.PriZip,
            VoiceBudget.PriRound,
            VoiceBudget.PriRound
        };

        public static readonly float[] Volumes =
        {
            0.46f, 0.34f, 0.58f, 0.40f, 0.22f, 0.20f, 0.42f, 0.50f, 0.48f, 0.30f,
            0.64f, 0.56f, 0.36f, 0.44f, 0.52f, 0.40f, 0.18f, 0.34f, 0.44f, 0.50f
        };

        public static bool Flat(int hook)
        {
            if (hook < 0 || hook >= Names.Length) return false;
            return Names[hook] == "countdown beep" || Names[hook] == "round end";
        }

        public static int Count => Names.Length;
    }
}
