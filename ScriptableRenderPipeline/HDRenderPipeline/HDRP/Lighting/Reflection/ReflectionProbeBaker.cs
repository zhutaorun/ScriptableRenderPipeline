using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Internal
{
    public class ReflectionProbeBaker
    {
        Camera m_RenderCamera = null;
        HDAdditionalCameraData m_RenderCameraData;

        public void Render(ReflectionProbe probe, RenderTexture target)
        {
            var renderCamera = GetRenderHDCamera(probe);

            SetupCameraForRender(renderCamera.camera, probe);
            renderCamera.camera.RenderToCubemap(target, -1);
            target.IncrementUpdateCount();
        }

        public void Render(ReflectionProbe[] probes, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var probe = probes[i];
                Render(probe, probe.realtimeTexture);
            }
        }

        public HDCamera GetRenderHDCamera(ReflectionProbe probe)
        {
            var camera = GetRenderCamera();

            var hdCameData = ComponentSingleton<HDAdditionalCameraData>.instance;
            var frameSettings = hdCameData.GetFrameSettings();
            frameSettings.CopyTo(m_RenderCameraData.GetFrameSettings());

            return HDCamera.Get(camera, null, frameSettings);
        }

        Camera GetRenderCamera()
        {
            if (m_RenderCamera == null)
            {
                GameObject go = null;
                for (int i = 0, c = SceneManager.sceneCount; i < c; ++i)
                {
                    go = SceneManager.GetSceneAt(i).GetRootGameObjects().FirstOrDefault(g => g.name == "__Reflection Probe Render Camera");
                    if (go != null)
                        break;
                }
                go = go ?? new GameObject("__Reflection Probe Render Camera");
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

        static void SetupCameraForRender(Camera camera, ReflectionProbe probe)
        {
            var ptr = probe.transform;
            var ctr = camera.transform;
            ctr.position = ptr.position;
            ctr.rotation = ptr.rotation;
        }

        public void AllocateRealtimeTextureIfRequired(ReflectionProbe[] probes, int probeResolution, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var probe = probes[i];
                if (!IsProbeRealtimeTextureValid(probe.realtimeTexture, probeResolution))
                {
                    if (probe.realtimeTexture != null)
                        probe.realtimeTexture.Release();
                    probe.realtimeTexture = NewRenderTarget(probe, probeResolution);
                }
            }
        }

        public RenderTexture NewRenderTarget(ReflectionProbe probe, int probeResolution)
        {
            var rt = new RenderTexture(probeResolution, probeResolution, 0, RenderTextureFormat.ARGBHalf);
            // No hide and don't save for this one
            rt.useMipMap = true;
            rt.autoGenerateMips = false;
            rt.dimension = TextureDimension.Cube;
            rt.name = CoreUtils.GetRenderTargetAutoName(probeResolution, probeResolution, RenderTextureFormat.ARGBHalf, "PlanarProbeRT");
            rt.Create();
            return rt;
        }

        static bool IsProbeRealtimeTextureValid(RenderTexture renderTexture, int probeResolution)
        {
            return renderTexture != null
                && renderTexture.width == probeResolution
                && renderTexture.height == probeResolution
                && renderTexture.format == RenderTextureFormat.ARGBHalf
                && renderTexture.dimension == TextureDimension.Cube
                && renderTexture.useMipMap;
        }
    }
}
