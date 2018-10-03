namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public partial class HDAdditionalReflectionData : HDProbe
    {
        protected override void PopulateSettings(ref ProbeSettings settings)
        {
            base.PopulateSettings(ref settings);

            ComputeTransformRelativeToInfluence(
                out settings.proxySettings.capturePositionProxySpace,
                out settings.proxySettings.captureRotationProxySpace
            );
        }
        
        internal override void Awake()
        {
            base.Awake();

            //launch migration at creation too as m_Version could not have an
            //existance in older version
            k_MigrationDescription.Migrate(this);
        }
    }
}
