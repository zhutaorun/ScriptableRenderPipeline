using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class ReflectionBakeJob : IDisposable
    {
        enum Stage
        {
            BakeReflectionProbe,
            BakePlanarProbe,
            Completed
        }

        delegate void BakingStage(ReflectionBakeJob job);

        static readonly BakingStage[] s_Stages =
        {
                StageBakeReflectionProbe,
                StageBakePlanarProbe,
            };

        Stage m_CurrentStage = Stage.BakeReflectionProbe;
        int m_StageIndex;

        public bool isComplete { get { return m_CurrentStage == Stage.Completed; } }
        public BakeReflectionProbeRequest request;
        public List<ReflectionProbe> reflectionProbesToBake = new List<ReflectionProbe>();
        public List<PlanarReflectionProbe> planarReflectionProbesToBake = new List<PlanarReflectionProbe>();

        public ReflectionBakeJob(BakeReflectionProbeRequest request)
        {
            this.request = request;
        }

        public void Tick()
        {
            if (m_StageIndex == -1 && m_CurrentStage != Stage.Completed)
            {
                m_CurrentStage = (Stage)((int)m_CurrentStage + 1);
                m_StageIndex = 0;
            }

            if (m_CurrentStage == Stage.Completed)
            {
                request.Progress = 1;
                return;
            }

            s_Stages[(int)m_CurrentStage](this);
        }

        public void Dispose()
        {
            request.Progress = 1;
            m_CurrentStage = Stage.Completed;
            m_StageIndex = 0;
        }

        static void StageBakeReflectionProbe(ReflectionBakeJob job)
        {
            if (job.m_StageIndex >= job.reflectionProbesToBake.Count)
            {
                job.m_StageIndex = -1;
                return;
            }

            // 1. Setup stage information
            var stageProgress = job.reflectionProbesToBake.Count > 0
                ? 1f - job.m_StageIndex / (float)job.reflectionProbesToBake.Count
                : 1f;

            job.request.Progress = ((float)Stage.BakeReflectionProbe + stageProgress) / (float)Stage.Completed;
            job.request.ProgressMessage = string.Format("Reflection Probes ({0}/{1})", job.m_StageIndex + 1, job.reflectionProbesToBake.Count);

            EditorReflectionSystem.BakeReflectionProbeSnapshot(job.reflectionProbesToBake[job.m_StageIndex]);

            ++job.m_StageIndex;
        }

        static void StageBakePlanarProbe(ReflectionBakeJob job)
        {
            if (job.m_StageIndex >= job.planarReflectionProbesToBake.Count)
            {
                job.m_StageIndex = -1;
                return;
            }

            // 1. Setup stage information
            var stageProgress = job.planarReflectionProbesToBake.Count > 0
                ? 1f - job.m_StageIndex / (float)job.planarReflectionProbesToBake.Count
                : 1f;

            job.request.Progress = ((float)Stage.BakePlanarProbe + stageProgress) / (float)Stage.Completed;
            job.request.ProgressMessage = string.Format("Reflection Probes ({0}/{1})", job.m_StageIndex + 1, job.planarReflectionProbesToBake.Count);

            EditorReflectionSystem.BakeReflectionProbeSnapshot(job.planarReflectionProbesToBake[job.m_StageIndex]);

            ++job.m_StageIndex;
        }
    }
}
