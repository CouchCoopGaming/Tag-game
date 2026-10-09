using System.IO;
using System.Reflection;
using Tag.Couch;
using Tag.Ui.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Walks every front-end screen and writes a PNG. The editor menu and the
    /// batchmode entry point both call Request. Gameplay frames do not.
    /// Composite stills are not proof. The station captures play mode with
    /// Tag/Menu/Capture Screens, or:
    /// Unity -batchmode -projectPath . -executeMethod Tag.Ui.Menu.MenuScreenCapture.Capture -screenshot Docs/UiStills/captures
    /// Do not pass -quit. If the Screen Capture module is off, Save grabs the
    /// overlay canvas through a camera instead.
    /// </summary>
    public static class MenuCapture
    {
        public static bool Running => _run;

        static readonly MenuScreenId[] Order =
        {
            MenuScreenId.Title,
            MenuScreenId.Main,
            MenuScreenId.Join,
            MenuScreenId.Cast,
            MenuScreenId.Rules,
            MenuScreenId.Arena,
            MenuScreenId.Loading,
            MenuScreenId.Pause,
            MenuScreenId.Results,
            MenuScreenId.Options,
            MenuScreenId.Controls,
            MenuScreenId.Credits,
            MenuScreenId.Practice
        };

        static readonly string[] Names =
        {
            "01-title",
            "02-main",
            "03-join",
            "04-characters",
            "05-rules",
            "06-arena",
            "07-loading",
            "08-pause",
            "09-results",
            "10-options",
            "11-controls",
            "12-credits",
            "13-practice"
        };

        static readonly int[] HudPlayers = { 1, 2, 4 };
        static readonly string[] HudNames = { "14-hud-1", "15-hud-2", "16-hud-4" };

        static readonly string[] ExtraNames =
        {
            "17-pause-4",
            "18-audio",
            "19-display",
            "20-access",
            "21-disconnect"
        };

        static bool _run;
        static int _index;
        static int _wait;
        static string _folder = "Docs/UiStills/captures";

        public static void Request(string folder)
        {
            _folder = string.IsNullOrEmpty(folder) ? "Docs/UiStills/captures" : folder;
            _index = 0;
            _wait = 0;
            _run = true;
            if (!CouchPlay.Joined(CouchPlay.DeviceKeyboard))
                CouchPlay.Join(CouchPlay.DeviceKeyboard);
            Directory.CreateDirectory(Absolute(_folder));
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EditorResume()
        {
            string dir = UnityEditor.SessionState.GetString("Tag.Menu.Capture", "");
            if (string.IsNullOrEmpty(dir)) return;
            UnityEditor.SessionState.SetString("Tag.Menu.Capture", "");
            Request(dir);
        }
#endif

        public static bool Drive(MenuHost host)
        {
            if (!_run || host == null) return false;
            int menus = Order.Length;
            int extras = ExtraNames.Length;
            int total = menus + extras + HudPlayers.Length;
            if (_index < 0 || _index >= total)
            {
                Finish();
                return true;
            }
            _wait++;
            if (_index < menus)
            {
                if (_wait == 1)
                {
                    MenuFlow.PreviewLost(false);
                    MatchHud.EndPreview();
                    host.Present(Order[_index]);
                    return true;
                }
                if (_wait < 4) return true;
                Save(Names[_index]);
            }
            else if (_index < menus + extras)
            {
                int extra = _index - menus;
                if (_wait == 1)
                {
                    MenuFlow.PreviewLost(false);
                    MatchHud.EndPreview();
                    if (extra == 0) host.PresentSplit(4);
                    else if (extra == 1) host.PresentOptions(MenuDepth.Audio);
                    else if (extra == 2) host.PresentOptions(MenuDepth.Display);
                    else if (extra == 3) host.PresentOptions(MenuDepth.Access);
                    else host.PresentLost();
                    return true;
                }
                if (_wait < 4) return true;
                Save(ExtraNames[extra]);
                MenuFlow.PreviewLost(false);
            }
            else
            {
                int hud = _index - menus - extras;
                if (_wait == 1)
                {
                    host.HideForMatch();
                    Stage(HudPlayers[hud]);
                    MatchHud.Preview(HudPlayers[hud]);
                    return true;
                }
                if (_wait < 4) return true;
                Save(HudNames[hud]);
            }
            _wait = 0;
            _index++;
            if (_index >= total)
            {
                MatchHud.EndPreview();
                Finish();
            }
            return true;
        }

        static void Save(string name)
        {
            string path = Path.Combine(Absolute(_folder), name + ".png");
            try
            {
                Texture2D shot = GrabFrame();
                if (shot == null)
                {
                    Debug.LogWarning("[MenuCapture] No frame for " + path);
                    return;
                }
                File.WriteAllBytes(path, shot.EncodeToPNG());
                Object.Destroy(shot);
                Debug.Log("[MenuCapture] " + path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[MenuCapture] " + path + " " + e.Message);
            }
        }

        static Texture2D GrabFrame()
        {
            // ScreenCapture.CaptureScreenshotAsTexture renders nothing in
            // batchmode, so always grab camera + canvases into a RenderTexture.
            return CanvasGrab();
        }

        // Fixed 16:9 grab. The batchmode screen is 640x480, which squeezed the
        // canvas to about 1660 units wide and cut the join and character cards.
        public const int GrabW = 1920;
        public const int GrabH = 1080;

        static Texture2D CanvasGrab()
        {
            int w = GrabW;
            int h = GrabH;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0.02f, 0.04f, 0.08f, 1f));
            RenderTexture.active = prev;

            // Every live world camera, by depth, each in its own viewport rect.
            // Rendering Camera.main alone left split panes (and HUD 1) black.
            Camera[] cams = Camera.allCameras;
            System.Array.Sort(cams, (x, y) => x.depth.CompareTo(y.depth));
            bool world = false;
            for (int i = 0; i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (c == null || !c.isActiveAndEnabled || c.targetTexture != null) continue;
                c.targetTexture = rt;
                c.Render();
                c.targetTexture = null;
                world = true;
            }

            var camGo = new GameObject("MenuGrabCam");
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = world ? CameraClearFlags.Depth : CameraClearFlags.Nothing;
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 10f;
            cam.targetTexture = rt;
            // Parked far from the world so a full mask only picks up the canvases
            // (code-built UI children often sit on Default, not the UI layer).
            camGo.transform.position = new Vector3(0f, -10000f, 0f);
            cam.cullingMask = ~0;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var oldCams = new Camera[canvases.Length];
            var planes = new float[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                modes[i] = canvas.renderMode;
                oldCams[i] = canvas.worldCamera;
                planes[i] = canvas.planeDistance;
                if (!canvas.isActiveAndEnabled || !canvas.isRootCanvas) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode != RenderMode.ScreenSpaceCamera)
                    continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            Refresh();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null) continue;
                canvases[i].renderMode = modes[i];
                canvases[i].worldCamera = oldCams[i];
                canvases[i].planeDistance = planes[i];
            }
            Canvas.ForceUpdateCanvases();
            cam.targetTexture = null;
            Kill(camGo);
            rt.Release();
            Kill(rt);
            return tex;
        }

        /// <summary>
        /// Rebuild layout and text at the grab size before the one-off render.
        /// Glyphs are requested first, then every Text is re-dirtied, so no label
        /// draws with stale font-atlas UVs (blank rows, grey blocks).
        /// </summary>
        static void Refresh()
        {
            Canvas.ForceUpdateCanvases();
            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                Text t = texts[i];
                if (t == null || t.font == null || string.IsNullOrEmpty(t.text)) continue;
                int size = t.resizeTextForBestFit ? t.resizeTextMaxSize : t.fontSize;
                t.font.RequestCharactersInTexture(t.text, size, t.fontStyle);
                if (t.resizeTextForBestFit && t.resizeTextMinSize != size)
                    t.font.RequestCharactersInTexture(t.text, t.resizeTextMinSize, t.fontStyle);
            }
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null) texts[i].SetAllDirty();
            }
            Canvas.ForceUpdateCanvases();
        }

        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        // HUD stills need the arena behind them. The front-end scene has none, so
        // the capture builds Mega Park once and gives each pane a camera in its
        // split rect (P1 top-left, P2 top-right, P3 bottom-left, P4 bottom-right).
        static GameObject _stage;
        static readonly System.Collections.Generic.List<GameObject> _paneCams = new System.Collections.Generic.List<GameObject>();

        static readonly Vector3[] PaneEye =
        {
            new Vector3(83f, 3.2f, 45f), new Vector3(42f, 3.2f, 62f),
            new Vector3(118f, 3.4f, 40f), new Vector3(62f, 3.2f, 78f)
        };
        static readonly Vector3[] PaneLook =
        {
            new Vector3(96f, 1.4f, 53f), new Vector3(34f, 1.2f, 47f),
            new Vector3(140f, 2f, 49f), new Vector3(75f, 1.6f, 87f)
        };

        static void Stage(int humans)
        {
            if (_stage == null && Object.FindAnyObjectByType<Tag.Level.MegaParkP1Bootstrap>() == null)
            {
                _stage = new GameObject("MenuCaptureArena");
                var boot = _stage.AddComponent<Tag.Level.MegaParkP1Bootstrap>();
                try { boot.Build(); }
                catch (System.Exception e) { Debug.LogWarning("[MenuCapture] arena build " + e.Message); }
            }
            ClearPaneCams();
            int panes = humans <= 1 ? 1 : humans == 2 ? 2 : 4;
            for (int i = 0; i < panes; i++)
            {
                if (humans == 3 && i == 3) break;
                var go = new GameObject("MenuCapturePane" + (i + 1));
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = 62f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 600f;
                cam.depth = -5 + i;
                go.transform.position = PaneEye[i];
                go.transform.LookAt(PaneLook[i]);
                if (panes == 1) cam.rect = new Rect(0f, 0f, 1f, 1f);
                else if (panes == 2) cam.rect = new Rect(i * 0.5f, 0f, 0.5f, 1f);
                else cam.rect = new Rect((i % 2) * 0.5f, i < 2 ? 0.5f : 0f, 0.5f, 0.5f);
                _paneCams.Add(go);
            }
        }

        static void ClearPaneCams()
        {
            for (int i = 0; i < _paneCams.Count; i++) Kill(_paneCams[i]);
            _paneCams.Clear();
        }

        static void Unstage()
        {
            ClearPaneCams();
            if (_stage != null) Kill(_stage);
            _stage = null;
        }

        static void Finish()
        {
            Unstage();
            _run = false;
            Debug.Log("[MenuCapture] Wrote " + Absolute(_folder));
#if UNITY_EDITOR
            if (Application.isBatchMode)
                UnityEditor.EditorApplication.Exit(0);
#endif
        }

        static string Absolute(string folder)
        {
            if (Path.IsPathRooted(folder)) return folder;
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, folder);
        }
    }
}
