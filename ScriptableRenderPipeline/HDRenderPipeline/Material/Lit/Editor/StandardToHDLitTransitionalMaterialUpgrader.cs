using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class StandardToHDLitTransitionalMaterialUpgrader : MaterialUpgrader
    {       
        public StandardToHDLitTransitionalMaterialUpgrader() : this("Standard", "HDRenderPipeline/LitTransitional", LitGUI.SetupMaterialKeywordsAndPass) { }

        public StandardToHDLitTransitionalMaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);
            CommonRename();

            RenameTexture("_MetallicGlossMap", "_MaskMap");
            RenameFloat("_Metallic", "_Metallic");          
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            base.Convert(srcMaterial, dstMaterial);
            CommonConvert(srcMaterial, dstMaterial);

            if (srcMaterial.GetTexture("_MetallicGlossMap") != null)
            {
                dstMaterial.SetFloat("_Metallic", 1.0f);
            }
        }
    }
}
