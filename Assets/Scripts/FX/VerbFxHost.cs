using Tag.Art;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled verb FX. Rings, ghosts, rope sparks, pad wind, zip sparks,
    /// dizzy stars, a tag-back rim, wall streaks, and sprint wisps.
    /// Reduced flashing and Effects Off hide all of it. The motor is not written.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class VerbFxHost : MonoBehaviour
    {
        const int Ghosts = 4;
        const int Streaks = 4;
        const int Wisps = 4;
        const int Stars = 3;
        const int RingSeg = 20;

        PlayerMotor _motor;
        DummyLocomotor _loco;
        FxBurstPool _fx;
        ExperimentalGrapple _grapple;
        ItController _it;
        TagRole _role;
        int _seat;
        bool _ready;

        bool _prevGround;
        bool _prevDash;
        bool _prevGrapple;
        bool _prevLaunch;
        bool _prevZip;
        float _prevSurf;
        int _wallId;
        int _wallSurf = (int)DustLook.Surface.Concrete;
        int _groundId;
        int _groundSurf = (int)DustLook.Surface.Concrete;

        float _ringAge = -1f;
        float _ringR;
        float _swirlAge = -1f;
        float _swirlGap;
        Vector3 _swirlDir;
        Vector3 _swirlPos;
        float _snapAge = -1f;
        Vector3 _snapPos;
        float _padAge = -1f;
        float _ghostGap;
        int _ghostCursor;
        readonly float[] _ghostAge = new float[Ghosts];
        readonly Vector3[] _ghostPos = new Vector3[Ghosts];
        readonly float[] _streakAge = new float[Streaks];
        readonly Vector3[] _streakA = new Vector3[Streaks];
        readonly Vector3[] _streakB = new Vector3[Streaks];
        int _streakCursor;
        readonly Vector3[] _wispPos = new Vector3[Wisps];
        float _wispGap;
        int _wispCount;
        float _zipGap;
        float _colorR = 0.95f;
        float _colorG = 0.28f;
        float _colorB = 0.32f;

        LineRenderer _ring;
        LineRenderer _pad;
        LineRenderer _shimmer;
        LineRenderer _snap;
        LineRenderer[] _streaks;
        LineRenderer[] _rims;
        Transform[] _ghosts;
        Material[] _ghostMat;
        Transform[] _wisps;
        Material[] _wispMat;
        Transform _dizzy;
        Transform[] _stars;
        Transform _wind;
        Material _windMat;
        RaycastHit _hookHit;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<VerbFxHost>() == null)
                host.AddComponent<VerbFxHost>();
        }

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _loco = GetComponent<DummyLocomotor>();
            _fx = FxBurstPool.Ensure(transform);
            _grapple = GetComponent<Tag.Experimental.ExperimentalGrapple>();
            _it = GetComponent<ItController>();
            _role = GetComponent<TagRole>();
            CacheSeat();
            VerbFxLook.PlayerColor(_seat, out _colorR, out _colorG, out _colorB);
            _ring = MakeLine("LandRing", new Color(0.85f, 0.72f, 0.48f, 0.85f));
            _pad = MakeLine("PadRing", new Color(0.45f, 0.85f, 1f, 0.9f));
            _shimmer = MakeLine("ZipShimmer", new Color(0.85f, 0.45f, 1f, 0.7f));
            _snap = MakeLine("RopeSnap", new Color(1f, 0.9f, 0.4f, 0.9f));
            _streaks = new LineRenderer[Streaks];
            for (int i = 0; i < Streaks; i++)
            {
                _streaks[i] = MakeLine("WallStreak", new Color(0.8f, 0.78f, 0.72f, 0.7f));
                _streakAge[i] = -1f;
            }
            _rims = new LineRenderer[2];
            _rims[0] = MakeLine("TagRimLow", new Color(_colorR, _colorG, _colorB, 0.7f));
            _rims[1] = MakeLine("TagRimHigh", new Color(_colorR, _colorG, _colorB, 0.7f));
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Mesh quad = Quad();
            Mesh star = Star();
            _ghosts = new Transform[Ghosts];
            _ghostMat = new Material[Ghosts];
            for (int i = 0; i < Ghosts; i++)
            {
                _ghostAge[i] = -1f;
                _ghosts[i] = MakeQuad(transform, "DashGhost", quad, shader, out _ghostMat[i]);
            }
            _wisps = new Transform[Wisps];
            _wispMat = new Material[Wisps];
            for (int i = 0; i < Wisps; i++)
                _wisps[i] = MakeQuad(transform, "SpeedWisp", quad, shader, out _wispMat[i]);
            _wind = MakeQuad(transform, "PadWind", quad, shader, out _windMat);
            _dizzy = new GameObject("Dizzy").transform;
            _dizzy.SetParent(transform, false);
            _stars = new Transform[Stars];
            for (int i = 0; i < Stars; i++)
            {
                Material mat;
                _stars[i] = MakeQuad(_dizzy, "Star", star, shader, out mat);
                _stars[i].SetParent(_dizzy, false);
                ComicWords.ColorOf(i, out float r, out float g, out float b);
                mat.color = new Color(r, g, b, 1f);
            }
            _dizzy.gameObject.SetActive(false);
            _ready = _motor != null && _fx != null;
        }

        void LateUpdate()
        {
            if (!_ready) return;
            bool show = FxAmount.Show(GameSettings.Current);
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (!show)
            {
                HideAll();
                Remember();
                return;
            }
            float speed = _motor.HorizSpeed;
            bool grounded = _motor.Ground.grounded;
            if (grounded)
                _groundSurf = DustContact.Read(_motor.Ground.collider, ref _groundId, ref _groundSurf);
            TickLand(grounded, speed);
            TickDash(dt);
            TickGrapple();
            TickLaunch(dt);
            TickZip(dt, speed);
            TickStagger(dt);
            TickRim();
            TickWall(dt, speed);
            TickWisps(dt, speed);
            FadeRing(_ring, ref _ringAge, 0.36f, _ringR, dt, 0.85f, 0.72f, 0.48f);
            FadeRing(_pad, ref _padAge, 0.32f, VerbFxLook.PadRing, dt, 0.45f, 0.85f, 1f);
            FadeSnap(dt);
            FadeStreaks(dt);
            FadeGhosts(dt);
            Remember();
        }

        void TickLand(bool grounded, float speed)
        {
            if (grounded && !_prevGround)
            {
                float impact = _motor.LastLandImpactSpeed;
                if (impact >= LandingRollPose.SoftFloor)
                {
                    _ringAge = 0f;
                    _ringR = VerbFxLook.LandRing(impact);
                    Vector3 pos = _motor.transform.position;
                    pos.y += 0.05f;
                    DustLook.Puff puff = DustLook.At(_groundSurf, speed, (int)DustLook.Kick.None);
                    puff.Count = VerbFxLook.Debris(impact, 1f);
                    puff.Size *= 1.15f;
                    _fx.PlayShaped(FxBurstKind.Land, pos, puff);
                    if (VerbFxLook.RollSwirl(impact, speed))
                    {
                        _swirlAge = 0f;
                        _swirlGap = 0f;
                        Vector3 v = _motor.Velocity;
                        v.y = 0f;
                        if (v.sqrMagnitude < 0.25f) v = _motor.transform.forward;
                        _swirlDir = v.normalized;
                        _swirlPos = pos;
                    }
                }
            }
            if (_swirlAge >= 0f)
            {
                float step = Time.deltaTime;
                _swirlAge += step;
                _swirlGap -= step;
                if (_swirlAge >= LandingRollPose.Seconds)
                    _swirlAge = -1f;
                else if (_swirlGap <= 0f)
                {
                    _swirlGap = 0.06f;
                    Vector3 p = _swirlPos + _swirlDir * (_swirlAge * 2.2f);
                    DustLook.Puff swirl = DustLook.At(_groundSurf, DustLook.Sprint, (int)DustLook.Kick.Trail);
                    swirl.Count = 2;
                    _fx.PlayShaped(FxBurstKind.Roll, p, swirl);
                }
            }
        }

        void TickDash(float dt)
        {
            bool dash = _motor.IsAirDashing;
            if (dash && !_prevDash)
            {
                _ghostGap = 0f;
                _ghostCursor = 0;
                DropGhost(_motor.transform.position + Vector3.up * 0.9f);
            }
            else if (dash)
            {
                _ghostGap += dt;
                float step = VerbFxLook.DashSeconds / VerbFxLook.GhostsFull;
                if (_ghostGap >= step && _ghostCursor < Ghosts)
                {
                    _ghostGap = 0f;
                    DropGhost(_motor.transform.position + Vector3.up * 0.9f);
                }
            }
        }

        void TickGrapple()
        {
            bool on = _grapple != null && _grapple.IsPulling;
            if (on && !_prevGrapple)
            {
                Vector3 anchor;
                float length;
                float slack;
                if (_grapple.TryGetRope(out anchor, out length, out slack))
                {
                    int surf = _groundSurf;
                    if (Physics.Raycast(anchor + Vector3.up * 0.2f, Vector3.down, out _hookHit, 1.2f))
                        surf = DustContact.Read(_hookHit.collider, ref _groundId, ref surf);
                    DustLook.Puff puff = DustLook.At(surf, DustLook.Sprint, (int)DustLook.Kick.Pivot);
                    puff.Count = VerbFxLook.HookBits(surf, true);
                    if (surf == (int)DustLook.Surface.Metal) puff.Spark = 1;
                    _fx.PlayShaped(FxBurstKind.VaultPuff, anchor, puff);
                }
            }
            if (!on && _prevGrapple)
            {
                _snapAge = 0f;
                _snapPos = _motor.transform.position + Vector3.up * 1.1f;
                DustLook.Puff puff = DustLook.At(_groundSurf, DustLook.Walk, (int)DustLook.Kick.None);
                puff.Count = 4;
                puff.Life = 0.12f;
                _fx.PlayShaped(FxBurstKind.VaultPuff, _snapPos, puff);
            }
        }

        void TickLaunch(float dt)
        {
            bool arc = _motor.LaunchArc;
            if (arc && !_prevLaunch)
            {
                _padAge = 0f;
                Vector3 pos = _motor.transform.position;
                DustLook.Puff puff = DustLook.At(_groundSurf, DustLook.Sprint, (int)DustLook.Kick.None);
                puff.Count = 6;
                puff.R = 0.55f;
                puff.G = 0.85f;
                puff.B = 1f;
                _fx.PlayShaped(FxBurstKind.Land, pos, puff);
            }
            bool rise = arc && _motor.Velocity.y > 0.5f;
            _wind.gameObject.SetActive(rise);
            if (rise)
            {
                _wind.position = _motor.transform.position + Vector3.up * 1.05f - _motor.transform.forward * 0.35f;
                float a = 0.35f + 0.25f * Mathf.Sin(Time.time * 20f);
                Color c = _windMat.color;
                c.r = 0.75f;
                c.g = 0.92f;
                c.b = 1f;
                c.a = a;
                _windMat.color = c;
                _wind.localScale = new Vector3(0.12f, 0.7f, 1f);
            }
        }

        void TickZip(float dt, float speed)
        {
            bool ride = _motor.ZipRiding;
            _shimmer.enabled = ride;
            if (!ride) return;
            Vector3 v = _motor.Velocity;
            if (v.sqrMagnitude < 0.01f) v = _motor.transform.forward;
            v.Normalize();
            Vector3 side = Vector3.Cross(v, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 origin = _motor.transform.position + Vector3.up * 1.2f;
            int n = 6;
            if (_shimmer.positionCount != n) _shimmer.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float wob = VerbFxLook.ShimmerAmp * Mathf.Sin(Time.time * 16f + t * 8f);
                _shimmer.SetPosition(i, origin + v * ((t - 0.5f) * 1.6f) + side * wob);
            }
            _zipGap -= dt;
            if (_zipGap <= 0f)
            {
                _zipGap = 0.05f;
                DustLook.Puff puff = DustLook.At((int)DustLook.Surface.Metal, speed, (int)DustLook.Kick.Pivot);
                puff.Spark = 1;
                puff.Count = VerbFxLook.ZipSparks(speed, 1f);
                if (puff.Count > 4) puff.Count = 4;
                _fx.PlayShaped(FxBurstKind.WallScuff, origin, puff);
            }
        }

        void TickStagger(float dt)
        {
            bool on = _motor.IsPunchStaggered;
            _dizzy.gameObject.SetActive(on);
            if (!on) return;
            _dizzy.position = _motor.transform.position + Vector3.up * 1.85f;
            _dizzy.Rotate(0f, 140f * dt, 0f, Space.World);
            float spin = Time.time * 3f;
            for (int i = 0; i < Stars; i++)
            {
                float a = spin + i * 2.094f;
                _stars[i].localPosition = new Vector3(Mathf.Cos(a) * 0.28f, Mathf.Sin(a) * 0.1f, 0f);
                _stars[i].localScale = new Vector3(0.18f, 0.18f, 1f);
            }
        }

        void TickRim()
        {
            float left = 0f;
            if (_it != null && _it.TagBackRemaining > left) left = _it.TagBackRemaining;
            if (_role != null && _role.TagBackRemaining > left) left = _role.TagBackRemaining;
            float pulse = VerbFxLook.Rim(left, TagBackImmunity.DefaultSeconds, Time.time);
            bool on = pulse > 0.02f;
            _rims[0].enabled = on;
            _rims[1].enabled = on;
            if (!on) return;
            float radius = 0.46f + 0.06f * pulse;
            Color c = new Color(_colorR, _colorG, _colorB, 0.25f + 0.55f * pulse);
            PlaceRing(_rims[0], _motor.transform.position + Vector3.up * (0.7f + 0.04f * pulse), radius, c);
            PlaceRing(_rims[1], _motor.transform.position + Vector3.up * (1.25f + 0.04f * pulse), radius * 0.92f, c);
        }

        void TickWall(float dt, float speed)
        {
            if (_motor.State == MoveState.WallRun && _loco != null)
            {
                _wallSurf = DustContact.Read(_motor.WallCollider, ref _wallId, ref _wallSurf);
                float surf = _loco.SurfPhase;
                bool left;
                if (DustLook.FootDown(_prevSurf, surf, out left))
                {
                    Vector3 point = _motor.WallPoint;
                    if (point.sqrMagnitude < 0.0001f)
                        point = _motor.transform.position + Vector3.up * 0.5f;
                    Vector3 along = _motor.Velocity;
                    along.y = 0f;
                    if (along.sqrMagnitude < 0.04f) along = _motor.transform.forward;
                    along.Normalize();
                    float len = VerbFxLook.ScuffLength(speed);
                    int slot = _streakCursor % Streaks;
                    _streakCursor++;
                    _streakAge[slot] = 0f;
                    _streakA[slot] = point;
                    _streakB[slot] = point - along * len;
                    if (VerbFxLook.Drip(_wallSurf))
                    {
                        DustLook.Puff puff = DustLook.At(_wallSurf, speed, (int)DustLook.Kick.None);
                        puff.Count = 3;
                        _fx.PlayShaped(FxBurstKind.WallScuff, point, puff);
                    }
                }
                _prevSurf = surf;
            }
            else if (_loco != null)
                _prevSurf = _loco.SurfPhase;
        }

        void TickWisps(float dt, float speed)
        {
            int want = VerbFxLook.Wisps(speed, FxAmount.Density(GameSettings.Current));
            _wispGap -= dt;
            if (want > 0 && _wispGap <= 0f)
            {
                _wispGap = 0.06f;
                _wispPos[_wispCount % Wisps] = _motor.transform.position + Vector3.up * 0.8f;
                _wispCount++;
            }
            for (int i = 0; i < Wisps; i++)
            {
                bool on = want > 0 && i < want;
                _wisps[i].gameObject.SetActive(on);
                if (!on) continue;
                int idx = (_wispCount - 1 - i) % Wisps;
                if (idx < 0) idx += Wisps;
                _wisps[i].position = _wispPos[idx] - _motor.transform.forward * (0.15f * i);
                float a = 0.45f - i * 0.08f;
                Color c = _wispMat[i].color;
                c.r = 0.9f;
                c.g = 0.92f;
                c.b = 1f;
                c.a = a;
                _wispMat[i].color = c;
                _wisps[i].localScale = new Vector3(0.16f, 0.28f, 1f);
            }
        }

        void DropGhost(Vector3 pos)
        {
            int slot = _ghostCursor % Ghosts;
            _ghostCursor++;
            _ghostAge[slot] = 0f;
            _ghostPos[slot] = pos;
        }

        void FadeGhosts(float dt)
        {
            for (int i = 0; i < Ghosts; i++)
            {
                if (_ghostAge[i] < 0f)
                {
                    _ghosts[i].gameObject.SetActive(false);
                    continue;
                }
                _ghostAge[i] += dt;
                if (_ghostAge[i] >= VerbFxLook.GhostLife)
                {
                    _ghostAge[i] = -1f;
                    _ghosts[i].gameObject.SetActive(false);
                    continue;
                }
                _ghosts[i].gameObject.SetActive(true);
                _ghosts[i].position = _ghostPos[i];
                float u = 1f - _ghostAge[i] / VerbFxLook.GhostLife;
                Color c = _ghostMat[i].color;
                c.r = _colorR;
                c.g = _colorG;
                c.b = _colorB;
                c.a = 0.55f * u;
                _ghostMat[i].color = c;
                _ghosts[i].localScale = new Vector3(0.42f, 0.9f, 1f);
            }
        }

        void FadeRing(LineRenderer line, ref float age, float life, float radius, float dt, float r, float g, float b)
        {
            if (age < 0f)
            {
                line.enabled = false;
                return;
            }
            age += dt;
            if (age >= life)
            {
                age = -1f;
                line.enabled = false;
                return;
            }
            float u = 1f - age / life;
            float rad = radius * (0.7f + 0.5f * (age / life));
            Color c = new Color(r, g, b, 0.8f * u);
            PlaceRing(line, _motor.transform.position + Vector3.up * 0.06f, rad, c);
        }

        void FadeSnap(float dt)
        {
            if (_snapAge < 0f)
            {
                _snap.enabled = false;
                return;
            }
            _snapAge += dt;
            if (_snapAge >= VerbFxLook.ReleaseSeconds)
            {
                _snapAge = -1f;
                _snap.enabled = false;
                return;
            }
            float u = 1f - _snapAge / VerbFxLook.ReleaseSeconds;
            _snap.enabled = true;
            if (_snap.positionCount != 2) _snap.positionCount = 2;
            _snap.SetPosition(0, _snapPos);
            _snap.SetPosition(1, _snapPos + Vector3.up * (0.15f + u * 0.45f));
            _snap.startWidth = 0.08f * u;
            _snap.endWidth = 0.01f;
        }

        void FadeStreaks(float dt)
        {
            for (int i = 0; i < Streaks; i++)
            {
                if (_streakAge[i] < 0f)
                {
                    _streaks[i].enabled = false;
                    continue;
                }
                _streakAge[i] += dt;
                if (_streakAge[i] > 0.28f)
                {
                    _streakAge[i] = -1f;
                    _streaks[i].enabled = false;
                    continue;
                }
                _streaks[i].enabled = true;
                if (_streaks[i].positionCount != 2) _streaks[i].positionCount = 2;
                _streaks[i].SetPosition(0, _streakA[i]);
                _streaks[i].SetPosition(1, _streakB[i]);
                float u = 1f - _streakAge[i] / 0.28f;
                _streaks[i].startWidth = 0.07f * u;
                _streaks[i].endWidth = 0.02f * u;
            }
        }

        void PlaceRing(LineRenderer line, Vector3 center, float radius, Color color)
        {
            line.enabled = true;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = 0.045f;
            line.endWidth = 0.045f;
            if (line.positionCount != RingSeg) line.positionCount = RingSeg;
            for (int i = 0; i < RingSeg; i++)
            {
                float a = i / (float)(RingSeg - 1) * 6.2831855f;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        void HideAll()
        {
            _ringAge = -1f;
            _swirlAge = -1f;
            _snapAge = -1f;
            _padAge = -1f;
            if (_ring != null) _ring.enabled = false;
            if (_pad != null) _pad.enabled = false;
            if (_shimmer != null) _shimmer.enabled = false;
            if (_snap != null) _snap.enabled = false;
            if (_wind != null) _wind.gameObject.SetActive(false);
            if (_dizzy != null) _dizzy.gameObject.SetActive(false);
            if (_rims != null)
            {
                _rims[0].enabled = false;
                _rims[1].enabled = false;
            }
            if (_streaks != null)
            {
                for (int i = 0; i < Streaks; i++)
                {
                    _streakAge[i] = -1f;
                    _streaks[i].enabled = false;
                }
            }
            if (_ghosts != null)
            {
                for (int i = 0; i < Ghosts; i++)
                {
                    _ghostAge[i] = -1f;
                    _ghosts[i].gameObject.SetActive(false);
                }
            }
            if (_wisps != null)
            {
                for (int i = 0; i < Wisps; i++)
                    _wisps[i].gameObject.SetActive(false);
            }
        }

        void Remember()
        {
            _prevGround = _motor.Ground.grounded;
            _prevDash = _motor.IsAirDashing;
            _prevGrapple = _grapple != null && _grapple.IsPulling;
            _prevLaunch = _motor.LaunchArc;
            _prevZip = _motor.ZipRiding;
        }

        void CacheSeat()
        {
            _seat = 0;
            string n = gameObject.name;
            if (string.IsNullOrEmpty(n)) return;
            char c = n[n.Length - 1];
            if (c >= '1' && c <= '4') _seat = c - '1';
        }

        LineRenderer MakeLine(string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startWidth = 0.04f;
            line.endWidth = 0.04f;
            line.startColor = color;
            line.endColor = color;
            line.material = LineMat(color);
            line.enabled = false;
            return line;
        }

        static Material LineMat(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        static Transform MakeQuad(Transform parent, string name, Mesh mesh, Shader shader, out Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            mat = new Material(shader);
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.SetActive(false);
            return go.transform;
        }

        static Mesh Quad()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        static Mesh Star()
        {
            const int spikes = 10;
            var verts = new Vector3[spikes * 2 + 1];
            var uv = new Vector2[verts.Length];
            var tris = new int[spikes * 6];
            verts[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < spikes; i++)
            {
                ComicArt.Point(i, spikes, false, out float vx, out float vy);
                ComicArt.Point(i, spikes, true, out float tx, out float ty);
                verts[1 + i * 2] = new Vector3(vx, vy, 0f);
                verts[2 + i * 2] = new Vector3(tx, ty, 0f);
                uv[1 + i * 2] = new Vector2(0.5f, 0.5f);
                uv[2 + i * 2] = new Vector2(0.5f, 0.5f);
                int t = i * 6;
                tris[t] = 0;
                tris[t + 1] = 1 + i * 2;
                tris[t + 2] = 2 + i * 2;
                tris[t + 3] = 0;
                tris[t + 4] = 2 + i * 2;
                tris[t + 5] = 1 + ((i + 1) % spikes) * 2;
            }
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.triangles = tris;
            return mesh;
        }
    }
}
