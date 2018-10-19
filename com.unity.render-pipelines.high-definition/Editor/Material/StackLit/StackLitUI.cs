using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class StackLitGUI : BaseMaterialGUI
    {
        protected override uint defaultExpendedState { get { return (uint)(Expendable.Base | Expendable.Input | Expendable.VertexAnimation | Expendable.Detail | Expendable.Emissive | Expendable.Transparency | Expendable.Other); } }

        protected static class StylesStackLit
        {
            public const string stackOptionText = "Stack Options";

            public static GUIContent useLocalPlanarMapping = new GUIContent("Use Local Planar Mapping", "Use local space for planar/triplanar mapping instead of world space");
        };

        #region Strings
        public const string k_StackLitShaderName = "HDRenderPipeline/StackLit";
        public const string k_StackLitShaderPath = "Runtime/Material/StackLit/StackLit.shader";
        public const string k_StackLitPackagedShaderPath = "Packages/com.unity.render-pipelines.high-definition/" + k_StackLitShaderPath;

        protected const string k_DoubleSidedNormalMode = "_DoubleSidedNormalMode";

        protected const string k_UVBase = "_UVBase";

        // Base
        protected const string k_BaseColor = "_BaseColor";
        protected const string k_BaseColorMap = "_BaseColorMap";
        protected const string k_BaseColorMapUV = "_BaseColorMapUV";

        protected const string k_SpecularColor = "_SpecularColor";
        protected const string k_SpecularColorMap = "_SpecularColorMap";
        protected const string k_SpecularColorMapUV = "_SpecularColorMapUV";
        protected const string k_EnergyConservingSpecularColor = "_EnergyConservingSpecularColor";

        protected const string k_BaseParametrization = "_BaseParametrization";

        protected const string k_Metallic = "_Metallic";
        protected const string k_MetallicMap = "_MetallicMap";
        protected const string k_MetallicMapUV = "_MetallicMapUV";

        protected const string k_DielectricIor = "_DielectricIor";

        protected const string k_SmoothnessA = "_SmoothnessA";
        protected const string k_SmoothnessAMap = "_SmoothnessAMap";
        protected const string k_SmoothnessAMapUV = "_SmoothnessAMapUV";

        protected const string k_Normal = "_Normal";
        protected const string k_NormalMap = "_NormalMap";
        protected const string k_NormalMapUV = "_NormalMapUV";
        protected const string k_NormalScale = "_NormalScale";

        protected const string k_BentNormal = "_BentNormal";
        protected const string k_BentNormalMap = "_BentNormalMap";

        protected const string k_EnableSpecularOcclusion = "_EnableSpecularOcclusion";

        protected const string k_AmbientOcclusion = "_AmbientOcclusion";
        protected const string k_AmbientOcclusionMap = "_AmbientOcclusionMap";
        protected const string k_AmbientOcclusionMapUV = "_AmbientOcclusionMapUV";

        // Emissive
        protected const string k_EmissiveColor = "_EmissiveColor";
        protected const string k_EmissiveColorMap = "_EmissiveColorMap";
        protected const string k_EmissiveColorMapUV = "_EmissiveColorMapUV";
        protected const string k_AlbedoAffectEmissive = "_AlbedoAffectEmissive";

        // Coat
        protected const string k_EnableCoat = "_EnableCoat";
        protected const string k_CoatSmoothness = "_CoatSmoothness";
        protected const string k_CoatSmoothnessMap = "_CoatSmoothnessMap";
        protected const string k_CoatSmoothnessMapUV = "_CoatSmoothnessMapUV";
        protected const string k_CoatIor = "_CoatIor";
        protected const string k_CoatIorMap = "_CoatIorMap";
        protected const string k_CoatIorMapUV = "_CoatIorMapUV";
        protected const string k_CoatThickness = "_CoatThickness";
        protected const string k_CoatThicknessMap = "_CoatThicknessMap";
        protected const string k_CoatThicknessMapUV = "_CoatThicknessMapUV";
        protected const string k_CoatExtinction = "_CoatExtinction";
        protected const string k_CoatExtinctionMap = "_CoatExtinctionMap";
        protected const string k_CoatExtinctionMapUV = "_CoatExtinctionMapUV";
        protected const string k_EnableCoatNormalMap = "_EnableCoatNormalMap";
        protected const string k_CoatNormal = "_CoatNormal";
        protected const string k_CoatNormalMap = "_CoatNormalMap";
        protected const string k_CoatNormalMapUV = "_CoatNormalMapUV";
        protected const string k_CoatNormalScale = "_CoatNormalScale";

        // SSS
        protected const string k_EnableSubsurfaceScattering = "_EnableSubsurfaceScattering";
        protected const string k_DiffusionProfile = "_DiffusionProfile";
        protected const string k_SubsurfaceMask = "_SubsurfaceMask";
        protected const string k_SubsurfaceMaskMap = "_SubsurfaceMaskMap";
        protected const string k_SubsurfaceMaskMapUV = "_SubsurfaceMaskMapUV";

        // Translucency
        protected const string k_EnableTransmission = "_EnableTransmission";
        protected const string k_Thickness = "_Thickness";
        protected const string k_ThicknessMap = "_ThicknessMap";
        protected const string k_ThicknessMapUV = "_ThicknessMapUV";

        // Second Lobe.
        protected const string k_DualSpecularLobeParametrization = "_DualSpecularLobeParametrization";

        protected const string k_EnableDualSpecularLobe = "_EnableDualSpecularLobe";
        protected const string k_SmoothnessB = "_SmoothnessB";
        protected const string k_SmoothnessBMap = "_SmoothnessBMap";
        protected const string k_SmoothnessBMapUV = "_SmoothnessBMapUV";

        protected const string k_LobeMix = "_LobeMix";
        protected const string k_LobeMixMap = "_LobeMixMap";
        protected const string k_LobeMixMapUV = "_LobeMixMapUV";

        protected const string k_Haziness = "_Haziness";
        protected const string k_HazinessMap = "_HazinessMap";
        protected const string k_HazinessMapUV = "_HazinessMapUV";

        protected const string k_HazeExtent = "_HazeExtent";
        protected const string k_HazeExtentMap = "_HazeExtentMap";
        protected const string k_HazeExtentMapUV = "_HazeExtentMapUV";

        protected const string k_CapHazinessWrtMetallic = "_CapHazinessWrtMetallic";
        protected const string k_HazyGlossMaxDielectricF0 = "_HazyGlossMaxDielectricF0"; // only valid if above option enabled and we have a basecolor + metallic input parametrization

        // Anisotropy
        protected const string k_EnableAnisotropy = "_EnableAnisotropy";

        protected const string k_Tangent = "_Tangent";
        protected const string k_TangentMap = "_TangentMap";
        protected const string k_TangentMapUV = "_TangentMapUV";

        protected const string k_AnisotropyA = "_AnisotropyA";
        protected const string k_AnisotropyAMap = "_AnisotropyAMap";
        protected const string k_AnisotropyAMapUV = "_AnisotropyAMapUV";

        protected const string k_AnisotropyB = "_AnisotropyB";
        protected const string k_AnisotropyBMap = "_AnisotropyBMap";
        protected const string k_AnisotropyBMapUV = "_AnisotropyBMapUV";

        // Iridescence
        protected const string k_EnableIridescence = "_EnableIridescence";
        protected const string k_IridescenceIor = "_IridescenceIor";
        protected const string k_IridescenceThickness = "_IridescenceThickness";
        protected const string k_IridescenceThicknessMap = "_IridescenceThicknessMap";
        protected const string k_IridescenceThicknessMapUV = "_IridescenceThicknessMapUV";
        protected const string k_IridescenceMask = "_IridescenceMask";
        protected const string k_IridescenceMaskMap = "_IridescenceMaskMap";
        protected const string k_IridescenceMaskMapUV = "_IridescenceMaskMapUV";

        // Details
        protected const string k_EnableDetails = "_EnableDetails";
        
        protected const string k_DetailMask = "_DetailMask";
        protected const string k_DetailMaskMap = "_DetailMaskMap";
        protected const string k_DetailMaskMapUV = "_DetailMaskMapUV";

        protected const string k_DetailSmoothness = "_DetailSmoothness";
        protected const string k_DetailSmoothnessScale = "_DetailSmoothnessScale";
        protected const string k_DetailSmoothnessMap = "_DetailSmoothnessMap";
        protected const string k_DetailSmoothnessMapUV = "_DetailSmoothnessMapUV";

        protected const string k_DetailNormal = "_DetailNormal";
        protected const string k_DetailNormalMap = "_DetailNormalMap";
        protected const string k_DetailNormalMapUV = "_DetailNormalMapUV";
        protected const string k_DetailNormalScale = "_DetailNormalScale";

        // Stencil is use to control lighting mode (regular, split lighting)
        protected const string kStencilRef = "_StencilRef";
        protected const string kStencilWriteMask = "_StencilWriteMask";
        protected const string kStencilRefMV = "_StencilRefMV";
        protected const string kStencilWriteMaskMV = "_StencilWriteMaskMV";
        protected const string kStencilDepthPrepassRef = "_StencilDepthPrepassRef";
        protected const string kStencilDepthPrepassWriteMask = "_StencilDepthPrepassWriteMask";

        protected const string k_GeometricNormalFilteringEnabled = "_GeometricNormalFilteringEnabled";
        protected const string k_TextureNormalFilteringEnabled = "_TextureNormalFilteringEnabled";

        protected const string k_EnableAnisotropyForAreaLights = "_EnableAnisotropyForAreaLights";
        protected const string k_VlayerRecomputePerLight = "_VlayerRecomputePerLight";
        protected const string k_VlayerUseRefractedAnglesForBase = "_VlayerUseRefractedAnglesForBase";
        protected const string k_DebugEnable = "_DebugEnable";

        protected const string k_EnableSamplerSharing = "_EnableSamplerSharing";
        protected const string k_EnableSamplerSharingAutoShaderGeneration = "_EnableSamplerSharingAutoGeneration";
        protected const string k_SamplerSharingUsage = "_SamplerSharingUsage";
        protected const string k_GeneratedShaderSamplerSharingUsage = "_GeneratedShaderSamplerSharingUsage";

        #endregion

        // Add the properties into an array.
        private readonly GroupProperty _baseMaterialProperties = null;
        private readonly GroupProperty _materialProperties = null;

        private Property EnableDetails;
        private Property EnableSpecularOcclusion;
        private Property EnableSSS;
        private Property EnableTransmission;
        private Property EnableCoat;
        private Property EnableCoatNormalMap;
        private Property EnableAnisotropy;
        private Property EnableDualSpecularLobe;
        private Property EnableIridescence;

        private Property EnableGeometricNormalFiltering;
        private Property EnableTextureNormalFiltering;

        private Property EnableSamplerSharing;
        private Property EnableSamplerSharingAutoShaderGeneration;

        private ComboProperty BaseParametrization;
        private ComboProperty DualSpecularLobeParametrization;

        private Property CapHazinessWrtMetallic;

        protected bool stackOptionExpended = true;

        private bool IsMetallicParametrizationUsed
        {
            get { return (!BaseParametrization.IsValid) || ((StackLit.BaseParametrization)BaseParametrization.FloatValue == StackLit.BaseParametrization.BaseMetallic); }
        }

        private bool IsHazyGlossParametrizationUsed
        {
            get { return DualSpecularLobeParametrization.IsValid && ((StackLit.DualSpecularLobeParametrization)DualSpecularLobeParametrization.FloatValue == StackLit.DualSpecularLobeParametrization.HazyGloss); }
        }

        private bool IsCapDielectricUsed
        {
            get { return (IsMetallicParametrizationUsed) && (CapHazinessWrtMetallic.IsValid && CapHazinessWrtMetallic.BoolValue == true); }
        }

        private bool IsSamplerSharingGlobalEnabled
        {
            get { return (EnableSamplerSharing.IsValid && EnableSamplerSharing.BoolValue == true); }
        }

        private static Vector4 GetSamplerSharingUsage(Material material, bool generated = false)
        {
            Vector4 vec = Vector4.zero;
            var prop = generated ? k_GeneratedShaderSamplerSharingUsage : k_SamplerSharingUsage;
            if (material.HasProperty(prop))
            {
                vec = material.GetVector(prop);
            }
            return vec;
        }

        private static void SetSamplerSharingUsage(Material material, Vector4 vec, bool generated = false)
        {
            var prop = generated ? k_GeneratedShaderSamplerSharingUsage : k_SamplerSharingUsage;
            if (material.HasProperty(prop))
            {
                material.SetVector(prop, vec);
            }
        }

        public StackLitGUI()
        {
            _baseMaterialProperties = new GroupProperty(this, "_BaseMaterial", new BaseProperty[]
            {
                new Property(this, k_DoubleSidedNormalMode, "Normal mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal.",
                    false, _ => (/* from BaseUnlitUI: */ base.doubleSidedEnable != null && base.doubleSidedEnable.floatValue > 0.0f)),
                new GroupProperty(this, "_BaseUnlitDistortion", "Distortion", new BaseProperty[] { },
                    extraOnGUI: _ => {  base.DoDistortionInputsGUI(); },
                    isVisible: _ => ((SurfaceType)base.surfaceType.floatValue == SurfaceType.Transparent) )
            });

            EnableDetails = new Property(this, k_EnableDetails, "Enable Details", "Enable Detail", true);
            EnableSpecularOcclusion = new Property(this, k_EnableSpecularOcclusion, "Enable Specular Occlusion", "Enable Specular Occlusion", true);
            EnableSSS = new Property(this, k_EnableSubsurfaceScattering, "Enable Subsurface Scattering", "Enable Subsurface Scattering", true);
            EnableTransmission = new Property(this, k_EnableTransmission, "Enable Transmission", "Enable Transmission", true);
            EnableCoat = new Property(this, k_EnableCoat, "Enable Coat", "Enable coat layer with true vertical physically based BSDF mixing", true);
            EnableCoatNormalMap = new Property(this, k_EnableCoatNormalMap, "Enable Coat Normal Map", "Enable separate top coat normal map", true);
            EnableAnisotropy = new Property(this, k_EnableAnisotropy, "Enable Anisotropy", "Enable anisotropy, correct anisotropy for punctual light but very coarse approximated for reflection", true);
            EnableDualSpecularLobe = new Property(this, k_EnableDualSpecularLobe, "Enable Dual Specular Lobe", "Enable a second specular lobe, aim to simulate a mix of a narrow and a haze lobe that better match measured material", true);
            EnableIridescence = new Property(this, k_EnableIridescence, "Enable Iridescence", "Enable physically based iridescence layer", true);
            EnableSamplerSharing = new Property(this, k_EnableSamplerSharing, "Enable Sampler Sharing", "Enable Sampler Sharing", true);
            EnableSamplerSharingAutoShaderGeneration = new PropertyWithActionButton(this, k_EnableSamplerSharingAutoShaderGeneration, 
                buttonCaptions: new string[] 
                {
                    "Reassign Non Generated StackLit shader",
                    "Generate Custom shader",
                    "Print Sampler Configuration",
                },
                onClickActions: new Action<Property, Material>[]
                {
                    // Note: only called when valid and visible so we can safely touch .Value:
                    (thisProperty, material) =>
                    {
                        Shader orig = Shader.Find(k_StackLitShaderName);
                        if (orig != null)
                        {
                            material.shader = orig;
                        }
                        else
                        {
                            Debug.LogWarning("Can't find original StackLit shader to reassign to material!");
                        }
                        thisProperty.BoolValue = false; // disable auto generation
                    },
                    (thisProperty, material) =>
                    {
                        // A bit brutish as this will be called again but it's idempotent:
                        SetupMaterialKeywordsAndPassWithOptions(material, doShaderGeneration: true, printSamplerSharingInfo: true);
                    },
                    (thisProperty, material) =>
                    {
                        // Print sampler config: we can only do that if we do the full config again.
                        // Should be idempotent anyway, but if auto-generation is not on, we will assume it is if the shader name assigned to the material
                        // starts with k_StackLitGeneratedShaderNamePrefix. Since we do an actual SetupMaterialKeywordsAndPassWithOptions() call, if the 
                        // generated shader is not the proper one, this will swap it out, too bad:
                        bool shaderHasGeneratedName = material.shader.name.StartsWith(TextureSamplerSharingShaderGenerator.k_StackLitGeneratedShaderNamePrefix);
                        // A bit brutish as this will be called again but it's idempotent:
                        SetupMaterialKeywordsAndPassWithOptions(material, doShaderGeneration: shaderHasGeneratedName, printSamplerSharingInfo: true);
                    },
                },
                displayUnder: (thisProperty, material) =>
                {
                    // We will indicate we're not sure the stats match up since we're not auto-generating, but we assume from the shader name assigned to the material:
                    // (thisProperty is the auto shader generation toggle)
                    bool shaderHasGeneratedName = material.shader.name.StartsWith(TextureSamplerSharingShaderGenerator.k_StackLitGeneratedShaderNamePrefix);
                    bool generatedStatsAreAssumed = shaderHasGeneratedName && (thisProperty.BoolValue == false);
                    //bool showGeneratedStats = (thisProperty.BoolValue == true) || shaderHasGeneratedName;

                    Vector4 vec = GetSamplerSharingUsage(material, generated: shaderHasGeneratedName);
                    if (!vec.Equals(Vector4.zero))
                    {
                        int sharedSamplerUsedNumDefine = TextureSamplerSharing.GetMaterialSharedSamplerUsedNumDefineProperty(material);
                        string generatedStatsString = (generatedStatsAreAssumed ? " (generated shader stats assumed)" : "");
                        EditorGUILayout.LabelField("Shared sampler slots used: " + vec.x);
                        EditorGUILayout.LabelField("Shared sampler slots max: "
                            // generated shaders can always use the maximum SharedSamplersMaxNum unless we actually reach the engine limit from other sampler uses 
                            // outside our reserved slots:
                            + (shaderHasGeneratedName ? TextureSamplerSharing.SharedSamplersMaxNum.ToString() + generatedStatsString : sharedSamplerUsedNumDefine.ToString()));
                        EditorGUILayout.LabelField("Built-in (also shared) samplers used: " + vec.y);
                        EditorGUILayout.LabelField("Map's own samplers used: " + vec.z + generatedStatsString);
                        EditorGUILayout.LabelField("Total samplers used: " + (vec.x + vec.y + vec.z));
                    }
                },
                guiText: "Enable Sampler Sharing Auto Shader Generation", toolTip: "Enable Sampler Sharing Auto Shader Generation", isMandatory: true);

            EnableGeometricNormalFiltering = new Property(this, k_GeometricNormalFilteringEnabled, "Enable Geometric Normal filtering", "Enable specular antialiasing", true);
            EnableTextureNormalFiltering = new Property(this, k_TextureNormalFilteringEnabled, "Enable Normal Texture filtering", "Require normal map to use _NF or _OSNF suffix for normal map name", true);

            // This property appears after one which references it:
            var BentNormal = new TextureProperty(this, k_BentNormalMap, "", "Bent Normal Map", "Bent Normal Map", pairConstantWithTexture: true, isMandatory: false, isNormalMap: true, showScaleOffset: false,
                slaveTexOneLineProp: null, isVisible: null, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);

            // --------------------------------------------------------------------------
            // Variable display configuration sections (depend on actual property values)
            // --------------------------------------------------------------------------

            // Base parametrization
            BaseParametrization = new ComboProperty(this, k_BaseParametrization, "Base Parametrization", Enum.GetNames(typeof(StackLit.BaseParametrization)), false);

            var BaseColor = new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Base Color + Opacity", "Albedo (RGB) and Opacity (A)", true, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var Metallic = new TextureProperty(this, k_MetallicMap, k_Metallic, "Metallic", "Metallic", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var DielectricIor = new Property(this, k_DielectricIor, "DieletricIor", "IOR use for dielectric material (i.e non metallic material)", false);
            var SmoothnessA = new TextureProperty(this, k_SmoothnessAMap, k_SmoothnessA, "Smoothness", "Smoothness", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var AnisotropyA = new TextureProperty(this, k_AnisotropyAMap, k_AnisotropyA, "AnisotropyA", "Anisotropy of primary lobe of base layer", pairConstantWithTexture: false, isMandatory: false,
                UIRangeLimits: new Vector2(-1.0f, 1.0f),
                isVisible: _ => EnableAnisotropy.BoolValue == true, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var TangentMap = new TextureProperty(this, k_TangentMap, "", "Tangent Map", "Tangent Map", pairConstantWithTexture: false, isMandatory: false, isNormalMap: true, showScaleOffset: true, slaveTexOneLineProp: null, UIRangeLimits: null,
                isVisible: _ => EnableAnisotropy.BoolValue == true, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var NormalMap = new TextureProperty(this, k_NormalMap, k_NormalScale, "Normal Map", "Normal Map", pairConstantWithTexture: true, isMandatory: false, isNormalMap: true, showScaleOffset: true,
                slaveTexOneLineProp: BentNormal.m_TextureProperty,
                isVisible: null, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var AmbientOcclusion = new TextureProperty(this, k_AmbientOcclusionMap, k_AmbientOcclusion, "AmbientOcclusion", "AmbientOcclusion Map", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var SpecularColor = new TextureProperty(this, k_SpecularColorMap, k_SpecularColor, "Specular Color (f0)", "Specular Color  (f0) (RGB)", true, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var EnergyConservingSpecularColor = new Property(this, k_EnergyConservingSpecularColor, "Energy Conserving Specular Color", "Mimics legacy Unity and Lit shader to balance diffuse and specular color", false);

            var StandardMetallicGroup = new GroupProperty(this, "_Standard", "Standard Basecolor and Metallic", new BaseProperty[]
            {
                BaseParametrization,
                BaseColor,
                Metallic,
                DielectricIor,
                SmoothnessA,
                AnisotropyA,
                TangentMap,
                NormalMap,
                BentNormal,
                AmbientOcclusion,
            }, isVisible: _ => (IsMetallicParametrizationUsed));

            // We keep the name "_Standard" so that the same UI-used foldout property "_StandardShow" is used
            var StandardSpecularColorGroup = new GroupProperty(this, "_Standard", "Standard Diffuse and Specular Color", new BaseProperty[]
            {
                BaseParametrization,
                BaseColor,
                SpecularColor,
                EnergyConservingSpecularColor,
                SmoothnessA,
                AnisotropyA,
                TangentMap,
                NormalMap,
                BentNormal,
                AmbientOcclusion,
            }, isVisible: _ => (!IsMetallicParametrizationUsed));

            // Dual specular lobe parametrizations:
            DualSpecularLobeParametrization = new ComboProperty(this, k_DualSpecularLobeParametrization, "Dual Specular Lobe Parametrization", Enum.GetNames(typeof(StackLit.DualSpecularLobeParametrization)), false);

            var SmoothnessB = new TextureProperty(this, k_SmoothnessBMap, k_SmoothnessB, "Smoothness B", "Smoothness B", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var AnisotropyB = new TextureProperty(this, k_AnisotropyBMap, k_AnisotropyB, "AnisotropyB", "Anisotropy of secondary lobe of base layer", pairConstantWithTexture: false, isMandatory: false,
                UIRangeLimits: new Vector2(-1.0f, 1.0f),
                isVisible: _ => EnableAnisotropy.BoolValue == true, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var LobeMix = new TextureProperty(this, k_LobeMixMap, k_LobeMix, "LobeMix", "LobeMix", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var Haziness = new TextureProperty(this, k_HazinessMap, k_Haziness, "Haziness", "Haziness", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);
            var HazeExtent = new TextureProperty(this, k_HazeExtentMap, k_HazeExtent, "Haze Extent", "Haze Extent", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled);

            CapHazinessWrtMetallic = new Property(this, k_CapHazinessWrtMetallic, "Cap Haziness Wrt Metallic", "Cap Haziness To Agree With Metallic", false,
                _ => (IsMetallicParametrizationUsed));

            var HazyGlossMaxDielectricF0UI = new UIBufferedProperty(this, k_HazyGlossMaxDielectricF0, "Maximum Dielectric Specular Color", "Cap Dielectrics To This Maximum Dielectric Specular Color", false,
                _ => (IsCapDielectricUsed));

            var HazyGlossMaxDielectricF0 = new Property(this, k_HazyGlossMaxDielectricF0, "Maximum Dielectric Specular Color", "Cap Dielectrics To This Maximum Dielectric Specular Color", true, _ => (false));
            // ...this later property is always used by the shader when IsMetallicParametrizationUsed and IsHazyGlossParametrizationUsed, but since these are dynamic, 
            // we always make the base property mandatory.

            var DualSpecularLobeDirectGroup = new GroupProperty(this, "_DualSpecularLobe", "Dual Specular Lobe (Direct Control Mode)", new BaseProperty[]
            {
                DualSpecularLobeParametrization,
                SmoothnessB,
                AnisotropyB,
                LobeMix,
            }, isVisible: _ => ( EnableDualSpecularLobe.BoolValue == true && !IsHazyGlossParametrizationUsed) );

            var DualSpecularLobeHazyGlossGroup = new GroupProperty(this, "_DualSpecularLobe", "Dual Specular Lobe (Hazy Gloss Mode)", new BaseProperty[]
            {
                DualSpecularLobeParametrization,
                Haziness,
                CapHazinessWrtMetallic,
                HazyGlossMaxDielectricF0UI,
                HazyGlossMaxDielectricF0, // dummy, always hidden, we create it just to force the isMandatory check
                HazeExtent,
                AnisotropyB,
            }, isVisible: _ => (EnableDualSpecularLobe.BoolValue == true && IsHazyGlossParametrizationUsed) );

            // All material properties
            // All GroupPropery below need to define a
            // [HideInInspector] _XXXShow("_XXXShow", Float) = 0.0 parameter in the StackLit.shader to work

            _materialProperties = new GroupProperty(this, "_Material", new BaseProperty[]
            {
                new GroupProperty(this, "_MaterialFeatures", "Material Features", new BaseProperty[]
                {
                    EnableDetails,
                    EnableSpecularOcclusion,
                    EnableDualSpecularLobe,
                    EnableAnisotropy,
                    EnableCoat,
                    EnableCoatNormalMap,
                    EnableIridescence,
                    EnableSSS,
                    EnableTransmission,
                    EnableSamplerSharing
                }),

                StandardMetallicGroup,
                StandardSpecularColorGroup,

                new GroupProperty(this, "_Details", "Details (base layer)", new BaseProperty[]
                {
                    new TextureProperty(this, k_DetailMaskMap, "", "Detail Mask Map", "Detail Mask Map", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                    new TextureProperty(this, k_DetailNormalMap, k_DetailNormalScale, "Detail Normal Map", "Detail Normal Map Scale", true, false, true, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                    new TextureProperty(this, k_DetailSmoothnessMap, k_DetailSmoothnessScale, "Detail Smoothness", "Detail Smoothness", true, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                }, isVisible: _ => EnableDetails.BoolValue == true),

                DualSpecularLobeDirectGroup,
                DualSpecularLobeHazyGlossGroup,

                new GroupProperty(this, "_Coat", "Coat", new BaseProperty[]
                {
                    new TextureProperty(this, k_CoatSmoothnessMap, k_CoatSmoothness, "Coat smoothness", "Coat smoothness", false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                    new TextureProperty(this, k_CoatNormalMap, k_CoatNormalScale, "Coat Normal Map", "Coat Normal Map",
                                        pairConstantWithTexture:true, isMandatory:false, isNormalMap:true, showScaleOffset:true, slaveTexOneLineProp:null,
                                        isVisible: _ => EnableCoatNormalMap.BoolValue == true, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),

                    new TextureProperty(this, k_CoatIorMap, k_CoatIor, "Coat IOR", "Index of refraction", false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                    new TextureProperty(this, k_CoatThicknessMap, k_CoatThickness, "Coat Thickness", "Coat thickness", false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                    new TextureProperty(this, k_CoatExtinctionMap, k_CoatExtinction, "Coat Absorption", "Coat absorption tint (the thicker the coat, the more that color is removed)", true, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),

                }, isVisible: _ => EnableCoat.BoolValue == true),

                new GroupProperty(this, "_Iridescence", "Iridescence", new BaseProperty[]
                {
                    //just to test: to use the same EvalIridescence as lit, find a good mapping for the top IOR (over the iridescence dielectric film)
                    //when having iridescence:
                    //new Property(this, "_IridescenceIor", "TopIOR", "Index of refraction on top of iridescence layer", false),
                    new TextureProperty(this, k_IridescenceMaskMap, k_IridescenceMask, "Iridescence Mask", "Iridescence Mask", false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                    new TextureProperty(this, k_IridescenceThicknessMap, k_IridescenceThickness, "Iridescence thickness (Remap to 0..3000nm)", "Iridescence thickness (Remap to 0..3000nm)", false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                }, isVisible: _ => EnableIridescence.BoolValue == true),

                new GroupProperty(this, "_SSS", "Sub-Surface Scattering", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfile, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false),
                    new TextureProperty(this, k_SubsurfaceMaskMap, k_SubsurfaceMask, "Subsurface mask map (R)", "Determines the strength of the subsurface scattering effect.", false, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                }, isVisible: _ => EnableSSS.BoolValue == true),

                new GroupProperty(this, "_Transmission", "Transmission", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfile, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false, _ => EnableSSS.BoolValue == false),
                    new TextureProperty(this, k_ThicknessMap, k_Thickness, "Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.", false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                }, isVisible: _ => EnableTransmission.BoolValue == true),

                new GroupProperty(this, "_Emissive", "Emissive", new BaseProperty[]
                {
                    new TextureProperty(this, k_EmissiveColorMap, k_EmissiveColor, "Emissive Color", "Emissive", true, false, samplerSharingEnabled: (_, __) => IsSamplerSharingGlobalEnabled),
                    new Property(this, k_AlbedoAffectEmissive, "Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.", false),
                }),

                new GroupProperty(this, "_SpecularAntiAliasing", "Specular Anti-Aliasing", new BaseProperty[]
                {
                    EnableTextureNormalFiltering,
                    EnableGeometricNormalFiltering,
                    new Property(this, "_SpecularAntiAliasingThreshold", "Threshold", "Threshold", false, _ => (EnableGeometricNormalFiltering.BoolValue || EnableTextureNormalFiltering.BoolValue) == true),
                    new Property(this, "_SpecularAntiAliasingScreenSpaceVariance", "Screen Space Variance", "Screen Space Variance (should be less than 0.25)", false, _ => EnableGeometricNormalFiltering.BoolValue == true),
                }),

                new GroupProperty(this, "_SamplerSharing", "Sampler Sharing", new BaseProperty[]
                {
                    EnableSamplerSharingAutoShaderGeneration,
                }, isVisible: _ => EnableSamplerSharing.BoolValue == true),

                new GroupProperty(this, "_Debug", "Advanced", new BaseProperty[]
                {
                    new Property(this, k_EnableAnisotropyForAreaLights, "Enable Anisotropy For Area Lights", "", false),
                    new Property(this, k_VlayerRecomputePerLight, "Vlayer Recompute Per Light", "", false),
                    new Property(this, k_VlayerUseRefractedAnglesForBase, "Vlayer Use Refracted Angles For Base", "", false),
                    new Property(this, k_DebugEnable, "Debug Enable", "Switch to a debug version of the shader", false),
                    new Property(this, "_DebugEnvLobeMask", "DebugEnvLobeMask", "xyz is Environments Lobe 0 1 2 Enable, w is Enable VLayering", false),
                    new Property(this, "_DebugLobeMask", "DebugLobeMask", "xyz is Analytical Lobe 0 1 2 Enable", false),
                    new Property(this, "_DebugAniso", "DebugAniso", "x is Hack Enable, y is factor", false),
                    new Property(this, "_DebugSpecularOcclusion", "DebugSpecularOcclusion", "eg (2,2,1,2), .x = {0 = fromAO, 1 = conecone, 2 = SPTD} .y = bentao algo {0 = uniform, cos, bent cos}, .z = use hemisphere clipping", false),
                    // The last component of _DebugSpecularOcclusion controls debug visualization: -1 colors the object according to the SO algorithm used, 
                    // and values from 1 to 4 controls what the lighting debug display mode will show when set to show "indirect specular occlusion":
                    // Since there's not one value in our case, 0 will show the object all red to indicate to choose one, 1-4 corresponds to showing
                    // 1 = coat SO, 2 = base lobe A SO, 3 = base lobe B SO, 4 = shows the result of sampling the SSAO texture (screenSpaceAmbientOcclusion).
                }),
            });
        }

        protected override bool ShouldEmissionBeEnabled(Material material)
        {
            return (material.GetColor(k_EmissiveColor) != Color.black) || material.GetTexture(k_EmissiveColorMap);
        }

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);
            _baseMaterialProperties.OnFindProperty(props);
        }

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            //TODO
            //base.FindMaterialProperties(props);
            _materialProperties.OnFindProperty(props);
        }

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI(); // This is from BaseUnlitGUI (BaseUnlitUI.cs) and finish with the double sided option.
            _baseMaterialProperties.OnGUI(null); // TODOTODO warning: the built groupproperty for basematerialproperties should not need the material.
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            //if (GUILayout.Button("Generate All Properties"))
            //{
            //    Debug.Log(_materialProperties.ToShaderPropertiesStringInternal());
            //}
            
            using (var header = new HeaderScope(StylesStackLit.stackOptionText, (uint)Expendable.Input, this, spaceAtEnd: false))
            {
                if (header.expended)
                    _materialProperties.OnGUI(material);
            }
        }

        protected override void MaterialPropertiesAdvanceGUI(Material material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {
        }

        // Driven by BaseUnlitGUI:ShaderPropertiesGUI:
        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPassWithOptions(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the materials with a script if code change
        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            // This function should not be driven by GUI, so we allow refreshing SamplerUsedNumDefineProperty from the main shader source
            SetupMaterialKeywordsAndPassWithOptions(material, refreshSharedSamplerUsedNumDefineProperty: true);
        }

        public static void SetupMaterialKeywordsAndPassWithOptions(Material material,
            bool doShaderGeneration = false, bool printSamplerSharingInfo = false, bool refreshSharedSamplerUsedNumDefineProperty = false)
        {
            // TODO check SetupBaseLitKeywords()
            // Base UI:
            SetupBaseUnlitKeywords(material);
            SetupBaseUnlitMaterialPass(material);

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;

            if (doubleSidedEnable)
            {
                BaseLitGUI.DoubleSidedNormalMode doubleSidedNormalMode =
                    (BaseLitGUI.DoubleSidedNormalMode)material.GetFloat(k_DoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case BaseLitGUI.DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case BaseLitGUI.DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case BaseLitGUI.DoubleSidedNormalMode.None: // None mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;
                }
            }

            SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor", material);

            //TODO: disable DBUFFER

            //
            // First, we determine what features are enabled and setup keywords accordingly.
            // The enabled bools will be reused for texture map properties configuration.
            //
            bool detailsEnabled = material.HasProperty(k_EnableDetails) && material.GetFloat(k_EnableDetails) > 0.0f;
            CoreUtils.SetKeyword(material, "_DETAILMAP", detailsEnabled); // todo: should be reserved for actual map present

            bool bentNormalMapPresent = material.HasProperty(k_BentNormalMap) && material.GetTexture(k_BentNormalMap);
            CoreUtils.SetKeyword(material, "_BENTNORMALMAP", bentNormalMapPresent);

            bool specularOcclusionEnabled = material.HasProperty(k_EnableSpecularOcclusion) && material.GetFloat(k_EnableSpecularOcclusion) > 0.0f;
            CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", specularOcclusionEnabled);

            bool specularColorEnabled = material.HasProperty(k_BaseParametrization) 
                                        && ((StackLit.BaseParametrization)material.GetFloat(k_BaseParametrization) == StackLit.BaseParametrization.SpecularColor);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SPECULAR_COLOR", specularColorEnabled);

            bool dualSpecularLobeEnabled = material.HasProperty(k_EnableDualSpecularLobe) && material.GetFloat(k_EnableDualSpecularLobe) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_DUAL_SPECULAR_LOBE", dualSpecularLobeEnabled);

            bool hazyGlossEnabled = dualSpecularLobeEnabled && material.HasProperty(k_DualSpecularLobeParametrization)
                                        && ((StackLit.DualSpecularLobeParametrization)material.GetFloat(k_DualSpecularLobeParametrization) == StackLit.DualSpecularLobeParametrization.HazyGloss);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_HAZY_GLOSS", hazyGlossEnabled);

            // This is not a keyword but we validate an input here that act as one:
            // HazyGlossMaxDielectricF0
            bool hazyGlossMaxDielectricF0Required = hazyGlossEnabled && (specularColorEnabled == false);
            bool hazyGlossUseMaxDielectricF0 = hazyGlossMaxDielectricF0Required
                                               && material.HasProperty(k_CapHazinessWrtMetallic) && material.GetFloat(k_CapHazinessWrtMetallic) > 0.0f;
            if (hazyGlossMaxDielectricF0Required)
            {
                if  (hazyGlossUseMaxDielectricF0 == false)
                {
                    // In that case, the shader expects k_HazyGlossMaxDielectricF0 so we don't pre-check material.HasProperty(k_HazyGlossMaxDielectricF0),
                    // it is considered mandatory.
                    // The use of the option is nevertheless disabled, so we need to make sure the value (again that is used anyway in StackLitData) 
                    // will be set to its neutral input:
                    material.SetFloat(k_HazyGlossMaxDielectricF0, 1.0f);
                }
                else
                {
                    // In that case, the UI is supposed to have a valid UI value, forward it to the shader-used value:
                    UIBufferedProperty.SetupUIBufferedMaterialProperty(material, k_HazyGlossMaxDielectricF0, MaterialProperty.PropType.Float);
                }
            }

            bool anisotropyEnabled = material.HasProperty(k_EnableAnisotropy) && material.GetFloat(k_EnableAnisotropy) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_ANISOTROPY", anisotropyEnabled);

            bool tangentMapPresentAndEnabled = anisotropyEnabled && material.HasProperty(k_TangentMap) && material.GetTexture(k_TangentMap);
            CoreUtils.SetKeyword(material, "_TANGENTMAP", tangentMapPresentAndEnabled);

            bool iridescenceEnabled = material.HasProperty(k_EnableIridescence) && material.GetFloat(k_EnableIridescence) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_IRIDESCENCE", iridescenceEnabled);

            bool transmissionEnabled = material.HasProperty(k_EnableTransmission) && material.GetFloat(k_EnableTransmission) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", transmissionEnabled);

            bool sssEnabled = material.HasProperty(k_EnableSubsurfaceScattering) && material.GetFloat(k_EnableSubsurfaceScattering) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", sssEnabled);

            bool coatEnabled = material.HasProperty(k_EnableCoat) && material.GetFloat(k_EnableCoat) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_COAT", coatEnabled);

            bool coatNormalMapEnabled = coatEnabled && material.HasProperty(k_EnableCoatNormalMap) && material.GetFloat(k_EnableCoatNormalMap) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_COAT_NORMALMAP", coatNormalMapEnabled);

            // Advanced options to remove once dev is finished or hide
            bool anisotropyForAreaLights = (anisotropyEnabled) && material.HasProperty(k_EnableAnisotropyForAreaLights) && material.GetFloat(k_EnableAnisotropyForAreaLights) > 0.0f;
            CoreUtils.SetKeyword(material, "_ANISOTROPY_FOR_AREA_LIGHTS", anisotropyForAreaLights);

            bool vlayerRecomputePerLight = (coatEnabled || iridescenceEnabled) && material.HasProperty(k_VlayerRecomputePerLight) && material.GetFloat(k_VlayerRecomputePerLight) > 0.0f;
            CoreUtils.SetKeyword(material, "_VLAYERED_RECOMPUTE_PERLIGHT", vlayerRecomputePerLight);

            bool vlayerUseRefractedAnglesForBase = coatEnabled && material.HasProperty(k_VlayerUseRefractedAnglesForBase) && material.GetFloat(k_VlayerUseRefractedAnglesForBase) > 0.0f;
            CoreUtils.SetKeyword(material, "_VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE", vlayerUseRefractedAnglesForBase);

            bool debugEnabled = material.HasProperty(k_DebugEnable) && material.GetFloat(k_DebugEnable) > 0.0f;
            CoreUtils.SetKeyword(material, "_STACKLIT_DEBUG", debugEnabled);

            //
            // Setup texture material properties used for sampling and processing map values:
            //
            bool samplerSharingEnabled = material.HasProperty(k_EnableSamplerSharing) && material.GetFloat(k_EnableSamplerSharing) > 0.0f;
            TextureSamplerSharing samplerSharing = null;
            TextureSamplerSharingShaderGenerator shaderGenerator = null;
            bool samplerSharingGenerationEnabled = samplerSharingEnabled && (doShaderGeneration || (material.HasProperty(k_EnableSamplerSharingAutoShaderGeneration) && material.GetFloat(k_EnableSamplerSharingAutoShaderGeneration) > 0.0f));
            if (samplerSharingEnabled)
            {
                if(samplerSharingGenerationEnabled)
                {
                    shaderGenerator = new TextureSamplerSharingShaderGenerator();
                }
                // sharedSamplerUsedNumDefine is a *material property* assumed to be caching the #define in the .shader to avoid parsing it at every UI tick.
                // It defines the number of reserved shared sampler slots available to the sampler sharing system.
                // This is not used when generating, as the required amount is automatically calculated and #defined in the generated shader.
                // if we called SetupMaterialKeywordsAndPassWithOptions using any of the flags, this is not a normal UI tick callback, we force a read from file
                // by constructing the TextureSamplerSharing with sharedSamplerUsedNumDefine == 0.
                // In that case, TextureSamplerSharing also automatically updates that SamplerUsedNumDefine property.
                bool setupOptionUsed = doShaderGeneration || printSamplerSharingInfo || refreshSharedSamplerUsedNumDefineProperty;
                int sharedSamplerUsedNumDefine = setupOptionUsed ? 0 : TextureSamplerSharing.GetMaterialSharedSamplerUsedNumDefineProperty(material);

                samplerSharing = new TextureSamplerSharing(material, shaderName: k_StackLitShaderName,
                    assignmentCallback: (samplerClient, slot, isExternalSlot, isClientUnique) => 
                    TextureProperty.SetupUseMapOfTextureMaterialProperty(material, shaderGenerator, samplerClient, slot, isExternalSlot, isClientUnique),
                    definedSharedSamplerUsedNum: sharedSamplerUsedNumDefine);

                // We want to enable all our samplers to try to use even the engine parsed-by-name ones that are used in the shader:
                samplerSharing.AddExternalExistingSamplerStates();
            }
            else
            {
                // If sampler sharing is disabled, make sure the original shader is assigned to the material then:
                Shader orig = Shader.Find(k_StackLitShaderName);
                if (orig != null)
                {
                    material.shader = orig;
                }
                else
                {
                    Debug.LogWarning("Can't find original StackLit shader to reassign to material!");
                }
            }

            // Note: we disallow default value (unassigned map) normal map sampling when textureNormalFilteringEnabled because if we encoded variance, 0 is the neutral value
            // (that doesn't add roughness).
            bool textureNormalFilteringEnabled = material.HasProperty(k_TextureNormalFilteringEnabled) && material.GetFloat(k_TextureNormalFilteringEnabled) > 0.0f;
            bool allowUnassignedNormalSampling = (textureNormalFilteringEnabled == false);

            TextureProperty.SetupTextureMaterialProperty(material, k_BaseColor,            enableMap: true, allowUnassignedSampling: true, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_Normal,               enableMap: true, allowUnassignedSampling: allowUnassignedNormalSampling, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            // For the bentnormal map, UseMap will be set to 0 or 1, no samplerSharing is used for the actual sampler as the sampler for the normal map is used:
            TextureProperty.SetupTextureMaterialProperty(material, k_BentNormal,           enableMap: bentNormalMapPresent, samplerSharing: null);
            TextureProperty.SetupTextureMaterialProperty(material, k_Metallic,             enableMap: (!specularColorEnabled), samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_SpecularColor,        enableMap: specularColorEnabled, allowUnassignedSampling: true, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_SmoothnessA,          enableMap: true, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_SmoothnessB,          enableMap: (dualSpecularLobeEnabled && !hazyGlossEnabled), samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_LobeMix,              enableMap: (dualSpecularLobeEnabled && !hazyGlossEnabled), samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_Haziness,             enableMap: (dualSpecularLobeEnabled && hazyGlossEnabled), samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_HazeExtent,           enableMap: (dualSpecularLobeEnabled && hazyGlossEnabled), samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_AmbientOcclusion,     enableMap: true, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_SubsurfaceMask,       enableMap: sssEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_Thickness,            enableMap: (sssEnabled || transmissionEnabled), samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_AnisotropyA,          enableMap: anisotropyEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_AnisotropyB,          enableMap: anisotropyEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_Tangent,              enableMap: anisotropyEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator); // allowUnassignedSampling is false default, anisotropyEnabled is enough
            TextureProperty.SetupTextureMaterialProperty(material, k_IridescenceThickness, enableMap: iridescenceEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_IridescenceMask,      enableMap: iridescenceEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_CoatSmoothness,       enableMap: coatEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_CoatIor,              enableMap: coatEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_CoatThickness,        enableMap: coatEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_CoatExtinction,       enableMap: coatEnabled, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_CoatNormal,           enableMap: coatEnabled, allowUnassignedSampling: allowUnassignedNormalSampling, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);

            // details
            TextureProperty.SetupTextureMaterialProperty(material, k_DetailMask, enableMap: detailsEnabled, allowUnassignedSampling: true, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_DetailSmoothness, enableMap: detailsEnabled, allowUnassignedSampling: true, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);
            TextureProperty.SetupTextureMaterialProperty(material, k_DetailNormal, enableMap: detailsEnabled, allowUnassignedSampling: allowUnassignedNormalSampling, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);

            TextureProperty.SetupTextureMaterialProperty(material, k_EmissiveColor, enableMap: true, allowUnassignedSampling: true, samplerSharing: samplerSharing, shaderGenerator: shaderGenerator);

            if (samplerSharingEnabled)
            {
                int totalSamplersUsedNum = samplerSharing.DoClientAssignment();

                bool samplerSharingSpilled = samplerSharing.SharedSamplersNeededOnLastAssignement > TextureSamplerSharing.SharedSamplersMaxNum;
                if (samplerSharingSpilled)
                {
                    Debug.LogError("StackLit Sampler Sharing: too many shared sampler slots required, allow sharing on your textured parameters "
                        + "or change their texture importer settings to make them similar.\n");
                }
                if (samplerSharingSpilled || printSamplerSharingInfo)
                {
                    int definedSharedSamplerUsedNum = 0;
                    if (!TextureSamplerSharingShaderGenerator.GetOriginalShaderDefinedSharedSamplerUseNum(ref definedSharedSamplerUsedNum, material.shader))
                    {
                        Debug.LogWarning("Cannot find " + TextureSamplerSharingShaderGenerator.k_SharedSamplerUsedNumDefine + " in StackLit shader!");
                    }

                    Debug.LogFormat(TextureSamplerSharingShaderGenerator.k_SharedSamplerUsedNumDefine + " is " + "{0} " 
                        + (shaderGenerator != null && material.shader.name.Equals(k_StackLitShaderName, StringComparison.Ordinal) ? "(before generation) \n": "\n")
                        + "Maximum shared sampler slots available: {1} \n"
                        + "Needed shared sampler slots: {2} \n",
                        definedSharedSamplerUsedNum, (shaderGenerator != null ? TextureSamplerSharing.SharedSamplersMaxNum : definedSharedSamplerUsedNum), samplerSharing.SharedSamplersNeededOnLastAssignement);

                    Debug.Log("Sampler usage by textured parameters: ");
                    // Set a callback just to display the configuration:
                    samplerSharing.SetClientAssignmentCallback((samplerClient, slot, slotIsExternalSampler, isClientUnique) =>
                    {
                        bool mapUsesOwnSampler = slotIsExternalSampler && isClientUnique;
                        if (!mapUsesOwnSampler)
                        {
                            Debug.LogFormat("{0} uses slot: {1} from: {2} samplers; and this client {3}.",
                                samplerClient.BasePropertyName, slot,
                                (slotIsExternalSampler ? "built-in" : "shared"),
                                (isClientUnique ? "asked to use a UNIQUE slot!" : "SHARES its slot with others."));
                        }
                        else
                        {
                            Debug.LogFormat("{0} uses its own sampler (shader is generated).", samplerClient.BasePropertyName);
                        }
                    });
                    samplerSharing.DoClientAssignment(); // display the above for clients that are ok

                    if (samplerSharingSpilled)
                    {
                        Debug.LogError("List of textured parameters with potential problems: ");
                        // If we have generation, we could just setup those to use their own sampler, although in that case, there's a chance
                        // we would reach the engine limit too. We can just advise the user to use generation + exclude some maps from sharing,
                        // but an even better solution is to automatically raise the SHARED_SAMPLER_USED_NUM define (up to 13) when doing generation.
                        samplerSharing.DoSpilledClientAssignment((samplerClient, slot, slotIsExternalSampler, isClientUnique) =>
                        {
                            if (!slotIsExternalSampler)
                            {
                                Debug.LogErrorFormat("{0} {1} will be sampled using the most commonly used shared sampler",
                                    samplerClient.BasePropertyName,
                                    (isClientUnique ? "asked to use a UNIQUE slot but " : "SHARES its sampler but no others has same config, it "));
                                Debug.LogError("Use shader generation or raise SHARED_SAMPLER_USED_NUM in the .shader");
                            }
                        });
                    }
                }
                // Even without generation we could set a limiting keyword to limit the different cases of the shader switch statement
                // based on SharedSamplersNeededOnLastAssignement
                // ie : it would then set the SHARED_SAMPLER_USED_NUM value using multi compile. We have essentially better with shader
                // generation, and this leaves the shader without generation to be more responsive on changes without a cached compiled
                // shader.

                bool hasAssignedGeneratedShader = false;
                if (shaderGenerator != null)
                {
                    Shader originalShader = material.shader;
                    shaderGenerator.SetSamplerFinalConfigMD5();
                    bool found = hasAssignedGeneratedShader = shaderGenerator.HaveCurrentAndComptabibleGeneratedShader(material, tryAssignAlreadyGenerated: true);
                    if (!found)
                    {
                        // We need generation:
                        bool success = hasAssignedGeneratedShader = shaderGenerator.GenerateShader(material, assignGeneratedShader: true);
                        if (!success)
                        {
                            material.shader = originalShader;
                        }
                    }
                }
                // Save stats for the UI:
                SetSamplerSharingUsage(material,
                    new Vector4(samplerSharing.SharedSamplersUsedOnLastAssignement, samplerSharing.BuiltInSamplersUsedOnLastAssignement, samplerSharing.OwnSamplersUsedOnLastAssignement, totalSamplersUsedNum),
                    generated: hasAssignedGeneratedShader);
            }
            CoreUtils.SetKeyword(material, "_USE_SAMPLER_SHARING", samplerSharingEnabled);

            //
            // Check if we are using specific UVs (but only do it for potentially used maps):
            //
            TextureProperty.UVMapping baseColorMapUV            = (TextureProperty.UVMapping)material.GetFloat(k_BaseColorMapUV);
            TextureProperty.UVMapping normalMapUV               = (TextureProperty.UVMapping)material.GetFloat(k_NormalMapUV);
            TextureProperty.UVMapping metallicMapUV             = specularColorEnabled ? TextureProperty.UVMapping.UV0 : (TextureProperty.UVMapping)material.GetFloat(k_MetallicMapUV);
            TextureProperty.UVMapping specularColorMapUV        = specularColorEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_SpecularColorMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping smoothnessAMapUV          = (TextureProperty.UVMapping)material.GetFloat(k_SmoothnessAMapUV);
            TextureProperty.UVMapping smoothnessBMapUV          = (dualSpecularLobeEnabled && !hazyGlossEnabled) ? (TextureProperty.UVMapping)material.GetFloat(k_SmoothnessBMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping lobeMixMapUV              = (dualSpecularLobeEnabled && !hazyGlossEnabled) ? (TextureProperty.UVMapping)material.GetFloat(k_LobeMixMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping hazinessMapUV             = (dualSpecularLobeEnabled && hazyGlossEnabled) ? (TextureProperty.UVMapping)material.GetFloat(k_HazinessMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping hazeExtentMapUV           = (dualSpecularLobeEnabled && hazyGlossEnabled) ? (TextureProperty.UVMapping)material.GetFloat(k_HazeExtentMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping ambientOcclusionMapUV     = (TextureProperty.UVMapping)material.GetFloat(k_AmbientOcclusionMapUV);
            TextureProperty.UVMapping subsurfaceMaskMapUV       = sssEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_SubsurfaceMaskMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping thicknessMapUV            = sssEnabled || transmissionEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_ThicknessMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping anisotropyAMapUV          = anisotropyEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_AnisotropyAMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping anisotropyBMapUV          = anisotropyEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_AnisotropyBMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping tangentMapUV              = tangentMapPresentAndEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_TangentMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping iridescenceThicknessMapUV = iridescenceEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_IridescenceThicknessMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping iridescenceMaskMapUV      = iridescenceEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_IridescenceMaskMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping coatSmoothnessMapUV       = coatEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_CoatSmoothnessMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping coatIorMapUV              = coatEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_CoatIorMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping coatThicknessMapUV        = coatEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_CoatThicknessMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping coatExtinctionMapUV       = coatEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_CoatExtinctionMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping coatNormalMapUV           = coatEnabled && coatNormalMapEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_CoatNormalMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping detailMaskMapUV           = detailsEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_DetailMaskMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping detailSmoothnessMapUV     = detailsEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_DetailSmoothnessMapUV) : TextureProperty.UVMapping.UV0;
            TextureProperty.UVMapping detailNormalMapUV         = detailsEnabled ? (TextureProperty.UVMapping)material.GetFloat(k_DetailNormalMapUV) : TextureProperty.UVMapping.UV0;

            TextureProperty.UVMapping emissiveColorMapUV = (TextureProperty.UVMapping)material.GetFloat(k_EmissiveColorMapUV);

            TextureProperty.UVMapping[] uvIndices = new[]
            {
                baseColorMapUV,
                normalMapUV,
                metallicMapUV,
                specularColorMapUV,
                smoothnessAMapUV,
                smoothnessBMapUV,
                lobeMixMapUV,
                hazinessMapUV,
                hazeExtentMapUV,
                ambientOcclusionMapUV,
                subsurfaceMaskMapUV,
                thicknessMapUV,
                anisotropyAMapUV,
                anisotropyBMapUV,
                tangentMapUV,
                iridescenceThicknessMapUV,
                iridescenceMaskMapUV,
                coatSmoothnessMapUV,
                coatIorMapUV,
                coatThicknessMapUV,
                coatExtinctionMapUV,
                coatNormalMapUV,
                detailMaskMapUV,
                detailSmoothnessMapUV,
                detailNormalMapUV,
                emissiveColorMapUV,
            };

            //
            // Set keyword for required coordinate mappings:
            //

            //bool requireUv2 = false;
            //bool requireUv3 = false;
            bool requireTriplanar = false;
            for (int i = 0; i < uvIndices.Length; ++i)
            {
                //requireUv2 = requireUv2 || uvIndices[i] == TextureProperty.UVMapping.UV2;
                //requireUv3 = requireUv3 || uvIndices[i] == TextureProperty.UVMapping.UV3;
                requireTriplanar = requireTriplanar || uvIndices[i] == TextureProperty.UVMapping.Triplanar;
            }
            CoreUtils.SetKeyword(material, "_MAPPING_TRIPLANAR", requireTriplanar);

            //
            // Set the reference value for the stencil test - required for SSS
            //
            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            if (sssEnabled)
            {
                stencilRef = (int)StencilLightingUsage.SplitLighting;
            }

            // As we tag both during velocity pass and Gbuffer pass we need a separate state and we need to use the write mask
            material.SetInt(kStencilRef, stencilRef);
            material.SetInt(kStencilWriteMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
            material.SetInt(kStencilRefMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);
            material.SetInt(kStencilWriteMaskMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);

            // for depth only pass to be used in decal to normal buffer compositing
            material.SetInt(kStencilDepthPrepassRef, (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer);
            material.SetInt(kStencilDepthPrepassWriteMask, (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer);
        }
    }
} // namespace UnityEditor
