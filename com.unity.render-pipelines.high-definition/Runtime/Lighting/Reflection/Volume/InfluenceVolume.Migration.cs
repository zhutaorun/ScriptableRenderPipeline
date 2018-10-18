using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class InfluenceVolume : IVersionable<InfluenceVolume.Version>, ISerializationCallbackReceiver
    {
        enum Version
        {
            Initial,
            SphereOffset
        }

        static readonly MigrationDescription<Version, InfluenceVolume> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SphereOffset, (InfluenceVolume i) =>
            {
                if (i.shape == InfluenceShape.Sphere)
                {
#pragma warning disable CS0618
                    i.m_ObsoleteOffset = i.m_ObsoleteSphereBaseOffset;
#pragma warning restore CS0618
                }
            })
        );

        [SerializeField]
        Version m_Version;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        // Obsolete fields
#pragma warning disable 649 //never assigned
        [SerializeField, FormerlySerializedAs("m_SphereBaseOffset"), Obsolete("For Data Migration")]
        Vector3 m_ObsoleteSphereBaseOffset;
        [SerializeField, FormerlySerializedAs("m_BoxBaseOffset"), FormerlySerializedAs("m_Offset")]
        Vector3 m_ObsoleteOffset;
#pragma warning restore 649 //never assigned

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() => k_Migration.Migrate(this);
    }
}
