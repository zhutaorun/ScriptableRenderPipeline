using UnityEditor;
using UnityEditor.Experimental.Rendering;

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
                                var target = new Cubemap(cubemapSize, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                                var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                                HDRenderUtilities.Render(probe.settings, positionSettings, target);
                                throw new System.NotImplementedException("save asset and link to reflection probe component")
                                break;
                            }
                    }
                }
            }
            

            handle.EnterStage((int)BakingStages.ReflectionProbes, "", 0);
            handle.ExitStage((int)BakingStages.ReflectionProbes);
        }
    }
}
