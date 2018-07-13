using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Additional Light")]
    public sealed class AdditionalLightNode : CodeFunctionNode
    {
        public AdditionalLightNode()
        {
            name = "Additional Light";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Additional-Light-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_AdditionalLight", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_AdditionalLight(
            [Slot(0, Binding.WorldSpacePosition)] Vector3 Position,
            [Slot(1, Binding.None)] Integer Index,
            [Slot(2, Binding.None)] out Vector1 Attenuation,
            [Slot(3, Binding.None)] out Vector3 Direction,
            [Slot(4, Binding.None)] out Vector3 Color)
        {
            Direction = Vector3.one;
            Color = Vector3.one;
            return
                @"
{
    SHADERGRAPH_ADDITIONAL_LIGHT(Index, Position, Attenuation, Direction, Color);
}
";
        }

        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
