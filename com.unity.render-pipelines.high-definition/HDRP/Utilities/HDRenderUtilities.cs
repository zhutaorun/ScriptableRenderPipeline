using System;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>API to perform a rendering with HDRP.</summary>
    /// <example>
    /// How to perform standard rendering:
    /// <code>
    /// class StandardRenderingExample
    /// {
    ///     public void Render()
    ///     {
    ///         // Copy default settings
    ///         var settings = CameraRenderSettings.Default;
    ///         // Adapt default settings to our custom usage
    ///         settings.position.position = new Vector3(0, 1, 0);
    ///         settings.camera.frustum.fieldOfView = 60.0f;
    ///         // Get our render target
    ///         var rt = new RenderTexture(128, 128, 1, GraphicsFormat.B8G8R8A8_SNorm);
    ///         HDRenderUtilities.Render(settings, rt);
    ///         // Do something with rt
    ///         rt.Release();
    ///     }
    /// }
    /// </code>
    ///
    /// How to perform a cubemap rendering:
    /// <code>
    /// class CubemapRenderExample
    /// {
    ///     public void Render()
    ///     {
    ///         // Copy default settings
    ///         var settings = CameraRenderSettings.Default;
    ///         // Adapt default settings to our custom usage
    ///         settings.position.position = new Vector3(0, 1, 0);
    ///         settings.camera.physical.iso = 800.0f;
    ///         // Frustum settings are ignored and driven by the cubemap rendering
    ///         // Get our render target
    ///         var rt = new RenderTexture(128, 128, 1, GraphicsFormat.B8G8R8A8_SNorm)
    ///         {
    ///             dimension = TextureDimension.Cube
    ///         };
    ///         // The TextureDimension is detected and the renderer will perform a cubemap rendering.
    ///         HDRenderUtilities.Render(settings, rt);
    ///         // Do something with rt
    ///         rt.Release();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class HDRenderUtilities
    {
        public void Render(CameraSettings settings, CameraPositionSettings position, Texture target)
        {
            // Argument checking
            if (target == null)
                throw new ArgumentNullException("target");
            // Assert for frame settings
            if (settings.frameSettings == null)
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
                camera.ApplySettings(position);

                GL.invertCulling = settings.culling.invertCulling;
                switch (target.dimension)
                {
                    case TextureDimension.Tex2D:
                        {
                            Assert.IsNotNull(rtTarget);
                            camera.targetTexture = rtTarget;
                            camera.Render();
                            camera.targetTexture = null;
                            target.IncrementUpdateCount();
                            break;
                        }
                    case TextureDimension.Cube:
                        {
                            Assert.IsTrue(rtTarget != null || cubeTarget != null);
                            if (rtTarget != null)
                                camera.RenderToCubemap(rtTarget);
                            if (cubeTarget != null)
                                camera.RenderToCubemap(cubeTarget);
                            target.IncrementUpdateCount();
                            break;
                        }
                }
                GL.invertCulling = false;
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
}
