using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct ProbeSettings
    {
        public enum ProbeType
        {
            ReflectionProbe,
            PlanarProbe
        }

        public enum Mode
        {
            Baked,
            Custom,
            Realtime
        }

        public struct Lighting
        {
            public float multiplier;
            public float weight;
        }

        public ProbeType type;
        public Mode mode;
        public Lighting lighting;
        public InfluenceVolume influence;
        public ProxyVolume proxy;
        public CameraSettings camera;
    }

    [Serializable]
    public struct ProbeCapturePositionSettings
    {
        public static readonly ProbeCapturePositionSettings @default = new ProbeCapturePositionSettings(
            Vector3.zero, Quaternion.identity,
            Vector3.zero, Quaternion.identity
        );

        public Vector3 probePosition;
        public Quaternion probeRotation;
        public Vector3 referencePosition;
        public Quaternion referenceRotation;

        public ProbeCapturePositionSettings(
            Vector3 probePosition,
            Quaternion probeRotation
        )
        {
            this.probePosition = probePosition;
            this.probeRotation = probeRotation;
            referencePosition = Vector3.zero;
            referenceRotation = Quaternion.identity;
        }

        public ProbeCapturePositionSettings(
            Vector3 probePosition,
            Quaternion probeRotation,
            Vector3 referencePosition,
            Quaternion referenceRotation
        )
        {
            this.probePosition = probePosition;
            this.probeRotation = probeRotation;
            this.referencePosition = referencePosition;
            this.referenceRotation = referenceRotation;
        }
    }

    public struct HDProbeRenderer
    {
        HDCameraRenderer renderer;

        public void Render(ProbeSettings settings, ProbeCapturePositionSettings position, Texture target)
        {
            // Copy settings
            var cameraSettings = settings.camera;
            var cameraPositionSettings = CameraPositionSettings.@default;

            ProbeRenderSettingsUtilities.UpdateSettings(
                ref settings, ref position,
                ref cameraSettings, ref cameraPositionSettings
            );

            renderer.Render(cameraSettings, cameraPositionSettings, target);
        }
    }

    public static class ProbeRenderSettingsUtilities
    {
        enum PositionMode
        {
            UseProbeTransform,
            MirrorReferenceTransfromWithProbePlane
        }

        public static void UpdateSettings(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition               // InOut parameter
        )
        {
            // Compute the modes for each probe type
            PositionMode positionMode;
            bool useReferenceTransformAsNearClipPlane;
            switch (settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                    positionMode = PositionMode.MirrorReferenceTransfromWithProbePlane;
                    useReferenceTransformAsNearClipPlane = true;
                    break;
                case ProbeSettings.ProbeType.ReflectionProbe:
                    positionMode = PositionMode.UseProbeTransform;
                    useReferenceTransformAsNearClipPlane = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update the position
            cameraPosition.mode = CameraPositionSettings.Mode.UseWorldToCameraMatrixField;
            switch (positionMode)
            {
                case PositionMode.UseProbeTransform:
                    cameraPosition.worldToCameraMatrix = GeometryUtils.CalculateWorldToCameraMatrixRHS(
                        probePosition.probePosition,
                        probePosition.probeRotation
                    );
                    break;
                case PositionMode.MirrorReferenceTransfromWithProbePlane:
                    {
                        var worldToCameraRHS = GeometryUtils.CalculateWorldToCameraMatrixRHS(
                            probePosition.referencePosition,
                            probePosition.referenceRotation
                        );
                        var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(
                            probePosition.probePosition,
                            probePosition.probeRotation * Vector3.forward
                        );
                        cameraPosition.worldToCameraMatrix = worldToCameraRHS * reflectionMatrix;
                        // We must invert the culling because we performed a plane reflection
                        cameraSettings.culling.invertCulling = true;
                        break;
                    }
            }

            // Update the clip plane
            if (useReferenceTransformAsNearClipPlane)
            {
                var clipPlaneCameraSpace = GeometryUtils.CameraSpacePlane(
                    cameraPosition.worldToCameraMatrix,
                    probePosition.probePosition,
                    probePosition.probeRotation * Vector3.forward
                );
                var sourceProjection = Matrix4x4.Perspective(
                    cameraSettings.frustum.fieldOfView,
                    cameraSettings.frustum.aspect,
                    cameraSettings.frustum.nearClipPlane,
                    cameraSettings.frustum.farClipPlane
                );
                var obliqueProjection = GeometryUtils.CalculateObliqueMatrix(
                    sourceProjection, clipPlaneCameraSpace
                );
                cameraSettings.frustum.mode = CameraSettings.Frustum.Mode.UseProjectionMatrixField;
                cameraSettings.frustum.projectionMatrix = obliqueProjection;
            }
        }
    }
}
