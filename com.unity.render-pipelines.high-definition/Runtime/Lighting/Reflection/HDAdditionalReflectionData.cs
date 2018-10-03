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

        public void CopyTo(HDAdditionalReflectionData data)
        {
            influenceVolume.CopyTo(data.influenceVolume);
            data.influenceVolume.shape = influenceVolume.shape; //force the legacy probe to refresh its size

            data.mode = mode;
            data.multiplier = multiplier;
            data.weight = weight;
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
