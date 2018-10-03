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

        internal override void UpdatedInfluenceVolumeShape(Vector3 size, Vector3 offset)
        {
            reflectionProbe.size = size;
            reflectionProbe.center = transform.rotation * offset;
        }
    }
}
