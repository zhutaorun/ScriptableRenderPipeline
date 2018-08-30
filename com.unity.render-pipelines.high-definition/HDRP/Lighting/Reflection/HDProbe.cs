using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct CameraSettings
    {
        // HD Additional Camera Data
        public HDAdditionalCameraData.ClearColorMode clearColorMode;
        public Color backgroundColorHDR;
        public bool clearDepth;
        public HDAdditionalCameraData.RenderingPath renderingPath;
        public LayerMask volumeLayerMask;
        public Transform volumeAnchorOverride;
        public float aperture;
        public float shutterSpeed;
        public float iso;
        public FrameSettings frameSettings;

        // Legacy Camera
        public float farClipPlane;
        public float nearClipPlane;
        public float fieldOfview;
        public bool useOcclusionCulling;
        public int cullingMask;

        // Post process
        public PostProcessLayer postProcessLayer;
    }

    public enum HDReflectionProbeMode
    {
        Baked,
        Custom,
        Realtime
    }

    [Serializable]
    public struct RenderData
    {
        // Capture data for planar probes
        public Matrix4x4 worldToCameraMatrix;
        public Matrix4x4 projectionMatrix;
    }

    [ExecuteInEditMode]
    public abstract class HDProbe : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct CaptureProperties
        {
            public HDReflectionProbeMode mode;
            public CameraSettings cameraSettings;
        }

        [Serializable]
        internal struct Assets
        {
            public Texture bakedTexture;        // TODO: texture should not be serialized here
            public RenderData bakedData;
            public Texture customTexture;       // TODO: otherwise, you can have like the baked texture
            public RenderData customData;
            public Texture realtimeTexture;     // TODO: included in build for a realtime probe...
            public RenderData realtimeData;
        }

        // TODO: maybe set this one as private and use properties to access its fields
        // TODO: move to internal visibility
        // TODO: create getter/setter to expose fields for scripting
        public CaptureProperties captureProperties;

        public Texture bakedTexture { get { return assets.bakedTexture; } set { assets.bakedTexture = value; } }
        public RenderData bakedRenderData { get { return assets.bakedData; } set { assets.bakedData = value; } }

        public RenderData renderData
        {
            get
            {
                switch (captureProperties.mode)
                {
                    case HDReflectionProbeMode.Baked: return bakedRenderData;
                    default: throw new ArgumentException();
                }
            }
        }
        public Texture texture
        {
            get
            {
                switch (captureProperties.mode)
                {
                    case HDReflectionProbeMode.Baked: return bakedTexture;
                    default: throw new ArgumentException();
                }
            }
        }

        public abstract Hash128 ComputeBakePropertyHashes();
        public abstract void GetCaptureTransformFor(
            Vector3 viewerPosition, Quaternion viewerRotation,
            out Vector3 capturePosition, out Quaternion captureRotation
        );

        // ---------------

        [SerializeField]
        internal Assets assets;

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

        internal void Awake()
        {
            if (influenceVolume == null)
                influenceVolume = new InfluenceVolume();
            influenceVolume.Init(this);
        }

        protected virtual void OnEnable()
        {
            HDReflectionEntitySystem.instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            HDReflectionEntitySystem.instance.Unregister(this);
        }

        protected virtual void OnValidate()
        {
            HDReflectionEntitySystem.instance.Unregister(this);
            HDReflectionEntitySystem.instance.Register(this);
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
