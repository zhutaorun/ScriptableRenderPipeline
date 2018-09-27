using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace ShaderGraph.Tests
{
    public class TestPipelineAsset : RenderPipelineAsset
    {
        [System.Serializable]
        public struct Settings
        {
            public bool whatGoesHere;
            public Material defaultMaterial;
        }

        [SerializeField]
        private Settings m_settings = new Settings();
        public Settings settings
        {
            get
            {
                return m_settings;
            }
        }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new TestPipeline();
        }

        public override Material GetDefaultMaterial()
        {
            return m_settings.defaultMaterial;
        }
    }
}