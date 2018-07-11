using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Must be kept in sync with variants defined in UberPost.compute
    [GenerateHLSL, Flags]
    public enum UberPostFeatureFlags
    {
        None,
        ChromaticAberration,
        Vignette
    }
}
