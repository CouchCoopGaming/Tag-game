using Tag.Gameplay;
using Tag.Modes;
using Tag.Practice;
using TagArena.Movement;
using UnityEngine;

namespace Tag.MatchStats
{
    /// <summary>
    /// Live match sampler. Refs are cached when the round opens.
    /// The per-frame path only reads motors and writes the book.
    /// </summary>
    public static class MatchLive
    {
        static readonly ItController[] Roster = new ItController[MatchBook.Cap];
        static readonly ItController[] Its = new ItController[MatchBook.Cap];
        static readonly PlayerMotor[] Motors = new PlayerMotor[MatchBook.Cap];
        static readonly float[] X = new float[MatchBook.Cap];
        static readonly float[] Y = new float[MatchBook.Cap];
        static readonly float[] Z = new float[MatchBook.Cap];
        static readonly float[] Yaw = new float[MatchBook.Cap];
        static readonly byte[] Pose = new byte[MatchBook.Cap];
        static readonly bool[] PrevWall = new bool[MatchBook.Cap];
        static readonly bool[] PrevDash = new bool[MatchBook.Cap];
        static readonly bool[] PrevZip = new bool[MatchBook.Cap];
        static readonly bool[] PrevPad = new bool[MatchBook.Cap];
        static readonly bool[] Reach = new bool[MatchBook.Cap];
        static readonly int[] WallJumps = new int[MatchBook.Cap];
        static readonly int[] Ids = new int[MatchBook.Cap];

        static int _bound;
        static bool _open;
        static bool _hold;
        static bool _tagged;
        static float _dt;

        public static bool Holding => _hold;

        public static void ResetAll()
        {
            _hold = false;
            _open = false;
            _bound = 0;
            _tagged = false;
            _dt = 0f;
            for (int i = 0; i < MatchBook.Cap; i++)
            {
                Roster[i] = null;
                Its[i] = null;
                Motors[i] = null;
                PrevWall[i] = false;
                PrevDash[i] = false;
                PrevZip[i] = false;
                PrevPad[i] = false;
                Reach[i] = false;
                WallJumps[i] = 0;
            }
            MatchBook.ResetMatch();
            MatchHighlight.Reset();
        }

        public static void Hold()
        {
            _hold = true;
        }

        public static void OnRoundStarting()
        {
            MatchGhostView.Release();
            if (_hold)
            {
                _hold = false;
                MatchHighlight.NewRound();
                return;
            }
            _open = false;
            _bound = 0;
            _tagged = false;
            MatchBook.ResetMatch();
            MatchHighlight.Reset();
        }

        public static void Arm()
        {
            TagModeController mode = TagModeController.Instance;
            if (mode == null) return;
            int n = mode.ExportRoster(Roster);
            if (n > MatchBook.Cap) n = MatchBook.Cap;
            for (int i = 0; i < n; i++)
            {
                ItController it = Roster[i];
                Ids[i] = it != null ? it.GetInstanceID() : 0;
            }
            MatchBook.SyncRoster(n, Ids);
            _open = true;
            _bound = n;
            for (int i = 0; i < n; i++)
            {
                ItController it = Roster[i];
                Its[i] = it;
                PlayerMotor motor = it != null ? it.Motor : null;
                Motors[i] = motor;
                string named = Tag.Profiles.LocalProfiles.SeatName(i);
                string id = !string.IsNullOrEmpty(named)
                    ? named
                    : (it != null && !string.IsNullOrEmpty(it.PlayerId) ? it.PlayerId : HudName(i));
                MatchBook.SetName(i, id);
                Tag.Profiles.LocalProfiles.NoteRoster(i, Tag.Profiles.LocalProfiles.ProfileAt(i));
                WallJumps[i] = motor != null ? motor.WallJumpCount : 0;
                PrevWall[i] = motor != null && motor.IsWallRunning;
                PrevDash[i] = motor != null && motor.IsAirDashing;
                PrevZip[i] = motor != null && motor.ZipRiding;
                PrevPad[i] = motor != null && motor.LaunchArc;
                Reach[i] = false;
            }
        }

        public static void Sample(float dt)
        {
            if (!_open) return;
            if (dt < 0f) dt = 0f;
            _dt = dt;
            int n = _bound;
            for (int i = 0; i < n; i++)
            {
                ItController it = Its[i];
                if (it == null) continue;
                PlayerMotor motor = Motors[i];
                Vector3 p = it.transform.position;
                float speed = motor != null ? motor.HorizSpeed : 0f;
                bool wall = motor != null && motor.IsWallRunning;
                bool dash = motor != null && motor.IsAirDashing;
                bool zip = motor != null && motor.ZipRiding;
                bool pad = motor != null && motor.LaunchArc;
                bool air = motor != null && !motor.IsGrounded && !wall && !zip;
                bool wallJump = false;
                if (motor != null)
                {
                    int count = motor.WallJumpCount;
                    wallJump = count > WallJumps[i];
                    WallJumps[i] = count;
                }
                X[i] = p.x;
                Y[i] = p.y;
                Z[i] = p.z;
                Yaw[i] = it.transform.eulerAngles.y;
                Pose[i] = PoseOf(motor, wall, dash, zip, pad, air);
                bool wallEdge = wall && !PrevWall[i];
                bool dashEdge = dash && !PrevDash[i];
                bool zipEdge = zip && !PrevZip[i];
                bool padEdge = pad && !PrevPad[i];
                PrevWall[i] = wall;
                PrevDash[i] = dash;
                PrevZip[i] = zip;
                PrevPad[i] = pad;
                MatchBook.NoteMotion(i, p.x, p.y, p.z, speed, dt, it.IsIt, air, wallEdge, wallJump, padEdge, zipEdge, dashEdge);
            }
        }

        public static void CloseFrame()
        {
            if (!_open) return;
            int n = _bound;
            int it = -1;
            for (int i = 0; i < n; i++)
            {
                if (Its[i] != null && Its[i].IsIt)
                {
                    it = i;
                    break;
                }
            }
            if (it >= 0 && !_tagged)
            {
                for (int j = 0; j < n; j++)
                {
                    if (j == it || Its[j] == null)
                    {
                        Reach[j] = false;
                        continue;
                    }
                    float dx = X[j] - X[it];
                    float dz = Z[j] - Z[it];
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    bool close = dist <= MatchBook.PunchReach && dist > 0.05f;
                    if (close && !Reach[j])
                        MatchBook.NoteNear(it, dist);
                    else if (close)
                        MatchHighlight.ConsiderMiss(dist);
                    Reach[j] = close;
                }
            }
            else
            {
                for (int j = 0; j < n; j++)
                    Reach[j] = false;
            }
            _tagged = false;
            MatchHighlight.Offer(_dt, n, X, Y, Z, Yaw, Pose);
        }

        public static void NoteTag(ItController from, ItController to)
        {
            int a = Index(from);
            int b = Index(to);
            if (a < 0 || b < 0) return;
            _tagged = true;
            MatchBook.NoteTag(a, b);
        }

        public static void NoteLanded(ItController who)
        {
            int i = Index(who);
            if (i >= 0) MatchBook.NoteLanded(i);
        }

        public static void NoteWhiff(ItController who)
        {
            int i = Index(who);
            if (i >= 0) MatchBook.NoteWhiff(i);
        }

        public static void NoteStagger(ItController who)
        {
            int i = Index(who);
            if (i >= 0) MatchBook.NoteStagger(i);
        }

        public static void NoteBlocked(ItController who)
        {
            int i = Index(who);
            if (i >= 0) MatchBook.NoteBlocked(i);
        }

        public static void Seal()
        {
            if (!_open && MatchBook.Count < 1) return;
            _open = false;
            MatchBook.Seal();
            Tag.Profiles.LocalProfiles.Absorb();
        }

        static int Index(ItController who)
        {
            if (who == null) return -1;
            int n = _bound;
            for (int i = 0; i < n; i++)
            {
                if (Its[i] == who) return i;
            }
            return -1;
        }

        static byte PoseOf(PlayerMotor motor, bool wall, bool dash, bool zip, bool pad, bool air)
        {
            if (wall) return PracticeVerb.WallRun;
            if (dash) return PracticeVerb.AirDash;
            if (zip) return PracticeVerb.Zip;
            if (pad) return PracticeVerb.Pad;
            if (motor != null && motor.IsSliding) return PracticeVerb.Slide;
            if (air) return PracticeVerb.Jump;
            return PracticeVerb.None;
        }

        static string HudName(int i)
        {
            return "P" + Tag.Core.HudDigits.Whole0(i + 1);
        }
    }
}
