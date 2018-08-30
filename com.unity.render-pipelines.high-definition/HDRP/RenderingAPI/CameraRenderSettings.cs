using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct CameraRenderSettings
    {
        public struct Position
        {
            public enum Mode
            {
                ComputeWorldToCameraMatrix,
                UseWorldToCameraMatrixField
            }

            public Mode mode;

            public Vector3 position;
            public Quaternion rotation;

            public Matrix4x4 worldToCameraMatrix;

            public Matrix4x4 ComputeWorldToCameraMatrix()
            {
                return GeometryUtils.CalculateWorldToCameraMatrixRHS(position, rotation);
            }
        }

        public CameraSettings camera;
        public Position position;
    }
}
