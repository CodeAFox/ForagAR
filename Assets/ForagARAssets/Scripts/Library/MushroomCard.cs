using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForagAR.Library
{
    /// <summary>
    /// A single tile in the mushroom library grid: a preview image and a name.
    /// Drag the card into the AR view and let go to place the mushroom. When the library is long enough
    /// to scroll, a drag scrolls it until the finger leaves the library (or the card is long-pressed first).
    /// </summary>
    public class MushroomCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        RawImage m_Thumbnail;

        [SerializeField]
        TMP_Text m_Label;

        [SerializeField]
        [Tooltip("How long the card must be held before it can be dragged out.")]
        float m_LongPressSeconds = 0.3f;

        [SerializeField]
        [Tooltip("Card scale while picked up.")]
        float m_PickedUpScale = 1.08f;

        MushroomPlacementController m_PlacementController;
        ScrollRect m_ScrollRect;

        bool m_Pressed;
        bool m_PickedUp;
        bool m_Dragging;
        bool m_Scrolling;
        float m_PressTime;
        Vector2 m_PressPosition;

        /// <summary>
        /// Index of this mushroom in <see cref="MushroomLibraryView.availableEntries"/>, which matches
        /// the index in the scene's ObjectSpawner prefab list.
        /// </summary>
        public int index { get; private set; }

        /// <summary>
        /// The mushroom this card represents.
        /// </summary>
        public MushroomEntry entry { get; private set; }

        /// <summary>
        /// The card's preview image.
        /// </summary>
        public RawImage thumbnail => m_Thumbnail;

        public void Setup(int entryIndex, MushroomEntry mushroom, MushroomPlacementController placementController)
        {
            index = entryIndex;
            entry = mushroom;
            m_PlacementController = placementController;
            name = $"Card - {mushroom.displayName}";

            if (m_Label != null)
                m_Label.text = mushroom.displayName;

            if (mushroom.thumbnailOverride != null)
                SetThumbnail(mushroom.thumbnailOverride);
            else
                SetThumbnail((Texture)null);
        }

        public void SetThumbnail(Texture texture)
        {
            if (m_Thumbnail == null)
                return;

            m_Thumbnail.texture = texture;
            m_Thumbnail.uvRect = new Rect(0f, 0f, 1f, 1f);
            // Hide the empty white quad until the thumbnail is ready.
            m_Thumbnail.enabled = texture != null;
        }

        void SetThumbnail(Sprite sprite)
        {
            if (m_Thumbnail == null)
                return;

            var texture = sprite.texture;
            var rect = sprite.textureRect;
            m_Thumbnail.texture = texture;
            m_Thumbnail.uvRect = new Rect(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height);
            m_Thumbnail.enabled = true;
        }

        void Awake()
        {
            m_ScrollRect = GetComponentInParent<ScrollRect>();
        }

        void Update()
        {
            if (m_Pressed && !m_PickedUp && !m_Scrolling && Time.unscaledTime - m_PressTime >= m_LongPressSeconds)
                PickUp(m_PressPosition);
        }

        void OnDisable()
        {
            if (m_PickedUp && m_PlacementController != null)
                m_PlacementController.CancelDrag();

            ResetGesture();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_Pressed)
                return;

            m_Pressed = true;
            m_PressTime = Time.unscaledTime;
            m_PressPosition = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // A drag in progress is finished by OnEndDrag, which runs after this.
            if (m_Dragging)
                return;

            if (m_PickedUp && m_PlacementController != null)
                m_PlacementController.CancelDrag();

            ResetGesture();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (m_ScrollRect != null)
                m_ScrollRect.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Nothing to scroll, so any drag picks the mushroom up.
            if (!m_PickedUp && !CanScroll())
                PickUp(eventData.pressPosition);

            if (m_PickedUp)
            {
                m_Dragging = true;
                m_PlacementController.UpdateDrag(eventData.position);
                return;
            }

            m_Scrolling = true;
            if (m_ScrollRect != null)
                m_ScrollRect.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_PickedUp)
            {
                m_PlacementController.UpdateDrag(eventData.position);
                return;
            }

            if (!m_Scrolling)
                return;

            // Scrolling finger left the library: stop scrolling and carry the mushroom out instead.
            if (IsOutsideLibrary(eventData.position))
            {
                if (m_ScrollRect != null)
                    m_ScrollRect.OnEndDrag(eventData);

                m_Scrolling = false;
                PickUp(eventData.position);
                if (m_PickedUp)
                {
                    m_Dragging = true;
                    m_PlacementController.UpdateDrag(eventData.position);
                }

                return;
            }

            if (m_ScrollRect != null)
                m_ScrollRect.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_PickedUp)
                m_PlacementController.EndDrag(eventData.position);
            else if (m_Scrolling && m_ScrollRect != null)
                m_ScrollRect.OnEndDrag(eventData);

            ResetGesture();
        }

        void PickUp(Vector2 screenPosition)
        {
            if (m_PlacementController == null || entry?.prefab == null)
            {
                m_Pressed = false;
                return;
            }

            m_PickedUp = true;
            transform.localScale = Vector3.one * m_PickedUpScale;
            m_PlacementController.BeginDrag(this, screenPosition);
        }

        bool CanScroll()
        {
            if (m_ScrollRect == null || m_ScrollRect.content == null)
                return false;

            var viewport = m_ScrollRect.viewport != null ? m_ScrollRect.viewport : (RectTransform)m_ScrollRect.transform;
            return m_ScrollRect.content.rect.height > viewport.rect.height + 1f;
        }

        bool IsOutsideLibrary(Vector2 screenPosition)
        {
            var panel = m_PlacementController != null ? m_PlacementController.libraryPanel : null;
            return panel != null && !RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition, null);
        }

        void ResetGesture()
        {
            m_Pressed = false;
            m_PickedUp = false;
            m_Dragging = false;
            m_Scrolling = false;
            transform.localScale = Vector3.one;
        }
    }
}
