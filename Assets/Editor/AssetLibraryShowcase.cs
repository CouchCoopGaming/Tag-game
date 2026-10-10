#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Tag.Art.Library;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tag.EditorTools
{
    public static class AssetLibraryShowcase
    {
        const string ScenePath = "Assets/Scenes/AssetShowcase.unity";
        const string LibraryRoot = "Assets/Art/Props/Library";

        static readonly string[] CategoryOrder =
        {
            "Showcase", "StreetFurniture", "Roads", "Buildings", "Utility", "Park", "Harbor"
        };

        [MenuItem("Tag/Asset Showcase")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var buckets = new Dictionary<string, List<GameObject>>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { LibraryRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;
                var meta = prefab.GetComponent<LibraryPropMeta>();
                var cat = meta != null && !string.IsNullOrEmpty(meta.category) ? meta.category : "Other";
                if (!buckets.ContainsKey(cat))
                    buckets[cat] = new List<GameObject>();
                buckets[cat].Add(prefab);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            var rowZ = 0f;
            var minX = 0f;
            var maxX = 10f;
            var minZ = 0f;
            foreach (var cat in Ordered(buckets))
            {
                var items = buckets[cat];
                items.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                var label = new GameObject("Row_" + cat);
                label.transform.position = new Vector3(-2.2f, 0.15f, rowZ);
                var text = label.AddComponent<TextMesh>();
                text.text = cat;
                text.characterSize = 0.18f;
                text.fontSize = 64;
                text.anchor = TextAnchor.MiddleRight;
                text.color = new Color(0.12f, 0.13f, 0.15f);

                var cursor = 0f;
                var depth = 2f;
                foreach (var prefab in items)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    var bounds = BoundsOf(instance);
                    var px = cursor - bounds.min.x;
                    instance.transform.position = new Vector3(px, 0f, rowZ);
                    bounds = BoundsOf(instance);
                    cursor = bounds.max.x + 1.8f;
                    depth = Mathf.Max(depth, bounds.size.z);
                    maxX = Mathf.Max(maxX, bounds.max.x);
                    minZ = Mathf.Min(minZ, bounds.min.z);
                }
                rowZ -= depth + 6f;
                minX = Mathf.Min(minX, -4f);
            }

            ground.transform.position = new Vector3((minX + maxX) * 0.5f, -0.05f, (minZ + 2f) * 0.5f);
            ground.transform.localScale = new Vector3(Mathf.Max(20f, maxX - minX + 16f), 0.1f, Mathf.Max(20f, 2f - minZ + 16f));

            var cam = Camera.main;
            if (cam != null)
            {
                var eye = new Vector3(maxX * 0.35f, Mathf.Max(8f, -rowZ * 0.35f), 12f);
                cam.transform.position = eye;
                cam.transform.LookAt(new Vector3(maxX * 0.35f, 1f, rowZ * 0.5f));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Tag] Asset showcase written to " + ScenePath + ". It is not in the build settings.");
        }

        static List<string> Ordered(Dictionary<string, List<GameObject>> buckets)
        {
            var list = new List<string>();
            foreach (var cat in CategoryOrder)
            {
                if (buckets.ContainsKey(cat))
                    list.Add(cat);
            }
            foreach (var cat in buckets.Keys)
            {
                if (!list.Contains(cat))
                    list.Add(cat);
            }
            return list;
        }

        static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
#endif
