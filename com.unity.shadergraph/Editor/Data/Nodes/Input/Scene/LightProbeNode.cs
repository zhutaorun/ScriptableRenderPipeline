using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.LightProbeNode")]
    [Title("Input", "Scene", "Baked GI")]
    public class BakedGINode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public BakedGINode()
        {
            name = "Baked GI";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Baked-GI-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_BakedGI", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_BakedGI(
            [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SHADERGRAPH_SAMPLE_BAKED_GI(Normal);
}
";
        }
    }
}
