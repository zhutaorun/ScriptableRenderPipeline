using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.LightProbeNode")]
    [Title("Input", "Lighting", "Sample Baked GI")]
    public class SampleBakedGINode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public SampleBakedGINode()
        {
            name = "Sample Baked GI";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Sample-Baked-GI-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SampleBakedGI", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SampleBakedGI(
            [Slot(2, Binding.WorldSpacePosition)] Vector3 Position,
            [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
            [Slot(3, Binding.MeshUV1)] Vector2 StaticUV,
            [Slot(4, Binding.MeshUV2)] Vector2 DynamicUV,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SHADERGRAPH_SAMPLE_BAKED_GI(Position, Normal, StaticUV, DynamicUV);
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
