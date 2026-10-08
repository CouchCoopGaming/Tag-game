using Tag.Art;
using Tag.Profiles;
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
        readonly Transform[] _hatMark = new Transform[Slots];

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
        }

        public void Hide()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_cam[i] != null) _cam[i].enabled = false;
            }
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
            {
                if (_hatMark[seat] != null) _hatMark[seat].gameObject.SetActive(hat != 0);
                return;
            }
            _hier[seat] = hier;
            _accent[seat] = accent;
            _hat[seat] = hat;
            for (int c = _anchor[seat].childCount - 1; c >= 0; c--)
                DestroyImmediate(_anchor[seat].GetChild(c).gameObject);
            string body = NameOf(hier);
            string trim = NameOf(accent);
            DummyPrimitiveFactory.Build(_anchor[seat], false, body, trim);
            var hatGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hatGo.name = "MenuHat";
            hatGo.transform.SetParent(_anchor[seat], false);
            hatGo.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            hatGo.transform.localScale = new Vector3(0.28f, 0.16f, 0.28f);
            var col = hatGo.GetComponent<Collider>();
            if (col != null) Destroy(col);
            _hatMark[seat] = hatGo.transform;
            hatGo.SetActive(hat != 0);
            Aim(seat);
        }

        void Update()
        {
            if (MenuVideo.ReduceMotion) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < Slots; i++)
            {
                if (_anchor[i] == null) continue;
                float bob = Mathf.Sin(t * 1.6f + i) * 0.04f;
                _anchor[i].localRotation = Quaternion.Euler(0f, t * 22f + i * 40f, 0f);
                Vector3 p = _anchor[i].position;
                p.y = -40f + bob;
                _anchor[i].position = p;
            }
        }

        void Aim(int i)
        {
            if (_cam[i] == null || _anchor[i] == null) return;
            Vector3 focus = _anchor[i].position + new Vector3(0f, 1.15f, 0f);
            _cam[i].transform.position = focus + new Vector3(0f, 0.35f, 3.15f);
            _cam[i].transform.LookAt(focus);
        }

        static string NameOf(int index)
        {
            int n = LocalProfiles.HierNames.Length;
            if (n < 1) return "Tan";
            if (index < 0) index = 0;
            if (index >= n) index = n - 1;
            return LocalProfiles.HierNames[index];
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
        }
    }
}
