using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    struct HDReflectionEntityManager2
    {
        public int BakedProbeCount { get; internal set; }

        internal IEnumerator<HDProbe2> GetActiveBakedProbeEnumerator()
        {
            throw new NotImplementedException();
        }

        internal IEnumerator<HDProbe2> GetActiveCustomProbeEnumerator()
        {
            throw new NotImplementedException();
        }

        public HDProbe2 GetProbeByID(int instanceId)
        {
            return (HDProbe2)EditorUtility.InstanceIDToObject(instanceId);
        }
    }
}
