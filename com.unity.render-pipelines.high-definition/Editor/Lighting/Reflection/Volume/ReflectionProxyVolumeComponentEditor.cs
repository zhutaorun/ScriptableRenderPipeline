using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(ReflectionProxyVolumeComponent))]
    [CanEditMultipleObjects]
    class ReflectionProxyVolumeComponentEditor : Editor
    {
        static readonly Color k_HandleColor = new Color(0 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);

        HierarchicalBox m_Handle = new HierarchicalBox(k_HandleColor);
        SerializedReflectionProxyVolumeComponent m_SerializedData;
        ReflectionProxyVolumeComponentUI m_UIState = new ReflectionProxyVolumeComponentUI();
        ReflectionProxyVolumeComponent[] m_TypedTargets;

        void OnEnable()
        {
            m_SerializedData = new SerializedReflectionProxyVolumeComponent(serializedObject);
            System.Array.Resize(ref m_TypedTargets, serializedObject.targetObjects.Length);
            for (int i = 0; i < serializedObject.targetObjects.Length; ++i)
                m_TypedTargets[i] = (ReflectionProxyVolumeComponent)serializedObject.targetObjects[i];

            m_Handle = m_Handle ?? new HierarchicalBox(k_HandleColor);
            m_UIState = m_UIState ?? new ReflectionProxyVolumeComponentUI();

            m_Handle.monoHandle = false;
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedData;
            var o = this;

            d.Update();
            s.Update(d);

            ReflectionProxyVolumeComponentUI.Inspector.Draw(s, d, o);

            d.Apply();
        }

        void OnSceneGUI()
        {
            
            for (int i = 0; i < m_TypedTargets.Length; ++i)
            {
                var comp = (ReflectionProxyVolumeComponent)m_TypedTargets[i];
                var tr = comp.transform;
                var prox = comp.proxyVolume;

                switch (prox.shape)
                {
                    case ProxyShape.Box:
                        m_Handle.center = tr.position;
                        m_Handle.size = prox.boxSize;
                        EditorGUI.BeginChangeCheck();
                        m_Handle.DrawHull(true); 
                        m_Handle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { tr, comp }, "Update Proxy Volume Size");
                            tr.position = m_Handle.center;
                            prox.boxSize = m_Handle.size;
                        }
                        break;
                    case ProxyShape.Sphere:
                        break;
                    case ProxyShape.Infinite:
                        break;
                }
            }
        }
    }
}
