namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public sealed partial class HDAdditionalReflectionData : HDProbe
    {
        protected override void Awake()
        {
            base.Awake();
            type = ProbeSettings.ProbeType.ReflectionProbe;
            k_Migration.Migrate(this);
        }
    }
}
