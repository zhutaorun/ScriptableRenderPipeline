using NUnit.Framework;
using System;

using static UnityEngine.Experimental.Rendering.HDPipeline.Tests.Utilities;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public class HDCameraRendererTests
    {
        HDCameraRenderer renderer;

        [Test]
        public void HDCameraRendererThrowNullForTarget()
        {
            Assert.Throws<ArgumentNullException>(() => renderer.Render(default(CameraRenderSettings), null));
        }

        [Test]
        public void HDCameraRendererThrowNullForFrameSettings()
        {
            Assert.Throws<ArgumentNullException>(() => renderer.Render(default(CameraRenderSettings), Texture2D.white));
        }
    }
}
