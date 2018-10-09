using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public sealed partial class HDAdditionalReflectionData : IVersionable<HDAdditionalReflectionData.Version>
    {
        enum Version
        {
            First,
            HDProbeChild = 2,
            UseInfluenceVolume,
            MergeEditors,
            AddCaptureSettingsAndFrameSettings,
            ProbeSettings
        }

        static readonly MigrationDescription<Version, HDAdditionalReflectionData> k_Migration
            = MigrationDescription.New(
                MigrationStep.New(Version.UseInfluenceVolume, (HDAdditionalReflectionData t) =>
                {
                    t.influenceVolume.boxSize = t.reflectionProbe.size;
#pragma warning disable CS0618 // Type or member is obsolete
                    t.influenceVolume.sphereRadius = t.influenceSphereRadius;
                    t.influenceVolume.shape = t.influenceShape; //must be done after each size transfert
                    t.influenceVolume.boxBlendDistancePositive = t.blendDistancePositive;
                    t.influenceVolume.boxBlendDistanceNegative = t.blendDistanceNegative;
                    t.influenceVolume.boxBlendNormalDistancePositive = t.blendNormalDistancePositive;
                    t.influenceVolume.boxBlendNormalDistanceNegative = t.blendNormalDistanceNegative;
                    t.influenceVolume.boxSideFadePositive = t.boxSideFadePositive;
                    t.influenceVolume.boxSideFadeNegative = t.boxSideFadeNegative;
#pragma warning restore CS0618 // Type or member is obsolete
                    //Note: former editor parameters will be recreated as if non existent.
                    //User will lose parameters corresponding to non used mode between simplified and advanced
                }),
                MigrationStep.New(Version.MergeEditors, (HDAdditionalReflectionData t) =>
                {
                    t.m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume
                        = t.reflectionProbe.boxProjection;
                    t.reflectionProbe.boxProjection = false;
                }),
                MigrationStep.New(Version.AddCaptureSettingsAndFrameSettings, (HDAdditionalReflectionData t) =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    t.m_ObsoleteCaptureSettings.shadowDistance = t.reflectionProbe.shadowDistance;
                    t.m_ObsoleteCaptureSettings.cullingMask = t.reflectionProbe.cullingMask;
#if UNITY_EDITOR //m_UseOcclusionCulling is not exposed in c# !
                    var serializedReflectionProbe = new UnityEditor.SerializedObject(t.reflectionProbe);
                    t.m_ObsoleteCaptureSettings.useOcclusionCulling = serializedReflectionProbe.FindProperty("m_UseOcclusionCulling").boolValue;
#endif
                    t.m_ObsoleteCaptureSettings.nearClipPlane = t.reflectionProbe.nearClipPlane;
                    t.m_ObsoleteCaptureSettings.farClipPlane = t.reflectionProbe.farClipPlane;
#pragma warning restore CS0618 // Type or member is obsolete
                }),
                MigrationStep.New(Version.ProbeSettings, (HDAdditionalReflectionData t) =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    // TODO: shadow distance is not yet handled
                    // ?? = t.m_ObsoleteCaptureSettings.shadowDistance;
                    t.m_ProbeSettings.camera.culling.cullingMask = t.m_ObsoleteCaptureSettings.cullingMask;
                    t.m_ProbeSettings.camera.culling.useOcclusionCulling = t.m_ObsoleteCaptureSettings.useOcclusionCulling;
                    t.m_ProbeSettings.camera.frustum.nearClipPlane = t.m_ObsoleteCaptureSettings.nearClipPlane;
                    t.m_ProbeSettings.camera.frustum.farClipPlane = t.m_ObsoleteCaptureSettings.farClipPlane;
#pragma warning restore CS0618 // Type or member is obsolete
                })
            );

        [SerializeField, FormerlySerializedAs("version")]
        int m_ReflectionProbeVersion;
        Version IVersionable<Version>.version { get => (Version)m_ReflectionProbeVersion; set => m_ReflectionProbeVersion = (int)value; }

        #region Deprecated Fields
#pragma warning disable 649 //never assigned
        //data only kept for migration, to be removed in future version
        [SerializeField, System.Obsolete("influenceShape is deprecated, use influenceVolume parameters instead")]
        InfluenceShape influenceShape;
        [SerializeField, System.Obsolete("influenceSphereRadius is deprecated, use influenceVolume parameters instead")]
        float influenceSphereRadius = 3.0f;
        [SerializeField, System.Obsolete("blendDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 blendDistancePositive = Vector3.zero;
        [SerializeField, System.Obsolete("blendDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 blendDistanceNegative = Vector3.zero;
        [SerializeField, System.Obsolete("blendNormalDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 blendNormalDistancePositive = Vector3.zero;
        [SerializeField, System.Obsolete("blendNormalDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 blendNormalDistanceNegative = Vector3.zero;
        [SerializeField, System.Obsolete("boxSideFadePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 boxSideFadePositive = Vector3.one;
        [SerializeField, System.Obsolete("boxSideFadeNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 boxSideFadeNegative = Vector3.one;
        #pragma warning restore 649 //never assigned
        #endregion
    }
}
