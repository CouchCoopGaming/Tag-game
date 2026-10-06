using System;
using System.IO;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Gameplay
{
    /// <summary>
    /// After It moves from pawn A to pawn B, B cannot tag A back for
    /// <see cref="DefaultSeconds"/> (the PunchTagTuning.tagBackImmunity field).
    /// Only that pair is closed. B can tag any other runner immediately.
    /// A punch that is not this tag-back still staggers. Punch-stagger immunity
    /// stays 0.50 s and is not written here.
    /// </summary>
    public static class TagBackImmunity
    {
        public const float DefaultSeconds = 1.0f;
        /// <summary>Soft body pulse. The envelope fades across the window.</summary>
        public const float PulseHz = 6.2f;
        const float TwoPi = 6.2831855f;

        public struct Window
        {
            public int FromId;
            public float Remaining;
            public float Duration;
        }

        public static float Seconds(PunchTagTuning tuning)
        {
            if (tuning == null) return DefaultSeconds;
            return tuning.tagBackImmunity < 0f ? 0f : tuning.tagBackImmunity;
        }

        public static Window Open(int newItId, float seconds)
        {
            float d = seconds > 0f ? seconds : 0f;
            return new Window { FromId = newItId, Remaining = d, Duration = d };
        }

        public static Window Tick(Window window, float dt)
        {
            if (dt < 0f) dt = 0f;
            if (window.Remaining <= 0f)
            {
                window.Remaining = 0f;
                return window;
            }
            window.Remaining -= dt;
            if (window.Remaining <= 0f)
            {
                window.Remaining = 0f;
                window.FromId = 0;
            }
            return window;
        }

        /// <summary>True only while this attacker is the specific new It who just took It.</summary>
        public static bool Blocks(Window window, int attackerId)
        {
            return attackerId != 0 && window.Remaining > 0f && window.FromId == attackerId;
        }

        /// <summary>The new It swung at the old It. No transfer and no stagger.</summary>
        public static bool IsBlockedTag(bool puncherIsIt, bool blocks)
        {
            return puncherIsIt && blocks;
        }

        /// <summary>A connect that is not a blocked tag-back still stumbles.</summary>
        public static bool IsStagger(bool blockedTag, bool staggerHit)
        {
            return !blockedTag && staggerHit;
        }

        public static float GlowEnvelope(Window window)
        {
            if (window.Duration <= 0.0001f || window.Remaining <= 0f) return 0f;
            return Mathf.Clamp01(window.Remaining / window.Duration);
        }

        /// <summary>0 outside the window. Inside, a soft pulse that fades with time left.</summary>
        public static float GlowPulse(Window window, float time)
        {
            float env = GlowEnvelope(window);
            if (env <= 0f) return 0f;
            float wave = 0.58f + 0.42f * Mathf.Sin(time * PulseHz * TwoPi);
            if (wave < 0.35f) wave = 0.35f;
            return env * wave;
        }

        public static bool GlowActive(Window window)
        {
            return GlowEnvelope(window) > 0.001f;
        }

        /// <summary>False means the new It drops this runner and picks the next, or holds.</summary>
        public static bool DummyKeepsTarget(bool blocksTagBack)
        {
            return !blocksTagBack;
        }

        /// <summary>Preferred is the old It. Other is the next runner. 0 is hold.</summary>
        public static int DummyPick(int preferred, int other, bool preferredBlocked, bool otherBlocked)
        {
            if (DummyKeepsTarget(preferredBlocked)) return preferred;
            if (other != 0 && DummyKeepsTarget(otherBlocked)) return other;
            return 0;
        }

        public static bool Holds()
        {
            if (Mathf.Abs(DefaultSeconds - 1.0f) > 0.001f) return false;
            if (Mathf.Abs(PunchStagger.Immunity - 0.50f) > 0.001f) return false;
            if (Mathf.Abs(PunchStagger.Duration - 0.25f) > 0.001f) return false;
            if (PunchStagger.Knockback != 0f) return false;

            PunchTagTuning tuning = ScriptableObject.CreateInstance<PunchTagTuning>();
            if (Mathf.Abs(tuning.tagBackImmunity - DefaultSeconds) > 0.001f) return false;
            if (Mathf.Abs(Seconds(tuning) - DefaultSeconds) > 0.001f) return false;
            if (Mathf.Abs(Seconds(null) - DefaultSeconds) > 0.001f) return false;
            if (Mathf.Abs(tuning.reach - 1.55f) > 0.001f) return false;

            const int A = 1;
            const int B = 2;
            const int C = 3;
            Window immune = Open(B, Seconds(tuning));
            Window other = default;

            Window atHalf = Tick(immune, 0.5f);
            if (!Blocks(atHalf, B)) return false;
            if (!IsBlockedTag(true, Blocks(atHalf, B))) return false;
            if (IsStagger(true, true)) return false;
            if (Blocks(atHalf, C)) return false;
            if (IsBlockedTag(false, Blocks(atHalf, B))) return false;
            if (!IsStagger(false, PunchStagger.IsStaggerHit(true, false))) return false;

            Window atEnd = Tick(immune, 1.05f);
            if (Blocks(atEnd, B)) return false;
            if (IsBlockedTag(true, Blocks(atEnd, B))) return false;

            Window otherSoon = Tick(other, 0.1f);
            if (Blocks(otherSoon, B)) return false;
            if (IsBlockedTag(true, Blocks(otherSoon, B))) return false;

            if (!GlowActive(atHalf)) return false;
            if (GlowPulse(atHalf, 0.2f) <= 0.001f) return false;
            if (GlowActive(atEnd)) return false;
            if (GlowPulse(atEnd, 0.2f) > 0.001f) return false;
            if (GlowPulse(immune, 0f) <= GlowPulse(atHalf, 0f) + 0.01f) return false;

            if (DummyKeepsTarget(Blocks(atHalf, B))) return false;
            if (!DummyKeepsTarget(Blocks(otherSoon, B))) return false;
            if (DummyPick(A, C, true, false) != C) return false;
            if (DummyPick(A, C, false, false) != A) return false;
            if (DummyPick(A, 0, true, true) != 0) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) return false;
            if (cfg.slideBoost != 0f) return false;
            if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f) return false;

            string lunge = Read("Assets/Scripts/Art/OpponentLungeTell.cs");
            if (lunge == null || lunge.IndexOf("LeadSeconds = 0.45f", StringComparison.Ordinal) < 0)
                return false;

            if (!Wired()) return false;
            return true;
        }

        public static string ProofLine()
        {
            return "no-tag-back"
                + " B->A blocked at 0.5 s"
                + " B->A allowed at 1.05 s"
                + " B->C allowed at 0.1 s"
                + " glow active window"
                + " dummy retargets"
                + " tagBackImmunity=" + DefaultSeconds.ToString("0.0");
        }

        static bool Wired()
        {
            string hit = Read("Assets/Scripts/Tag/PunchHitbox.cs");
            string it = Read("Assets/Scripts/Tag/ItController.cs");
            string mode = Read("Assets/Scripts/Modes/TagModeController.cs");
            string patrol = Read("Assets/Scripts/Modes/DummyPatrol.cs");
            string glow = Read("Assets/Scripts/Art/TagBackGlow.cs");
            string spark = Read("Assets/Scripts/Art/TagBackBlockedTell.cs");
            string role = Read("Assets/TagArenaMovement/Scripts/Tag/TagRole.cs");
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string hud = Read("Assets/TagArenaMovement/Scripts/Tag/SpeedEnergyHUD.cs");
            string tuning = Read("Assets/Scripts/Tag/PunchTagTuning.cs");
            string asset = Read("Assets/ScriptableObjects/PunchTagTuning.asset");
            string sfx = Read("Assets/Scripts/Audio/TagSfx.cs");
            if (hit == null || it == null || mode == null || patrol == null || glow == null
                || spark == null || role == null || motor == null || hud == null
                || tuning == null || asset == null || sfx == null)
                return false;

            int block = hit.IndexOf("IsBlockedTag", StringComparison.Ordinal);
            int stagger = hit.IndexOf("ReceivePunchStagger", StringComparison.Ordinal);
            int tell = hit.IndexOf("TagLandFlash.PlayOn", StringComparison.Ordinal);
            if (block < 0 || stagger < 0 || tell < 0 || block > stagger || block > tell) return false;
            if (hit.IndexOf("TagBackBlockedTell.PlayAt", StringComparison.Ordinal) < 0) return false;
            string audioBus = Read("Assets/Scripts/Audio/AudioBus.cs");
            bool thunk = hit.IndexOf("TagSfx.TagBackThunk", StringComparison.Ordinal) >= 0
                || (hit.IndexOf("TagBackBlocked", StringComparison.Ordinal) >= 0
                    && audioBus != null
                    && audioBus.IndexOf("TagSfx.TagBackThunk", StringComparison.Ordinal) >= 0);
            if (!thunk) return false;
            if (hit.IndexOf("BeginTagBackImmunity", StringComparison.Ordinal) < 0) return false;
            if (hit.IndexOf("victim.ReceiveTagHit", StringComparison.Ordinal) < 0) return false;

            if (it.IndexOf("BeginTagBackImmunity", StringComparison.Ordinal) < 0) return false;
            if (it.IndexOf("BlocksTagBackFrom", StringComparison.Ordinal) < 0) return false;
            if (it.IndexOf("TagBackImmunity.Tick", StringComparison.Ordinal) < 0) return false;
            if (it.IndexOf("TagBackGlow", StringComparison.Ordinal) < 0) return false;

            if (mode.IndexOf("BeginTagBackImmunity", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("DummyKeepsTarget", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("BlocksTagBackFrom", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("TagBackRemaining", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("TagLand", StringComparison.Ordinal) >= 0) return false;
            if (patrol.IndexOf(".Move(", StringComparison.Ordinal) >= 0) return false;

            if (glow.IndexOf("GlowPulse", StringComparison.Ordinal) < 0
                && glow.IndexOf("TagBackGlow01", StringComparison.Ordinal) < 0)
                return false;
            if (spark.IndexOf("DestroyImmediate", StringComparison.Ordinal) < 0) return false;
            if (role.IndexOf("BeginTagBackImmunity", StringComparison.Ordinal) < 0) return false;
            if (role.IndexOf("BlocksTagBackFrom", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("tagRole.Tag", StringComparison.Ordinal) < 0) return false;
            if (Count(motor, "_cc.Move(") != 1) return false;
            if (hud.IndexOf("no tag-back", StringComparison.Ordinal) < 0) return false;
            if (hud.IndexOf("TagBackRemaining", StringComparison.Ordinal) < 0) return false;
            if (tuning.IndexOf("tagBackImmunity", StringComparison.Ordinal) < 0) return false;
            if (asset.IndexOf("tagBackImmunity:", StringComparison.Ordinal) < 0) return false;
            if (asset.IndexOf("reach: 1.55", StringComparison.Ordinal) < 0) return false;
            if (sfx.IndexOf("TagBackThunk", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        static int Count(string hay, string needle)
        {
            int n = 0;
            int i = 0;
            while (i >= 0 && i < hay.Length)
            {
                i = hay.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0) break;
                n++;
                i += needle.Length;
            }
            return n;
        }
    }
}
