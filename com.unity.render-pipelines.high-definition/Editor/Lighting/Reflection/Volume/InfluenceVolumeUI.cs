using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI : IUpdateable<SerializedInfluenceVolume>
    {
        [Flags]
        internal enum Flag
        {
            None = 0,
            SectionExpandedShape = 1 << 0,
            SectionExpandedShapeSphere = 1 << 1,
            SectionExpandedShapeBox = 1 << 2,
            ShowInfluenceHandle = 1 << 3
        }

        EditorPrefBoolFlags<Flag> m_FlagStorage = new EditorPrefBoolFlags<Flag>("InfluenceVolumeUI");

        public Gizmo6FacesBox boxBaseHandle;
        public Gizmo6FacesBoxContained boxInfluenceHandle;
        public Gizmo6FacesBoxContained boxInfluenceNormalHandle;

        public SphereBoundsHandle sphereBaseHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceNormalHandle = new SphereBoundsHandle();

        public bool HasFlag(Flag v) => m_FlagStorage.HasFlag(v);
        public bool SetFlag(Flag f, bool v) => m_FlagStorage.SetFlag(f, v);

        public InfluenceVolumeUI()
        {
            boxBaseHandle = new Gizmo6FacesBox(monochromeFace:true, monochromeSelectedFace:true);
            boxInfluenceHandle = new Gizmo6FacesBoxContained(boxBaseHandle, monochromeFace:true, monochromeSelectedFace:true);
            boxInfluenceNormalHandle = new Gizmo6FacesBoxContained(boxBaseHandle, monochromeFace:true, monochromeSelectedFace:true);

            Color[] handleColors = new Color[]
            {
                HDReflectionProbeEditor.k_handlesColor[0][0],
                HDReflectionProbeEditor.k_handlesColor[0][1],
                HDReflectionProbeEditor.k_handlesColor[0][2],
                HDReflectionProbeEditor.k_handlesColor[1][0],
                HDReflectionProbeEditor.k_handlesColor[1][1],
                HDReflectionProbeEditor.k_handlesColor[1][2]
            };
            boxBaseHandle.handleColors = handleColors;
            boxInfluenceHandle.handleColors = handleColors;
            boxInfluenceNormalHandle.handleColors = handleColors;

            boxBaseHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorExtent };
            boxBaseHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorExtentFace };
            boxInfluenceHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend };
            boxInfluenceHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace };
            boxInfluenceNormalHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend };
            boxInfluenceNormalHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace };
        }

        public void Update(SerializedInfluenceVolume v)
        {
            m_FlagStorage.SetFlag(Flag.SectionExpandedShapeBox | Flag.SectionExpandedShapeSphere, false);
            switch ((InfluenceShape)v.shape.intValue)
            {
                case InfluenceShape.Box: m_FlagStorage.SetFlag(Flag.SectionExpandedShapeBox, true); break;
                case InfluenceShape.Sphere: m_FlagStorage.SetFlag(Flag.SectionExpandedShapeSphere, true); break;
            }
        }
    }
}
