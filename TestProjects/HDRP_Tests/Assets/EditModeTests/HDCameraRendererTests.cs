using NUnit.Framework;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public class HDCameraRendererTests
    {
        HDCameraRenderer renderer;

        [Test]
        public void RenderThrowWhenTargetIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => renderer.Render(default(CameraRenderSettings), null));
        }

        [Test]
        public void RenderThrowWhenFrameSettingsIsNull()
        {
            Assert.Throws<ArgumentNullException>(()
                => renderer.Render(default(CameraRenderSettings), Texture2D.whiteTexture));
        }

        [Test]
        public void RenderThrowWhenTargetIsNotARenderTextureForTex2DRendering()
        {
            Assert.Throws<ArgumentException>(()
                => renderer.Render(
                    new CameraRenderSettings
                    {
                        camera = new CameraSettings { frameSettings = new FrameSettings() }
                    },
                    Texture2D.whiteTexture)
                );
        }
    }
}
