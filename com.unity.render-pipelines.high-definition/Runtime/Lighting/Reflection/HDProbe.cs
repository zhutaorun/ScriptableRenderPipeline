using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public abstract class HDProbe : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField, FormerlySerializedAs("proxyVolumeComponent"), FormerlySerializedAs("m_ProxyVolumeReference")]
        ReflectionProxyVolumeComponent m_ProxyVolume = null;
        [SerializeField]
        bool m_InfiniteProjection = true; //usable when no proxy set

        [SerializeField]
        InfluenceVolume m_InfluenceVolume;

        [SerializeField]
        FrameSettings m_FrameSettings = null;

        [SerializeField, FormerlySerializedAsAttribute("dimmer"), FormerlySerializedAsAttribute("m_Dimmer"), FormerlySerializedAsAttribute("multiplier")]
        float m_Multiplier = 1.0f;
        [SerializeField, FormerlySerializedAsAttribute("weight")]
        [Range(0.0f, 1.0f)]
        float m_Weight = 1.0f;

        [SerializeField]
        ProbeSettings.Mode m_Mode = ProbeSettings.Mode.Baked;

        [SerializeField]
        Texture m_BakedTexture;
        [SerializeField]
        Texture m_CustomTexture;
        RenderTexture m_RealtimeTexture;

        public Texture bakedTexture { get { return m_BakedTexture; } }
        public Texture customTexture { get { return m_CustomTexture; } }
        public RenderTexture realtimeTexture { get { return m_RealtimeTexture; } }

        public Texture texture { get { return GetTexture(mode); } }

        internal ProbeSettings settings
        {
            get
            {
                var settings = ProbeSettings.@default;
                PopulateSettings(ref settings);
                return settings;
            }
        }

        public Texture GetTexture(ProbeSettings.Mode targetMode)
        {
            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return m_BakedTexture;
                case ProbeSettings.Mode.Custom: return m_CustomTexture;
                case ProbeSettings.Mode.Realtime: return m_RealtimeTexture;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public Texture SetTexture(ProbeSettings.Mode targetMode, Texture texture)
        {
            if (targetMode == ProbeSettings.Mode.Realtime && !(texture is RenderTexture))
                throw new ArgumentException("'texture' must be a RenderTexture for the Realtime mode.");

            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return m_BakedTexture = texture;
                case ProbeSettings.Mode.Custom: return m_CustomTexture = texture;
                case ProbeSettings.Mode.Realtime: return m_RealtimeTexture = (RenderTexture)texture;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual void PopulateSettings(ref ProbeSettings settings)
        {
            settings.type = probeType;
            settings.influence = influenceVolume;
            settings.linkedProxy = proxyVolume != null ? proxyVolume.proxyVolume : null;
            settings.camera.frameSettings = frameSettings;
            settings.lighting.multiplier = multiplier;
            settings.lighting.weight = weight;
            settings.proxySettings.useInfluenceVolumeAsProxyVolume = !infiniteProjection;
            settings.mode = mode;
        }

        protected void ComputeTransformRelativeToInfluence(out Vector3 position, out Quaternion rotation)
        {
            if (proxyVolume == null)
            {
                if (infiniteProjection)
                {
                    // The proxy is the world itself
                    // The position is the position of the game object
                    position = transform.position;
                    rotation = transform.rotation;
                }
                else
                {
                    // The proxy is the influence volume
                    // The position is at the center of the influence
                    position = Vector3.zero;
                    rotation = Quaternion.identity;
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
                position = positionPS;
                rotation = rotationPS;
            }
        }

        public virtual ProbeSettings.ProbeType probeType { get { return ProbeSettings.ProbeType.ReflectionProbe; } }
        
        [SerializeField]
        Texture m_CustomTexture;
        [SerializeField]
        Texture m_BakedTexture;

        /// <summary>Light layer to use by this probe.</summary>
        public LightLayerEnum lightLayers = LightLayerEnum.LightLayerDefault;

        // This function return a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1
        public uint GetLightLayers()
        {
            int value = (int)(lightLayers);
            return value < 0 ? (uint)LightLayerEnum.Everything : (uint)value;
        }

        /// <summary>ProxyVolume currently used by this probe.</summary>
        public ReflectionProxyVolumeComponent proxyVolume { get { return m_ProxyVolume; } }

        /// <summary>InfluenceVolume of the probe.</summary>
        public InfluenceVolume influenceVolume { get { return m_InfluenceVolume; } private set { m_InfluenceVolume = value; } }

        /// <summary>Frame settings in use with this probe.</summary>
        public FrameSettings frameSettings { get { return m_FrameSettings; } }

        /// <summary>Multiplier factor of reflection (non PBR parameter).</summary>
        public float multiplier { get { return m_Multiplier; } set { m_Multiplier = value; } }
        /// <summary>Weight for blending amongst probes (non PBR parameter).</summary>
        public float weight { get { return m_Weight; } set { m_Weight = value; } }

        /// <summary>The capture mode.</summary>
        public virtual ProbeSettings.Mode mode
        {
            get { return m_Mode; }
            set { m_Mode = value; }
        }
        /// <summary>Is the projection at infinite? Value could be changed by Proxy mode.</summary>
        public bool infiniteProjection
        {
            get
            {
                return (proxyVolume != null && proxyVolume.proxyVolume.shape == ProxyShape.Infinite)
                    || (proxyVolume == null && m_InfiniteProjection);
            }
            set
            {
                m_InfiniteProjection = value;
            }
        }

        internal Matrix4x4 influenceToWorld
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
        internal Vector3 influenceExtents
        {
            get
            {
                switch (influenceVolume.shape)
                {
                    default:
                    case InfluenceShape.Box:
                        return influenceVolume.boxSize * 0.5f;
                    case InfluenceShape.Sphere:
                        return influenceVolume.sphereRadius * Vector3.one;
                }
            }
        }
        internal Matrix4x4 proxyToWorld
        {
            get
            {
                return proxyVolume != null
                    ? Matrix4x4.TRS(proxyVolume.transform.position, proxyVolume.transform.rotation, Vector3.one)
                    : influenceToWorld;
            }
        }
        public virtual Vector3 proxyExtents
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.extents
                    : influenceExtents;
            }
        }
        internal virtual Vector3 capturePosition
        {
            get
            {
                return transform.position; //at the moment capture position is at probe position
            }
        }

        internal virtual void Awake()
        {
            if (influenceVolume == null)
                influenceVolume = new InfluenceVolume();
            influenceVolume.Init(this);
        }

        internal virtual void OnEnable()
        {
            HDProbeSystem.RegisterProbe(this);
        }

        internal virtual void OnDisable()
        {
            HDProbeSystem.UnregisterProbe(this);
        }

        internal virtual void OnValidate()
        {
            HDProbeSystem.UnregisterProbe(this);

            if (isActiveAndEnabled)
                HDProbeSystem.RegisterProbe(this);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            influenceVolume.Init(this);
        }

        internal virtual void UpdatedInfluenceVolumeShape(Vector3 size, Vector3 offset) { }
    }
}
