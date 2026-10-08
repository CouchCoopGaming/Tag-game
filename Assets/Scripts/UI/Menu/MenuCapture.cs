using System.IO;
using Tag.Couch;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Walks every front-end screen and writes a PNG. The editor menu and the
    /// batchmode entry point both call Request. Gameplay frames do not.
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
            if (_index < 0 || _index >= Order.Length)
            {
                Finish();
                return true;
            }
            _wait++;
            if (_wait == 1)
            {
                host.Present(Order[_index]);
                return true;
            }
            if (_wait < 4) return true;
            Save(Names[_index]);
            _wait = 0;
            _index++;
            if (_index >= Order.Length) Finish();
            return true;
        }

        static void Save(string name)
        {
            string path = Path.Combine(Absolute(_folder), name + ".png");
            try
            {
                Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
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
