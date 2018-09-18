using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Contains all settings required to setup a camera in HDRP.</summary>
    [Serializable]
    public struct CameraSettings
    {
        /// <summary>Physical camera settings, this will impact exposure of the rendered image.</summary>
        [Serializable]
        public struct Physical
        {
            /// <summary>Default value.</summary>
            public static readonly Physical @default = new Physical
            {
                aperture = 8,
                iso = 400,
                shutterSpeed = 1.0f / 200
            };

            /// <summary>Aperture size of the camera.</summary>
            public float aperture;
            /// <summary>Shutter speed of the camera.</summary>
            public float shutterSpeed;
            /// <summary>ISO of the camera.</summary>
            public float iso;
        }

        /// <summary>Defines how color and depth buffers are cleared.</summary>
        [Serializable]
        public struct BufferClearing
        {
            /// <summary>Default value.</summary>
            public static readonly BufferClearing @default = new BufferClearing
            {
                clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky,
                backgroundColorHDR = new Color(0.025f, 0.07f, 0.19f, 0.0f),
                clearDepth = true
            };

            /// <summary>Define the source for the clear color.</summary>
            public HDAdditionalCameraData.ClearColorMode clearColorMode;
            /// <summary>
            /// The color to use when
            /// <c><see cref="clearColorMode"/> == <see cref="HDAdditionalCameraData.ClearColorMode.BackgroundColor"/></c>.
            /// </summary>
            public Color backgroundColorHDR;
            /// <summary>True to clear the depth.</summary>
            public bool clearDepth;
        }

        /// <summary>Defines how the volume framework is queried.</summary>
        [Serializable]
        public struct Volumes
        {
            /// <summary>Default value.</summary>
            public static readonly Volumes @default = new Volumes
            {
                volumeLayerMask = -1,
                volumeAnchorOverride = null
            };

            /// <summary>The <see cref="LayerMask"/> to use for the volumes.</summary>
            public LayerMask volumeLayerMask;
            /// <summary>If not null, define the location of the evaluation of the volume framework.</summary>
            public Transform volumeAnchorOverride;
        }


        /// <summary>Defines the projection matrix of the camera.</summary>
        [Serializable]
        public struct Frustum
        {
            /// <summary>Default value.</summary>
            public static readonly Frustum @default = new Frustum
            {
                mode = Mode.ComputeProjectionMatrix,
                aspect = 1.0f,
                farClipPlane = 1000.0f,
                nearClipPlane = 0.1f,
                fieldOfView = 90.0f,
                projectionMatrix = Matrix4x4.identity
            };

            /// <summary>Defines how the projection matrix is computed.</summary>
            public enum Mode
            {
                /// <summary>
                /// For perspective projection, the matrix is computed from <see cref="aspect"/>,
                /// <see cref="farClipPlane"/>, <see cref="nearClipPlane"/> and <see cref="fieldOfView"/> parameters.
                ///
                /// Orthographic projection is not currently supported.
                /// </summary>
                ComputeProjectionMatrix,
                /// <summary>The projection matrix provided is assigned.</summary>
                UseProjectionMatrixField
            }

            /// <summary>Which mode will be used for the projection matrix.</summary>
            public Mode mode;
            /// <summary>Aspect ratio of the frustum (width/height).</summary>
            public float aspect;
            /// <summary>Far clip plane distance.</summary>
            public float farClipPlane;
            /// <summary>Near clip plane distance.</summary>
            public float nearClipPlane;
            /// <summary>Field of view for perspective matrix (for y axis, in degree).</summary>
            public float fieldOfView;

            /// <summary>Projection matrix used for <see cref="Mode.UseProjectionMatrixField"/> mode.</summary>
            public Matrix4x4 projectionMatrix;

            /// <summary>Compute the projection matrix based on the mode and settings provided.</summary>
            /// <returns>The projection matrix.</returns>
            public Matrix4x4 ComputeProjectionMatrix()
            {
                return Matrix4x4.Perspective(fieldOfView, aspect, nearClipPlane, farClipPlane);
            }
        }

        /// <summary>Defines the culling settings of the camera.</summary>
        [Serializable]
        public struct Culling
        {
            /// <summary>Default value.</summary>
            public static readonly Culling @default = new Culling
            {
                cullingMask = -1,
                useOcclusionCulling = true,
                invertCulling = false
            };

            /// <summary>True when occlusion culling will be performed during rendering, false otherwise.</summary>
            public bool useOcclusionCulling;
            /// <summary>The mask for visible objects.</summary>
            public int cullingMask;
            /// <summary>True to invert face culling, false otherwise.</summary>
            public bool invertCulling;
        }

        /// <summary>Default value.</summary>
        public static readonly CameraSettings @default = new CameraSettings
        {
            bufferClearing = BufferClearing.@default,
            culling = Culling.@default,
            frameSettings = new FrameSettings(),
            frustum = Frustum.@default,
            physical = Physical.@default,
            postProcessLayer = null,
            renderingPath = HDAdditionalCameraData.RenderingPath.Default,
            volumes = Volumes.@default
        };

        /// <summary>Rendering path to use.</summary>
        public HDAdditionalCameraData.RenderingPath renderingPath;
        /// <summary>Frame settings to use.</summary>
        public FrameSettings frameSettings;
        /// <summary>Post process layer to use.</summary>
        public PostProcessLayer postProcessLayer;
        /// <summary>Physical settings to use.</summary>
        public Physical physical;
        /// <summary>Buffer clearing settings to use.</summary>
        public BufferClearing bufferClearing;
        /// <summary>Volumes settings to use.</summary>
        public Volumes volumes;
        /// <summary>Frustum settings to use.</summary>
        public Frustum frustum;
        /// <summary>Culling settings to use.</summary>
        public Culling culling;
    }
}
