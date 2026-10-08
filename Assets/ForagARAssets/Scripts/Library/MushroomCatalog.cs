using System.Collections.Generic;
using UnityEngine;

namespace ForagAR.Library
{
    /// <summary>
    /// All mushrooms available in the library. Kept in sync with the mushroom prefab folder by
    /// <c>MushroomCatalogSync</c> in the editor, since Unity cannot list a folder at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "MushroomCatalog", menuName = "ForagAR/Mushroom Catalog")]
    public class MushroomCatalog : ScriptableObject
    {
        [SerializeField]
        List<MushroomEntry> m_Entries = new List<MushroomEntry>();

        /// <summary>
        /// The mushrooms in library order.
        /// </summary>
        public List<MushroomEntry> entries => m_Entries;
    }
}
