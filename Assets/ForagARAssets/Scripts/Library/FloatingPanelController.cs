using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForagAR.Library
{
    public class FloatingPanelController : MonoBehaviour
    {
        private PlacedMushroom current;
        public GameObject characteristics;   
        public Camera cameraPlace;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            PlacedMushroom.selected += OnMushroomSelected;
        }

        // Update is called once per frame
        void Update()
        {

            if(current != null)
            {   
                characteristics.transform.position = current.gameObject.transform.position;
                characteristics.transform.LookAt(cameraPlace.transform.position);
            }
            
        }

        void OnMushroomSelected(PlacedMushroom mushroom)
        {
            current = mushroom;
        }
    }
}


