using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    struct HDProbeTextureImporter
    {
        interface IProbeTextureImporter<T>
            where T : HDProbe
        {
            Texture ImportBakedTextureFromAssetPath(T probe, string pathInAssets);
            string GetBakedPathFor(T probe);
            void GetCacheBakePathFor(T probe, Hash128 hash, out string cachePath, out string cacheDataPath);
        }

        struct CommonTextureImporter
        {
            public static string GetBakedPathFor(HDProbe probe)
            {
                var scenePath = probe.gameObject.scene.path;
                var filename = Path.GetFileNameWithoutExtension(scenePath);
                var directoryName = Path.Combine(Path.GetDirectoryName(scenePath), filename);
                return Path.Combine(directoryName, probe.name + ".exr");
            }

            public static void GetCacheBakePathFor(
                HDProbe probe, Hash128 hash,
                out string cachePath, out string cacheDataPath
            )
            {
                cachePath = HDBakeUtilities.GetCacheBakePath(probe.gameObject.scene.path, hash, ".exr");
                cacheDataPath = HDBakeUtilities.GetCacheBakePath(probe.gameObject.scene.path, hash, "-data.exr");
            }
        }

        struct HDPlanarTextureImporter : IProbeTextureImporter<PlanarReflectionProbe>
        {
            public string GetBakedPathFor(PlanarReflectionProbe probe)
            {
                return CommonTextureImporter.GetBakedPathFor(probe);
            }

            public void GetCacheBakePathFor(
                PlanarReflectionProbe probe, Hash128 hash,
                out string cachePath, out string cacheDataPath
            )
            {
                CommonTextureImporter.GetCacheBakePathFor(probe, hash, out cachePath, out cacheDataPath);
            }

            public Texture ImportBakedTextureFromAssetPath(PlanarReflectionProbe probe, string pathInAssets)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(pathInAssets);
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();

                return AssetDatabase.LoadAssetAtPath<Texture2D>(pathInAssets);
            }
        }

        struct HDReflectionProbeTextureImporter : IProbeTextureImporter<HDAdditionalReflectionData>
        {
            public string GetBakedPathFor(HDAdditionalReflectionData probe)
            {
                return CommonTextureImporter.GetBakedPathFor(probe);
            }

            public void GetCacheBakePathFor(
                HDAdditionalReflectionData probe, Hash128 hash,
                out string cachePath, out string cacheDataPath
            )
            {
                CommonTextureImporter.GetCacheBakePathFor(probe, hash, out cachePath, out cacheDataPath);
            }

            public Texture ImportBakedTextureFromAssetPath(HDAdditionalReflectionData probe, string pathInAssets)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(pathInAssets);
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();

                return AssetDatabase.LoadAssetAtPath<Cubemap>(pathInAssets);
            }
        }

        HDPlanarTextureImporter m_Planar;
        HDReflectionProbeTextureImporter m_ReflectionProbe;

        internal string GetBakedPathFor(HDProbe probe)
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
                return m_ReflectionProbe.GetBakedPathFor(standard);
            if (planar != null)
                return m_Planar.GetBakedPathFor(planar);

            throw new ArgumentException();
        }

        internal void GetCacheBakePathFor(
            HDProbe probe, Hash128 bakedOutputHash,
            out string cacheFilePath, out string cacheDataFilePath
        )
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
            {
                m_ReflectionProbe.GetCacheBakePathFor(
                    standard, bakedOutputHash,
                    out cacheFilePath, out cacheDataFilePath
                );
                return;
            }
            if (planar != null)
            {
                m_Planar.GetCacheBakePathFor(
                    planar, bakedOutputHash,
                    out cacheFilePath, out cacheDataFilePath
                );
            }
            throw new ArgumentException();
        }

        internal Texture ImportBakedTextureFromFile(
            HDProbe probe,
            string pathInCache,
            string pathInAssets
        )
        {
            Assert.IsTrue(File.Exists(pathInCache));

            var targetFile = new FileInfo(pathInAssets);
            if (!targetFile.Directory.Exists)
                targetFile.Directory.Create();

            File.Copy(pathInCache, pathInAssets, true);

            return ImportBakedTextureFromAssetPath(probe, pathInAssets);
        }

        internal Texture ImportBakedTextureFromAssetPath(
            HDProbe probe,
            string pathInAssets
        )
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
                return m_ReflectionProbe.ImportBakedTextureFromAssetPath(standard, pathInAssets);
            if (planar != null)
                return m_Planar.ImportBakedTextureFromAssetPath(planar, pathInAssets);

            throw new ArgumentException();
        }
    }
}
