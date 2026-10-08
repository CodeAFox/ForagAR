using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForagAR.Library
{
    /// <summary>
    /// Swaps the bottom panel between the library and the information section of a mushroom.
    /// The information is shown after a mushroom is placed, or when a placed mushroom is selected.
    /// Text comes from the mushroom's entry in the <see cref="MushroomCatalog"/>.
    /// </summary>
    public class MushroomInfoView : MonoBehaviour
    {
        [SerializeField]
        MushroomPlacementController m_PlacementController;

        [SerializeField]
        [Tooltip("Everything shown while browsing the library (filter bar, grid).")]
        GameObject m_LibraryContent;

        [SerializeField]
        [Tooltip("Everything shown in the information section.")]
        GameObject m_InfoContent;

        [SerializeField]
        TMP_Text m_Title;

        [SerializeField]
        TMP_Text m_ScientificName;

        [SerializeField]
        TMP_Text m_Body;

        [SerializeField]
        ScrollRect m_BodyScroll;

        [SerializeField]
        Button m_BackButton;

        [SerializeField]
        [Tooltip("Shown when a mushroom has no identification text yet. {0} is the mushroom's name.")]
        string m_PlaceholderText = "<i>Identifiers for {0} will be added here.</i>";

        /// <summary>
        /// The mushroom whose information is showing, or null while the library is showing.
        /// </summary>
        public MushroomEntry currentEntry { get; private set; }

        void OnEnable()
        {
            if (m_PlacementController != null)
                m_PlacementController.mushroomPlaced += OnMushroomPlaced;

            PlacedMushroom.selected += OnMushroomSelected;

            if (m_BackButton != null)
                m_BackButton.onClick.AddListener(ShowLibrary);
        }

        void OnDisable()
        {
            if (m_PlacementController != null)
                m_PlacementController.mushroomPlaced -= OnMushroomPlaced;

            PlacedMushroom.selected -= OnMushroomSelected;

            if (m_BackButton != null)
                m_BackButton.onClick.RemoveListener(ShowLibrary);
        }

        void Start()
        {
            ShowLibrary();
        }

        public void ShowInfo(MushroomEntry entry)
        {
            if (entry == null)
                return;

            currentEntry = entry;

            if (m_Title != null)
                m_Title.text = entry.displayName;

            if (m_ScientificName != null)
            {
                var hasScientificName = !string.IsNullOrWhiteSpace(entry.scientificName);
                m_ScientificName.gameObject.SetActive(hasScientificName);
                m_ScientificName.text = hasScientificName ? entry.scientificName : string.Empty;
            }

            if (m_Body != null)
            {
                m_Body.text = string.IsNullOrWhiteSpace(entry.identification)
                    ? string.Format(m_PlaceholderText, entry.displayName)
                    : entry.identification;
            }

            SetShowingInfo(true);

            if (m_BodyScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                m_BodyScroll.verticalNormalizedPosition = 1f;
            }
        }

        public void ShowLibrary()
        {
            currentEntry = null;
            SetShowingInfo(false);
        }

        void SetShowingInfo(bool showInfo)
        {
            if (m_LibraryContent != null)
                m_LibraryContent.SetActive(!showInfo);

            if (m_InfoContent != null)
                m_InfoContent.SetActive(showInfo);
        }

        void OnMushroomPlaced(PlacedMushroom mushroom)
        {
            ShowInfo(mushroom.entry);
        }

        void OnMushroomSelected(PlacedMushroom mushroom)
        {
            // Ignore the select that happens while a mushroom is being dragged in from the library.
            if (m_PlacementController != null && m_PlacementController.isDragging)
                return;

            ShowInfo(mushroom.entry);
        }
    }
}
