using System;
using UnityEditor;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
using XRSettings = UnityEngine.XR.XRSettings;
#elif UNITY_5_6_OR_NEWER
using UnityEngine.VR;
using XRSettings = UnityEngine.VR.VRSettings;
#endif

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public class XRGraphics
    { // XRGraphics insulates SRP from API changes across platforms, Editor versions, and as XR transitions into XR SDK

        public enum StereoRenderingMode
        {
            None,
            MultiPass,
            SinglePassDoubleWide,
            SinglePassInstanced,
            SinglePassMultiView
        };

        public static float eyeTextureResolutionScale
        {
            get
            {
                if (!enabled)
                    return 1.0f;
                else
                    return XRSettings.eyeTextureResolutionScale;
            }
        }

        public static float renderViewportScale
        {
            get
            {
                if (!enabled)
                    return 1.0f;
                else
                    return XRSettings.renderViewportScale;
            }
        }

        public static bool useOcclusionMesh
        {
            get
            {
                if (!enabled)
                    return false;
                else
                    return XRSettings.useOcclusionMesh;
            }
        }
        
#if UNITY_EDITOR
        public static bool tryEnable
        { // TryEnable gets updated before "play" is pressed- we use this for updating GUI only. 
            get { return PlayerSettings.virtualRealitySupported; }
        }
#endif

        public static bool enabled
        { // SRP should use this to safely determine whether XR is enabled at runtime.
            get
            {
#if ENABLE_VR
                return XRSettings.enabled;
#else
                return false;
#endif
            }
        }

        public static StereoRenderingMode stereoRenderingMode
        {
            get
            {
                if (!enabled)
                    return StereoRenderingMode.None;
#if UNITY_2018_3_OR_NEWER
                XRSettings.StereoRenderingMode stereoRenderMode = XRSettings.stereoRenderingMode;
                switch (stereoRenderMode)
                {
                    case XRSettings.StereoRenderingMode.MultiPass:
                        return StereoRenderingMode.MultiPass;
                    case XRSettings.StereoRenderingMode.SinglePass:
                        return StereoRenderingMode.SinglePassDoubleWide;
                    case XRSettings.StereoRenderingMode.SinglePassInstanced:
                        return StereoRenderingMode.SinglePassInstanced;
                    case XRSettings.StereoRenderingMode.SinglePassMultiview:
                        return StereoRenderingMode.SinglePassMultiView;
                    default:
                        return StereoRenderingMode.None;
                }
#else // Reverse engineer it
                if (!enabled)
                    return StereoRenderingMode.None;
                if (eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes)
                {
                    if (eyeTextureDesc.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
                        return StereoRenderingMode.SinglePassInstanced;
                    return StereoRenderingMode.SinglePassDoubleWide;
                }
                else
                    return StereoRenderingMode.MultiPass;
#endif
            }
        }
        public static uint GetPixelOffset(uint eye)
        {
            if (!enabled || stereoRenderingMode != StereoRenderingMode.SinglePassDoubleWide)
                return 0;
            return (uint)(Mathf.CeilToInt((eye * XRSettings.eyeTextureWidth) / 2));
        }

        public static RenderTextureDescriptor eyeTextureDesc
        {
            get
            {
                if (!enabled)
                {
                    return new RenderTextureDescriptor(0, 0);
                }

                return XRSettings.eyeTextureDesc;
            }
        }

        public static int eyeTextureWidth
        {
            get
            {
                if (!enabled)
                {
                    return 0;
                }

                return XRSettings.eyeTextureWidth;
            }
        }
        public static int eyeTextureHeight
        {
            get
            {
                if (!enabled)
                {
                    return 0;
                }

                return XRSettings.eyeTextureHeight;
            }
        }
    }
}
