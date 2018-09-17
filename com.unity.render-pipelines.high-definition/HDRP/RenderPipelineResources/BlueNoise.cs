namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public sealed class BlueNoise
    {
        public Texture2D[] textures64 { get; private set; }
        public Texture2DArray textureArray64 { get; private set; }

        public BlueNoise(HDRenderPipelineAsset asset)
        {
            var resources = asset.renderPipelineResources;

            int len = resources.blueNoise64Texture.Length;
            textures64 = new Texture2D[len];
            textureArray64 = new Texture2DArray(64, 64, len, TextureFormat.Alpha8, false, true);
            textureArray64.hideFlags = HideFlags.HideAndDontSave;

            for (int i = 0; i < len; i++)
            {
                var noiseTex = resources.blueNoise64Texture[i];

                // Fail safe; should never happen unless the resources asset is broken
                if (noiseTex == null)
                {
                    textures64[i] = Texture2D.whiteTexture;
                    continue;
                }

                textures64[i] = noiseTex;
                Graphics.CopyTexture(noiseTex, 0, 0, textureArray64, i, 0);
            }
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(textureArray64);
            textureArray64 = null;
        }

        public Texture2D GetRandom64()
        {
            return textures64[(int)(Random.value * (textures64.Length - 1))];
        }
    }
}
