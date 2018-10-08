using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract partial class HDProbe : IVersionable<HDProbe.Version>
    {
        enum Version
        {
            Initial,
            ProbeSettings
        }

        static readonly MigrationDescription<Version, HDProbe> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.ProbeSettings, (HDProbe p) =>
            {
#pragma warning disable 618
                p.m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume = !p.m_ObsoleteInfiniteProjection;
                p.m_ProbeSettings.influence = p.m_ObsoleteInfluenceVolume;
                p.m_ProbeSettings.camera.frameSettings = p.m_ObsoleteFrameSettings;
                p.m_ProbeSettings.lighting.multiplier = p.m_ObsoleteMultiplier;
                p.m_ProbeSettings.lighting.weight = p.m_ObsoleteWeight;
                p.m_ProbeSettings.lighting.lightLayer = p.m_ObsoleteLightLayers;
                p.m_ProbeSettings.mode = p.m_ObsoleteMode;
#pragma warning restore 618
            })
        );

        [SerializeField]
        Version m_Version;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        // Legacy fields for HDProbe
        [SerializeField, FormerlySerializedAs("m_InfiniteProjection"), Obsolete("For Data Migration")]
        bool m_ObsoleteInfiniteProjection = true;

        [SerializeField, FormerlySerializedAs("m_InfluenceVolume"), Obsolete("For Data Migration")]
        InfluenceVolume m_ObsoleteInfluenceVolume;

        [SerializeField, FormerlySerializedAs("m_FrameSettings"), Obsolete("For Data Migration")]
        FrameSettings m_ObsoleteFrameSettings = null;

        [SerializeField, FormerlySerializedAs("m_Multiplier"), FormerlySerializedAs("dimmer")]
        [FormerlySerializedAs("m_Dimmer"), FormerlySerializedAs("multiplier"), Obsolete("For Data Migration")]
        float m_ObsoleteMultiplier = 1.0f;
        [SerializeField, FormerlySerializedAs("m_Weight"), FormerlySerializedAs("weight")]
        [Obsolete("For Data Migration")]
        [Range(0.0f, 1.0f)]
        float m_ObsoleteWeight = 1.0f;

        [SerializeField, FormerlySerializedAs("m_Mode"), Obsolete("For Data Migration")]
        ProbeSettings.Mode m_ObsoleteMode = ProbeSettings.Mode.Baked;

        [SerializeField, FormerlySerializedAs("lightLayer"), Obsolete("For Data Migration")]
        LightLayerEnum m_ObsoleteLightLayers = LightLayerEnum.LightLayerDefault;

        [SerializeField, FormerlySerializedAs("m_CaptureSettings"), Obsolete("For Data Migration")]
        protected ObsoleteCaptureSettings m_ObsoleteCaptureSettings;
    }
}
