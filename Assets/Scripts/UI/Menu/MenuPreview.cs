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
        readonly Camera[] _cam = new Camera[Slots];
        readonly RenderTexture[] _rt = new RenderTexture[Slots];
        readonly int[] _hier = { -1, -1, -1, -1 };
        readonly int[] _accent = { -1, -1, -1, -1 };
        readonly int[] _hat = { -1, -1, -1, -1 };
        Transform _podiumRoot;
        Camera _podiumCam;
        RenderTexture _podiumRt;
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
                cam.backgroundColor = new Color(0.07f, 0.11f, 0.20f, 1f);
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
        }

        public void Hide()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_cam[i] != null) _cam[i].enabled = false;
            }
            if (_podiumCam != null) _podiumCam.enabled = false;
            gameObject.SetActive(false);
        }

        public void Apply(int seat, int hier, int accent, int hat, RawImage view)
        {
            if (seat < 0 || seat >= Slots) return;
            if (view != null)
            {
                view.texture = _rt[seat];
                view.color = Color.white;
            }
            if (_hier[seat] == hier && _accent[seat] == accent && _hat[seat] == hat && _anchor[seat].childCount > 0)
                return;
            _hier[seat] = hier;
            _accent[seat] = accent;
            _hat[seat] = hat;
            for (int c = _anchor[seat].childCount - 1; c >= 0; c--)
                DestroyImmediate(_anchor[seat].GetChild(c).gameObject);
            MenuMannequin.Spawn(_anchor[seat], MenuMannequin.NameOf(hier), MenuMannequin.NameOf(accent), hat != 0);
            Aim(seat);
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
            if (view != null)
            {
                view.texture = _podiumRt;
                view.color = Color.white;
            }
            if (count < 0) count = 0;
            if (count > Slots) count = Slots;
            for (int i = 0; i < Slots; i++)
            {
                if (_podiumAnchor[i] == null) continue;
                for (int c = _podiumAnchor[i].childCount - 1; c >= 0; c--)
                    DestroyImmediate(_podiumAnchor[i].GetChild(c).gameObject);
                bool on = i < count && rows != null;
                _step[i].gameObject.SetActive(on);
                if (!on) continue;
                MenuPodium.Row row = rows[i];
                MenuMannequin.Spawn(_podiumAnchor[i], MenuMannequin.NameOf(row.Hier), MenuMannequin.NameOf(row.Accent), row.Hat != 0);
            }
            bool party = count > 0 && rows != null && rows[0].Winner && !MenuVideo.ReduceMotion;
            for (int i = 0; i < _confetti.Length; i++)
            {
                if (_confetti[i] != null) _confetti[i].gameObject.SetActive(party);
            }
        }

        void Update()
        {
            if (MenuVideo.ReduceMotion) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < Slots; i++)
            {
                if (_anchor[i] == null) continue;
                _anchor[i].localRotation = Quaternion.Euler(0f, t * 18f + i * 40f, 0f);
            }
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
        }

        void BuildPodium()
        {
            var root = new GameObject("Podium");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(0f, -80f, 60f);
            _podiumRoot = root.transform;
            float[] heights = { 1.15f, 0.78f, 0.52f, 0.36f };
            float[] xs = { 0f, -1.7f, 1.7f, 3.2f };
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
                step.transform.localPosition = new Vector3(xs[i], heights[i] * 0.5f, 0f);
                step.transform.localScale = new Vector3(1.35f, heights[i], 1.15f);
                var col = step.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var rend = step.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(paints[i], 0.35f, 0.05f);
                _step[i] = step.transform;
                var stand = new GameObject("Stand" + i.ToString());
                stand.transform.SetParent(_podiumRoot, false);
                stand.transform.localPosition = new Vector3(xs[i], heights[i], 0f);
                _podiumAnchor[i] = stand.transform;
            }
            for (int i = 0; i < _confetti.Length; i++)
            {
                var bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bit.name = "Confetti" + i.ToString();
                bit.transform.SetParent(_podiumRoot, false);
                bit.transform.localScale = new Vector3(0.12f, 0.18f, 0.04f);
                bit.transform.localPosition = new Vector3((i % 6) * 0.7f - 1.8f, 2f, (i / 6) * 0.4f);
                var col = bit.GetComponent<Collider>();
                if (col != null) Destroy(col);
                Color paint = i % 3 == 0 ? MenuTheme.Gold : (i % 3 == 1 ? MenuTheme.Seat(0) : MenuTheme.Seat(1));
                var rend = bit.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(paint, 0.2f, 0f);
                bit.SetActive(false);
                _confetti[i] = bit.transform;
            }
            var camGo = new GameObject("PodiumCam");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.28f, 0.62f, 1f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
            cam.depth = -19;
            cam.enabled = false;
            _podiumRt = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
            _podiumRt.Create();
            cam.targetTexture = _podiumRt;
            cam.transform.position = _podiumRoot.position + new Vector3(0f, 2.4f, 7.2f);
            cam.transform.LookAt(_podiumRoot.position + new Vector3(0.4f, 1.3f, 0f));
            _podiumCam = cam;
        }

        void Aim(int i)
        {
            if (_cam[i] == null || _anchor[i] == null) return;
            Vector3 focus = _anchor[i].position + new Vector3(0f, 1.15f, 0f);
            _cam[i].transform.position = focus + new Vector3(0f, 0.35f, 3.15f);
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
        }
    }
}
