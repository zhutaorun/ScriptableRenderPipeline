using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(LightweightRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class LightweightRenderPipelineCameraEditor : CameraEditor
    {
        internal class Styles
        {
            public readonly GUIContent renderingPathLabel = EditorGUIUtility.TrTextContent("Rendering Path", "Lightweight Render Pipeline only supports Forward rendering path.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Shadow-Muppets in da House.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "Depth-Muppets in da House.");
            public static GUIContent requireColorTexture = EditorGUIUtility.TrTextContent("Color Texture", "Color-Muppets in da House.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture) null);

            public readonly GUIContent[] renderingPathOptions = { EditorGUIUtility.TrTextContent("Forward") };
            public readonly string hdrDisabledWarning = "HDR rendering is disabled in the Lightweight Render Pipeline asset.";
            public readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Lightweight Render Pipeline asset.";
            public static GUIContent[] displayedDefaultOptions =
            {
                new GUIContent("Off"),
                new GUIContent("On")
            };
            public static int[] optionDefaultValues = { 0, 1 };

            // This is for adding more data like Pipeline Asset option
            public static GUIContent[] displayedDataOptions =
            {
                new GUIContent("Off"),
                new GUIContent("On"),
            };
            public static int[] optionDataValues = { 0, 1 };

            // Using the pipeline Settings
            public static GUIContent[] displayedOptions =
            {
                new GUIContent("Off"),
                new GUIContent("Use Pipeline Settings")
            };
            public static int[] optionValues = { 0, 1 };
        };

        public Camera camera { get { return target as Camera; } }

        // Animation Properties
        public bool isSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        public bool isSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

        static readonly int[] s_RenderingPathValues = {0};
        static Styles s_Styles;
        LightweightRenderPipelineAsset m_LightweightRenderPipeline;
        AdditionalCameraData m_AdditionalCameraData;
        SerializedObject m_AddtionalCameraDataSO;
        SerializedObject m_LightweightRenderPipelineSO;

        readonly AnimBool m_ShowBGColorAnim = new AnimBool();
        readonly AnimBool m_ShowOrthoAnim = new AnimBool();
        readonly AnimBool m_ShowTargetEyeAnim = new AnimBool();

        SerializedProperty m_AdditionalCameraDataRenderShadowsProp;
        SerializedProperty m_AdditionalCameraDataRenderDepthProp;
        SerializedProperty m_AdditionalCameraDataRenderColorProp;

        void SetAnimationTarget(AnimBool anim, bool initialize, bool targetValue)
        {
            if (initialize)
            {
                anim.value = targetValue;
                anim.valueChanged.AddListener(Repaint);
            }
            else
            {
                anim.target = targetValue;
            }
        }

        void UpdateAnimationValues(bool initialize)
        {
            SetAnimationTarget(m_ShowBGColorAnim, initialize, isSameClearFlags && (camera.clearFlags == CameraClearFlags.SolidColor || camera.clearFlags == CameraClearFlags.Skybox));
            SetAnimationTarget(m_ShowOrthoAnim, initialize, isSameOrthographic && camera.orthographic);
            SetAnimationTarget(m_ShowTargetEyeAnim, initialize, settings.targetEye.intValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported);
        }

        public new void OnEnable()
        {
            m_LightweightRenderPipeline = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            m_LightweightRenderPipelineSO = new SerializedObject(m_LightweightRenderPipeline);

            m_AdditionalCameraData = camera.gameObject.GetComponent<AdditionalCameraData>();
            init(m_AdditionalCameraData);


            settings.OnEnable();
            UpdateAnimationValues(true);
        }

        void init(AdditionalCameraData additionalCameraData)
        {
            if(additionalCameraData == null)
                return;
            m_AddtionalCameraDataSO = new SerializedObject(additionalCameraData);
            m_AdditionalCameraDataRenderShadowsProp = m_AddtionalCameraDataSO.FindProperty("m_RenderShadows");
            m_AdditionalCameraDataRenderDepthProp = m_AddtionalCameraDataSO.FindProperty("m_RequiresDepthTexture");
            m_AdditionalCameraDataRenderColorProp = m_AddtionalCameraDataSO.FindProperty("m_RequiresColorTexture");
        }

        public void OnDisable()
        {
            m_ShowBGColorAnim.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoAnim.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            m_LightweightRenderPipeline = null;
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();
            UpdateAnimationValues(false);

            settings.DrawClearFlags();

            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowBGColorAnim.faded))
                if (group.visible) settings.DrawBackgroundColor();

            settings.DrawCullingMask();

            EditorGUILayout.Space();

            settings.DrawProjection();
            settings.DrawClippingPlanes();
            settings.DrawNormalizedViewPort();

            EditorGUILayout.Space();
            settings.DrawDepth();
            DrawRenderingPath();
            DrawTargetTexture();
            settings.DrawOcclusionCulling();
            DrawHDR();
            DrawMSAA();
            DrawAdditionalData();
            settings.DrawVR();
            settings.DrawMultiDisplay();

            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowTargetEyeAnim.faded))
                if (group.visible) settings.DrawTargetEye();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            settings.ApplyModifiedProperties();
        }

        void DrawRenderingPath()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntPopup(s_Styles.renderingPathLabel, 0, s_Styles.renderingPathOptions, s_RenderingPathValues);
            }
        }

        public void DrawHDR()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.allowHDR, settings.HDR);
            int selectedValue = !settings.HDR.boolValue ? 0 : 1;
            settings.HDR.boolValue = EditorGUI.IntPopup(controlRect, Styles.allowHDR, selectedValue, Styles.displayedOptions, Styles.optionValues) == 1;
            EditorGUI.EndProperty();
        }

        public void DrawMSAA()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.allowMSAA, settings.allowMSAA);
            int selectedValue = !settings.allowMSAA.boolValue ? 0 : 1;
            settings.allowMSAA.boolValue = EditorGUI.IntPopup(controlRect, Styles.allowMSAA, selectedValue, Styles.displayedOptions, Styles.optionValues) == 1;
            EditorGUI.EndProperty();
        }

        void DrawTargetTexture()
        {
            EditorGUILayout.PropertyField(settings.targetTexture);

            if (!settings.targetTexture.hasMultipleDifferentValues)
            {
                var texture = settings.targetTexture.objectReferenceValue as RenderTexture;
                int pipelineSamplesCount = m_LightweightRenderPipeline.msaaSampleCount;

                if (texture && texture.antiAliasing > pipelineSamplesCount)
                {
                    string pipelineMSAACaps = (pipelineSamplesCount > 1)
                        ? String.Format("is set to support {0}x", pipelineSamplesCount)
                        : "has MSAA disabled";
                    EditorGUILayout.HelpBox(String.Format("Camera target texture requires {0}x MSAA. Lightweight pipeline {1}.", texture.antiAliasing, pipelineMSAACaps),
                        MessageType.Warning, true);
                }
            }
        }

        void DrawAdditionalData()
        {
            bool hasChanged = false;
            int selectedValueShadows;
            int selectedValueDepth;
            int selectedValueColor;

            if (m_AddtionalCameraDataSO == null)
            {
                selectedValueShadows = 1;
                selectedValueDepth = 1;
                selectedValueColor = 1;
            }
            else
            {
                m_AddtionalCameraDataSO.Update();
                selectedValueShadows = !m_AdditionalCameraData.renderShadows ? 0 : 1;
                selectedValueDepth = !m_AdditionalCameraData.requiresDepthTexture ? 0 : 1;
                selectedValueColor = !m_AdditionalCameraData.requiresColorTexture ? 0 : 1;
            }

            Rect controlRectShadows = EditorGUILayout.GetControlRect(true);
            if(m_AddtionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectShadows, Styles.renderingShadows, m_AdditionalCameraDataRenderShadowsProp);
            EditorGUI.BeginChangeCheck();

            selectedValueShadows = EditorGUI.IntPopup(controlRectShadows, Styles.renderingShadows, selectedValueShadows, Styles.displayedDefaultOptions, Styles.optionDefaultValues);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            if(m_AddtionalCameraDataSO != null)
                EditorGUI.EndProperty();

            // Depth Texture
            Rect controlRectDepth = EditorGUILayout.GetControlRect(true);
            if(m_AddtionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectDepth, Styles.renderingShadows, m_AdditionalCameraDataRenderDepthProp);
            EditorGUI.BeginChangeCheck();

            selectedValueDepth = EditorGUI.IntPopup(controlRectDepth, Styles.requireDepthTexture, selectedValueDepth, Styles.displayedDataOptions, Styles.optionDataValues);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            if(m_AddtionalCameraDataSO != null)
                EditorGUI.EndProperty();

            // Color Texture
            Rect controlRectColor = EditorGUILayout.GetControlRect(true);
            // Starting to check the property if we have the scriptable object
            if(m_AddtionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectColor, Styles.renderingShadows, m_AdditionalCameraDataRenderColorProp);
            EditorGUI.BeginChangeCheck();
            selectedValueColor = EditorGUI.IntPopup(controlRectColor, Styles.requireColorTexture, selectedValueColor, Styles.displayedDataOptions, Styles.optionDataValues);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            // Ending to check the property if we have the scriptable object
            if(m_AddtionalCameraDataSO != null)
                EditorGUI.EndProperty();

            if (hasChanged)
            {
                if (m_AddtionalCameraDataSO == null)
                {
                    camera.gameObject.AddComponent<AdditionalCameraData>();
                    m_AdditionalCameraData = camera.gameObject.GetComponent<AdditionalCameraData>();
                    init(m_AdditionalCameraData);
                }
                m_AdditionalCameraDataRenderShadowsProp.intValue = selectedValueShadows;
                m_AdditionalCameraDataRenderDepthProp.intValue = selectedValueDepth;
                m_AdditionalCameraDataRenderColorProp.intValue = selectedValueColor;
                m_AddtionalCameraDataSO.ApplyModifiedProperties();
            }
        }
    }
}
