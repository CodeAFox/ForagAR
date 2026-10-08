using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForagAR.Library
{
    /// <summary>
    /// Added to every mushroom placed from the library. Remembers which catalog entry it came from,
    /// and reports when the user selects (taps or grabs) it so its information can be shown.
    /// </summary>
    public class PlacedMushroom : MonoBehaviour
    {
        /// <summary>
        /// Invoked when any placed mushroom is selected.
        /// </summary>
        public static event Action<PlacedMushroom> selected;

        XRBaseInteractable m_Interactable;

        /// <summary>
        /// The catalog entry this mushroom was placed from.
        /// </summary>
        public MushroomEntry entry { get; private set; }

        public static PlacedMushroom Attach(GameObject mushroom, MushroomEntry mushroomEntry)
        {
            var placed = mushroom.AddComponent<PlacedMushroom>();
            placed.entry = mushroomEntry;
            return placed;
        }

        void OnEnable()
        {
            if (TryGetComponent(out m_Interactable))
                m_Interactable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDisable()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            selected?.Invoke(this);
        }
    }
}
