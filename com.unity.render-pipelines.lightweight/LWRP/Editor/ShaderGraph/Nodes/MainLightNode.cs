using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Main Light")]
    public sealed class MainLightNode : CodeFunctionNode
    {
        public MainLightNode()
        {
            name = "Main Light";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Main-Light-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_MainLight", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_MainLight(
            [Slot(0, Binding.None)] out Vector1 Attenuation,
            [Slot(1, Binding.None)] out Vector3 Direction,
            [Slot(2, Binding.None)] out Vector3 Color)
        {
            Direction = Vector3.one;
            Color = Vector3.one;
            return
                @"
{
    SHADERGRAPH_MAIN_LIGHT(Attenuation, Direction, Color);
}
";
        }

        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
