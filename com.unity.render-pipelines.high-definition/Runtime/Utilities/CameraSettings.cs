using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum CameraSettingsFields
    {
        none = 0,
        physicalAperture = 1 << 0,
        physicalShutterSpeed = 1 << 1,
        physicalIso = 1 << 2,
        bufferClearColorMode = 1 << 3,
        bufferClearBackgroundColorHDR = 1 << 4,
        bufferClearClearDepth = 1 << 5,
        volumesLayerMask = 1 << 6,
        volumesAnchorOverride = 1 << 7,
        frustumMode = 1 << 8,
        frustumAspect = 1 << 9,
        frustumFarClipPlane = 1 << 10,
        frustumNearClipPlane = 1 << 11,
        frustumFieldOfView = 1 << 12,
        frustumProjectionMatrix = 1 << 13,
        cullingUseOcclusionCulling = 1 << 14,
        cullingCullingMask = 1 << 15,
        cullingInvertCulling = 1 << 16,
        renderingPath = 1 << 17,
        flipYMode = 1 << 18,
    }

    [Serializable]
    public struct CameraSettingsOverride
    {
        public CameraSettingsFields camera;
    }

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
                backgroundColorHDR = new Color32(6, 18, 48, 0),
                clearDepth = true
            };

            /// <summary>Define the source for the clear color.</summary>
            public HDAdditionalCameraData.ClearColorMode clearColorMode;
            /// <summary>
            /// The color to use when
            /// <c><see cref="clearColorMode"/> == <see cref="HDAdditionalCameraData.ClearColorMode.BackgroundColor"/></c>.
            /// </summary>
            [ColorUsage(true, true)]
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
                layerMask = -1,
                anchorOverride = null
            };

            /// <summary>The <see cref="LayerMask"/> to use for the volumes.</summary>
            public LayerMask layerMask;
            /// <summary>If not null, define the location of the evaluation of the volume framework.</summary>
            public Transform anchorOverride;
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
            [Range(1, 179.0f)]
            public float fieldOfView;

            /// <summary>Projection matrix used for <see cref="Mode.UseProjectionMatrixField"/> mode.</summary>
            public Matrix4x4 projectionMatrix;

            /// <summary>Compute the projection matrix based on the mode and settings provided.</summary>
            /// <returns>The projection matrix.</returns>
            public Matrix4x4 ComputeProjectionMatrix()
            {
                return Matrix4x4.Perspective(fieldOfView, aspect, nearClipPlane, farClipPlane);
            }

            public Matrix4x4 GetUsedProjectionMatrix()
            {
                switch (mode)
                {
                    case Mode.ComputeProjectionMatrix: return ComputeProjectionMatrix();
                    case Mode.UseProjectionMatrixField: return projectionMatrix;
                    default: throw new ArgumentException();
                }
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
            renderingPath = HDAdditionalCameraData.RenderingPath.UseGraphicsSettings,
            volumes = Volumes.@default,
            flipYMode = HDAdditionalCameraData.FlipYMode.Automatic
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
        public HDAdditionalCameraData.FlipYMode flipYMode;
    }
}
