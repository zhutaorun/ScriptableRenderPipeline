using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct MigrationDescription<TVersion, TTarget>
        where TVersion : struct, IConvertible
        where TTarget : class, IVersionable<TVersion>
    {
        readonly MigrationStep<TVersion, TTarget>[] Steps;

        public MigrationDescription(params MigrationStep<TVersion, TTarget>[] steps)
        {
            // Sort by version
            Array.Sort(steps, (l, r) => (int)(object)l.Version - (int)(object)r.Version);
            Steps = steps;
        }

        public void Migrate(TTarget target)
        {
            if ((int)(object)target.Version == (int)(object)Steps[Steps.Length - 1].Version)
                return;

            for (int i = 0; i < Steps.Length; ++i)
            {
                if ((int)(object)target.Version < (int)(object)Steps[i].Version)
                {
                    Steps[i].Migrate(target);
                    target.Version = Steps[i].Version;
                }
            }
        }
    }
}
