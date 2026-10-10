using System;
using System.IO;
using Tag.Level;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Headless check for the couch-screen slide. Keyboard and pad both skip.
    /// The clock is unscaled, so a pause (time scale 0) does not freeze it.
    /// </summary>
    public static class MenuTravel
    {
        public const string OkLine = "transitions=ok skip=ok";

        public static string Line(string repo)
        {
            return Holds(repo) ? OkLine : "transitions=no skip=no";
        }

        static bool Holds(string repo)
        {
            if (!Source(repo)) return false;
            if (!Counts()) return false;
            for (int kind = 0; kind < 2; kind++)
            {
                if (!Forward(kind, false)) return false;
                if (!Forward(kind, true)) return false;
                if (!Back(kind)) return false;
            }
            return PopSettles();
        }

        static bool Source(string repo)
        {
            string flow = Read(repo, "Assets/Scripts/UI/Menu/MenuFlow.cs");
            string host = Read(repo, "Assets/Scripts/UI/Menu/MenuHost.cs");
            string tile = Read(repo, "Assets/Scripts/UI/Menu/MenuTile.cs");
            string idle = Read(repo, "Assets/Scripts/UI/Menu/MenuIdle.cs");
            string card = Read(repo, "Assets/Scripts/UI/Menu/MenuArenaCard.cs");
            string audio = Read(repo, "Assets/Scripts/UI/Menu/MenuAudio.cs");
            string bus = Read(repo, "Assets/Scripts/Audio/AudioBus.cs");
            if (flow == null || host == null || tile == null || idle == null || card == null || audio == null || bus == null)
                return false;
            if (flow.IndexOf("SlideSeconds = 0.2f", StringComparison.Ordinal) < 0) return false;
            if (flow.IndexOf("u * u * (3f - 2f * u)", StringComparison.Ordinal) < 0) return false;
            if (flow.IndexOf("sign < 0f ? -160f : 160f", StringComparison.Ordinal) < 0) return false;
            if (flow.IndexOf("0.92f + 0.08f * e", StringComparison.Ordinal) < 0) return false;
            int feel = flow.IndexOf("public static bool Feel", StringComparison.Ordinal);
            int ease = flow.IndexOf("public static float Ease", StringComparison.Ordinal);
            if (feel < 0 || ease < feel) return false;
            string feelBody = flow.Substring(feel, ease - feel);
            if (feelBody.IndexOf("MenuScreenId.Title", StringComparison.Ordinal) < 0) return false;
            if (feelBody.IndexOf("MenuScreenId.Main", StringComparison.Ordinal) < 0) return false;
            if (feelBody.IndexOf("MenuScreenId.Cast", StringComparison.Ordinal) < 0) return false;
            if (feelBody.IndexOf("MenuScreenId.Rules", StringComparison.Ordinal) < 0) return false;
            if (feelBody.IndexOf("MenuScreenId.Arena", StringComparison.Ordinal) < 0) return false;
            if (feelBody.IndexOf("MenuScreenId.Results", StringComparison.Ordinal) < 0) return false;
            if (feelBody.IndexOf("Pause", StringComparison.Ordinal) >= 0) return false;
            if (feelBody.IndexOf("Options", StringComparison.Ordinal) >= 0) return false;
            if (feelBody.IndexOf("Controls", StringComparison.Ordinal) >= 0) return false;
            if (feelBody.IndexOf("Credits", StringComparison.Ordinal) >= 0) return false;
            if (feelBody.IndexOf("Records", StringComparison.Ordinal) >= 0) return false;
            if (host.IndexOf("_travel = _enterBack ? -1f : 1f", StringComparison.Ordinal) < 0) return false;
            if (host.IndexOf("Time.unscaledDeltaTime / MenuFlow.SlideSeconds", StringComparison.Ordinal) < 0) return false;
            if (host.IndexOf("MenuFlow.Travel(_slide, _travel", StringComparison.Ordinal) < 0) return false;
            int anim = host.IndexOf("void Animate()", StringComparison.Ordinal);
            int watch = host.IndexOf("void WatchMatch()", StringComparison.Ordinal);
            if (anim < 0 || watch < anim) return false;
            if (host.Substring(anim, watch - anim).IndexOf("Time.deltaTime", StringComparison.Ordinal) >= 0) return false;
            int skip = host.IndexOf("bool SkipTravel()", StringComparison.Ordinal);
            int tick = host.IndexOf("void TickShared()", StringComparison.Ordinal);
            if (skip < 0 || tick < skip) return false;
            string skipBody = host.Substring(skip, tick - skip);
            if (skipBody.IndexOf("_slide = 0f", StringComparison.Ordinal) < 0) return false;
            if (skipBody.IndexOf("Time.unscaledTime", StringComparison.Ordinal) < 0) return false;
            if (skipBody.IndexOf("HasTravelInput()", StringComparison.Ordinal) < 0) return false;
            if (!SliceHas(host, "void TickTitle()", "void TickJoin()", "SkipTravel()")) return false;
            if (!SliceHas(host, "void TickShared()", "void Activate()", "SkipTravel()")) return false;
            if (!SliceHas(host, "void TickCast()", "void TickRules()", "SkipTravel()")) return false;
            if (!SliceHas(host, "void TickRules()", "void TickArena()", "SkipTravel()")) return false;
            if (!SliceHas(host, "void TickArena()", "void TickLoading()", "SkipTravel()")) return false;
            if (SliceHas(host, "void TickPause()", "void TickOptions()", "SkipTravel()")) return false;
            if (!SliceHas(host, "void Retreat(", "static MenuScreenId SafeScreen", "_enterBack = true")) return false;
            if (tile.IndexOf("void PopSelect()", StringComparison.Ordinal) < 0) return false;
            if (tile.IndexOf("1.08f / rest", StringComparison.Ordinal) < 0) return false;
            if (tile.IndexOf("Time.unscaledDeltaTime / 0.2f", StringComparison.Ordinal) < 0) return false;
            if (idle.IndexOf("_hop = 1f", StringComparison.Ordinal) < 0) return false;
            if (idle.IndexOf("MenuPolish.Hop", StringComparison.Ordinal) < 0) return false;
            if (idle.IndexOf("dt / 0.36f", StringComparison.Ordinal) < 0) return false;
            if (idle.IndexOf("MenuAlive.Ready()", StringComparison.Ordinal) < 0) return false;
            if (idle.IndexOf("Time.unscaledDeltaTime", StringComparison.Ordinal) < 0) return false;
            if (card.IndexOf("StackYardLayout.MidY", StringComparison.Ordinal) < 0) return false;
            if (card.IndexOf("StackYardLayout.RoofY", StringComparison.Ordinal) < 0) return false;
            if (card.IndexOf("LoopCcw", StringComparison.Ordinal) < 0) return false;
            if (card.IndexOf("LaunchPads", StringComparison.Ordinal) < 0) return false;
            if (card.IndexOf("ZipLines", StringComparison.Ordinal) < 0) return false;
            if (card.IndexOf("A tight park", StringComparison.Ordinal) >= 0) return false;
            if (card.IndexOf("long loop", StringComparison.Ordinal) >= 0) return false;
            if (audio.IndexOf("RaiseMenu(AudioBus.MenuHook.Move)", StringComparison.Ordinal) < 0) return false;
            if (audio.IndexOf("RaiseMenu(AudioBus.MenuHook.Confirm)", StringComparison.Ordinal) < 0) return false;
            if (audio.IndexOf("RaiseMenu(AudioBus.MenuHook.Back)", StringComparison.Ordinal) < 0) return false;
            if (audio.IndexOf("RaiseMenu(AudioBus.MenuHook.Ready)", StringComparison.Ordinal) < 0) return false;
            if (audio.IndexOf("RaiseMenu(AudioBus.MenuHook.Start)", StringComparison.Ordinal) < 0) return false;
            if (bus.IndexOf("MenuHook.Move: TagSfx.UiMove()", StringComparison.Ordinal) < 0) return false;
            if (bus.IndexOf("MenuHook.Confirm: TagSfx.UiConfirm()", StringComparison.Ordinal) < 0) return false;
            if (bus.IndexOf("MenuHook.Back: TagSfx.UiBack()", StringComparison.Ordinal) < 0) return false;
            if (bus.IndexOf("MenuHook.Ready: TagSfx.RoundWin()", StringComparison.Ordinal) < 0) return false;
            if (bus.IndexOf("MenuHook.Start: TagSfx.RoundStart()", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static bool SliceHas(string text, string from, string to, string needle)
        {
            int a = text.IndexOf(from, StringComparison.Ordinal);
            int b = a < 0 ? -1 : text.IndexOf(to, a + from.Length, StringComparison.Ordinal);
            if (a < 0 || b < a) return false;
            return text.Substring(a, b - a).IndexOf(needle, StringComparison.Ordinal) >= 0;
        }

        static bool Counts()
        {
            if (MegaParkP1Layout.LaunchPads == null || MegaParkP1Layout.LaunchPads.Length != 5) return false;
            if (MegaParkP1Layout.ZipLines == null || MegaParkP1Layout.ZipLines.Length != 5) return false;
            if (PocketParkLayout.LaunchPads == null || PocketParkLayout.LaunchPads.Length != 2) return false;
            if (PocketParkLayout.ZipLines == null || PocketParkLayout.ZipLines.Length != 2) return false;
            if (StackYardLayout.LaunchPads == null || StackYardLayout.LaunchPads.Length != 3) return false;
            if (StackYardLayout.ZipLines == null || StackYardLayout.ZipLines.Length != 3) return false;
            if (Math.Abs(StackYardLayout.MidY - 6f) > 0.01f) return false;
            if (Math.Abs(StackYardLayout.RoofY - 12f) > 0.01f) return false;
            if (MegaParkP1Layout.LoopCcw == null || MegaParkP1Layout.LoopCcw.Length < 4) return false;
            if (PocketParkLayout.LoopCcw == null || PocketParkLayout.LoopCcw.Length < 4) return false;
            if (StackYardLayout.LoopCcw == null || StackYardLayout.LoopCcw.Length < 4) return false;
            return true;
        }

        static bool Forward(int kind, bool skip)
        {
            if (kind != 0 && kind != 1) return false;
            const float dur = 0.2f;
            float slide = 1f;
            Travel(slide, 1f, out float off0, out float scale0);
            if (off0 < 150f) return false;
            if (scale0 > 0.93f) return false;
            if (skip)
                slide = 0f;
            else
            {
                float t = 0f;
                const float step = 1f / 60f;
                while (t < dur - 0.0001f)
                {
                    float unscaled = step;
                    float timeScale = 0f;
                    float scaled = unscaled * timeScale;
                    if (scaled != 0f) return false;
                    t += unscaled;
                    slide = 1f - t / dur;
                    if (slide < 0f) slide = 0f;
                }
            }
            Travel(slide, 1f, out float off1, out float scale1);
            if (Math.Abs(off1) > 0.5f) return false;
            if (Math.Abs(scale1 - 1f) > 0.01f) return false;
            Travel(0.75f, 1f, out float eased, out _);
            if (eased < 128f) return false;
            return true;
        }

        static bool Back(int kind)
        {
            if (kind != 0 && kind != 1) return false;
            Travel(1f, -1f, out float off, out float scale);
            if (off > -150f) return false;
            if (scale > 0.93f) return false;
            Travel(0f, -1f, out float done, out float scale1);
            if (Math.Abs(done) > 0.5f) return false;
            if (Math.Abs(scale1 - 1f) > 0.01f) return false;
            return true;
        }

        static bool PopSettles()
        {
            float rest = MenuPolish.HotScale;
            float peak = 1.08f / rest;
            float start = rest * 1f;
            float top = rest * peak;
            float end = rest * 1f;
            if (Math.Abs(top - 1.08f) > 0.001f) return false;
            if (top <= start) return false;
            if (Math.Abs(end - rest) > 0.001f) return false;
            return true;
        }

        static void Travel(float slide, float sign, out float offset, out float scale)
        {
            if (slide < 0f) slide = 0f;
            if (slide > 1f) slide = 1f;
            float u = 1f - slide;
            float e = u * u * (3f - 2f * u);
            float from = sign < 0f ? -160f : 160f;
            offset = from * (1f - e);
            scale = 0.92f + 0.08f * e;
        }

        static string Read(string repo, string rel)
        {
            if (string.IsNullOrEmpty(repo)) return null;
            string path = Path.Combine(repo, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }
    }
}
