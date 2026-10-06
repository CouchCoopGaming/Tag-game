using Tag.Settings;

namespace Tag.Onboard
{
    /// <summary>
    /// Cached sentences. Rebuilds when the bind table or the last-used device changes.
    /// </summary>
    public static class PromptText
    {
        static readonly string[] Hints = new string[OnboardingSession.StepCount];
        static string _cling = "";
        static string _zip = "";
        static string _drop = "";
        static ActionBinds _from;
        static int _rev = -1;
        static InputDeviceKind _dev;

        public static void ResetStatics()
        {
            _from = null;
            _rev = -1;
            _dev = InputDeviceKind.Keyboard;
            _cling = "";
            _zip = "";
            _drop = "";
            for (int i = 0; i < Hints.Length; i++) Hints[i] = null;
        }

        public static void Ensure()
        {
            ActionBinds binds = ActionBinds.Current;
            int rev = binds != null ? binds.Revision : 0;
            if (binds == _from && rev == _rev && ControlGlyphs.Device == _dev && Hints[0] != null)
                return;
            Rebuild(binds);
        }

        public static string Hint(int step)
        {
            Ensure();
            if (step < 0 || step >= Hints.Length) return "";
            return Hints[step] ?? "";
        }

        public static string ContextLine(ContextKind kind)
        {
            Ensure();
            if (kind == ContextKind.Cling) return _cling;
            if (kind == ContextKind.ZipGrab) return _zip;
            if (kind == ContextKind.ZipDrop) return _drop;
            return "";
        }

        public static bool IconOnly(ContextKind kind)
        {
            return kind == ContextKind.Pad;
        }

        static void Rebuild(ActionBinds binds)
        {
            _from = binds;
            _rev = binds != null ? binds.Revision : 0;
            _dev = ControlGlyphs.Device;
            InputDeviceKind dev = _dev;
            Hints[(int)HintStep.Move] = Bracket("Move", PlayAction.Move, binds, dev);
            Hints[(int)HintStep.Sprint] = Bracket("Sprint", PlayAction.Sprint, binds, dev);
            Hints[(int)HintStep.Jump] = Bracket("Jump", PlayAction.Jump, binds, dev);
            Hints[(int)HintStep.Slide] = Bracket("Slide", PlayAction.Slide, binds, dev);
            string cling = ControlGlyphs.GlyphOf(PlayAction.Cling, binds, dev);
            Hints[(int)HintStep.ClingClimb] = "Hold [" + cling + "] and climb";
            Hints[(int)HintStep.WallJump] = Bracket("Wall jump", PlayAction.Jump, binds, dev);
            Hints[(int)HintStep.AirDash] = Bracket("Air dash", PlayAction.AirDash, binds, dev);
            Hints[(int)HintStep.Punch] = Bracket("Punch / tag", PlayAction.Punch, binds, dev);
            _cling = "Hold [" + cling + "]";
            _zip = "Hold [" + cling + "] to grab";
            _drop = "Jump [" + ControlGlyphs.GlyphOf(PlayAction.Jump, binds, dev) + "] to drop";
        }

        static string Bracket(string name, PlayAction action, ActionBinds binds, InputDeviceKind device)
        {
            return name + "  [" + ControlGlyphs.GlyphOf(action, binds, device) + "]";
        }
    }
}
