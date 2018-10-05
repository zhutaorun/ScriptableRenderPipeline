using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract partial class HDProbe : MonoBehaviour
    {
        // Serialized Data
        [SerializeField]
        protected ProbeSettings m_ProbeSettings;
        [SerializeField]
        ReflectionProxyVolumeComponent m_ProxyVolume;

        [SerializeField]
        Texture m_BakedTexture;
        [SerializeField]
        Texture m_CustomTexture;

        // Runtime Data
        RenderTexture m_RealtimeTexture;

        // Public API
        // Texture asset
        public Texture bakedTexture => m_BakedTexture;
        public Texture customTexture => m_CustomTexture;
        public RenderTexture realtimeTexture => m_RealtimeTexture;
        public Texture texture => GetTexture(mode);
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

        // Settings
        // General
        public ProbeSettings.ProbeType type => m_ProbeSettings.type;
        /// <summary>The capture mode.</summary>
        public virtual ProbeSettings.Mode mode { get => m_ProbeSettings.mode; set => m_ProbeSettings.mode = value; }

        // Lighting
        /// <summary>Light layer to use by this probe.</summary>
        public LightLayerEnum lightLayers
        { get => m_ProbeSettings.lighting.lightLayer; set => m_ProbeSettings.lighting.lightLayer = value; }
        // This function return a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1
        public uint lightLayersAsUInt => lightLayers < 0 ? (uint)LightLayerEnum.Everything : (uint)lightLayers;
        /// <summary>Multiplier factor of reflection (non PBR parameter).</summary>
        public float multiplier
        { get => m_ProbeSettings.lighting.multiplier; set => m_ProbeSettings.lighting.multiplier = value; }
        /// <summary>Weight for blending amongst probes (non PBR parameter).</summary>
        public float weight
        { get => m_ProbeSettings.lighting.weight; set => m_ProbeSettings.lighting.weight = value; }

        // Proxy
        /// <summary>ProxyVolume currently used by this probe.</summary>
        public ReflectionProxyVolumeComponent proxyVolume => m_ProxyVolume;
        public bool useInfluenceVolumeAsProxyVolume => m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume;
        /// <summary>Is the projection at infinite? Value could be changed by Proxy mode.</summary>
        public bool isProjectionInfinite
            => m_ProxyVolume != null && m_ProxyVolume.proxyVolume.shape == ProxyShape.Infinite
            || m_ProxyVolume == null && !m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume;

        // Influence
        /// <summary>InfluenceVolume of the probe.</summary>
        public InfluenceVolume influenceVolume
        { get => m_ProbeSettings.influence; private set => m_ProbeSettings.influence = value; }
        internal Matrix4x4 influenceToWorld => influenceVolume.GetInfluenceToWorld(transform);

        // Camera
        /// <summary>Frame settings in use with this probe.</summary>
        public FrameSettings frameSettings => m_ProbeSettings.camera.frameSettings;
        internal Vector3 influenceExtents => influenceVolume.extents;
        internal Matrix4x4 proxyToWorld
            => proxyVolume != null ? proxyVolume.transform.localToWorldMatrix : influenceToWorld;
        public Vector3 proxyExtents
            => proxyVolume != null ? proxyVolume.proxyVolume.extents : influenceExtents;

        public BoundingSphere boundingSphere => influenceVolume.GetBoundingSphereAt(transform);
        public Bounds bounds => influenceVolume.GetBoundsAt(transform);

        internal ProbeSettings settings
        {
            get
            {
                var settings = m_ProbeSettings;
                // Special case here, we reference a component that is a wrapper
                // So we need to update with the actual value for the proxyVolume
                settings.linkedProxy = m_ProxyVolume?.proxyVolume;
                PopulateSettings(ref settings);
                return settings;
            }
        }

        // API
        /// <summary>
        /// Prepare the probe for culling.
        /// You should call this method when you update the <see cref="influenceVolume"/> parameters during runtime.
        /// </summary>
        public virtual void PrepareCulling() { }

        // Life cycle methods
        internal virtual void Awake() => k_Migration.Migrate(this);

        internal virtual void OnEnable()
        {
            PrepareCulling();
            HDProbeSystem.RegisterProbe(this);
        }
        internal virtual void OnDisable() => HDProbeSystem.UnregisterProbe(this);

        internal virtual void OnValidate()
        {
            HDProbeSystem.UnregisterProbe(this);

            if (isActiveAndEnabled)
                HDProbeSystem.RegisterProbe(this);
        }

        // Private API
        protected virtual void PopulateSettings(ref ProbeSettings settings) { }

        protected void ComputeTransformRelativeToInfluence(out Vector3 position, out Quaternion rotation)
        {
            if (proxyVolume == null)
            {
                if (isProjectionInfinite)
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
    }
}
