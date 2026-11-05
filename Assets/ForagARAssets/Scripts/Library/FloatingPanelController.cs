using System;
using TMPro;
using Unity.Tutorials.Editor;
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
        public TMP_Text quick_characteristics;
        public Toggle enable;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            PlacedMushroom.selected += OnMushroomSelected;
            characteristics.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if(enable.isOn)
            {
                characteristics.SetActive(true);
            }
            else
            {
                characteristics.SetActive(false);
            }

            if(current != null)
            {   
                characteristics.transform.position = new Vector3(current.gameObject.transform.position.x, current.gameObject.transform.position.y + 0.5f, current.gameObject.transform.position.z);
                characteristics.transform.LookAt(cameraPlace.transform.position);
            }
            
        }

        void OnMushroomSelected(PlacedMushroom mushroom)
        {
            current = mushroom;
            if(mushroom.entry.quick_characteristics.IsNotNullOrEmpty())
            {
                characteristics.SetActive(true);
                quick_characteristics.SetText(mushroom.entry.quick_characteristics);
            }
        }
    }
}


