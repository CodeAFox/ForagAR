using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace ForagAR.Library
{
    /// <summary>
    /// Fills the library grid with one card per mushroom in the catalog and hands the same
    /// prefab list to the scene's <see cref="ObjectSpawner"/>, so a card index is a spawn index.
    /// </summary>
    public class MushroomLibraryView : MonoBehaviour
    {
        [SerializeField]
        MushroomCatalog m_Catalog;

        [SerializeField]
        MushroomCard m_CardPrefab;

        [SerializeField]
        [Tooltip("Parent of the cards, usually the Content of the library Scroll View.")]
        RectTransform m_CardContainer;

        [SerializeField]
        MushroomThumbnailRenderer m_ThumbnailRenderer;

        [SerializeField]
        [Tooltip("Spawner that places mushrooms. Its prefab list is replaced by the catalog's prefabs.")]
        ObjectSpawner m_ObjectSpawner;

        [SerializeField]
        [Tooltip("Handles dragging a card into the AR view to place it.")]
        MushroomPlacementController m_PlacementController;

        readonly List<MushroomEntry> m_AvailableEntries = new List<MushroomEntry>();
        readonly List<MushroomCard> m_Cards = new List<MushroomCard>();

        /// <summary>
        /// Catalog entries that have a prefab, in card order.
        /// </summary>
        public IReadOnlyList<MushroomEntry> availableEntries => m_AvailableEntries;

        /// <summary>
        /// The cards currently shown in the library.
        /// </summary>
        public IReadOnlyList<MushroomCard> cards => m_Cards;

        public ObjectSpawner objectSpawner => m_ObjectSpawner;

        void Start()
        {
            if (m_Catalog == null || m_CardPrefab == null || m_CardContainer == null)
            {
                Debug.LogWarning("Mushroom library is missing its catalog, card prefab or card container.", this);
                return;
            }

            if (m_PlacementController == null && !TryGetComponent(out m_PlacementController))
                m_PlacementController = gameObject.AddComponent<MushroomPlacementController>();

            CollectEntries();
            SyncSpawner();
            BuildCards();
            StartCoroutine(RenderThumbnails());
        }

        void CollectEntries()
        {
            m_AvailableEntries.Clear();
            foreach (var entry in m_Catalog.entries)
            {
                if (entry != null && entry.prefab != null)
                    m_AvailableEntries.Add(entry);
            }
        }

        void SyncSpawner()
        {
            if (m_ObjectSpawner == null)
                return;

            var prefabs = new List<GameObject>(m_AvailableEntries.Count);
            foreach (var entry in m_AvailableEntries)
                prefabs.Add(entry.prefab);

            m_ObjectSpawner.objectPrefabs = prefabs;
        }

        void BuildCards()
        {
            foreach (var card in m_Cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }

            m_Cards.Clear();

            for (var i = 0; i < m_AvailableEntries.Count; i++)
            {
                var card = Instantiate(m_CardPrefab, m_CardContainer);
                card.Setup(i, m_AvailableEntries[i], m_PlacementController);
                m_Cards.Add(card);
            }
        }

        IEnumerator RenderThumbnails()
        {
            if (m_ThumbnailRenderer == null)
                yield break;

            // One thumbnail per frame keeps startup from hitching when the library grows.
            foreach (var card in m_Cards)
            {
                yield return null;

                if (card == null || card.entry.thumbnailOverride != null)
                    continue;

                card.SetThumbnail(m_ThumbnailRenderer.GetThumbnail(card.entry.prefab));
            }
        }
    }
}
