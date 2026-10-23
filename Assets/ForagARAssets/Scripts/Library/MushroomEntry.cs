using System;
using UnityEngine;
using UnityEngine.Serialization;

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

        // Each text below fills one tab of the information section. They support rich text such as
        // <b>bold</b>, <i>italic</i> and line breaks. A placeholder is shown for an empty one.

        [TextArea(6, 30)]
        [Tooltip("Description tab: how to identify this mushroom.")]
        [FormerlySerializedAs("identification")]
        public string description;

        [TextArea(6, 30)]
        [Tooltip("Habitat tab: where and when this mushroom grows.")]
        public string habitat;

        [TextArea(6, 30)]
        [Tooltip("Uses tab: edibility, cooking, medicinal or other uses.")]
        public string uses;

        [TextArea(6, 30)]
        [Tooltip("Look-Alikes tab: which mushrooms this one can be confused with and how to tell them apart.")]
        public string lookAlikes;
    }
}
