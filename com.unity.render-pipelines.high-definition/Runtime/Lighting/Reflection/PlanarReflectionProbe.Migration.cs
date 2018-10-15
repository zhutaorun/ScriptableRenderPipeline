using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public sealed partial class PlanarReflectionProbe : IVersionable<PlanarReflectionProbe.Version>
    {
        enum Version
        {
            Initial,
            First = 2,
            CaptureSettings,
            ProbeSettings
        }

        [SerializeField, FormerlySerializedAs("version"), FormerlySerializedAs("m_Version")]
        int m_PlanarProbeVersion;
        Version IVersionable<Version>.version { get => (Version)m_PlanarProbeVersion; set => m_PlanarProbeVersion = (int)value; }

        static readonly MigrationDescription<Version, PlanarReflectionProbe> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.CaptureSettings, (Action<PlanarReflectionProbe>)((PlanarReflectionProbe p) =>
            {
#pragma warning disable 618
                if (p.m_ObsoleteCaptureSettings == null)
                    p.m_ObsoleteCaptureSettings = new ObsoleteCaptureSettings();
                if (p.m_ObsoleteOverrideFieldOfView)
                    p.m_ObsoleteCaptureSettings.overrides |= ObsoleteCaptureSettingsOverrides.FieldOfview;
                p.m_ObsoleteCaptureSettings.fieldOfView = p.m_ObsoleteFieldOfViewOverride;
                p.m_ObsoleteCaptureSettings.nearClipPlane = p.m_ObsoleteCaptureNearPlane;
                p.m_ObsoleteCaptureSettings.farClipPlane = p.m_ObsoleteCaptureFarPlane;
                p.m_ProbeSettings.camera.frustum.nearClipPlane = p.m_ObsoleteCaptureNearPlane;
                p.m_ProbeSettings.camera.frustum.farClipPlane = p.m_ObsoleteCaptureFarPlane;
#pragma warning restore 618
            })),
            MigrationStep.New(Version.ProbeSettings, (Action<PlanarReflectionProbe>)((PlanarReflectionProbe p) =>
            {
#pragma warning disable 618
                p.m_ProbeSettings.camera.frustum.nearClipPlane = p.m_ObsoleteCaptureSettings.nearClipPlane;
                p.m_ProbeSettings.camera.frustum.farClipPlane = p.m_ObsoleteCaptureSettings.farClipPlane;
#pragma warning restore 618
            }))
        );

        // Obsolete Properties
        [SerializeField, FormerlySerializedAs("m_CaptureNearPlane"), Obsolete("For data migration")]
        float m_ObsoleteCaptureNearPlane = ObsoleteCaptureSettings.@default.nearClipPlane;
        [SerializeField, FormerlySerializedAs("m_CaptureFarPlane"), Obsolete("For data migration")]
        float m_ObsoleteCaptureFarPlane = ObsoleteCaptureSettings.@default.farClipPlane;

        [SerializeField, FormerlySerializedAs("m_OverrideFieldOfView"), Obsolete("For data migration")]
        bool m_ObsoleteOverrideFieldOfView;
        [SerializeField, FormerlySerializedAs("m_FieldOfViewOverride"), Obsolete("For data migration")]
        float m_ObsoleteFieldOfViewOverride = ObsoleteCaptureSettings.@default.fieldOfView;
    }
}
