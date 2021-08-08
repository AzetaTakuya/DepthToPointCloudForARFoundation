using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AppBase
{
    public class ViewerSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject PointCloudViewer;
        [SerializeField] private GameObject RGBViewer;

        private enum ViewerType
        {
            None,
            PointCloud,
            RGB
        }
        private ViewerType Type = ViewerType.None;

        private void Start()
        {
            ChangeView();
        }

        private void Update()
        {
            if(Input.GetMouseButtonDown(0))
            {
                ChangeView();
            }
        }

        private void ChangeView()
        {
            switch(Type)
            {
                case ViewerType.None:
                    SetPointCloud();
                    break;
                case ViewerType.PointCloud:
                    SetRGB();
                    break;
                case ViewerType.RGB:
                    SetPointCloud();
                    break;
            }

            void SetPointCloud()
            {
                PointCloudViewer.SetActive(true);
                RGBViewer.SetActive(false);
                Type = ViewerType.PointCloud;
            }

            void SetRGB()
            {
                PointCloudViewer.SetActive(false);
                RGBViewer.SetActive(true);
                Type = ViewerType.RGB;
            }

        }
    }
}