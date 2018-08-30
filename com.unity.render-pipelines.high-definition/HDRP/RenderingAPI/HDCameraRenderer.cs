using System;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct HDCameraRenderer
    {
        public void Render(CameraRenderSettings settings, Texture target)
        {
            // Argument checking
            if (target == null)
                throw new ArgumentNullException("target");
            // Assert for frame settings
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
