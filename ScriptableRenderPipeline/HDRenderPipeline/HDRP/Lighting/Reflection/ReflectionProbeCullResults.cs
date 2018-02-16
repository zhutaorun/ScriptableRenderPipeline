using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionProbeCullResults
    {
        int[] m_PlanarReflectionProbeIndices;
        PlanarReflectionProbe[] m_VisiblePlanarReflectionProbes;
        int[] m_ReflectionProbeIndices;
        ReflectionProbe[] m_VisibleReflectionProbes;

        CullingGroup m_PlanarCullingGroup;
        CullingGroup m_ReflectionCullingGroup;
        PlanarReflectionProbe[] m_PlanarProbes;
        ReflectionProbe[] m_ReflectionProbes;

        public int visiblePlanarReflectionProbeCount { get; private set; }
        public int visibleReflectionProbeCount { get; private set; }
        public PlanarReflectionProbe[] visiblePlanarReflectionProbes { get { return m_VisiblePlanarReflectionProbes; } }
        public ReflectionProbe[] visibleReflectionProbes { get { return m_VisibleReflectionProbes; } }

        internal ReflectionProbeCullResults(ReflectionSystemParameters parameters)
        {
            Assert.IsTrue(parameters.maxPlanarReflectionProbes >= 0, "Maximum number of planar reflection probe must be positive");
            Assert.IsTrue(parameters.maxReflectionProbes >= 0, "Maximum number of reflection probe must be positive");

            visiblePlanarReflectionProbeCount = 0;
            visibleReflectionProbeCount = 0;

            m_PlanarReflectionProbeIndices = new int[parameters.maxPlanarReflectionProbes];
            m_ReflectionProbeIndices = new int[parameters.maxReflectionProbes];
            m_VisiblePlanarReflectionProbes = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbes];
            m_VisibleReflectionProbes = new ReflectionProbe[parameters.maxReflectionProbes];
        }

        public void PrepareCull(
            CullingGroup planarCullingGroup, 
            CullingGroup reflectionCullingGroup,
            PlanarReflectionProbe[] planarReflectionProbesArray,
            ReflectionProbe[] reflectionProbesArray)
        {
            Assert.IsNull(m_PlanarCullingGroup, "Culling was prepared but not used nor disposed");
            Assert.IsNull(m_PlanarProbes, "Culling was prepared but not used nor disposed");

            m_PlanarCullingGroup = planarCullingGroup;
            m_PlanarProbes = planarReflectionProbesArray;
            m_ReflectionCullingGroup = reflectionCullingGroup;
            m_ReflectionProbes = reflectionProbesArray;
        }

        public void Cull()
        {
            Assert.IsNotNull(m_PlanarCullingGroup, "Culling was not prepared, please prepare cull before performing it.");
            Assert.IsNotNull(m_ReflectionCullingGroup, "Culling was not prepared, please prepare cull before performing it.");
            Assert.IsNotNull(m_PlanarProbes, "Culling was not prepared, please prepare cull before performing it.");
            Assert.IsNotNull(m_ReflectionProbes, "Culling was not prepared, please prepare cull before performing it.");

            visiblePlanarReflectionProbeCount = m_PlanarCullingGroup.QueryIndices(true, m_PlanarReflectionProbeIndices, 0);
            for (var i = 0; i < visiblePlanarReflectionProbeCount; ++i)
                m_VisiblePlanarReflectionProbes[i] = m_PlanarProbes[m_PlanarReflectionProbeIndices[i]];

            visibleReflectionProbeCount = m_ReflectionCullingGroup.QueryIndices(true, m_ReflectionProbeIndices, 0);
            for (var i = 0; i < visibleReflectionProbeCount; ++i)
                m_VisibleReflectionProbes[i] = m_ReflectionProbes[m_ReflectionProbeIndices[i]];

            m_PlanarCullingGroup.Dispose();
            m_PlanarCullingGroup = null;
            m_PlanarProbes = null;

            m_ReflectionCullingGroup.Dispose();
            m_ReflectionCullingGroup = null;
            m_ReflectionProbes = null;
        }
    }
}
