namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct ReflectionSystemParameters
    {
        public static ReflectionSystemParameters Default = new ReflectionSystemParameters
        {
            maxPlanarReflectionProbes = 128,
            planarReflectionProbeSize = 128,
            maxReflectionProbes = 128,
            reflectionProbeSize = 128
        };

        public int maxPlanarReflectionProbes;
        public int planarReflectionProbeSize;
        public int maxReflectionProbes;
        public int reflectionProbeSize;
    }
}
