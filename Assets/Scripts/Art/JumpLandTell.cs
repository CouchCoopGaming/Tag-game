using Tag.Local;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Impact read for the solo pawn when a jump or a real drop meets the ground.
    /// Presentation only. A short body thud and two heel puffs.
    /// Arms only after <see cref="MinAirSeconds"/> off the ground, which is longer
    /// than the 0.10s coyote window, so a probe flicker or a coyote re-ground stays quiet.
    /// The thud squash and the heel-dust alpha then scale with that air time.
    /// A long fall reads heavier than a short hop. The body stops at <see cref="ThudYFloor"/>
    /// and the dust stops at <see cref="AlphaCap"/>, so a very long fall does not pancake
    /// or hide the feet. A bunny-hop chain does not arm it.
    /// Slide enter, wall cling, and a mantle plant do not arm it.
    /// No lamp, no extra volume, no camera pop, no slow-mo.
    /// Does not change jump height, coyote, land speed, or the one kinematic step.
    /// </summary>
    public static class JumpLandTell
    {
        /// <summary>How long the thud and the puffs stay up. Inside 0.15–0.35s.</summary>
        public const float FlashSeconds = 0.24f;
        /// <summary>
        /// Continuous air required before a touchdown counts.
        /// Above coyote (0.10) so a re-ground inside that window stays quiet.
        /// A full jump is far longer than this.
        /// </summary>
        public const float MinAirSeconds = 0.22f;
        /// <summary>
        /// Air time that reaches the heavy cap. Longer stays there.
        /// The 0.22s gate stays the light end, so a short hop does not
        /// hit as hard as a long fall.
        /// </summary>
        public const float HeavyAirSeconds = 1.25f;
        /// <summary>Body height at the impact frame for a landing on the air gate. 1 is the pose scale.</summary>
        public const float ThudY = 0.76f;
        /// <summary>Body height at the impact frame once air reaches <see cref="HeavyAirSeconds"/>. Never lower.</summary>
        public const float ThudYFloor = 0.62f;
        /// <summary>Body width at the impact frame for a gate landing. 1 is the pose scale.</summary>
        public const float ThudXZ = 1.14f;
        /// <summary>Body width at the impact frame for a heavy landing. Stays short of a blob.</summary>
        public const float ThudXZCap = 1.20f;
        /// <summary>Peak puff opacity for a gate landing. The legs stay readable.</summary>
        public const float MaxAlpha = 0.58f;
        /// <summary>Peak puff opacity once air reaches <see cref="HeavyAirSeconds"/>. Still under the feet.</summary>
        public const float AlphaCap = 0.66f;
        public const int PuffCount = 2;
        /// <summary>Disc diameter at the impact frame.</summary>
        public const float PuffTight = 0.16f;
        /// <summary>Disc diameter as the puff blooms out.</summary>
        public const float PuffWide = 0.32f;
        /// <summary>Meters above the pawn origin. On the ground, at the heels.</summary>
        public const float PuffHeight = 0.06f;
        /// <summary>Lateral split from the center line. Inside the capsule, at the feet.</summary>
        public const float FootOffset = 0.15f;
        /// <summary>Meters behind the origin along travel, so the puffs sit at the heels.</summary>
        public const float HeelBack = 0.18f;
        /// <summary>Dry heel dust. Not the punch rings and not a white flash.</summary>
        public const float MarkR = 0.86f;
        public const float MarkG = 0.72f;
        public const float MarkB = 0.46f;
        /// <summary>No bloom. The chase camera must not wash out on the land.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "JumpLandTell";

        /// <summary>Outer span of the pair, including the wide puff. Stays at the feet.</summary>
        public static float PairSpan => FootOffset * 2f + PuffWide;

        /// <summary>Solo human only. Couch pawns and the campus opponent do not.</summary>
        public static bool ForPawn(bool couch, bool ai, int index, string pawnName)
        {
            return SoloGrappleGate.EnableFor(couch, ai, index, pawnName);
        }

        /// <summary>
        /// Track air time and arm on a landing edge.
        /// A slide, a wall cling, or a mantle clears the cue and does not arm.
        /// Leaving the ground clears a cue that was still playing.
        /// Tiny air and a coyote re-ground do not arm.
        /// </summary>
        public static void Note(
            ref float age,
            ref float airTime,
            bool solo,
            bool grounded,
            bool wasGrounded,
            bool slide,
            bool cling,
            float dt)
        {
            float weight = 0f;
            Note(ref age, ref airTime, solo, grounded, wasGrounded, slide, cling, dt, ref weight);
        }

        /// <summary>
        /// Same as the other Note. When the landing arms, <paramref name="weight"/>
        /// becomes <see cref="Weight"/> of the air that was just consumed.
        /// Standing on that landing leaves the weight alone so the thud can play out.
        /// A slide, a cling, leaving the ground, or a pawn that is not solo clears it.
        /// </summary>
        public static void Note(
            ref float age,
            ref float airTime,
            bool solo,
            bool grounded,
            bool wasGrounded,
            bool slide,
            bool cling,
            float dt,
            ref float weight)
        {
            if (!solo)
            {
                age = -1f;
                airTime = 0f;
                weight = 0f;
                return;
            }

            if (slide || cling)
            {
                age = -1f;
                weight = 0f;
                if (grounded)
                    airTime = 0f;
                else if (dt > 0f)
                    airTime += dt;
                return;
            }

            if (!grounded)
            {
                age = -1f;
                weight = 0f;
                if (dt > 0f) airTime += dt;
                return;
            }

            if (!wasGrounded && airTime >= MinAirSeconds)
            {
                age = 0f;
                weight = Weight(airTime);
            }

            airTime = 0f;
        }

        /// <summary>Drop the cue.</summary>
        public static void Clear(ref float age) => age = -1f;

        /// <summary>True only for the solo pawn, inside the short window.</summary>
        public static bool Show(bool solo, float age)
        {
            return solo && age >= 0f && age < FlashSeconds;
        }

        /// <summary>1 at the impact, 0 when the window closes.</summary>
        public static float Fade(float age)
        {
            if (age < 0f || age >= FlashSeconds) return 0f;
            return 1f - (age / FlashSeconds);
        }

        /// <summary>
        /// 0 on the air gate, 1 once air reaches <see cref="HeavyAirSeconds"/>.
        /// Under the gate stays 0. Longer than the cap stays 1.
        /// </summary>
        public static float Weight(float airSeconds)
        {
            float span = HeavyAirSeconds - MinAirSeconds;
            if (span <= 0.0001f) return 1f;
            float t = (airSeconds - MinAirSeconds) / span;
            return Mathf.Clamp01(t);
        }

        /// <summary>Gate-landing dust. A heavy landing uses <see cref="Alpha(float, float)"/>.</summary>
        public static float Alpha(float age) => Alpha(age, 0f);

        /// <summary>
        /// Heel-dust opacity. Weight 0 is the gate hop. Weight 1 is the long-fall cap.
        /// The window still fades it out.
        /// </summary>
        public static float Alpha(float age, float weight)
        {
            float peak = Mathf.Lerp(MaxAlpha, AlphaCap, Mathf.Clamp01(weight));
            return peak * Fade(age);
        }

        /// <summary>
        /// Extra scale on the pose for a gate landing.
        /// Fade 1 is the light thud. Fade 0 leaves the pose alone.
        /// </summary>
        public static void ThudScale(float fade, out float y, out float xz)
        {
            ThudScale(fade, 0f, out y, out xz);
        }

        /// <summary>
        /// Extra scale on the pose. Weight 0 is the gate hop. Weight 1 is the long-fall cap.
        /// Fade 1 is the impact. Fade 0 leaves the pose alone. Y never goes under <see cref="ThudYFloor"/>.
        /// </summary>
        public static void ThudScale(float fade, float weight, out float y, out float xz)
        {
            float w = Mathf.Clamp01(weight);
            float yHit = Mathf.Lerp(ThudY, ThudYFloor, w);
            float xzHit = Mathf.Lerp(ThudXZ, ThudXZCap, w);
            float f = Mathf.Clamp01(fade);
            y = Mathf.Lerp(1f, yHit, f);
            xz = Mathf.Lerp(1f, xzHit, f);
        }

        /// <summary>Tight at the impact, wider as the puff dies.</summary>
        public static float PuffDiameter(float fade) => Mathf.Lerp(PuffWide, PuffTight, fade);

        /// <summary>
        /// Heel puffs. Left and right are across travel, behind the origin.
        /// A zero direction returns false so the caller can skip the frame.
        /// </summary>
        public static bool Heels(Vector3 origin, Vector3 travel, out Vector3 left, out Vector3 right)
        {
            Vector3 dir = travel;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
            {
                left = origin + Vector3.up * PuffHeight;
                right = left;
                return false;
            }

            dir.Normalize();
            Vector3 side = FlatSide(dir);
            Vector3 heel = origin - dir * HeelBack + Vector3.up * PuffHeight;
            left = heel - side * FootOffset;
            right = heel + side * FootOffset;
            return true;
        }

        /// <summary>Cross(up, dir) for a flattened direction: (dir.z, 0, -dir.x).</summary>
        static Vector3 FlatSide(Vector3 dir)
        {
            Vector3 side = new Vector3(dir.z, 0f, -dir.x);
            if (side.sqrMagnitude < 1e-6f)
                return Vector3.right;
            side.Normalize();
            return side;
        }

        /// <summary>
        /// Advance one frame. Returns false once the window has closed.
        /// A pawn that is not the solo human clears and stays quiet.
        /// </summary>
        public static bool Step(ref float age, float dt, bool solo)
        {
            if (!Show(solo, age))
            {
                age = -1f;
                return false;
            }

            if (dt > 0f) age += dt;
            if (!Show(solo, age))
            {
                age = -1f;
                return false;
            }

            return true;
        }

        /// <summary>
        /// The air gate, the heavy cap, and the squash and dust limits stay in range.
        /// A longer fall is a deeper thud and a brighter puff, and neither crosses the cap.
        /// </summary>
        public static bool ScaleHolds()
        {
            if (Mathf.Abs(MinAirSeconds - 0.22f) > 0.001f) return false;
            if (HeavyAirSeconds <= MinAirSeconds + 0.50f || HeavyAirSeconds > 2.50f) return false;
            if (ThudY < 0.68f || ThudY >= 0.90f) return false;
            if (ThudYFloor < 0.60f || ThudYFloor > 0.66f || ThudYFloor >= ThudY) return false;
            if (ThudXZ <= 1.06f || ThudXZ > 1.24f) return false;
            if (ThudXZCap <= ThudXZ || ThudXZCap > 1.24f) return false;
            if (MaxAlpha < 0.40f || MaxAlpha > 0.70f) return false;
            if (AlphaCap <= MaxAlpha || AlphaCap > 0.70f) return false;
            if (Mathf.Abs(FlashSeconds - 0.24f) > 0.001f) return false;
            if (VerticalImpulse != 0f || Glow != 0f) return false;
            if (PuffCount != 2) return false;

            if (Weight(MinAirSeconds) > 0.0001f) return false;
            if (Weight(MinAirSeconds - 0.05f) > 0.0001f) return false;
            if (Weight(0f) > 0.0001f) return false;
            if (Mathf.Abs(Weight(HeavyAirSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Weight(HeavyAirSeconds + 4f) - 1f) > 0.0001f) return false;
            float midAir = (MinAirSeconds + HeavyAirSeconds) * 0.5f;
            float midW = Weight(midAir);
            if (midW <= 0.35f || midW >= 0.65f) return false;
            if (Weight(MinAirSeconds + 0.20f) >= midW) return false;
            if (Weight(HeavyAirSeconds - 0.20f) <= midW) return false;

            ThudScale(1f, 0f, out float hopY, out float hopXZ);
            ThudScale(1f, 1f, out float fallY, out float fallXZ);
            ThudScale(1f, Weight(HeavyAirSeconds + 4f), out float longY, out float longXZ);
            ThudScale(1f, midW, out float midY, out float midXZ);
            ThudScale(0f, 1f, out float restY, out float restXZ);
            ThudScale(0.5f, 1f, out float easeY, out float easeXZ);
            if (Mathf.Abs(hopY - ThudY) > 0.001f || Mathf.Abs(hopXZ - ThudXZ) > 0.001f) return false;
            if (Mathf.Abs(fallY - ThudYFloor) > 0.001f || Mathf.Abs(fallXZ - ThudXZCap) > 0.001f) return false;
            if (Mathf.Abs(longY - ThudYFloor) > 0.001f || Mathf.Abs(longXZ - ThudXZCap) > 0.001f) return false;
            if (longY < ThudYFloor - 0.001f || fallY < ThudYFloor - 0.001f) return false;
            if (midY <= fallY || midY >= hopY || midXZ <= hopXZ || midXZ >= fallXZ) return false;
            if (Mathf.Abs(restY - 1f) > 0.001f || Mathf.Abs(restXZ - 1f) > 0.001f) return false;
            if (easeY <= fallY || easeY >= 1f || easeXZ >= fallXZ || easeXZ <= 1f) return false;

            if (Mathf.Abs(Alpha(0f) - MaxAlpha) > 0.001f) return false;
            if (Mathf.Abs(Alpha(0f, 0f) - MaxAlpha) > 0.001f) return false;
            if (Mathf.Abs(Alpha(0f, 1f) - AlphaCap) > 0.001f) return false;
            if (Alpha(0f, Weight(HeavyAirSeconds + 4f)) > AlphaCap + 0.001f) return false;
            float midA = Alpha(0f, midW);
            if (midA <= MaxAlpha || midA >= AlphaCap) return false;
            if (Alpha(FlashSeconds, 1f) != 0f || Alpha(-1f, 1f) != 0f) return false;
            if (Alpha(FlashSeconds) != 0f || Alpha(-1f) != 0f) return false;
            return true;
        }

        public static string ProofLine()
        {
            ThudScale(1f, 0f, out float hopY, out float hopXZ);
            ThudScale(1f, 1f, out float fallY, out float fallXZ);
            float midAir = (MinAirSeconds + HeavyAirSeconds) * 0.5f;
            float midW = Weight(midAir);
            ThudScale(1f, midW, out float midY, out _);
            return "land-scale"
                + " gate=" + MinAirSeconds.ToString("0.00")
                + " heavy=" + HeavyAirSeconds.ToString("0.00")
                + " wGate=" + Weight(MinAirSeconds).ToString("0.00")
                + " wMid=" + midW.ToString("0.00")
                + " wFall=" + Weight(HeavyAirSeconds + 2f).ToString("0.00")
                + " hopY=" + hopY.ToString("0.00")
                + " midY=" + midY.ToString("0.00")
                + " fallY=" + fallY.ToString("0.00")
                + " floor=" + ThudYFloor.ToString("0.00")
                + " hopXZ=" + hopXZ.ToString("0.00")
                + " fallXZ=" + fallXZ.ToString("0.00")
                + " hopAlpha=" + Alpha(0f, 0f).ToString("0.00")
                + " fallAlpha=" + Alpha(0f, 1f).ToString("0.00")
                + " ceil=" + AlphaCap.ToString("0.00")
                + " visual=1";
        }
    }
}
