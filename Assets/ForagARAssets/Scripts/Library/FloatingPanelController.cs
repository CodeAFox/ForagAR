using System;
using UnityEngine;

namespace ForagAR.Library
{
    public class FloatingPanelController : MonoBehaviour
    {
        

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            PlacedMushroom.selected += OnMushroomSelected;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        void OnMushroomSelected(PlacedMushroom mushroom)
        {
            Console.WriteLine("Selected");
        }
    }
}


