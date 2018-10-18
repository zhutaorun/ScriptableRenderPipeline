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
        protected const string k_CoatThickness = "_CoatThickness";
        protected const string k_CoatExtinction = "_CoatExtinction";
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

        protected const string k_EnableSamplerSharing = "_EnableSamplerSharing";

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

        private Property EnableSamplerSharing;

        private Property EnableGeometricNormalFiltering;
        private Property EnableTextureNormalFiltering;

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

        public StackLitGUI()
        {
            _baseMaterialProperties = new GroupProperty(this, "_BaseMaterial", new BaseProperty[]
            {
                // JFFTODO: Find the proper condition, and proper way to display this.
                new Property(this, k_DoubleSidedNormalMode, "Normal mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal.", false),
            });

            //
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

            EnableGeometricNormalFiltering = new Property(this, k_GeometricNormalFilteringEnabled, "Enable Geometric Normal filtering", "Enable specular antialiasing", true);
            EnableTextureNormalFiltering = new Property(this, k_TextureNormalFilteringEnabled, "Enable Normal Texture filtering", "Require normal map to use _NA or _OSNA suffix for normal map name", true);

            // This property appears after one which references it:
            var BentNormal = new TextureProperty(this, k_BentNormalMap, "", "Bent Normal Map", "Bent Normal Map", pairConstantWithTexture: true, isMandatory: false, isNormalMap: true, showScaleOffset: false);

            // --------------------------------------------------------------------------
            // Variable display configuration sections (depend on actual property values)
            // --------------------------------------------------------------------------

            // Base parametrization
            BaseParametrization = new ComboProperty(this, k_BaseParametrization, "Base Parametrization", Enum.GetNames(typeof(StackLit.BaseParametrization)), false);

            var BaseColor = new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Base Color + Opacity", "Albedo (RGB) and Opacity (A)", true, false);
            var Metallic = new TextureProperty(this, k_MetallicMap, k_Metallic, "Metallic", "Metallic", false, false);
            var DielectricIor = new Property(this, k_DielectricIor, "DieletricIor", "IOR use for dielectric material (i.e non metallic material)", false);
            var SmoothnessA = new TextureProperty(this, k_SmoothnessAMap, k_SmoothnessA, "Smoothness", "Smoothness", false, false);
            var AnisotropyA = new TextureProperty(this, k_AnisotropyAMap, k_AnisotropyA, "AnisotropyA", "Anisotropy of primary lobe of base layer", pairConstantWithTexture: false, isMandatory: false,
                rangeUILimits: new TextureProperty.RangeMinMax() { MinLimit = -1.0f, MaxLimit = 1.0f },
                isVisible: _ => EnableAnisotropy.BoolValue == true);
            var TangentMap = new TextureProperty(this, k_TangentMap, "", "Tangent Map", "Tangent Map", pairConstantWithTexture: false, isMandatory: false, isNormalMap: true, showScaleOffset: true, slaveTexOneLineProp: null, rangeUILimits: null,
                isVisible: _ => EnableAnisotropy.BoolValue == true);
            var NormalMap = new TextureProperty(this, k_NormalMap, k_NormalScale, "Normal Map", "Normal Map", pairConstantWithTexture: true, isMandatory: false, isNormalMap: true, showScaleOffset: true, slaveTexOneLineProp: BentNormal.m_TextureProperty);
            var AmbientOcclusion = new TextureProperty(this, k_AmbientOcclusionMap, k_AmbientOcclusion, "AmbientOcclusion", "AmbientOcclusion Map", false, false);
            var SpecularColor = new TextureProperty(this, k_SpecularColorMap, k_SpecularColor, "Specular Color (f0)", "Specular Color  (f0) (RGB)", true, false);
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
            }, _ => (IsMetallicParametrizationUsed));

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
            }, _ => (!IsMetallicParametrizationUsed));

            // Dual specular lobe parametrizations:
            DualSpecularLobeParametrization = new ComboProperty(this, k_DualSpecularLobeParametrization, "Dual Specular Lobe Parametrization", Enum.GetNames(typeof(StackLit.DualSpecularLobeParametrization)), false);

            var SmoothnessB = new TextureProperty(this, k_SmoothnessBMap, k_SmoothnessB, "Smoothness B", "Smoothness B", false, false);
            var AnisotropyB = new TextureProperty(this, k_AnisotropyBMap, k_AnisotropyB, "AnisotropyB", "Anisotropy of secondary lobe of base layer", pairConstantWithTexture: false, isMandatory: false,
                rangeUILimits: new TextureProperty.RangeMinMax() { MinLimit = -1.0f, MaxLimit = 1.0f },
                isVisible: _ => EnableAnisotropy.BoolValue == true);
            var LobeMix = new TextureProperty(this, k_LobeMixMap, k_LobeMix, "LobeMix", "LobeMix", false, false);
            var Haziness = new TextureProperty(this, k_HazinessMap, k_Haziness, "Haziness", "Haziness", false, false);
            var HazeExtent = new TextureProperty(this, k_HazeExtentMap, k_HazeExtent, "Haze Extent", "Haze Extent", false, false);

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
            }, _ => ( EnableDualSpecularLobe.BoolValue == true && !IsHazyGlossParametrizationUsed) );

            var DualSpecularLobeHazyGlossGroup = new GroupProperty(this, "_DualSpecularLobe", "Dual Specular Lobe (Hazy Gloss Mode)", new BaseProperty[]
            {
                DualSpecularLobeParametrization,
                Haziness,
                CapHazinessWrtMetallic,
                HazyGlossMaxDielectricF0UI,
                HazyGlossMaxDielectricF0, // dummy, always hidden, we create it just to force the isMandatory check
                HazeExtent,
                AnisotropyB,
            }, _ => (EnableDualSpecularLobe.BoolValue == true && IsHazyGlossParametrizationUsed) );

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
                    new TextureProperty(this, k_DetailMaskMap, "", "Detail Mask Map", "Detail Mask Map", false, false),
                    new TextureProperty(this, k_DetailNormalMap, k_DetailNormalScale, "Detail Normal Map", "Detail Normal Map Scale", true, false, true),
                    new TextureProperty(this, k_DetailSmoothnessMap, k_DetailSmoothnessScale, "Detail Smoothness", "Detail Smoothness", true, false),
                }, _ => EnableDetails.BoolValue == true),

                DualSpecularLobeDirectGroup,
                DualSpecularLobeHazyGlossGroup,

                new GroupProperty(this, "_Coat", "Coat", new BaseProperty[]
                {
                    new TextureProperty(this, k_CoatSmoothnessMap, k_CoatSmoothness, "Coat smoothness", "Coat smoothness", false),
                    new TextureProperty(this, k_CoatNormalMap, k_CoatNormalScale, "Coat Normal Map", "Coat Normal Map", 
                                        pairConstantWithTexture:true, isMandatory:false, isNormalMap:true, showScaleOffset:true, slaveTexOneLineProp:null, isVisible: _ => EnableCoatNormalMap.BoolValue == true),
                    new Property(this, "_CoatIor", "Coat IOR", "Index of refraction", false),
                    new Property(this, "_CoatThickness", "Coat Thickness", "Coat thickness", false),
                    new Property(this, "_CoatExtinction", "Coat Absorption", "Coat absorption tint (the thicker the coat, the more that color is removed)", false),
                }, _ => EnableCoat.BoolValue == true),

                new GroupProperty(this, "_Iridescence", "Iridescence", new BaseProperty[]
                {
                    //just to test: to use the same EvalIridescence as lit, find a good mapping for the top IOR (over the iridescence dielectric film)
                    //when having iridescence:
                    //new Property(this, "_IridescenceIor", "TopIOR", "Index of refraction on top of iridescence layer", false),
                    new TextureProperty(this, k_IridescenceMaskMap, k_IridescenceMask, "Iridescence Mask", "Iridescence Mask", false),
                    new TextureProperty(this, k_IridescenceThicknessMap, k_IridescenceThickness, "Iridescence thickness (Remap to 0..3000nm)", "Iridescence thickness (Remap to 0..3000nm)", false),
                }, _ => EnableIridescence.BoolValue == true),

                new GroupProperty(this, "_SSS", "Sub-Surface Scattering", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfile, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false),
                    new TextureProperty(this, k_SubsurfaceMaskMap, k_SubsurfaceMask, "Subsurface mask map (R)", "Determines the strength of the subsurface scattering effect.", false, false),
                }, _ => EnableSSS.BoolValue == true),

                new GroupProperty(this, "_Transmission", "Transmission", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfile, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false, _ => EnableSSS.BoolValue == false),
                    new TextureProperty(this, k_ThicknessMap, k_Thickness, "Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.", false),
                }, _ => EnableTransmission.BoolValue == true),

                new GroupProperty(this, "_Emissive", "Emissive", new BaseProperty[]
                {
                    new TextureProperty(this, k_EmissiveColorMap, k_EmissiveColor, "Emissive Color", "Emissive", true, false),
                    new Property(this, k_AlbedoAffectEmissive, "Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.", false),
                }),

                new GroupProperty(this, "_SpecularAntiAliasing", "Specular Anti-Aliasing", new BaseProperty[]
                {
                    EnableTextureNormalFiltering,
                    EnableGeometricNormalFiltering,
                    new Property(this, "_SpecularAntiAliasingThreshold", "Threshold", "Threshold", false, _ => (EnableGeometricNormalFiltering.BoolValue || EnableTextureNormalFiltering.BoolValue) == true),
                    new Property(this, "_SpecularAntiAliasingScreenSpaceVariance", "Screen Space Variance", "Screen Space Variance (should be less than 0.25)", false, _ => EnableGeometricNormalFiltering.BoolValue == true),
                }),

                new GroupProperty(this, "_Debug", "Debug", new BaseProperty[]
                {
                    new Property(this, "_VlayerRecomputePerLight", "Vlayer Recompute Per Light", "", false),
                    new Property(this, "_VlayerUseRefractedAnglesForBase", "Vlayer Use Refracted Angles For Base", "", false),
                    new Property(this, "_DebugEnable", "Debug Enable", "Switch to a debug version of the shader", false),
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
            //base.FindMaterialProperties(props);
            _materialProperties.OnFindProperty(props);
        }

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();
            _baseMaterialProperties.OnGUI();
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
                    _materialProperties.OnGUI();
            }
        }

        protected override void MaterialPropertiesAdvanceGUI(Material material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        public static void SetupMaterialKeywordsAndPass(Material material)
        {
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

            // TEMP - Remove once dev is finish
            bool debugEnabled = material.HasProperty("_DebugEnable") && material.GetFloat("_DebugEnable") > 0.0f;
            CoreUtils.SetKeyword(material, "_STACKLIT_DEBUG", debugEnabled);

            bool vlayerRecomputePerLight = (coatEnabled || iridescenceEnabled) && material.HasProperty("_VlayerRecomputePerLight") && material.GetFloat("_VlayerRecomputePerLight") > 0.0f;
            CoreUtils.SetKeyword(material, "_VLAYERED_RECOMPUTE_PERLIGHT", vlayerRecomputePerLight);

            bool vlayerUseRefractedAnglesForBase = coatEnabled && material.HasProperty("_VlayerUseRefractedAnglesForBase") && material.GetFloat("_VlayerUseRefractedAnglesForBase") > 0.0f;
            CoreUtils.SetKeyword(material, "_VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE", vlayerUseRefractedAnglesForBase);

            //
            // Setup texture material properties used for sampling and processing map values:
            //
            bool samplerSharingEnabled = material.HasProperty(k_EnableSamplerSharing) && material.GetFloat(k_EnableSamplerSharing) > 0.0f;
            TextureSamplerSharing samplerSharing = samplerSharingEnabled ? 
                new TextureSamplerSharing(material, (a, b, c) => TextureProperty.SetupUseMapOfTextureMaterialProperty(material, a, b, c)) : null;
            if (samplerSharingEnabled)
            {
                // We want to enable all our samplers to try to use even the engine parsed-by-name ones that
                // are used in the shader:
                samplerSharing.AddExternalExistingSamplerStates();
            }
            // Note: we disallow default value (unassigned map) normal map sampling when textureNormalFilteringEnabled because if we encoded variance, 0 is the neutral value
            // (that doesn't add roughness).
            bool textureNormalFilteringEnabled = material.HasProperty(k_TextureNormalFilteringEnabled) && material.GetFloat(k_TextureNormalFilteringEnabled) > 0.0f;
            bool allowUnassignedNormalSampling = (textureNormalFilteringEnabled == false);

            TextureProperty.SetupTextureMaterialProperty(material, k_BaseColor,            enableMap: true, allowUnassignedSampling: true, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_Normal,               enableMap: true, allowUnassignedSampling: allowUnassignedNormalSampling, samplerSharing: samplerSharing);
            // For the bentnormal map, UseMap will be set to 0 or 1, no samplerSharing is used for the actual sampler as the sampler for the normal map is used:
            TextureProperty.SetupTextureMaterialProperty(material, k_BentNormal,           enableMap: bentNormalMapPresent, samplerSharing: null);
            TextureProperty.SetupTextureMaterialProperty(material, k_Metallic,             enableMap: (!specularColorEnabled), samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_SpecularColor,        enableMap: specularColorEnabled, allowUnassignedSampling: true, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_SmoothnessA,          enableMap: true, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_SmoothnessB,          enableMap: (dualSpecularLobeEnabled && !hazyGlossEnabled), samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_LobeMix,              enableMap: (dualSpecularLobeEnabled && !hazyGlossEnabled), samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_Haziness,             enableMap: (dualSpecularLobeEnabled && hazyGlossEnabled), samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_HazeExtent,           enableMap: (dualSpecularLobeEnabled && hazyGlossEnabled), samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_AmbientOcclusion,     enableMap: true, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_SubsurfaceMask,       enableMap: sssEnabled, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_Thickness,            enableMap: (sssEnabled || transmissionEnabled), samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_AnisotropyA,          enableMap: anisotropyEnabled, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_AnisotropyB,          enableMap: anisotropyEnabled, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_Tangent,              enableMap: anisotropyEnabled, samplerSharing: samplerSharing); // allowUnassignedSampling is false default, anisotropyEnabled is enough
            TextureProperty.SetupTextureMaterialProperty(material, k_IridescenceThickness, enableMap: iridescenceEnabled, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_IridescenceMask,      enableMap: iridescenceEnabled, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_CoatSmoothness,       enableMap: coatEnabled, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_CoatNormal,           enableMap: coatEnabled, allowUnassignedSampling: allowUnassignedNormalSampling, samplerSharing: samplerSharing);

            // details
            TextureProperty.SetupTextureMaterialProperty(material, k_DetailMask, enableMap: detailsEnabled, allowUnassignedSampling: true, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_DetailSmoothness, enableMap: detailsEnabled, allowUnassignedSampling: true, samplerSharing: samplerSharing);
            TextureProperty.SetupTextureMaterialProperty(material, k_DetailNormal, enableMap: detailsEnabled, allowUnassignedSampling: allowUnassignedNormalSampling, samplerSharing: samplerSharing);

            TextureProperty.SetupTextureMaterialProperty(material, k_EmissiveColor, enableMap: true, allowUnassignedSampling: true, samplerSharing: samplerSharing);

            if (samplerSharingEnabled)
            {
                int sharedSamplersUsedNum = samplerSharing.DoClientAssignment();
                // TODO : set limiting keyword for the different cases of the shader switch?
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
