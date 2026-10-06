using System;
using System.Globalization;
using System.IO;
using Tag.Level;
using Tag.Settings;

namespace Tag.Audio
{
    public struct AudioReport
    {
        public bool Ok;
        public string Line;
        public string Failure;

        public void Fail(string why)
        {
            Ok = false;
            if (string.IsNullOrEmpty(Failure)) Failure = why;
        }
    }

    /// <summary>
    /// Headless check: 20 hooks each have a baked clip, the voice cap holds
    /// under a 4-pawn burst, the four surfaces map, and the four sliders round-trip.
    /// </summary>
    public static class AudioProof
    {
        public static AudioReport Run()
        {
            var report = new AudioReport { Ok = true };
            string root = FindRoot();
            int mapped = 0;
            int missing = 0;
            if (ClipCatalog.Count != 20 || ClipCatalog.Files.Length != 20 || ClipCatalog.Priorities.Length != 20
                || ClipCatalog.Volumes.Length != 20)
                report.Fail("hook catalog is not 20");

            string bus = Read(Path.Combine(root, "Assets", "Scripts", "Audio", "AudioBus.cs"));
            string doc = Read(Path.Combine(root, "Docs", "AudioHooks.md"));
            if (bus == null || doc == null)
                report.Fail("audio bus or hook doc is missing");
            else if (!NamesMatch(bus))
                report.Fail("bus names drifted from the clip catalog");

            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            if (doc != null)
            {
                for (int i = 0; i < ClipCatalog.Count; i++)
                {
                    string name = ClipCatalog.Names[i];
                    if (doc.IndexOf(name, StringComparison.Ordinal) < 0)
                        report.Fail("doc is missing " + name);
                    if (!seen.Add(ClipCatalog.Files[i]))
                        report.Fail("two hooks share " + ClipCatalog.Files[i]);
                    if (ClipCatalog.Volumes[i] <= 0f)
                        report.Fail(name + " has no level");
                    string path = Path.Combine(root, "Assets", "Audio", ClipCatalog.Files[i].Replace('/', Path.DirectorySeparatorChar));
                    if (!Audible(path, out string why))
                    {
                        missing++;
                        report.Fail(name + " " + why);
                    }
                    else
                    {
                        mapped++;
                        string mirror = path.Replace(
                            Path.Combine("Assets", "Audio"),
                            Path.Combine("Assets", "Resources", "Audio"));
                        if (!SameSize(path, mirror))
                            report.Fail(name + " is missing its Resources copy");
                    }
                }
            }

            bool extraOk = Extras(root, report);
            bool stepOk = Footsteps(root, report);
            if (!stepOk)
                report.Fail("per-surface footstep map is incomplete");
            bool voiceOk = Voices(report, out int peak);
            if (!voiceOk)
                report.Fail("voice cap did not hold");
            bool sliderOk = Sliders(report);
            if (!sliderOk)
                report.Fail("volume sliders did not persist");
            bool mixOk = MixWired(root, report);
            if (!mixOk)
                report.Fail("spatial mix is not wired");
            bool rideOk = PadZip(root);
            if (!rideOk)
                report.Fail("pad and zip cues are missing on pocket or stack");
            if (!extraOk || !stepOk || !voiceOk || !sliderOk || !mixOk || !rideOk)
                report.Ok = false;

            int cap = VoiceBudget.Cap;
            report.Line = "audio hooks=" + ClipCatalog.Count.ToString(CultureInfo.InvariantCulture)
                + " mapped=" + mapped.ToString(CultureInfo.InvariantCulture)
                + " missing=" + missing.ToString(CultureInfo.InvariantCulture)
                + " peakVoices=" + peak.ToString(CultureInfo.InvariantCulture)
                + " cap=" + cap.ToString(CultureInfo.InvariantCulture)
                + " peakVoices<=cap"
                + " footstep-map=" + (stepOk ? "ok" : "FAIL")
                + " sliders=" + (sliderOk ? "persist" : "FAIL");
            if (!report.Ok)
                report.Line += " FAIL " + report.Failure;
            return report;
        }

        static bool NamesMatch(string bus)
        {
            int a = bus.IndexOf("string[] Names", StringComparison.Ordinal);
            if (a < 0) return false;
            int b = bus.IndexOf("};", a, StringComparison.Ordinal);
            if (b < 0) return false;
            string block = bus.Substring(a, b - a);
            int found = 0;
            int i = 0;
            while (found < ClipCatalog.Count)
            {
                int q = block.IndexOf('"', i);
                if (q < 0) return false;
                int q2 = block.IndexOf('"', q + 1);
                if (q2 < 0) return false;
                string name = block.Substring(q + 1, q2 - q - 1);
                if (name != ClipCatalog.Names[found]) return false;
                found++;
                i = q2 + 1;
            }
            return found == 20;
        }

        static bool Extras(string root, AudioReport report)
        {
            string[] extra =
            {
                "SFX/sfx_climb_scuff.wav",
                "SFX/sfx_wallrun.wav",
                "SFX/sfx_round_start.wav",
                "SFX/sfx_round_tick.wav",
                "SFX/sfx_round_win.wav",
                "SFX/sfx_round_lose.wav",
                "UI/ui_move.wav",
                "UI/ui_confirm.wav",
                "UI/ui_back.wav"
            };
            bool ok = true;
            for (int i = 0; i < extra.Length; i++)
            {
                string path = Path.Combine(root, "Assets", "Audio", extra[i].Replace('/', Path.DirectorySeparatorChar));
                if (!Audible(path, out _))
                {
                    report.Fail("missing " + extra[i]);
                    ok = false;
                }
            }
            string doc = Read(Path.Combine(root, "Docs", "AudioHooks.md"));
            if (doc == null
                || doc.IndexOf("climb scuff", StringComparison.Ordinal) < 0
                || doc.IndexOf("wall-run patter", StringComparison.Ordinal) < 0
                || doc.IndexOf("last-10s", StringComparison.Ordinal) < 0
                || doc.IndexOf("ui move", StringComparison.Ordinal) < 0)
            {
                report.Fail("doc is missing an extra cue");
                ok = false;
            }
            return ok;
        }

        static bool Footsteps(string root, AudioReport report)
        {
            if (FootstepMap.SurfaceCount != 4) return false;
            if (FootstepMap.Classify("MEGA_concrete") != FootstepMap.Surface.Concrete) return false;
            if (FootstepMap.Classify("MEGA_sand") != FootstepMap.Surface.Concrete) return false;
            if (FootstepMap.Classify("MEGA_grass") != FootstepMap.Surface.Grass) return false;
            if (FootstepMap.Classify("MEGA_mulch") != FootstepMap.Surface.Grass) return false;
            if (FootstepMap.Classify("MEGA_steel") != FootstepMap.Surface.Metal) return false;
            if (FootstepMap.Classify("MEGA_fence") != FootstepMap.Surface.Metal) return false;
            if (FootstepMap.Classify("MEGA_wood") != FootstepMap.Surface.Wood) return false;
            if (FootstepMap.Classify("MEGA_cedar") != FootstepMap.Surface.Wood) return false;
            if (FootstepMap.FromSpeed(6f) != FootstepMap.Gait.Walk) return false;
            if (FootstepMap.FromSpeed(8.5f) != FootstepMap.Gait.Run) return false;
            if (FootstepMap.FromSpeed(12f) != FootstepMap.Gait.Sprint) return false;
            if (!(FootstepMap.Interval(FootstepMap.Gait.Walk) > FootstepMap.Interval(FootstepMap.Gait.Run)
                && FootstepMap.Interval(FootstepMap.Gait.Run) > FootstepMap.Interval(FootstepMap.Gait.Sprint)))
                return false;
            if (!(FootstepMap.Pitch(FootstepMap.Gait.Walk) < FootstepMap.Pitch(FootstepMap.Gait.Run)
                && FootstepMap.Pitch(FootstepMap.Gait.Run) < FootstepMap.Pitch(FootstepMap.Gait.Sprint)))
                return false;
            if (!(FootstepMap.Volume(FootstepMap.Gait.Walk) < FootstepMap.Volume(FootstepMap.Gait.Run)
                && FootstepMap.Volume(FootstepMap.Gait.Run) < FootstepMap.Volume(FootstepMap.Gait.Sprint)))
                return false;
            if (FootstepMap.PitchJitter <= 0f || FootstepMap.VolumeJitter <= 0f) return false;
            if (VoiceBudget.ItFootstepGain <= 1.05f || VoiceBudget.ItFootstepGain > 1.5f) return false;
            if (Math.Abs(VoiceBudget.HearMeters - 40f) > 0.01f) return false;
            if (!ArenaFooting()) return false;

            var files = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < 4; i++)
            {
                var surface = (FootstepMap.Surface)i;
                string rel = FootstepMap.File(surface);
                if (!files.Add(rel)) return false;
                string path = Path.Combine(root, "Assets", "Audio", rel.Replace('/', Path.DirectorySeparatorChar));
                if (!Audible(path, out _)) return false;
            }
            return true;
        }

        static bool ArenaFooting()
        {
            if (!MapFooting(MegaParkP1Layout.BuildSolids(), MegaParkP1Layout.BuildDressing(), false, false))
                return false;
            if (!MapFooting(PocketParkLayout.BuildSolids(), PocketParkLayout.BuildDressing(), true, false))
                return false;
            if (!MapFooting(StackYardLayout.BuildSolids(), StackYardLayout.BuildDressing(), true, true))
                return false;
            return true;
        }

        static bool MapFooting(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Dress[] dress, bool needBark, bool yard)
        {
            if (solids == null || solids.Length == 0) return false;
            bool grass = false, mulch = false, concrete = false, sand = false, metal = false, bark = false;
            bool ship = false, cat = false, shipConcrete = false;
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                string painted = "MEGA_" + (s.Mat ?? "");
                FootstepMap.Surface surface = FootstepMap.Classify(painted, s.Name);
                if (s.Kind == "ground")
                {
                    if (s.Mat == "grass")
                    {
                        if (surface != FootstepMap.Surface.Grass) return false;
                        grass = true;
                    }
                    else if (s.Mat == "mulch")
                    {
                        if (surface != FootstepMap.Surface.Grass) return false;
                        mulch = true;
                    }
                    else if (s.Mat == "concrete")
                    {
                        if (surface != FootstepMap.Surface.Concrete) return false;
                        concrete = true;
                    }
                    else if (s.Mat == "sand")
                    {
                        if (surface != FootstepMap.Surface.Concrete) return false;
                        sand = true;
                    }
                    else if (s.Mat == "bark")
                    {
                        if (surface != FootstepMap.Surface.Wood) return false;
                        bark = true;
                    }
                }
                if (Prefixed(s.Name, "Ship_") || Prefixed(s.Name, "Cat_") || Prefixed(s.Name, "Wh_"))
                {
                    if (surface != FootstepMap.Surface.Metal) return false;
                    if (Prefixed(s.Name, "Ship_"))
                    {
                        ship = true;
                        if (s.Mat == "concrete") shipConcrete = true;
                    }
                    if (Prefixed(s.Name, "Cat_")) cat = true;
                }
                if (surface == FootstepMap.Surface.Metal) metal = true;
            }
            if (!grass || !mulch || !concrete || !sand || !metal) return false;
            if (needBark && !bark) return false;
            if (yard && (!ship || !cat || !shipConcrete || !bark)) return false;
            return DressWood(dress);
        }

        static bool DressWood(MegaParkP1Layout.Dress[] dress)
        {
            if (dress == null) return false;
            bool wood = false;
            for (int i = 0; i < dress.Length; i++)
            {
                string mat = dress[i].Mat;
                if (mat != "wood" && mat != "cedar" && mat != "bark" && mat != "plank") continue;
                if (FootstepMap.Classify("MEGA_" + mat, dress[i].Name) != FootstepMap.Surface.Wood)
                    return false;
                wood = true;
            }
            return wood;
        }

        static bool Prefixed(string name, string prefix)
        {
            return name != null && name.StartsWith(prefix, StringComparison.Ordinal);
        }

        static bool PadZip(string root)
        {
            if (PocketParkLayout.LaunchPads == null || PocketParkLayout.LaunchPads.Length == 0) return false;
            if (PocketParkLayout.ZipLines == null || PocketParkLayout.ZipLines.Length == 0) return false;
            if (StackYardLayout.LaunchPads == null || StackYardLayout.LaunchPads.Length == 0) return false;
            if (StackYardLayout.ZipLines == null || StackYardLayout.ZipLines.Length == 0) return false;

            string boot = Read(Path.Combine(root, "Assets", "Scripts", "Level", "MegaParkP1Bootstrap.cs"));
            if (boot == null) return false;
            if (boot.IndexOf("BuildLaunchPadList(PocketParkLayout.LaunchPads)", StringComparison.Ordinal) < 0) return false;
            if (boot.IndexOf("BuildZipLineList(PocketParkLayout.ZipLines)", StringComparison.Ordinal) < 0) return false;
            if (boot.IndexOf("BuildLaunchPadList(StackYardLayout.LaunchPads)", StringComparison.Ordinal) < 0) return false;
            if (boot.IndexOf("BuildZipLineList(StackYardLayout.ZipLines)", StringComparison.Ordinal) < 0) return false;

            string launch = Read(Path.Combine(root, "Assets", "Scripts", "Level", "LaunchPad.cs"));
            string zip = Read(Path.Combine(root, "Assets", "Scripts", "Level", "ZipLine.cs"));
            if (launch == null || zip == null) return false;
            if (launch.IndexOf("motor.QueueLaunch", StringComparison.Ordinal) < 0) return false;
            if (zip.IndexOf("motor.TryBeginZip", StringComparison.Ordinal) < 0) return false;

            string motor = Read(Path.Combine(root, "Assets", "TagArenaMovement", "Scripts", "Core", "PlayerMotor.cs"));
            if (motor == null) return false;
            if (motor.IndexOf("AudioBus.Hook.PadLaunch", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("AudioBus.Hook.ZipGrab", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("AudioBus.Hook.ZipLoop", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("AudioBus.Hook.ZipDrop", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static bool Voices(AudioReport report, out int peak)
        {
            peak = 0;
            VoiceBudget.Reset();
            for (int p = 0; p < 4; p++)
            {
                int stepPri = p == 0 ? VoiceBudget.PriItStep : VoiceBudget.PriStep;
                if (VoiceBudget.Admit(stepPri, 0.08f, 0f) < 0) return false;
                if (VoiceBudget.Admit(VoiceBudget.PriSlide, 0.44f, 0f) < 0) return false;
            }
            if (VoiceBudget.Admit(VoiceBudget.PriPunch, 0.09f, 0f) < 0) return false;
            if (VoiceBudget.Admit(VoiceBudget.PriTag, 0.22f, 0f) < 0) return false;
            if (VoiceBudget.Admit(VoiceBudget.PriUi, 0.04f, 0f) < 0) return false;
            if (VoiceBudget.Admit(VoiceBudget.PriRound, 0.05f, 0f) < 0) return false;
            if (VoiceBudget.Live != 12 || VoiceBudget.Rejected != 0) return false;

            int flooded = 0;
            for (int i = 0; i < 24; i++)
            {
                if (VoiceBudget.Admit(10, 1f, 0f) < 0) flooded++;
            }
            if (flooded == 0 || VoiceBudget.Live > VoiceBudget.Cap) return false;

            VoiceBudget.Reset();
            for (int i = 0; i < VoiceBudget.Cap; i++)
            {
                if (VoiceBudget.Admit(VoiceBudget.PriStep, 2f, 0f) < 0) return false;
            }
            if (VoiceBudget.Admit(VoiceBudget.PriTag, 0.2f, 0f) < 0) return false;
            if (VoiceBudget.Admit(8, 2f, 0f) >= 0) return false;
            for (int i = 0; i < 8; i++)
                VoiceBudget.Admit(VoiceBudget.PriScuff, 0.05f, 0f);
            peak = VoiceBudget.Peak;
            if (peak != VoiceBudget.Cap || peak > VoiceBudget.Cap) return false;
            if (VoiceBudget.Live > VoiceBudget.Cap) return false;
            VoiceBudget.Advance(10f);
            if (VoiceBudget.Live != 0) return false;
            return true;
        }

        static bool Sliders(AudioReport report)
        {
            if (MenuGraph.SettingsRows != GameSettings.RowCount) return false;
            GameSettings shown = GameSettings.Defaults();
            bool master = false, sfx = false, ui = false, music = false;
            for (int i = 0; i < GameSettings.RowCount; i++)
            {
                string label = shown.RowLabel(i);
                if (label.IndexOf("Master", StringComparison.Ordinal) >= 0) master = true;
                if (label.IndexOf("SFX", StringComparison.Ordinal) >= 0) sfx = true;
                if (label.IndexOf("UI", StringComparison.Ordinal) >= 0) ui = true;
                if (label.IndexOf("Music", StringComparison.Ordinal) >= 0) music = true;
            }
            if (!master || !sfx || !ui || !music) return false;

            GameSettings edited = GameSettings.Defaults();
            edited.Nudge(GameSettings.RowMaster, -1);
            edited.Nudge(GameSettings.RowSfx, -1);
            edited.Nudge(GameSettings.RowUi, -1);
            edited.Nudge(GameSettings.RowMusic, 1);
            if (Near(edited.Master, GameSettings.MasterDefault)) return false;
            if (Near(edited.Sfx, GameSettings.SfxDefault)) return false;
            if (Near(edited.Ui, GameSettings.UiDefault)) return false;
            if (Near(edited.Music, GameSettings.MusicDefault)) return false;

            string blob = SettingsFile.Write(edited, ActionBinds.Defaults());
            if (blob.IndexOf("ui=", StringComparison.Ordinal) < 0 || blob.IndexOf("music=", StringComparison.Ordinal) < 0)
                return false;
            if (blob.IndexOf("master=", StringComparison.Ordinal) < 0 || blob.IndexOf("sfx=", StringComparison.Ordinal) < 0)
                return false;

            GameSettings loaded = GameSettings.Defaults();
            SettingsFile.Read(blob, loaded, ActionBinds.Defaults());
            if (!Near(loaded.Master, edited.Master) || !Near(loaded.Sfx, edited.Sfx)) return false;
            if (!Near(loaded.Ui, edited.Ui) || !Near(loaded.Music, edited.Music)) return false;
            loaded.ResetToDefaults();
            if (!Near(loaded.Ui, GameSettings.UiDefault) || !Near(loaded.Music, GameSettings.MusicDefault)) return false;
            if (!Near(loaded.Master, GameSettings.MasterDefault) || !Near(loaded.Sfx, GameSettings.SfxDefault)) return false;
            return true;
        }

        static bool MixWired(string root, AudioReport report)
        {
            string mix = Read(Path.Combine(root, "Assets", "Scripts", "Audio", "AudioMix.cs"));
            if (mix == null) return false;
            if (mix.IndexOf("spatialBlend = 1f", StringComparison.Ordinal) < 0) return false;
            if (mix.IndexOf("spatialBlend = 0f", StringComparison.Ordinal) < 0) return false;
            if (mix.IndexOf("VoiceBudget.HearMeters", StringComparison.Ordinal) < 0) return false;
            if (mix.IndexOf("AudioRolloffMode.Logarithmic", StringComparison.Ordinal) < 0) return false;
            if (mix.IndexOf("ItFootstepGain", StringComparison.Ordinal) < 0) return false;
            string pawn = Read(Path.Combine(root, "Assets", "Scripts", "Audio", "PawnAudio.cs"));
            if (pawn == null || pawn.IndexOf("FootstepMap", StringComparison.Ordinal) < 0) return false;
            if (pawn.IndexOf("ItFootstepGain", StringComparison.Ordinal) < 0
                && pawn.IndexOf("itLouder", StringComparison.Ordinal) < 0
                && pawn.IndexOf("PriItStep", StringComparison.Ordinal) < 0)
                return false;
            return true;
        }

        static bool Audible(string path, out string why)
        {
            why = "missing";
            if (!File.Exists(path)) return false;
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 44) { why = "short"; return false; }
            if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F')
            {
                why = "not riff";
                return false;
            }
            int channels = Bit(bytes, 22);
            int rate = Bit32(bytes, 24);
            int bits = Bit(bytes, 34);
            if (channels != 1 || rate != 22050 || bits != 16)
            {
                why = "format";
                return false;
            }
            int data = -1;
            for (int i = 12; i + 8 < bytes.Length; i++)
            {
                if (bytes[i] == (byte)'d' && bytes[i + 1] == (byte)'a' && bytes[i + 2] == (byte)'t' && bytes[i + 3] == (byte)'a')
                {
                    data = i;
                    break;
                }
            }
            if (data < 0) { why = "no data"; return false; }
            int size = Bit32(bytes, data + 4);
            int start = data + 8;
            if (size < 400 || start + size > bytes.Length) { why = "empty"; return false; }
            int samples = size / 2;
            double acc = 0.0;
            for (int i = 0; i < samples; i++)
            {
                int lo = bytes[start + i * 2];
                int hi = bytes[start + i * 2 + 1];
                short v = (short)(lo | (hi << 8));
                acc += v < 0 ? -v : v;
            }
            double mean = acc / samples;
            if (mean < 400.0) { why = "silent"; return false; }
            float seconds = samples / 22050f;
            if (seconds < 0.02f || seconds > 0.55f) { why = "length"; return false; }
            why = "";
            return true;
        }

        static int Bit(byte[] b, int i)
        {
            return b[i] | (b[i + 1] << 8);
        }

        static int Bit32(byte[] b, int i)
        {
            return b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24);
        }

        static bool SameSize(string a, string b)
        {
            if (!File.Exists(a) || !File.Exists(b)) return false;
            return new FileInfo(a).Length == new FileInfo(b).Length && new FileInfo(a).Length > 44;
        }

        static bool Near(float a, float b)
        {
            return Math.Abs(a - b) < 0.001f;
        }

        static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        static string FindRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(Path.Combine(dir, "Assets")) && Directory.Exists(Path.Combine(dir, "Tools")))
                    return dir;
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
