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

        static readonly Color[] k_HandleColors = new Color[]
        {
            HDReflectionProbeEditor.k_handlesColor[0][0],
            HDReflectionProbeEditor.k_handlesColor[0][1],
            HDReflectionProbeEditor.k_handlesColor[0][2],
            HDReflectionProbeEditor.k_handlesColor[1][0],
            HDReflectionProbeEditor.k_handlesColor[1][1],
            HDReflectionProbeEditor.k_handlesColor[1][2]
        };

        EditorPrefBoolFlags<Flag> m_FlagStorage = new EditorPrefBoolFlags<Flag>("InfluenceVolumeUI");

        public HierarchicalBox boxBaseHandle;
        public HierarchicalBox boxInfluenceHandle;
        public HierarchicalBox boxInfluenceNormalHandle;

        public SphereBoundsHandle sphereBaseHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceNormalHandle = new SphereBoundsHandle();

        public bool HasFlag(Flag v) => m_FlagStorage.HasFlag(v);
        public bool SetFlag(Flag f, bool v) => m_FlagStorage.SetFlag(f, v);

        public InfluenceVolumeUI()
        {
            boxBaseHandle = new HierarchicalBox(
                HDReflectionProbeEditor.k_GizmoThemeColorExtent, k_HandleColors
            );
            boxInfluenceHandle = new HierarchicalBox(
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend, k_HandleColors, container: boxBaseHandle
            );
            boxInfluenceNormalHandle = new HierarchicalBox(
                HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend, k_HandleColors, container: boxBaseHandle
            );
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
