using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    abstract class HDProbe2 : MonoBehaviour
    {
        public enum WorkflowType
        {
            Baked,
            Custom,
            Realtime
        }

        public struct CaptureProperties
        {
            public Vector3 position;
            public float farClipPlane;
            public float nearClipPlane;
            public float fieldOfview;
            public CameraClearFlags clearFlags;
            public Color backgroundColor;
            public WorkflowType workflowType;
        }

        internal struct BakedProperties
        {
            public Hash128 bakedTextureHash { get; set; }
        }

        internal struct CustomProperties
        {
            public Hash128 customTextureHash { get; set; }
        }

        internal struct RealtimeProperties
        {
            public Hash128 realtimeHash { get; set; }
        }

        internal struct Assets
        {
            public Texture bakedTexture;
            public Texture customTexture;
            public Texture realtimeTexture;
            public FrameSettings captureFrameSettings;
            public PostProcessLayer postProcessLayer;
        }

        internal HDReflectionEntityID entityId;
        internal Assets assets;

        public BakedProperties bakedProperties;
        public CustomProperties customProperties;
        public RealtimeProperties realtimeProperties;

        public Texture bakedTexture { get { return assets.bakedTexture; } set { assets.bakedTexture = value; } }

        public abstract Hash128 ComputeBakePropertyHashes();
    }
}
