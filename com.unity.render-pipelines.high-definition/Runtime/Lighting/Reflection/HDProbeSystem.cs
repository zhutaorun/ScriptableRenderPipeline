using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDProbeSystem
    {
        static HDProbeSystemInternal s_Instance = new HDProbeSystemInternal();

        public static IList<HDProbe> realtimeViewDependentProbes => s_Instance.realtimeViewDependentProbes;
        public static IList<HDProbe> realtimeViewIndependentProbes => s_Instance.realtimeViewIndependentProbes;
        public static IList<HDProbe> bakedProbes => s_Instance.bakedProbes;

        public static void RegisterProbe(HDProbe probe) => s_Instance.RegisterProbe(probe);
        public static void UnregisterProbe(HDProbe probe) => s_Instance.UnregisterProbe(probe);

        public static void RenderAndUpdateRenderData(
            IEnumerable<HDProbe> probes,
            Transform viewerTransform,
            ProbeSettings.Mode targetMode
        )
        {
            foreach (var probe in probes)
                RenderAndUpdateRenderData(probe, viewerTransform, targetMode);
        }

        public static void RenderAndUpdateRenderData(
            HDProbe probe, Transform viewerTransform, ProbeSettings.Mode targetMode
        )
        {
            var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, viewerTransform);
            var target = CreateAndSetRenderTargetIfRequired(probe, targetMode);
            Matrix4x4 projectionMatrix, worldToCameraRHSMatrix;
            HDRenderUtilities.Render(
                probe.settings,
                positionSettings,
                target,
                out worldToCameraRHSMatrix, out projectionMatrix
            );

            var renderData = new PlanarReflectionProbe.RenderData
            {
                projectionMatrix = projectionMatrix,
                worldToCameraRHS = worldToCameraRHSMatrix
            };
            AssignRenderData(probe, renderData, targetMode);
        }

        public static void AssignRenderData(
            HDProbe probe,
            PlanarReflectionProbe.RenderData renderData,
            ProbeSettings.Mode targetMode
        )
        {
            var planarProbe = probe as PlanarReflectionProbe;
            if (planarProbe == null)
                return;

            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: planarProbe.bakedRenderData = renderData; break;
                case ProbeSettings.Mode.Custom: planarProbe.customRenderData = renderData; break;
                case ProbeSettings.Mode.Realtime: planarProbe.realtimeRenderData = renderData; break;
            }
        }

        public static void PrepareCull(Camera camera, ReflectionProbeCullResults results)
            => s_Instance.PrepareCull(camera, results);

        static Texture CreateAndSetRenderTargetIfRequired(HDProbe probe, ProbeSettings.Mode targetMode)
        {
            var settings = probe.settings;
            Texture target = probe.GetTexture(targetMode);

            if (target != null)
                return target;

            var hd = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            switch (targetMode)
            {
                case ProbeSettings.Mode.Realtime:
                    {
                        switch (settings.type)
                        {
                            case ProbeSettings.ProbeType.PlanarProbe:
                                target = HDRenderUtilities.CreatePlanarProbeRenderTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize
                                );
                                break;
                            case ProbeSettings.ProbeType.ReflectionProbe:
                                target = HDRenderUtilities.CreateReflectionProbeRenderTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize
                                );
                                break;
                        }
                        break;
                    }
                case ProbeSettings.Mode.Baked:
                case ProbeSettings.Mode.Custom:
                    {
                        switch (settings.type)
                        {
                            case ProbeSettings.ProbeType.PlanarProbe:
                                target = HDRenderUtilities.CreatePlanarProbeRenderTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize
                                );
                                break;
                            case ProbeSettings.ProbeType.ReflectionProbe:
                                target = HDRenderUtilities.CreateReflectionProbeTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize
                                );
                                break;
                        }
                        break;
                    }
            }

            probe.SetTexture(targetMode, target);
            return target;
        }
    }

    class HDProbeSystemInternal
    {
        List<HDProbe> m_BakedProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewDependentProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewIndependentProbes = new List<HDProbe>();
        int m_RealtimePlanarProbeCount = 0;
        PlanarReflectionProbe[] m_RealtimePlanarProbes = new PlanarReflectionProbe[32];
        BoundingSphere[] m_RealtimePlanarProbeBounds = new BoundingSphere[32];

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
                            // Grow the arrays
                            if (m_RealtimePlanarProbeCount == m_RealtimePlanarProbes.Length)
                            {
                                Array.Resize(ref m_RealtimePlanarProbes, m_RealtimePlanarProbes.Length * 2);
                                Array.Resize(ref m_RealtimePlanarProbeBounds, m_RealtimePlanarProbeBounds.Length * 2);
                            }
                            m_RealtimePlanarProbes[m_RealtimePlanarProbeCount] = (PlanarReflectionProbe)probe;
                            m_RealtimePlanarProbeBounds[m_RealtimePlanarProbeCount] = ((PlanarReflectionProbe)probe).boundingSphere;
                            ++m_RealtimePlanarProbeCount;
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

            // Remove swap back
            var index = Array.IndexOf(m_RealtimePlanarProbes, probe);
            if (index != -1)
            {
                if (index < m_RealtimePlanarProbeCount)
                {
                    m_RealtimePlanarProbes[index] = m_RealtimePlanarProbes[m_RealtimePlanarProbeCount - 1];
                    m_RealtimePlanarProbeBounds[index] = m_RealtimePlanarProbeBounds[m_RealtimePlanarProbeCount - 1];
                }
                --m_RealtimePlanarProbeCount;
            }
        }

        internal void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            var cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = camera;
            cullingGroup.SetBoundingSpheres(m_RealtimePlanarProbeBounds);
            cullingGroup.SetBoundingSphereCount(m_RealtimePlanarProbeCount);

            results.PrepareCull(cullingGroup, m_RealtimePlanarProbes);
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
