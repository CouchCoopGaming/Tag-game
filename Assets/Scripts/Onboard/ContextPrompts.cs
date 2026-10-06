using Tag.Level;
using UnityEngine;

namespace Tag.Onboard
{
    public enum ContextKind
    {
        None = 0,
        Cling = 1,
        Pad = 2,
        ZipGrab = 3,
        ZipDrop = 4
    }

    public struct ContextSample
    {
        public bool NearCling;
        public bool NearPad;
        public bool NearZip;
        public bool RidingZip;
        public bool DidCling;
        public bool DidPad;
        public bool DidZipGrab;
        public bool DidZipDrop;
    }

    /// <summary>
    /// Contextual lines. They hold, fade, then wait. After RetireAfter uses they stay quiet.
    /// Pads draw an icon. The jump word is not used for a pad.
    /// </summary>
    public sealed class ContextPrompts
    {
        public const int RetireAfter = 3;
        public const float HoldSeconds = 1.6f;
        public const float FadeSeconds = 0.4f;
        public const float ThrottleSeconds = 4f;
        public const float ClingClose = 1.6f;
        public const float PadRange = 2.2f;
        /// <summary>Approach band past the grab volume so the line is readable as you arrive.</summary>
        public const float ZipRange = ZipLineRules.GrabRadius + 1.65f;

        public readonly int[] Uses = new int[5];
        public ContextKind Kind;
        public float Alpha;
        public float Age;
        public float Lock;

        public bool BlocksInput => false;
        public bool Pauses => false;

        public static ContextKind Want(ContextSample sample)
        {
            if (sample.RidingZip) return ContextKind.ZipDrop;
            if (sample.NearZip) return ContextKind.ZipGrab;
            if (sample.NearCling) return ContextKind.Cling;
            if (sample.NearPad) return ContextKind.Pad;
            return ContextKind.None;
        }

        /// <summary>
        /// Planar pad test used by the live prompt and the three-arena proof.
        /// The radius is <see cref="PadRange"/>. Y is ignored, matching <c>LaunchPad.PromptNear</c>.
        /// </summary>
        public static bool PadNearLayout(int arena, float x, float z)
        {
            MegaParkP1Layout.PadSpot[] pads = PadsOf(arena);
            if (pads == null) return false;
            float r2 = PadRange * PadRange;
            for (int i = 0; i < pads.Length; i++)
            {
                float dx = pads[i].X - x;
                float dz = pads[i].Z - z;
                if (dx * dx + dz * dz <= r2) return true;
            }
            return false;
        }

        /// <summary>
        /// Cable distance test. The radius is <see cref="ZipRange"/>, the same band as <c>ZipLine.PromptNear</c>.
        /// </summary>
        public static bool ZipNearLayout(int arena, float x, float y, float z)
        {
            MegaParkP1Layout.ZipLineSpot[] zips = ZipsOf(arena);
            if (zips == null) return false;
            Vector3 pawn = new Vector3(x, y, z);
            for (int i = 0; i < zips.Length; i++)
            {
                MegaParkP1Layout.ZipLineSpot line = zips[i];
                Vector3 a = new Vector3(line.Ax, line.Ay, line.Az);
                Vector3 b = new Vector3(line.Bx, line.By, line.Bz);
                if (ZipLineRules.DistanceToSegment(pawn, a, b) <= ZipRange) return true;
            }
            return false;
        }

        static MegaParkP1Layout.PadSpot[] PadsOf(int arena)
        {
            if (arena == ParkArena.Pocket) return PocketParkLayout.LaunchPads;
            if (arena == ParkArena.Stack) return StackYardLayout.LaunchPads;
            return MegaParkP1Layout.LaunchPads;
        }

        static MegaParkP1Layout.ZipLineSpot[] ZipsOf(int arena)
        {
            if (arena == ParkArena.Pocket) return PocketParkLayout.ZipLines;
            if (arena == ParkArena.Stack) return StackYardLayout.ZipLines;
            return MegaParkP1Layout.ZipLines;
        }

        public bool Retired(ContextKind kind)
        {
            int i = (int)kind;
            if (i <= 0 || i >= Uses.Length) return false;
            return Uses[i] >= RetireAfter;
        }

        public void Tick(float dt, ContextSample sample)
        {
            if (sample.DidCling) Uses[(int)ContextKind.Cling]++;
            if (sample.DidPad) Uses[(int)ContextKind.Pad]++;
            if (sample.DidZipGrab) Uses[(int)ContextKind.ZipGrab]++;
            if (sample.DidZipDrop) Uses[(int)ContextKind.ZipDrop]++;

            if (dt < 0f) dt = 0f;
            if (Lock > 0f)
            {
                Lock -= dt;
                if (Lock < 0f) Lock = 0f;
            }

            if (Kind == ContextKind.None)
            {
                Alpha = 0f;
                if (Lock > 0f) return;
                ContextKind want = Want(sample);
                if (want == ContextKind.None || Retired(want)) return;
                Kind = want;
                Age = 0f;
                Alpha = 1f;
                return;
            }

            Age += dt;
            if (Age <= HoldSeconds)
            {
                Alpha = 1f;
                return;
            }

            float span = FadeSeconds > 0.0001f ? FadeSeconds : 0.0001f;
            float t = (Age - HoldSeconds) / span;
            if (t >= 1f)
            {
                Kind = ContextKind.None;
                Alpha = 0f;
                Age = 0f;
                Lock = ThrottleSeconds;
                return;
            }
            Alpha = 1f - t;
        }
    }
}
