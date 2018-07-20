namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDReflectionProbe : HDProbe
    {
        public struct ProbeCaptureProperties
        {
            public CaptureProperties common;
        }

        public ProbeCaptureProperties captureSettings;

        public override Hash128 ComputeBakePropertyHashes()
        {
            var hash = new Hash128();
            HashUtilities.QuantisedVectorHash(ref captureSettings.common.position, ref hash);
            return hash;
        }
    }
}
