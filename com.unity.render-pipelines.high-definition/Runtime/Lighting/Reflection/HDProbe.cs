using UnityEngine.Rendering;
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
        ReflectionProbeMode m_Mode = ReflectionProbeMode.Baked;
        [SerializeField]
        ReflectionProbeRefreshMode m_RefreshMode = ReflectionProbeRefreshMode.OnAwake;

        internal ProbeSettings settings
        {
            get
            {
                var settings = ProbeSettings.@default;
                settings.type = probeType;
                settings.influence = influenceVolume;
                settings.linkedProxy = proxyVolume != null ? proxyVolume.proxyVolume : null;
                settings.camera.frameSettings = frameSettings;
                settings.lighting.multiplier = multiplier;
                settings.lighting.weight = weight;
                settings.proxySettings.useInfluenceVolumeAsProxyVolume = !infiniteProjection;
                switch (mode)
                {
                    case ReflectionProbeMode.Baked: settings.mode = ProbeSettings.Mode.Baked; break;
                    case ReflectionProbeMode.Custom: settings.mode = ProbeSettings.Mode.Custom; break;
                    case ReflectionProbeMode.Realtime: settings.mode = ProbeSettings.Mode.Realtime; break;
                }
                return settings;
            }
        }

        public virtual ProbeSettings.ProbeType probeType { get { return ProbeSettings.ProbeType.ReflectionProbe; } }


        RenderTexture m_RealtimeTexture = null;

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

        /// <summary>Get the realtime acquired Render Texture</summary>
        public RenderTexture realtimeTexture { get { return m_RealtimeTexture; } internal set { m_RealtimeTexture = value; } }

        /// <summary>The capture mode.</summary>
        public virtual ReflectionProbeMode mode
        {
            get { return m_Mode; }
            set { m_Mode = value; }
        }

        /// <summary>Refreshing rate of the capture for Realtime capture mode.</summary>
        public virtual ReflectionProbeRefreshMode refreshMode
        {
            get { return m_RefreshMode; }
            set { m_RefreshMode = value; }
        }

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
