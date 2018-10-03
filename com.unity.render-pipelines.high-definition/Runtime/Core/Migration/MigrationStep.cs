using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct MigrationStep
    {
        public static MigrationStep<TVersion, TTarget> New<TVersion, TTarget>(TVersion version, Action<TTarget> action)
            where TVersion : struct, IConvertible
            where TTarget : class, IVersionable<TVersion>
        { return new MigrationStep<TVersion, TTarget>(version, action); }
    }

    public struct MigrationStep<TVersion, TTarget>
        where TVersion : struct, IConvertible
        where TTarget : class, IVersionable<TVersion>
    {
        public readonly TVersion Version;
        readonly Action<TTarget> m_MigrationAction;

        public MigrationStep(TVersion version, Action<TTarget> action)
        {
            Version = version;
            m_MigrationAction = action;
        }

        public void Migrate(TTarget target)
        {
            m_MigrationAction(target);
            target.Version = Version;
        }
    }
}
