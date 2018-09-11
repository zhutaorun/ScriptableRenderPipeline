using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public abstract class BaseMaterialGUI : BaseUnlitGUI
    {
        #region String Constants
        public const string k_Show = "Show";
        public const string k_UIBuffer = "UIBuffer";
        // Maps:
        public const string k_RangeScale = "RangeScale";
        public const string k_UV = "UV";
        public const string k_UVLocal = "UVLocal";
        public const string k_ObjSpace = "ObjSpace";

        public const string k_Map = "Map";
        public const string k_UseMap = "UseMap";
        // Map Suffixes:
        // Prefix those with k_Map if using base property name,
        // or not if the property name already ends with "Map"
        // (ie a texture property name)
        public const string k_SamplerSharingOptout = "SamplerSharingOptout";
        public const string k_SamplerSharingAllowNullOptout = "SamplerSharingNullOptout";
        public const string k_Remap = "Remap";
        public const string k_RemapInverted = "RemapInverted";
        public const string k_Range = "Range";
        public const string k_UIRangeLimits = "UIRangeLimits";
        public const string k_Channel = "Channel";
        public const string k_ChannelMask = "ChannelMask";
        #endregion

        #region GUI Property Classes
        public abstract class BaseProperty
        {
            public BaseMaterialGUI Parent = null;

            protected Func<object, bool> IsVisible;

            protected BaseProperty(BaseMaterialGUI parent, Func<object, bool> isVisible = null)
            {
                Parent = parent;
                IsVisible = isVisible;
            }

            public abstract void OnFindProperty(MaterialProperty[] props);

            public abstract void OnGUI(Material material);

            internal abstract string ToShaderPropertiesStringInternal();

            public static bool IsNullOrEmpty(Array array)
            {
                return array == null || array.Length == 0;
            }
        }

        public class GroupProperty : BaseProperty
        {
            public string m_Title = string.Empty;

            private readonly BaseProperty[] m_ChildProperties;
            private readonly Property m_Show;
            private readonly Action<Material> m_ExtraOnGUI;

            public GroupProperty(BaseMaterialGUI parent, string groupName, BaseProperty[] childProperties, Func<object, bool> isVisible = null)
                : this(parent, groupName, string.Empty, childProperties, null, isVisible)
            {
            }

            public GroupProperty(BaseMaterialGUI parent, string groupName, string groupTitle, BaseProperty[] childProperties, Action<Material> extraOnGUI = null, Func<object, bool> isVisible = null)
                : base(parent, isVisible)
            {
                m_Show = new Property(parent, groupName + k_Show, "", false);

                m_Title = groupTitle;
                m_ChildProperties = childProperties;
                m_ExtraOnGUI = extraOnGUI;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                m_Show.OnFindProperty(props);

                foreach (var c in m_ChildProperties)
                {
                    c.OnFindProperty(props);
                }
            }

            public override void OnGUI(Material material)
            {
                if (IsVisible == null || IsVisible(this))
                {
                    if (!string.IsNullOrEmpty(m_Title))
                    {
                        m_Show.BoolValue = EditorGUILayout.Foldout(m_Show.BoolValue, m_Title);
                    }
                    else if (m_Show.IsValid)
                    {
                        m_Show.BoolValue = true;
                    }

                    if (!m_Show.IsValid || m_Show.BoolValue)
                    {
                        EditorGUI.indentLevel++;

                        foreach (var c in m_ChildProperties)
                        {
                            c.OnGUI(material);
                        }

                        EditorGUI.indentLevel--;

                        if (m_ExtraOnGUI != null)
                        {
                            m_ExtraOnGUI(material);
                        }
                    }
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                string outputString = string.Empty;

                foreach (var c in m_ChildProperties)
                {
                    outputString += c.ToShaderPropertiesStringInternal() + "\n";
                }

                return outputString;
            }
        }

        public class Property : BaseProperty
        {
            public string PropertyName;
            public string PropertyText;

            public MaterialProperty m_MaterialProperty = null;

            protected readonly GUIContent m_GuiContent = null;

            public bool IsMandatory = false;

            public bool IsValid
            {
                get { return m_MaterialProperty != null; }
            }

            public float FloatValue
            {
                get { return m_MaterialProperty.floatValue; }
                set { m_MaterialProperty.floatValue = value; }
            }

            public bool BoolValue
            {
                get { return m_MaterialProperty == null || Math.Abs(m_MaterialProperty.floatValue) > 0.0f; }
                set { m_MaterialProperty.floatValue = value ? 1.0f : 0.0f; }
            }

            public Texture TextureValue
            {
                get { return m_MaterialProperty != null ? m_MaterialProperty.textureValue : null; }
                set { if (m_MaterialProperty != null) { m_MaterialProperty.textureValue = value; } }
            }

            public Vector4 VectorValue
            {
                get { return m_MaterialProperty.vectorValue; }
                set { m_MaterialProperty.vectorValue = value; }
            }

            public Property(BaseMaterialGUI parent, string propertyName, string guiText, bool isMandatory = true, Func<object, bool> isVisible = null)
                : this(parent, propertyName, guiText, string.Empty, isMandatory, isVisible)
            {
            }

            public Property(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, isVisible)
            {
                m_GuiContent = new GUIContent(guiText, toolTip);
                PropertyName = propertyName;
                PropertyText = guiText;
                IsMandatory = isMandatory;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                m_MaterialProperty = ShaderGUI.FindProperty(PropertyName, props, IsMandatory);
            }

            public override void OnGUI(Material material)
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    Parent.m_MaterialEditor.ShaderProperty(m_MaterialProperty, m_GuiContent);
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    switch (m_MaterialProperty.type)
                    {
                        case MaterialProperty.PropType.Color:
                            return string.Format("{0}(\"{1}\", Color) = (1, 1, 1, 1)", PropertyName, PropertyText);

                        case MaterialProperty.PropType.Vector:
                            return string.Format("{0}(\"{1}\", Vector) = (0, 0, 0, 0)", PropertyName, PropertyText);

                        case MaterialProperty.PropType.Float:
                            return string.Format("{0}(\"{1}\", Float) = 0.0", PropertyName, PropertyText);

                        case MaterialProperty.PropType.Range:
                            return string.Format("{0}(\"{1}\", Range({2:0.0###}, {3:0.0###})) = 0", PropertyName, PropertyText, m_MaterialProperty.rangeLimits.x, m_MaterialProperty.rangeLimits.y);

                        case MaterialProperty.PropType.Texture:
                            return string.Format("{0}(\"{1}\", 2D) = \"white\" {{}}", PropertyName, PropertyText);

                        default:
                            // Unknown type... default to outputting a float.
                            return string.Format("{0}(\"{1}\", Float) = 0.0", PropertyName, PropertyText);
                    }
                }
                else
                {
                    // Material property is not loaded, default to outputting a float.
                    return string.Format("{0}(\"{1}\", Float) = 0.0", PropertyName, PropertyText);
                }
            }
        }

        public class ComboProperty : Property
        {
            private readonly string[] m_Options;
            private readonly int[] m_Values = null;

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string[] options, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, isMandatory, isVisible)
            {
                m_Options = options;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, string[] options, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                m_Options = options;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string[] options, int[] values, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, isMandatory, isVisible)
            {
                m_Options = options;
                m_Values = values;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, string[] options, int[] values, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                m_Options = options;
                m_Values = values;
            }

            public override void OnGUI(Material material)
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    EditorGUI.showMixedValue = m_MaterialProperty.hasMixedValue;
                    float floatValue = m_MaterialProperty.floatValue;

                    EditorGUI.BeginChangeCheck();

                    if (m_Values == null)
                    {
                        floatValue = EditorGUILayout.Popup(m_GuiContent, (int)floatValue, m_Options);
                    }
                    else
                    {
                        floatValue = EditorGUILayout.IntPopup(m_GuiContent.text, (int)floatValue, m_Options, m_Values);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Parent.m_MaterialEditor.RegisterPropertyChangeUndo(PropertyName);
                        m_MaterialProperty.floatValue = (float)floatValue;
                    }

                    EditorGUI.showMixedValue = false;
                }
            }
        }

        public class UIBufferedProperty : Property
        {
            public string RealPropertyName;
            public MaterialProperty m_RealMaterialProperty = null;

            public new bool IsValid
            {
                get { return (m_MaterialProperty != null) && (m_RealMaterialProperty != null); }
            }

            public UIBufferedProperty(BaseMaterialGUI parent, string propertyName, string guiText, bool isMandatory = true, Func<object, bool> isVisible = null)
                : this(parent, propertyName, guiText, string.Empty, isMandatory, isVisible)
            {
            }

            public UIBufferedProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName + k_UIBuffer, guiText, toolTip, isMandatory, isVisible)
            {
                RealPropertyName = propertyName;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                base.OnFindProperty(props);
                m_RealMaterialProperty = ShaderGUI.FindProperty(RealPropertyName, props, IsMandatory);
            }

            public override void OnGUI(Material material)
            {
                if (IsValid)
                {
                    base.OnGUI(material);
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    switch (m_MaterialProperty.type)
                    {
                        case MaterialProperty.PropType.Color:
                            return base.ToShaderPropertiesStringInternal() + "\n" + string.Format("[HideInInspector] {0}(\"{1}\", Color) = (1, 1, 1, 1)", RealPropertyName, PropertyText);

                        case MaterialProperty.PropType.Vector:
                            return base.ToShaderPropertiesStringInternal() + "\n" + string.Format("[HideInInspector] {0}(\"{1}\", Vector) = (0, 0, 0, 0)", RealPropertyName, PropertyText);

                        case MaterialProperty.PropType.Float:
                            return base.ToShaderPropertiesStringInternal() + "\n" + string.Format("[HideInInspector] {0}(\"{1}\", Float) = 0.0", RealPropertyName, PropertyText);

                        case MaterialProperty.PropType.Range:
                            return base.ToShaderPropertiesStringInternal() + "\n" + string.Format("[HideInInspector] {0}(\"{1}\", Range({2:0.0###}, {3:0.0###})) = 0", RealPropertyName, PropertyText, m_MaterialProperty.rangeLimits.x, m_MaterialProperty.rangeLimits.y);

                        case MaterialProperty.PropType.Texture:
                            return base.ToShaderPropertiesStringInternal() + "\n" + string.Format("[HideInInspector] {0}(\"{1}\", 2D) = \"white\" {{}}", RealPropertyName, PropertyText);

                        default:
                            // Unknown type... default to outputting a float.
                            return base.ToShaderPropertiesStringInternal() + "\n" + string.Format("[HideInInspector] {0}(\"{1}\", Float) = 0.0", RealPropertyName, PropertyText);
                    }
                }
                else
                {
                    // Material property is not loaded, default to outputting a float.
                    return base.ToShaderPropertiesStringInternal() + "\n" + string.Format("{0}(\"{1}\", Float) = 0.0", PropertyName, PropertyText);
                }
            }

            public static void SetupUIBufferedMaterialProperty(Material material, string basePropertyName, MaterialProperty.PropType propType)
            {
                string uibufferPropertyName = basePropertyName + k_UIBuffer;

                if (material.HasProperty(basePropertyName) && material.HasProperty(uibufferPropertyName))
                {
                    switch(propType)
                    {
                        case MaterialProperty.PropType.Color:
                            material.SetColor(basePropertyName, material.GetColor(uibufferPropertyName));
                            break;

                        case MaterialProperty.PropType.Vector:
                            material.SetVector(basePropertyName, material.GetVector(uibufferPropertyName));
                            break;

                        case MaterialProperty.PropType.Float:
                            material.SetFloat(basePropertyName, material.GetFloat(uibufferPropertyName));
                            break;

                        case MaterialProperty.PropType.Texture:
                            material.SetTexture(basePropertyName, material.GetTexture(uibufferPropertyName));
                            break;

                        default:
                            // Unknown / not implemented
                            break;
                    }
                }
            }
        }

        public class DiffusionProfileProperty : Property
        {
            public DiffusionProfileProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
            }

            public override void OnGUI(Material material)
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                    if (hdPipeline == null)
                    {
                        return;
                    }

                    var diffusionProfileSettings = hdPipeline.diffusionProfileSettings;
                    if (hdPipeline.IsInternalDiffusionProfile(diffusionProfileSettings))
                    {
                        EditorGUILayout.HelpBox(
                            "No diffusion profile Settings have been assigned to the render pipeline asset.",
                            MessageType.Warning);
                        return;
                    }

                    // TODO: Optimize me
                    var profiles = diffusionProfileSettings.profiles;
                    var names = new GUIContent[profiles.Length + 1];
                    names[0] = new GUIContent("None");

                    var values = new int[names.Length];
                    values[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

                    for (int i = 0; i < profiles.Length; i++)
                    {
                        names[i + 1] = new GUIContent(profiles[i].name);
                        values[i + 1] = i + 1;
                    }

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        int profileID = (int)FloatValue;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel(m_GuiContent.text);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                profileID = EditorGUILayout.IntPopup(profileID, names, values);

                                if (GUILayout.Button("Goto", EditorStyles.miniButton, GUILayout.Width(50f)))
                                {
                                    Selection.activeObject = diffusionProfileSettings;
                                }
                            }
                        }

                        if (scope.changed)
                        {
                            FloatValue = profileID;
                        }
                    }
                }
            }
        }

        public class PropertyWithActionButton : Property
        {
            private string[] m_ButtonCaptions;
            private Action<Property, Material>[] m_OnClickActions;
            private Action<Property, Material> m_DisplayUnder;

            public PropertyWithActionButton(BaseMaterialGUI parent, string propertyName,
                string[] buttonCaptions, Action<Property, Material>[] onClickActions,
                string guiText, string toolTip,
                Action<Property, Material> displayUnder = null,
                bool isMandatory = true,
                Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                m_ButtonCaptions = buttonCaptions;
                m_OnClickActions = onClickActions;
                m_DisplayUnder = displayUnder;
            }

            public override void OnGUI(Material material)
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    Parent.m_MaterialEditor.ShaderProperty(m_MaterialProperty, m_GuiContent);
                    if (!IsNullOrEmpty(m_ButtonCaptions) && m_OnClickActions != null)
                    {
                        for (int i = 0; i < Math.Min(m_ButtonCaptions.Length, m_OnClickActions.Length); i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.PrefixLabel(""); // alignment fix
                                if (GUILayout.Button(m_ButtonCaptions[i], EditorStyles.miniButton, GUILayout.Width(250f)))
                                {
                                    m_OnClickActions[i](this, material);
                                }
                            }
                        }
                    }
                    if (m_DisplayUnder != null)
                    {
                        m_DisplayUnder(this, material);
                    }
                } // valid
            }
        }

        public class TextureOneLineProperty : Property
        {
            public string ExtraPropertyName;

            private MaterialProperty m_ExtraProperty;

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string guiText, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, isMandatory, isVisible)
            {
            }

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
            }

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string extraPropertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                ExtraPropertyName = extraPropertyName;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                base.OnFindProperty(props);

                if (!string.IsNullOrEmpty(ExtraPropertyName))
                {
                    m_ExtraProperty = ShaderGUI.FindProperty(ExtraPropertyName, props, IsMandatory);
                }
            }

            public override void OnGUI(Material material)
            {
                if (IsValid && (IsVisible == null || IsVisible(this)))
                {
                    Parent.m_MaterialEditor.TexturePropertySingleLine(m_GuiContent, m_MaterialProperty, m_ExtraProperty);
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                return string.Format("{0}(\"{1}\", 2D) = \"white\" {{}}\n" +
                    "{2}(\"{3}\", Float) = 0.0",
                    PropertyName, PropertyText,
                    ExtraPropertyName, ExtraPropertyName);
            }
        }

        public class TextureProperty : Property
        {
            public enum Channel
            {
                R,
                G,
                B,
                A,
            }

            public enum PlanarSpace
            {
                World,
                Local
            }

            public enum NormalSpace
            {
                Tangent,
                Object
            }

            public enum UVMapping
            {
                UV0,
                UV1,
                UV2,
                UV3,
                PlanarXY,
                PlanarYZ,
                PlanarZX,
                Triplanar,
            }

            public Property m_Show;

            public Property m_ConstantProperty;

            public Property m_RangeScaleProperty;

            public TextureOneLineProperty m_TextureProperty;

            public ComboProperty m_UvSetProperty;

            public ComboProperty m_LocalOrWorldProperty;

            public ComboProperty m_NormalSpaceProperty;

            public Property m_ChannelProperty;

            public Property m_RemapProperty;

            public Property m_UIRangeLimitsProperty;

            public Property m_InvertRemapProperty;

            public Property m_SamplerSharingOptoutProperty;

            public Property m_SamplerSharingAllowNullOptoutProperty;

            public string m_ConstantPropertyName;

            public bool m_IsNormalMap;

            public bool m_ShowScaleOffset;

            public TextureOneLineProperty m_SlaveTexOneLineProp;

            Func<object, Material, bool> m_IsSamplerSharingEnabled;

            public Vector2? m_UIRangeLimits;

            public static Vector2 m_UIRangeLimitsDefault = new Vector2(0.0f, 1.0f);

            public TextureProperty(BaseMaterialGUI parent, string propertyName, string constantPropertyName, string guiText,
                bool pairConstantWithTexture, bool isMandatory = true, bool isNormalMap = false, bool showScaleOffset = true,
                Func<object, bool> isVisible = null, Func<object, Material, bool> samplerSharingEnabled = null)
                : this(parent, propertyName, constantPropertyName, guiText, string.Empty, pairConstantWithTexture, isMandatory, isNormalMap, showScaleOffset, 
                      slaveTexOneLineProp:null, UIRangeLimits: null, isVisible: isVisible, samplerSharingEnabled: samplerSharingEnabled)
            {
            }

            // UIRangeLimits overrides m_UIRangeLimitsProperty even if the later is found in the shader
            public TextureProperty(BaseMaterialGUI parent, string propertyName, string constantPropertyName, string guiText, string toolTip,
                bool pairConstantWithTexture, bool isMandatory = true, bool isNormalMap = false, bool showScaleOffset = true,
                TextureOneLineProperty slaveTexOneLineProp = null, Vector2? UIRangeLimits = null, Func < object, bool> isVisible = null,
                Func<object, Material, bool> samplerSharingEnabled = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                m_IsSamplerSharingEnabled = samplerSharingEnabled;
                m_UIRangeLimits = UIRangeLimits;
                m_IsNormalMap = isNormalMap;
                m_ShowScaleOffset = showScaleOffset;
                m_SlaveTexOneLineProp = slaveTexOneLineProp;

                m_ConstantPropertyName = constantPropertyName;

                m_Show = new Property(parent, propertyName + k_Show, "", isMandatory);

                if (pairConstantWithTexture == false)
                {
                    m_ConstantProperty = new Property(parent, constantPropertyName, guiText, toolTip, isMandatory);
                }

                m_RangeScaleProperty = new Property(parent, propertyName + k_RangeScale, "Range Multiplier", false);

                m_TextureProperty = new TextureOneLineProperty(parent, propertyName, pairConstantWithTexture ? constantPropertyName : string.Empty, guiText, toolTip, isMandatory);

                m_UvSetProperty = new ComboProperty(parent, propertyName + k_UV, "UV Mapping", Enum.GetNames(typeof(UVMapping)), false);
                m_LocalOrWorldProperty = new ComboProperty(parent, propertyName + k_UVLocal, "Local or world", Enum.GetNames(typeof(PlanarSpace)), false);

                m_NormalSpaceProperty = new ComboProperty(parent, propertyName + k_ObjSpace, "Normal space", Enum.GetNames(typeof(NormalSpace)), false);

                m_ChannelProperty = new ComboProperty(parent, propertyName + k_Channel, "Channel", Enum.GetNames(typeof(Channel)), false);

                m_RemapProperty = new Property(parent, propertyName + k_Remap, "Remapping", "Defines the range to remap/scale the values in texture", false);
                m_InvertRemapProperty = new Property(parent, propertyName + k_RemapInverted, "Invert Remapping", "Whether the mapping values are inverted.", false);

                m_UIRangeLimitsProperty = new Property(parent, propertyName + k_UIRangeLimits, "UI range limits", "Defines the range that the UI widget will allow", false);

                m_SamplerSharingOptoutProperty = new Property(parent, propertyName + k_SamplerSharingOptout, "Exclude From Sampler Sharing", "Opt-out of Sampler Sharing", false);
                m_SamplerSharingAllowNullOptoutProperty = new Property(parent, propertyName + k_SamplerSharingAllowNullOptout, "Allow Opt-out When Unassigned (When Generating Shader)",
                    "Allow Opt-out When Unassigned (When Generating Shader)", false);
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                base.OnFindProperty(props);

                m_Show.OnFindProperty(props);
                if (m_ConstantProperty != null)
                {
                    m_ConstantProperty.OnFindProperty(props);
                }
                m_RangeScaleProperty.OnFindProperty(props);
                m_TextureProperty.OnFindProperty(props);
                m_UvSetProperty.OnFindProperty(props);
                m_LocalOrWorldProperty.OnFindProperty(props);
                m_NormalSpaceProperty.OnFindProperty(props);
                m_ChannelProperty.OnFindProperty(props);
                m_RemapProperty.OnFindProperty(props);
                m_InvertRemapProperty.OnFindProperty(props);
                m_UIRangeLimitsProperty.OnFindProperty(props);
                m_SamplerSharingOptoutProperty.OnFindProperty(props);
                m_SamplerSharingAllowNullOptoutProperty.OnFindProperty(props);
            }

            public override void OnGUI(Material material)
            {
                if ((IsVisible == null || IsVisible(this))
                    && m_Show.IsValid
                    && m_TextureProperty.IsValid)
                {
                    m_Show.BoolValue = EditorGUILayout.Foldout(m_Show.BoolValue, PropertyText);

                    if (m_Show.BoolValue)
                    {
                        EditorGUI.indentLevel++;

                        if (m_RangeScaleProperty.IsValid)
                        {
                            m_RangeScaleProperty.OnGUI(material);
                        }

                        if (m_ConstantProperty != null && m_ConstantProperty.IsValid
                            && m_TextureProperty.TextureValue == null)
                        {
                            m_ConstantProperty.OnGUI(material);
                        }

                        m_TextureProperty.OnGUI(material);

                        bool mainTextureOrSlaveTextureAssigned = m_TextureProperty.TextureValue != null || ((m_SlaveTexOneLineProp != null) && (m_SlaveTexOneLineProp.TextureValue != null));
                        if (mainTextureOrSlaveTextureAssigned)
                        {
                            m_UvSetProperty.OnGUI(material);
                            m_ChannelProperty.OnGUI(material);

                            if (m_ShowScaleOffset)
                            {
                                Parent.m_MaterialEditor.TextureScaleOffsetProperty(m_TextureProperty.m_MaterialProperty);
                            }

                            if (m_UvSetProperty.IsValid && m_UvSetProperty.FloatValue >= (float)UVMapping.PlanarXY)
                            {
                                m_LocalOrWorldProperty.OnGUI(material);
                            }

                            if (m_IsNormalMap)
                            {
                                m_NormalSpaceProperty.OnGUI(material);
                            }

                            if (m_RemapProperty.IsValid)
                            {
                                // Display the remap of texture values.
                                Vector2 remap = m_RemapProperty.VectorValue;
                                Vector2 uiLimits = (m_UIRangeLimits != null) ? (Vector2) m_UIRangeLimits : ((m_UIRangeLimitsProperty.IsValid) ? (Vector2) m_UIRangeLimitsProperty.VectorValue : m_UIRangeLimitsDefault);

                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.MinMaxSlider(m_RemapProperty.PropertyText, ref remap.x, ref remap.y, uiLimits.x, uiLimits.y);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_RemapProperty.VectorValue = remap;
                                }

                                if (m_InvertRemapProperty.IsValid)
                                {
                                    m_InvertRemapProperty.OnGUI(material);
                                }
                            }
                        }
                        if (m_SamplerSharingOptoutProperty.IsValid 
                            && (m_IsSamplerSharingEnabled == null || m_IsSamplerSharingEnabled(this, material))
                            && (mainTextureOrSlaveTextureAssigned || (m_SamplerSharingAllowNullOptoutProperty.IsValid && m_SamplerSharingAllowNullOptoutProperty.BoolValue) ) )
                        {
                            m_SamplerSharingOptoutProperty.OnGUI(material);
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                string constantName = m_ConstantPropertyName.StartsWith("_")
                    ? m_ConstantPropertyName.Substring(1)
                    : m_ConstantPropertyName;

                return string.Format(
                    "[HideInInspector] {0}MapShow(\"{1} Show\", Float) = 0\n" +
                    "{0}(\"{1}\", Range(0.0, 1.0)) = 0\n" +
                    "{2}(\"{1} Map\", 2D) = " + (m_IsNormalMap ? "\"bump\"" : "\"white\"") + " {{ }}\n" +
                    "{0}UseMap(\"{1} Use Map\", Float) = 0\n" +
                    (m_IsNormalMap ? "{0}ObjSpace(\"{1} Object Space\", Float) = 0\n" : "") +
                    "{2}UV(\"{1} Map UV\", Float) = 0.0\n" +
                    "{2}UVLocal(\"{1} Map UV Local\", Float) = 0.0\n" +
                    "{2}Channel(\"{1} Map Channel\", Float) = 0.0\n" +
                    "{2}ChannelMask(\"{1} Map Channel Mask\", Vector) = (1, 0, 0, 0)\n" +
                    "{0}Remap(\"{1} Remap\", Vector) = (0, 1, 0, 0)\n" +
                    "[ToggleUI] {0}RemapInverted(\"Invert {1} Remap\", Float) = 0.0\n" +
                    "[HideInInspector] {0}Range(\"{1} Range\", Vector) = (0, 1, 0, 0)\n",
                    m_ConstantPropertyName, constantName, PropertyName);
            }

            public static void SetupUseMapOfTextureMaterialProperty(Material material,
                TextureSamplerSharingShaderGenerator samplerShaderGenerator, TextureSamplerSharing.SamplerClient samplerClient,
                int slotAssigned, bool isExternalSampler, bool isClientUnique)
            {
                // If we opt-out of sharing, we need to ask the sampler sharing system to allocate a sampler just for this property:
                string useMapPropertyName = samplerClient.BasePropertyName + k_UseMap;

                // It really should have it if we added it to the potential sharer (clients) of TextureSamplerSharing
                // (see SetupTextureMaterialProperty)
                if (material.HasProperty(useMapPropertyName))
                {
                    // UseMap = 0.0f means no slots assigned.
                    //
                    // UseMap = 1.0f just means a texture was assigned (eg could mean the texture's own sampler, but since
                    // it is compilation that determines potential sampler usage, not very useful.)
                    //
                    // We use it eg for _BentNormalMap to indicate with 1.0f that it is enabled, but the bent normal map never has
                    // its own sampler, and doesn't have one assigned by the sharing system since it always uses the one of the normal
                    // map.
                    // We also use numbers starting at 1 (but not overlapping SharedSamplerID.First) for built-in samplers that we know
                    // are used anyways by internal LUTs (like FGD), lightloop (since we're a forward shader) and shadow sampling.
                    //
                    // By default, an enabled map that has the UseMap property will have the later set to 1.0f by 
                    // SetupTextureMaterialProperty().
                    //
                    // For whatever reason we don't get a callback here to adjust "UseMap" (eg we ran out of sampler slots),
                    // we will use the sampler that have the id == 1.0f (see TextureSamplerSharing.cs and TextureSamplerSharing.hlsl)
                    float useMapPropertyValue = (isExternalSampler == false) ? (int)(SharedSamplerID.First) + slotAssigned : 1.0f + slotAssigned;
                    if(samplerShaderGenerator != null)
                    {
                        bool mapUsesOwnSampler = isExternalSampler && isClientUnique;
                        // The following override of useMapPropertyValue is not really necessary, usemap just need to be > 0
                        // but the shader generator will override anyway:
                        samplerShaderGenerator.AddUseMapPropertyConfig(useMapPropertyName, useMapPropertyValue, isExternalSampler, mapUsesOwnSampler);
                    }
                    material.SetFloat(useMapPropertyName, useMapPropertyValue);
                }
            }

            //
            // allowUnassignedSampling allows to set UseMap to 1 even though no texture is assigned.
            // Useful in case we want to sample the default symbolic value (ends up being a dummy texture assigned by the engine).
            //
            // enableMap allows to set UseMap to 0 even when a texture is assigned.
            // Useful in case there's no feature keyword to exclude sampling and we want to avoid it.
            //
            // TextureSamplerSharing object is mandatory if shader keyword _USE_SAMPLER_SHARING is set, otherwise, it is useless
            // as it will set UseMap properties to various shared sampler slots (numbers starting at 2.0) but the
            // shader code will only test if(UseMap) so it should be ok. The shader code samples with the texture's
            // own sampler when that keyword isn't set.
            public static void SetupTextureMaterialProperty(Material material, string basePropertyName, bool enableMap = true, bool allowUnassignedSampling = false,
                TextureSamplerSharing samplerSharing = null, TextureSamplerSharingShaderGenerator shaderGenerator = null)
            {

                // TODO: Caution this can generate a lot of garbage collection call ?
                string useMapPropertyName = basePropertyName + k_UseMap;
                string mapPropertyName = basePropertyName + k_Map;
                string mapSamplerSharingOptoutPropertyName = mapPropertyName + k_SamplerSharingOptout;
                string mapSamplerSharingAllowNullOptoutPropertyName = mapPropertyName + k_SamplerSharingAllowNullOptout;
                string remapPropertyName = mapPropertyName + k_Remap;
                string invertPropertyName = mapPropertyName + k_RemapInverted;
                string rangePropertyName = mapPropertyName + k_Range;
                string channelPropertyName = mapPropertyName + k_Channel;
                string channelMaskPropertyName = mapPropertyName + k_ChannelMask;

                Texture texture = material.GetTexture(mapPropertyName);
                if (enableMap && (texture || allowUnassignedSampling))
                {
                    if (material.HasProperty(useMapPropertyName))
                    {
                        material.SetFloat(useMapPropertyName, 1.0f);

                        // If useMap property is there, and we gave a sampler sharing object, add it to the potential texture maps that will share a sampler
                        // unless we escape the feature through the *MapSamplerSharingOptout == 1.0f property:
                        //
                        // In that case, we handle the opt-out differently if we have a texture assigned or not and if we're generating a shader or not:
                        // If we have a texture assigned (+ opt-out) and:
                        //     A) If we're not going to be generating a shader, we just allocate a shared sampler for use uniquely for this texture (makeUnique param to AddClient),
                        //     B) otherwise, when generating, we have the option to just use the texture's own sampler and in that case, the user can screw itself
                        //      over by using too many samplers than are available before our UI can warn him (the engine shader compilation pipeline will spew a
                        //      sampler limit exceeded error).
                        if (samplerSharing != null)
                        {
                            // If we opt-out of sharing, we need to ask the sampler sharing system to allocate a sampler just for this property:
                            bool makeUnique = material.HasProperty(mapSamplerSharingOptoutPropertyName) && (material.GetFloat(mapSamplerSharingOptoutPropertyName) == 1.0f);
                            bool mapWillUseOwnSamplerAndGeneratedCode = (shaderGenerator != null && makeUnique);

                            if (texture != null)
                            {
                                if (mapWillUseOwnSamplerAndGeneratedCode == false)
                                {
                                    // if makeUnique, this will be Case A) described above:
                                    samplerSharing.AddClient(basePropertyName, texture, makeUnique, tryExternalExistingSamplers: true);
                                }
                                else
                                {
                                    // Case B) We still want the client assignment callback in this case so we can streamline shader generation configuration for this map
                                    // in the same function "SetupUseMapOfTextureMaterialProperty":
                                    samplerSharing.AddClientForOwnUniqueSampler(basePropertyName);
                                }
                            }
                            else
                            {
                                // Finally, if there's no texture assigned, we can't use samplerSharing.AddClient() (see point 2) but there could still be texture sampling
                                // in two cases (either way at this point the sampler state required is unknown):
                                //
                                // 1) If we allowUnassignedSampling and we don't expect anything to be assigned (programmatically or by the engine)
                                //    past the call to this function which configures the material from the UI.
                                //    In that case, we sample a single default value, and any sampler will do (short of bordercolor sampling mode, but Unity doesn't expose that)
                                //    and we pick one we know is commonly used.
                                //
                                // 2) If we allowUnassignedSampling and condition 1) is not met.
                                //    Then there's no way for us to allow sharing, even if we let the user specify a state manually, as if this texture property
                                //    is the only one using that sampler state in the end, there will be no way for us to make the engine transfer this custom sampler state
                                //    (which normally is done by assigning one of the texture clients with the proper state on the shared map slot.)
                                //    (An easy solution in that specific case is just to make the user specify that state by assigning a dummy 1 pixel texture instead.)
                                //
                                //    If we opt-out of sharing, we have the exact same problem as above with the same solution.
                                //    If we're generating though, we have the option to use the texture's own sampler (which is what happens when no sampler sharing is used
                                //    globally anyway)
                                //
                                //    => Since this increases the number of samplers used by the shader (and this is the reason for the sampler sharing system in the first place),
                                //    we can't systematically use that option for all unassigned textures.
                                //    Instead, the shader needs to specify it (that condition 1 is not met ie to use its own sampler when unassigned).
                                //
                                bool allowNullOptout = material.HasProperty(mapSamplerSharingAllowNullOptoutPropertyName) && (material.GetFloat(mapSamplerSharingAllowNullOptoutPropertyName) == 1.0f);
                                if (makeUnique && allowNullOptout == false)
                                {
                                    // Sanitizing the .mat (we've setup the UI to not show the opt-out when the map property is unassigned and we're not allowing unassigned opt-out,
                                    // because we ignore it):
                                    // Clear the optout option if we don't allow it for null textures.
                                    material.SetFloat(mapSamplerSharingOptoutPropertyName, 0f);
                                }

                                // On a null texture map slot, if we don't have shader generation (which is needed to honor null-slot opt-out), opt-out doesn't change a thing, so
                                // whether we allow it on a null texture map slot is irrelevant, we still add the texture map as a client for an existing built-in (but external
                                // to the sharing system) sampler, as this will not change sampler allocation, and is the wanted default behavior of case 1) described above
                                // (sample an engine allocated dummy texture like UnityWhite).
                                if(mapWillUseOwnSamplerAndGeneratedCode == false)
                                {
                                    samplerSharing.AddClientForExternalExistingSampler(basePropertyName, ExternalExistingSampler.LinearRepeat);
                                }
                                else if (allowNullOptout == true)
                                {
                                    // ie true == (texture == null) == mapWillUseOwnSamplerAndGeneratedCode == allowNullOptout
                                    // Case 2) above and we have shader generation.
                                    // If we want to allow unassigned texture map sampler sharing opt-out (and we do have it), we need generation, and in that case,
                                    // the UseMap value won't matter (as long as it's > 0), but we need to tell the generator to setup the map to use its own sampler. We register
                                    // ourself for callback via AddClientForOwnUniqueSampler to then call the generator.
                                    // (ie we streamline shader generation configuration for this map in the same function "SetupUseMapOfTextureMaterialProperty")
                                    samplerSharing.AddClientForOwnUniqueSampler(basePropertyName);
                                }

                            }//else, texture is null
                        }//samplerSharing
                    }//usemap

                    if (texture)
                    {
                        if (material.HasProperty(remapPropertyName) && material.HasProperty(rangePropertyName))
                        {
                            Vector4 rangeVector = material.GetVector(remapPropertyName);
                            if (material.HasProperty(invertPropertyName) && material.GetFloat(invertPropertyName) > 0.0f)
                            {
                                float s = rangeVector.x;
                                rangeVector.x = rangeVector.y;
                                rangeVector.y = s;
                            }

                            material.SetVector(rangePropertyName, rangeVector);
                        }

                        if (material.HasProperty(channelPropertyName))
                        {
                            int channel = (int)material.GetFloat(channelPropertyName);
                            switch (channel)
                            {
                                case 0:
                                    material.SetVector(channelMaskPropertyName, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                                    break;
                                case 1:
                                    material.SetVector(channelMaskPropertyName, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                                    break;
                                case 2:
                                    material.SetVector(channelMaskPropertyName, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
                                    break;
                                case 3:
                                    material.SetVector(channelMaskPropertyName, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (material.HasProperty(rangePropertyName))
                        {
                            material.SetVector(rangePropertyName, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                        }
                        if (material.HasProperty(channelPropertyName))
                        {
                            material.SetVector(channelMaskPropertyName, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                        }
                    }
                }
                else
                {
                    // No texture sampling wanted:

                    if (material.HasProperty(useMapPropertyName))
                    {
                        material.SetFloat(useMapPropertyName, 0.0f);
                    }
                    if (material.HasProperty(rangePropertyName))
                    {
                        material.SetVector(rangePropertyName, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                    }
                    if (material.HasProperty(channelPropertyName))
                    {
                        material.SetVector(channelMaskPropertyName, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                    }
                }
            }

        }
        #endregion
    }
}
