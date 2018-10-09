using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedProbeSettings
    {
        internal SerializedProperty root;
        internal SerializedCameraSettings cameraSettings;
        internal SerializedInfluenceVolume influence;
        internal SerializedProxyVolume proxy;

        internal SerializedProperty type;
        internal SerializedProperty mode;
        internal SerializedProperty lightingMultiplier;
        internal SerializedProperty lightingWeight;
        internal SerializedProperty lightingLightLayer;
        internal SerializedProperty proxyUseInfluenceVolumeAsProxyVolume;
        internal SerializedProperty proxyCapturePositionProxySpace;
        internal SerializedProperty proxyCaptureRotationProxySpace;
        internal SerializedProperty proxyMirrorPositionProxySpace;
        internal SerializedProperty proxyMirrorRotationProxySpace;

        internal SerializedProbeSettings(SerializedProperty root)
        {
            this.root = root;

            type = root.Find((ProbeSettings p) => p.type);
            mode = root.Find((ProbeSettings p) => p.mode);
            lightingMultiplier = root.FindPropertyRelative("lighting.multiplier");
            lightingWeight = root.FindPropertyRelative("lighting.weight");
            lightingLightLayer = root.FindPropertyRelative("lighting.lightLayer");
            proxyUseInfluenceVolumeAsProxyVolume = root.FindPropertyRelative("proxySettings.useInfluenceVolumeAsProxyVolume");
            proxyCapturePositionProxySpace = root.FindPropertyRelative("proxySettings.capturePositionProxySpace");
            proxyCaptureRotationProxySpace = root.FindPropertyRelative("proxySettings.captureRotationProxySpace");
            proxyMirrorPositionProxySpace = root.FindPropertyRelative("proxySettings.mirrorPositionProxySpace");
            proxyMirrorRotationProxySpace = root.FindPropertyRelative("proxySettings.mirrorRotationProxySpace");

            cameraSettings = new SerializedCameraSettings(root.Find((ProbeSettings p) => p.camera));
            influence = new SerializedInfluenceVolume(root.Find((ProbeSettings p) => p.influence));
            proxy = new SerializedProxyVolume(root.Find((ProbeSettings p) => p.proxy));
        }
    }
}
