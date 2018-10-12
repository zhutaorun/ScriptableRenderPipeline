using System;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Schema;
using UnityEngine;

namespace UnityEditor
{
    public abstract class BaseShaderGUI : ShaderGUI
    {
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum BlendMode
        {
            Alpha,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
            Additive,
            Multiply
        }
        
        public enum SmoothnessSource
        {
            BaseAlpha,
            SpecularAlpha
        }

        public enum Culling
        {
            BackFace = 2,
            FrontFace = 1,
            Off = 0
        }

        protected class Styles
        {
            // Catergories
            public static readonly GUIContent SurfaceOptions = new GUIContent("Surface Options", "Tooltip");
            public static readonly GUIContent SurfaceInputs = new GUIContent("Surface Inputs", "Tooltip");
            public static readonly GUIContent AdvancedLabel = new GUIContent("Advanced", "Tooltip");
            
            public static readonly GUIContent surfaceType = new GUIContent("Surface Type", "Tooltip");
            public static readonly GUIContent blendingMode = new GUIContent("Blending Mode", "Tooltip");
            public static readonly GUIContent twoSidedText = new GUIContent("Double-sided", "Enable to render both front and back faces, disable to cull the back faces");
            public static readonly GUIContent cullingText = new GUIContent("Face Culling", "Choose the culling option for this material.");
            public static readonly GUIContent alphaClipText = new GUIContent("Alpha Clipping", "Enables Alpha Clipping on this material, this uses the alpha value of the Base Map and Base Color, use the threshold to adjust the bias.");
            public static readonly GUIContent alphaClipThresholdText = new GUIContent("Threshold", "Acts as an offset for the alpha clip");
            public static readonly GUIContent receiveShadowText = new GUIContent("Receive Shadows", "Enables this material to receive shadows if there is at least one shadow casting light affecting it.");
            
            public static readonly GUIContent baseMap = new GUIContent("Base Map", "This Property is used for adding base coloring to the material.\n" +
                                                                      "If there is no RGB texture map assigned the color property is used, otherwise it is multiplied over the texture map.");
            public static readonly GUIContent baseColor = new GUIContent("Base Color", "This color property adds base coloring to the material, if a Base Map is assigned this color will be multiplied over it.");
            public static readonly GUIContent emissionMap = new GUIContent("Emission Map", "This Property is used for adding emissive light to the material.\n" +
                                                                              "If there is no RGB texture map assigned the color property controls the emission, otherwise it is multiplied over the texture map.");
            public static readonly GUIContent emissionColor = new GUIContent("Emission Color", "This color property adds emissive to the material, if a Base Map is assigned this color will be multiplied over it.");
        }

        protected MaterialEditor materialEditor { get; set; }
        protected MaterialProperty surfaceTypeProp { get; set; }
        protected MaterialProperty blendModeProp { get; set; }
        protected MaterialProperty cullingProp { get; set; }
        protected MaterialProperty alphaClipProp { get; set; }
        protected MaterialProperty alphaCutoffProp { get; set; }
        protected MaterialProperty receiveShadowsProp { get; set; }
        // Common Surface Input properties
        protected MaterialProperty baseMapProp { get; set; }
        protected MaterialProperty baseColorProp { get; set; }
        protected MaterialProperty emissionMapProp { get; set; }
        protected MaterialProperty emissionColorProp { get; set; }
        private bool m_FirstTimeApply = true;

        private const string k_KeyPrefix = "LightweightRP:Material:UI_State:";
        private string m_HeaderStateKey;
        protected uint defaultHeaderState = 0;

        public abstract void MaterialChanged(Material material);

        public virtual void FindProperties(MaterialProperty[] properties)
        {
            surfaceTypeProp = FindProperty("_Surface", properties);
            blendModeProp = FindProperty("_Blend", properties);
            cullingProp = FindProperty("_Cull", properties);
            alphaClipProp = FindProperty("_AlphaClip", properties);
            alphaCutoffProp = FindProperty("_Cutoff", properties);
            receiveShadowsProp = FindProperty("_ReceiveShadows", properties, false);
            baseMapProp = FindProperty("_BaseMap", properties, false);
            baseColorProp = FindProperty("_BaseColor", properties, false);
            emissionMapProp = FindProperty("_EmissionMap", properties, false);
            emissionColorProp = FindProperty("_EmissionColor", properties, false);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            var surfaceState = EditorGUILayout.BeginFoldoutHeaderGroup(GetHeaderState(0), Styles.SurfaceOptions);
            if(surfaceState){
                DrawSurfaceOptions(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            StoreHeaderState(surfaceState, 0);
            
            var inputState = EditorGUILayout.BeginFoldoutHeaderGroup(GetHeaderState(1), Styles.SurfaceInputs);
            if (inputState)
            {
                DrawSurfaceInputs(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            StoreHeaderState(inputState, 1);

            var advanced = EditorGUILayout.BeginFoldoutHeaderGroup(GetHeaderState(2), Styles.AdvancedLabel);
            if (advanced)
            {
                DrawAdvancedOptions(material);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            StoreHeaderState(advanced, 2);

            MaterialChanged(material);
        }

        public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            if (materialEditorIn == null)
                throw new ArgumentNullException("materialEditorIn");

            FindProperties(properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            materialEditor = materialEditorIn;
            Material material = materialEditor.target as Material;
            m_HeaderStateKey = k_KeyPrefix + material.shader.name; // Create key string for editor prefs

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a lightweight shader.
            if (m_FirstTimeApply)
            {
                MaterialChanged(material);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        public virtual void DrawSurfaceOptions(Material material)
        {
            DoPopup(Styles.surfaceType.text, surfaceTypeProp, Enum.GetNames(typeof(SurfaceType)));
            if ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Transparent)
                DoPopup(Styles.blendingMode.text, blendModeProp, Enum.GetNames(typeof(BlendMode)));

            EditorGUI.BeginChangeCheck();
            var culling = (Culling)cullingProp.floatValue;
            culling = (Culling)EditorGUILayout.EnumPopup(Styles.cullingText, culling);
            if (EditorGUI.EndChangeCheck())
            {
                cullingProp.floatValue = (float)culling;
                material.doubleSidedGI = culling != Culling.BackFace;
            }

            EditorGUI.BeginChangeCheck();
            var alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipText, alphaClipProp.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                alphaClipProp.floatValue = alphaClipEnabled ? 1 : 0;

            if (alphaClipProp.floatValue == 1)
                materialEditor.ShaderProperty(alphaCutoffProp, Styles.alphaClipThresholdText, 1);

            if (receiveShadowsProp != null)
            {
                EditorGUI.BeginChangeCheck();
                var receiveShadows =
                    EditorGUILayout.Toggle(Styles.receiveShadowText, receiveShadowsProp.floatValue == 1.0f);
                if (EditorGUI.EndChangeCheck())
                    receiveShadowsProp.floatValue = receiveShadows ? 1.0f : 0.0f;
            }
        }

        public virtual void DrawSurfaceInputs(Material material)
        {
            DrawBaseProperties();
        }

        public virtual void DrawAdvancedOptions(Material material)
        {
            materialEditor.EnableInstancingField();
        }

        public virtual void DrawBaseProperties()
        {
            if (baseMapProp != null && baseColorProp != null) // Draw the baseMap, most shader will have at least a baseMap
            {
                materialEditor.TexturePropertySingleLine(Styles.baseMap, baseMapProp, baseColorProp);
            }
        }
        
        public virtual void DrawEmissionProperties(Material material, bool keyword)
        {
            var emissive = true;
            var hadEmissionTexture = emissionMapProp.textureValue != null;
            
            if (emissionMapProp != null && emissionColorProp != null) // Draw the baseMap, most shader will have at least a baseMap
            {
                if (!keyword)
                {
                    materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProp, emissionColorProp,
                        false);
                }
                else
                {
                    // Emission for GI?
                    emissive = materialEditor.EmissionEnabledProperty();

                    EditorGUI.BeginDisabledGroup(!emissive);
                    {
                        // Texture and HDR color controls
                        materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProp,
                            emissionColorProp,
                            false);
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            // If texture was assigned and color was black set color to white
            var brightness = emissionColorProp.colorValue.maxColorComponent;
            if (emissionMapProp.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                emissionColorProp.colorValue = Color.white;
            // LW does not support RealtimeEmissive. We set it to bake emissive and handle the emissive is black right.
            if (emissive)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (brightness <= 0f)
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        public void DrawBaseTileOffset()
        {
            materialEditor.TextureScaleOffsetProperty(baseMapProp);
        }

        public static void SetupMaterialBlendMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool alphaClip = material.GetFloat("_AlphaClip") == 1;
            if (alphaClip)
                material.EnableKeyword("_ALPHATEST_ON");
            else
                material.DisableKeyword("_ALPHATEST_ON");

            SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
            if (surfaceType == SurfaceType.Opaque)
            {
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                material.SetShaderPassEnabled("ShadowCaster", true);
            }
            else
            {
                BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");
                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        material.SetShaderPassEnabled("ShadowCaster", false);
                        break;
                    case BlendMode.Premultiply:
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        material.SetShaderPassEnabled("ShadowCaster", false);
                        break;
                    case BlendMode.Additive:
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        material.SetShaderPassEnabled("ShadowCaster", false);
                        break;
                    case BlendMode.Multiply:
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        material.SetShaderPassEnabled("ShadowCaster", false);
                        break;
                }
            }
        }

        protected void DoPopup(string label, MaterialProperty property, string[] options)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            EditorGUI.showMixedValue = property.hasMixedValue;

            var mode = property.floatValue;
            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.Popup(label, (int)mode, options);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(label);
                property.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected void StoreHeaderState(bool headerState, int index)
        {
            if(EditorPrefs.HasKey(m_HeaderStateKey))
            {
                var bitMask = EditorPrefs.GetInt(m_HeaderStateKey);
                if (headerState)
                {
                    bitMask |= 1 << (index + 1);
                }
                else
                {
                    bitMask &= ~(1 << (index + 1));
                }
                EditorPrefs.SetInt(m_HeaderStateKey, bitMask);
            }
        }
        
        protected bool GetHeaderState(int index)
        {
            var bitMask = (int) defaultHeaderState;
            if(!EditorPrefs.HasKey(m_HeaderStateKey))
            {
                EditorPrefs.SetInt(m_HeaderStateKey, bitMask);
            }
            else
            {
                bitMask = EditorPrefs.GetInt(m_HeaderStateKey);
            }

            var headerState = (bitMask & (1 << index+1)) != 0;
            
            return headerState;
        }
    }
}
