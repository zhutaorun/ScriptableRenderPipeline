using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    /// <summary>Contains the logic to bake all reflection probes.</summary>
    unsafe struct HDReflectionSystemBakeAllReflectionProbes
    {
        // Injected resources by callee
        public HDReflectionEntitySystem entitySystem;
        public ReflectionSettings settings;
        public HDProbeRenderer renderer;
        public HDProbeTextureImporter textureImporter;

        public bool BakeAllReflectionProbes()
        {
            var bakedProbes = entitySystem.GetActiveBakedProbes();
            for (int i = 0; i < bakedProbes.Length; ++i)
            {
                try
                {
                    var probe = bakedProbes[i];
                    var probeScene = probe.gameObject.scene;
                    var bakedPath = textureImporter.GetBakedPathFor(probe);

                    EditorUtility.DisplayProgressBar(
                        "Baking Probes",
                        string.Format("Baking Probes ({0}/{1}) {2}", i + 1, bakedProbes.Length, bakedPath),
                        (i + 1.0f) / bakedProbes.Length
                    );

                    // Render probe
                    var renderTarget = HDProbeRendererUtilities.CreateRenderTarget(probe);
                    RenderData renderData;
                    renderer.Render(probe, renderTarget, null, out renderData);

                    // Save rendered texture to disk
                    HDBakeUtilities.WriteBakedTextureTo(renderTarget, bakedPath);
                    CoreUtils.Destroy(renderTarget);
                    AssetDatabase.ImportAsset(bakedPath);

                    // Import baked texture
                    var bakedTexture = textureImporter.ImportBakedTextureFromAssetPath(
                        probe,
                        bakedPath
                    );

                    // Set baked texture in lighting asset
                    var lightingAsset = HDLightingSceneAsset.GetOrCreateForScene(probeScene);
                    lightingAsset.SetBakedTextureFor(probe, bakedTexture, renderData);
                    probe.bakedTexture = bakedTexture;
                    probe.bakedRenderData = renderData;
                    EditorUtility.SetDirty(lightingAsset);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayProgressBar(string.Empty, string.Empty, 1.0f);
                }
            }

            EditorUtility.ClearProgressBar();

            return true;
        }
    }
}
