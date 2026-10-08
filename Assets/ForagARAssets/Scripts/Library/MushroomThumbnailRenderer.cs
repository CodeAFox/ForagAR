using System.Collections.Generic;
using UnityEngine;

namespace ForagAR.Library
{
    /// <summary>
    /// Renders preview images of mushroom prefabs at runtime with an off-screen camera,
    /// so new mushrooms get a library thumbnail without any manual screenshots.
    /// </summary>
    /// <remarks>
    /// Only the meshes of the prefab are copied into the thumbnail stage, so interactables,
    /// physics and other scripts on the prefab never run.
    /// </remarks>
    public class MushroomThumbnailRenderer : MonoBehaviour
    {
        // Far away from the AR session so the AR camera never sees the stage.
        static readonly Vector3 k_StageOrigin = new Vector3(0f, -10000f, 0f);

        [SerializeField]
        [Tooltip("Layer used for the thumbnail stage. Should not be rendered by the AR camera's normal content.")]
        int m_Layer = 31;

        [SerializeField]
        [Tooltip("Width and height of each thumbnail in pixels.")]
        int m_Size = 256;

        [SerializeField]
        Color m_BackgroundColor = new Color(0f, 0f, 0f, 0f);

        [SerializeField]
        [Tooltip("Camera rotation looking at the mushroom.")]
        Vector3 m_ViewAngles = new Vector3(20f, -30f, 0f);

        [SerializeField]
        [Tooltip("Extra space around the mushroom. 1 fits its bounding sphere exactly.")]
        float m_Padding = 1.1f;

        readonly Dictionary<GameObject, Texture2D> m_Cache = new Dictionary<GameObject, Texture2D>();
        Camera m_Camera;
        Light m_Light;

        /// <summary>
        /// Returns a cached thumbnail for the prefab, rendering it the first time.
        /// </summary>
        public Texture2D GetThumbnail(GameObject prefab)
        {
            if (prefab == null)
                return null;

            if (m_Cache.TryGetValue(prefab, out var cached) && cached != null)
                return cached;

            var texture = Render(prefab);
            m_Cache[prefab] = texture;
            return texture;
        }

        void OnDestroy()
        {
            foreach (var texture in m_Cache.Values)
            {
                if (texture != null)
                    Destroy(texture);
            }

            m_Cache.Clear();

            if (m_Camera != null)
                Destroy(m_Camera.gameObject);
        }

        void EnsureRig()
        {
            if (m_Camera != null)
                return;

            var rig = new GameObject("Thumbnail Camera");
            m_Camera = rig.AddComponent<Camera>();
            m_Camera.enabled = false;
            m_Camera.cullingMask = 1 << m_Layer;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = m_BackgroundColor;
            m_Camera.fieldOfView = 30f;
            m_Camera.allowHDR = false;

            var lightObject = new GameObject("Thumbnail Light");
            lightObject.transform.SetParent(rig.transform, false);
            m_Light = lightObject.AddComponent<Light>();
            m_Light.type = LightType.Directional;
            m_Light.intensity = 1.2f;
            m_Light.cullingMask = 1 << m_Layer;
            m_Light.shadows = LightShadows.None;
            m_Light.enabled = false;
        }

        Texture2D Render(GameObject prefab)
        {
            EnsureRig();

            var stage = new GameObject($"Thumbnail Stage - {prefab.name}");
            stage.transform.position = k_StageOrigin;

            if (!MushroomMeshCopy.Build(prefab, stage.transform, m_Layer, out var bounds))
            {
                DestroyImmediate(stage);
                Debug.LogWarning($"Mushroom prefab '{prefab.name}' has no meshes to render a thumbnail from.", prefab);
                return null;
            }

            var viewRotation = Quaternion.Euler(m_ViewAngles);
            var viewDirection = viewRotation * Vector3.forward;
            var radius = Mathf.Max(bounds.extents.magnitude, 0.001f);
            var distance = radius * m_Padding / Mathf.Sin(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            m_Camera.transform.SetPositionAndRotation(bounds.center - viewDirection * distance, viewRotation);
            m_Camera.nearClipPlane = Mathf.Max(0.001f, distance - radius * 2f);
            m_Camera.farClipPlane = distance + radius * 2f;
            m_Light.transform.rotation = viewRotation * Quaternion.Euler(25f, 25f, 0f);

            var descriptor = new RenderTextureDescriptor(m_Size, m_Size, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 4,
                sRGB = true,
            };
            var renderTexture = RenderTexture.GetTemporary(descriptor);
            var previousActive = RenderTexture.active;

            m_Camera.targetTexture = renderTexture;
            m_Light.enabled = true;
            m_Camera.Render();
            m_Light.enabled = false;
            m_Camera.targetTexture = null;

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(m_Size, m_Size, TextureFormat.RGBA32, false)
            {
                name = $"{prefab.name} Thumbnail",
            };
            texture.ReadPixels(new Rect(0, 0, m_Size, m_Size), 0, 0);
            texture.Apply(false, true);
            RenderTexture.active = previousActive;

            RenderTexture.ReleaseTemporary(renderTexture);
            DestroyImmediate(stage);

            return texture;
        }
    }
}
