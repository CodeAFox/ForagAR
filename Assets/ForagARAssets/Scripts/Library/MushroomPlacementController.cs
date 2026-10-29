using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

namespace ForagAR.Library
{
    /// <summary>
    /// Drags a mushroom from a library card into the AR view. While dragging over the camera view,
    /// a preview of the mushroom sits on the detected surface under the finger; releasing places it
    /// through the scene's ObjectSpawner. Releasing over the library or off any surface cancels.
    /// </summary>
    public class MushroomPlacementController : MonoBehaviour
    {
        static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        [SerializeField]
        MushroomLibraryView m_Library;

        [SerializeField]
        [Tooltip("The library panel. Releasing over it cancels the drag.")]
        RectTransform m_LibraryPanel;

        [SerializeField]
        [Tooltip("Optional. Found or added on the XR Origin at runtime if empty.")]
        ARRaycastManager m_RaycastManager;

        [SerializeField]
        [Tooltip("Only place mushrooms on floors and table tops, not walls or ceilings.")]
        bool m_RequireHorizontalSurface = true;

        [SerializeField]
        [Tooltip("Turn off the AR template's tap-a-surface-to-place, so mushrooms only come from the library.")]
        bool m_DisableTapToPlace = true;

        [SerializeField]
        float m_GhostSize = 220f;

        [SerializeField]
        [Tooltip("Ghost tint while over the camera view but not over a surface.")]
        Color m_NoSurfaceTint = new Color(1f, 0.55f, 0.55f, 0.85f);

        MushroomCard m_Card;
        RawImage m_Ghost;
        GameObject m_Preview;
        bool m_HasPlacement;
        Vector3 m_PlacementPoint;
        Vector3 m_PlacementNormal;

        /// <summary>
        /// Whether a mushroom is currently being dragged.
        /// </summary>
        public bool isDragging => m_Card != null;

        /// <summary>
        /// Invoked after a mushroom from the library has been placed in the world.
        /// </summary>
        public event Action<PlacedMushroom> mushroomPlaced;

        /// <summary>
        /// The library panel. Dragging a card out of it starts placing the mushroom.
        /// </summary>
        public RectTransform libraryPanel => m_LibraryPanel;

        void Awake()
        {
            if (m_Library == null)
                m_Library = GetComponent<MushroomLibraryView>();

            if (m_LibraryPanel == null)
                m_LibraryPanel = transform as RectTransform;
        }

        void Start()
        {
            if (m_DisableTapToPlace)
            {
                foreach (var trigger in FindObjectsByType<ARInteractorSpawnTrigger>(FindObjectsSortMode.None))
                    trigger.enabled = false;
            }

            if (m_RaycastManager == null)
                m_RaycastManager = FindAnyObjectByType<ARRaycastManager>();

            if (m_RaycastManager == null)
            {
                // ARRaycastManager must live on the XR Origin.
                var origin = FindAnyObjectByType<XROrigin>();
                if (origin != null)
                    m_RaycastManager = origin.gameObject.AddComponent<ARRaycastManager>();
                else
                    Debug.LogWarning("No XR Origin in the scene, so mushrooms cannot be placed.", this);
            }
        }

        void OnDisable()
        {
            CancelDrag();
        }

        public void BeginDrag(MushroomCard card, Vector2 screenPosition)
        {
            CancelDrag();

            m_Card = card;
            CreateGhost(card);
            CreatePreview(card.entry.prefab);
            UpdateDrag(screenPosition);
        }

        public void UpdateDrag(Vector2 screenPosition)
        {
            if (!isDragging)
                return;

            var overLibrary = m_LibraryPanel != null &&
                RectTransformUtility.RectangleContainsScreenPoint(m_LibraryPanel, screenPosition, null);

            m_HasPlacement = !overLibrary && TryFindSurface(screenPosition, out m_PlacementPoint, out m_PlacementNormal);

            if (m_Preview != null)
            {
                m_Preview.SetActive(m_HasPlacement);
                if (m_HasPlacement)
                    m_Preview.transform.SetPositionAndRotation(m_PlacementPoint, FaceCamera(m_PlacementPoint, m_PlacementNormal));
            }

            if (m_Ghost != null)
            {
                m_Ghost.gameObject.SetActive(!m_HasPlacement);
                m_Ghost.color = overLibrary ? Color.white : m_NoSurfaceTint;
                MoveGhost(screenPosition);
            }
        }

        public void EndDrag(Vector2 screenPosition)
        {
            if (!isDragging)
                return;

            UpdateDrag(screenPosition);
            if (m_HasPlacement)
                Place(m_Card, m_PlacementPoint, m_PlacementNormal);

            CancelDrag();
        }

        public void CancelDrag()
        {
            if (m_Ghost != null)
                Destroy(m_Ghost.gameObject);

            if (m_Preview != null)
                Destroy(m_Preview);

            m_Ghost = null;
            m_Preview = null;
            m_Card = null;
            m_HasPlacement = false;
        }

        bool TryFindSurface(Vector2 screenPosition, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = Vector3.up;

            if (m_RaycastManager == null || !m_RaycastManager.Raycast(screenPosition, s_Hits, TrackableType.PlaneWithinPolygon))
                return false;

            // Hits are sorted nearest first.
            foreach (var hit in s_Hits)
            {
                var plane = hit.trackable as ARPlane;
                if (m_RequireHorizontalSurface && (plane == null || plane.alignment != PlaneAlignment.HorizontalUp))
                    continue;

                point = hit.pose.position;
                normal = plane != null ? plane.normal : hit.pose.up;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Same facing as <c>ObjectSpawner</c>: upright on the surface, turned towards the camera.
        /// </summary>
        Quaternion FaceCamera(Vector3 point, Vector3 normal)
        {
            var spawner = m_Library != null ? m_Library.objectSpawner : null;
            var camera = spawner != null ? spawner.cameraToFace : Camera.main;
            if (camera == null)
                return Quaternion.FromToRotation(Vector3.up, normal);

            var forward = Vector3.ProjectOnPlane(camera.transform.position - point, normal);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(camera.transform.forward, normal);

            return Quaternion.LookRotation(forward, normal);
        }

        void Place(MushroomCard card, Vector3 point, Vector3 normal)
        {
            var spawner = m_Library != null ? m_Library.objectSpawner : null;
            GameObject mushroom = null;

            if (spawner == null)
            {
                mushroom = Instantiate(card.entry.prefab);
                mushroom.transform.SetPositionAndRotation(point, FaceCamera(point, normal));
            }
            else
            {
                // Spawn exactly what the preview showed, then restore the spawner's own settings.
                var previousIndex = spawner.spawnOptionIndex;
                var previousRandomAngle = spawner.applyRandomAngleAtSpawn;
                var previousOnlyInView = spawner.onlySpawnInView;

                void OnSpawned(GameObject spawned) => mushroom = spawned;
                spawner.objectSpawned += OnSpawned;

                spawner.spawnOptionIndex = card.index;
                spawner.applyRandomAngleAtSpawn = false;
                spawner.onlySpawnInView = false;
                spawner.TrySpawnObject(point, normal);

                spawner.objectSpawned -= OnSpawned;
                spawner.spawnOptionIndex = previousIndex;
                spawner.applyRandomAngleAtSpawn = previousRandomAngle;
                spawner.onlySpawnInView = previousOnlyInView;
            }

            if (mushroom != null)
                mushroomPlaced?.Invoke(PlacedMushroom.Attach(mushroom, card.entry));
        }

        void CreatePreview(GameObject prefab)
        {
            m_Preview = new GameObject($"Placement Preview - {prefab.name}");
            m_Preview.transform.localScale = prefab.transform.localScale;
            MushroomMeshCopy.Build(prefab, m_Preview.transform, prefab.layer, out _);
            m_Preview.SetActive(false);
        }

        void CreateGhost(MushroomCard card)
        {
            var canvas = m_LibraryPanel != null ? m_LibraryPanel.GetComponentInParent<Canvas>().rootCanvas : null;
            if (canvas == null)
                return;

            var ghostObject = new GameObject("Drag Ghost", typeof(RectTransform));
            ghostObject.layer = canvas.gameObject.layer;
            ghostObject.transform.SetParent(canvas.transform, false);
            ghostObject.transform.SetAsLastSibling();

            m_Ghost = ghostObject.AddComponent<RawImage>();
            m_Ghost.raycastTarget = false;
            m_Ghost.rectTransform.sizeDelta = new Vector2(m_GhostSize, m_GhostSize);

            if (card.thumbnail != null && card.thumbnail.texture != null)
            {
                m_Ghost.texture = card.thumbnail.texture;
                m_Ghost.uvRect = card.thumbnail.uvRect;
            }
        }

        void MoveGhost(Vector2 screenPosition)
        {
            var canvasRect = (RectTransform)m_Ghost.canvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out var localPoint))
                m_Ghost.rectTransform.localPosition = localPoint;
        }
    }
}
