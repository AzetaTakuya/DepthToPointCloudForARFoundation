using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using System.Runtime.InteropServices;

namespace DepthToPointCloud
{
    public class ImagesHandler : MonoBehaviour
    {
        [SerializeField] private ARCameraManager CameraManager;
        [SerializeField] private AROcclusionManager OcclusionManager;

        [Space]
        [SerializeField] private Material EnvDepth_Material;

        public Texture2D RGB_Texture { get; private set; }

        private RenderTexture RGB_RT;
        private RenderTexture EnvDepth_RT;
        private Texture2D HubTexture;

        public byte[] RGB_Buffer { get; private set; }
        public byte[] EnvDepth_Buffer { get; private set; }

        private readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        private readonly int MinDistanceId = Shader.PropertyToID("_MinDistance");
        public float EnvDepth_MinDistance { get; private set; }
        public float EnvDepth_MaxDistance { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool IsStarted { get; private set; }

        #region Runtime
        private void OnEnable()
        {
            CameraManager.frameReceived += OnARCameraFrameReceived;

            EnvDepth_MinDistance = 0;
            EnvDepth_MaxDistance = 5;
            EnvDepth_Material.SetFloat(MaxDistanceId, EnvDepth_MaxDistance);
            EnvDepth_Material.SetFloat(MinDistanceId, EnvDepth_MinDistance);
        }

        private void OnDisable()
        {
            CameraManager.frameReceived -= OnARCameraFrameReceived;
        }

        private void Update()
        {
            var env_Texture = OcclusionManager.environmentDepthTexture;

            if (EnvDepth_RT == null)
            {
                Width = env_Texture.width;
                Height = env_Texture.height;
                EnvDepth_RT = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                EnvDepth_RT.Create();
            }

            Graphics.Blit(env_Texture, EnvDepth_RT, EnvDepth_Material);

            #region get buffer
            if (HubTexture == null)
            {
                HubTexture = new Texture2D(Width, Height);
            }

            var currentRT = RenderTexture.active;

            if (RGB_Texture == null) return;

            RenderTexture.active = RGB_RT;
            HubTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            HubTexture.Apply();
            var colors = HubTexture.GetPixels32();
            RGB_Buffer = Color32ArrayToByteArray(colors);

            RenderTexture.active = EnvDepth_RT;
            HubTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            HubTexture.Apply();
            colors = HubTexture.GetPixels32();
            EnvDepth_Buffer = Color32ArrayToByteArray(colors);
            RenderTexture.active = currentRT;
            #endregion

            if (!IsStarted) IsStarted = true;

            byte[] Color32ArrayToByteArray(Color32[] colors)
            {
                if (colors == null || colors.Length == 0)
                    return null;

                int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
                int length = lengthOfColor32 * colors.Length;
                byte[] bytes = new byte[length];

                GCHandle handle = default(GCHandle);
                try
                {
                    handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    Marshal.Copy(ptr, bytes, 0, length);
                }
                finally
                {
                    if (handle != default(GCHandle))
                        handle.Free();
                }

                return bytes;
            }

        }
        #endregion

        #region ARCamera Callback
        unsafe void OnARCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            //Reference:https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/manual/cpu-camera-image.html

            if (!CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorY
            };

            int size = image.GetConvertedDataSize(conversionParams);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);
            image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
            image.Dispose();

            if (HubTexture == null) return;

            if (RGB_Texture == null)
            {
                RGB_Texture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);
                RGB_RT = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RGB_RT.Create();
            }
            RGB_Texture.LoadRawTextureData(buffer);
            RGB_Texture.Apply();

            Graphics.Blit(RGB_Texture, RGB_RT);
            buffer.Dispose();
        }
        #endregion
    }
}

