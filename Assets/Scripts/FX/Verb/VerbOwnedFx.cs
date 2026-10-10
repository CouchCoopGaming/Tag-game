using Tag.Art;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Landing ring and debris, roll swirl, dash ghosts, hook spark,
    /// release snap, comic dizzy stars, the tag-back rim, and wet drips.
    /// Speed lines, chest-and-hand scrape sparks, the tag hit burst, the It
    /// handoff flash, and the pad and zip polylines live on Pass5Host and
    /// Pass5Burst. Pad wind, zip sparks, wisps, and foot scuffs stay on
    /// VerbFxHost. Visual only.
    /// </summary>
    [DefaultExecutionOrder(130)]
    public sealed class VerbOwnedFx : MonoBehaviour
    {
        const int Ghosts = 4;
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
        float _prevSurf;
        int _wallId;
        int _wallSurf = (int)DustLook.Surface.Concrete;
        int _groundId;
        int _groundSurf = (int)DustLook.Surface.Concrete;
        int _hookId;

        float _ringAge = -1f;
        float _ringR;
        float _swirlAge = -1f;
        float _swirlGap;
        Vector3 _swirlDir;
        Vector3 _swirlPos;
        float _snapAge = -1f;
        Vector3 _snapPos;
        float _ghostGap;
        int _ghostCursor;
        readonly float[] _ghostAge = new float[Ghosts];
        DashSilhouette _sil;
        float _colorR = 0.95f;
        float _colorG = 0.28f;
        float _colorB = 0.32f;

        LineRenderer _ring;
        LineRenderer _snap;
        LineRenderer[] _rims;
        LineRenderer[] _rimInk;
        Transform _dizzy;
        Transform[] _stars;
        Material[] _starMat;
        RaycastHit _hookHit;
        VerbFxCards _cards;
        float _rimVis;
        float _starAge = -1f;
        bool _prevStagger;
        int _ringSurf;
        bool _ringCracked;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            VerbFxCards.Ensure(host);
            DashSilhouette.Ensure(host);
            if (host.GetComponent<VerbOwnedFx>() == null)
                host.AddComponent<VerbOwnedFx>();
        }

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _loco = GetComponent<DummyLocomotor>();
            _fx = FxBurstPool.Ensure(transform);
            _grapple = GetComponent<ExperimentalGrapple>();
            _it = GetComponent<ItController>();
            _role = GetComponent<TagRole>();
            CacheSeat();
            VerbFxLook.PlayerColor(_seat, out _colorR, out _colorG, out _colorB);
            _ring = MakeLine("LandRing", new Color(0.85f, 0.72f, 0.48f, 0.85f));
            _snap = MakeLine("RopeSnap", new Color(1f, 0.9f, 0.4f, 0.9f));
            _rims = new LineRenderer[2];
            _rimInk = new LineRenderer[2];
            _rims[0] = MakeLine("TagRimLow", new Color(_colorR, _colorG, _colorB, 0.7f));
            _rims[1] = MakeLine("TagRimHigh", new Color(_colorR, _colorG, _colorB, 0.7f));
            _rimInk[0] = MakeLine("TagRimLowInk", new Color(VerbFxLook.InkR, VerbFxLook.InkG, VerbFxLook.InkB, VerbFxLook.InkA));
            _rimInk[1] = MakeLine("TagRimHighInk", new Color(VerbFxLook.InkR, VerbFxLook.InkG, VerbFxLook.InkB, VerbFxLook.InkA));
            _rimInk[0].positionCount = RingSeg;
            _rimInk[1].positionCount = RingSeg;
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Mesh quad = Quad();
            Texture2D starTex = LoadStar();
            for (int i = 0; i < Ghosts; i++)
                _ghostAge[i] = -1f;
            _sil = GetComponent<DashSilhouette>();
            _dizzy = new GameObject("Dizzy").transform;
            _dizzy.SetParent(transform, false);
            _stars = new Transform[Stars];
            _starMat = new Material[Stars];
            for (int i = 0; i < Stars; i++)
            {
                _stars[i] = MakeQuad(_dizzy, "Star", quad, shader, starTex, out _starMat[i]);
                _starMat[i].color = Color.white;
                _starMat[i].SetFloat("_Tilt", (i - 1) * 0.12f);
            }
            _cards = GetComponent<VerbFxCards>();
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
            TickLand(grounded, speed, dt);
            TickDash(dt);
            TickGrapple();
            TickStagger(dt);
            TickRim(dt);
            TickDrip();
            FadeRing(dt);
            FadeSnap(dt);
            FadeGhosts(dt);
            if (_cards != null) _cards.Tick(dt);
            Remember();
        }

        void TickLand(bool grounded, float speed, float dt)
        {
            if (grounded && !_prevGround)
            {
                float impact = _motor.LastLandImpactSpeed;
                if (impact >= LandingRollPose.SoftFloor)
                {
                    float tier = LandingRollPose.TierScale(impact);
                    _ringAge = 0f;
                    _ringR = VerbFxLook.LandRing(impact) * (0.55f + 0.45f * tier);
                    _ringSurf = _groundSurf;
                    _ringCracked = tier >= 0.99f;
                    Vector3 pos = _motor.transform.position;
                    pos.y += 0.05f;
                    DustLook.Puff puff = DustLook.At(_groundSurf, speed, (int)DustLook.Kick.None);
                    puff.Count = VerbFxLook.Debris(impact, 1f);
                    puff.Size *= 0.75f + 0.5f * tier;
                    if (_cards != null)
                        _cards.Burst(pos, puff.Count, puff.Size * 2.4f, puff.R, puff.G, puff.B, 0.28f + tier * 0.35f);
                    DustLook.Puff ring = DustLook.LandDust(_groundSurf, impact);
                    EmitRing(pos, _ringR, ring);
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
                _swirlAge += dt;
                _swirlGap -= dt;
                if (_swirlAge >= LandingRollPose.Seconds)
                    _swirlAge = -1f;
                else if (_swirlGap <= 0f)
                {
                    _swirlGap = 0.06f;
                    Vector3 p = _swirlPos + _swirlDir * (_swirlAge * 2.2f);
                    DustLook.Puff swirl = DustLook.At(_groundSurf, DustLook.Sprint, (int)DustLook.Kick.Trail);
                    swirl.Count = 2;
                    _fx.PlayShaped(FxBurstKind.Roll, p, swirl);
                    if (_cards != null)
                        _cards.SpawnSwirl(p, swirl.R, swirl.G, swirl.B);
                }
            }
        }

        void EmitRing(Vector3 center, float radius, DustLook.Puff ring)
        {
            if (_fx == null || radius < 0.05f) return;
            int n = ring.Count;
            if (n < 6) n = 6;
            if (n > 16) n = 16;
            DustLook.Puff one = ring;
            one.Count = 1;
            one.Back = 0f;
            float step = 6.2831855f / n;
            float outSp = 0.35f + one.Lift;
            float upSp = 0.20f + one.Lift;
            for (int i = 0; i < n; i++)
            {
                float ang = i * step;
                Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 p = center + radial * radius + Vector3.up * 0.04f;
                _fx.PlayRadial(p, radial * outSp + Vector3.up * upSp, one);
            }
        }

        void TickDash(float dt)
        {
            bool dash = _motor.IsAirDashing;
            int cap = VerbFxLook.Ghosts(FxAmount.Density(GameSettings.Current));
            if (dash && !_prevDash)
            {
                _ghostGap = 0f;
                _ghostCursor = 0;
                if (cap > 0)
                    DropGhost(_motor.transform.position + Vector3.up * 0.9f);
            }
            else if (dash)
            {
                _ghostGap += dt;
                float step = VerbFxLook.DashSeconds / VerbFxLook.GhostsFull;
                if (_ghostGap >= step && _ghostCursor < cap)
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
                        surf = DustContact.Read(_hookHit.collider, ref _hookId, ref surf);
                    DustLook.Puff puff = DustLook.At(surf, DustLook.Sprint, (int)DustLook.Kick.Pivot);
                    puff.Count = VerbFxLook.HookBits(surf, true);
                    if (surf == (int)DustLook.Surface.Metal) puff.Spark = 1;
                    _fx.PlayShaped(FxBurstKind.VaultPuff, anchor, puff);
                    if (_cards != null)
                        _cards.SpawnHook(anchor, surf, surf == (int)DustLook.Surface.Concrete || surf == (int)DustLook.Surface.Metal);
                }
            }
            if (!on && _prevGrapple)
            {
                _snapAge = 0f;
                _snapPos = _motor.transform.position + Vector3.up * 1.1f;
                DustLook.Puff puff = DustLook.At(_groundSurf, DustLook.Walk, (int)DustLook.Kick.None);
                puff.Count = 4;
                puff.Life = VerbFxLook.ReleaseSeconds;
                _fx.PlayShaped(FxBurstKind.VaultPuff, _snapPos, puff);
            }
        }

        void TickStagger(float dt)
        {
            bool on = _motor.IsPunchStaggered;
            if (on && !_prevStagger) _starAge = 0.0001f;
            if (!on)
            {
                _starAge = -1f;
                _dizzy.gameObject.SetActive(false);
                return;
            }
            _starAge += dt;
            float life = PunchStaggerPose.Duration;
            float a = VerbFxEase.Alpha(_starAge, life);
            float s = VerbFxEase.Scale(_starAge, life, 0.2f, 1f);
            _dizzy.gameObject.SetActive(a > 0.01f);
            if (a <= 0.01f) return;
            _dizzy.position = _motor.transform.position + Vector3.up * 1.85f;
            _dizzy.Rotate(0f, 140f * dt, 0f, Space.World);
            float spin = Time.time * 3f;
            for (int i = 0; i < Stars; i++)
            {
                float ang = spin + i * 2.094f;
                _stars[i].localPosition = new Vector3(Mathf.Cos(ang) * 0.32f, Mathf.Sin(ang) * 0.12f, 0f);
                _stars[i].localScale = new Vector3(0.42f * s, 0.42f * s, 1f);
                Color c = _starMat[i].color;
                c.r = 1f;
                c.g = 1f;
                c.b = 1f;
                c.a = a;
                _starMat[i].color = c;
            }
        }

        void TickRim(float dt)
        {
            float left = 0f;
            if (_it != null && _it.TagBackRemaining > left) left = _it.TagBackRemaining;
            if (_role != null && _role.TagBackRemaining > left) left = _role.TagBackRemaining;
            float pulse = VerbFxLook.Rim(left, TagBackImmunity.DefaultSeconds, Time.time);
            float follow = dt * 8f;
            if (follow > 1f) follow = 1f;
            _rimVis += (pulse - _rimVis) * follow;
            bool on = _rimVis > 0.02f;
            bool inkOn = GameSettings.Current != null && GameSettings.Current.SeatInk;
            _rims[0].enabled = on;
            _rims[1].enabled = on;
            if (_rimInk != null)
            {
                _rimInk[0].enabled = on && inkOn;
                _rimInk[1].enabled = on && inkOn;
            }
            if (!on) return;
            float radius = 0.46f + 0.06f * _rimVis;
            Color c = new Color(_colorR, _colorG, _colorB, 0.25f + 0.55f * _rimVis);
            Vector3 low = _motor.transform.position + Vector3.up * (0.7f + 0.04f * _rimVis);
            Vector3 high = _motor.transform.position + Vector3.up * (1.25f + 0.04f * _rimVis);
            PlaceRing(_rims[0], low, radius, c);
            PlaceRing(_rims[1], high, radius * 0.92f, c);
            if (inkOn)
            {
                PlaceInk(_rimInk[0], low, radius, c.a);
                PlaceInk(_rimInk[1], high, radius * 0.92f, c.a);
            }
        }

        void TickDrip()
        {
            if (_loco == null) return;
            if (_motor.State == MoveState.WallRun)
            {
                float surf = _loco.SurfPhase;
                bool left;
                if (DustLook.FootDown(_prevSurf, surf, out left))
                {
                    _wallSurf = DustContact.Read(_motor.WallCollider, ref _wallId, ref _wallSurf);
                    if (VerbFxLook.Drip(_wallSurf))
                    {
                        Vector3 point = _motor.WallPoint;
                        if (point.sqrMagnitude < 0.0001f)
                            point = _motor.transform.position + Vector3.up * 0.5f;
                        DustLook.Puff puff = DustLook.At(_wallSurf, _motor.HorizSpeed, (int)DustLook.Kick.None);
                        puff.Count = 3;
                        _fx.PlayShaped(FxBurstKind.WallScuff, point, puff);
                        if (_cards != null)
                        {
                            _cards.SpawnDrip(point);
                            _cards.SpawnDrip(point + new Vector3(0.04f, 0.08f, 0f));
                        }
                    }
                }
                _prevSurf = surf;
            }
            else
                _prevSurf = _loco.SurfPhase;
        }

        void DropGhost(Vector3 pos)
        {
            int slot = _ghostCursor % Ghosts;
            _ghostCursor++;
            _ghostAge[slot] = 0.0001f;
            if (_sil != null)
                _sil.Capture(slot);
        }

        void FadeGhosts(float dt)
        {
            for (int i = 0; i < Ghosts; i++)
            {
                if (_ghostAge[i] < 0f)
                {
                    if (_sil != null) _sil.Hide(i);
                    continue;
                }
                _ghostAge[i] += dt;
                if (_ghostAge[i] >= VerbFxLook.GhostLife)
                {
                    _ghostAge[i] = -1f;
                    if (_sil != null) _sil.Hide(i);
                    continue;
                }
                float a = VerbFxEase.Trail(_ghostAge[i], VerbFxLook.GhostLife);
                if (_sil != null) _sil.Show(i, a);
            }
        }

        void FadeRing(float dt)
        {
            if (_ringAge < 0f)
            {
                _ring.enabled = false;
                if (_cards != null) _cards.HideRing();
                return;
            }
            _ringAge += dt;
            if (_ringAge >= 0.36f)
            {
                _ringAge = -1f;
                _ring.enabled = false;
                if (_cards != null) _cards.HideRing();
                return;
            }
            float a = VerbFxEase.Alpha(_ringAge, 0.36f);
            float rad = _ringR * VerbFxEase.Scale(_ringAge, 0.36f, 0.7f, 1.15f);
            if (_cards != null)
            {
                _ring.enabled = false;
                Vector3 pos = _motor.transform.position;
                pos.y += 0.04f;
                _cards.PaintRing(pos, rad, _ringAge, 0.36f, _ringSurf, _ringCracked);
                return;
            }
            Color c = new Color(0.85f, 0.72f, 0.48f, 0.8f * a);
            PlaceRing(_ring, _motor.transform.position + Vector3.up * 0.06f, rad, c);
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
            float a = VerbFxEase.Alpha(_snapAge, VerbFxLook.ReleaseSeconds);
            float lift = VerbFxEase.Scale(_snapAge, VerbFxLook.ReleaseSeconds, 0.12f, 0.62f);
            _snap.enabled = a > 0.01f;
            if (_snap.positionCount != 2) _snap.positionCount = 2;
            _snap.SetPosition(0, _snapPos);
            _snap.SetPosition(1, _snapPos + Vector3.up * lift);
            _snap.startWidth = 0.08f * a;
            _snap.endWidth = 0.012f * a;
            Color c = new Color(1f, 0.9f, 0.4f, a);
            _snap.startColor = c;
            _snap.endColor = c;
        }

        void PlaceInk(LineRenderer line, Vector3 center, float radius, float alpha)
        {
            if (line == null) return;
            Color dark = new Color(VerbFxLook.InkR, VerbFxLook.InkG, VerbFxLook.InkB, VerbFxLook.InkA * alpha);
            PlaceRing(line, center, radius + 0.0225f + VerbFxLook.InkWorld * 0.5f, dark);
            line.startWidth = VerbFxLook.InkWorld;
            line.endWidth = VerbFxLook.InkWorld;
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
            _starAge = -1f;
            _rimVis = 0f;
            if (_cards != null) _cards.HideAll();
            if (_sil != null) _sil.HideAll();
            if (_ring != null) _ring.enabled = false;
            if (_snap != null) _snap.enabled = false;
            if (_dizzy != null) _dizzy.gameObject.SetActive(false);
            if (_rims != null)
            {
                _rims[0].enabled = false;
                _rims[1].enabled = false;
            }
            if (_rimInk != null)
            {
                _rimInk[0].enabled = false;
                _rimInk[1].enabled = false;
            }
            for (int i = 0; i < Ghosts; i++)
                _ghostAge[i] = -1f;
        }

        void Remember()
        {
            _prevGround = _motor.Ground.grounded;
            _prevDash = _motor.IsAirDashing;
            _prevGrapple = _grapple != null && _grapple.IsPulling;
            _prevStagger = _motor.IsPunchStaggered;
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

        static Transform MakeQuad(Transform parent, string name, Mesh mesh, Shader shader, Texture2D tex, out Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            mat = new Material(shader);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
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

        static Texture2D LoadStar()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(ComicDizzy.Png());
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
