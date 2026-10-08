using Tag.Art;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// One camera and render texture per seat. One shared light rig
    /// (key, fill, rim) lights every seat. The body uses the look's
    /// matte primary and secondary colours. Idle motion is visual only
    /// and uses unscaled time.
    /// </summary>
    public sealed class MenuPreview : MonoBehaviour
    {
        const int Slots = 4;
        const float BandH = 0.15f;
        const float NumSize = 0.22f;

        readonly Transform[] _anchor = new Transform[Slots];
        readonly Transform[] _figure = new Transform[Slots];
        readonly Transform[] _disc = new Transform[Slots];
        readonly bool[] _ready = new bool[Slots];
        readonly float[] _hop = new float[Slots];
        readonly Camera[] _cam = new Camera[Slots];
        readonly RenderTexture[] _rt = new RenderTexture[Slots];
        readonly int[] _hier = { -1, -1, -1, -1 };
        readonly int[] _accent = { -1, -1, -1, -1 };
        readonly int[] _hat = { -1, -1, -1, -1 };
        Transform _podiumRoot;
        Camera _podiumCam;
        RenderTexture _podiumRt;
        Transform _paradeRoot;
        Camera _paradeCam;
        RenderTexture _paradeRt;
        readonly Transform[] _step = new Transform[Slots];
        readonly Transform[] _trim = new Transform[Slots];
        readonly Transform[] _blot = new Transform[Slots];
        readonly Transform[] _band = new Transform[Slots];
        readonly Renderer[] _bandRend = new Renderer[Slots];
        readonly TextMesh[] _rankNum = new TextMesh[Slots];
        readonly Transform[] _podiumAnchor = new Transform[Slots];
        readonly MenuCheer[] _planter = new MenuCheer[Slots];
        readonly float[] _stepH = new float[Slots];
        readonly Transform[] _confetti = new Transform[18];
        float _rise0;
        bool _rising;
        static Sprite _wellSprite;
        static Texture2D _wellTex;
        static Texture2D _shadowTex;
        Material _wellMat;
        Material _shadowMat;

        public void Build()
        {
            for (int i = 0; i < Slots; i++)
            {
                var anchor = new GameObject("PreviewAnchor" + i.ToString());
                anchor.transform.SetParent(transform, false);
                anchor.transform.position = new Vector3(i * 8f, -40f, 30f);
                _anchor[i] = anchor.transform;

                var camGo = new GameObject("PreviewCam" + i.ToString());
                camGo.transform.SetParent(transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.58f, 0.66f, 0.74f, 1f);
                cam.fieldOfView = 26f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 20f;
                cam.depth = -20;
                cam.enabled = false;
                _rt[i] = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                _rt[i].Create();
                cam.targetTexture = _rt[i];
                _cam[i] = cam;
                Aim(i);
            }
            // Shared rig. Rays point at the figure. The key comes from the
            // camera's front-left, the fill from the other front side, and
            // the rim from behind so the silhouette leaves the well.
            // Key from the front three-quarter, fill kept soft, rim from behind
            // so the face plate and the eye sockets are not a flat ball.
            AddSun("PreviewKey", new Vector3(0.39f, -0.48f, -0.79f), new Color(1f, 0.96f, 0.90f, 1f), 1.55f, true);
            AddSun("PreviewFill", new Vector3(-0.42f, -0.18f, -0.55f), new Color(0.75f, 0.82f, 1f, 1f), 0.28f);
            AddSun("PreviewRim", new Vector3(-0.55f, -0.25f, 0.80f), new Color(0.82f, 0.90f, 1f, 1f), 0.70f);
            for (int i = 0; i < Slots; i++)
            {
                BuildWell(i);
                BuildShadow(i);
            }
            BuildPodium();
            BuildParade();
            gameObject.SetActive(false);
        }

        public RenderTexture Texture(int seat)
        {
            if (seat < 0 || seat >= Slots) return null;
            return _rt[seat];
        }

        public void Show()
        {
            gameObject.SetActive(true);
            for (int i = 0; i < Slots; i++)
            {
                if (_cam[i] != null) _cam[i].enabled = true;
            }
            if (_podiumCam != null) _podiumCam.enabled = false;
            if (_paradeCam != null) _paradeCam.enabled = false;
        }

        public void Hide()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_cam[i] != null) _cam[i].enabled = false;
            }
            if (_podiumCam != null) _podiumCam.enabled = false;
            if (_paradeCam != null) _paradeCam.enabled = false;
            gameObject.SetActive(false);
        }

        public void ShowParade(RawImage view)
        {
            if (_paradeRoot == null) return;
            gameObject.SetActive(true);
            for (int i = 0; i < Slots; i++)
            {
                if (_cam[i] != null) _cam[i].enabled = false;
            }
            if (_podiumCam != null) _podiumCam.enabled = false;
            if (_paradeCam != null) _paradeCam.enabled = true;
            if (view != null)
            {
                view.texture = _paradeRt;
                view.color = Color.white;
            }
        }

        public void Apply(int seat, int hier, int accent, int hat, RawImage view)
        {
            if (seat < 0 || seat >= Slots) return;
            if (view != null)
            {
                view.texture = _rt[seat];
                view.color = Color.white;
            }
            bool same = _hier[seat] == hier && _accent[seat] == accent && _hat[seat] == hat && _figure[seat] != null;
            if (!same)
            {
                _hier[seat] = hier;
                _accent[seat] = accent;
                _hat[seat] = hat;
                for (int c = _anchor[seat].childCount - 1; c >= 0; c--)
                {
                    Transform child = _anchor[seat].GetChild(c);
                    if (child != null && (child.name == "Pedestal" || child.name == "Contact" || child.name == "SoftShadow")) continue;
                    DestroyImmediate(child.gameObject);
                }
                GameObject body = MenuMannequin.Spawn(_anchor[seat], MenuMannequin.NameOf(hier), MenuMannequin.NameOf(accent), hat != 0);
                _figure[seat] = body != null ? body.transform : null;
                if (_figure[seat] != null)
                    _figure[seat].localPosition = new Vector3(0f, 0.12f, 0f);
                Aim(seat);
            }
            EnsureDisc(seat);
            if (_figure[seat] != null)
            {
                MenuIdle idle = _figure[seat].GetComponent<MenuIdle>();
                if (idle != null) idle.SetReady(_ready[seat]);
            }
        }

        public void SetReady(int seat, bool ready)
        {
            if (seat < 0 || seat >= Slots) return;
            bool edge = _ready[seat] != ready;
            _ready[seat] = ready;
            if (edge && ready && !MenuVideo.ReduceMotion) _hop[seat] = 1f;
            if (!ready) _hop[seat] = 0f;
            if (_figure[seat] == null) return;
            MenuIdle idle = _figure[seat].GetComponent<MenuIdle>();
            if (idle != null) idle.SetReady(ready);
        }

        public void ShowPodium(int count, MenuPodium.Row[] rows, RawImage view)
        {
            if (_podiumRoot == null) return;
            gameObject.SetActive(true);
            for (int i = 0; i < Slots; i++)
            {
                if (_cam[i] != null) _cam[i].enabled = false;
            }
            if (_podiumCam != null) _podiumCam.enabled = true;
            if (_paradeCam != null) _paradeCam.enabled = false;
            if (view != null)
            {
                view.texture = _podiumRt;
                view.color = Color.white;
                // The results camera's look-at faces the other way from the still.
                // Flip the picture so 2nd stays on the left, under the same cards.
                view.uvRect = new Rect(1f, 0f, -1f, 1f);
            }
            if (count < 0) count = 0;
            if (count > Slots) count = Slots;
            bool crowned = false;
            if (rows != null)
            {
                int look = count < rows.Length ? count : rows.Length;
                for (int r = 0; r < look; r++)
                {
                    if (rows[r].Winner) crowned = true;
                }
            }
            for (int i = 0; i < Slots; i++)
            {
                if (_podiumAnchor[i] == null) continue;
                for (int c = _podiumAnchor[i].childCount - 1; c >= 0; c--)
                {
                    Transform child = _podiumAnchor[i].GetChild(c);
                    if (child != null && child.name == "Shade" + i.ToString()) continue;
                    DestroyImmediate(child.gameObject);
                }
                bool on = i < count && rows != null;
                _step[i].gameObject.SetActive(on);
                if (_band[i] != null) _band[i].gameObject.SetActive(on);
                if (_rankNum[i] != null) _rankNum[i].gameObject.SetActive(on);
                if (_blot[i] != null) _blot[i].gameObject.SetActive(on);
                if (!on)
                {
                    _planter[i] = null;
                    continue;
                }
                MenuPodium.Row row = rows[i];
                int seat = row.Seat;
                if (seat < 0) seat = i;
                if (_bandRend[i] != null)
                    _bandRend[i].sharedMaterial = Flat(MenuTheme.SeatBand(seat));
                if (_rankNum[i] != null)
                    _rankNum[i].color = MenuTheme.SeatInk(seat);
                GameObject body = MenuMannequin.Spawn(_podiumAnchor[i], MenuMannequin.NameOf(row.Hier), MenuMannequin.NameOf(row.Accent), row.Hat != 0);
                bool win = row.Winner;
                bool clap = !win && i < 3;
                MenuCheer.Play(body, win, clap, i == 2);
                if (body != null) body.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                MenuCheer planted = body != null ? body.GetComponent<MenuCheer>() : null;
                _planter[i] = planted;
                if (planted != null && _step[i] != null)
                {
                    // Cube top, plus the 5 cm cap that sits on it.
                    float top = _step[i].position.y + _step[i].lossyScale.y * 0.5f + 0.05f;
                    planted.Floor(top);
                }
            }
            _rise0 = Time.unscaledTime;
            _rising = !MenuVideo.ReduceMotion && !MenuCapture.Running;
            SetRise(_rising ? 0f : 1f);
            bool party = crowned && !MenuVideo.ReduceMotion;
            for (int i = 0; i < _confetti.Length; i++)
            {
                if (_confetti[i] != null) _confetti[i].gameObject.SetActive(party);
            }
        }

        /// <summary>
        /// Horizontal place of a block in the results picture, 0 at the left.
        /// Matches the still: 2nd, 1st, 3rd, 4th.
        /// </summary>
        public static float BlockFraction(int rank)
        {
            // Picture x of each block centre through the results camera.
            // Equally spaced, and centred. Left to right: 2nd, 1st, 3rd, 4th.
            if (rank <= 0) return 0.398f;
            if (rank == 1) return 0.192f;
            if (rank == 2) return 0.603f;
            return 0.807f;
        }

        void SetRise(float e)
        {
            if (e < 0f) e = 0f;
            if (e > 1f) e = 1f;
            for (int i = 0; i < Slots; i++)
            {
                Transform step = _step[i];
                if (step == null || !step.gameObject.activeSelf) continue;
                float full = _stepH[i];
                float h = full * (0.15f + 0.85f * e);
                Vector3 sc = step.localScale;
                sc.y = h;
                step.localScale = sc;
                Vector3 p = step.localPosition;
                p.y = h * 0.5f;
                step.localPosition = p;
                Transform trim = _trim[i];
                if (trim != null)
                {
                    Vector3 tp = trim.localPosition;
                    tp.y = h + 0.025f;
                    trim.localPosition = tp;
                }
                Transform stand = _podiumAnchor[i];
                if (stand != null)
                {
                    Vector3 sp = stand.localPosition;
                    sp.y = h + 0.05f;
                    stand.localPosition = sp;
                }
                Transform band = _band[i];
                float bandH = BandH;
                if (bandH > h * 0.82f) bandH = h * 0.82f;
                if (band != null)
                {
                    Vector3 bp = band.localPosition;
                    bp.y = h * 0.5f;
                    band.localPosition = bp;
                    Vector3 bs = band.localScale;
                    bs.y = bandH;
                    band.localScale = bs;
                }
                TextMesh num = _rankNum[i];
                if (num != null)
                {
                    Vector3 np = num.transform.localPosition;
                    np.y = h * 0.5f;
                    num.transform.localPosition = np;
                    num.characterSize = NumSize * (bandH / BandH);
                }
                MenuCheer planter = _planter[i];
                if (planter != null)
                {
                    float top = step.position.y + step.lossyScale.y * 0.5f + 0.05f;
                    planter.Floor(top);
                }
            }
        }

        void EnsureDisc(int seat)
        {
            if (_anchor[seat] == null || _disc[seat] != null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Pedestal";
            go.transform.SetParent(_anchor[seat], false);
            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            go.transform.localScale = new Vector3(1.15f, 0.045f, 1.15f);
            go.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
                rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.08f, 0.10f, 0.14f, 1f), 0.40f, 0f);
            var shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.name = "Contact";
            shade.transform.SetParent(_anchor[seat], false);
            Collider shadeCol = shade.GetComponent<Collider>();
            if (shadeCol != null) DestroyImmediate(shadeCol);
            shade.transform.localScale = new Vector3(1.65f, 0.012f, 1.65f);
            shade.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            Renderer shadeRend = shade.GetComponent<Renderer>();
            if (shadeRend != null)
                shadeRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.05f, 0.07f, 0.10f, 1f), 0.40f, 0f);
            _disc[seat] = go.transform;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.05f) dt = 0.05f;
            bool still = MenuVideo.ReduceMotion;
            float t = Time.unscaledTime;
            for (int i = 0; i < Slots; i++)
            {
                if (_anchor[i] == null) continue;
                if (!still)
                    _anchor[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.6f) * 10f, 0f);
                else
                    _anchor[i].localRotation = Quaternion.identity;
                if (_hop[i] > 0f)
                {
                    _hop[i] -= dt / 0.36f;
                    if (_hop[i] < 0f) _hop[i] = 0f;
                }
                if (_figure[i] == null) continue;
                Vector3 p = _figure[i].localPosition;
                p.y = 0.12f + (still ? 0f : MenuPolish.Hop(_hop[i]));
                _figure[i].localPosition = p;
            }
            if (_rising)
            {
                if (still)
                {
                    SetRise(1f);
                    _rising = false;
                }
                else
                {
                    float u = (t - _rise0) / 0.72f;
                    if (u < 0f) u = 0f;
                    if (u > 1f)
                    {
                        u = 1f;
                        _rising = false;
                    }
                    float e = u * u * (3f - 2f * u);
                    SetRise(e);
                }
            }
            if (still) return;
            for (int i = 0; i < _confetti.Length; i++)
            {
                Transform bit = _confetti[i];
                if (bit == null || !bit.gameObject.activeSelf) continue;
                float y = Mathf.Repeat(t * (0.8f + (i % 5) * 0.15f) + i * 0.2f, 3.2f);
                Vector3 p = bit.localPosition;
                p.y = 3.4f - y;
                bit.localPosition = p;
                bit.localRotation = Quaternion.Euler(0f, t * 80f + i * 20f, 0f);
            }
            if (_paradeRoot != null && _paradeCam != null && _paradeCam.enabled)
            {
                Vector3 p = _paradeRoot.localPosition;
                p.x = Mathf.Sin(t * 0.15f) * 1.2f;
                _paradeRoot.localPosition = p;
            }
        }

        static Material Flat(Color c)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return DummyPrimitiveFactory.MakeMat(c, 0.02f, 0f);
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            return mat;
        }

        void BuildPodium()
        {
            var root = new GameObject("Results");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(0f, -80f, 60f);
            _podiumRoot = root.transform;
            float[] heights = new float[Slots];
            float[] xs = new float[Slots];
            for (int s = 0; s < Slots; s++)
                MenuCheer.Slot(s, out xs[s], out heights[s]);
            Color stone = new Color(0.62f, 0.60f, 0.57f, 1f);
            Color cap = new Color(0.74f, 0.72f, 0.68f, 1f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(_podiumRoot, false);
            floor.transform.localPosition = new Vector3(0.80f, -0.04f, 0.4f);
            floor.transform.localScale = new Vector3(36f, 0.08f, 36f);
            var floorCol = floor.GetComponent<Collider>();
            if (floorCol != null) Destroy(floorCol);
            var floorRend = floor.GetComponent<Renderer>();
            if (floorRend != null)
                floorRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.48f, 0.47f, 0.45f, 1f), 0.18f, 0f);
            for (int i = 0; i < Slots; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = "Step" + i.ToString();
                step.transform.SetParent(_podiumRoot, false);
                float wide = MenuCheer.BlockWide;
                float deep = MenuCheer.BlockDeep;
                step.transform.localPosition = new Vector3(xs[i], heights[i] * 0.5f, 0f);
                step.transform.localScale = new Vector3(wide, heights[i], deep);
                _stepH[i] = heights[i];
                var col = step.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var rend = step.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(stone, 0.22f, 0.02f);
                _step[i] = step.transform;
                var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = "Trim" + i.ToString();
                trim.transform.SetParent(_podiumRoot, false);
                trim.transform.localPosition = new Vector3(xs[i], heights[i] + 0.025f, 0f);
                trim.transform.localScale = new Vector3(wide, 0.05f, deep);
                var trimCol = trim.GetComponent<Collider>();
                if (trimCol != null) Destroy(trimCol);
                _trim[i] = trim.transform;
                var trimRend = trim.GetComponent<Renderer>();
                if (trimRend != null)
                    trimRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(cap, 0.2f, 0.04f);
                float plateY = heights[i] * 0.5f;
                var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
                face.name = "Face" + i.ToString();
                face.transform.SetParent(_podiumRoot, false);
                face.transform.localPosition = new Vector3(xs[i], plateY, deep * 0.5f + 0.02f);
                face.transform.localScale = new Vector3(wide * 0.92f, BandH, 0.035f);
                var faceCol = face.GetComponent<Collider>();
                if (faceCol != null) Destroy(faceCol);
                var faceRend = face.GetComponent<Renderer>();
                if (faceRend != null)
                    faceRend.sharedMaterial = Flat(MenuTheme.SeatBand(i));
                _band[i] = face.transform;
                _bandRend[i] = faceRend;
                var numGo = new GameObject("Num" + i.ToString());
                numGo.transform.SetParent(_podiumRoot, false);
                numGo.transform.localPosition = new Vector3(xs[i], plateY, deep * 0.5f + 0.05f);
                var num = numGo.AddComponent<TextMesh>();
                num.text = (i + 1).ToString();
                num.font = MenuTheme.Display;
                num.fontSize = 128;
                num.characterSize = NumSize;
                num.anchor = TextAnchor.MiddleCenter;
                num.alignment = TextAlignment.Center;
                num.color = MenuTheme.SeatInk(i);
                _rankNum[i] = num;
                var blot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                blot.name = "BlockShade" + i.ToString();
                blot.transform.SetParent(_podiumRoot, false);
                blot.transform.localPosition = new Vector3(xs[i], 0.012f, 0.06f);
                blot.transform.localScale = new Vector3(wide + 0.28f, 0.008f, deep + 0.22f);
                var blotCol = blot.GetComponent<Collider>();
                if (blotCol != null) Destroy(blotCol);
                var blotRend = blot.GetComponent<Renderer>();
                if (blotRend != null)
                    blotRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.02f, 0.02f, 0.03f, 0.45f), 0.95f, 0f);
                _blot[i] = blot.transform;
                var stand = new GameObject("Stand" + i.ToString());
                stand.transform.SetParent(_podiumRoot, false);
                stand.transform.localPosition = new Vector3(xs[i], heights[i] + 0.05f, 0f);
                var shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shade.name = "Shade" + i.ToString();
                shade.transform.SetParent(stand.transform, false);
                shade.transform.localPosition = new Vector3(0f, 0.015f, 0f);
                shade.transform.localScale = new Vector3(0.76f, 0.012f, 0.50f);
                var shadeCol = shade.GetComponent<Collider>();
                if (shadeCol != null) Destroy(shadeCol);
                var shadeRend = shade.GetComponent<Renderer>();
                if (shadeRend != null)
                    shadeRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.05f, 0.05f, 0.08f, 1f), 0.95f, 0f);
                _podiumAnchor[i] = stand.transform;
            }
            for (int i = 0; i < _confetti.Length; i++)
            {
                var bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bit.name = "Confetti" + i.ToString();
                bit.transform.SetParent(_podiumRoot, false);
                bit.transform.localScale = new Vector3(0.16f, 0.26f, 0.05f);
                float ang = i * 0.55f;
                bit.transform.localPosition = new Vector3(Mathf.Sin(ang) * 1.35f, 2.4f + (i % 4) * 0.15f, 0.92f + Mathf.Cos(ang) * 0.18f);
                var col = bit.GetComponent<Collider>();
                if (col != null) Destroy(col);
                Color paint = i % 3 == 0 ? MenuTheme.Gold : (i % 3 == 1 ? MenuTheme.Seat(0) : MenuTheme.Seat(2));
                var rend = bit.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(paint, 0.2f, 0f);
                bit.SetActive(false);
                _confetti[i] = bit.transform;
            }
            var camGo = new GameObject("ResultsCam");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 80f;
            cam.depth = -19;
            cam.enabled = false;
            _podiumRt = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
            _podiumRt.Create();
            cam.targetTexture = _podiumRt;
            cam.transform.position = _podiumRoot.position + new Vector3(-1.05f, 1.65f, 9.20f);
            cam.transform.LookAt(_podiumRoot.position + new Vector3(0.73f, 1.35f, 0f));
            _podiumCam = cam;
        }

        void BuildParade()
        {
            var root = new GameObject("Parade");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(0f, -120f, 80f);
            _paradeRoot = root.transform;
            float[] x = { -2.4f, -0.8f, 0.8f, 2.4f };
            for (int i = 0; i < Slots; i++)
            {
                var stand = new GameObject("Runner" + i.ToString());
                stand.transform.SetParent(_paradeRoot, false);
                stand.transform.localPosition = new Vector3(x[i], 0f, 0f);
                var shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shade.name = "Contact";
                shade.transform.SetParent(stand.transform, false);
                Collider shadeCol = shade.GetComponent<Collider>();
                if (shadeCol != null) DestroyImmediate(shadeCol);
                shade.transform.localScale = new Vector3(1.55f, 0.012f, 1.55f);
                shade.transform.localPosition = new Vector3(0f, 0.012f, 0f);
                Renderer shadeRend = shade.GetComponent<Renderer>();
                if (shadeRend != null)
                    shadeRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.04f, 0.06f, 0.10f, 1f), 0.9f, 0f);
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Pedestal";
                disc.transform.SetParent(stand.transform, false);
                Collider discCol = disc.GetComponent<Collider>();
                if (discCol != null) DestroyImmediate(discCol);
                disc.transform.localScale = new Vector3(1.15f, 0.04f, 1.15f);
                disc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                Renderer discRend = disc.GetComponent<Renderer>();
                if (discRend != null)
                    discRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(MenuTheme.Seat(i), 0.22f, 0.18f);
                GameObject body = MenuMannequin.Spawn(stand.transform, MenuMannequin.NameOf(4), MenuMannequin.NameOf(i), false);
                if (body != null)
                {
                    body.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                    MenuCheer.Dress(body, MenuTheme.Seat(i));
                    MenuIdle idle = body.GetComponent<MenuIdle>();
                    if (idle != null) idle.enabled = false;
                    MenuStride stride = body.AddComponent<MenuStride>();
                    stride.Begin(i % 2 == 1, i * 0.37f);
                }
            }
            var camGo = new GameObject("ParadeCam");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
            cam.depth = -18;
            cam.enabled = false;
            _paradeRt = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
            _paradeRt.Create();
            cam.targetTexture = _paradeRt;
            cam.transform.position = _paradeRoot.position + new Vector3(0f, 1.15f, 7.2f);
            cam.transform.LookAt(_paradeRoot.position + new Vector3(0f, 0.82f, 0f));
            _paradeCam = cam;
        }

        public static Sprite WellSprite()
        {
            if (_wellSprite != null) return _wellSprite;
            Texture2D tex = WellTex();
            _wellSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _wellSprite;
        }

        static Texture2D WellTex()
        {
            if (_wellTex != null) return _wellTex;
            const int w = 8;
            const int h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color bot = new Color(0.10f, 0.20f, 0.40f, 1f);
            Color top = new Color(0.58f, 0.66f, 0.74f, 1f);
            for (int y = 0; y < h; y++)
            {
                float u = y / (h - 1f);
                Color c = Color.Lerp(bot, top, u);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            _wellTex = tex;
            return tex;
        }

        static Texture2D ShadowTex()
        {
            if (_shadowTex != null) return _shadowTex;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - d;
                    if (a < 0f) a = 0f;
                    a = a * a * 0.55f;
                    tex.SetPixel(x, y, new Color(0.02f, 0.04f, 0.07f, a));
                }
            }
            tex.Apply();
            _shadowTex = tex;
            return tex;
        }

        void AddSun(string name, Vector3 rayDir, Color color, float intensity, bool shadow = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.LookRotation(rayDir);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadow ? LightShadows.Soft : LightShadows.None;
            if (shadow) light.shadowStrength = 0.65f;
        }

        void BuildWell(int i)
        {
            if (_cam[i] == null) return;
            if (_wellMat == null)
            {
                var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
                _wellMat = new Material(shader);
                _wellMat.mainTexture = WellTex();
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PreviewWell";
            quad.transform.SetParent(_cam[i].transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 8f);
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(4.4f, 4.4f, 1f);
            Collider col = quad.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer rend = quad.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = _wellMat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }

        void BuildShadow(int i)
        {
            if (_anchor[i] == null) return;
            if (_shadowMat == null)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
                _shadowMat = new Material(shader);
                _shadowMat.mainTexture = ShadowTex();
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SoftShadow";
            quad.transform.SetParent(_anchor[i], false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
            quad.transform.localScale = new Vector3(1.45f, 0.72f, 1f);
            Collider col = quad.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer rend = quad.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = _shadowMat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }

        void Aim(int i)
        {
            if (_cam[i] == null || _anchor[i] == null) return;
            Vector3 focus = _anchor[i].position + new Vector3(0f, 1.10f, 0f);
            // Far enough that a ready hop of 0.28 m still leaves about 5% above the head.
            _cam[i].transform.position = focus + new Vector3(1.90f, 0.50f, 5.42f);
            _cam[i].transform.LookAt(focus);
        }

        void OnDestroy()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_rt[i] == null) continue;
                _rt[i].Release();
                Destroy(_rt[i]);
                _rt[i] = null;
            }
            if (_podiumRt != null)
            {
                _podiumRt.Release();
                Destroy(_podiumRt);
                _podiumRt = null;
            }
            if (_paradeRt != null)
            {
                _paradeRt.Release();
                Destroy(_paradeRt);
                _paradeRt = null;
            }
        }
    }
}
