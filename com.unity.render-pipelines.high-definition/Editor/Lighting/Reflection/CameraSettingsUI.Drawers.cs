using UnityEngine.Experimental.Rendering.HDPipeline;

using static UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    internal partial class CameraSettingsUI
    {
        public static void Draw(
            CameraSettingsUI s, SerializedCameraSettings d, Editor o,
            SerializedCameraSettingsOverride @override, CameraSettingsOverride displayedFields
        )
        {
            const CameraSettingsFields physicalFields = CameraSettingsFields.physicalAperture
                | CameraSettingsFields.physicalIso
                | CameraSettingsFields.physicalShutterSpeed;
            const CameraSettingsFields bufferFields = CameraSettingsFields.bufferClearBackgroundColorHDR
                | CameraSettingsFields.bufferClearClearDepth
                | CameraSettingsFields.bufferClearColorMode;
            const CameraSettingsFields volumesFields = CameraSettingsFields.volumesAnchorOverride
                | CameraSettingsFields.volumesLayerMask;
            const CameraSettingsFields cullingFields = CameraSettingsFields.cullingCullingMask
                | CameraSettingsFields.cullingInvertCulling
                | CameraSettingsFields.cullingUseOcclusionCulling;
            const CameraSettingsFields frustumFields = CameraSettingsFields.frustumAspect
                | CameraSettingsFields.frustumFarClipPlane
                | CameraSettingsFields.frustumMode
                | CameraSettingsFields.frustumNearClipPlane
                | CameraSettingsFields.frustumProjectionMatrix
                | CameraSettingsFields.frustumFieldOfView;
            const CameraSettingsFields frustumFarOrNearPlane = CameraSettingsFields.frustumFarClipPlane
                | CameraSettingsFields.frustumNearClipPlane;

            if (displayedFields.camera.HasFlag(physicalFields))
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.physicalAperture, d.physicalAperture, _.GetContent("Aperture"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.physicalIso, d.physicalIso, _.GetContent("ISO"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.physicalShutterSpeed, d.physicalShutterSpeed, _.GetContent("Shutter Speed"), @override.camera, displayedFields.camera);
                EditorGUILayout.Space();
            }

            if (displayedFields.camera.HasFlag(bufferFields))
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearColorMode, d.bufferClearColorMode, _.GetContent("Clear Mode"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearBackgroundColorHDR, d.bufferClearBackgroundColorHDR, _.GetContent("Background Color"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearClearDepth, d.bufferClearClearDepth, _.GetContent("Clear Depth"), @override.camera, displayedFields.camera);
                EditorGUILayout.Space();
            }

            if (displayedFields.camera.HasFlag(volumesFields))
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.volumesLayerMask, d.volumesLayerMask, _.GetContent("Volume Layer Mask"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.volumesAnchorOverride, d.volumesAnchorOverride, _.GetContent("Volume Anchor Override"), @override.camera, displayedFields.camera);
                EditorGUILayout.Space();
            }

            if (displayedFields.camera.HasFlag(cullingFields))
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingUseOcclusionCulling, d.cullingUseOcclusionCulling, _.GetContent("Use Occlusion Culling"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingCullingMask, d.cullingCullingMask, _.GetContent("Culling Mask"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingInvertCulling, d.cullingCullingMask, _.GetContent("Invert Backface Culling"), @override.camera, displayedFields.camera);
                EditorGUILayout.Space();
            }

            if (displayedFields.camera.HasFlag(frustumFields))
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumAspect, d.frustumAspect, _.GetContent("Aspect"), @override.camera, displayedFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumFieldOfView, d.frustumFieldOfView, _.GetContent("Field Of View"), @override.camera, displayedFields.camera);
                if (displayedFields.camera.HasFlag(frustumFarOrNearPlane))
                {
                    EditorGUILayout.BeginHorizontal();
                    FlagToggle(frustumFarOrNearPlane, @override.camera);
                    _.DrawMultipleFields(
                        "Clip Planes",
                        new[] { d.frustumNearClipPlane, d.frustumFarClipPlane },
                        new[] { _.GetContent("Near"), _.GetContent("Far") });
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumFarClipPlane, d.frustumFarClipPlane, _.GetContent("Far Clip Plane"), @override.camera, displayedFields.camera);
                    PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumNearClipPlane, d.frustumNearClipPlane, _.GetContent("Near Clip Plane"), @override.camera, displayedFields.camera);
                }
                EditorGUILayout.Space();
            }

            PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.flipYMode, d.flipYMode, _.GetContent("Flip Y"), @override.camera, displayedFields.camera);
            PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.renderingPath, d.renderingPath, _.GetContent("Rendering Path"), @override.camera, displayedFields.camera);
        }
    }
}
