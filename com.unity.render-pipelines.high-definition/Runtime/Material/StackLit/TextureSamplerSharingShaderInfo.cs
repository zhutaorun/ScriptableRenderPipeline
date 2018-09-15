using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class TextureSamplerSharingShaderInfo : ScriptableObject
    {
        public string ShaderFileChecksum;
        public string PropertiesFileChecksum;
        public DateTimeSerializable LastChecksumDateUTC;
    }
}
