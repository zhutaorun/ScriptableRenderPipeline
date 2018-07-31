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
            public Texture customTexture;       // TODO: otherwise, you can have like the baked texture
            public Texture realtimeTexture;     // TODO: included in build for a realtime probe...
        }

        // TODO: maybe set this one as private and use properties to access its fields
        public CaptureProperties captureProperties;

        public Texture bakedTexture { get { return assets.bakedTexture; } set { assets.bakedTexture = value; } }

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
        InfluenceVolume m_InfluenceVolume;

        [SerializeField, FormerlySerializedAsAttribute("dimmer"), FormerlySerializedAsAttribute("m_Dimmer"), FormerlySerializedAsAttribute("multiplier")]
        float m_Multiplier = 1.0f;
        [SerializeField, FormerlySerializedAsAttribute("weight")]
        [Range(0.0f, 1.0f)]
        float m_Weight = 1.0f;

        [SerializeField]
        ReflectionProbeMode m_Mode = ReflectionProbeMode.Baked;
        [SerializeField]
        ReflectionProbeRefreshMode m_RefreshMode = ReflectionProbeRefreshMode.OnAwake;

        /// <summary>ProxyVolume currently used by this probe.</summary>
        public ReflectionProxyVolumeComponent proxyVolume { get { return m_ProxyVolume; } }

        /// <summary>InfluenceVolume of the probe.</summary>
        public InfluenceVolume influenceVolume { get { return m_InfluenceVolume; } private set { m_InfluenceVolume = value; } }

        /// <summary>Multiplier factor of reflection (non PBR parameter).</summary>
        public float multiplier { get { return m_Multiplier; } }

        /// <summary>Weight for blending amongst probes (non PBR parameter).</summary>
        public float weight { get { return m_Weight; } }

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
