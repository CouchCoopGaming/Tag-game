using UnityEngine;

namespace Tag.Practice
{
    /// <summary>
    /// Unity asset over the JSON. The map lane edits the file; this object
    /// only reads it. Feel numbers are not stored here.
    /// </summary>
    [CreateAssetMenu(menuName = "Tag/Practice Routes", fileName = "PracticeRoutes")]
    public class PracticeRouteTable : ScriptableObject
    {
        public string sourceNote = "Assets/Resources/TagArena/PracticeRoutes.json";
        public int routeCount;

        public void Pull()
        {
            PracticeCatalog.Load();
            routeCount = PracticeCatalog.Count;
        }
    }
}
