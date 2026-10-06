using UnityEngine;

namespace Tag.Audio
{
    /// <summary>
    /// One named hook per gameplay sound. Each hook plays its own baked clip.
    /// World cues are spatial. Countdown and round end are flat.
    /// Mute and master volume gate playback. Subscribers should not start a second copy.
    /// </summary>
    public static class AudioBus
    {
        public enum Hook
        {
            Jump,
            LandSoft,
            LandHard,
            SlideStart,
            SlideLoop,
            SlideEnd,
            ClingGrab,
            WallJump,
            AirDash,
            PunchWhiff,
            PunchHit,
            Tag,
            TagBackBlocked,
            Stagger,
            PadLaunch,
            ZipGrab,
            ZipLoop,
            ZipDrop,
            CountdownBeep,
            RoundEnd
        }

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

        public static event System.Action<Hook, Vector3> Raised;

        public static string Name(Hook hook)
        {
            int i = (int)hook;
            if (i < 0 || i >= Names.Length) return "unknown";
            return Names[i];
        }

        public static bool Audible()
        {
            return !AudioMaster.Muted && AudioMaster.Volume > 0.001f;
        }

        public static void Raise(Hook hook, Vector3 position)
        {
            if (Raised != null) Raised.Invoke(hook, position);
            if (!Audible()) return;
            Play(hook, position);
        }

        /// <summary>One round-end hook. Win, lose, and draw pick the existing clip.</summary>
        public static void RaiseRoundEnd(string message)
        {
            if (Raised != null) Raised.Invoke(Hook.RoundEnd, Vector3.zero);
            if (!Audible()) return;
            var msg = (message ?? "").ToLowerInvariant();
            if (msg.Contains("no winner"))
                TagSfx.RoundEnd();
            else if (msg.Contains("winner") || msg.Contains(" win"))
                TagSfx.RoundWin();
            else if (msg.Contains("lose") || msg.Contains("loss"))
                TagSfx.RoundLose();
            else
                TagSfx.RoundEnd();
        }

        static void Play(Hook hook, Vector3 position)
        {
            switch (hook)
            {
                case Hook.SlideStart: TagSfx.PlayAt(TagSfx.Slide, position, 0.42f, VoiceBudget.PriSlide); break;
                case Hook.SlideLoop: TagSfx.PlayAt(TagSfx.SlideLoop, position, 0.22f, VoiceBudget.PriSlide); break;
                case Hook.PunchHit: TagSfx.PunchConnect(position); break;
                case Hook.Tag: TagSfx.BecomeIt(position); break;
                case Hook.TagBackBlocked: TagSfx.TagBackThunk(position); break;
                case Hook.CountdownBeep: TagSfx.CountdownBeep(); break;
                case Hook.RoundEnd: TagSfx.RoundEnd(); break;
                default: TagSfx.PlayHook(hook, position); break;
            }
        }
    }
}
