using UnityEngine;
using UnityEngine.Rendering;

namespace ForagAR.Library
{
    /// <summary>
    /// Copies only the visible meshes of a prefab onto plain GameObjects, so the copy can be shown
    /// (as a thumbnail or placement preview) without running any of the prefab's scripts or physics.
    /// </summary>
    public static class MushroomMeshCopy
    {
        /// <summary>
        /// Builds the copy under <paramref name="parent"/>, laid out relative to the prefab root.
        /// </summary>
        /// <returns><see langword="true"/> if the prefab had at least one mesh to copy.</returns>
        public static bool Build(GameObject prefab, Transform parent, int layer, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            var prefabToLocal = prefab.transform.worldToLocalMatrix;

            foreach (var source in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!source.enabled)
                    continue;

                Mesh mesh = null;
                if (source is SkinnedMeshRenderer skinned)
                    mesh = skinned.sharedMesh;
                else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var filter))
                    mesh = filter.sharedMesh;

                if (mesh == null)
                    continue;

                var matrix = prefabToLocal * source.transform.localToWorldMatrix;
                var copy = new GameObject(source.name) { layer = layer };
                copy.transform.SetParent(parent, false);
                copy.transform.localPosition = matrix.GetPosition();
                copy.transform.localRotation = matrix.rotation;
                copy.transform.localScale = matrix.lossyScale;

                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = copy.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = source.sharedMaterials;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;

                if (hasBounds)
                {
                    bounds.Encapsulate(meshRenderer.bounds);
                }
                else
                {
                    bounds = meshRenderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }
    }
}
