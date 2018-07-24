using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Sample Shadowmask")]
    public class SampleShadowmaskNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public SampleShadowmaskNode()
        {
            name = "Sample Shadowmask";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Sample-Shadowmask-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SampleShadowmask", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SampleShadowmask(
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

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview3D;
            }
        }
    }
}
