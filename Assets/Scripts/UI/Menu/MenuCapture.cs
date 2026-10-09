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
            Texture2D shot = ReflectGrab();
            if (shot != null) return shot;
            return CanvasGrab();
        }

        static Texture2D ReflectGrab()
        {
            System.Type module = System.Type.GetType("UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule");
            if (module == null) return null;
            MethodInfo method = module.GetMethod("CaptureScreenshotAsTexture", BindingFlags.Public | BindingFlags.Static, null, System.Type.EmptyTypes, null);
            if (method == null) return null;
            try
            {
                return method.Invoke(null, null) as Texture2D;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        static Texture2D CanvasGrab()
        {
            int w = UnityEngine.Screen.width;
            int h = UnityEngine.Screen.height;
            if (w < 2 || h < 2) return null;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            var camGo = new GameObject("MenuGrabCam");
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            cam.cullingMask = 0;
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 10f;
            cam.targetTexture = rt;
            Camera main = Camera.main;
            RenderTexture mainRt = null;
            if (main != null && main.isActiveAndEnabled && main.targetTexture == null)
            {
                mainRt = main.targetTexture;
                main.targetTexture = rt;
                main.Render();
                main.targetTexture = mainRt;
            }
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
                if (!canvas.isActiveAndEnabled) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode != RenderMode.ScreenSpaceCamera)
                    continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            cam.Render();
            RenderTexture prev = RenderTexture.active;
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
            cam.targetTexture = null;
            Object.Destroy(camGo);
            rt.Release();
            Object.Destroy(rt);
            return tex;
        }

        static void Finish()
        {
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
