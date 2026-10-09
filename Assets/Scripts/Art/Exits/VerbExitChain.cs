namespace Tag.Art
{
    /// <summary>
    /// One exit hands the next exit a short ease, so a vault into a slide
    /// does not pop. Input still wins inside the cancel window.
    /// </summary>
    public static class VerbExitChain
    {
        public const float BlendSeconds = 0.08f;
        /// <summary>Degrees in one 60 Hz frame. A cancel of the roll stays under this. A snap does not.</summary>
        public const float StepBudget = 96f;
        public const float Frame = 1f / 60f;

        public static VerbExitSample Blend(VerbExitSample from, VerbExitSample to, float age)
        {
            float u = 1f;
            if (BlendSeconds > 0.0001f)
                u = age / BlendSeconds;
            return VerbExitSample.Lerp(from, to, PoseHandoff.Ease(u));
        }

        public static bool Holds()
        {
            if (BlendSeconds > 0.10f || BlendSeconds < 0.05f) return false;
            if (ChainStep(VerbExitId.Vault, VerbExitId.Slide) > StepBudget) return false;
            if (ChainStep(VerbExitId.WallJump, VerbExitId.WallRun) > StepBudget) return false;
            if (ChainStep(VerbExitId.Roll, VerbExitId.SoftLand) > StepBudget) return false;
            if (CancelRoll() > StepBudget) return false;
            for (int i = 0; i < VerbExitClock.Catalog.Length; i++)
            {
                if (ClipStep(VerbExitClock.Catalog[i]) > StepBudget) return false;
            }
            if (!Alive(VerbExitId.Vault)) return false;
            if (!Alive(VerbExitId.WallRun)) return false;
            if (!Alive(VerbExitId.Slide)) return false;
            if (!Alive(VerbExitId.Punch)) return false;
            if (!Alive(VerbExitId.SoftLand)) return false;
            VerbExitSample right = VerbExitClips.At(VerbExitId.Vault, 0.5f, 1f, false, false);
            VerbExitSample left = VerbExitClips.At(VerbExitId.Vault, 0.5f, 1f, false, true);
            if (VerbExitSample.Gap(right, left) < 20f) return false;
            VerbExitSample wallR = VerbExitClips.At(VerbExitId.WallRun, 0.5f, 1f, false, false);
            VerbExitSample wallL = VerbExitClips.At(VerbExitId.WallRun, 0.5f, 1f, false, true);
            if (wallL.HipRoll > 0f && wallR.HipRoll < 0f) return false;
            if (VerbExitSample.Gap(wallR, wallL) < 20f) return false;
            return true;
        }

        public static float ChainStep(VerbExitId from, VerbExitId to)
        {
            VerbExitSample prev = VerbExitClips.At(from, 0.55f, 1f, false, false);
            float max = 0f;
            float age = 0f;
            float dur = VerbExitClock.Duration(to);
            if (dur < 0.05f) dur = 0.05f;
            for (int i = 0; i < 8; i++)
            {
                age += Frame;
                float u = age / dur;
                if (u > 1f) u = 1f;
                VerbExitSample target = VerbExitClips.At(to, u, 1f, false, false);
                VerbExitSample next = Blend(prev, target, age);
                float step = VerbExitSample.MaxStep(prev, next);
                if (step > max) max = step;
                prev = next;
            }
            return max;
        }

        /// <summary>Jump during the roll peels the spin off inside the cancel window.</summary>
        public static float CancelRoll()
        {
            VerbExitSample pose = VerbExitClips.At(VerbExitId.Roll, 0.5f, 1f, false, false);
            float prevW = 1f;
            float max = 0f;
            VerbExitSample shown = Scale(pose, prevW);
            for (int i = 1; i <= 6; i++)
            {
                float w = VerbExitClock.CancelMul(i * Frame);
                VerbExitSample next = Scale(pose, w);
                float step = VerbExitSample.MaxStep(shown, next);
                if (step > max) max = step;
                shown = next;
                prevW = w;
            }
            return max;
        }

        public static float ClipStep(VerbExitId id)
        {
            float dur = VerbExitClock.Duration(id);
            if (dur < 0.05f) return 0f;
            int frames = (int)(dur / Frame);
            if (frames < 2) frames = 2;
            if (frames > 48) frames = 48;
            VerbExitSample prev = VerbExitClips.At(id, 0f, 1f, false, false);
            float max = 0f;
            for (int i = 1; i <= frames; i++)
            {
                float u = i / (float)frames;
                VerbExitSample next = VerbExitClips.At(id, u, 1f, false, false);
                float step = VerbExitSample.MaxStep(prev, next);
                if (step > max) max = step;
                prev = next;
            }
            return max;
        }

        static bool Alive(VerbExitId id)
        {
            VerbExitSample a = VerbExitClips.At(id, 0.12f, 1f, false, false);
            VerbExitSample b = VerbExitClips.At(id, 0.50f, 1f, false, false);
            VerbExitSample c = VerbExitClips.At(id, 0.88f, 1f, false, false);
            float arm = Range(a.ArmPitchL, b.ArmPitchL, c.ArmPitchL) + Range(a.ArmPitchR, b.ArmPitchR, c.ArmPitchR);
            float head = Range(a.Head, b.Head, c.Head);
            float weight = Range(a.HipYaw, b.HipYaw, c.HipYaw) + Range(a.HipRoll, b.HipRoll, c.HipRoll);
            return arm > 8f && head > 4f && weight > 4f;
        }

        static float Range(float a, float b, float c)
        {
            float lo = a;
            float hi = a;
            if (b < lo) lo = b;
            if (c < lo) lo = c;
            if (b > hi) hi = b;
            if (c > hi) hi = c;
            return hi - lo;
        }

        static VerbExitSample Scale(VerbExitSample s, float w)
        {
            return VerbExitSample.Lerp(default, s, w);
        }

        public static string ProofLine()
        {
            float vault = ChainStep(VerbExitId.Vault, VerbExitId.Slide);
            float wall = ChainStep(VerbExitId.WallJump, VerbExitId.WallRun);
            float roll = CancelRoll();
            float clip = ClipStep(VerbExitId.Roll);
            return "exit-chain"
                + " vault-slide=" + vault.ToString("0.0")
                + " walljump-wallrun=" + wall.ToString("0.0")
                + " roll-jump=" + roll.ToString("0.0")
                + " roll-clip=" + clip.ToString("0.0")
                + " budget=" + StepBudget.ToString("0");
        }
    }
}
