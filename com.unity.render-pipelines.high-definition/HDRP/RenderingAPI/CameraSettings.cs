using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct CameraSettings
    {
        public struct Physical
        {
            public float aperture;
            public float shutterSpeed;
            public float iso;
        }

        public struct BufferClearing
        {
            public HDAdditionalCameraData.ClearColorMode clearColorMode;
            public Color backgroundColorHDR;
            public bool clearDepth;
        }

        public struct Volumes
        {
            public LayerMask volumeLayerMask;
            public Transform volumeAnchorOverride;
        }

        public struct Frustum
        {
            public enum Mode
            {
                ComputeProjectionMatrix,
                UseProjectionMatrixField
            }

            public Mode mode;
            public float aspect;
            public float farClipPlane;
            public float nearClipPlane;
            public float fieldOfView;

            public Matrix4x4 projectionMatrix;

            public Matrix4x4 ComputeProjectionMatrix()
            {
                return Matrix4x4.Perspective(fieldOfView, aspect, nearClipPlane, farClipPlane);
            }
        }

        public struct Culling
        {
            public bool useOcclusionCulling;
            public int cullingMask;
        }

        public HDAdditionalCameraData.RenderingPath renderingPath;
        public FrameSettings frameSettings;
        public PostProcessLayer postProcessLayer;
        public Physical physical;
        public BufferClearing bufferClearing;
        public Volumes volumes;
        public Frustum frustum;
        public Culling culling;
    }
}
