using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDProbeRenderer
    {
        struct ReflectionProbeRenderer
        {
            public bool Render(HDReflectionProbe probe, Texture target)
            {
                var cubemapTarget = target as Cubemap;
                var rtTarget = target as RenderTexture;
                if (cubemapTarget == null
                    && (rtTarget == null || rtTarget.dimension != UnityEngine.Rendering.TextureDimension.Cube))
                {
                    Debug.LogWarningFormat("Trying to render a reflection probe in an invalid target: {0}", target);
                    return false;
                }

                var camera = NewCamera(probe.assets.captureFrameSettings, probe.assets.postProcessLayer);

                SetupCaptureCamera(camera, probe.captureSettings, rtTarget);

                if (cubemapTarget != null)
                    camera.RenderToCubemap(cubemapTarget);
                else if (rtTarget != null)
                    camera.RenderToCubemap(rtTarget);

                CoreUtils.Destroy(camera.gameObject);

                return true;
            }

            void SetupCaptureCamera(
                Camera camera,
                HDReflectionProbe.ProbeCaptureProperties capture,
                RenderTexture target
            )
            {
                camera.transform.position = capture.common.position;
                camera.farClipPlane = capture.common.farClipPlane;
                camera.nearClipPlane = capture.common.nearClipPlane;
                camera.fieldOfView = capture.common.fieldOfview;
                camera.clearFlags = capture.common.clearFlags;
                camera.backgroundColor = capture.common.backgroundColor;
                camera.aspect = target.width / (float)target.height;
            }
        }

        ReflectionProbeRenderer m_ReflectionProbeRenderer = new ReflectionProbeRenderer();

        public bool Render(HDProbe2 probe, Texture target)
        {
            var standard = probe as HDReflectionProbe;
            var planar = probe as HDPlanarProbe;
            if (standard != null)
                return m_ReflectionProbeRenderer.Render(standard, target);
            if (planar != null)
                throw new NotImplementedException();
            return false;
        }

        static Camera NewCamera(FrameSettings frameSettings, PostProcessLayer postLayer)
        {
            var go = new GameObject("__Probe Camera");
            var camera = go.AddComponent<Camera>();
            var add = go.AddComponent<HDAdditionalCameraData>();

            frameSettings.CopyTo(add.GetFrameSettings());

            if (postLayer != null)
            {
                // TODO: copy post process settings when initializing the capture camera.
                //var layer = go.AddComponent<PostProcessLayer>();
                //EditorUtility.CopySerialized(postLayer, layer);
            }

            return camera;
        }
    }
}
