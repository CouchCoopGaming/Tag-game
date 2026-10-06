using System;
using System.Globalization;
using System.IO;
using Tag.Onboard;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Practice
{
    public static class PracticeProof
    {
        public sealed class Report
        {
            public bool Ok = true;
            public string Line = "";
            public string Failure = "";

            public void Fail(string why)
            {
                Ok = false;
                if (Failure.Length == 0) Failure = why;
            }
        }

        public static Report Run()
        {
            var report = new Report();
            PracticeBests.Clear();
            PracticeGhost.Clear();
            PracticeGhost.ClearSaved();
            PracticeSession.ResetStatics();

            int loaded = PracticeCatalog.Load();
            if (loaded < 3 || PracticeCatalog.Count < 3)
                report.Fail("routes " + loaded.ToString(CultureInfo.InvariantCulture));
            int mega = PracticeCatalog.CountFor("Mega Park");
            if (mega < 3)
                report.Fail("mega routes " + mega.ToString(CultureInfo.InvariantCulture));
            if (ArenaRegistry.Count != 2
                || ArenaRegistry.All[0].Root != "PARK"
                || ArenaRegistry.All[1].Root != "MegaPark"
                || ArenaRegistry.All[1].Name != "Mega Park")
                report.Fail("arena registry roots drifted");

            PracticeRouteTable table = ScriptableObject.CreateInstance<PracticeRouteTable>();
            table.Pull();
            if (table.routeCount < 3)
                report.Fail("route table did not pull the json");

            if ((int)PlayAction.Count != 12 || MenuGraph.RebindRows != 14)
                report.Fail("practice added a rebind row");

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            var times = new string[mega];
            var ids = new string[mega];
            for (int i = 0; i < mega; i++)
            {
                PracticeRoute route = PracticeCatalog.ForArena("Mega Park", i);
                if (route == null)
                {
                    report.Fail("missing mega route " + i.ToString(CultureInfo.InvariantCulture));
                    continue;
                }
                ids[i] = route.Id;
                if (!KnownArena(route.Arena))
                    report.Fail(route.Id + " arena is not in the registry");
                if (!VerbsPresent(route))
                    report.Fail(route.Id + " verbs " + route.Verbs);
                PracticeSim.Result result = PracticeSim.Run(route, cfg);
                times[i] = result.Time.ToString("0.000", CultureInfo.InvariantCulture);
                if (!result.Ok)
                    report.Fail(result.Why.Length == 0 ? route.Id + " unreachable" : result.Why);
                else if (!PracticeBests.Has(route.Id))
                    report.Fail(route.Id + " did not store a best");
            }

            CheckDeterministic(report, cfg);
            CheckPersist(report);
            CheckRestart(report);
            CheckInput(report);
            CheckKeys(report);
            CheckFigure(report);
            if (!PracticePose.ClipsHold())
                report.Fail("ghost pose clips drifted");

            var line = "practice routes=" + PracticeCatalog.Count.ToString(CultureInfo.InvariantCulture)
                + " reachable=" + mega.ToString(CultureInfo.InvariantCulture)
                + " verbs=ok pb=persist ghost=deterministic restart=0 collider=0 controller=0 it=0 ai=0";
            for (int i = 0; i < mega; i++)
            {
                line += " " + (ids[i] ?? "?") + "=" + (times[i] ?? "0");
            }
            if (!report.Ok && report.Failure.Length > 0)
                line = "practice FAIL " + report.Failure;
            report.Line = line;
            return report;
        }

        static void CheckDeterministic(Report report, MovementConfig cfg)
        {
            PracticeRoute route = PracticeCatalog.ById("mega-beginner");
            if (route == null)
            {
                report.Fail("mega-beginner missing");
                return;
            }
            PracticeSim.Result first = PracticeSim.Run(route, cfg);
            if (!first.Ok)
            {
                report.Fail("determinism run " + first.Why);
                return;
            }
            int n = PracticeGhost.Count;
            var bx = new float[PracticeGhost.Cap];
            var by = new float[PracticeGhost.Cap];
            var bz = new float[PracticeGhost.Cap];
            var byaw = new float[PracticeGhost.Cap];
            var bpose = new byte[PracticeGhost.Cap];
            PracticeGhost.CopyLiveTo(bx, by, bz, byaw, bpose, out int copied);
            PracticeSim.Result second = PracticeSim.Run(route, cfg);
            if (!second.Ok || !PracticeGhost.Matches(bx, by, bz, byaw, bpose, copied) || copied != n || n < 2)
            {
                report.Fail("ghost record was not deterministic");
                return;
            }
            PracticeGhost.At(0f, out float x, out float y, out float z, out float yaw, out byte pose);
            if (x != PracticeGhost.X[0] || y != PracticeGhost.Y[0] || z != PracticeGhost.Z[0] || yaw != PracticeGhost.Yaw[0] || pose != PracticeGhost.Pose[0])
                report.Fail("ghost replay missed the first sample");
            PracticeGhost.Replay(route.Id, 0f, out float rx, out float ry, out float rz, out float ryaw, out byte rpose);
            PracticeGhost.Replay(route.Id, 0f, out float rx2, out float ry2, out float rz2, out float ryaw2, out byte rpose2);
            if (rx != rx2 || ry != ry2 || rz != rz2 || ryaw != ryaw2 || rpose != rpose2)
                report.Fail("ghost replay was not deterministic");
            if (rx != bx[0] || ry != by[0] || rz != bz[0])
                report.Fail("ghost replay left the recorded path");
        }

        static void CheckPersist(Report report)
        {
            if (!PracticeBests.Has("mega-beginner") || !PracticeGhost.HasReplay("mega-beginner"))
            {
                report.Fail("best or ghost was empty before save");
                return;
            }
            float time = PracticeBests.TimeOf("mega-beginner");
            int splits = PracticeBests.SplitsOf("mega-beginner");
            float split0 = PracticeBests.SplitOf("mega-beginner", 1);
            int samples = PracticeGhost.SavedSamples("mega-beginner");
            string blob = SettingsFile.Write(GameSettings.Defaults(), ActionBinds.Defaults());
            if (blob.IndexOf("pb.mega-beginner=", StringComparison.Ordinal) < 0
                || blob.IndexOf("gh.mega-beginner=", StringComparison.Ordinal) < 0)
            {
                report.Fail("settings json omitted the best or the ghost");
                return;
            }
            PracticeBests.Clear();
            PracticeGhost.ClearSaved();
            GameSettings loaded = GameSettings.Defaults();
            ActionBinds binds = ActionBinds.Defaults();
            SettingsFile.Read(blob, loaded, binds);
            if (Math.Abs(PracticeBests.TimeOf("mega-beginner") - time) > 0.001f)
                report.Fail("personal best did not round-trip");
            if (PracticeBests.SplitsOf("mega-beginner") != splits)
                report.Fail("checkpoint splits did not round-trip");
            if (splits > 1 && Math.Abs(PracticeBests.SplitOf("mega-beginner", 1) - split0) > 0.001f)
                report.Fail("a checkpoint split changed in the save");
            if (!PracticeGhost.Load("mega-beginner") || PracticeGhost.Count != samples || samples < 2)
                report.Fail("ghost save did not round-trip");
        }

        static void CheckRestart(Report report)
        {
            PracticeSession.ResetStatics();
            GameSettings.Current = GameSettings.Defaults();
            PracticeSession.Open();
            PracticeSession.Arena = 1;
            PracticeSession.RouteSlot = 1;
            PracticeSession.Arm();
            PracticeGhost.Offer(0f, 1f, 2f, 3f, 0f, PracticeVerb.Jump);
            PracticeSession.RestartRun();
            if (PracticeSession.Leftovers != 0 || PracticeGhost.Count != 0 || PracticeSession.Live != 1)
                report.Fail("restart left leftovers");
            if (PracticeSession.ItAssigned || PracticeSession.AiCount != 0)
                report.Fail("practice assigned It or spawned AI");
            PracticeSession.Dummy = true;
            if (PracticeSession.AiCount != 1)
                report.Fail("the passive dummy did not stay single");
            PracticeSession.Dummy = false;
            if (PracticeSession.AiCount != 0)
                report.Fail("dummy off still requested AI");
            PracticeSession.Stop();
        }

        static void CheckInput(Report report)
        {
            string line = PracticeInput.Line(63);
            if (line.IndexOf("Jump", StringComparison.Ordinal) < 0
                || line.IndexOf("Slide", StringComparison.Ordinal) < 0
                || line.IndexOf("Air dash", StringComparison.Ordinal) < 0
                || line.IndexOf("Punch", StringComparison.Ordinal) < 0
                || line.IndexOf("Sprint", StringComparison.Ordinal) < 0
                || line.IndexOf("Cling", StringComparison.Ordinal) < 0)
                report.Fail("input display missed a verb");
            if (line.IndexOf("Ledge", StringComparison.Ordinal) >= 0
                || line.IndexOf("Shimmy", StringComparison.Ordinal) >= 0)
                report.Fail("input display invented a verb");
            if (PracticeInput.Line(0) != " ")
                report.Fail("empty input display was not blank");
        }

        static void CheckKeys(Report report)
        {
            if (PracticeSession.RestartKey != "t" || PracticeSession.RestartPad != "buttonNorth")
                report.Fail("restart bind drifted");
            if (PracticeSession.GhostKey != "g" || PracticeSession.GhostPad != "leftStickPress")
                report.Fail("ghost bind drifted");
            if (PracticeSession.InputKey != "i" || PracticeSession.InputPad != "rightStickPress")
                report.Fail("input bind drifted");
            string text = Read("Assets/Scripts/Settings/BindSampler.cs");
            string controls = Read("Docs/Controls.md");
            string doc = Read("Docs/Practice.md");
            if (text == null || controls == null || doc == null)
            {
                report.Fail("practice docs or sampler missing");
                return;
            }
            int at = text.IndexOf("PracticeRestartDown", StringComparison.Ordinal);
            int end = text.IndexOf("LookVector", at, StringComparison.Ordinal);
            if (at < 0 || end < at)
            {
                report.Fail("restart sampler missing");
                return;
            }
            string slice = text.Substring(at, end - at);
            if (slice.IndexOf("KeyCode.T", StringComparison.Ordinal) < 0
                || slice.IndexOf("buttonNorth", StringComparison.Ordinal) < 0
                || slice.IndexOf("KeyCode.G", StringComparison.Ordinal) < 0
                || slice.IndexOf("JoystickButton8", StringComparison.Ordinal) < 0
                || slice.IndexOf("KeyCode.I", StringComparison.Ordinal) < 0
                || slice.IndexOf("JoystickButton9", StringComparison.Ordinal) < 0)
                report.Fail("practice binds are not sampled");
            if (slice.IndexOf("F3", StringComparison.Ordinal) >= 0
                || slice.IndexOf("F6", StringComparison.Ordinal) >= 0
                || slice.IndexOf("KeyCode.M", StringComparison.Ordinal) >= 0
                || slice.IndexOf("Comma", StringComparison.Ordinal) >= 0)
                report.Fail("practice bind collided with trail tag, the overlay, the minimap, or mute");
            if (controls.IndexOf("Practice", StringComparison.Ordinal) < 0
                || controls.IndexOf("buttonNorth", StringComparison.Ordinal) < 0)
                report.Fail("controls doc missed the practice bind");
            if (doc.IndexOf("Mega Park", StringComparison.Ordinal) < 0
                || doc.IndexOf("ghost", StringComparison.Ordinal) < 0)
                report.Fail("practice doc missed the routes or the ghost");
        }

        static void CheckFigure(Report report)
        {
            if (PracticeGhost.HasCharacterController || PracticeGhost.HasCollider || PracticeGhost.HasRigidbody || PracticeGhost.RootMotion)
                report.Fail("ghost claims a body");
            string play = Read("Assets/Scripts/Practice/PracticePlay.cs");
            if (play == null)
            {
                report.Fail("practice figure missing");
                return;
            }
            if (play.IndexOf("CharacterController", StringComparison.Ordinal) >= 0
                || play.IndexOf("Rigidbody", StringComparison.Ordinal) >= 0
                || play.IndexOf("Collider", StringComparison.Ordinal) >= 0
                || play.IndexOf(".Move(", StringComparison.Ordinal) >= 0)
                report.Fail("ghost figure has a body or a move");
        }

        static bool KnownArena(string name)
        {
            for (int i = 0; i < ArenaRegistry.Count; i++)
            {
                if (ArenaRegistry.All[i].Name == name) return true;
            }
            return false;
        }

        static bool VerbsPresent(PracticeRoute route)
        {
            string verbs = route.Verbs ?? "";
            if (route.Id == "mega-beginner")
                return verbs.IndexOf("sprint", StringComparison.Ordinal) >= 0 && verbs.IndexOf("jump", StringComparison.Ordinal) >= 0;
            if (route.Id == "mega-wall")
                return verbs.IndexOf("wallrun", StringComparison.Ordinal) >= 0 && verbs.IndexOf("walljump", StringComparison.Ordinal) >= 0;
            if (route.Id == "mega-toy")
                return verbs.IndexOf("pad", StringComparison.Ordinal) >= 0
                    && verbs.IndexOf("zip", StringComparison.Ordinal) >= 0
                    && verbs.IndexOf("airdash", StringComparison.Ordinal) >= 0;
            return verbs.Length > 0 || route.Gates.Length >= 2;
        }

        static string Read(string relative)
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                string path = Path.Combine(dir, relative);
                if (File.Exists(path)) return File.ReadAllText(path);
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
