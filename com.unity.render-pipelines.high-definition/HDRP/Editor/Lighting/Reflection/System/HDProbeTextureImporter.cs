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
            Texture ImportBakedTextureFromFile(T probe, string pathInCache, string pathInAssets);
            string GetBakedPathFor(T probe);
            string GetCacheBakePathFor(T probe, Hash128 hash);
        }

        struct HDPlanarTextureImporter : IProbeTextureImporter<PlanarReflectionProbe>
        {
            public string GetBakedPathFor(PlanarReflectionProbe probe)
            {
                throw new NotImplementedException();
            }

            public string GetCacheBakePathFor(PlanarReflectionProbe probe, Hash128 hash)
            {
                throw new NotImplementedException();
            }

            public Texture ImportBakedTextureFromFile(PlanarReflectionProbe probe, string pathInCache, string pathInAssets)
            {
                throw new NotImplementedException();
            }
        }

        struct HDReflectionProbeTextureImporter : IProbeTextureImporter<HDAdditionalReflectionData>
        {
            public string GetBakedPathFor(HDAdditionalReflectionData probe)
            {
                return Path.Combine(probe.gameObject.scene.path, probe.name + ".exr");
            }

            public string GetCacheBakePathFor(HDAdditionalReflectionData probe, Hash128 hash)
            {
                var bakedTexturePathInCache = HDBakeUtilities.GetCacheBakePath(
                    probe.gameObject.scene.path,
                    hash,
                    ".exr"
                );

                return bakedTexturePathInCache;
            }

            public Texture ImportBakedTextureFromFile(HDAdditionalReflectionData probe, string pathInCache, string pathInAssets)
            {
                Assert.IsTrue(File.Exists(pathInCache));
                Assert.IsTrue(File.Exists(pathInAssets));

                File.Copy(pathInCache, pathInAssets);
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

        internal string GetCacheBakePathFor(HDProbe probe, Hash128 bakedOutputHash)
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
                return m_ReflectionProbe.GetCacheBakePathFor(standard, bakedOutputHash);
            if (planar != null)
                return m_Planar.GetCacheBakePathFor(planar, bakedOutputHash);

            throw new ArgumentException();
        }

        internal Texture ImportBakedTextureFromFile(
            HDProbe probe,
            string bakedTexturePathInCache,
            string bakedPath
        )
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
                return m_ReflectionProbe.ImportBakedTextureFromFile(standard, bakedTexturePathInCache, bakedPath);
            if (planar != null)
                return m_Planar.ImportBakedTextureFromFile(planar, bakedTexturePathInCache, bakedPath);

            throw new ArgumentException();
        }
    }
}
