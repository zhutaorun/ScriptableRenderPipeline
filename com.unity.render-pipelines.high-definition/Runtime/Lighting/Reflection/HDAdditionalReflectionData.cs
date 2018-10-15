using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public sealed partial class HDAdditionalReflectionData : HDProbe
    {
        protected override void PopulateSettings(ref ProbeSettings settings)
        {
            base.PopulateSettings(ref settings);

            ComputeTransformRelativeToInfluence(
                out settings.proxySettings.capturePositionProxySpace,
                out settings.proxySettings.captureRotationProxySpace
            );
        }

        protected override void Awake()
        {
            base.Awake();
            type = ProbeSettings.ProbeType.ReflectionProbe;
            k_Migration.Migrate(this);
        }
    }
}
