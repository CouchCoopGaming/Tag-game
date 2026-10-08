#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Tag → Menu → Capture Screens, or batchmode:
    /// Unity -batchmode -projectPath . -executeMethod Tag.Ui.Menu.MenuScreenCapture.Capture -screenshot Docs/UiStills/captures
    /// Do not pass -quit. The walk exits on its own when it finishes.
    /// </summary>
    public static class MenuScreenCapture
    {
        const string Key = "Tag.Menu.Capture";
        const string Boot = "Assets/Scenes/Boot.unity";

        [MenuItem("Tag/Menu/Capture Screens")]
        public static void Capture()
        {
            string dir = ScreenshotArg();
            SessionState.SetString(Key, dir);
            if (EditorApplication.isPlaying)
            {
                SessionState.SetString(Key, "");
                MenuCapture.Request(dir);
                return;
            }
            EditorSceneManager.OpenScene(Boot);
            EditorApplication.EnterPlaymode();
        }

        static string ScreenshotArg()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-screenshot") return args[i + 1];
            }
            return "Docs/UiStills/captures";
        }
    }
}
#endif
