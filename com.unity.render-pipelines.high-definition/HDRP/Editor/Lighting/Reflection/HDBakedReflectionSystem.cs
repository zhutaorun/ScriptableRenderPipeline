using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    unsafe class HDBakedReflectionSystem : ScriptableBakedReflectionSystem
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            ScriptableBakedReflectionSystemSettings.system = new HDBakedReflectionSystem();
        }

        enum BakingStages
        {
            ReflectionProbes
        }

        HDBakedReflectionSystem() : base(1)
        {
        }

        public override void Tick(
            SceneStateHash sceneStateHash,
            IScriptableBakedReflectionSystemStageNotifier handle
        )
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline == null)
            {
                Debug.LogWarning("HDBakedReflectionSystem work with HDRP, " +
                    "please switch your render pipeline or use another reflection system");
                handle.ExitStage((int)BakingStages.ReflectionProbes);
                return;
            }

            handle.EnterStage((int)BakingStages.ReflectionProbes, "Baking Reflection Probes", 0);

            var bakedProbes = HDProbeSystem.bakedProbes;
            if (bakedProbes.Count > 0)
            {
                var cubemapSize = (int)hdPipeline.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
                for (int i = 0; i < bakedProbes.Count; ++i)
                {
                    var probe = bakedProbes[i];
                    var settings = probe.settings;
                    switch (settings.type)
                    {
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            {
                                var rt = new RenderTexture(cubemapSize, cubemapSize, 1)
                                {
                                    dimension = TextureDimension.Cube,
                                    useMipMap = false,
                                    autoGenerateMips = false,
                                    format = RenderTextureFormat.ARGBHalf,
                                    name = "Temporary Reflection Probe Target"
                                };
                                var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                                HDRenderUtilities.Render(probe.settings, positionSettings, rt);
                                var bakedTexture = CreateBakedTextureFromRenderTexture(rt, probe);
                                var reflectionProbe = probe.GetComponent<ReflectionProbe>();
                                reflectionProbe.bakedTexture = bakedTexture;
                                break;
                            }
                    }
                }
            }

            handle.ExitStage((int)BakingStages.ReflectionProbes);
            handle.SetIsDone(true);
        }

        static Texture CreateBakedTextureFromRenderTexture(RenderTexture rt, HDProbe probe)
        {
            Assert.IsNotNull(rt);
            Assert.IsNotNull(probe);

            var targetFile = HDBakingUtilities.GetBakedTextureFilePath(probe);
            HDBakingUtilities.CreateParentDirectoryIfMissing(targetFile);
            HDTextureUtilities.WriteTextureFileToDisk(rt, targetFile);

            AssetDatabase.ImportAsset(targetFile);

            var importer = (TextureImporter)AssetImporter.GetAtPath(targetFile);
            importer.filterMode = FilterMode.Bilinear;
            importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture>(targetFile);
        }
    }
}
