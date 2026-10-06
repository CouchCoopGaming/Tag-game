using Tag.Practice;

namespace Tag.MatchStats
{
    /// <summary>
    /// Last eight seconds of pawn position and pose. The ring and the frozen window
    /// are allocated once. Recording only writes into them.
    /// Playback poses ghost figures. Those figures have no body.
    /// </summary>
    public static class MatchHighlight
    {
        public const float Window = 8f;
        public const int Hz = 20;
        public const int Samples = 160;
        public const int Pawns = MatchBook.Cap;
        public const bool HasCharacterController = false;
        public const bool HasCollider = false;
        public const bool HasRigidbody = false;
        public const bool RootMotion = false;

        public static int RingAlloc => _alloc;
        public static int Leftovers;
        public static bool Playing;
        public static bool FromTag;
        public static int SnapCount;
        public static float Clock;
        public static float MissDistance = float.MaxValue;

        static readonly float[] X = new float[Pawns * Samples];
        static readonly float[] Y = new float[Pawns * Samples];
        static readonly float[] Z = new float[Pawns * Samples];
        static readonly float[] Yaw = new float[Pawns * Samples];
        static readonly byte[] Pose = new byte[Pawns * Samples];
        static readonly float[] SnapX = new float[Pawns * Samples];
        static readonly float[] SnapY = new float[Pawns * Samples];
        static readonly float[] SnapZ = new float[Pawns * Samples];
        static readonly float[] SnapYaw = new float[Pawns * Samples];
        static readonly byte[] SnapPose = new byte[Pawns * Samples];

        static int _head;
        static int _filled;
        static float _accum;
        static int _alloc;
        static bool _missHeld;

        public static void Reset()
        {
            _head = 0;
            _filled = 0;
            _accum = 0f;
            _alloc = 0;
            Playing = false;
            FromTag = false;
            _missHeld = false;
            SnapCount = 0;
            Clock = 0f;
            MissDistance = float.MaxValue;
            Leftovers = 0;
        }

        public static int Filled => _filled;

        /// <summary>One sample step for every bound pawn. No allocation.</summary>
        public static void Offer(float dt, int n, float[] x, float[] y, float[] z, float[] yaw, byte[] pose)
        {
            if (n < 1) return;
            if (n > Pawns) n = Pawns;
            if (dt < 0f) dt = 0f;
            float step = 1f / Hz;
            if (_filled == 0)
            {
                Write(n, x, y, z, yaw, pose);
                _accum = 0f;
                return;
            }
            _accum += dt;
            if (_accum < step) return;
            _accum -= step;
            if (_accum > step) _accum = step;
            Write(n, x, y, z, yaw, pose);
        }

        public static void MarkTag()
        {
            Freeze();
            FromTag = true;
        }

        public static void ConsiderMiss(float dist)
        {
            if (FromTag) return;
            if (dist <= 0f || dist > MatchBook.PunchReach) return;
            if (_missHeld && dist >= MissDistance - 0.0001f) return;
            MissDistance = dist;
            _missHeld = true;
            Freeze();
        }

        public static void BeginPlayback()
        {
            Clock = 0f;
            Playing = SnapCount > 1;
        }

        public static void Tick(float dt)
        {
            if (!Playing) return;
            if (dt < 0f) dt = 0f;
            Clock += dt;
            float len = Length();
            if (Clock >= len)
                Playing = false;
        }

        public static void Skip()
        {
            Playing = false;
            Clock = 0f;
            Leftovers = 0;
        }

        public static void Release()
        {
            Playing = false;
            Leftovers = 0;
        }

        public static float Length()
        {
            if (SnapCount < 2) return 0f;
            return (SnapCount - 1) / (float)Hz;
        }

        public static bool At(int pawn, float time, out float x, out float y, out float z, out float yaw, out byte pose)
        {
            x = 0f;
            y = 0f;
            z = 0f;
            yaw = 0f;
            pose = PracticeVerb.None;
            if (pawn < 0 || pawn >= Pawns || SnapCount < 1) return false;
            int o = pawn * Samples;
            if (SnapCount == 1 || time <= 0f)
            {
                x = SnapX[o];
                y = SnapY[o];
                z = SnapZ[o];
                yaw = SnapYaw[o];
                pose = SnapPose[o];
                return true;
            }
            float u = time * Hz;
            int i = (int)u;
            if (i >= SnapCount - 1) i = SnapCount - 2;
            if (i < 0) i = 0;
            float f = u - i;
            if (f < 0f) f = 0f;
            if (f > 1f) f = 1f;
            int a = o + i;
            int b = a + 1;
            x = SnapX[a] + (SnapX[b] - SnapX[a]) * f;
            y = SnapY[a] + (SnapY[b] - SnapY[a]) * f;
            z = SnapZ[a] + (SnapZ[b] - SnapZ[a]) * f;
            yaw = SnapYaw[a] + (SnapYaw[b] - SnapYaw[a]) * f;
            pose = f < 0.5f ? SnapPose[a] : SnapPose[b];
            return true;
        }

        static void Write(int n, float[] x, float[] y, float[] z, float[] yaw, byte[] pose)
        {
            int slot = _head;
            if (slot < 0 || slot >= Samples) slot = 0;
            for (int p = 0; p < n; p++)
            {
                int o = p * Samples + slot;
                X[o] = x != null && p < x.Length ? x[p] : 0f;
                Y[o] = y != null && p < y.Length ? y[p] : 0f;
                Z[o] = z != null && p < z.Length ? z[p] : 0f;
                Yaw[o] = yaw != null && p < yaw.Length ? yaw[p] : 0f;
                Pose[o] = pose != null && p < pose.Length ? pose[p] : PracticeVerb.None;
            }
            _head = slot + 1;
            if (_head >= Samples) _head = 0;
            if (_filled < Samples) _filled++;
        }

        static void Freeze()
        {
            int n = _filled;
            if (n < 1) return;
            if (n > Samples) n = Samples;
            int pawns = MatchBook.Count;
            if (pawns < 1) pawns = 1;
            if (pawns > Pawns) pawns = Pawns;
            int start = _head - n;
            if (start < 0) start += Samples;
            for (int p = 0; p < pawns; p++)
            {
                int dst = p * Samples;
                for (int s = 0; s < n; s++)
                {
                    int srcSlot = start + s;
                    if (srcSlot >= Samples) srcSlot -= Samples;
                    int src = p * Samples + srcSlot;
                    SnapX[dst + s] = X[src];
                    SnapY[dst + s] = Y[src];
                    SnapZ[dst + s] = Z[src];
                    SnapYaw[dst + s] = Yaw[src];
                    SnapPose[dst + s] = Pose[src];
                }
            }
            SnapCount = n;
        }
    }
}
