using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class UpgradeStandardShaderMaterials
    {
        static List<MaterialUpgrader> GetHDUpgraders()
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(new StandardToHDLitMaterialUpgrader("Standard", "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass));
            upgraders.Add(new StandardSpecularToHDLitMaterialUpgrader("Standard (Specular setup)", "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass));
            return upgraders;
        }


        static List<MaterialUpgrader> GetHDUpgraders_SS()
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(new StandardToHDLitMaterialUpgrader_SS("Standard", "HDRenderPipeline/LitTransitional", LitGUI.SetupMaterialKeywordsAndPass));           
            return upgraders;
        }

/*
        [MenuItem("RenderPipeline/HDRenderPipeline/Material Upgraders/Upgrade Standard Materials to Lit Materials (SS) - Project Folder", false, 4)]
        static void UpgradeMaterialsProject_SS()
        {
            MaterialUpgrader.UpgradeProjectFolder_SS(GetHDUpgraders(), "Upgrade to HD Material");
        }
*/

        [MenuItem("RenderPipeline/HDRenderPipeline/Material Upgraders/Upgrade Standard Materials to Lit Materials (SS) - Selection", false, 2)]
        static void UpgradeMaterialsSelection_SS()
        {
            MaterialUpgrader.UpgradeSelection_SS(GetHDUpgraders_SS(), "Upgrade to HD Material");
        }


        [MenuItem("RenderPipeline/HDRenderPipeline/Material Upgraders/Upgrade Standard Materials to Lit Materials - Project Folder", false, 1)]
        static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("RenderPipeline/HDRenderPipeline/Material Upgraders/Upgrade Standard Materials to Lit Materials - Selection", false, 2)]
        static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("RenderPipeline/HDRenderPipeline/Material Upgraders/Modify Light Intensity for Upgrade - Scene Only", false, 3)]
        static void UpgradeLights()
        {
            Light[] lights = Light.GetLights(LightType.Directional, 0);
            foreach (var l in lights)
            {
                l.intensity *= Mathf.PI;
            }
        }
    }
}
