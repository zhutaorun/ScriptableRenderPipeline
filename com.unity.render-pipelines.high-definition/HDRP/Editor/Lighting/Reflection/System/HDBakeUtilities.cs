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
        // get a bake path in the cache folder for object in a scene
        public static string GetCacheBakePath(string scenePath, Hash128 hash, string ext)
        {
            return Path.Combine("Temp/Cache/" + scenePath, hash.ToString() + ext);
        }

        internal static void WriteBakedTextureTo(Texture renderTarget, string cacheFilePath)
        {
            var rt = renderTarget as RenderTexture;
            if (rt != null)
            {
                var t2D = CopyRenderTargetToTexture2D(rt);
                var bytes = t2D.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                CreateParentDirectoryIfMissing(cacheFilePath);
                File.WriteAllBytes(cacheFilePath, bytes);
                return;
            }
            throw new ArgumentException();
        }

        /// <summary>
        /// Export a render texture to a texture2D.
        ///
        /// <list type="bullet">
        /// <item>Cubemap will be exported in a Texture2D of size (size * 6, size) and with a layout +X,-X,+Y,-Y,+Z,-Z</item>
        /// <item>Texture2D will be copied to a Texture2D</item>
        /// </list>
        /// </summary>
        /// <param name="source"></param>
        /// <returns>The copied Texture2D.</returns>
        public static Texture2D CopyRenderTargetToTexture2D(RenderTexture source)
        {
            TextureFormat format = TextureFormat.RGBAFloat;
            switch (source.format)
            {
                case RenderTextureFormat.ARGBFloat: format = TextureFormat.RGBAFloat; break;
                case RenderTextureFormat.ARGBHalf: format = TextureFormat.RGBAHalf; break;
                default:
                    Assert.IsFalse(true, "Unmanaged format");
                    break;
            }

            switch (source.dimension)
            {
                case TextureDimension.Cube:
                    {
                        var resolution = source.width;
                        var result = new Texture2D(resolution * 6, resolution, format, false);

                        var offset = 0;
                        for (var i = 0; i < 6; ++i)
                        {
                            Graphics.SetRenderTarget(source, 0, (CubemapFace)i);
                            result.ReadPixels(new Rect(0, 0, resolution, resolution), offset, 0);
                            result.Apply();
                            offset += resolution;
                        }
                        Graphics.SetRenderTarget(null);

                        return result;
                    }
                case TextureDimension.Tex2D:
                    {
                        var resolution = source.width;
                        var result = new Texture2D(resolution, resolution, format, false);

                        Graphics.SetRenderTarget(source, 0);
                        result.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                        result.Apply();
                        Graphics.SetRenderTarget(null);

                        return result;
                    }
                default:
                    throw new ArgumentException();
            }
        }

        static void CreateParentDirectoryIfMissing(string path)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
        }

        static Texture2D FlipY(Texture2D source)
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
