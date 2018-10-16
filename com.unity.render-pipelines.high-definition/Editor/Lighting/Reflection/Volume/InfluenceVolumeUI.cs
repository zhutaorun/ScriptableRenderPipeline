using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI : BaseUI<SerializedInfluenceVolume>
    {
        const int k_AnimBoolFields = 2;
        static readonly int k_ShapeCount = Enum.GetValues(typeof(InfluenceShape)).Length;

        public HierarchicalBox boxBaseHandle;
        public HierarchicalBox boxInfluenceHandle;
        public HierarchicalBox boxInfluenceNormalHandle;

        public SphereBoundsHandle sphereBaseHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceNormalHandle = new SphereBoundsHandle();

        public AnimBool isSectionExpandedShape { get { return m_AnimBools[k_ShapeCount]; } }
        public bool showInfluenceHandles { get; set; }

        public InfluenceVolumeUI()
            : base(k_ShapeCount + k_AnimBoolFields)
        {
            isSectionExpandedShape.value = true;

            Color[] handleColors = new Color[]
            {
                InfluenceVolumeUI.k_HandlesColor[0][0],
                InfluenceVolumeUI.k_HandlesColor[0][1],
                InfluenceVolumeUI.k_HandlesColor[0][2],
                InfluenceVolumeUI.k_HandlesColor[1][0],
                InfluenceVolumeUI.k_HandlesColor[1][1],
                InfluenceVolumeUI.k_HandlesColor[1][2]
            };

            Color baseHandle = InfluenceVolumeUI.k_GizmoThemeColorBase;
            baseHandle.a = 1f;
            Color[] basehandleColors = new Color[]
            {
                baseHandle, baseHandle, baseHandle,
                baseHandle, baseHandle, baseHandle
            };
            boxBaseHandle = new HierarchicalBox(InfluenceVolumeUI.k_GizmoThemeColorBase, basehandleColors);
            boxBaseHandle.monoHandle = false;
            boxInfluenceHandle = new HierarchicalBox(InfluenceVolumeUI.k_GizmoThemeColorInfluence, handleColors, container: boxBaseHandle);
            boxInfluenceNormalHandle = new HierarchicalBox(InfluenceVolumeUI.k_GizmoThemeColorInfluenceNormal, handleColors, container: boxBaseHandle);
        }

        public void SetIsSectionExpanded_Shape(InfluenceShape shape)
        {
            SetIsSectionExpanded_Shape((int)shape);
        }

        public void SetIsSectionExpanded_Shape(int shape)
        {
            for (var i = 0; i < k_ShapeCount; i++)
                m_AnimBools[i].target = shape == i;
        }

        public AnimBool IsSectionExpanded_Shape(InfluenceShape shapeType)
        {
            return m_AnimBools[(int)shapeType];
        }
    }
}
