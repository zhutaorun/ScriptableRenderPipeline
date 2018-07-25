using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class HDProbeTickedRenderer
    {
        HDProbeRenderer m_Renderer = new HDProbeRenderer();
        int m_NextIndexToBake = 0;
        Hash128 m_InputHash = new Hash128();
        bool m_IsComplete = true;
        bool m_IsRunning = false;
        HDReflectionEntityID[] m_ToBakeIDs;
        Hash128[] m_ToBakeHashes;

        HDProbeTextureImporter m_TextureImporter;

        internal bool isComplete { get { return m_IsComplete; } }
        internal Hash128 inputHash;

        // To inject by callee
        public HDReflectionEntityManager2 entityManager;

        internal void Cancel()
        {
            m_IsRunning = false;
            m_IsComplete = false;
        }

        internal unsafe void Start(
            Hash128 inputHash,
            ReflectionSettings settings,
            int addCount,
            HDReflectionEntityID* toBakeIDs,
            Hash128* toBakeHashes
        )
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("Trying to start the HDProbeTickedRenderer while it is running. Be sure to call Cancel() before.");
                return;
            }
            m_IsRunning = true;

            m_InputHash = inputHash;
            m_NextIndexToBake = 0;

            Array.Resize(ref m_ToBakeIDs, addCount);
            for (int i = 0; i < m_ToBakeIDs.Length; ++i)
                m_ToBakeIDs[i] = toBakeIDs[i];
            Array.Resize(ref m_ToBakeHashes, addCount);
            for (int i = 0; i < m_ToBakeHashes.Length; ++i)
                m_ToBakeHashes[i] = toBakeHashes[i];
        }

        internal bool Tick()
        {
            if (!m_IsRunning
                || m_NextIndexToBake >= m_ToBakeIDs.Length)
            {
                m_IsComplete = true;
                m_IsRunning = !m_IsComplete;
                return m_IsComplete;
            }

            var index = m_NextIndexToBake;
            ++m_NextIndexToBake;

            var probeId = m_ToBakeIDs[index];
            var probe = entityManager.GetProbeByID(probeId);
            var scenePath = probe.gameObject.scene.path;
            var hash = m_ToBakeHashes[index];

            var renderTarget = HDProbeRendererUtilities.CreateRenderTarget(probe);
            m_Renderer.Render(probe, renderTarget);

            var cacheFilePath = m_TextureImporter.GetCacheBakePathFor(probe, hash);
            HDBakeUtilities.WriteBakedTextureTo(renderTarget, cacheFilePath);

            m_IsComplete = m_NextIndexToBake >= m_ToBakeIDs.Length;
            m_IsRunning = !m_IsComplete;
            return m_IsComplete;
        }
    }
}
