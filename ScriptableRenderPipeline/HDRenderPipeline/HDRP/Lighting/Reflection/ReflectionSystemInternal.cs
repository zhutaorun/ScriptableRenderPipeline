using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Internal
{
    class ReflectionSystemInternal
    {
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbes;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_DirtyBounds;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_RequestRealtimeRender;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_RealtimeUpdate;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_PerCamera_RealtimeUpdate;
        PlanarReflectionProbe[] m_PlanarReflectionProbe_RealtimeUpdate_WorkArray;
            
        Dictionary<PlanarReflectionProbe, BoundingSphere> m_PlanarReflectionProbeBounds;
        PlanarReflectionProbe[] m_PlanarReflectionProbesArray;
        BoundingSphere[] m_PlanarReflectionProbeBoundsArray;

        HashSet<ReflectionProbe> m_ReflectionProbes;
        HashSet<ReflectionProbe> m_ReflectionProbe_DirtyBounds;
        HashSet<ReflectionProbe> m_ReflectionProbe_RequestRealtimeRender;
        HashSet<ReflectionProbe> m_ReflectionProbe_RealtimeUpdate;
        ReflectionProbe[] m_ReflectionProbes_RealtimeUpdate_WorkArray;
        Dictionary<ReflectionProbe, BoundingSphere> m_ReflectionProbeBounds;
        ReflectionProbe[] m_ReflectionProbesArray;
        BoundingSphere[] m_ReflectionProbeBoundsArray;

        ReflectionSystemParameters m_Parameters;
        PlanarReflectionProbeBaker m_PlanarReflectionProbeBaker = new PlanarReflectionProbeBaker();
        ReflectionProbeBaker m_ReflectionProbeBaker = new ReflectionProbeBaker();

        public ReflectionSystemParameters parameters { get { return m_Parameters; } }

        public ReflectionSystemInternal(ReflectionSystemParameters parameters, ReflectionSystemInternal previous)
        {
            m_Parameters = parameters;

            // Planar probes
            // Runtime collections
            m_PlanarReflectionProbeBounds = new Dictionary<PlanarReflectionProbe, BoundingSphere>(parameters.maxPlanarReflectionProbes);
            m_PlanarReflectionProbesArray = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbes];
            m_PlanarReflectionProbeBoundsArray = new BoundingSphere[parameters.maxPlanarReflectionProbes];
            m_PlanarReflectionProbe_RealtimeUpdate_WorkArray = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbes];
            // Persistent collections
            m_PlanarReflectionProbes = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_DirtyBounds = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RequestRealtimeRender = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RealtimeUpdate = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_PerCamera_RealtimeUpdate = new HashSet<PlanarReflectionProbe>();

            // Reflection probes
            // Runtime collections
            m_ReflectionProbes_RealtimeUpdate_WorkArray = new ReflectionProbe[parameters.maxReflectionProbes];
            m_ReflectionProbeBounds = new Dictionary<ReflectionProbe, BoundingSphere>();
            m_ReflectionProbesArray = new ReflectionProbe[parameters.maxReflectionProbes];
            m_ReflectionProbeBoundsArray = new BoundingSphere[parameters.maxReflectionProbes];
            // Persistent collections
            m_ReflectionProbes = new HashSet<ReflectionProbe>();
            m_ReflectionProbe_RequestRealtimeRender = new HashSet<ReflectionProbe>();
            m_ReflectionProbe_RealtimeUpdate = new HashSet<ReflectionProbe>();
            m_ReflectionProbe_DirtyBounds = new HashSet<ReflectionProbe>();

            if (previous != null)
            {
                // Planar probes
                m_PlanarReflectionProbes.UnionWith(previous.m_PlanarReflectionProbes);
                m_PlanarReflectionProbe_DirtyBounds.UnionWith(m_PlanarReflectionProbes);
                m_PlanarReflectionProbe_RequestRealtimeRender.UnionWith(previous.m_PlanarReflectionProbe_RequestRealtimeRender);
                m_PlanarReflectionProbe_RealtimeUpdate.UnionWith(previous.m_PlanarReflectionProbe_RealtimeUpdate);
                m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.UnionWith(previous.m_PlanarReflectionProbe_PerCamera_RealtimeUpdate);

                // Reflection probes
                m_ReflectionProbes.UnionWith(previous.m_ReflectionProbes);
                m_ReflectionProbe_DirtyBounds.UnionWith(m_ReflectionProbes);
                m_ReflectionProbe_RequestRealtimeRender.UnionWith(previous.m_ReflectionProbe_RequestRealtimeRender);
                m_ReflectionProbe_RealtimeUpdate.UnionWith(previous.m_ReflectionProbe_RealtimeUpdate);
            }
        }
        public void RenderAllRealtimeProbesFor(ReflectionProbeType probeType, Camera viewerCamera)
        {
            if ((probeType & ReflectionProbeType.PlanarReflection) != 0)
                RenderlAllPlanarRealtimeProbesFor(viewerCamera);
        }

        public void RenderAllRealtimeProbes(ReflectionProbeType probeTypes)
        {
            if ((probeTypes & ReflectionProbeType.PlanarReflection) != 0)
                RenderAllPlanarRealtimeProbes();
            if ((probeTypes & ReflectionProbeType.ReflectionProbe) != 0)
                RenderAllReflectionRealtimeProbes();
        }

        #region Planar Probes
        public void RegisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Add(planarProbe);
            SetPlanarProbeBoundsDirty(planarProbe);

            if (planarProbe.mode == ReflectionProbeMode.Realtime)
            {
                switch (planarProbe.refreshMode)
                {
                    case ReflectionProbeRefreshMode.OnAwake:
                        m_PlanarReflectionProbe_RequestRealtimeRender.Add(planarProbe);
                        break;
                    case ReflectionProbeRefreshMode.EveryFrame:
                    {
                        switch (planarProbe.capturePositionMode)
                        {
                            case PlanarReflectionProbe.CapturePositionMode.Static:
                                m_PlanarReflectionProbe_RealtimeUpdate.Add(planarProbe);
                                break;
                            case PlanarReflectionProbe.CapturePositionMode.MirrorCamera:
                                m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Add(planarProbe);
                                break;
                        }
                        break;
                    }
                }
            }
        }

        public void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Remove(planarProbe);
            m_PlanarReflectionProbeBounds.Remove(planarProbe);
            m_PlanarReflectionProbe_DirtyBounds.Remove(planarProbe);
            m_PlanarReflectionProbe_RequestRealtimeRender.Remove(planarProbe);
            m_PlanarReflectionProbe_RealtimeUpdate.Remove(planarProbe);
            m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Remove(planarProbe);
        }

        public void RequestRealtimeRender(PlanarReflectionProbe probe)
        {
            m_PlanarReflectionProbe_RequestRealtimeRender.Add(probe);
        }

        public void Render(PlanarReflectionProbe probe, RenderTexture target, Camera viewerCamera = null)
        {
            m_PlanarReflectionProbeBaker.Render(probe, target, viewerCamera);
        }

        void RenderlAllPlanarRealtimeProbesFor(Camera viewerCamera)
        {
            var length = m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Count;
            m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.CopyTo(m_PlanarReflectionProbe_RealtimeUpdate_WorkArray);

            m_PlanarReflectionProbeBaker.AllocateRealtimeTextureIfRequired(
                m_PlanarReflectionProbe_RealtimeUpdate_WorkArray,
                m_Parameters.planarReflectionProbeSize,
                length
            );
            m_PlanarReflectionProbeBaker.Render(
                m_PlanarReflectionProbe_RealtimeUpdate_WorkArray,
                viewerCamera,
                length
            );
        }

        void RenderAllPlanarRealtimeProbes()
        {
            // Discard disabled probes in requested render probes
            m_PlanarReflectionProbe_RequestRealtimeRender.IntersectWith(m_PlanarReflectionProbes);

            // Include all realtime probe modes
            m_PlanarReflectionProbe_RequestRealtimeRender.UnionWith(m_PlanarReflectionProbe_RealtimeUpdate);
            m_PlanarReflectionProbe_RequestRealtimeRender.CopyTo(m_PlanarReflectionProbe_RealtimeUpdate_WorkArray);
            var length = m_PlanarReflectionProbe_RequestRealtimeRender.Count;
            m_PlanarReflectionProbe_RequestRealtimeRender.Clear();

            m_PlanarReflectionProbeBaker.AllocateRealtimeTextureIfRequired(
                m_PlanarReflectionProbe_RealtimeUpdate_WorkArray,
                m_Parameters.planarReflectionProbeSize,
                length
            );
            m_PlanarReflectionProbeBaker.Render(
                m_PlanarReflectionProbe_RealtimeUpdate_WorkArray,
                null,
                length);
        }

        void SetPlanarProbeBoundsDirty(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbe_DirtyBounds.Add(planarProbe);
        }

        void UpdateAllPlanarReflectionProbeBounds()
        {
            if (m_PlanarReflectionProbe_DirtyBounds.Count > 0)
            {
                m_PlanarReflectionProbe_DirtyBounds.IntersectWith(m_PlanarReflectionProbes);
                foreach (var planarReflectionProbe in m_PlanarReflectionProbe_DirtyBounds)
                    UpdatePlanarReflectionProbeBounds(planarReflectionProbe);

                m_PlanarReflectionProbeBounds.Values.CopyTo(m_PlanarReflectionProbeBoundsArray, 0);
                m_PlanarReflectionProbeBounds.Keys.CopyTo(m_PlanarReflectionProbesArray, 0);
            }
        }

        void UpdatePlanarReflectionProbeBounds(PlanarReflectionProbe planarReflectionProbe)
        {
            m_PlanarReflectionProbeBounds[planarReflectionProbe] = planarReflectionProbe.boundingSphere;
        }

        public static void CalculateCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera)
        {
            PlanarReflectionProbeBaker.CalculateCaptureCameraProperties(
                probe, 
                out nearClipPlane, out farClipPlane, 
                out aspect, out fov, 
                out clearFlags, out backgroundColor,
                out worldToCamera, out projection, 
                out capturePosition, out captureRotation, 
                viewerCamera);
        }

        public static void CalculateCaptureCameraViewProj(PlanarReflectionProbe probe, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera)
        {
            PlanarReflectionProbeBaker.CalculateCaptureCameraViewProj(
                probe,
                out worldToCamera, out projection,
                out capturePosition, out captureRotation,
                viewerCamera);
        }
        #endregion

        #region Reflection probes
        internal void UnregisterProbe(ReflectionProbe reflectionProbe)
        {
            m_ReflectionProbes.Remove(reflectionProbe);
        }

        internal void RegisterProbe(ReflectionProbe reflectionProbe)
        {
            m_ReflectionProbes.Add(reflectionProbe);
            SetReflectionProbeBoundsDirty(reflectionProbe);

            if (reflectionProbe.mode == ReflectionProbeMode.Realtime)
            {
                switch (reflectionProbe.refreshMode)
                {
                    case ReflectionProbeRefreshMode.OnAwake:
                        m_ReflectionProbe_RequestRealtimeRender.Add(reflectionProbe);
                        break;
                    case ReflectionProbeRefreshMode.EveryFrame:
                        m_ReflectionProbe_RealtimeUpdate.Add(reflectionProbe);
                        break;
                }
            }
        }

        void SetReflectionProbeBoundsDirty(ReflectionProbe reflectionProbe)
        {
            m_ReflectionProbe_DirtyBounds.Add(reflectionProbe);
        }

        void UpdateAllReflectionProbeBounds()
        {
            if (m_ReflectionProbe_DirtyBounds.Count > 0)
            {
                m_ReflectionProbe_DirtyBounds.IntersectWith(m_ReflectionProbes);
                foreach (var reflectionProbe in m_ReflectionProbe_DirtyBounds)
                    UpdateReflectionProbeBounds(reflectionProbe);

                m_ReflectionProbeBounds.Values.CopyTo(m_ReflectionProbeBoundsArray, 0);
                m_ReflectionProbeBounds.Keys.CopyTo(m_ReflectionProbesArray, 0);
            }
        }

        void UpdateReflectionProbeBounds(ReflectionProbe reflectionProbe)
        {
            m_ReflectionProbeBounds[reflectionProbe] = reflectionProbe.GetComponent<HDAdditionalReflectionData>().boundingSphere;
        }

        void RenderAllReflectionRealtimeProbes()
        {
            // Discard disabled probes in requested render probes
            m_ReflectionProbe_RequestRealtimeRender.IntersectWith(m_ReflectionProbes);

            // Include all realtime probe modes
            m_ReflectionProbe_RequestRealtimeRender.UnionWith(m_ReflectionProbe_RealtimeUpdate);
            m_ReflectionProbe_RequestRealtimeRender.CopyTo(m_ReflectionProbes_RealtimeUpdate_WorkArray);
            var length = m_ReflectionProbe_RequestRealtimeRender.Count;
            m_ReflectionProbe_RequestRealtimeRender.Clear();

            m_ReflectionProbeBaker.AllocateRealtimeTextureIfRequired(
                m_ReflectionProbes_RealtimeUpdate_WorkArray,
                m_Parameters.reflectionProbeSize,
                length
            );
            m_ReflectionProbeBaker.Render(
                m_ReflectionProbes_RealtimeUpdate_WorkArray,
                length);
        }
        #endregion

        #region Culling
        public void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            UpdateAllPlanarReflectionProbeBounds();
            UpdateAllReflectionProbeBounds();

            var planarCullingGroup = new CullingGroup();
            planarCullingGroup.targetCamera = camera;
            planarCullingGroup.SetBoundingSpheres(m_PlanarReflectionProbeBoundsArray);
            planarCullingGroup.SetBoundingSphereCount(m_PlanarReflectionProbeBounds.Count);

            var reflectionCullingGroup = new CullingGroup();
            reflectionCullingGroup.targetCamera = camera;
            reflectionCullingGroup.SetBoundingSpheres(m_ReflectionProbeBoundsArray);
            reflectionCullingGroup.SetBoundingSphereCount(m_ReflectionProbeBounds.Count);

            results.PrepareCull(
                planarCullingGroup, 
                reflectionCullingGroup, 
                m_PlanarReflectionProbesArray, 
                m_ReflectionProbesArray);
        }
        #endregion
    }
}
