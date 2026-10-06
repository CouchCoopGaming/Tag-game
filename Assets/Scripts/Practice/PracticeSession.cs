using Tag.Onboard;
using Tag.Settings;

namespace Tag.Practice
{
    /// <summary>
    /// Practice is a free arena. Nobody is It. AI stays off unless the card
    /// asks for one passive dummy. Routes come from the catalog, which reads
    /// the registry name, so a later map shows up without a new menu.
    /// </summary>
    public static class PracticeSession
    {
        public const int Rows = 7;
        public const string RestartKey = "t";
        public const string RestartPad = "buttonNorth";
        public const string GhostKey = "g";
        public const string GhostPad = "leftStickPress";
        public const string InputKey = "i";
        public const string InputPad = "rightStickPress";

        public static bool Active;
        public static bool Dummy;
        public static bool GhostOn = true;
        public static bool InputsOn;
        public static bool ItAssigned;
        public static int Arena;
        public static int RouteSlot;
        public static int Live;
        public static int Leftovers;
        public static string RouteId = "";

        public static int AiCount => Active && Dummy ? 1 : 0;

        public static void ResetStatics()
        {
            Active = false;
            Dummy = false;
            GhostOn = true;
            InputsOn = false;
            ItAssigned = false;
            Arena = 0;
            RouteSlot = 0;
            Live = 0;
            Leftovers = 0;
            RouteId = "";
        }

        public static void Open()
        {
            Active = false;
            ItAssigned = false;
            if (GameSettings.Current != null)
                Arena = GameSettings.Current.Arena;
            if (Arena < 0) Arena = 0;
            if (Arena >= ArenaRegistry.Count) Arena = ArenaRegistry.Count - 1;
            Dummy = false;
            GhostOn = true;
            InputsOn = false;
            RouteSlot = 0;
            EnsureCatalog();
        }

        public static void StepRow(int row, int dir)
        {
            if (dir == 0) return;
            EnsureCatalog();
            if (row == 0)
            {
                Arena += dir > 0 ? 1 : -1;
                if (Arena < 0) Arena = 0;
                if (Arena >= ArenaRegistry.Count) Arena = ArenaRegistry.Count - 1;
                RouteSlot = 0;
            }
            else if (row == 1)
            {
                int choices = RouteChoices();
                RouteSlot += dir > 0 ? 1 : -1;
                if (RouteSlot < 0) RouteSlot = 0;
                if (RouteSlot >= choices) RouteSlot = choices - 1;
            }
            else if (row == 2) Dummy = !Dummy;
            else if (row == 3) GhostOn = !GhostOn;
            else if (row == 4) InputsOn = !InputsOn;
        }

        public static int RouteChoices()
        {
            EnsureCatalog();
            return 1 + PracticeCatalog.CountFor(ArenaName());
        }

        public static string ArenaName()
        {
            if (Arena < 0 || Arena >= ArenaRegistry.Count) return ArenaRegistry.All[0].Name;
            return ArenaRegistry.All[Arena].Name;
        }

        public static string RootName()
        {
            if (Arena < 0 || Arena >= ArenaRegistry.Count) return ArenaRegistry.All[0].Root;
            return ArenaRegistry.All[Arena].Root;
        }

        public static PracticeRoute SelectedRoute()
        {
            if (RouteSlot <= 0) return null;
            EnsureCatalog();
            return PracticeCatalog.ForArena(ArenaName(), RouteSlot - 1);
        }

        public static void Arm()
        {
            EnsureCatalog();
            Active = true;
            ItAssigned = false;
            PracticeRoute route = SelectedRoute();
            RouteId = route == null ? "" : route.Id;
            if (GameSettings.Current != null)
            {
                GameSettings.Current.Arena = Arena;
                GameSettings.Current.Clamp();
            }
            RestartRun();
        }

        public static void Stop()
        {
            Active = false;
            ItAssigned = false;
            ReleaseActors();
            RouteId = "";
        }

        public static void ReleaseActors()
        {
            Live = 0;
            PracticeGhost.Clear();
        }

        public static void RestartRun()
        {
            ReleaseActors();
            Leftovers = Live;
            Live = 1;
        }

        public static string RowLabel(int row)
        {
            EnsureCatalog();
            switch (row)
            {
                case 0: return "Arena  " + ArenaName();
                case 1: return "Route  " + RouteLabel();
                case 2: return "Dummy  " + (Dummy ? "On" : "Off");
                case 3: return "Ghost  " + (GhostOn ? "On" : "Off");
                case 4: return "Input display  " + (InputsOn ? "On" : "Off");
                case 5: return "Start practice";
                default: return "Back";
            }
        }

        static string RouteLabel()
        {
            if (RouteSlot <= 0) return "Free roam";
            PracticeRoute route = SelectedRoute();
            if (route == null) return "Free roam";
            return route.Name;
        }

        static void EnsureCatalog()
        {
            if (PracticeCatalog.Count < 1)
                PracticeCatalog.Load();
        }
    }

    /// <summary>
    /// Which existing verbs are down. The line is picked from a table built once.
    /// </summary>
    public static class PracticeInput
    {
        public const int Jump = 1;
        public const int Slide = 2;
        public const int Dash = 4;
        public const int Punch = 8;
        public const int Sprint = 16;
        public const int Cling = 32;

        static readonly string[] Lines = Build();

        public static int Mask(bool jump, bool slide, bool dash, bool punch, bool sprint, bool cling)
        {
            int mask = 0;
            if (jump) mask |= Jump;
            if (slide) mask |= Slide;
            if (dash) mask |= Dash;
            if (punch) mask |= Punch;
            if (sprint) mask |= Sprint;
            if (cling) mask |= Cling;
            return mask;
        }

        public static string Line(int mask)
        {
            int i = mask & 63;
            return Lines[i];
        }

        static string[] Build()
        {
            var lines = new string[64];
            string[] names = { "Jump", "Slide", "Air dash", "Punch", "Sprint", "Cling" };
            for (int mask = 0; mask < 64; mask++)
            {
                string text = "";
                for (int b = 0; b < 6; b++)
                {
                    if ((mask & (1 << b)) == 0) continue;
                    if (text.Length > 0) text += "  ";
                    text += names[b];
                }
                lines[mask] = text.Length == 0 ? " " : text;
            }
            return lines;
        }
    }
}
