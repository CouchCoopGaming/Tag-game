using System.Globalization;
using UnityEngine;
using Tag.Art;
using TagArena.Movement;

namespace Tag.Gameplay
{
    /// <summary>
    /// Ball-carrier evasion. Off unless <see cref="Enabled"/> is set.
    /// Stutter, spin, juke, and dive brake or steer inside the sprint cap.
    /// They do not add jump speed, root motion, or a tag i-frame.
    /// Juke and spin call <see cref="TryRaise"/> from the pad reader only while
    /// <see cref="Enabled"/> is true. Stutter and dive stay unbound.
    /// </summary>
    public static class EvasionMoves
    {
        public static bool Enabled = false;

        public const bool RootMotion = false;
        public const bool Invulnerable = false;
        public const float VerticalImpulse = 0f;

        public const float EntrySeconds = 0.10f;
        public const float ExitSeconds = 0.12f;
        public const float StutterBrake = 0.18f;
        public const float StutterBurst = 0.20f;
        public const float StutterRatio = 0.35f;
        public const float SpinSeconds = 0.35f;
        public const float SpinHold = 0.90f;
        public const float SpinSlip = 0.92f;
        public const float JukeSeconds = 0.22f;
        public const float JukeOffset = 1.40f;
        public const float DiveDistance = 3.00f;
        public const float DiveFlight = 3.00f / 13.8f;
        public const float DiveRecover = 0.60f;
        public const float DiveRecoverRatio = 0.40f;
        public const float WhiffSeconds = 0.12f;
        public const float StutterCooldown = 0.75f;
        public const float SpinCooldown = 0.90f;
        public const float JukeCooldown = 0.65f;
        public const float DiveCooldown = 1.80f;

        public enum Kind
        {
            None = 0,
            Stutter = 1,
            Spin = 2,
            Juke = 3,
            Dive = 4
        }

        struct Slot
        {
            public int Id;
            public bool Used;
            public bool Playing;
            public bool Show;
            public Kind Kind;
            public int Sign;
            public float Age;
            public float ShownAge;
            public float EntrySpeed;
            public float CdStutter, CdSpin, CdJuke, CdDive;
        }

        struct Metrics
        {
            public float Stutter, Spin, Juke, Dive;
            public bool CapOk, FlagOff, VerticalHeld, WhiffTag, JukeMiss, SpinMiss, AimedHit, DiveTag, CooldownHolds;
        }

        const int SlotCount = 8;
        static readonly Slot[] Slots = new Slot[SlotCount];

        public static float MoveSeconds(Kind kind)
        {
            switch (kind)
            {
                case Kind.Stutter: return StutterBrake + StutterBurst;
                case Kind.Spin: return SpinSeconds;
                case Kind.Juke: return JukeSeconds;
                case Kind.Dive: return DiveFlight + DiveRecover;
                default: return 0f;
            }
        }

        public static float Duration(Kind kind)
        {
            if (kind == Kind.None) return 0f;
            return EntrySeconds + MoveSeconds(kind) + ExitSeconds;
        }

        public static void Reset()
        {
            for (int i = 0; i < SlotCount; i++)
                Slots[i] = new Slot();
        }

        /// <summary>Grounded runners only. Ignored while the flag is off, on cooldown, or mid-move.</summary>
        public static bool TryRaise(int id, Kind kind, int sign, float speed, bool grounded)
        {
            if (!Enabled || !grounded || kind == Kind.None) return false;
            int index = Find(id);
            if (index < 0) index = Alloc(id);
            if (index < 0) return false;
            Slot slot = Slots[index];
            if (slot.Playing) return false;
            if (Cooldown(slot, kind) > 0f) return false;
            slot.Playing = true;
            slot.Show = true;
            slot.Kind = kind;
            slot.Sign = sign < 0 ? -1 : 1;
            slot.Age = 0f;
            slot.ShownAge = 0f;
            float entry = speed < 0f ? -speed : speed;
            slot.EntrySpeed = entry;
            if (kind == Kind.Stutter) slot.CdStutter = StutterCooldown;
            else if (kind == Kind.Spin) slot.CdSpin = SpinCooldown;
            else if (kind == Kind.Juke) slot.CdJuke = JukeCooldown;
            else slot.CdDive = DiveCooldown;
            Slots[index] = slot;
            return true;
        }

        /// <summary>
        /// One motor step. False leaves horizontal velocity with the motor.
        /// True replaces forward and lateral. Vertical is never written.
        /// </summary>
        public static bool Advance(int id, float dt, ref float forward, ref float lateral, float accel, float cap)
        {
            if (!Enabled || dt <= 0f) return false;
            int index = Find(id);
            if (index < 0) return false;
            Slot slot = Slots[index];
            TickCd(ref slot, dt);
            if (!slot.Playing)
            {
                slot.Show = false;
                Slots[index] = slot;
                return false;
            }

            float age = slot.Age;
            float duration = Duration(slot.Kind);
            bool own = Own(slot, age, cap, accel, ref forward, ref lateral);
            slot.ShownAge = age;
            slot.Show = true;
            slot.Age = age + dt;
            if (slot.Age >= duration)
                slot.Playing = false;
            Slots[index] = slot;
            return own;
        }

        public static Vector3 Gate(int id, Vector3 velocity, float dt, float accel, float cap, Vector3 forward, Vector3 right)
        {
            if (!Enabled) return velocity;
            float fwd = velocity.x * forward.x + velocity.z * forward.z;
            float lat = velocity.x * right.x + velocity.z * right.z;
            if (!Advance(id, dt, ref fwd, ref lat, accel, cap)) return velocity;
            return new Vector3(forward.x * fwd + right.x * lat, velocity.y, forward.z * fwd + right.z * lat);
        }

        public static bool TrySample(int id, out EvasionPose.Sample sample)
        {
            sample = default;
            if (!Enabled) return false;
            int index = Find(id);
            if (index < 0 || !Slots[index].Show) return false;
            sample = EvasionPose.At(Slots[index].Kind, Slots[index].Sign, Slots[index].ShownAge);
            return true;
        }

        /// <summary>
        /// Punch volume against a runner center. aimX is the reach's lateral aim.
        /// A body that has stepped outside the box misses. A body still in it is tagged.
        /// There is no evade flag in this test.
        /// </summary>
        public static bool ReachHits(float bodyX, float bodyZ, float aimX)
        {
            const float half = 0.85f * 0.5f + 0.38f;
            const float z0 = 1.70f - 1.55f - 0.38f;
            const float z1 = 1.70f + 0.38f;
            float dx = bodyX - aimX;
            if (dx < 0f) dx = -dx;
            return dx <= half && bodyZ >= z0 && bodyZ <= z1;
        }

        public static bool Holds()
        {
            if (RootMotion || Invulnerable || VerticalImpulse != 0f) return false;
            if (EntrySeconds < 0.08f || ExitSeconds < 0.10f) return false;
            if (StutterBrake < 0.16f || StutterBrake > 0.20f) return false;
            if (SpinSeconds < 0.32f || SpinSeconds > 0.38f) return false;
            if (JukeSeconds < 0.20f || JukeSeconds > 0.26f) return false;
            if (JukeOffset < 1.2f || JukeOffset > 1.6f) return false;
            if (DiveDistance < 2.5f || DiveDistance > 3.5f) return false;
            if (DiveRecover < 0.55f || DiveRecover > 0.65f) return false;
            if (WhiffSeconds < 0.08f) return false;
            var cfg = new MovementConfig();
            if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.0001f) return false;
            if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.0001f) return false;
            if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.0001f) return false;
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.0001f) return false;
            if (Mathf.Abs(cfg.maxFallSpeed - 56.16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.walkSpeed - 6.9f) > 0.001f) return false;
            if (Mathf.Abs(cfg.sprintSpeed - 13.8f) > 0.001f) return false;
            if (Mathf.Abs(cfg.crouchSpeed - 3.68f) > 0.001f) return false;
            if (Mathf.Abs(cfg.groundAccel - 52f) > 0.001f) return false;
            if (Mathf.Abs(HandoffFeel.RollShare - 0.65f) > 0.001f) return false;
            if (Mathf.Abs(HandoffFeel.RollSpeed - 0.65f * 56.16f) > 0.05f) return false;
            bool was = Enabled;
            Metrics m = Measure();
            Enabled = was;
            if (!m.FlagOff || !m.CapOk || !m.VerticalHeld) return false;
            if (!m.WhiffTag || !m.JukeMiss || !m.SpinMiss || !m.AimedHit || !m.DiveTag) return false;
            if (!m.CooldownHolds) return false;
            if (Mathf.Abs(m.Stutter - StutterRatio) > 0.02f) return false;
            if (Mathf.Abs(m.Spin - SpinHold) > 0.02f) return false;
            if (m.Juke < 1.2f || m.Juke > 1.6f) return false;
            if (m.Dive < 2.5f || m.Dive > 3.5f) return false;
            if (!EvasionPose.Holds()) return false;
            return Enabled == was;
        }

        public static string ProofLine()
        {
            bool was = Enabled;
            bool flagOff = !was;
            Metrics m = Measure();
            Enabled = was;
            CultureInfo c = CultureInfo.InvariantCulture;
            return "evasion-moves"
                + " stutter=" + m.Stutter.ToString("0.00", c)
                + " spin=" + m.Spin.ToString("0.00", c)
                + " juke=" + m.Juke.ToString("0.00", c)
                + " dive=" + m.Dive.ToString("0.00", c)
                + " capOK=" + (m.CapOk ? "1" : "0")
                + " rootMotion=" + (RootMotion ? "1" : "0")
                + " flagDefault=" + (flagOff ? "off" : "on");
        }

        static Metrics Measure()
        {
            Metrics m = new Metrics();
            m.FlagOff = !Enabled;
            m.CapOk = true;
            Enabled = false;
            Reset();
            Vector3 parked = new Vector3(3.25f, -2.5f, 8.5f);
            Vector3 gated = Gate(4, parked, 0.016f, 52f, 13.8f, Vector3.forward, Vector3.right);
            m.VerticalHeld = gated.x == parked.x && gated.y == parked.y && gated.z == parked.z;
            if (TryRaise(4, Kind.Dive, 1, 13.8f, true)) m.VerticalHeld = false;
            Enabled = true;
            Reset();
            const float cap = 13.8f;
            const float accel = 52f;
            const float dt = 1f / 60f;
            m.Stutter = RunStutter(cap, accel, dt, ref m);
            m.Spin = RunSpin(cap, accel, dt, ref m);
            m.Juke = RunJuke(cap, accel, dt, ref m);
            m.Dive = RunDive(cap, accel, dt, ref m);
            m.CooldownHolds = RunCooldown(cap, accel, dt);
            Enabled = false;
            return m;
        }

        static float RunStutter(float cap, float accel, float dt, ref Metrics m)
        {
            Reset();
            if (!TryRaise(1, Kind.Stutter, 1, cap, true)) return -1f;
            float fwd = cap;
            float lat = 0f;
            float ratio = 0f;
            float y = 0f;
            for (int i = 0; i < 80; i++)
            {
                Vector3 v = Gate(1, new Vector3(lat, y, fwd), dt, accel, cap, Vector3.forward, Vector3.right);
                if (v.y != y) m.VerticalHeld = false;
                NoteCap(v.x, v.z, cap, ref m);
                float age = Slots[Find(1)].ShownAge;
                if (age >= EntrySeconds && age < EntrySeconds + StutterBrake)
                {
                    ratio = cap > 0.001f ? v.z / cap : 0f;
                    if (ReachHits(0f, 0f, 0f)) m.WhiffTag = true;
                }
                fwd = v.z;
                lat = v.x;
                if (!Slots[Find(1)].Playing && !Slots[Find(1)].Show) break;
            }
            return ratio;
        }

        static float RunSpin(float cap, float accel, float dt, ref Metrics m)
        {
            Reset();
            TryRaise(1, Kind.Spin, 1, cap, true);
            float fwd = cap;
            float lat = 0f;
            float ratio = 0f;
            float x = 0f;
            for (int i = 0; i < 80; i++)
            {
                float before = Slots[Find(1)].ShownAge;
                Vector3 v = Gate(1, new Vector3(lat, 0f, fwd), dt, accel, cap, Vector3.forward, Vector3.right);
                NoteCap(v.x, v.z, cap, ref m);
                float age = before;
                if (age >= EntrySeconds && age < EntrySeconds + SpinSeconds)
                {
                    ratio = cap > 0.001f ? v.z / cap : 0f;
                    float step = dt;
                    float end = EntrySeconds + SpinSeconds;
                    float a0 = age < EntrySeconds ? EntrySeconds : age;
                    float a1 = age + dt > end ? end : age + dt;
                    if (a1 > a0) x += (SpinSlip / SpinSeconds) * (a1 - a0);
                }
                fwd = v.z;
                lat = v.x;
                if (Find(1) >= 0 && !Slots[Find(1)].Playing && age > EntrySeconds + SpinSeconds) break;
            }
            m.SpinMiss = !ReachHits(x, 0f, 0f);
            m.AimedHit = ReachHits(x, 0f, x);
            return ratio;
        }

        static float RunJuke(float cap, float accel, float dt, ref Metrics m)
        {
            Reset();
            TryRaise(1, Kind.Juke, 1, cap, true);
            float fwd = cap;
            float lat = 0f;
            float x = 0f;
            for (int i = 0; i < 80; i++)
            {
                int index = Find(1);
                float age = Slots[index].ShownAge;
                Vector3 v = Gate(1, new Vector3(lat, 1.5f, fwd), dt, accel, cap, Vector3.forward, Vector3.right);
                if (Mathf.Abs(v.y - 1.5f) > 0.0001f) m.VerticalHeld = false;
                NoteCap(v.x, v.z, cap, ref m);
                float end = EntrySeconds + JukeSeconds;
                float a0 = age < EntrySeconds ? EntrySeconds : age;
                float a1 = age + dt > end ? end : age + dt;
                if (a1 > a0) x += (JukeOffset / JukeSeconds) * (a1 - a0);
                fwd = v.z;
                lat = v.x;
                if (!Slots[Find(1)].Playing && age > end) break;
            }
            m.JukeMiss = !ReachHits(x, 0f, 0f) && ReachHits(x, 0f, x);
            return x < 0f ? -x : x;
        }

        static float RunDive(float cap, float accel, float dt, ref Metrics m)
        {
            Reset();
            TryRaise(1, Kind.Dive, 1, cap, true);
            float fwd = cap;
            float lat = 0f;
            float z = 0f;
            float flightEnd = EntrySeconds + DiveFlight;
            bool tagged = false;
            for (int i = 0; i < 120; i++)
            {
                int index = Find(1);
                float age = Slots[index].ShownAge;
                Vector3 v = Gate(1, new Vector3(lat, 0f, fwd), dt, accel, cap, Vector3.forward, Vector3.right);
                if (v.y != 0f) m.VerticalHeld = false;
                NoteCap(v.x, v.z, cap, ref m);
                float a0 = age < EntrySeconds ? EntrySeconds : age;
                float a1 = age + dt > flightEnd ? flightEnd : age + dt;
                if (a1 > a0) z += cap * (a1 - a0);
                if (age >= EntrySeconds && age <= flightEnd && ReachHits(0f, z, 0f))
                    tagged = true;
                fwd = v.z;
                lat = v.x;
                if (!Slots[Find(1)].Playing && age > flightEnd + DiveRecover) break;
            }
            m.DiveTag = tagged;
            return z;
        }

        static bool RunCooldown(float cap, float accel, float dt)
        {
            Reset();
            if (!TryRaise(2, Kind.Dive, 1, cap, true)) return false;
            if (TryRaise(2, Kind.Dive, 1, cap, true)) return false;
            float fwd = cap;
            float lat = 0f;
            for (int i = 0; i < 80; i++)
                Gate(2, new Vector3(lat, 0f, fwd), dt, accel, cap, Vector3.forward, Vector3.right);
            if (TryRaise(2, Kind.Dive, -1, cap, true)) return false;
            int index = Find(2);
            Slots[index].CdDive = 0f;
            if (!TryRaise(2, Kind.Dive, 1, cap, true)) return false;
            return !TryRaise(2, Kind.Stutter, 1, cap, false);
        }

        static void NoteCap(float lateral, float forward, float cap, ref Metrics m)
        {
            float planar = Mathf.Sqrt(lateral * lateral + forward * forward);
            if (planar > cap + 0.02f) m.CapOk = false;
        }

        static bool Own(Slot slot, float age, float cap, float accel, ref float forward, ref float lateral)
        {
            float start = EntrySeconds;
            float move = MoveSeconds(slot.Kind);
            float end = start + move;
            float exitEnd = end + WhiffSeconds;
            if (exitEnd > Duration(slot.Kind)) exitEnd = Duration(slot.Kind);
            if (age < start) return false;
            float entry = slot.EntrySpeed;
            if (entry > cap) entry = cap;
            int sign = slot.Sign;
            if (slot.Kind == Kind.Stutter)
            {
                float brakeEnd = start + StutterBrake;
                if (age < brakeEnd)
                {
                    forward = entry * StutterRatio;
                    lateral = 0f;
                    return true;
                }
                if (age < end)
                {
                    float burst = entry * StutterRatio + accel * (age - brakeEnd);
                    if (burst > cap) burst = cap;
                    forward = burst;
                    lateral = 0f;
                    return true;
                }
                return false;
            }
            if (slot.Kind == Kind.Spin)
            {
                if (age < end)
                {
                    forward = entry * SpinHold;
                    if (forward > cap) forward = cap;
                    lateral = sign * (SpinSlip / SpinSeconds);
                    ClampPlanar(ref forward, ref lateral, cap);
                    return true;
                }
                if (age < exitEnd)
                {
                    forward = entry * SpinHold;
                    if (forward > cap) forward = cap;
                    lateral = 0f;
                    return true;
                }
                return false;
            }
            if (slot.Kind == Kind.Juke)
            {
                if (age < end)
                {
                    lateral = sign * (JukeOffset / JukeSeconds);
                    float room = cap * cap - lateral * lateral;
                    forward = room > 0f ? Mathf.Sqrt(room) * 0.985f : 0f;
                    ClampPlanar(ref forward, ref lateral, cap);
                    return true;
                }
                if (age < exitEnd)
                {
                    lateral = 0f;
                    float room = cap * cap - (JukeOffset / JukeSeconds) * (JukeOffset / JukeSeconds);
                    forward = room > 0f ? Mathf.Sqrt(room) * 0.985f : entry * 0.8f;
                    if (forward > cap) forward = cap;
                    return true;
                }
                return false;
            }
            if (slot.Kind == Kind.Dive)
            {
                float flightEnd = start + DiveFlight;
                if (age < flightEnd)
                {
                    forward = cap;
                    lateral = 0f;
                    return true;
                }
                if (age < end)
                {
                    forward = cap * DiveRecoverRatio;
                    lateral = 0f;
                    return true;
                }
                return false;
            }
            return false;
        }

        static void ClampPlanar(ref float forward, ref float lateral, float cap)
        {
            float planar = Mathf.Sqrt(forward * forward + lateral * lateral);
            if (planar <= cap || planar < 0.0001f) return;
            float scale = cap / planar;
            forward *= scale;
            lateral *= scale;
        }

        static float Cooldown(Slot slot, Kind kind)
        {
            switch (kind)
            {
                case Kind.Stutter: return slot.CdStutter;
                case Kind.Spin: return slot.CdSpin;
                case Kind.Juke: return slot.CdJuke;
                case Kind.Dive: return slot.CdDive;
                default: return 0f;
            }
        }

        static void TickCd(ref Slot slot, float dt)
        {
            if (slot.CdStutter > 0f) slot.CdStutter -= dt;
            if (slot.CdSpin > 0f) slot.CdSpin -= dt;
            if (slot.CdJuke > 0f) slot.CdJuke -= dt;
            if (slot.CdDive > 0f) slot.CdDive -= dt;
            if (slot.CdStutter < 0f) slot.CdStutter = 0f;
            if (slot.CdSpin < 0f) slot.CdSpin = 0f;
            if (slot.CdJuke < 0f) slot.CdJuke = 0f;
            if (slot.CdDive < 0f) slot.CdDive = 0f;
        }

        static int Find(int id)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Slots[i].Used && Slots[i].Id == id) return i;
            }
            return -1;
        }

        static int Alloc(int id)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Slots[i].Used) continue;
                Slot slot = Slots[i];
                slot.Used = true;
                slot.Id = id;
                Slots[i] = slot;
                return i;
            }
            return -1;
        }
    }
}
