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

        //[TODO] change this to be modifiable shortcuts
        static Dictionary<KeyCode, ToolBar> s_Toolbar_ShortCutKey = null;
        protected static Dictionary<KeyCode, ToolBar> toolbar_ShortCutKey
        {
            get
            {
                return s_Toolbar_ShortCutKey ?? (s_Toolbar_ShortCutKey = new Dictionary<KeyCode, ToolBar>
                {
                    { KeyCode.Alpha1, ToolBar.InfluenceShape },
                    { KeyCode.Alpha2, ToolBar.Blend },
                    { KeyCode.Alpha3, ToolBar.NormalBlend },
                    { KeyCode.Alpha4, ToolBar.CapturePosition }
                });
            }
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
