using System;
using UnityEngine;

namespace ForagAR.Library
{
    /// <summary>
    /// One mushroom shown in the library.
    /// </summary>
    [Serializable]
    public class MushroomEntry
    {
        [Tooltip("Name shown on the library card.")]
        public string displayName;

        [Tooltip("Prefab placed in the AR world.")]
        public GameObject prefab;

        [Tooltip("Optional hand-made thumbnail. If empty, one is rendered from the prefab at runtime.")]
        public Sprite thumbnailOverride;

        [Header("Information section")]
        [Tooltip("Latin name shown under the name. Hidden if empty.")]
        public string scientificName;

        [TextArea(6, 30)]
        [Tooltip("Identifiers shown in the information section after this mushroom is placed or selected. " +
            "Supports rich text such as <b>bold</b>, <i>italic</i> and line breaks. A placeholder is shown if empty.")]
        public string identification;
    }
}
