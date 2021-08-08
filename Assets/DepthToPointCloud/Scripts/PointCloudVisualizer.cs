using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DepthToPointCloud
{
    public class PointCloudVisualizer : MonoBehaviour
    {
        [SerializeField] private ImagesHandler Handler;
        [SerializeField] private ParticleSystem ParticleSystem;
        [Space][SerializeField] private Transform ARCamera;

        private ParticleSystem.Particle[] Particles;
        private readonly float ParticleSize = 0.025f;

        private void Awake()
        {
            GameObject particleObj = ParticleSystem.gameObject;
            particleObj.transform.SetParent(ARCamera);
            particleObj.transform.localPosition = Vector3.forward;
        }

        private void Update()
        {
            if (!Handler.IsStarted) return;

            // get info
            int width = Handler.Width;
            int height = Handler.Height;
            int pixelNum = width * height;

            float minDistance = Handler.EnvDepth_MinDistance;
            float maxDistance = Handler.EnvDepth_MaxDistance;

            var rgb_Buffer = Handler.RGB_Buffer;
            var env_Buffer = Handler.EnvDepth_Buffer;

            var env_Colors = new Color32[env_Buffer.Length / 4];
            var env_Distances = new float[env_Buffer.Length / 4];

            CalcDistance();

            VisualizeParticle();

            void CalcDistance()
            {
                for (int i = 0; i < env_Buffer.Length / 4; i++)
                {
                    int index = i * 4;
                    env_Colors[i].r = env_Buffer[index + 0];
                    env_Colors[i].g = env_Buffer[index + 1];
                    env_Colors[i].b = env_Buffer[index + 2];
                    env_Colors[i].a = env_Buffer[index + 3];

                    Color.RGBToHSV(env_Colors[i], out float H, out float S, out float V);
                    float distance = 0;
                    if (H > 0.7f && H < 0.85f)
                    {
                        distance = 0;
                    }
                    else if (H >= 0 && H <= 0.7f)
                    {
                        distance = (0.7f - H) * (maxDistance - minDistance) / (0.7f + 0.15f);
                    }
                    else if (H >= 0.85f && H <= 1)
                    {
                        distance = (1.7f - H) * (maxDistance - minDistance) / (0.7f + 0.15f);
                    }
                    env_Distances[i] = distance;
                }
            }

            void VisualizeParticle()
            {
                Array.Resize(ref Particles, pixelNum);
                int index = 0;
                for(int y = 0; y < height; y++)
                {
                    for(int x = 0; x < width; x++)
                    {
                        float distance = env_Distances[index];

                        //color
                        var _x = width - x - 1;
                        int colorIndex = y * width + _x; //x軸
                        var r = rgb_Buffer[colorIndex * 4 + 0];
                        var g = rgb_Buffer[colorIndex * 4 + 1];
                        var b = rgb_Buffer[colorIndex * 4 + 2];
                        var a = rgb_Buffer[colorIndex * 4 + 3];

                        var particle = Particles[index];
                        particle.position = new Vector3(x * 0.01f, y * -0.01f, distance);
                        var color = new Color32(r, g, b, a);
                        particle.startColor = color;
                        particle.startSize = ParticleSize;
                        Particles[index] = particle;

                        index++;
                    }
                }

                float posX = - width * 0.01f / 2;
                float posY = height * 0.01f / 2;
                float posZ = ParticleSystem.gameObject.transform.localPosition.z;
                ParticleSystem.gameObject.transform.localPosition = new Vector3(posX, posY, posZ);

                ParticleSystem.SetParticles(Particles, Particles.Length);
            }
        }
    }
}
