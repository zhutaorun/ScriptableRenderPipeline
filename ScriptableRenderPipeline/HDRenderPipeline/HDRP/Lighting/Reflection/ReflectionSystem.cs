using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.HDPipeline.Internal;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class ReflectionSystem
    {
        static ReflectionSystemInternal s_Instance = new ReflectionSystemInternal(ReflectionSystemParameters.Default, null);

        public static ReflectionSystemParameters parameters
        {
            get { return s_Instance.parameters; }
            set { s_Instance = new ReflectionSystemInternal(value, s_Instance); }
        }

        public static void QueryReflectionProbes(
            List<ReflectionProbe> results,
            bool enabled = true,
            ReflectionProbeMode mode = ReflectionProbeMode.Realtime)
        {
            s_Instance.QueryReflectionProbes(results, enabled, mode);
        }

        public static void QueryPlanarProbes(
            List<PlanarReflectionProbe> results,
            bool enabled = true,
            ReflectionProbeMode mode = ReflectionProbeMode.Realtime)
        {
            s_Instance.QueryPlanarProbes(results, enabled, mode);
        }

        public static void SetParameters(ReflectionSystemParameters parameters)
        {
            s_Instance = new ReflectionSystemInternal(parameters, s_Instance);
        }

        public static void RegisterProbe(PlanarReflectionProbe planarProbe)
        {
            s_Instance.RegisterProbe(planarProbe);
        }

        public static void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            s_Instance.UnregisterProbe(planarProbe);
        }

        public static void RequestRealtimeRender(PlanarReflectionProbe probe)
        {
            s_Instance.RequestRealtimeRender(probe);
        }

        public static void RenderAllRealtimeProbes(ReflectionProbeType probeTypes)
        {
            s_Instance.RenderAllRealtimeProbes(probeTypes);
        }

        public static void Render(PlanarReflectionProbe probe, RenderTexture target)
        {
            s_Instance.Render(probe, target);
        }

        public static void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            s_Instance.PrepareCull(camera, results);
        }

        public static void RenderAllRealtimeViewerDependentProbesFor(ReflectionProbeType probeType, Camera camera)
        {
            s_Instance.RenderAllRealtimeProbesFor(probeType, camera);
        }

        public static void CalculateCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            ReflectionSystemInternal.CalculateCaptureCameraProperties(
                probe,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCamera, out projection, out capturePosition, out captureRotation,
                viewerCamera);
        }

        public static void UnregisterProbe(ReflectionProbe reflectionProbe)
        {
            s_Instance.UnregisterProbe(reflectionProbe);
        }

        public static void RegisterProbe(ReflectionProbe reflectionProbe)
        {
            s_Instance.RegisterProbe(reflectionProbe);
        }

        public static void CalculateCaptureCameraViewProj(PlanarReflectionProbe probe, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            ReflectionSystemInternal.CalculateCaptureCameraViewProj(
                probe,
                out worldToCamera, out projection, out capturePosition, out captureRotation,
                viewerCamera);
        }
    }
}
