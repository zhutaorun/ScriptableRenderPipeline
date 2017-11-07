using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class StandardSpecularToHDLitTransitionalMaterialUpgrader : MaterialUpgrader
    {
        public StandardSpecularToHDLitTransitionalMaterialUpgrader() : this("Standard (Specular setup)", "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass) { }

        public StandardSpecularToHDLitTransitionalMaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer)
        {          
            RenameShader(sourceShaderName, destShaderName, finalizer);
            CommonRename();
           
            RenameTexture("_SpecGlossMap", "_SpecularColorMap");
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            base.Convert(srcMaterial, dstMaterial);
            CommonConvert(srcMaterial, dstMaterial);

            dstMaterial.SetFloat("_MaterialID", 4.0f);
            Color c = new Color(1,1,1,1);
            dstMaterial.SetColor("_SpecularColor", c);
            float smoothness = srcMaterial.GetFloat("_Glossiness");
            dstMaterial.SetFloat("_Smoothness", smoothness * smoothness);
        }     
    }
}
