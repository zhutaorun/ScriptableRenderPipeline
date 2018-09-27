using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDProbeSystem
    {
        static HDProbeSystemInternal s_Instance = new HDProbeSystemInternal();

        public static IList<HDProbe> realtimeViewDependentProbes { get { return s_Instance.realtimeViewDependentProbes; } }
        public static IList<HDProbe> realtimeViewIndependentProbes { get { return s_Instance.realtimeViewIndependentProbes; } }
        public static IList<HDProbe> bakedProbes { get { return s_Instance.bakedProbes; } }

        public static void RegisterProbe(HDProbe probe)
        {
            s_Instance.RegisterProbe(probe);
        }

        public static void UnregisterProbe(HDProbe probe)
        {
            s_Instance.UnregisterProbe(probe);
        }

        public static void RenderAndUpdateRealtimeData(
            IEnumerable<HDProbe> probes,
            Transform viewerTransform
        )
        {
            foreach (var probe in probes)
                RenderAndUpdateRealtimeData(probe, viewerTransform);
        }

        public static void RenderAndUpdateRealtimeData(HDProbe probe, Transform viewerTransform)
        {
            CreateAndSetRealtimeRenderTargetIfRequired(probe);
            var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, viewerTransform);
            Matrix4x4 projectionMatrix, worldToCameraRHSMatrix;
            HDRenderUtilities.Render(
                probe.settings,
                positionSettings,
                probe.realtimeTexture,
                out worldToCameraRHSMatrix, out projectionMatrix
            );

            if (probe.settings.type == ProbeSettings.ProbeType.PlanarProbe)
            {
                var planar = (PlanarReflectionProbe)probe;
                planar.realtimeRenderData = new PlanarReflectionProbe.RenderData
                {
                    projectionMatrix = projectionMatrix,
                    worldToCameraRHS = worldToCameraRHSMatrix
                };
            }
        }

        static void CreateAndSetRealtimeRenderTargetIfRequired(HDProbe probe)
        {
            if (probe.realtimeTexture != null)
                return;

            var hd = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            switch (probe.settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                    probe.realtimeTexture = HDRenderUtilities.CreatePlanarProbeTarget(
                        (int)hd.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize
                    );
                    break;
                case ProbeSettings.ProbeType.ReflectionProbe:
                    probe.realtimeTexture = HDRenderUtilities.CreateReflectionProbeTarget(
                        (int)hd.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize
                    );
                    break;
            }
        }
    }

    class HDProbeSystemInternal
    {
        List<HDProbe> m_BakedProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewDependentProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewIndependentProbes = new List<HDProbe>();

        public IList<HDProbe> bakedProbes
        { get { RemoveDestroyedProbes(m_BakedProbes); return m_BakedProbes; } }
        public IList<HDProbe> realtimeViewDependentProbes
        { get { RemoveDestroyedProbes(m_RealtimeViewDependentProbes); return m_RealtimeViewDependentProbes; } }
        public IList<HDProbe> realtimeViewIndependentProbes
        { get { RemoveDestroyedProbes(m_RealtimeViewIndependentProbes); return m_RealtimeViewIndependentProbes; } }

        internal void RegisterProbe(HDProbe probe)
        {
            var settings = probe.settings;
            switch (settings.mode)
            {
                case ProbeSettings.Mode.Baked:
                    m_BakedProbes.Add(probe);
                    break;
                case ProbeSettings.Mode.Realtime:
                    switch (probe.settings.type)
                    {
                        case ProbeSettings.ProbeType.PlanarProbe:
                            m_RealtimeViewDependentProbes.Add(probe);
                            break;
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            m_RealtimeViewIndependentProbes.Add(probe);
                            break;
                    }
                    break;
            }
        }

        internal void UnregisterProbe(HDProbe probe)
        {
            m_BakedProbes.Remove(probe);
        }

        void RemoveDestroyedProbes(List<HDProbe> probes)
        {
            for (int i = probes.Count - 1; i >= 0; --i)
            {
                if (probes[i] == null || probes[i].Equals(null))
                    probes.RemoveAt(i);
            }
        }
    }
}
