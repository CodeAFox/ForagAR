using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForagAR.Library
{
    /// <summary>
    /// The tabs of the information section, in the order their buttons appear.
    /// </summary>
    public enum MushroomInfoTab
    {
        Description,
        Habitat,
        Uses,
        LookAlikes,
    }

    /// <summary>
    /// Swaps the bottom panel between the library and the information section of a mushroom.
    /// The information is shown after a mushroom is placed, or when a placed mushroom is selected.
    /// It is split into tabs, and the text of each tab comes from the mushroom's entry in the
    /// <see cref="MushroomCatalog"/>.
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
        [Tooltip("One button per tab, in the order of MushroomInfoTab: Description, Habitat, Uses, Look-Alikes.")]
        Button[] m_TabButtons = new Button[0];

        [SerializeField]
        Color m_TabColor = new Color32(0x2D, 0x34, 0x2A, 0xFF);

        [SerializeField]
        Color m_SelectedTabColor = new Color32(0x6B, 0x8F, 0x4E, 0xFF);

        [SerializeField]
        Color m_TabTextColor = new Color32(0xA8, 0xAD, 0x9C, 0xFF);

        [SerializeField]
        Color m_SelectedTabTextColor = new Color32(0xEE, 0xEA, 0xDE, 0xFF);

        [SerializeField]
        [Tooltip("Shown when a tab has no text yet. {0} is the mushroom's name, {1} the tab's name.")]
        string m_PlaceholderText = "<i>No {1} added for {0} yet.</i>";

        /// <summary>
        /// The mushroom whose information is showing, or null while the library is showing.
        /// </summary>
        public MushroomEntry currentEntry { get; private set; }

        /// <summary>
        /// The tab showing in the information section.
        /// </summary>
        public MushroomInfoTab currentTab { get; private set; }

        void OnEnable()
        {
            if (m_PlacementController != null)
                m_PlacementController.mushroomPlaced += OnMushroomPlaced;

            PlacedMushroom.selected += OnMushroomSelected;

            if (m_BackButton != null)
                m_BackButton.onClick.AddListener(ShowLibrary);

            for (var i = 0; i < m_TabButtons.Length; i++)
            {
                if (m_TabButtons[i] == null)
                    continue;

                // Copy the index so each button's listener keeps its own tab.
                var tab = (MushroomInfoTab)i;
                m_TabButtons[i].onClick.AddListener(() => ShowTab(tab));
            }
        }

        void OnDisable()
        {
            if (m_PlacementController != null)
                m_PlacementController.mushroomPlaced -= OnMushroomPlaced;

            PlacedMushroom.selected -= OnMushroomSelected;

            if (m_BackButton != null)
                m_BackButton.onClick.RemoveListener(ShowLibrary);

            foreach (var button in m_TabButtons)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }
        }

        void Start()
        {
            ShowLibrary();
        }

        /// <summary>
        /// Shows the information section for <paramref name="entry"/>, opened on the Description tab.
        /// </summary>
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

            SetShowingInfo(true);
            ShowTab(MushroomInfoTab.Description);
        }

        /// <summary>
        /// Switches the information section to <paramref name="tab"/> for the current mushroom.
        /// </summary>
        public void ShowTab(MushroomInfoTab tab)
        {
            currentTab = tab;
            UpdateTabButtons();

            if (currentEntry == null || m_Body == null)
                return;

            var text = GetTabText(currentEntry, tab);
            m_Body.text = string.IsNullOrWhiteSpace(text)
                ? string.Format(m_PlaceholderText, currentEntry.displayName, GetTabName(tab).ToLowerInvariant())
                : text;

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

        /// <summary>
        /// The label shown on a tab's button.
        /// </summary>
        public static string GetTabName(MushroomInfoTab tab)
        {
            switch (tab)
            {
                case MushroomInfoTab.Description: return "Description";
                case MushroomInfoTab.Habitat: return "Habitat";
                case MushroomInfoTab.Uses: return "Uses";
                case MushroomInfoTab.LookAlikes: return "Look-Alikes";
                default: return tab.ToString();
            }
        }

        static string GetTabText(MushroomEntry entry, MushroomInfoTab tab)
        {
            switch (tab)
            {
                case MushroomInfoTab.Description: return entry.description;
                case MushroomInfoTab.Habitat: return entry.habitat;
                case MushroomInfoTab.Uses: return entry.uses;
                // Links to the look-alike mushrooms will go under this text once the library has more of them.
                case MushroomInfoTab.LookAlikes: return entry.lookAlikes;
                default: return null;
            }
        }

        void UpdateTabButtons()
        {
            for (var i = 0; i < m_TabButtons.Length; i++)
            {
                var button = m_TabButtons[i];
                if (button == null)
                    continue;

                var selected = i == (int)currentTab;

                if (button.targetGraphic != null)
                    button.targetGraphic.color = selected ? m_SelectedTabColor : m_TabColor;

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.color = selected ? m_SelectedTabTextColor : m_TabTextColor;
                    label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                }
            }
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
