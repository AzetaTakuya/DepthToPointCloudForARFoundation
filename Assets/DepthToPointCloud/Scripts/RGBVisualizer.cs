using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DepthToPointCloud
{
    public class RGBVisualizer : MonoBehaviour
    {
        [SerializeField] private ImagesHandler Handler;
        [SerializeField] private RawImage Image;

        private void Update()
        {
            if (!Handler.IsStarted) return;

            if(Image.texture == null)
            {
                Image.texture = Handler.RGB_Texture;
            }
        }
    }

}
