using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Utilities for <see cref="ProbeSettings"/></summary>
    public static class ProbeSettingsUtilities
    {
        internal enum PositionMode
        {
            UseProbeTransform,
            MirrorReferenceTransfromWithProbePlane
        }

        /// <summary>
        /// Apply <paramref name="settings"/> and <paramref name="probePosition"/> to
        /// <paramref name="cameraPosition"/> and <paramref name="cameraSettings"/>.
        /// </summary>
        /// <param name="settings">Settings to apply. (Read only)</param>
        /// <param name="probePosition">Position to apply. (Read only)</param>
        /// <param name="cameraSettings">Settings to update.</param>
        /// <param name="cameraPosition">Position to update.</param>
        public static void ApplySettings(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition               // InOut parameter
        )
        {
            cameraSettings = settings.camera;
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
                    cameraSettings.frustum.mode = CameraSettings.Frustum.Mode.ComputeProjectionMatrix;
                    cameraSettings.frustum.aspect = 1;
                    cameraSettings.frustum.fieldOfView = 90;
                    cameraSettings.flipYMode = SystemInfo.graphicsUVStartsAtTop
                        ? HDAdditionalCameraData.FlipYMode.ForceFlipY
                        : HDAdditionalCameraData.FlipYMode.Automatic;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update the position
            switch (positionMode)
            {
                case PositionMode.UseProbeTransform:
                    cameraPosition.mode = CameraPositionSettings.Mode.ComputeWorldToCameraMatrix;
                    cameraPosition.position = probePosition.proxyPosition;
                    cameraPosition.rotation = probePosition.proxyRotation;
                    break;
                case PositionMode.MirrorReferenceTransfromWithProbePlane:
                    cameraPosition.mode = CameraPositionSettings.Mode.UseWorldToCameraMatrixField;
                    ApplyMirroredReferenceTransform(
                        ref settings, ref probePosition,
                        ref cameraSettings, ref cameraPosition
                    );
                    break;
            }

            // Update the clip plane
            if (useReferenceTransformAsNearClipPlane)
            {
                ApplyObliqueNearClipPlane(
                    ref settings, ref probePosition,
                    ref cameraSettings, ref cameraPosition
                );
            }
        }

        internal static void ApplyMirroredReferenceTransform(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition               // InOut parameter
        )
        {
            var worldToCameraRHS = GeometryUtils.CalculateWorldToCameraMatrixRHS(
                probePosition.referencePosition,
                probePosition.referenceRotation
            );
            var proxyMatrix = Matrix4x4.TRS(probePosition.proxyPosition, probePosition.proxyRotation, Vector3.one);
            var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(
                proxyMatrix.MultiplyPoint(settings.proxySettings.capturePositionProxySpace),
                proxyMatrix.MultiplyVector(settings.proxySettings.captureRotationProxySpace * Vector3.forward)
            );
            cameraPosition.worldToCameraMatrix = worldToCameraRHS * reflectionMatrix;
            // We must invert the culling because we performed a plane reflection
            cameraSettings.culling.invertCulling = true;
        }

        internal static void ApplyObliqueNearClipPlane(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition               // InOut parameter
        )
        {
            var proxyMatrix = Matrix4x4.TRS(probePosition.proxyPosition, probePosition.proxyRotation, Vector3.one);

            var clipPlaneCameraSpace = GeometryUtils.CameraSpacePlane(
                cameraPosition.worldToCameraMatrix,
                proxyMatrix.MultiplyPoint(settings.proxySettings.capturePositionProxySpace),
                proxyMatrix.MultiplyVector(settings.proxySettings.captureRotationProxySpace * Vector3.forward)
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
