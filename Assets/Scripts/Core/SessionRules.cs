using Tag.Gameplay;
using Tag.Level;
using TagArena.Movement;

namespace Tag.Core
{
    /// <summary>
    /// Shared decisions for pause, tag handoff, stagger, round end, and the kill box.
    /// The headless sweep steps these. The motor and the respawn call the same ones.
    /// Feel numbers stay in MovementConfig and the tuning assets.
    /// </summary>
    public static class SessionRules
    {
        public const float ParkGrayW = 72f;
        public const float ParkGrayD = 54f;
        public const float ParkWorldScale = 10f;
        public const float Margin = 4f;

        public struct Clocks
        {
            public float ClingGrace;
            public float JumpSlot;
            public float WallJumpSlot;
            public float Coyote;
            public float AirDash;
            public float AirDashCd;
            public float Lunge;
            public float LungeCd;
            public float Stagger;
            public float StaggerImmune;
            public bool Zip;
            public bool Arc;
            public bool Slide;
            public bool Climb;
        }

        public struct ArenaBox
        {
            public int Arena;
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;
            public float KillY;
        }

        /// <summary>timeScale 0 freezes the pawn. It does not clear a buffer or a verb.</summary>
        public static bool TimeFrozen(float timeScale) => timeScale <= 0f;

        /// <summary>
        /// True only in the playing phase. Countdown and results refuse a new zip,
        /// pad, or stagger, and a kill-box teleport does not grant punch i-frames.
        /// Pause leaves this set and freezes with timeScale instead, so a ride
        /// stays attached until the round actually leaves play.
        /// </summary>
        public static bool RoundPlay = true;

        public static bool NewCarrierAllowed() => RoundPlay;

        public static bool StaggerStarts() => RoundPlay;

        public static bool RespawnGrantsIFrames() => RoundPlay;

        public static void ResetRound()
        {
            RoundPlay = true;
        }

        public static Clocks Pause(Clocks clocks) => clocks;

        public static Clocks Tick(Clocks clocks, float dt, bool frozen)
        {
            if (frozen || dt <= 0f) return clocks;
            clocks.ClingGrace = Decay(clocks.ClingGrace, dt);
            clocks.JumpSlot = Decay(clocks.JumpSlot, dt);
            clocks.WallJumpSlot = Decay(clocks.WallJumpSlot, dt);
            clocks.Coyote = KinematicStep.DecayCoyote(clocks.Coyote, dt);
            clocks.AirDash = Decay(clocks.AirDash, dt);
            clocks.AirDashCd = Decay(clocks.AirDashCd, dt);
            clocks.Lunge = Decay(clocks.Lunge, dt);
            clocks.LungeCd = Decay(clocks.LungeCd, dt);
            PunchStagger.Clock stagger;
            stagger.Stagger = clocks.Stagger;
            stagger.Immune = clocks.StaggerImmune;
            PunchStagger.Tick(ref stagger, dt);
            clocks.Stagger = stagger.Stagger;
            clocks.StaggerImmune = stagger.Immune;
            return clocks;
        }

        public static bool Same(Clocks a, Clocks b)
        {
            if (!Near(a.ClingGrace, b.ClingGrace)) return false;
            if (!Near(a.JumpSlot, b.JumpSlot)) return false;
            if (!Near(a.WallJumpSlot, b.WallJumpSlot)) return false;
            if (!Near(a.Coyote, b.Coyote)) return false;
            if (!Near(a.AirDash, b.AirDash)) return false;
            if (!Near(a.AirDashCd, b.AirDashCd)) return false;
            if (!Near(a.Lunge, b.Lunge)) return false;
            if (!Near(a.LungeCd, b.LungeCd)) return false;
            if (!Near(a.Stagger, b.Stagger)) return false;
            if (!Near(a.StaggerImmune, b.StaggerImmune)) return false;
            if (a.Zip != b.Zip || a.Arc != b.Arc || a.Slide != b.Slide || a.Climb != b.Climb) return false;
            return true;
        }

        /// <summary>The victim leaves the cable, the pad arc, the lunge, and the dash. The attacker keeps the ride.</summary>
        public static void TagCarriers(ref VerbIntegration.Carrier attacker, ref VerbIntegration.Carrier victim)
        {
            VerbIntegration.TransferCarriers(ref attacker, ref victim);
        }

        public static bool StaggerBlocksZip(float stagger)
        {
            return VerbIntegration.ZipGrabBlocked(stagger > 0f, false, false, false, false, false, false);
        }

        /// <summary>A stumble owns the body. A new air dash does not start.</summary>
        public static bool AirDashAllowed(float stagger)
        {
            return stagger <= 0f;
        }

        /// <summary>Round end closes the window. The next round does not inherit it.</summary>
        public static TagBackImmunity.Window OnRoundEnd(TagBackImmunity.Window window)
        {
            return default;
        }

        public static bool TagBackBlocks(TagBackImmunity.Window window, int attackerId, bool roundPlaying)
        {
            if (!roundPlaying) return false;
            return TagBackImmunity.Blocks(window, attackerId);
        }

        /// <summary>
        /// Arena 0 is the scaled PARK campus. Arena 1 is Mega Park at 1:1.
        /// Anything else uses the campus so a missing setting cannot inherit the park box.
        /// </summary>
        public static ArenaBox Bounds(int arena)
        {
            ArenaBox box;
            box.KillY = MegaParkP1Layout.KillPlaneY;
            box.Arena = arena == 1 ? 1 : 0;
            if (box.Arena == 1)
            {
                box.MinX = -Margin;
                box.MaxX = MegaParkP1Layout.MapW + Margin;
                box.MinZ = -Margin;
                box.MaxZ = MegaParkP1Layout.MapD + Margin;
                return box;
            }

            box.MinX = -Margin;
            box.MaxX = ParkGrayW * ParkWorldScale + Margin;
            box.MinZ = -Margin;
            box.MaxZ = ParkGrayD * ParkWorldScale + Margin;
            return box;
        }

        public static bool Outside(ArenaBox box, float x, float y, float z)
        {
            if (y < box.KillY) return true;
            if (x < box.MinX || x > box.MaxX || z < box.MinZ || z > box.MaxZ) return true;
            return false;
        }

        /// <summary>A respawn is not still on the cable.</summary>
        public static bool RidingAfterRespawn(bool wasRiding) => false;

        /// <summary>
        /// Practice restart and a kill-box snap drop the ride. Feel clocks are
        /// zeroed here. The durations in the tuning assets stay put.
        /// </summary>
        public static Clocks ClearCarriers(Clocks clocks)
        {
            clocks.ClingGrace = 0f;
            clocks.JumpSlot = 0f;
            clocks.WallJumpSlot = 0f;
            clocks.AirDash = 0f;
            clocks.Lunge = 0f;
            clocks.Stagger = 0f;
            clocks.StaggerImmune = 0f;
            clocks.Zip = false;
            clocks.Arc = false;
            clocks.Slide = false;
            clocks.Climb = false;
            return clocks;
        }

        static float Decay(float value, float dt)
        {
            if (value <= 0f) return 0f;
            value -= dt;
            return value < 0f ? 0f : value;
        }

        static bool Near(float a, float b) => a > b - 0.0001f && a < b + 0.0001f;
    }
}
