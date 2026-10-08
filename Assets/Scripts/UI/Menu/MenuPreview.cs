using Tag.Art;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// One camera and render texture per seat. The body is the existing
    /// primitive Hier mannequin (same color keys as the match pawn).
    /// Idle motion is visual only and uses unscaled time.
    /// </summary>
    public sealed class MenuPreview : MonoBehaviour
    {
        const int Slots = 4;

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
        readonly Transform[] _podiumAnchor = new Transform[Slots];
        readonly Transform[] _confetti = new Transform[18];

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
                cam.backgroundColor = new Color(0.12f, 0.36f, 0.74f, 1f);
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
                    if (child != null && (child.name == "Pedestal" || child.name == "Contact")) continue;
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
                if (!on) continue;
                MenuPodium.Row row = rows[i];
                GameObject body = MenuMannequin.Spawn(_podiumAnchor[i], MenuMannequin.NameOf(row.Hier), MenuMannequin.NameOf(row.Accent), row.Hat != 0);
                bool win = row.Winner;
                bool clap = !win && i < 3;
                MenuCheer.Play(body, win, clap, i == 2);
                if (body != null)
                {
                    int seat = row.Seat;
                    if (seat < 0) seat = 0;
                    if (seat > 3) seat = 3;
                    MenuMannequin.PaintSlot(body, MenuTheme.Seat(seat));
                    body.transform.localPosition = Vector3.zero;
                    if (win) body.transform.localScale = new Vector3(1.16f, 1.16f, 1.16f);
                }
            }
            bool party = crowned && !MenuVideo.ReduceMotion;
            for (int i = 0; i < _confetti.Length; i++)
            {
                if (_confetti[i] != null) _confetti[i].gameObject.SetActive(party);
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
                rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(MenuTheme.Seat(seat), 0.22f, 0.18f);
            var shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.name = "Contact";
            shade.transform.SetParent(_anchor[seat], false);
            Collider shadeCol = shade.GetComponent<Collider>();
            if (shadeCol != null) DestroyImmediate(shadeCol);
            shade.transform.localScale = new Vector3(1.65f, 0.012f, 1.65f);
            shade.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            Renderer shadeRend = shade.GetComponent<Renderer>();
            if (shadeRend != null)
                shadeRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.04f, 0.06f, 0.10f, 1f), 0.9f, 0f);
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
                    _anchor[i].localRotation = Quaternion.Euler(0f, t * 18f + i * 40f, 0f);
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
            Color[] paints =
            {
                MenuTheme.Gold,
                new Color(0.75f, 0.78f, 0.84f, 1f),
                new Color(0.72f, 0.42f, 0.22f, 1f),
                MenuTheme.Panel
            };
            for (int i = 0; i < Slots; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = "Step" + i.ToString();
                step.transform.SetParent(_podiumRoot, false);
                float wide = i == 0 ? 1.16f : i == 3 ? 1.28f : 1.06f;
                float deep = 1.02f;
                step.transform.localPosition = new Vector3(xs[i], heights[i] * 0.5f, 0f);
                step.transform.localScale = new Vector3(wide, heights[i], deep);
                var col = step.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var rend = step.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(paints[i], 0.35f, 0.08f);
                _step[i] = step.transform;
                var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = "Trim" + i.ToString();
                trim.transform.SetParent(_podiumRoot, false);
                trim.transform.localPosition = new Vector3(xs[i], heights[i] + 0.025f, 0f);
                trim.transform.localScale = new Vector3(wide + 0.10f, 0.05f, deep + 0.08f);
                var trimCol = trim.GetComponent<Collider>();
                if (trimCol != null) Destroy(trimCol);
                var trimRend = trim.GetComponent<Renderer>();
                if (trimRend != null)
                    trimRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.98f, 0.94f, 0.82f, 1f), 0.28f, 0.12f);
                float plateH = 0.30f;
                float plateY = -0.02f;
                var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
                face.name = "Face" + i.ToString();
                face.transform.SetParent(_podiumRoot, false);
                face.transform.localPosition = new Vector3(xs[i], plateY, deep * 0.5f + 0.72f);
                face.transform.localScale = new Vector3(wide * 0.46f, plateH, 0.05f);
                var faceCol = face.GetComponent<Collider>();
                if (faceCol != null) Destroy(faceCol);
                var faceRend = face.GetComponent<Renderer>();
                if (faceRend != null)
                    faceRend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.98f, 0.96f, 0.90f, 1f), 0.4f, 0.02f);
                var numGo = new GameObject("Num" + i.ToString());
                numGo.transform.SetParent(_podiumRoot, false);
                numGo.transform.localPosition = new Vector3(xs[i], plateY, deep * 0.5f + 0.78f);
                var num = numGo.AddComponent<TextMesh>();
                num.text = (i + 1).ToString();
                num.fontSize = 64;
                num.characterSize = 0.045f;
                num.anchor = TextAnchor.MiddleCenter;
                num.alignment = TextAlignment.Center;
                num.color = new Color(0.08f, 0.08f, 0.12f, 1f);
                var stand = new GameObject("Stand" + i.ToString());
                stand.transform.SetParent(_podiumRoot, false);
                stand.transform.localPosition = new Vector3(xs[i], heights[i] + 0.05f, 0f);
                var shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shade.name = "Shade" + i.ToString();
                shade.transform.SetParent(stand.transform, false);
                shade.transform.localPosition = new Vector3(0f, 0.015f, 0f);
                shade.transform.localScale = new Vector3(1.15f, 0.012f, 0.72f);
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
                bit.transform.localPosition = new Vector3(Mathf.Sin(ang) * 1.6f, 2.4f + (i % 4) * 0.15f, Mathf.Cos(ang) * 0.35f);
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
            cam.farClipPlane = 40f;
            cam.depth = -19;
            cam.enabled = false;
            _podiumRt = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
            _podiumRt.Create();
            cam.targetTexture = _podiumRt;
            cam.transform.position = _podiumRoot.position + new Vector3(0.78f, 1.55f, 6.2f);
            cam.transform.LookAt(_podiumRoot.position + new Vector3(0.78f, 1.05f, 0f));
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

        void Aim(int i)
        {
            if (_cam[i] == null || _anchor[i] == null) return;
            Vector3 focus = _anchor[i].position + new Vector3(0f, 0.98f, 0f);
            _cam[i].transform.position = focus + new Vector3(0f, 0.02f, 4.85f);
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
