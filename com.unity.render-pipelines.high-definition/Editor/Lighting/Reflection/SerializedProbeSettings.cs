using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedProbeSettings
    {
        internal SerializedProperty root;
        internal SerializedCameraSettings cameraSettings;
        internal SerializedInfluenceVolume influence;
        internal SerializedReflectionProxyVolumeComponent proxy;

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
        internal SerializedProperty linkedProxy;

        internal SerializedProbeSettings(SerializedProperty root)
        {
            this.root = root;

            linkedProxy = root.FindPropertyRelative("linkedProxy");
            type = root.FindPropertyRelative("type");
            mode = root.FindPropertyRelative("mode");
            lightingMultiplier = root.FindPropertyRelative("lighting.multiplier");
            lightingWeight = root.FindPropertyRelative("lighting.weight");
            lightingLightLayer = root.FindPropertyRelative("lighting.lightLayer");
            proxyUseInfluenceVolumeAsProxyVolume = root.FindPropertyRelative("proxy.useInfluenceVolumeAsProxyVolume");
            proxyCapturePositionProxySpace = root.FindPropertyRelative("proxy.capturePositionProxySpace");
            proxyCaptureRotationProxySpace = root.FindPropertyRelative("proxy.captureRotationProxySpace");
            proxyMirrorPositionProxySpace = root.FindPropertyRelative("proxy.mirrorPositionProxySpace");
            proxyMirrorRotationProxySpace = root.FindPropertyRelative("proxy.mirrorRotationProxySpace");

            cameraSettings = new SerializedCameraSettings(root.Find((ProbeSettings p) => p.camera));
            influence = new SerializedInfluenceVolume(root.Find((ProbeSettings p) => p.influence));
            proxy = linkedProxy.objectReferenceValue != null
                ? new SerializedReflectionProxyVolumeComponent(new SerializedObject(linkedProxy.objectReferenceValue))
                : null;
        }
    }
}
