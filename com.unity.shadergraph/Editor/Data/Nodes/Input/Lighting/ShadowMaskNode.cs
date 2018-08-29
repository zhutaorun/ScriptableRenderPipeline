using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Shadow Mask")]
    public class ShadowMaskNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public ShadowMaskNode()
        {
            name = "ShadowMask";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Shadow-Mask-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ShadowMask", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ShadowMask(
            [Slot(0, Binding.WorldSpacePosition)] Vector3 Position,
            [Slot(1, Binding.MeshUV1)] Vector2 StaticUV,
            [Slot(2, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.one;
            return
                @"
{
    Out = SHADERGRAPH_SAMPLE_SHADOWMASK(Position, StaticUV);
}
";
        }
    }
}
