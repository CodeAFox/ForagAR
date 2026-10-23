using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForagAR.Library.Editor
{
    /// <summary>
    /// Keeps <see cref="MushroomCatalog"/> matched to the prefabs directly inside the mushroom prefab folder.
    /// Runs automatically when prefabs in that folder are added, removed or moved.
    /// Subfolders (such as Prefabs/UI) are ignored.
    /// </summary>
    public class MushroomCatalogSync : AssetPostprocessor
    {
        public const string PrefabFolder = "Assets/ForagARAssets/Prefabs";
        public const string CatalogPath = "Assets/ForagARAssets/Data/MushroomCatalog.asset";

        [MenuItem("ForagAR/Sync Mushroom Catalog")]
        static void SyncFromMenu()
        {
            var catalog = Sync(GetOrCreateCatalog());
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }

        public static MushroomCatalog GetOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MushroomCatalog>(CatalogPath);
            if (catalog != null)
                return catalog;

            var folder = Path.GetDirectoryName(CatalogPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder)?.Replace('\\', '/'), Path.GetFileName(folder));

            catalog = ScriptableObject.CreateInstance<MushroomCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        /// <summary>
        /// Adds entries for new prefabs and removes entries whose prefab is gone or no longer in the folder.
        /// Existing entries keep their order, names and thumbnails.
        /// </summary>
        public static MushroomCatalog Sync(MushroomCatalog catalog)
        {
            var changed = catalog.entries.RemoveAll(entry =>
                entry == null || entry.prefab == null || !IsMushroomPrefabPath(AssetDatabase.GetAssetPath(entry.prefab))) > 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsMushroomPrefabPath(path))
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || catalog.entries.Exists(entry => entry.prefab == prefab))
                    continue;

                catalog.entries.Add(new MushroomEntry
                {
                    displayName = ObjectNames.NicifyVariableName(prefab.name),
                    prefab = prefab,
                });
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }

            return catalog;
        }

        static bool IsMushroomPrefabPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
                return false;

            return Path.GetDirectoryName(path)?.Replace('\\', '/') == PrefabFolder;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!AnyMushroomPrefab(importedAssets) && !AnyMushroomPrefab(deletedAssets) &&
                !AnyMushroomPrefab(movedAssets) && !AnyMushroomPrefab(movedFromAssetPaths))
                return;

            // Asset database changes are not allowed during import, so sync once it finishes.
            EditorApplication.delayCall -= SyncExistingCatalog;
            EditorApplication.delayCall += SyncExistingCatalog;
        }

        static bool AnyMushroomPrefab(string[] paths)
        {
            foreach (var path in paths)
            {
                if (IsMushroomPrefabPath(path))
                    return true;
            }

            return false;
        }

        static void SyncExistingCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MushroomCatalog>(CatalogPath);
            if (catalog != null)
                Sync(catalog);
        }
    }
}
