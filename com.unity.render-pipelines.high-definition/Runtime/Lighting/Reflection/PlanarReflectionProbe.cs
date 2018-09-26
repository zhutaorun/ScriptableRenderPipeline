using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class PlanarReflectionProbe : HDProbe, ISerializationCallbackReceiver
    {
        public struct RenderData
        {
            public Matrix4x4 worldToCameraRHS;
            public Matrix4x4 projectionMatrix;
        }

        const int currentVersion = 2;

        [SerializeField, FormerlySerializedAs("version")]
        int m_Version;

        public enum CapturePositionMode
        {
            Static,
            MirrorCamera,
        }

        [SerializeField]
        Vector3 m_CaptureLocalPosition;
        [SerializeField]
        Texture m_CustomTexture;
        [SerializeField]
        Texture m_BakedTexture;
        [SerializeField]
        float m_CaptureNearPlane = 1;
        [SerializeField]
        float m_CaptureFarPlane = 1000;
        [SerializeField]
        CapturePositionMode m_CapturePositionMode = CapturePositionMode.Static;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalPosition;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalNormal = Vector3.up;
        [SerializeField]
        bool m_OverrideFieldOfView = false;
        [SerializeField]
        [Range(0, 180)]
        float m_FieldOfViewOverride = 90;

        [SerializeField]
        Vector3 m_LocalReferencePosition = -Vector3.forward;
        [SerializeField]
        RenderData m_BakedRenderData;
        [SerializeField]
        RenderData m_CustomRenderData;
        RenderData m_RealtimeRenderData;

        public override ProbeSettings.ProbeType probeType { get { return ProbeSettings.ProbeType.PlanarProbe; } }

        public RenderData bakedRenderData { get { return m_BakedRenderData; } internal set { m_BakedRenderData = value; } }
        public RenderData customRenderData { get { return m_CustomRenderData; } internal set { m_CustomRenderData = value; } }
        public RenderData realtimeRenderData { get { return m_RealtimeRenderData; } internal set { m_RealtimeRenderData = value; } }
        public RenderData renderData
        {
            get
            {
                switch (mode)
                {
                    default:
                    case ReflectionProbeMode.Baked:
                        return bakedRenderData;
                    case ReflectionProbeMode.Custom:
                        return customRenderData;
                    case ReflectionProbeMode.Realtime:
                        return realtimeRenderData;
                }
            }
        }

        public Vector3 localReferencePosition { get { return m_LocalReferencePosition; } }
        public Vector3 referencePosition { get { return transform.TransformPoint(m_LocalReferencePosition); } }

        public bool overrideFieldOfView { get { return m_OverrideFieldOfView; } }
        public float fieldOfViewOverride { get { return m_FieldOfViewOverride; } }

        public BoundingSphere boundingSphere { get { return influenceVolume.GetBoundingSphereAt(transform); } }

        public Texture texture
        {
            get
            {
                switch (mode)
                {
                    default:
                    case ReflectionProbeMode.Baked:
                        return bakedTexture;
                    case ReflectionProbeMode.Custom:
                        return customTexture;
                    case ReflectionProbeMode.Realtime:
                        return realtimeTexture;
                }
            }
        }
        public Bounds bounds { get { return influenceVolume.GetBoundsAt(transform); } }
        public Vector3 captureLocalPosition { get { return m_CaptureLocalPosition; } set { m_CaptureLocalPosition = value; } }
        public Matrix4x4 influenceToWorld
        {
            get
            {
                var tr = transform;
                var influencePosition = influenceVolume.GetWorldPosition(tr);
                return Matrix4x4.TRS(
                    influencePosition,
                    tr.rotation,
                    Vector3.one
                    );
            }
        }
        public Texture customTexture { get { return m_CustomTexture; } set { m_CustomTexture = value; } }
        public Texture bakedTexture { get { return m_BakedTexture; } set { m_BakedTexture = value; }}
        public float captureNearPlane { get { return m_CaptureNearPlane; } }
        public float captureFarPlane { get { return m_CaptureFarPlane; } }
        public CapturePositionMode capturePositionMode { get { return m_CapturePositionMode; } }
        public Vector3 captureMirrorPlaneLocalPosition
        {
            get { return m_CaptureMirrorPlaneLocalPosition; }
            set { m_CaptureMirrorPlaneLocalPosition = value; }
        }
        public Vector3 captureMirrorPlanePosition { get { return transform.TransformPoint(m_CaptureMirrorPlaneLocalPosition); } }
        public Vector3 captureMirrorPlaneLocalNormal
        {
            get { return m_CaptureMirrorPlaneLocalNormal; }
            set { m_CaptureMirrorPlaneLocalNormal = value; }
        }
        public Vector3 captureMirrorPlaneNormal { get { return transform.TransformDirection(m_CaptureMirrorPlaneLocalNormal); } }

        #region Proxy Properties
        public Matrix4x4 proxyToWorld
        {
            get
            {
                return proxyVolume != null
                    ? Matrix4x4.TRS(proxyVolume.transform.position, proxyVolume.transform.rotation, Vector3.one)
                    : influenceToWorld;
            }
        }
        public ProxyShape proxyShape
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.shape
                    : (ProxyShape)influenceVolume.shape;
            }
        }
        public Vector3 proxyExtents
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.extents
                    : influenceVolume.boxSize;
            }
        }

        public bool useMirrorPlane
        {
            get
            {
                return mode == ReflectionProbeMode.Realtime
                    && refreshMode == ReflectionProbeRefreshMode.EveryFrame
                    && capturePositionMode == CapturePositionMode.MirrorCamera;
            }
        }

        #endregion

        protected override void PopulateSettings(ref ProbeSettings settings)
        {
            base.PopulateSettings(ref settings);

            if (proxyVolume == null)
            {
                if (infiniteProjection)
                {
                    // The proxy is the world itself
                    // The mirror position is the position of the game object
                    settings.proxySettings.mirrorPositionProxySpace = transform.position;
                    settings.proxySettings.mirrorRotationProxySpace = transform.rotation;
                }
                else
                {
                    // The proxy is the influence volume
                    // The mirror position is at the center of the influence
                    settings.proxySettings.mirrorPositionProxySpace = Vector3.zero;
                    settings.proxySettings.mirrorRotationProxySpace = Quaternion.identity;
                }
            }
            else
            {
                var influenceToWorld = transform.localToWorldMatrix;
                var proxyToWorld = proxyVolume.transform.localToWorldMatrix;
                var proxyToInfluence = proxyToWorld.inverse * influenceToWorld;
                // The mirror is a the center of the influence
                var positionPS = proxyToInfluence.MultiplyPoint(Vector3.zero);
                var rotationPS = proxyToInfluence.rotation;
                settings.proxySettings.mirrorPositionProxySpace = positionPS;
                settings.proxySettings.mirrorRotationProxySpace = rotationPS;
            }
        }

        public void RequestRealtimeRender()
        {
            if (isActiveAndEnabled)
                ReflectionSystem.RequestRealtimeRender(this);
        }

        internal override void OnEnable()
        {
            base.OnEnable();
            ReflectionSystem.RegisterProbe(this);
        }

        internal override void OnDisable()
        {
            base.OnDisable();
            ReflectionSystem.UnregisterProbe(this);
        }

        internal override void OnValidate()
        {
            ReflectionSystem.UnregisterProbe(this);

            if (isActiveAndEnabled)
                ReflectionSystem.RegisterProbe(this);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Assert.IsNotNull(influenceVolume, "influenceVolume must have an instance at this point. See HDProbe.Awake()");
            if (m_Version != currentVersion)
            {
                // Add here data migration code
                if(m_Version < 2)
                {
                    influenceVolume.MigrateOffsetSphere();
                }
                m_Version = currentVersion;
            }

            influenceVolume.boxBlendNormalDistanceNegative = Vector3.zero;
            influenceVolume.boxBlendNormalDistancePositive = Vector3.zero;
        }
    }
}
