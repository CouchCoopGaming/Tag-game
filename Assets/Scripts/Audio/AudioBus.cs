using UnityEngine;

namespace Tag.Audio
{
    /// <summary>
    /// One named hook per gameplay sound. Placeholders are the procedural tones
    /// and CC0 clips already resolved by TagSfx. Mute and master volume gate playback.
    /// Subscribers hear the hook after the placeholder decision. They should not
    /// start a second copy of the same clip.
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
                case Hook.Jump: TagSfx.PlayFlat(TagSfx.Jump, 0.4f); break;
                case Hook.LandSoft: TagSfx.LandAt(position, 0.28f); break;
                case Hook.LandHard: TagSfx.LandAt(position, 0.48f); break;
                case Hook.SlideStart: TagSfx.PlayAt(TagSfx.Slide, position, 0.4f); break;
                case Hook.SlideLoop: TagSfx.PlayAt(TagSfx.Slide, position, 0.18f); break;
                case Hook.SlideEnd: TagSfx.LandAt(position, 0.16f); break;
                case Hook.ClingGrab: TagSfx.PlayAt(TagSfx.Thunk, position, 0.32f); break;
                case Hook.WallJump: TagSfx.LungeWhoosh(position); break;
                case Hook.AirDash: TagSfx.PlayAirDash(position); break;
                case Hook.PunchWhiff: TagSfx.PunchMiss(position); break;
                case Hook.PunchHit: TagSfx.PunchConnect(position); break;
                case Hook.Tag: TagSfx.BecomeIt(position); break;
                case Hook.TagBackBlocked: TagSfx.TagBackThunk(position); break;
                case Hook.Stagger: TagSfx.PlayAt(TagSfx.Thunk, position, 0.3f); break;
                case Hook.PadLaunch: TagSfx.PlayAt(TagSfx.Jump, position, 0.46f); break;
                case Hook.ZipGrab: TagSfx.PlayAt(TagSfx.AirDash, position, 0.36f); break;
                case Hook.ZipLoop: TagSfx.PlayAt(TagSfx.AirDash, position, 0.16f); break;
                case Hook.ZipDrop: TagSfx.LandAt(position, 0.22f); break;
                case Hook.CountdownBeep: TagSfx.CountdownBeep(); break;
                case Hook.RoundEnd: TagSfx.RoundEnd(); break;
            }
        }
    }
}
