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

        public static void RenderAndUpdateRealtimeRenderData(
            IEnumerable<HDProbe> probes,
            Transform viewerTransform
        )
        {
            foreach (var probe in probes)
                RenderAndUpdateRealtimeRenderData(probe, viewerTransform);
        }

        public static void RenderAndUpdateRealtimeRenderData(
            HDProbe probe, Transform viewerTransform
        )
        {
            var target = CreateAndSetRenderTargetIfRequired(probe, ProbeSettings.Mode.Realtime);
            Render(probe, viewerTransform, target, out HDProbe.RenderData renderData);
            AssignRenderData(probe, renderData, ProbeSettings.Mode.Realtime);
        }

        public static void Render(
            HDProbe probe, Transform viewerTransform,
            Texture outTarget, out HDProbe.RenderData outRenderData,
            bool forceFlipY = false
        )
        {
            var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, viewerTransform);
            HDRenderUtilities.Render(
                probe.settings,
                positionSettings,
                outTarget,
                out CameraSettings cameraSettings, out CameraPositionSettings cameraPosition,
                forceFlipY: forceFlipY
            );

            outRenderData = new HDProbe.RenderData(cameraSettings, cameraPosition);
        }

        public static void AssignRenderData(
            HDProbe probe,
            HDProbe.RenderData renderData,
            ProbeSettings.Mode targetMode
        )
        {
            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: probe.bakedRenderData = renderData; break;
                case ProbeSettings.Mode.Custom: probe.customRenderData = renderData; break;
                case ProbeSettings.Mode.Realtime: probe.realtimeRenderData = renderData; break;
            }
        }

        public static void PrepareCull(Camera camera, ReflectionProbeCullResults results)
            => s_Instance.PrepareCull(camera, results);

        public static Texture CreateRenderTargetForMode(HDProbe probe, ProbeSettings.Mode targetMode)
        {
            Texture target = null;
            var hd = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            var settings = probe.settings;
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

            return target;
        }

        static Texture CreateAndSetRenderTargetIfRequired(HDProbe probe, ProbeSettings.Mode targetMode)
        {
            var settings = probe.settings;
            Texture target = probe.GetTexture(targetMode);

            if (target != null)
                return target;

            target = CreateRenderTargetForMode(probe, targetMode);

            probe.SetTexture(targetMode, target);
            return target;
        }
    }

    class HDProbeSystemInternal
    {
        List<HDProbe> m_BakedProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewDependentProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewIndependentProbes = new List<HDProbe>();
        int m_PlanarProbeCount = 0;
        PlanarReflectionProbe[] m_PlanarProbes = new PlanarReflectionProbe[32];
        BoundingSphere[] m_PlanarProbeBounds = new BoundingSphere[32];

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
                    switch (settings.type)
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

            switch (settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                    {
                        // Grow the arrays
                        if (m_PlanarProbeCount == m_PlanarProbes.Length)
                        {
                            Array.Resize(ref m_PlanarProbes, m_PlanarProbes.Length * 2);
                            Array.Resize(ref m_PlanarProbeBounds, m_PlanarProbeBounds.Length * 2);
                        }
                        m_PlanarProbes[m_PlanarProbeCount] = (PlanarReflectionProbe)probe;
                        m_PlanarProbeBounds[m_PlanarProbeCount] = ((PlanarReflectionProbe)probe).boundingSphere;
                        ++m_PlanarProbeCount;
                        break;
                    }
            }
        }

        internal void UnregisterProbe(HDProbe probe)
        {
            m_BakedProbes.Remove(probe);
            m_RealtimeViewDependentProbes.Remove(probe);
            m_RealtimeViewIndependentProbes.Remove(probe);

            // Remove swap back
            var index = Array.IndexOf(m_PlanarProbes, probe);
            if (index != -1)
            {
                if (index < m_PlanarProbeCount)
                {
                    m_PlanarProbes[index] = m_PlanarProbes[m_PlanarProbeCount - 1];
                    m_PlanarProbeBounds[index] = m_PlanarProbeBounds[m_PlanarProbeCount - 1];
                }
                --m_PlanarProbeCount;
            }
        }

        internal void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            var cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = camera;
            cullingGroup.SetBoundingSpheres(m_PlanarProbeBounds);
            cullingGroup.SetBoundingSphereCount(m_PlanarProbeCount);

            results.PrepareCull(cullingGroup, m_PlanarProbes);
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
