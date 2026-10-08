#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Tag.EditorTools
{
    public static class MotionGalleryMenu
    {
        [MenuItem("Tag/Motion Gallery")]
        public static void Open()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MotionGallery.unity");
        }
    }
}
#endif
