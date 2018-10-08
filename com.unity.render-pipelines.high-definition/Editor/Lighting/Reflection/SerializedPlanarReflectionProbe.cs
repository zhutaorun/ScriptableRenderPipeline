using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedPlanarReflectionProbe : SerializedHDProbe
    {
        internal SerializedProperty localReferencePosition;

        internal SerializedProperty bakedRenderData;
        internal SerializedProperty customRenderData;

        internal new PlanarReflectionProbe target { get { return serializedObject.targetObject as PlanarReflectionProbe; } }

        internal SerializedPlanarReflectionProbe(SerializedObject serializedObject)
            : base(serializedObject)
        {
            localReferencePosition = serializedObject.Find((PlanarReflectionProbe p) => p.localReferencePosition);
            bakedRenderData = serializedObject.Find((PlanarReflectionProbe p) => p.bakedRenderData);
            customRenderData = serializedObject.Find((PlanarReflectionProbe p) => p.customRenderData);

            probeSettings.influence.editorSimplifiedModeBlendNormalDistance.floatValue = 0;
        }
    }
}
