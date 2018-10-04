using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public partial class HDAdditionalReflectionData : HDProbe
    {
        [SerializeField]
        public struct RenderData
        {
            public Matrix4x4 worldToCameraRHS;

            public Vector3 capturePosition
            {
                get
                {
                    var v = worldToCameraRHS.GetColumn(3);
                    return new Vector3(v.x, v.y, -v.z);
                }
            }
        }

        [SerializeField]
        RenderData m_BakedRenderData;
        [SerializeField]
        RenderData m_CustomRenderData;
        RenderData m_RealtimeRenderData;

        public RenderData bakedRenderData
        { get => m_BakedRenderData; internal set => m_BakedRenderData = value; }
        public RenderData customRenderData
        { get => m_CustomRenderData; internal set => m_CustomRenderData = value; }
        public RenderData realtimeRenderData
        { get => m_RealtimeRenderData; internal set => m_RealtimeRenderData = value;  }
        public RenderData renderData => GetRenderData(mode);
        public RenderData GetRenderData(ProbeSettings.Mode targetMode)
        {
            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return bakedRenderData;
                case ProbeSettings.Mode.Custom: return customRenderData;
                case ProbeSettings.Mode.Realtime: return realtimeRenderData;
                default: throw new ArgumentOutOfRangeException();
            }
        }

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
