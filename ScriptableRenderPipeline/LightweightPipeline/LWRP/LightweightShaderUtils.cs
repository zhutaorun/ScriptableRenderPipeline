using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum ShaderPathID
    {
        STANDARD_PBS = 0,
        STANDARD_SIMPLE_LIGHTING,
        STANDARD_UNLIT,
        STANDARD_TERRAIN,
        STANDARD_PARTICLES_LIT,
        STANDARD_PARTICLES_UNLIT,

        HIDDEN_BLIT,
        HIDDEN_DEPTH_COPY,

        SHADER_PATH_COUNT
    }

    public enum DebugViewKeyword
    {
        ALBEDO,
        METALNESS,
        NORMALS,
        SMOOTHNESS,

        NUM_VIEWS
    }

    public static class LightweightShaderUtils
    {
        private static readonly string[] m_ShaderPaths  =
        {
            "LightweightPipeline/Standard (Physically Based)",
            "LightweightPipeline/Standard (Simple Lighting)",
            "LightweightPipeline/Standard Unlit",
            "LightweightPipeline/Standard Terrain",
            "LightweightPipeline/Particles/Standard",
            "LightweightPipeline/Particles/Standard Unlit",
            "Hidden/LightweightPipeline/Blit",
            "Hidden/LightweightPipeline/CopyDepth"
        };

        private static readonly string[] m_DebugViewKeywords =
        {
            "_DEBUG_ALBEDO",
            "_DEBUG_METALNESS",
            "_DEBUG_NORMALS",
            "_DEBUG_SMOOTHNESS"
        };

        public static string GetShaderPath(ShaderPathID id)
        {
            int index = (int)id;
            if (index < 0 && index >= (int)ShaderPathID.SHADER_PATH_COUNT)
            {
                Debug.LogError("Trying to access lightweight shader path out of bounds");
                return "";
            }

            return m_ShaderPaths[index];
        }

        public static void ResetDebugModes(CommandBuffer cmd)
        {
            for(int i = 0; i < (int)DebugViewKeyword.NUM_VIEWS; i++)
            {
                cmd.DisableShaderKeyword(m_DebugViewKeywords[i]);
            }
        }

        public static void SetDebugMode(CommandBuffer cmd, DebugViewKeyword mode)
        {
            int m = (int)mode;
            if(m < 0 && m >= (int)DebugViewKeyword.NUM_VIEWS)
            {
                Debug.LogError("Debug view does not exist.");
            }

            cmd.EnableShaderKeyword(m_DebugViewKeywords[m]);
        }
    }
}
