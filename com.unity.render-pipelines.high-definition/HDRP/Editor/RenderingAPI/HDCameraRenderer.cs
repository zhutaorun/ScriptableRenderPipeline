using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
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
            public float fieldOfview;

            public Matrix4x4 projectionMatrix;
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

    [Serializable]
    public struct CameraRenderSettings
    {
        public struct Position
        {
            public enum Mode
            {
                ComputeWorldToCameraMatrix,
                UseWorldToCameraMatrixField
            }

            public Mode mode;

            public Vector3 position;
            public Quaternion rotation;

            public Matrix4x4 worldToCameraMatrix;
        }

        public CameraSettings camera;
        public Position position;
    }

    public struct HDCameraRenderer
    {
        public void Render(CameraRenderSettings settings, Texture target)
        {
            // Argument checking
            if (target == null)
                throw new ArgumentNullException("target");
            // Assert for target.dimension
            if (settings.camera.frameSettings == null)
                throw new ArgumentNullException("settings");

            var rtTarget = target as RenderTexture;
            var cubeTarget = target as Cubemap;
            switch (target.dimension)
            {
                case TextureDimension.Tex2D:
                    if (rtTarget == null)
                        throw new ArgumentException("'target' must be a RenderTexture when rendering into a 2D texture");
                    break;
                case TextureDimension.Cube:
                    break;
                default:
                    throw new ArgumentException(string.Format("Rendering into a target of dimension " +
                        "{0} is not supported", target.dimension));
            }


            var camera = NewRenderingCamera();
            try
            {
                camera.ApplySettings(settings);

                switch (target.dimension)
                {
                    case TextureDimension.Tex2D:
                        {
                            Assert.IsNotNull(rtTarget);
                            camera.targetTexture = rtTarget;
                            camera.Render();
                            camera.targetTexture = null;
                            break;
                        }
                    case TextureDimension.Cube:
                        {
                            if (rtTarget != null)
                                camera.RenderToCubemap(rtTarget);
                            if (cubeTarget != null)
                                camera.RenderToCubemap(cubeTarget);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                CoreUtils.Destroy(camera.gameObject);
            }
        }

        static Camera NewRenderingCamera()
        {
            var go = new GameObject("__Render Camera");
            var camera = go.AddComponent<Camera>();
            go.AddComponent<HDAdditionalCameraData>();

            return camera;
        }
    }

    public static class CameraSettingsUtilities
    {
        public static void ApplySettings(this Camera cam, CameraSettings settings)
        {
            if (settings.frameSettings == null)
                throw new InvalidOperationException("'frameSettings' must not be null.");

            var add = cam.GetComponent<HDAdditionalCameraData>()
                ?? cam.gameObject.AddComponent<HDAdditionalCameraData>();

            settings.frameSettings.CopyTo(add.GetFrameSettings());
            // Frustum
            switch (settings.frustum.mode)
            {
                case CameraSettings.Frustum.Mode.UseProjectionMatrixField:
                    cam.projectionMatrix = settings.frustum.projectionMatrix;
                    break;
                case CameraSettings.Frustum.Mode.ComputeProjectionMatrix:
                    cam.ResetProjectionMatrix();
                    cam.nearClipPlane = settings.frustum.nearClipPlane;
                    cam.farClipPlane = settings.frustum.farClipPlane;
                    cam.fieldOfView = settings.frustum.fieldOfview;
                    break;
            }
            // Culling
            cam.useOcclusionCulling = settings.culling.useOcclusionCulling;
            cam.cullingMask = settings.culling.cullingMask;
            // Buffer clearing
            add.clearColorMode = settings.bufferClearing.clearColorMode;
            add.backgroundColorHDR = settings.bufferClearing.backgroundColorHDR;
            add.clearDepth = settings.bufferClearing.clearDepth;
            // Volumes
            add.volumeLayerMask = settings.volumes.volumeLayerMask;
            add.volumeAnchorOverride = settings.volumes.volumeAnchorOverride;
            // Physical Parameters
            add.aperture = settings.physical.aperture;
            add.shutterSpeed = settings.physical.shutterSpeed;
            add.iso = settings.physical.iso;
            // HD Specific
            add.renderingPath = settings.renderingPath;

            add.OnAfterDeserialize();
        }

        public static void ApplySettings(this Camera cam, CameraRenderSettings settings)
        {
            // Position
            switch (settings.position.mode)
            {
                case CameraRenderSettings.Position.Mode.UseWorldToCameraMatrixField:
                    cam.worldToCameraMatrix = settings.position.worldToCameraMatrix;
                    break;
                case CameraRenderSettings.Position.Mode.ComputeWorldToCameraMatrix:
                    cam.ResetWorldToCameraMatrix();
                    cam.transform.position = settings.position.position;
                    cam.transform.rotation = settings.position.rotation;
                    break;
            }
            cam.ApplySettings(settings.camera);
        }
    }
}
