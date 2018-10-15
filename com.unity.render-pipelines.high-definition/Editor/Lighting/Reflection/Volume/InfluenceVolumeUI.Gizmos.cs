using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        [Flags]
        public enum HandleType
        {
            None = 0,
            Base = 1,
            Influence = 1 << 1,
            InfluenceNormal = 1 << 2,

            All = ~0
        }

        public static void DrawGizmos(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix, HandleType editedHandle, HandleType showedHandle)
        {
            var mat = Gizmos.matrix;
            var c = Gizmos.color;
            Gizmos.matrix = matrix;

            if ((showedHandle & HandleType.Base) != 0)
            {
                Gizmos.color = k_GizmoThemeColorBase;
                switch (d.shape)
                {
                    case InfluenceShape.Box: Gizmos.DrawWireCube(d.offset, d.boxSize); break;
                    case InfluenceShape.Sphere: Gizmos.DrawWireSphere(d.offset, d.sphereRadius); break;
                }
            }

            if ((showedHandle & HandleType.Influence) != 0)
            {
                Gizmos.color = k_GizmoThemeColorInfluence;
                switch (d.shape)
                {
                    case InfluenceShape.Box: Gizmos.DrawWireCube(d.offset + d.boxBlendOffset, d.boxSize + d.boxBlendSize); break;
                    case InfluenceShape.Sphere: Gizmos.DrawWireSphere(d.offset, d.sphereRadius - d.sphereBlendDistance); break;
                }
            }

            if ((showedHandle & HandleType.InfluenceNormal) != 0)
            {
                Gizmos.color = k_GizmoThemeColorInfluenceNormal;
                switch (d.shape)
                {
                    case InfluenceShape.Box: Gizmos.DrawWireCube(d.offset + d.boxBlendNormalOffset, d.boxSize + d.boxBlendNormalSize); break;
                    case InfluenceShape.Sphere: Gizmos.DrawWireSphere(d.offset, d.sphereRadius - d.sphereBlendNormalDistance); break;
                }
            }

            Gizmos.matrix = mat;
            Gizmos.color = c;
        }
    }
}
