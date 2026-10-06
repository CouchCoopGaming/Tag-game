using TagArena.Movement;

namespace Tag.Settings
{
    /// <summary>
    /// Jump buffer 0.16 and coyote 0.10 on one clock for Space and the pad.
    /// The edge helper is the same function the motor uses. The window does not
    /// branch on which device closed it.
    /// </summary>
    public static class InputBufferAudit
    {
        public const float Coyote = 0.10f;
        public const float Buffer = 0.16f;
        public const float JumpSpeed = 24.7f;
        public const float Dt = 1f / 60f;

        public static bool DevicesMatch(out string detail)
        {
            if (!CoyoteFrame(true, out bool kbFire, out bool kbMiss, out float kbElapsed))
            {
                detail = "keyboard coyote did not sample";
                return false;
            }
            if (!CoyoteFrame(false, out bool padFire, out bool padMiss, out float padElapsed))
            {
                detail = "gamepad coyote did not sample";
                return false;
            }
            if (!kbFire || !padFire || !kbMiss || !padMiss)
            {
                detail = "coyote window diverged";
                return false;
            }
            if (!Near(kbElapsed, Coyote) || !Near(padElapsed, Coyote) || !Near(kbElapsed, padElapsed))
            {
                detail = "coyote time diverged";
                return false;
            }

            BufferSlot(true, 9, out float kb9, out bool kb9Fire);
            BufferSlot(false, 9, out float pad9, out bool pad9Fire);
            BufferSlot(true, 10, out float kb10, out bool kb10Fire);
            BufferSlot(false, 10, out float pad10, out bool pad10Fire);
            if (!kb9Fire || !pad9Fire || kb10Fire || pad10Fire)
            {
                detail = "buffer window diverged";
                return false;
            }
            if (!Near(kb9, pad9) || !Near(kb10, pad10))
            {
                detail = "buffer slot diverged";
                return false;
            }
            if (!(kb9 > 0f) || !(pad9 > 0f) || kb10 > 0f || pad10 > 0f)
            {
                detail = "buffer edge was not 0.16";
                return false;
            }
            if (!Near(JumpSpeed, 24.7f))
            {
                detail = "jump speed diverged";
                return false;
            }
            detail = "coyote=0.10 jump=0.16 kb=pad";
            return true;
        }

        public static bool ResumeClean(out string detail)
        {
            HoldSample held = MenuHoldGate.WhileOpen(true, true, true);
            HoldResult released = MenuHoldGate.OnResume(held, false, false, false);
            if (released.Cling || released.Sprint || released.JumpEdge)
            {
                detail = "released cling or sprint stuck, or jump fired";
                return false;
            }

            HoldResult still = MenuHoldGate.OnResume(held, true, true, true);
            if (!still.Cling || !still.Sprint || still.JumpEdge)
            {
                detail = "held cling or sprint dropped, or a held jump fired";
                return false;
            }

            HoldSample closed = MenuHoldGate.WhileOpen(false, false, false);
            HoldResult resumeUp = MenuHoldGate.OnResume(closed, false, false, true);
            if (resumeUp.JumpEdge || resumeUp.Cling || resumeUp.Sprint)
            {
                detail = "resume frame fired a jump";
                return false;
            }

            HoldResult later = MenuHoldGate.Live(false, true, false, false);
            if (!later.JumpEdge)
            {
                detail = "a press after resume did not jump";
                return false;
            }
            HoldResult repeat = MenuHoldGate.Live(true, true, false, false);
            if (repeat.JumpEdge)
            {
                detail = "a held jump retriggered";
                return false;
            }
            detail = "resume=clean";
            return true;
        }

        static bool CoyoteFrame(bool keyboard, out bool fired, out bool missed, out float elapsed)
        {
            bool edge = keyboard
                ? KinematicStep.JumpEdge(true, true, false, false)
                : KinematicStep.JumpEdge(true, false, false, false);
            float coyote = Coyote;
            float t = 0f;
            fired = false;
            missed = false;
            elapsed = 0f;
            int window = -1;
            for (int frame = 1; frame <= 12; frame++)
            {
                coyote = KinematicStep.DecayCoyote(coyote, Dt);
                t += Dt;
                bool allow = edge && KinematicStep.CoyoteJumpAllowed(false, coyote);
                if (window < 0 && t + 0.00001f >= Coyote)
                {
                    window = frame;
                    fired = allow;
                    elapsed = t;
                }
                else if (frame == window + 1)
                {
                    missed = !allow;
                    break;
                }
            }
            return window > 0;
        }

        static void BufferSlot(bool keyboard, int decays, out float slot, out bool fired)
        {
            bool edge = keyboard
                ? KinematicStep.JumpEdge(true, true, false, false)
                : KinematicStep.JumpEdge(true, false, false, false);
            bool heldAgain = keyboard
                ? KinematicStep.JumpEdge(true, true, true, true)
                : KinematicStep.JumpEdge(true, false, true, false);
            slot = Buffer;
            for (int i = 0; i < decays; i++)
            {
                if (slot > 0f) slot -= Dt;
            }
            fired = edge && !heldAgain && slot > 0f && KinematicStep.CoyoteJumpAllowed(true, 0f)
                && Near(JumpSpeed, 24.7f);
        }

        static bool Near(float a, float b)
        {
            float d = a - b;
            if (d < 0f) d = -d;
            return d <= 0.0001f;
        }
    }
}
