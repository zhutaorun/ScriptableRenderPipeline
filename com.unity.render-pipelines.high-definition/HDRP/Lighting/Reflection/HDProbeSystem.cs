using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDProbeSystem
    {
        static HDProbeSystemInternal s_Instance = new HDProbeSystemInternal();
        public static IList<HDProbe> bakedProbes { get { return s_Instance.bakedProbes; } }

        public static void RegisterProbe(HDProbe probe)
        {
            s_Instance.RegisterProbe(probe);
        }

        public static void UnregisterProbe(HDProbe probe)
        {
            s_Instance.UnregisterProbe(probe);
        }
    }

    class HDProbeSystemInternal
    {
        List<HDProbe> m_BakedProbes = new List<HDProbe>();

        public IList<HDProbe> bakedProbes { get { return m_BakedProbes; } }


        internal void RegisterProbe(HDProbe probe)
        {
            var settings = probe.settings;
            switch (settings.mode)
            {
                case ProbeSettings.Mode.Baked:
                    m_BakedProbes.Add(probe);
                    break;
            }
        }

        internal void UnregisterProbe(HDProbe probe)
        {
            m_BakedProbes.Remove(probe);
        }
    }
}
