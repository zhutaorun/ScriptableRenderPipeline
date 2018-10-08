using System;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteAlways]
    public partial class PlanarReflectionProbe : HDProbe, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct RenderData
        {
            public Matrix4x4 worldToCameraRHS;
            public Matrix4x4 projectionMatrix;
        }

        // Serialized data
        [SerializeField]
        Vector3 m_LocalReferencePosition = -Vector3.forward;
        [SerializeField]
        RenderData m_BakedRenderData;
        [SerializeField]
        RenderData m_CustomRenderData;
        RenderData m_RealtimeRenderData;

        public RenderData bakedRenderData { get => m_BakedRenderData; internal set => m_BakedRenderData = value; }
        public RenderData customRenderData { get => m_CustomRenderData; internal set => m_CustomRenderData = value; }
        public RenderData realtimeRenderData { get => m_RealtimeRenderData; internal set => m_RealtimeRenderData = value; }
        public RenderData renderData => GetRenderData(mode);
        public RenderData GetRenderData(ProbeSettings.Mode targetMode)
        {
            switch (mode)
            {
                case ProbeSettings.Mode.Baked: return bakedRenderData;
                case ProbeSettings.Mode.Custom: return customRenderData;
                case ProbeSettings.Mode.Realtime: return realtimeRenderData;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>Reference position to mirror to find the capture point. (local space)</summary>
        public Vector3 localReferencePosition => m_LocalReferencePosition;
        /// <summary>Reference position to mirror to find the capture point. (world space)</summary>
        public Vector3 referencePosition => transform.TransformPoint(m_LocalReferencePosition);

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            Assert.IsNotNull(influenceVolume, "influenceVolume must have an instance at this point. See HDProbe.Awake()");
            // Keep this for a migration that has been done on HDRP/staging
        }

        internal override void Awake()
        {
            base.Awake();
            k_Migration.Migrate(this);
        }

        protected override void PopulateSettings(ref ProbeSettings settings)
        {
            base.PopulateSettings(ref settings);

            ComputeTransformRelativeToInfluence(
                out settings.proxySettings.mirrorPositionProxySpace,
                out settings.proxySettings.mirrorRotationProxySpace
            );
        }
    }
}
