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
                p.m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume = !p.m_LegacyInfiniteProjection;
                p.m_ProbeSettings.influence = p.m_LegacyInfluenceVolume;
                p.m_ProbeSettings.camera.frameSettings = p.m_LegacyFrameSettings;
                p.m_ProbeSettings.lighting.multiplier = p.m_LegacyMultiplier;
                p.m_ProbeSettings.lighting.weight = p.m_LegacyWeight;
                p.m_ProbeSettings.lighting.lightLayer = p.m_LegacyLightLayers;
                p.m_ProbeSettings.mode = p.m_LegacyMode;
            })
        );

        [SerializeField]
        Version m_Version;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        // Legacy fields for HDProbe
        [SerializeField, FormerlySerializedAs("m_InfiniteProjection")]
        bool m_LegacyInfiniteProjection = true;

        [SerializeField, FormerlySerializedAs("m_InfluenceVolume")]
        InfluenceVolume m_LegacyInfluenceVolume;

        [SerializeField, FormerlySerializedAs("m_FrameSettings")]
        FrameSettings m_LegacyFrameSettings = null;

        [SerializeField, FormerlySerializedAs("m_Multiplier"), FormerlySerializedAs("dimmer"), FormerlySerializedAs("m_Dimmer"), FormerlySerializedAs("multiplier")]
        float m_LegacyMultiplier = 1.0f;
        [SerializeField, FormerlySerializedAs("m_Weight"), FormerlySerializedAs("weight")]
        [Range(0.0f, 1.0f)]
        float m_LegacyWeight = 1.0f;

        [SerializeField, FormerlySerializedAs("m_Mode")]
        ProbeSettings.Mode m_LegacyMode = ProbeSettings.Mode.Baked;

        [SerializeField, FormerlySerializedAs("lightLayer")]
        LightLayerEnum m_LegacyLightLayers = LightLayerEnum.LightLayerDefault;
    }
}
