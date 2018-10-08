using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedPlanarReflectionProbe : SerializedHDProbe
    {
        internal SerializedProperty localReferencePosition;

        internal new PlanarReflectionProbe target { get { return serializedObject.targetObject as PlanarReflectionProbe; } }

        internal SerializedPlanarReflectionProbe(SerializedObject serializedObject) : base(serializedObject)
        {
            localReferencePosition = serializedObject.Find((PlanarReflectionProbe p) => p.localReferencePosition);

            influenceVolume.editorSimplifiedModeBlendNormalDistance.floatValue = 0;
        }
    }
}
