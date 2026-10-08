using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForagAR.Library.Editor
{
    /// <summary>
    /// Creates the ForagAR XR Simulation room and selects it for Play mode. The room is AR Foundation's
    /// default environment (floor, walls and a table), with the camera starting back from the table so it
    /// is framed in the top half of the screen, above the library panel.
    /// </summary>
    static class SimulationEnvironmentSetup
    {
        const string k_SourcePath = "Packages/com.unity.xr.arfoundation/Assets/Prefabs/DefaultSimulationEnvironment.prefab";
        const string k_EnvironmentPath = "Assets/ForagARAssets/SimulationEnvironment/ForagARSimulationEnvironment.prefab";
        const string k_PreferencesPath = "Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset";

        // The default room is a 2.5 m square floor with one wall at x = -1.25 and a table (top about 0.5 m high)
        // at the origin. Stand on the open side facing the wall and look down, so the whole table sits in the
        // visible top half of a portrait phone screen, above the library panel.
        static readonly Vector3 k_CameraPosition = new Vector3(1.1f, 1.4f, 0f);
        static readonly Vector3 k_CameraAngles = new Vector3(58f, -90f, 0f);

        // Where the camera may move (hold right mouse + W/A/S/D/Q/E). The default box stops at 1.1 m high,
        // so it is raised to a standing phone height and covers the whole floor.
        static readonly Bounds k_CameraMovementBounds = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(2.5f, 1.6f, 2.5f));

        // Only used by image tracking, and the white square is confusing in the library view.
        const string k_RemovedChild = "Simulated Tracked Image";

        [MenuItem("ForagAR/Set Up Simulation Environment")]
        static void SetUp()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(k_SourcePath);
            if (source == null)
            {
                Debug.LogError($"Could not find AR Foundation's default simulation environment at {k_SourcePath}.");
                return;
            }

            var room = Object.Instantiate(source);
            room.name = Path.GetFileNameWithoutExtension(k_EnvironmentPath);

            var trackedImage = FindChild(room.transform, k_RemovedChild);
            if (trackedImage != null)
                Object.DestroyImmediate(trackedImage.gameObject);

            if (!SetCameraStart(room))
            {
                Object.DestroyImmediate(room);
                Debug.LogError("The default simulation environment has no SimulationEnvironment component.");
                return;
            }

            var folder = Path.GetDirectoryName(k_EnvironmentPath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder)?.Replace('\\', '/'), Path.GetFileName(folder));

            var environment = PrefabUtility.SaveAsPrefabAsset(room, k_EnvironmentPath);
            Object.DestroyImmediate(room);

            SelectEnvironment(environment);
            EditorGUIUtility.PingObject(environment);
            Debug.Log($"XR Simulation will use {k_EnvironmentPath}. Press Play to try it.");
        }

        static bool SetCameraStart(GameObject room)
        {
            // SimulationEnvironment is internal to AR Foundation, so find it by name and edit it as serialized data.
            foreach (var component in room.GetComponents<MonoBehaviour>())
            {
                if (component == null || component.GetType().Name != "SimulationEnvironment")
                    continue;

                var settings = new SerializedObject(component);
                settings.FindProperty("m_CameraMovementBounds").boundsValue = k_CameraMovementBounds;
                settings.FindProperty("m_CameraStartingPose.position").vector3Value = k_CameraPosition;
                settings.FindProperty("m_CameraStartingPose.rotation").quaternionValue = Quaternion.Euler(k_CameraAngles);
                settings.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }

            return false;
        }

        static void SelectEnvironment(GameObject environment)
        {
            var preferences = AssetDatabase.LoadMainAssetAtPath(k_PreferencesPath);
            if (preferences == null)
            {
                Debug.LogWarning("XR Simulation preferences not found. Pick 'ForagARSimulationEnvironment' in the XR Environment view instead.");
                return;
            }

            var settings = new SerializedObject(preferences);
            settings.FindProperty("m_EnvironmentPrefab").objectReferenceValue = environment;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(preferences);
        }

        static Transform FindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                var match = FindChild(child, childName);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
