using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        internal static bool IsProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditBaseShape
                || editMode == EditInfluenceShape
                || editMode == EditInfluenceNormalShape
                || editMode == EditCenter;
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
            {
                var bounds = new Bounds();
                foreach (Component targetObject in o.targets)
                {
                    var rp = targetObject.transform;
                    var b = rp.position;
                    bounds.Encapsulate(b);
                }
                return bounds;
            };
        }
    }
}
