using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public interface IVersionable<TVersion>
        where TVersion : struct, IConvertible
    {
        TVersion Version { get; set; }
    }
}
