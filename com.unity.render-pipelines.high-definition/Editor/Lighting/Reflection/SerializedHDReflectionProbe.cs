using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedHDReflectionProbe : SerializedHDProbe
    {
        internal SerializedObject serializedLegacyObject;

        SerializedProperty legacyBlendDistance;
        SerializedProperty legacySize;
        SerializedProperty legacyOffset;
        SerializedProperty legacyMode;

        SerializedProperty bakedRenderData;
        SerializedProperty customRenderData;

        public SerializedHDReflectionProbe(SerializedObject legacyProbe, SerializedObject additionalData)
            : base(additionalData)
        {
            serializedLegacyObject = legacyProbe;

            legacySize = legacyProbe.FindProperty("m_BoxSize");
            legacyOffset = legacyProbe.FindProperty("m_BoxOffset");
            legacyBlendDistance = legacyProbe.FindProperty("m_BlendDistance");
            legacyMode = legacyProbe.FindProperty("m_Mode");

            bakedRenderData = additionalData.Find((HDAdditionalReflectionData d) => d.bakedRenderData);
            customRenderData = additionalData.Find((HDAdditionalReflectionData d) => d.customRenderData);
        }

        internal override void Update()
        {
            serializedLegacyObject.Update();
            base.Update();

            // Set the legacy blend distance to 0 so the legacy culling system use the probe extent
            legacyBlendDistance.floatValue = 0;
        }

        internal override void Apply()
        {
            // Sync mode with legacy reflection probe
            legacyMode.intValue = probeSettings.mode.intValue;

            serializedLegacyObject.ApplyModifiedProperties();
            base.Apply();
        }
    }
}
