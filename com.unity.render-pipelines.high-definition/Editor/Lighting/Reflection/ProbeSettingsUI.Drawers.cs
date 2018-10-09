using UnityEngine.Experimental.Rendering.HDPipeline;

using static UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    internal partial class ProbeSettingsUI
    {
        public static void Draw(
            ProbeSettingsUI s, SerializedProbeSettings d, Editor o,
            SerializedProbeSettingsOverride @override, ProbeSettingsOverride displayedFields
        )
        {
            const ProbeSettingsFields lighting = ProbeSettingsFields.lightingLightLayer
                | ProbeSettingsFields.lightingMultiplier
                | ProbeSettingsFields.lightingWeight;
            const ProbeSettingsFields proxy = ProbeSettingsFields.proxyCapturePositionProxySpace
                | ProbeSettingsFields.proxyCaptureRotationProxySpace
                | ProbeSettingsFields.proxyMirrorPositionProxySpace
                | ProbeSettingsFields.proxyMirrorRotationProxySpace
                | ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume;

            if (displayedFields.probe.HasFlag(lighting))
            {
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingLightLayer, d.lightingLightLayer, _.GetContent("Light Layer"), @override.probe, displayedFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingMultiplier, d.lightingMultiplier, _.GetContent("Multiplier"), @override.probe, displayedFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingWeight, d.lightingWeight, _.GetContent("Weight"), @override.probe, displayedFields.probe);
                EditorGUILayout.Space();
            }

            if (displayedFields.probe.HasFlag(proxy))
            {
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume, d.proxyUseInfluenceVolumeAsProxyVolume, _.GetContent("Use Influence Volume As Proxy Volume"), @override.probe, displayedFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyCapturePositionProxySpace, d.proxyCapturePositionProxySpace, _.GetContent("Capture Position Proxy Space"), @override.probe, displayedFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyCaptureRotationProxySpace, d.proxyCaptureRotationProxySpace, _.GetContent("Capture Rotation Proxy Space"), @override.probe, displayedFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyMirrorPositionProxySpace, d.proxyMirrorPositionProxySpace, _.GetContent("Mirror Position Proxy Space"), @override.probe, displayedFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyMirrorPositionProxySpace, d.proxyMirrorPositionProxySpace, _.GetContent("Mirror Rotation Proxy Space"), @override.probe, displayedFields.probe);
                EditorGUILayout.Space();
            }
        }
    }
}
