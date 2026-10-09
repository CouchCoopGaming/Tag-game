using Tag.Art;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled draws for one pawn. Tick does not allocate. The motor is not written.
    /// </summary>
    public sealed class FxKitSim
    {
        struct Mote
        {
            public float Age;
            public float Life;
            public float Size;
            public float R, G, B, Shape;
            public Vector3 P, V;
            public Renderer Rend;
        }

        public PlayerMotor Motor;
        public DummyLocomotor Loco;
        public ExperimentalGrapple Grapple;
        public ItController It;
        public TagRole Role;

        Transform _root;
        Transform _footL, _footR, _handL, _handR, _head, _spine, _hips, _armL, _armR;
        MaterialPropertyBlock _block;
        int _seat;
        float _cr = 0.95f, _cg = 0.28f, _cb = 0.32f;

        bool _primed;
        bool _prevGround;
        bool _prevPull;
        bool _prevLaunch;
        bool _prevStagger;
        bool _onWall;
        int _prevTags;
        float _prevSurf;
        int _groundId;
        int _groundSurf = (int)DustLook.Surface.Concrete;
        int _wallId;
        int _wallSurf = (int)DustLook.Surface.Concrete;
        int _hookId;
        int _hookSurf = (int)DustLook.Surface.Concrete;

        float _landAge = -1f;
        float _landScale;
        float _landFloor;
        bool _landRoll;
        int _landSurf;
        Vector3 _landPos;

        float _sparkAge = -1f;
        float _launchAge = -1f;
        float _starAge = -1f;
        float _flashAge = -1f;
        Vector3 _launchPos;
        Vector3 _anchor;
        bool _ropeOn;

        readonly float[] _scuffAge = new float[FxKitLook.Scuffs];
        readonly Vector3[] _scuffA = new Vector3[FxKitLook.Scuffs];
        readonly Vector3[] _scuffB = new Vector3[FxKitLook.Scuffs];
        int _scuffCursor;
        int _footCursor;
        readonly float[] _streakAge = new float[FxKitLook.Streaks];
        readonly Vector3[] _streakA = new Vector3[FxKitLook.Streaks];
        readonly Vector3[] _streakB = new Vector3[FxKitLook.Streaks];
        const int AirLines = 14;
        const int AirLong = 8;
        const float AirLife = 0.15f;
        readonly byte[] _airWide = new byte[AirLines];
        readonly float[] _airAge = new float[AirLines];
        readonly Vector3[] _airA = new Vector3[AirLines];
        readonly Vector3[] _airB = new Vector3[AirLines];
        int _airCursor;
        float _airGap;
        bool _airWas;

        Mote[] _dust;
        Mote[] _spark;
        Mote[] _debris;
        Mote[] _foot;
        Renderer[] _shell;
        Renderer[] _stars;
        LineRenderer _shock;
        LineRenderer _dustRing;
        LineRenderer _rope;
        LineRenderer _launchRing;
        LineRenderer[] _scuffLines;
        LineRenderer[] _streaks;
        LineRenderer[] _air;
        Renderer _disc;
        Transform _discT;

        Camera _cam;
        Renderer[] _bars;
        Transform[] _barT;

        static Mesh _quad;
        static Material _sprite;
        static Material _edge;
        static Material _line;
        static Material _rimMat;

        public void Build(Transform root)
        {
            _root = root;
            _block = new MaterialPropertyBlock();
            CacheSeat(root.gameObject);
            _footL = Find(root, "Foot_L");
            _footR = Find(root, "Foot_R");
            _handL = Find(root, "Hand_L");
            _handR = Find(root, "Hand_R");
            _head = Find(root, "Head");
            _spine = Find(root, "Spine");
            _hips = Find(root, "Hips");
            _armL = Find(root, "UpperArm_L");
            _armR = Find(root, "UpperArm_R");
            EnsureShared();
            _dust = MakeMotes(FxKitLook.DustRoll, "FxDust");
            _spark = MakeMotes(FxKitLook.SparkFull, "FxSpark");
            _debris = MakeMotes(FxKitLook.DebrisFull, "FxDebris");
            _foot = MakeMotes(FxKitLook.FootDust, "FxFoot");
            BuildShells(root);
            _stars = MakeCards(FxKitLook.Stars, "FxStar");
            _shock = MakeLine("FxShock");
            _dustRing = MakeLine("FxDustRing");
            _rope = MakeLine("FxRopeShimmer");
            _launchRing = MakeLine("FxLaunchRing");
            _scuffLines = new LineRenderer[FxKitLook.Scuffs];
            for (int i = 0; i < FxKitLook.Scuffs; i++)
            {
                _scuffLines[i] = MakeLine("FxScuff");
                _scuffAge[i] = -1f;
            }
            _streaks = new LineRenderer[FxKitLook.Streaks];
            for (int i = 0; i < FxKitLook.Streaks; i++)
            {
                _streaks[i] = MakeLine("FxStreak");
                _streakAge[i] = -1f;
            }
            _air = new LineRenderer[AirLines];
            for (int i = 0; i < AirLines; i++)
            {
                _air[i] = MakeLine("FxAir");
                _airAge[i] = -1f;
            }
            _discT = MakeQuad(root, "FxDisc", _sprite, out _disc);
            _discT.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _disc.enabled = false;
            if (It != null) _prevTags = It.TagsLanded;
        }

        public void BindCamera()
        {
            RefreshSeat();
            if (_cam != null || _root == null) return;
            Camera cam = null;
            if (It != null) cam = It.GetComponentInChildren<Camera>();
            if (cam == null) cam = _root.GetComponentInChildren<Camera>();
            if (cam == null) return;
            _cam = cam;
            EnsureShared();
            _bars = new Renderer[4];
            _barT = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                _barT[i] = MakeQuad(cam.transform, "FxEdge", _edge, out _bars[i]);
                _bars[i].enabled = false;
            }
        }

        public void Tick(float dt)
        {
            if (Motor == null || _root == null) return;
            if (!_primed)
            {
                Remember();
                _primed = true;
                return;
            }
            GameSettings settings = GameSettings.Current;
            if (!FxKitLook.Master(settings))
            {
                HideActive();
                Remember();
                return;
            }
            float density = FxKitLook.Density(settings);
            bool grounded = Motor.Ground.grounded;
            if (grounded)
                _groundSurf = DustContact.Read(Motor.Ground.collider, ref _groundId, ref _groundSurf);
            TickLand(dt, settings, density, grounded);
            TickGrapple(dt, settings, density);
            TickImmunity(settings);
            TickStagger(dt, settings);
            TickLaunch(dt, settings, density);
            TickWall(dt, settings, density);
            TickFlash(dt, settings);
            TickAir(dt, settings);
            Remember();
        }

        public void Hide()
        {
            HideActive();
        }

        void TickLand(float dt, GameSettings settings, float density, bool grounded)
        {
            if (grounded && !_prevGround && FxKitLook.Bursts(settings, FxKitOptions.Land))
            {
                float impact = Motor.LastLandImpactSpeed;
                if (FxKitLook.Hard(impact))
                {
                    _landAge = 0.0001f;
                    _landRoll = FxKitLook.IsRoll(impact, Motor.HorizSpeed);
                    _landScale = _landRoll ? FxKitLook.RollScale(impact) : FxKitLook.LandScale(impact);
                    _landPos = _root.position;
                    _landFloor = _landPos.y + 0.03f;
                    _landSurf = _groundSurf;
                    int n = FxKitLook.DustCount(impact, Motor.HorizSpeed, density);
                    SpawnDust(n, _landPos, _landScale, _landRoll ? 1.6f : 1.1f);
                }
            }
            if (_landAge < 0f)
            {
                if (_shock != null) _shock.enabled = false;
                if (_dustRing != null) _dustRing.enabled = false;
                if (_disc != null) _disc.enabled = false;
                return;
            }
            _landAge += dt;
            float life = _landRoll ? FxKitLook.RollLife : FxKitLook.LandLife;
            if (_landAge >= life)
            {
                _landAge = -1f;
                Clear(_dust);
                if (_shock != null) _shock.enabled = false;
                if (_dustRing != null) _dustRing.enabled = false;
                if (_disc != null) _disc.enabled = false;
                return;
            }
            Integrate(_dust, dt, 6.5f, _landFloor);
            float fade = FxKitLook.Fade(_landAge, life);
            float rad = FxKitLook.RingRadius(_landScale, _landAge, life);
            FxKitLook.SurfaceColor(_landSurf, out float sr, out float sg, out float sb);
            Ring(_dustRing, _landPos, rad * 0.72f, 0.09f * fade, sr, sg, sb, 0.75f * fade);
            Ring(_shock, _landPos, rad, 0.035f * fade, 0.93f, 0.91f, 0.84f, 0.85f * fade);
            if (_disc != null)
            {
                _disc.enabled = fade > 0.02f;
                _discT.position = _landPos + Vector3.up * 0.025f;
                _discT.rotation = Quaternion.Euler(90f, 0f, 0f);
                float d = rad * 1.35f;
                _discT.localScale = new Vector3(d, d, 1f);
                Paint(_disc, sr, sg, sb, 0.28f * fade, 0f, 0f, -1f);
            }
        }

        void TickGrapple(float dt, GameSettings settings, float density)
        {
            bool pull = Grapple != null && Grapple.IsPulling;
            bool show = FxKitLook.Bursts(settings, FxKitOptions.Grapple);
            if (pull && !_prevPull && show)
            {
                Vector3 anchor;
                float length;
                float slack;
                if (Grapple.TryGetRope(out anchor, out length, out slack))
                {
                    _anchor = anchor;
                    _hookSurf = _groundSurf;
                    RaycastHit hit;
                    if (Physics.Raycast(anchor + Vector3.up * 0.3f, Vector3.down, out hit, 1.6f))
                        _hookSurf = DustContact.Read(hit.collider, ref _hookId, ref _hookSurf);
                    int sparks = FxKitLook.Scaled(FxKitLook.SparkFull, density);
                    int bits = FxKitLook.Scaled(FxKitLook.DebrisFull, density);
                    SpawnSparks(sparks, anchor);
                    SpawnDebris(bits, anchor, _hookSurf);
                    _sparkAge = 0.0001f;
                }
            }
            if (_sparkAge >= 0f)
            {
                _sparkAge += dt;
                Integrate(_spark, dt, 2.2f, _anchor.y + 0.02f);
                Integrate(_debris, dt, 7.5f, _anchor.y + 0.02f);
                if (_sparkAge > FxKitLook.DebrisLife)
                    _sparkAge = -1f;
            }
            _ropeOn = pull && show;
            if (!_ropeOn || _rope == null)
            {
                if (_rope != null) _rope.enabled = false;
                return;
            }
            Vector3 hand = _handL != null ? _handL.position : _root.position + Vector3.up * 1.15f;
            Vector3 anchorNow;
            float ropeLen;
            float slackNow;
            if (!Grapple.TryGetRope(out anchorNow, out ropeLen, out slackNow))
            {
                _rope.enabled = false;
                return;
            }
            _anchor = anchorNow;
            Vector3 delta = anchorNow - hand;
            float dist = delta.magnitude;
            if (dist < 0.05f)
            {
                _rope.enabled = false;
                return;
            }
            Vector3 dir = delta / dist;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            else side.Normalize();
            float tension = ropeLen > 0.05f ? dist / ropeLen : 1f;
            if (tension > 1f) tension = 1f;
            int n = _rope.positionCount;
            _rope.enabled = true;
            _rope.startWidth = 0.018f;
            _rope.endWidth = 0.016f;
            Color c = new Color(1f, 0.9f, 0.55f, 0.45f);
            _rope.startColor = c;
            _rope.endColor = c;
            float time = Time.time;
            for (int i = 0; i < n; i++)
            {
                float t = n <= 1 ? 0f : i / (float)(n - 1);
                // Drawn sag stays under 1 cm. Shimmer() is unchanged, so the proof sample stays 1.8.
                float wob = FxKitLook.Shimmer(t, tension, time) * (0.009f / FxKitLook.ShimmerAmp);
                _rope.SetPosition(i, hand + dir * (dist * t) + side * wob);
            }
        }

        void TickImmunity(GameSettings settings)
        {
            float left = 0f;
            if (It != null && It.TagBackRemaining > left) left = It.TagBackRemaining;
            if (Role != null && Role.TagBackRemaining > left) left = Role.TagBackRemaining;
            bool show = FxKitLook.Immunity(settings) && left > 0.001f;
            float a = 0f;
            if (show)
                a = FxKitLook.RimAlpha(left, FxKitLook.ImmunitySeconds, Time.time, FxKitLook.SteadyGlow(settings));
            if (_shell == null) return;
            for (int i = 0; i < _shell.Length; i++)
            {
                if (_shell[i] == null) continue;
                if (!show || a < 0.02f)
                {
                    _shell[i].enabled = false;
                    continue;
                }
                _shell[i].enabled = true;
                Color c = new Color(_cr, _cg, _cb, a * 0.85f);
                _block.SetColor("_Color", c);
                _block.SetColor("_BaseColor", c);
                _shell[i].SetPropertyBlock(_block);
            }
        }

        void TickStagger(float dt, GameSettings settings)
        {
            bool on = Motor.IsPunchStaggered;
            bool show = FxKitLook.Bursts(settings, FxKitOptions.Stagger);
            if (on && !_prevStagger && show)
                _starAge = 0.0001f;
            if (!on || !show)
            {
                _starAge = -1f;
                HideStars();
                return;
            }
            _starAge += dt;
            float life = FxKitLook.StaggerSeconds;
            float a = FxKitLook.Fade(_starAge, life);
            if (_starAge > life) a = FxKitLook.Fade(life * 0.5f, life) * 0.35f;
            Vector3 head = _head != null ? _head.position : _root.position + Vector3.up * 1.72f;
            head.y += 0.34f;
            float spin = Time.time * 8.5f;
            for (int i = 0; i < _stars.Length; i++)
            {
                float ang = spin + i * 6.2831855f / _stars.Length;
                float bob = Mathf.Sin(spin * 0.5f + i) * 0.04f;
                Vector3 p = head + new Vector3(Mathf.Cos(ang) * 0.20f, bob, Mathf.Sin(ang) * 0.20f);
                Place(_stars[i], p, 0.11f, 1f, 0.88f, 0.28f, a, 2f);
            }
        }

        void TickLaunch(float dt, GameSettings settings, float density)
        {
            bool arc = Motor.LaunchArc;
            bool show = FxKitLook.Bursts(settings, FxKitOptions.Launch);
            if (arc && !_prevLaunch && show)
            {
                _launchAge = 0.0001f;
                _launchPos = _root.position;
                int n = FxKitLook.Scaled(FxKitLook.Streaks, density);
                for (int i = 0; i < FxKitLook.Streaks; i++)
                {
                    _streakAge[i] = i < n ? 0.0001f : -1f;
                    float ang = i * 6.2831855f / FxKitLook.Streaks;
                    Vector3 side = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    _streakA[i] = _launchPos + side * 0.28f + Vector3.up * 0.05f;
                    _streakB[i] = _streakA[i] + Vector3.up * (1.15f + (i & 1) * 0.45f) + side * 0.08f;
                }
            }
            if (_launchAge < 0f)
            {
                if (_launchRing != null) _launchRing.enabled = false;
                HideStreaks();
                return;
            }
            _launchAge += dt;
            if (_launchAge >= FxKitLook.LaunchLife)
            {
                _launchAge = -1f;
                if (_launchRing != null) _launchRing.enabled = false;
                HideStreaks();
                return;
            }
            float fade = FxKitLook.Fade(_launchAge, FxKitLook.LaunchLife);
            Vector3 center = _launchPos;
            center.y += _launchAge * 4.4f;
            float rad = 0.32f + _launchAge * 2.6f;
            Ring(_launchRing, center, rad, 0.045f * fade, 0.5f, 0.9f, 1f, 0.9f * fade);
            for (int i = 0; i < FxKitLook.Streaks; i++)
            {
                if (_streakAge[i] < 0f)
                {
                    _streaks[i].enabled = false;
                    continue;
                }
                _streaks[i].enabled = true;
                Vector3 a = _streakA[i];
                Vector3 b = _streakB[i];
                a.y += _launchAge * 3.2f;
                b.y += _launchAge * 5.1f;
                _streaks[i].SetPosition(0, a);
                _streaks[i].SetPosition(1, b);
                _streaks[i].startWidth = 0.05f * fade;
                _streaks[i].endWidth = 0.012f * fade;
                Color c = new Color(0.7f, 0.94f, 1f, 0.8f * fade);
                _streaks[i].startColor = c;
                _streaks[i].endColor = new Color(0.7f, 0.94f, 1f, 0.05f);
            }
        }

        void TickWall(float dt, GameSettings settings, float density)
        {
            float surf = Loco != null ? Loco.SurfPhase : 0f;
            bool show = FxKitLook.Bursts(settings, FxKitOptions.Wall);
            if (!Motor.IsWallRunning || !show)
            {
                _onWall = false;
                _prevSurf = surf;
                FadeScuffs(dt);
                Integrate(_foot, dt, 3.5f, _root.position.y);
                return;
            }
            if (!_onWall)
            {
                _onWall = true;
                _prevSurf = surf;
                _wallSurf = DustContact.Read(Motor.WallCollider, ref _wallId, ref _wallSurf);
                Vector3 n0 = Motor.WallNormal;
                if (n0.sqrMagnitude < 0.0001f) n0 = -_root.forward;
                Vector3 p0 = Motor.WallPoint;
                if (p0.sqrMagnitude < 0.0001f)
                    p0 = _root.position + Vector3.up * 0.9f + n0 * 0.35f;
                ImpactFx.WallRunStart(p0, n0, Motor.HorizSpeed, _wallSurf, WallMaterialName());
                return;
            }
            _wallSurf = DustContact.Read(Motor.WallCollider, ref _wallId, ref _wallSurf);
            bool leftFoot = false;
            bool plant = Loco != null && DustLook.FootDown(_prevSurf, surf, out leftFoot);
            _prevSurf = surf;
            if (plant)
            {
                Vector3 n = Motor.WallNormal;
                if (n.sqrMagnitude < 0.0001f) n = -_root.forward;
                else n.Normalize();
                Transform wallFoot = Motor.WallLeft ? _footL : _footR;
                Transform gaitFoot = leftFoot ? _footL : _footR;
                Transform foot = wallFoot != null ? wallFoot : gaitFoot;
                Vector3 point = foot != null ? foot.position : Motor.WallPoint;
                if (point.sqrMagnitude < 0.0001f) point = _root.position + Vector3.up * 0.25f;
                point += n * 0.035f;
                Vector3 along = Motor.Velocity;
                along.y = 0f;
                if (along.sqrMagnitude < 0.04f) along = _root.forward;
                else along.Normalize();
                int slot = _scuffCursor % FxKitLook.Scuffs;
                _scuffCursor++;
                _scuffAge[slot] = 0.0001f;
                _scuffA[slot] = point;
                _scuffB[slot] = point - along * FxKitLook.ScuffLength(Motor.HorizSpeed);
                int bits = FxKitLook.Scaled(2, density);
                SpawnFoot(bits, point, n, along);
            }
            FadeScuffs(dt);
            Integrate(_foot, dt, 3.5f, _root.position.y);
        }

        void TickFlash(float dt, GameSettings settings)
        {
            int tags = It != null ? It.TagsLanded : _prevTags;
            if (tags > _prevTags && FxKitLook.Bursts(settings, FxKitOptions.TagFlash) && _cam != null)
                _flashAge = 0.0001f;
            if (_flashAge < 0f || _bars == null)
            {
                HideBars();
                return;
            }
            _flashAge += dt;
            float a = FxKitLook.FlashAlpha(_flashAge);
            if (_flashAge >= FxKitLook.FlashSeconds || a <= 0.01f)
            {
                if (_flashAge >= FxKitLook.FlashSeconds) _flashAge = -1f;
                HideBars();
                return;
            }
            float near = _cam.nearClipPlane + 0.12f;
            if (near < 0.2f) near = 0.2f;
            float halfH = near * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float aspect = _cam.aspect;
            if (aspect < 0.2f) aspect = 0.2f;
            float halfW = halfH * aspect;
            float thickH = halfH * 0.22f;
            float thickW = halfW * 0.16f;
            PlaceBar(0, new Vector3(-halfW + thickW * 0.5f, 0f, near), new Vector3(thickW, halfH * 2f, 1f), 0f, a);
            PlaceBar(1, new Vector3(halfW - thickW * 0.5f, 0f, near), new Vector3(thickW, halfH * 2f, 1f), 1f, a);
            PlaceBar(2, new Vector3(0f, -halfH + thickH * 0.5f, near), new Vector3(halfW * 2f, thickH, 1f), 2f, a);
            PlaceBar(3, new Vector3(0f, halfH - thickH * 0.5f, near), new Vector3(halfW * 2f, thickH, 1f), 3f, a);
        }

        void PlaceBar(int i, Vector3 local, Vector3 scale, float edge, float a)
        {
            if (_bars == null || i >= _bars.Length || _bars[i] == null) return;
            _barT[i].localPosition = local;
            _barT[i].localRotation = Quaternion.identity;
            _barT[i].localScale = scale;
            _bars[i].enabled = true;
            _block.SetColor("_Color", new Color(_cr, _cg, _cb, a));
            _block.SetColor("_BaseColor", new Color(_cr, _cg, _cb, a));
            _block.SetFloat("_Edge", edge);
            _block.SetFloat("_Guard", 1f);
            _block.SetVector("_Owner", _cam.transform.position);
            _bars[i].SetPropertyBlock(_block);
        }

        void SpawnDust(int n, Vector3 origin, float scale, float up)
        {
            Clear(_dust);
            FxKitLook.SurfaceColor(_landSurf, out float r, out float g, out float b);
            for (int i = 0; i < n && i < _dust.Length; i++)
            {
                float ang = i * 6.2831855f / n;
                float sp = scale * (0.9f + (i & 3) * 0.22f);
                _dust[i].Age = 0.0001f;
                _dust[i].Life = _landRoll ? FxKitLook.RollLife : FxKitLook.LandLife;
                float puff = 0.032f + 0.018f * scale;
                if (puff > 0.07f) puff = 0.07f;
                if (_landRoll) puff *= 1.2f;
                _dust[i].Size = puff;
                _dust[i].R = r;
                _dust[i].G = g;
                _dust[i].B = b;
                _dust[i].Shape = 0f;
                _dust[i].P = origin + Vector3.up * 0.04f;
                _dust[i].V = new Vector3(Mathf.Cos(ang) * sp, up + (i & 1) * 0.4f, Mathf.Sin(ang) * sp);
            }
        }

        void SpawnSparks(int n, Vector3 origin)
        {
            Clear(_spark);
            for (int i = 0; i < n && i < _spark.Length; i++)
            {
                float ang = i * 6.2831855f / n;
                _spark[i].Age = 0.0001f;
                _spark[i].Life = FxKitLook.SparkLife;
                _spark[i].Size = 0.018f + (i & 1) * 0.008f;
                _spark[i].R = 1f;
                _spark[i].G = 0.9f;
                _spark[i].B = 0.45f;
                _spark[i].Shape = 1f;
                _spark[i].P = origin;
                _spark[i].V = new Vector3(Mathf.Cos(ang) * 1.6f, 1.8f + (i & 2) * 0.4f, Mathf.Sin(ang) * 1.6f);
            }
        }

        void SpawnDebris(int n, Vector3 origin, int surf)
        {
            Clear(_debris);
            FxKitLook.SurfaceColor(surf, out float r, out float g, out float b);
            for (int i = 0; i < n && i < _debris.Length; i++)
            {
                float ang = i * 6.2831855f / n + 0.3f;
                _debris[i].Age = 0.0001f;
                _debris[i].Life = FxKitLook.DebrisLife;
                _debris[i].Size = 0.02f + (i % 3) * 0.013f;
                _debris[i].R = r;
                _debris[i].G = g;
                _debris[i].B = b;
                _debris[i].Shape = 0f;
                _debris[i].P = origin;
                _debris[i].V = new Vector3(Mathf.Cos(ang) * 1.3f, 1.4f, Mathf.Sin(ang) * 1.3f);
            }
        }

        void SpawnFoot(int n, Vector3 origin, Vector3 normal, Vector3 along)
        {
            if (_foot == null || n <= 0) return;
            FxKitLook.SurfaceColor(_wallSurf, out float r, out float g, out float b);
            for (int i = 0; i < n; i++)
            {
                int slot = _footCursor % _foot.Length;
                _footCursor++;
                _foot[slot].Age = 0.0001f;
                _foot[slot].Life = FxKitLook.FootLife;
                _foot[slot].Size = 0.03f + (slot & 1) * 0.012f;
                _foot[slot].R = r;
                _foot[slot].G = g;
                _foot[slot].B = b;
                _foot[slot].Shape = 0f;
                _foot[slot].P = origin;
                _foot[slot].V = -along * (0.55f + i * 0.3f) + normal * (0.35f + i * 0.2f) + Vector3.up * 0.3f;
            }
        }

        void Integrate(Mote[] motes, float dt, float gravity, float floorY)
        {
            if (motes == null) return;
            for (int i = 0; i < motes.Length; i++)
            {
                if (motes[i].Age < 0f || motes[i].Rend == null)
                {
                    if (motes[i].Rend != null) motes[i].Rend.enabled = false;
                    continue;
                }
                motes[i].Age += dt;
                if (motes[i].Age >= motes[i].Life)
                {
                    motes[i].Age = -1f;
                    motes[i].Rend.enabled = false;
                    continue;
                }
                Vector3 v = motes[i].V;
                v.y -= gravity * dt;
                motes[i].V = v;
                Vector3 p = motes[i].P + v * dt;
                if (p.y < floorY) p.y = floorY;
                motes[i].P = p;
                float a = FxKitLook.Fade(motes[i].Age, motes[i].Life);
                float size = motes[i].Size * (0.75f + 0.45f * a);
                Place(motes[i].Rend, p, size, motes[i].R, motes[i].G, motes[i].B, a * 0.85f, motes[i].Shape);
            }
        }

        void FadeScuffs(float dt)
        {
            if (_scuffLines == null) return;
            for (int i = 0; i < _scuffLines.Length; i++)
            {
                if (_scuffAge[i] < 0f)
                {
                    _scuffLines[i].enabled = false;
                    continue;
                }
                _scuffAge[i] += dt;
                if (_scuffAge[i] >= FxKitLook.ScuffLife)
                {
                    _scuffAge[i] = -1f;
                    _scuffLines[i].enabled = false;
                    continue;
                }
                float a = FxKitLook.Fade(_scuffAge[i], FxKitLook.ScuffLife);
                _scuffLines[i].enabled = true;
                _scuffLines[i].SetPosition(0, _scuffA[i]);
                _scuffLines[i].SetPosition(1, _scuffB[i]);
                Color c = new Color(0.45f, 0.38f, 0.30f, 0.55f * a);
                _scuffLines[i].startColor = c;
                _scuffLines[i].endColor = c;
                _scuffLines[i].startWidth = 0.018f * a;
                _scuffLines[i].endWidth = 0.006f * a;
            }
        }

        void Ring(LineRenderer line, Vector3 center, float radius, float width, float r, float g, float b, float a)
        {
            if (line == null) return;
            if (a <= 0.02f)
            {
                line.enabled = false;
                return;
            }
            line.enabled = true;
            line.startWidth = width;
            line.endWidth = width;
            Color c = new Color(r, g, b, a);
            line.startColor = c;
            line.endColor = c;
            int n = line.positionCount;
            Vector3 lift = center;
            lift.y += 0.03f;
            for (int i = 0; i < n; i++)
            {
                float ang = i / (float)(n - 1) * 6.2831855f;
                line.SetPosition(i, lift + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius));
            }
        }

        void Place(Renderer rend, Vector3 pos, float size, float r, float g, float b, float a, float shape)
        {
            if (rend == null) return;
            rend.enabled = a > 0.02f;
            if (!rend.enabled) return;
            Transform t = rend.transform;
            t.position = pos;
            t.localScale = new Vector3(size, size, 1f);
            Paint(rend, r, g, b, a, shape, 1f, -1f);
        }

        void Paint(Renderer rend, float r, float g, float b, float a, float shape, float billboard, float edge)
        {
            Color c = new Color(r, g, b, a);
            _block.SetColor("_Color", c);
            _block.SetColor("_BaseColor", c);
            _block.SetFloat("_Shape", shape);
            _block.SetFloat("_Billboard", billboard);
            _block.SetFloat("_Guard", 0f);
            _block.SetFloat("_Edge", edge);
            rend.SetPropertyBlock(_block);
        }

        void Clear(Mote[] motes)
        {
            if (motes == null) return;
            for (int i = 0; i < motes.Length; i++)
            {
                motes[i].Age = -1f;
                if (motes[i].Rend != null) motes[i].Rend.enabled = false;
            }
        }

        void HideStars()
        {
            if (_stars == null) return;
            for (int i = 0; i < _stars.Length; i++)
                if (_stars[i] != null) _stars[i].enabled = false;
        }

        void HideAir()
        {
            _airWas = false;
            _airGap = 0f;
            if (_air == null) return;
            for (int i = 0; i < _air.Length; i++)
            {
                _airAge[i] = -1f;
                if (_air[i] != null) _air[i].enabled = false;
            }
        }

        void HideStreaks()
        {
            if (_streaks == null) return;
            for (int i = 0; i < _streaks.Length; i++)
            {
                _streakAge[i] = -1f;
                if (_streaks[i] != null) _streaks[i].enabled = false;
            }
        }

        void HideBars()
        {
            if (_bars == null) return;
            for (int i = 0; i < _bars.Length; i++)
                if (_bars[i] != null) _bars[i].enabled = false;
        }

        /// <summary>
        /// Thin seat-tinted streaks trailing an air dash or a grapple pull.
        /// They leave the torso and the limbs, opposite the travel, and die in 0.15 s.
        /// GameSettings.SpeedLines defaults off so four split panes stay readable.
        /// </summary>
        void TickAir(float dt, GameSettings settings)
        {
            if (_air == null) return;
            bool allow = settings != null && settings.SpeedLines && FxKitLook.Master(settings);
            if (allow && settings.AnyReduceFlash()) allow = false;
            bool dash = allow && Motor != null && Motor.IsAirDashing;
            bool pull = allow
                && Grapple != null
                && Grapple.IsPulling
                && FxKitLook.Bursts(settings, FxKitOptions.Grapple);
            if (dash || pull)
            {
                _airGap += dt;
                if (!_airWas || _airGap >= 0.12f)
                {
                    _airGap = 0f;
                    SpawnAir(dash);
                }
            }
            else
                _airGap = 0f;
            _airWas = dash || pull;
            for (int i = 0; i < _air.Length; i++)
            {
                if (_airAge[i] < 0f)
                {
                    if (_air[i] != null) _air[i].enabled = false;
                    continue;
                }
                _airAge[i] += dt;
                if (_airAge[i] > AirLife || _air[i] == null)
                {
                    _airAge[i] = -1f;
                    if (_air[i] != null) _air[i].enabled = false;
                    continue;
                }
                float u = 1f - _airAge[i] / AirLife;
                _air[i].enabled = true;
                _air[i].SetPosition(0, _airA[i]);
                _air[i].SetPosition(1, _airB[i]);
                bool edge = i >= AirLong || _airWide[i] != 0;
                _air[i].startWidth = (edge ? 0.11f : 0.045f) * u;
                _air[i].endWidth = (edge ? 0.02f : 0.008f) * u;
                float a = 0.35f * u;
                var c = new Color(_cr, _cg, _cb, a);
                _air[i].startColor = c;
                c.a = a * 0.2f;
                _air[i].endColor = c;
            }
        }

        void SpawnAir(bool dash)
        {
            Vector3 origin = _root.position;
            Vector3 travel;
            if (dash)
            {
                travel = Motor.AirDashDirection;
                travel.y = 0f;
            }
            else
            {
                Vector3 anchor;
                float length;
                float slack;
                if (Grapple.TryGetRope(out anchor, out length, out slack))
                    travel = anchor - origin;
                else if (_root.forward.sqrMagnitude > 0.0001f)
                    travel = _root.forward;
                else
                    travel = Vector3.forward;
            }
            if (travel.sqrMagnitude < 0.0001f) travel = Vector3.forward;
            travel.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, travel);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            else side.Normalize();
            Vector3 camRight = side;
            Vector3 camUp = Vector3.up;
            if (_cam != null)
            {
                camRight = _cam.transform.right;
                if (camRight.sqrMagnitude < 0.0001f) camRight = side;
                else camRight.Normalize();
                camUp = _cam.transform.up;
                if (camUp.sqrMagnitude < 0.0001f) camUp = Vector3.up;
                else camUp.Normalize();
            }
            // Dash and grapple both flare across the camera. A trail ribbon
            // collapses on a diagonal the same way a head-on dash does.
            for (int i = 0; i < _air.Length; i++)
            {
                _airWide[i] = 0;
                if (i < AirLong)
                {
                    float h = (i * 3 % 10) / 9f;
                    Vector3 start = LimbPoint(i, origin);
                    float sign = (i & 1) == 0 ? 1f : -1f;
                    float flare = 0.95f + h * 0.45f;
                    float lift = (i & 1) == 0 ? 0.16f : -0.10f;
                    _airA[i] = start + camRight * sign * 0.22f;
                    _airB[i] = _airA[i] + camRight * sign * flare + camUp * lift;
                    _airWide[i] = 1;
                }
                else
                {
                    // Camera-facing burst just outside the silhouette. Long enough for a quarter pane.
                    int e = i - AirLong;
                    float sign = (e & 1) == 0 ? 1f : -1f;
                    float outward = e < 2 ? 0.34f : e < 4 ? 0.26f : 0.16f;
                    float flare = 1.05f + (e % 3) * 0.18f;
                    float lift = e < 2 ? 0.22f : e < 4 ? -0.06f : 0.10f;
                    if ((e & 1) != 0) lift = -lift;
                    Vector3 start = EdgePoint(e, origin) + camRight * sign * outward;
                    _airA[i] = start;
                    _airB[i] = start + camRight * sign * flare + camUp * lift;
                    _airWide[i] = 1;
                }
                _airAge[i] = 0.0001f;
            }
        }

        Vector3 EdgePoint(int e, Vector3 origin)
        {
            Transform bone = null;
            float y = 1.1f;
            switch (e)
            {
                case 0: bone = _armL; y = 1.35f; break;
                case 1: bone = _armR; y = 1.35f; break;
                case 2: bone = _hips; y = 0.95f; break;
                case 3: bone = _hips; y = 0.95f; break;
                case 4: bone = _handL; y = 1.05f; break;
                default: bone = _handR; y = 1.05f; break;
            }
            if (bone != null) return bone.position;
            return origin + Vector3.up * y;
        }

        string WallMaterialName()
        {
            if (Motor == null || Motor.WallCollider == null) return null;
            Collider col = Motor.WallCollider;
            MeshRenderer rend = col.GetComponent<MeshRenderer>();
            if (rend != null && rend.sharedMaterial != null) return rend.sharedMaterial.name;
            return col.name;
        }

        Vector3 LimbPoint(int i, Vector3 origin)
        {
            Transform bone = null;
            float y = 1.05f;
            switch (i)
            {
                case 0: bone = _spine; y = 1.15f; break;
                case 1: bone = _hips; y = 0.92f; break;
                case 2: bone = _handL; y = 1.05f; break;
                case 3: bone = _handR; y = 1.02f; break;
                case 4: bone = _footL; y = 0.22f; break;
                case 5: bone = _footR; y = 0.28f; break;
                case 6: bone = _head; y = 1.45f; break;
                default: y = 1.25f; break;
            }
            if (bone != null) return bone.position;
            return origin + Vector3.up * y;
        }

        void HideActive()
        {
            _landAge = -1f;
            _sparkAge = -1f;
            _launchAge = -1f;
            _starAge = -1f;
            _flashAge = -1f;
            _ropeOn = false;
            Clear(_dust);
            Clear(_spark);
            Clear(_debris);
            Clear(_foot);
            if (_shell != null)
            {
                for (int i = 0; i < _shell.Length; i++)
                    if (_shell[i] != null) _shell[i].enabled = false;
            }
            HideStars();
            HideStreaks();
            HideAir();
            HideBars();
            if (_shock != null) _shock.enabled = false;
            if (_dustRing != null) _dustRing.enabled = false;
            if (_rope != null) _rope.enabled = false;
            if (_launchRing != null) _launchRing.enabled = false;
            if (_disc != null) _disc.enabled = false;
            if (_scuffLines != null)
            {
                for (int i = 0; i < _scuffLines.Length; i++)
                {
                    _scuffAge[i] = -1f;
                    _scuffLines[i].enabled = false;
                }
            }
        }

        void Remember()
        {
            _prevGround = Motor.Ground.grounded;
            _prevPull = Grapple != null && Grapple.IsPulling;
            _prevLaunch = Motor.LaunchArc;
            _prevStagger = Motor.IsPunchStaggered;
            if (It != null) _prevTags = It.TagsLanded;
        }

        void RefreshSeat()
        {
            string n = It != null ? It.PlayerId : null;
            if (string.IsNullOrEmpty(n) && _root != null) n = _root.name;
            if (string.IsNullOrEmpty(n)) return;
            char c = n[n.Length - 1];
            if (c < '1' || c > '4') return;
            _seat = c - '1';
            VerbFxLook.PlayerColor(_seat, out _cr, out _cg, out _cb);
        }

        void CacheSeat(GameObject go)
        {
            _seat = 0;
            string n = go.name;
            if (!string.IsNullOrEmpty(n))
            {
                char c = n[n.Length - 1];
                if (c >= '1' && c <= '4') _seat = c - '1';
            }
            VerbFxLook.PlayerColor(_seat, out _cr, out _cg, out _cb);
        }

        Mote[] MakeMotes(int count, string name)
        {
            var motes = new Mote[count];
            for (int i = 0; i < count; i++)
            {
                Renderer rend;
                MakeQuad(_root, name, _sprite, out rend);
                rend.enabled = false;
                motes[i].Rend = rend;
                motes[i].Age = -1f;
            }
            return motes;
        }

        Renderer[] MakeCards(int count, string name)
        {
            var rend = new Renderer[count];
            for (int i = 0; i < count; i++)
            {
                MakeQuad(_root, name, _sprite, out rend[i]);
                rend[i].enabled = false;
            }
            return rend;
        }

        void BuildShells(Transform root)
        {
            EnsureShared();
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            int n = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (BodyMesh(renderers[i])) n++;
            }
            _shell = new Renderer[n];
            int w = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!BodyMesh(renderers[i])) continue;
                _shell[w++] = MakeShell(renderers[i]);
            }
        }

        static bool BodyMesh(MeshRenderer src)
        {
            if (src == null) return false;
            string name = src.gameObject.name;
            if (string.IsNullOrEmpty(name) || name.Length < 5) return false;
            if (name[0] != 'M' || name[1] != 'e' || name[2] != 's' || name[3] != 'h' || name[4] != '_') return false;
            MeshFilter filter = src.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null;
        }

        static Renderer MakeShell(MeshRenderer src)
        {
            MeshFilter filter = src.GetComponent<MeshFilter>();
            var go = new GameObject("FxRimShell");
            go.transform.SetParent(src.transform, false);
            var shellFilter = go.AddComponent<MeshFilter>();
            shellFilter.sharedMesh = filter.sharedMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _rimMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        LineRenderer MakeLine(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = name == "FxRopeShimmer" ? 8 : FxKitLook.RingSeg;
            if (name == "FxScuff" || name == "FxStreak" || name == "FxAir") line.positionCount = 2;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = _line;
            line.enabled = false;
            return line;
        }

        static Transform MakeQuad(Transform parent, string name, Material mat, out Renderer rend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = _quad;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            rend = renderer;
            return go.transform;
        }

        static void EnsureShared()
        {
            if (_quad == null)
            {
                _quad = new Mesh();
                _quad.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f)
                };
                _quad.uv = new[]
                {
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
                };
                _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            }
            if (_sprite == null) _sprite = MakeMat("Tag/FxKitSprite");
            if (_edge == null) _edge = MakeMat("Tag/FxKitEdge");
            if (_rimMat == null) _rimMat = MakeMat("Tag/FxKitRim");
            if (_line == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                _line = new Material(shader);
                _line.color = Color.white;
            }
        }

        static Material MakeMat(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return new Material(shader);
        }

        static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            int n = root.childCount;
            for (int i = 0; i < n; i++)
            {
                Transform hit = Find(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
