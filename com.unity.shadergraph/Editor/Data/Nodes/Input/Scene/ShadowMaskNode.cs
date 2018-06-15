using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Shadow Mask")]
    public class ShadowMaskNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public ShadowMaskNode()
        {
            name = "Shadow Mask";
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
            [Slot(1, Binding.MeshUV2)] Vector3 UV,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SHADERGRAPH_SAMPLE_SHADOWMASK(Position, UV);
}
";
        }
    }
}
