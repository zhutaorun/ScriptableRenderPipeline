using UnityEditor.Experimental.Rendering.TestFramework;
using NUnit.Framework;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public class CameraSettingsUtilitiesTests
    {
        [Test]
        public void ApplyCameraSettingsThrowNullFrameSettings()
        {
            var settings = new CameraSettings();
            var go = new GameObject();
            var cam = go.AddComponent<Camera>();

            Assert.Throws<InvalidOperationException>(() => cam.ApplySettings(settings));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyCameraSettings()
        {
            for (int i = 0; i < 10; ++i)
            {
                var perspectiveMatrix = Matrix4x4.Perspective(
                    RandomUtilities.RandomFloat(i, 2943.06587f) * 30.0f + 75.0f,
                    RandomUtilities.RandomFloat(i, 6402.79532f) * 0.5f + 1,
                    RandomUtilities.RandomFloat(i, 8328.97521f) * 10.0f + 10f,
                    RandomUtilities.RandomFloat(i, 6875.12374f) * 100.0f + 1000.0f
                );
                var worldToCameraMatrix = GeometryUtils.CalculateWorldToCameraMatrixRHS(
                    RandomUtilities.RandomVector3(i),
                    RandomUtilities.RandomQuaternion(i)
                );

                var settings = new CameraRenderSettings
                {
                    position = new CameraRenderSettings.Position
                    {
                        mode = RandomUtilities.RandomEnum<CameraRenderSettings.Position.Mode>(i),
                        position = RandomUtilities.RandomVector3(i * 5.5f),
                        rotation = RandomUtilities.RandomQuaternion(i * 6.5f),
                        worldToCameraMatrix = worldToCameraMatrix
                    },
                    camera = new CameraSettings
                    {
                        bufferClearing = new CameraSettings.BufferClearing
                        {
                            backgroundColorHDR = RandomUtilities.RandomColor(i),
                            clearColorMode = RandomUtilities.RandomEnum<HDAdditionalCameraData.ClearColorMode>(i),
                            clearDepth = RandomUtilities.RandomBool(i)
                        },
                        culling = new CameraSettings.Culling
                        {
                            cullingMask = RandomUtilities.RandomInt(i),
                            useOcclusionCulling = RandomUtilities.RandomBool(i + 0.5f),
                        },
                        frameSettings = new FrameSettings(),
                        frustum = new CameraSettings.Frustum
                        {
                            aspect = RandomUtilities.RandomFloat(i, 6724.2745f) * 0.5f + 1,
                            nearClipPlane = RandomUtilities.RandomFloat(i, 7634.7235f) * 10.0f + 10f,
                            farClipPlane = RandomUtilities.RandomFloat(i, 1935.3234f) * 100.0f + 1000.0f,
                            fieldOfView = RandomUtilities.RandomFloat(i, 9364.2534f) * 30.0f + 75.0f,
                            mode = RandomUtilities.RandomEnum<CameraSettings.Frustum.Mode>(i * 2.5f),
                            projectionMatrix = perspectiveMatrix
                        },
                        physical = new CameraSettings.Physical
                        {
                            iso = RandomUtilities.RandomFloat(i, 7253.02142f) * 10000 + 13000,
                            shutterSpeed = RandomUtilities.RandomFloat(i, 5601.1486f) * 0.3f + 0.5f,
                            aperture = RandomUtilities.RandomFloat(i, 82141.301f) * 0.3f + 0.5f
                        },
                        volumes = new CameraSettings.Volumes
                        {
                            volumeAnchorOverride = null,
                            volumeLayerMask = RandomUtilities.RandomInt(i * 3.5f)
                        },
                        renderingPath = RandomUtilities.RandomEnum<HDAdditionalCameraData.RenderingPath>(i * 4.5f)
                    }
                };

                var go = new GameObject("TestObject");
                var cam = go.AddComponent<Camera>();

                cam.ApplySettings(settings);

                var add = cam.GetComponent<HDAdditionalCameraData>();
                Assert.NotNull(add);

                // Position
                switch (settings.position.mode)
                {
                    case CameraRenderSettings.Position.Mode.UseWorldToCameraMatrixField:
                        AssertUtilities.AssertAreEqual(settings.position.worldToCameraMatrix, cam.worldToCameraMatrix);
                        break;
                    case CameraRenderSettings.Position.Mode.ComputeWorldToCameraMatrix:
                        AssertUtilities.AssertAreEqual(settings.position.position, cam.transform.position);
                        AssertUtilities.AssertAreEqual(settings.position.rotation, cam.transform.rotation);
                        AssertUtilities.AssertAreEqual(settings.position.ComputeWorldToCameraMatrix(), cam.worldToCameraMatrix);
                        break;
                }
                // Frustum
                switch (settings.camera.frustum.mode)
                {
                    case CameraSettings.Frustum.Mode.UseProjectionMatrixField:
                        AssertUtilities.AssertAreEqual(settings.camera.frustum.projectionMatrix, cam.projectionMatrix);
                        break;
                    case CameraSettings.Frustum.Mode.ComputeProjectionMatrix:
                        Assert.AreEqual(settings.camera.frustum.nearClipPlane, cam.nearClipPlane);
                        Assert.AreEqual(settings.camera.frustum.farClipPlane, cam.farClipPlane);
                        Assert.AreEqual(settings.camera.frustum.fieldOfView, cam.fieldOfView);
                        Assert.AreEqual(settings.camera.frustum.aspect, cam.aspect);
                        AssertUtilities.AssertAreEqual(settings.camera.frustum.ComputeProjectionMatrix(), cam.projectionMatrix);
                        break;
                }
                // Culling
                Assert.AreEqual(settings.camera.culling.useOcclusionCulling, cam.useOcclusionCulling);
                Assert.AreEqual(settings.camera.culling.cullingMask, cam.cullingMask);
                // Buffer clearing
                Assert.AreEqual(settings.camera.bufferClearing.clearColorMode, add.clearColorMode);
                Assert.AreEqual(settings.camera.bufferClearing.backgroundColorHDR, add.backgroundColorHDR);
                Assert.AreEqual(settings.camera.bufferClearing.clearDepth, add.clearDepth);
                // Volumes
                Assert.AreEqual(settings.camera.volumes.volumeLayerMask, add.volumeLayerMask);
                Assert.AreEqual(settings.camera.volumes.volumeAnchorOverride, add.volumeAnchorOverride);
                // Physical Parameters
                Assert.AreEqual(settings.camera.physical.aperture, add.aperture);
                Assert.AreEqual(settings.camera.physical.shutterSpeed, add.shutterSpeed);
                Assert.AreEqual(settings.camera.physical.iso, add.iso);
                // HD Specific
                Assert.AreEqual(settings.camera.renderingPath, add.renderingPath);

                Object.DestroyImmediate(go);
            }
        }
    }
}
