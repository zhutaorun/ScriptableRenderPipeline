using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedCaptureSettings
    {
        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty clearDepth;

        public SerializedProperty cullingMask;
        public SerializedProperty useOcclusionCulling;

        public SerializedProperty volumeLayerMask;
        public SerializedProperty volumeAnchorOverride;

        public SerializedProperty projection;
        public SerializedProperty nearClipPlane;
        public SerializedProperty farClipPlane;
        public SerializedProperty fieldOfview;
        public SerializedProperty orthographicSize;

        public SerializedProperty renderingPath;

        public SerializedProperty shadowDistance;

        private SerializedProperty overrides;
        public bool overridesClearColorMode
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.ClearColorMode) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.ClearColorMode;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.ClearColorMode;
            }
        }
        public bool overridesBackgroundColorHDR
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.BackgroundColorHDR) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.BackgroundColorHDR;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.BackgroundColorHDR;
            }
        }
        public bool overridesClearDepth
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.ClearDepth) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.ClearDepth;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.ClearDepth;
            }
        }
        public bool overridesCullingMask
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.CullingMask) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.CullingMask;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.CullingMask;
            }
        }
        public bool overridesVolumeLayerMask
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.VolumeLayerMask) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.VolumeLayerMask;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.VolumeLayerMask;
            }
        }
        public bool overridesVolumeAnchorOverride
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.VolumeAnchorOverride) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.VolumeAnchorOverride;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.VolumeAnchorOverride;
            }
        }
        public bool overridesUseOcclusionCulling
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.UseOcclusionCulling) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.UseOcclusionCulling;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.UseOcclusionCulling;
            }
        }
        public bool overridesProjection
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.Projection) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.Projection;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.Projection;
            }
        }
        public bool overridesNearClip
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.NearClip) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.NearClip;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.NearClip;
            }
        }
        public bool overridesFarClip
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.FarClip) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.FarClip;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.FarClip;
            }
        }
        public bool overridesFieldOfview
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.FieldOfview) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.FieldOfview;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.FieldOfview;
            }
        }
        public bool overridesOrthographicSize
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.OrphographicSize) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.OrphographicSize;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.OrphographicSize;
            }
        }
        public bool overridesRenderingPath
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.RenderingPath) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.RenderingPath;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.RenderingPath;
            }
        }
        public bool overridesShadowDistance
        {
            get { return (overrides.intValue & (int)ObsoleteCaptureSettingsOverrides.ShadowDistance) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)ObsoleteCaptureSettingsOverrides.ShadowDistance;
                else
                    overrides.intValue &= ~(int)ObsoleteCaptureSettingsOverrides.ShadowDistance;
            }
        }

        public SerializedCaptureSettings(SerializedProperty root)
        {
            overrides = root.Find((ObsoleteCaptureSettings d) => d.overrides);

            clearColorMode = root.Find((ObsoleteCaptureSettings d) => d.clearColorMode);
            backgroundColorHDR = root.Find((ObsoleteCaptureSettings d) => d.backgroundColorHDR);
            clearDepth = root.Find((ObsoleteCaptureSettings d) => d.clearDepth);

            cullingMask = root.Find((ObsoleteCaptureSettings d) => d.cullingMask);
            useOcclusionCulling = root.Find((ObsoleteCaptureSettings d) => d.useOcclusionCulling);

            volumeLayerMask = root.Find((ObsoleteCaptureSettings d) => d.volumeLayerMask);
            volumeAnchorOverride = root.Find((ObsoleteCaptureSettings d) => d.volumeAnchorOverride);

            projection = root.Find((ObsoleteCaptureSettings d) => d.projection);
            nearClipPlane = root.Find((ObsoleteCaptureSettings d) => d.nearClipPlane);
            farClipPlane = root.Find((ObsoleteCaptureSettings d) => d.farClipPlane);
            fieldOfview = root.Find((ObsoleteCaptureSettings d) => d.fieldOfView);
            orthographicSize = root.Find((ObsoleteCaptureSettings d) => d.orthographicSize);

            renderingPath = root.Find((ObsoleteCaptureSettings d) => d.renderingPath);

            shadowDistance = root.Find((ObsoleteCaptureSettings d) => d.shadowDistance);
        }
    }
}
