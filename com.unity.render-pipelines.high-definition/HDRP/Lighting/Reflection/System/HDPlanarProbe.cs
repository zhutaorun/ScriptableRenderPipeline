using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDPlanarProbe : HDProbe2
    {
        public enum CapturePositionMode
        {
            Static,
            Mirrored
        }

        public struct PlanarCaptureProperties
        {
            public CaptureProperties common;
            public CapturePositionMode capturePositionMode;
        }

        public PlanarCaptureProperties captureSettings;

        public override Hash128 ComputeBakePropertyHashes()
        {
            throw new NotImplementedException();
        }
    }
}
