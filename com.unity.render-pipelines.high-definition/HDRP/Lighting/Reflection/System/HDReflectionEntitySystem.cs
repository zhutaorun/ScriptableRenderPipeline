using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDReflectionEntitySystem
    {
        static HDReflectionEntitySystem s_Instance = null;
        public static HDReflectionEntitySystem instance { get { return s_Instance; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            s_Instance = new HDReflectionEntitySystem();
        }

        public int BakedProbeCount { get; internal set; }

        internal IEnumerator<HDProbe> GetActiveBakedProbeEnumerator()
        {
            throw new NotImplementedException();
        }

        internal IEnumerator<HDProbe> GetActiveCustomProbeEnumerator()
        {
            throw new NotImplementedException();
        }

        internal void Register(HDProbe hDProbe)
        {
            throw new NotImplementedException();
        }

        internal void Unregister(HDProbe hDProbe)
        {
            throw new NotImplementedException();
        }
    }
}
