using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    struct HDProbeRenderer
    {
        struct CommonRenderer
        {
            public static void SetupCaptureCameraSettings(
                Camera camera,
                HDProbe.CaptureProperties captureProperties
            )
            {
                camera.farClipPlane = captureProperties.cameraSettings.farClipPlane;
                camera.nearClipPlane = captureProperties.cameraSettings.nearClipPlane;

                var add = camera.GetComponent<HDAdditionalCameraData>();
                add.Import(captureProperties.cameraSettings);
            }

            public static void SetupCaptureCamera(
                Camera camera,
                HDProbe probe,
                RenderTexture target,
                Transform viewer
            )
            {
                camera.aspect = target.width / (float)target.height;
                SetupCaptureCameraSettings(camera, probe.captureProperties);
            }
        }

        interface IProbeRenderer<T>
            where T : HDProbe
        {
            bool Render(T probe, Texture target, Transform viewer);
        }

        struct ReflectionProbeRenderer : IProbeRenderer<HDAdditionalReflectionData>
        {
            public bool Render(HDAdditionalReflectionData probe, Texture target, Transform viewer)
            {
                var cubemapTarget = target as Cubemap;
                var rtTarget = target as RenderTexture;
                if (cubemapTarget == null
                    && (rtTarget == null || rtTarget.dimension != UnityEngine.Rendering.TextureDimension.Cube))
                {
                    Debug.LogWarningFormat("Trying to render a reflection probe in an invalid target: {0}", target);
                    return false;
                }

                var camera = NewCamera(
                    probe.captureProperties.cameraSettings.frameSettings,
                    probe.captureProperties.cameraSettings.postProcessLayer
                );
                try
                {
                    CommonRenderer.SetupCaptureCamera(camera, probe, rtTarget, viewer);
                    camera.fieldOfView = 90;
                    SetupCaptureCameraTransform(
                        camera, probe,
                        viewer != null ? viewer.position : camera.transform.position,
                        viewer != null ? viewer.rotation : camera.transform.rotation
                    );

                    var renderingSuccess = false;
                    if (cubemapTarget != null)
                        renderingSuccess = camera.RenderToCubemap(cubemapTarget);
                    else if (rtTarget != null)
                        renderingSuccess = camera.RenderToCubemap(rtTarget);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    CoreUtils.Destroy(camera.gameObject);
                }

                return true;
            }

            static void SetupCaptureCameraTransform(
                Camera camera,
                HDProbe probe,
                Vector3 viewerPosition, Quaternion viewerRotation
            )
            {
                Vector3 position; Quaternion rotation;
                probe.GetCaptureTransformFor(
                    viewerPosition, viewerRotation,
                    out position, out rotation
                );
                camera.transform.position = position;
                camera.transform.rotation = rotation;
            }
        }

        struct PlanarProbeRenderer : IProbeRenderer<PlanarReflectionProbe>
        {
            public bool Render(PlanarReflectionProbe probe, Texture target, Transform viewer)
            {
                var rtTarget = target as RenderTexture;
                if ((rtTarget == null || rtTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex2D))
                {
                    Debug.LogWarningFormat("Trying to render a reflection probe in an invalid target: {0}", target);
                    return false;
                }

                var camera = NewCamera(
                    probe.captureProperties.cameraSettings.frameSettings,
                    probe.captureProperties.cameraSettings.postProcessLayer
                );
                try
                {
                    CommonRenderer.SetupCaptureCamera(camera, probe, rtTarget, viewer);
                    camera.fieldOfView = probe.captureProperties.cameraSettings.fieldOfview;
                    SetupCaptureCameraMatrices(camera, probe, viewer);

                    camera.targetTexture = rtTarget;
                    camera.Render();
                    camera.targetTexture = null;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    CoreUtils.Destroy(camera.gameObject);
                }

                return true;
            }

            static void SetupCaptureCameraMatrices(Camera camera, PlanarReflectionProbe probe, Transform viewer)
            {
                var referencePosition = probe.probeCaptureProperties.localReferencePosition;
                var referenceRotation = Quaternion.LookRotation(probe.transform.position - referencePosition);

                if (probe.probeCaptureProperties.capturePositionMode
                    == PlanarReflectionProbe.CapturePositionMode.MirrorViewer
                    && viewer != null)
                {
                    referencePosition = viewer.position;
                    referenceRotation = viewer.rotation;
                }

                var mirrorPlanePosition = probe.transform.position;
                var mirrorPlaneNormal = probe.transform.forward;

                var worldToCapture = GeometryUtils.CalculateWorldToCameraMatrixRHS(referencePosition, referenceRotation);
                var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(
                    new Plane(mirrorPlaneNormal, mirrorPlanePosition)
                );
                var worldToCamera = worldToCapture * reflectionMatrix;

                var clipPlane = GeometryUtils.CameraSpacePlane(worldToCamera, mirrorPlanePosition, mirrorPlaneNormal);
                var sourceProj = Matrix4x4.Perspective(
                    camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane
                );
                var projection = GeometryUtils.CalculateObliqueMatrix(sourceProj, clipPlane);

                var capturePosition = reflectionMatrix.MultiplyPoint(referencePosition);

                var forward = reflectionMatrix.MultiplyVector(referenceRotation * Vector3.forward);
                var up = reflectionMatrix.MultiplyVector(referenceRotation * Vector3.up);
                var captureRotation = Quaternion.LookRotation(forward, up);

                camera.transform.position = capturePosition;
                camera.transform.rotation = captureRotation;
            }
        }

        ReflectionProbeRenderer m_ReflectionProbeRenderer;
        PlanarProbeRenderer m_PlanarProbeRenderer;

        public bool Render(HDProbe probe, Texture target, Transform viewer)
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
                return m_ReflectionProbeRenderer.Render(standard, target, viewer);
            if (planar != null)
                return m_PlanarProbeRenderer.Render(planar, target, viewer);
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
