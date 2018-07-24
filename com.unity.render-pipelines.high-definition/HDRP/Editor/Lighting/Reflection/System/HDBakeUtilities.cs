using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class HDBakeUtilities
    {
        public const string BakedTextureExtension = ".exr";

        // get a bake path in the cache folder for object in a scene
        public static string GetCacheBakePath(string scenePath, Hash128 hash, string ext)
        {
            return Path.Combine("Temp/Cache/" + scenePath, hash.ToString() + ext);
        }

        internal static void WriteBakedTextureTo(Texture renderTarget, string cacheFilePath)
        {
            var rt = renderTarget as RenderTexture;
            if (rt != null && rt.dimension == TextureDimension.Cube)
            {
                var t2D = CopyCubemapToTexture2D(rt);
                var bytes = t2D.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                File.WriteAllBytes(cacheFilePath, bytes);
            }
            throw new ArgumentException();
        }

        /// <summary>
        /// Export a cubemap to a texture2D.
        /// 
        /// The Texture2D size is (size * 6, size) and the layout is +X,-X,+Y,-Y,+Z,-Z
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Texture2D CopyCubemapToTexture2D(RenderTexture source)
        {
            Assert.AreEqual(TextureDimension.Cube, source.dimension);
            TextureFormat format = TextureFormat.RGBAFloat;
            switch (source.format)
            {
                case RenderTextureFormat.ARGBFloat: format = TextureFormat.RGBAFloat; break;
                case RenderTextureFormat.ARGBHalf: format = TextureFormat.RGBAHalf; break;
                default:
                    Assert.IsFalse(true, "Unmanaged format");
                    break;
            }

            var resolution = source.width;

            var result = new Texture2D(resolution * 6, resolution, format, false);

            var offset = 0;
            for (var i = 0; i< 6; ++i)
            {
                Graphics.SetRenderTarget(source, 0, (CubemapFace) i);
                result.ReadPixels(new Rect(0, 0, resolution, resolution), offset, 0);
                result.Apply();
                offset += resolution;
            }
            Graphics.SetRenderTarget(null);

            return result;
        }

        public static Texture2D FlipY(Texture2D source)
        {
            var result = new Texture2D(source.width, source.height, source.format, false);

            var rtFormat = TextureFormatUtilities.GetUncompressedRenderTextureFormat(source);
            var tempRT = new RenderTexture(source.width, source.height, 0, rtFormat, RenderTextureReadWrite.Linear)
            {
                dimension = TextureDimension.Tex2D,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Trilinear
            };
            tempRT.Create();

            // Flip texture.
            UnityEngine.Graphics.Blit(source, tempRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 0.0f));

            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();

            Graphics.SetRenderTarget(null);
            CoreUtils.Destroy(tempRT);

            return result;
        }
    }
}
