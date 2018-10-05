namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public partial class HDAdditionalReflectionData
    {
        // We use the legacy ReflectionProbe for the culling system
        // So we need to update its influence (center, size) so the culling behave properly

        ReflectionProbe m_LegacyProbe;
        /// <summary>Get the sibling component ReflectionProbe</summary>
        ReflectionProbe reflectionProbe
        {
            get
            {
                if(m_LegacyProbe == null || m_LegacyProbe.Equals(null))
                {
                    m_LegacyProbe = GetComponent<ReflectionProbe>();
                }
                return m_LegacyProbe;
            }
        }

        public override void PrepareCulling()
        {
            base.PrepareCulling();
            var influence = settings.influence;
            var cubeProbe = reflectionProbe;
            switch (influence.shape)
            {
                case InfluenceShape.Box:
                    cubeProbe.size = influence.boxSize;
                    cubeProbe.center = transform.rotation * influence.offset;
                    break;
                case InfluenceShape.Sphere:
                    cubeProbe.size = Vector3.one * (2 * influence.sphereRadius);
                    cubeProbe.center = transform.rotation * influence.offset;
                    break;
            }
        }
    }
}
