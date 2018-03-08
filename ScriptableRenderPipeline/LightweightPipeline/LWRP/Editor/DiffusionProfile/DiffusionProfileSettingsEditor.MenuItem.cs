using System;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public sealed partial class DiffusionProfileSettingsEditor
    {
        class DoCreateNewAsset<TAssetType> : ProjectWindowCallback.EndNameEditAction where TAssetType : ScriptableObject
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset  = CreateInstance<TAssetType>();
                newAsset.name = Path.GetFileName(pathName);
                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        class DoCreateNewAssetDiffusionProfileSettings : DoCreateNewAsset<DiffusionProfileSettings> {}

        [MenuItem("Assets/Create/Rendering/Diffusion profile Settings (LW)", priority = CoreUtils.assetCreateMenuPriority2)]
        static void MenuCreateDiffusionProfile()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetDiffusionProfileSettings>(), "New Diffusion Profile Settings.asset", icon, null);
        }
    }
}