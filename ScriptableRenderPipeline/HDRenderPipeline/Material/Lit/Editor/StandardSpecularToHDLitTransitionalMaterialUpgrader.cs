using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class StandardSpecularToHDLitTransitionalMaterialUpgrader : MaterialUpgrader
    {
        public StandardSpecularToHDLitTransitionalMaterialUpgrader() : this("Standard (Specular setup)", "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass) { }

        public StandardSpecularToHDLitTransitionalMaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer)
        {
            // Anything reasonable that can be done here?
            //RenameFloat("_SpecColor", ...);
            RenameShader(sourceShaderName, destShaderName, finalizer);

            RenameTexture("_MainTex", "_BaseColorMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_Glossiness", "_Smoothness");
            RenameTexture("_BumpMap", "_NormalMap");
            RenameFloat("_BumpScale", "_NormalScale");
            RenameTexture("_EmissionMap", "_EmissiveColorMap");
            RenameColor("_EmissionColor", "_EmissiveColor");
            RenameFloat("_DetailNormalMapScale", "_DetailNormalScale");
            RenameFloat("_Cutoff", "_AlphaCutoff");
            RenameKeywordToFloat("_ALPHATEST_ON", "_AlphaCutoffEnable", 1f, 0f);
            RenameTexture("_SpecGlossMap", "_SpecularColorMap");
            RenameTexture("_ParallaxMap", "_HeightMap");

            // the HD renderloop packs detail albedo and detail normals into a single texture.
            // mapping the detail normal map, if any, to the detail map, should do the right thing if
            // there is no detail albedo.
            RenameTexture("_DetailAlbedoMap", "_DetailMapLegacy");
            RenameTexture("_DetailMask", "_DetailMaskMapLegacy");            
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            base.Convert(srcMaterial, dstMaterial);
            if (srcMaterial.GetTexture("_ParallaxMap") != null)
            {
                dstMaterial.SetFloat("_DisplacementMode", 2.0f);
                dstMaterial.SetFloat("_DisplacementLockObjectScale", 0.0f);
                dstMaterial.SetFloat("_DepthOffsetEnable", 1.0f);
            }
            dstMaterial.SetFloat("_EmissiveIntensity", 3.141f); // same as lights
            dstMaterial.SetFloat("_MaterialID", 4.0f);
            Color c = new Color(1,1,1,1);
            dstMaterial.SetColor("_SpecularColor", c);
            float smoothness = srcMaterial.GetFloat("_Glossiness");
            dstMaterial.SetFloat("_Smoothness", smoothness * smoothness);
        }

        [Test]
        public void UpgradeMaterial()
        {
            var newShader = Shader.Find("HDRenderPipeline/Lit");
            var mat = new Material(Shader.Find("Standard (Specular setup)"));
            var albedo = new Texture2D(1, 1);
            var normals = new Texture2D(1, 1);
            var baseScale = new Vector2(1, 1);
            var color = Color.red;
            mat.mainTexture = albedo;
            mat.SetTexture("_BumpMap", normals);
            mat.color = color;
            mat.SetTextureScale("_MainTex", baseScale);

            MaterialUpgrader.Upgrade(mat, this, MaterialUpgrader.UpgradeFlags.CleanupNonUpgradedProperties);

            Assert.AreEqual(newShader, mat.shader);
            Assert.AreEqual(albedo, mat.GetTexture("_BaseColorMap"));
            Assert.AreEqual(color, mat.GetColor("_BaseColor"));
            Assert.AreEqual(baseScale, mat.GetTextureScale("_BaseColorMap"));
            Assert.AreEqual(normals, mat.GetTexture("_NormalMap"));
            Assert.IsTrue(mat.IsKeywordEnabled("_NORMALMAP"));
        }
    }
}
