using Tag.Audio;
using Tag.Couch;
using Tag.Front;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using Tag.Settings;
using Tag.Trail;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Core
{
    /// <summary>
    /// Domain reload off keeps statics. SubsystemRegistration runs before the next play session
    /// and puts them back to the boot values. Awake still loads saved settings after this.
    /// </summary>
    public static class StaticLifecycle
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad()
        {
            ResetAll();
        }

        /// <summary>
        /// Drops the match roster and the front-end screen. Domain reload still
        /// runs ResetAll, which calls this before the other statics.
        /// </summary>
        public static void ReleaseMatch()
        {
            FrontSession.ResetStatics();
            CouchPlay.Release();
            FrontLive.Reset();
        }

        public static void ResetAll()
        {
            ReleaseMatch();
            ResumeInputGate.Reset();
            GameFlow.ResetStatics();
            TagModeController.ResetStatics();
            AudioCuePlayer.ResetStatics();
            AudioMaster.ResetStatics();
            ZipLine.ResetStatics();
            LaunchPad.ResetStatics();
            ItController.ResetPawnIds();
            TagRole.ResetPawnIds();
            PlayerMotor.ResetColliderIndex();
            TrailSegment.ResetStatics();
            SettingsMenuUi.ResetStatics();
            PadNav.ResetStatics();
            MinimapHud.ResetStatics();
            Tag.Onboard.PlayPromptHud.ResetStatics();
            FrameMeter.ResetStatics();
            Tag.Modes.EnemyAi.ResetLoopSearch();
            Tag.Art.TagBackBlockedTell.ResetStatics();
            SessionRules.ResetRound();
            AudioMix.SetWorldPaused(false);
            PadRumble.Silence();
        }
    }
}
