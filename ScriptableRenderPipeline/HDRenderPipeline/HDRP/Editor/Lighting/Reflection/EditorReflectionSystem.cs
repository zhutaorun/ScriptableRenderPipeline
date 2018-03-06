using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.Internal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static partial class EditorReflectionSystem
    {
        static int _Cubemap = Shader.PropertyToID("_Cubemap");
        static PlanarReflectionProbeBaker s_PlanarReflectionProbeBaker = new PlanarReflectionProbeBaker();
        static ReflectionProbeBaker s_ReflectionProbeBaker = new ReflectionProbeBaker();

        static MethodInfo k_Lightmapping_BakeAllReflectionProbesSnapshots = typeof(UnityEditor.Lightmapping).GetMethod("BakeAllReflectionProbesSnapshots", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeAllReflectionProbesSnapshots()
        {
            return (bool)k_Lightmapping_BakeAllReflectionProbesSnapshots.Invoke(null, new object[0]);
        }

        public static bool BakeReflectionProbeSnapshot(ReflectionProbe probe)
        {
            return BakeReflectionProbeSnapshot(
                probe,
                p => p.bakedTexture,
                (p, t) => p.bakedTexture = t,
                BakeReflectionProbe);
        }

        public static void ResetProbeSceneTextureInMaterial(ReflectionProbe p)
        {
            var renderer = p.GetComponent<Renderer>();
            renderer.sharedMaterial.SetTexture(_Cubemap, p.texture);
        }

        public static void BakeCustomReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath)
        {
            BakeCustomReflectionProbe(
                probe,
                usePreviousAssetPath,
                p => p.customBakedTexture,
                (p, t) => p.customBakedTexture = t,
                BakeReflectionProbe);
        }

        public static bool BakeReflectionProbe(ReflectionProbe probe, string assetPath)
        {
            // Probe rendering
            var target = s_ReflectionProbeBaker.NewRenderTarget(
                probe,
                ReflectionSystem.parameters.reflectionProbeSize
            );
            s_ReflectionProbeBaker.Render(probe, target);

            var isPathInProject = CoreEditorUtils.IsPathInProject(assetPath);
            TextureImporter textureImporter = null;
            if (isPathInProject)
            {
                var bakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
                if (bakedTexture == null)
                {
                    // Import a small texture to get the TextureImporter quickly
                    bakedTexture = new Cubemap(4, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                    AssetDatabase.CreateAsset(bakedTexture, assetPath);
                }

                // Setup importer
                textureImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                textureImporter.alphaSource = TextureImporterAlphaSource.None;
                textureImporter.sRGBTexture = false;
                textureImporter.mipmapEnabled = false;
                textureImporter.textureShape = TextureImporterShape.TextureCube;
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    textureImporter.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.reflectionCacheCompressed
                        ? TextureImporterCompression.Compressed
                        : TextureImporterCompression.Uncompressed;
                }
            }

            // Write texture into asset file and import
            var tex2D = CoreUtils.CopyCubemapToTexture2D(target);
            target.Release();
            var bytes = tex2D.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
            CoreUtils.Destroy(tex2D);
            File.WriteAllBytes(assetPath, bytes);

            if (isPathInProject)
                textureImporter.SaveAndReimport();

            return true;
        }

        public static bool BakeReflectionProbeSnapshot(PlanarReflectionProbe probe)
        {
            return BakeReflectionProbeSnapshot(
                probe,
                p => p.bakedTexture,
                (p, t) => p.bakedTexture = t,
                BakeReflectionProbe);
        }

        public static void BakeCustomReflectionProbe(PlanarReflectionProbe probe, bool usePreviousAssetPath)
        {
            BakeCustomReflectionProbe(
                probe,
                usePreviousAssetPath,
                p => p.customTexture,
                (p, t) => p.customTexture = t,
                BakeReflectionProbe);
        }

        public static void ResetProbeSceneTextureInMaterial(PlanarReflectionProbe p)
        {
        }

        public static bool BakeReflectionProbe(PlanarReflectionProbe probe, string assetPath)
        {
            // Probe rendering
            var target = s_PlanarReflectionProbeBaker.NewRenderTarget(
                probe,
                ReflectionSystem.parameters.planarReflectionProbeSize
            );
            s_PlanarReflectionProbeBaker.Render(probe, target);

            var isPathInProject = CoreEditorUtils.IsPathInProject(assetPath);
            TextureImporter textureImporter = null;
            Texture2D bakedTexture = null;
            if (isPathInProject)
            {
                bakedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (bakedTexture == null)
                {
                    // Import a small texture to get the TextureImporter quickly
                    bakedTexture = new Texture2D(4, 4, TextureFormat.RGBAHalf, true, false);
                    AssetDatabase.CreateAsset(bakedTexture, assetPath);
                }

                // Setup importer
                textureImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                textureImporter.alphaSource = TextureImporterAlphaSource.None;
                textureImporter.sRGBTexture = false;
                textureImporter.mipmapEnabled = false;
                textureImporter.textureShape = TextureImporterShape.Texture2D;
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    textureImporter.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed
                        ? TextureImporterCompression.Compressed
                        : TextureImporterCompression.Uncompressed;
                }
            }

            // Write texture into asset file and import
            var art = RenderTexture.active;
            RenderTexture.active = target;
            bakedTexture = new Texture2D(target.width, target.height, TextureFormat.RGBAHalf, true, false);
            bakedTexture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
            RenderTexture.active = art;
            var bytes = bakedTexture.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
            CoreUtils.Destroy(bakedTexture);
            File.WriteAllBytes(assetPath, bytes);

            if (isPathInProject)
                textureImporter.SaveAndReimport();

            return true;
        }

        static bool BakeReflectionProbeSnapshot<TProbe>(
            TProbe probe,
            Func<TProbe, Texture> bakedTextureGetter,
            Action<TProbe, Texture> bakedTextureSetter,
            Func<TProbe, string, bool> baker)
            where TProbe : Component
        {
            // Get asset path
            var bakedTexture = bakedTextureGetter(probe);

            var assetPath = string.Empty;
            if (bakedTexture != null)
                assetPath = AssetDatabase.GetAssetPath(bakedTexture);
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = GetBakePath(probe);
                bakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
            }

            baker(probe, assetPath);
            bakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);

            // Assign texture
            bakedTextureSetter(probe, bakedTexture);
            EditorUtility.SetDirty(probe);

            // Register baking information
            var id = SceneObjectIdentifierUtils.GetSceneObjectIdentifierFor(probe);
            var asset = EditorHDLightingDataAsset.GetOrCreateLightingDataAssetForScene(probe.gameObject.scene);
            if (id != SceneObjectIdentifier.Invalid && asset != null)
            {
                asset.SetBakedTexture(id, bakedTexture);
                EditorUtility.SetDirty(asset);
            }

            return true;
        }

        static void BakeCustomReflectionProbe<TProbe>(
            TProbe probe,
            bool usePreviousAssetPath,
            Func<TProbe, Texture> customTextureGetter,
            Action<TProbe, Texture> customTextureSetter,
            Func<TProbe, string, bool> baker)
            where TProbe : Component
        {
            string path;
            if (!TryGetCustomBakePath(probe, customTextureGetter(probe), usePreviousAssetPath, out path))
                return;

            TProbe collidingProbe;
            if (IsCollidingWithOtherProbes(path, probe, customTextureGetter, out collidingProbe))
            {
                if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                    string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                        path, collidingProbe.name), "Yes", "No"))
                    return;
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!baker(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            else
            {
                var bakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                customTextureSetter(probe, bakedTexture);
                EditorUtility.SetDirty(probe);
            }
            EditorUtility.ClearProgressBar();
        }

        static string GetBakePath(Component probe)
        {
            var id = SceneObjectIdentifierUtils.GetSceneObjectIdentifierFor(probe);
            if (id == SceneObjectIdentifier.Invalid)
                return string.Empty;

            var scene = probe.gameObject.scene;

            var targetPath = GetSceneBakeDirectoryPath(scene);
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            var fileName = string.Format("{0} {1}.exr", probe.GetType().Name, id);
            return Path.Combine(targetPath, fileName);
        }

        static bool TryGetCustomBakePath(Component probe, Texture customBakedTexture, bool usePreviousAssetPath, out string path)
        {
            var id = SceneObjectIdentifierUtils.GetSceneObjectIdentifierFor(probe);
            if (id == SceneObjectIdentifier.Invalid)
            {
                path = string.Empty;
                return false;
            }

            path = "";
            if (usePreviousAssetPath)
                path = AssetDatabase.GetAssetPath(customBakedTexture);

            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != ".exr")
            {
                // We use the path of the active scene as the target path
                var targetPath = GetSceneBakeDirectoryPath(SceneManager.GetActiveScene());
                if (Directory.Exists(targetPath) == false)
                    Directory.CreateDirectory(targetPath);

                var fileName = string.Format("{0} {1}.exr", probe.GetType().Name, id);
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName)));

                path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, "exr", "", targetPath);
                if (string.IsNullOrEmpty(path))
                    return false;
            }
            return true;
        }

        static string GetSceneBakeDirectoryPath(Scene scene)
        {
            var targetPath = scene.path;
            targetPath = Path.Combine(Path.GetDirectoryName(targetPath), Path.GetFileNameWithoutExtension(targetPath));
            if (string.IsNullOrEmpty(targetPath))
                targetPath = "Assets";
            else if (Directory.Exists(targetPath) == false)
                Directory.CreateDirectory(targetPath);
            return targetPath;
        }

        static bool IsCollidingWithOtherProbes<TType>(string targetPath, TType targetProbe, Func<TType, Texture> textureGetter, out TType collidingProbe)
            where TType : Object
        {
            var probes = Object.FindObjectsOfType<TType>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe)
                    continue;
                var customTexture = textureGetter(probe);
                if (customTexture == null)
                    continue;
                var path = AssetDatabase.GetAssetPath(customTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }
    }
}
