using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public partial class HDAdditionalReflectionData : IVersionable<HDAdditionalReflectionData.Version>
    {
        enum Version
        {
            First,
            HDProbeChild = 2,
            UseInfluenceVolume,
            MergeEditors
        }

        static readonly MigrationDescription<Version, HDAdditionalReflectionData> k_MigrationDescription
            = MigrationDescription.New(
                MigrationStep.New(Version.UseInfluenceVolume, (HDAdditionalReflectionData target) =>
                {
                    target.influenceVolume.boxSize = target.reflectionProbe.size;
#pragma warning disable CS0618 // Type or member is obsolete
                    target.influenceVolume.sphereRadius = target.influenceSphereRadius;
                    target.influenceVolume.shape = target.influenceShape; //must be done after each size transfert
                    target.influenceVolume.boxBlendDistancePositive = target.blendDistancePositive;
                    target.influenceVolume.boxBlendDistanceNegative = target.blendDistanceNegative;
                    target.influenceVolume.boxBlendNormalDistancePositive = target.blendNormalDistancePositive;
                    target.influenceVolume.boxBlendNormalDistanceNegative = target.blendNormalDistanceNegative;
                    target.influenceVolume.boxSideFadePositive = target.boxSideFadePositive;
                    target.influenceVolume.boxSideFadeNegative = target.boxSideFadeNegative;
#pragma warning restore CS0618 // Type or member is obsolete
                    //Note: former editor parameters will be recreated as if non existent.
                    //User will lose parameters corresponding to non used mode between simplified and advanced
                }),
                MigrationStep.New(Version.MergeEditors, (HDAdditionalReflectionData target) =>
                {
                    target.m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume
                        = target.reflectionProbe.boxProjection;
                    target.reflectionProbe.boxProjection = false;
                })
            );

        [SerializeField, FormerlySerializedAs("version")]
        int m_Version;
        Version IVersionable<Version>.version { get => (Version)m_Version; set => m_Version = (int)value; }

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
