using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class PlanarReflectionProbe : IVersionable<PlanarReflectionProbe.Version>
    {
        enum Version
        {
            Initial,
            First = 2,
            ProbeSettings
        }

        [SerializeField, FormerlySerializedAs("version")]
        int m_Version;
        Version IVersionable<Version>.version { get => (Version)m_Version; set => m_Version = (int)value; }

        static readonly MigrationDescription<Version, PlanarReflectionProbe> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.ProbeSettings, (PlanarReflectionProbe p) =>
            {
#pragma warning disable 618
                p.m_ProbeSettings.camera.frustum.nearClipPlane = p.m_ObsoleteCaptureNearPlane;
                p.m_ProbeSettings.camera.frustum.farClipPlane = p.m_ObsoleteCaptureFarPlane;
#pragma warning restore 618
            })
        );

        // Obsolete Properties
        [SerializeField, FormerlySerializedAs("m_CaptureNearPlane"), Obsolete("For data migration")]
        float m_ObsoleteCaptureNearPlane = 1;
        [SerializeField, FormerlySerializedAs("m_CaptureFarPlane"), Obsolete("For data migration")]
        float m_ObsoleteCaptureFarPlane = 1000;
    }
}
