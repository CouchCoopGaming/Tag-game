namespace Tag.Audio
{
    /// <summary>
    /// Fixed voice count for world, round, and UI one-shots.
    /// Music stays on its own looping source and does not take a slot.
    /// A louder priority steals the quietest slot. Same priority does not steal.
    /// Four pawns plus their verbs stay inside the cap. The rest are rejected.
    /// </summary>
    public static class VoiceBudget
    {
        public const int Cap = 16;
        public const float HearMeters = 40f;
        public const float ItFootstepGain = 1.25f;

        public const int PriUi = 100;
        public const int PriRound = 100;
        public const int PriTag = 90;
        public const int PriPunch = 80;
        public const int PriStagger = 72;
        public const int PriLandHard = 64;
        public const int PriPad = 60;
        public const int PriDash = 58;
        public const int PriWallJump = 56;
        public const int PriZip = 52;
        public const int PriCling = 50;
        public const int PriJump = 48;
        public const int PriWhiff = 44;
        public const int PriLandSoft = 40;
        public const int PriItStep = 36;
        public const int PriZipLoop = 30;
        public const int PriSlide = 28;
        public const int PriStep = 16;
        public const int PriScuff = 14;

        struct Slot
        {
            public bool On;
            public bool Ui;
            public int Priority;
            public float End;
        }

        static readonly Slot[] Slots = new Slot[Cap];

        public static int Peak { get; private set; }
        public static int Live { get; private set; }
        public static int Rejected { get; private set; }

        public static void Reset()
        {
            for (int i = 0; i < Cap; i++)
                Slots[i] = default;
            Peak = 0;
            Live = 0;
            Rejected = 0;
        }

        /// <summary>Returns the slot, or -1 when the bus is full of equal or louder voices.</summary>
        public static int Admit(int priority, float duration, float now)
        {
            return Admit(priority, duration, now, false);
        }

        public static int Admit(int priority, float duration, float now, bool ui)
        {
            if (duration < 0.02f) duration = 0.02f;
            int free = -1;
            int lowest = -1;
            int lowestPri = int.MaxValue;
            for (int i = 0; i < Cap; i++)
            {
                if (!Slots[i].On || now >= Slots[i].End)
                {
                    if (Slots[i].On) Slots[i].On = false;
                    if (free < 0) free = i;
                    continue;
                }
                if (Slots[i].Priority < lowestPri)
                {
                    lowestPri = Slots[i].Priority;
                    lowest = i;
                }
            }

            int slot = free;
            if (slot < 0)
            {
                if (lowest >= 0 && priority > Slots[lowest].Priority)
                    slot = lowest;
                else
                {
                    Rejected++;
                    return -1;
                }
            }

            Slots[slot].On = true;
            Slots[slot].Ui = ui;
            Slots[slot].Priority = priority;
            Slots[slot].End = now + duration;
            Recount();
            return slot;
        }

        /// <summary>Drops world and round voices. UI slots stay so a pause click can still speak.</summary>
        public static void SilenceWorld()
        {
            for (int i = 0; i < Cap; i++)
            {
                if (!Slots[i].On || Slots[i].Ui) continue;
                Slots[i].On = false;
            }
            Recount();
        }

        public static void Advance(float now)
        {
            for (int i = 0; i < Cap; i++)
            {
                if (Slots[i].On && now >= Slots[i].End)
                    Slots[i].On = false;
            }
            Recount();
        }

        public static void Free(int slot)
        {
            if (slot < 0 || slot >= Cap) return;
            Slots[slot].On = false;
            Recount();
        }

        public static bool Occupied(int slot)
        {
            return slot >= 0 && slot < Cap && Slots[slot].On;
        }

        public static float End(int slot)
        {
            if (slot < 0 || slot >= Cap) return 0f;
            return Slots[slot].End;
        }

        static void Recount()
        {
            int live = 0;
            for (int i = 0; i < Cap; i++)
            {
                if (Slots[i].On) live++;
            }
            Live = live;
            if (live > Peak) Peak = live;
        }
    }
}
