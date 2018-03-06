using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Internal
{
    public class PlanarReflectionProbeBaker
    {
        Camera m_RenderCamera = null;
        HDAdditionalCameraData m_RenderCameraData;

        public void Render(PlanarReflectionProbe probe, RenderTexture target, Camera viewerCamera = null)
        {
            var renderCamera = GetRenderHDCamera(probe);
            renderCamera.camera.targetTexture = target;

            SetupCameraForRender(renderCamera.camera, probe, viewerCamera);
            GL.invertCulling = IsProbeCaptureMirrored(probe, viewerCamera);
            renderCamera.camera.Render();
            GL.invertCulling = false;
            renderCamera.camera.targetTexture = null;
            target.IncrementUpdateCount();
        }

        public void Render(PlanarReflectionProbe[] probes, Camera viewerCamera, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var probe = probes[i];
                Render(probe, probe.realtimeTexture, viewerCamera);
            }
        }

        public HDCamera GetRenderHDCamera(PlanarReflectionProbe probe)
        {
            var camera = GetRenderCamera();

            probe.frameSettings.CopyTo(m_RenderCameraData.GetFrameSettings());

            return HDCamera.Get(camera, null, probe.frameSettings);
        }

        public static void CalculateCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            if (viewerCamera != null
                && probe.mode == ReflectionProbeMode.Realtime
                && probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame
                && probe.capturePositionMode == PlanarReflectionProbe.CapturePositionMode.MirrorCamera)
                CalculateMirroredCaptureCameraProperties(probe, viewerCamera, out nearClipPlane, out farClipPlane, out aspect, out fov, out clearFlags, out backgroundColor, out worldToCamera, out projection, out capturePosition, out captureRotation);
            else
                CalculateStaticCaptureCameraProperties(probe, out nearClipPlane, out farClipPlane, out aspect, out fov, out clearFlags, out backgroundColor, out worldToCamera, out projection, out capturePosition, out captureRotation);
        }

        public static void CalculateCaptureCameraViewProj(PlanarReflectionProbe probe, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            float nearClipPlane, farClipPlane, aspect, fov;
            CameraClearFlags clearFlags;
            Color backgroundColor;
            CalculateCaptureCameraProperties(
                probe,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCamera, out projection, out capturePosition, out captureRotation,
                viewerCamera);
        }

        static bool IsProbeCaptureMirrored(PlanarReflectionProbe probe, Camera viewerCamera)
        {
            return viewerCamera != null
                && probe.mode == ReflectionProbeMode.Realtime
                && probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame
                && probe.capturePositionMode == PlanarReflectionProbe.CapturePositionMode.MirrorCamera;
        }

        static void CalculateStaticCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation)
        {
            nearClipPlane = probe.captureNearPlane;
            farClipPlane = probe.captureFarPlane;
            aspect = 1f;
            fov = probe.overrideFieldOfView
                ? probe.fieldOfViewOverride
                : 90f;
            clearFlags = CameraClearFlags.Nothing;
            backgroundColor = Color.white;

            capturePosition = probe.transform.TransformPoint(probe.captureLocalPosition);
            captureRotation = Quaternion.LookRotation((Vector3)probe.influenceToWorld.GetColumn(3) - capturePosition, probe.transform.up);

            worldToCamera = GeometryUtils.CalculateWorldToCameraMatrixRHS(capturePosition, captureRotation);
            var clipPlane = GeometryUtils.CameraSpacePlane(worldToCamera, probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            projection = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
            projection = GeometryUtils.CalculateObliqueMatrix(projection, clipPlane);
        }

        static void CalculateMirroredCaptureCameraProperties(PlanarReflectionProbe probe, Camera viewerCamera, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation)
        {
            nearClipPlane = viewerCamera.nearClipPlane;
            farClipPlane = viewerCamera.farClipPlane;
            aspect = 1;
            fov = probe.overrideFieldOfView
                ? probe.fieldOfViewOverride
                : Mathf.Max(viewerCamera.fieldOfView, viewerCamera.fieldOfView * viewerCamera.aspect);
            clearFlags = viewerCamera.clearFlags;
            backgroundColor = viewerCamera.backgroundColor;

            var worldToCapture = GeometryUtils.CalculateWorldToCameraMatrixRHS(viewerCamera.transform);
            var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            worldToCamera = worldToCapture * reflectionMatrix;

            var clipPlane = GeometryUtils.CameraSpacePlane(worldToCamera, probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            var sourceProj = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
            projection = GeometryUtils.CalculateObliqueMatrix(sourceProj, clipPlane);

            capturePosition = reflectionMatrix.MultiplyPoint(viewerCamera.transform.position);

            var forward = reflectionMatrix.MultiplyVector(viewerCamera.transform.forward);
            var up = reflectionMatrix.MultiplyVector(viewerCamera.transform.up);
            captureRotation = Quaternion.LookRotation(forward, up);
        }

        Camera GetRenderCamera()
        {
            if (m_RenderCamera == null)
            {
                GameObject go = null;
                for (int i = 0, c = SceneManager.sceneCount; i < c; ++i)
                {
                    go = SceneManager.GetSceneAt(i).GetRootGameObjects().FirstOrDefault(g => g.name == "__Probe Render Camera");
                    if (go != null)
                        break;
                }
                go = go ?? new GameObject("__Probe Render Camera");
                go.hideFlags = HideFlags.HideAndDontSave;

                m_RenderCamera = go.GetComponent<Camera>();
                if (m_RenderCamera == null || m_RenderCamera.Equals(null))
                    m_RenderCamera = go.AddComponent<Camera>();

                // We need to setup cameraType before adding additional camera
                m_RenderCamera.cameraType = CameraType.Reflection;

                m_RenderCameraData = go.GetComponent<HDAdditionalCameraData>();
                if (m_RenderCameraData == null || m_RenderCameraData.Equals(null))
                    m_RenderCameraData = go.AddComponent<HDAdditionalCameraData>();

                go.SetActive(false);
            }

            return m_RenderCamera;
        }

        static void SetupCameraForRender(Camera camera, PlanarReflectionProbe probe, Camera viewerCamera = null)
        {
            float nearClipPlane, farClipPlane, aspect, fov;
            Color backgroundColor;
            CameraClearFlags clearFlags;
            Vector3 capturePosition;
            Quaternion captureRotation;
            Matrix4x4 worldToCamera, projection;

            CalculateCaptureCameraProperties(probe,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCamera, out projection,
                out capturePosition, out captureRotation, viewerCamera);

            camera.farClipPlane = farClipPlane;
            camera.nearClipPlane = nearClipPlane;
            camera.fieldOfView = fov;
            camera.aspect = aspect;
            camera.clearFlags = clearFlags;
            camera.backgroundColor = camera.backgroundColor;
            camera.projectionMatrix = projection;
            camera.worldToCameraMatrix = worldToCamera;

            var ctr = camera.transform;
            ctr.position = capturePosition;
            ctr.rotation = captureRotation;
        }

        public void AllocateRealtimeTextureIfRequired(PlanarReflectionProbe[] probes, int probeResolution, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var probe = probes[i];
                if (!IsPlanarProbeRealtimeTextureValid(probe.realtimeTexture, probeResolution))
                {
                    if (probe.realtimeTexture != null)
                        probe.realtimeTexture.Release();
                    probe.realtimeTexture = NewRenderTarget(probe, probeResolution);
                }
            }
        }

        public RenderTexture NewRenderTarget(PlanarReflectionProbe probe, int probeResolution)
        {
            var rt = new RenderTexture(probeResolution, probeResolution, 0, RenderTextureFormat.ARGBHalf);
            // No hide and don't save for this one
            rt.useMipMap = true;
            rt.autoGenerateMips = false;
            rt.name = CoreUtils.GetRenderTargetAutoName(probeResolution, probeResolution, RenderTextureFormat.ARGBHalf, "PlanarProbeRT");
            rt.Create();
            return rt;
        }

        static bool IsPlanarProbeRealtimeTextureValid(RenderTexture renderTexture, int probeResolution)
        {
            return renderTexture != null
                && renderTexture.width == probeResolution
                && renderTexture.height == probeResolution
                && renderTexture.format == RenderTextureFormat.ARGBHalf
                && renderTexture.useMipMap;
        }
    }
}
