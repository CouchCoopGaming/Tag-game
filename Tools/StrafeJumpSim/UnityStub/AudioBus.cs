using System;

namespace Tag.Audio
{
    /// <summary>
    /// Headless stand-in for the gameplay bus. The sim counts raises.
    /// Playback stays in the Unity AudioBus.
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

        public static event Action<Hook, UnityEngine.Vector3> Raised;

        public static void Raise(Hook hook, UnityEngine.Vector3 position)
        {
            if (Raised != null) Raised(hook, position);
        }

        public static void RaiseRoundEnd(string message)
        {
            if (Raised != null) Raised(Hook.RoundEnd, UnityEngine.Vector3.zero);
        }
    }
}
