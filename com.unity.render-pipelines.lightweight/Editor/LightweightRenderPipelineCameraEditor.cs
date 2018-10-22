using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
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
            public static GUIContent[] displayedRenderShadowsOptions =
            {
                new GUIContent("Off"),
                new GUIContent("On"),
            };
            public static int[] renderShadowsOptions = { 0, 1 };

            // This is for adding more data like Pipeline Asset option
            public static GUIContent[] displayedAdditionalDataOptions =
            {
                new GUIContent("Off"),
                new GUIContent("On"),
                new GUIContent("Use Pipeline Settings"),
            };

            public static int[] additionalDataOptions = Enum.GetValues(typeof(CameraOverrideOption)).Cast<int>().ToArray();

            // Using the pipeline Settings
            public static GUIContent[] displayedCameraOptions =
            {
                new GUIContent("Off"),
                new GUIContent("Use Pipeline Settings"),
            };
            public static int[] cameraOptions = { 0, 1 };
        };

        public Camera camera { get { return target as Camera; } }

        // Animation Properties
        public bool isSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        public bool isSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

        static readonly int[] s_RenderingPathValues = {0};
        static Styles s_Styles;
        LightweightRenderPipelineAsset m_LightweightRenderPipeline;
        LWRPAdditionalCameraData m_AdditionalCameraData;
        SerializedObject m_AdditionalCameraDataSO;

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

            m_AdditionalCameraData = camera.gameObject.GetComponent<LWRPAdditionalCameraData>();
            settings.OnEnable();
            init(m_AdditionalCameraData);
            
            UpdateAnimationValues(true);
        }

        void init(LWRPAdditionalCameraData additionalCameraData)
        {
            if(additionalCameraData == null)
                return;

            m_AdditionalCameraDataSO = new SerializedObject(additionalCameraData);
            m_AdditionalCameraDataRenderShadowsProp = m_AdditionalCameraDataSO.FindProperty("m_RenderShadows");
            m_AdditionalCameraDataRenderDepthProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresDepthTextureOption");
            m_AdditionalCameraDataRenderColorProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresOpaqueTextureOption");
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
            settings.HDR.boolValue = EditorGUI.IntPopup(controlRect, Styles.allowHDR, selectedValue, Styles.displayedCameraOptions, Styles.cameraOptions) == 1;
            EditorGUI.EndProperty();
        }

        public void DrawMSAA()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.allowMSAA, settings.allowMSAA);
            int selectedValue = !settings.allowMSAA.boolValue ? 0 : 1;
            settings.allowMSAA.boolValue = EditorGUI.IntPopup(controlRect, Styles.allowMSAA, selectedValue, Styles.displayedCameraOptions, Styles.cameraOptions) == 1;
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
            CameraOverrideOption selectedDepthOption;
            CameraOverrideOption selectedOpaqueOption;

            if (m_AdditionalCameraDataSO == null)
            {
                selectedValueShadows = 1;
                selectedDepthOption = CameraOverrideOption.UsePipelineSettings;
                selectedOpaqueOption = CameraOverrideOption.UsePipelineSettings;
            }
            else
            {
                m_AdditionalCameraDataSO.Update();
                selectedValueShadows = !m_AdditionalCameraData.renderShadows ? 0 : 1;
                selectedDepthOption = m_AdditionalCameraData.requiresDepthOption;
                selectedOpaqueOption = m_AdditionalCameraData.requiresColorOption;
            }

            Rect controlRectShadows = EditorGUILayout.GetControlRect(true);
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectShadows, Styles.renderingShadows, m_AdditionalCameraDataRenderShadowsProp);
            EditorGUI.BeginChangeCheck();

            selectedValueShadows = EditorGUI.IntPopup(controlRectShadows, Styles.renderingShadows, selectedValueShadows, Styles.displayedRenderShadowsOptions, Styles.renderShadowsOptions);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.EndProperty();

            // Depth Texture
            Rect controlRectDepth = EditorGUILayout.GetControlRect(true);
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectDepth, Styles.renderingShadows, m_AdditionalCameraDataRenderDepthProp);
            EditorGUI.BeginChangeCheck();

            selectedDepthOption = (CameraOverrideOption)EditorGUI.IntPopup(controlRectDepth, Styles.requireDepthTexture, (int)selectedDepthOption, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.EndProperty();

            // Color Texture
            Rect controlRectColor = EditorGUILayout.GetControlRect(true);
            // Starting to check the property if we have the scriptable object
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectColor, Styles.renderingShadows, m_AdditionalCameraDataRenderColorProp);
            EditorGUI.BeginChangeCheck();
            selectedOpaqueOption = (CameraOverrideOption)EditorGUI.IntPopup(controlRectColor, Styles.requireColorTexture, (int)selectedOpaqueOption, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            // Ending to check the property if we have the scriptable object
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.EndProperty();

            if (hasChanged)
            {
                if (m_AdditionalCameraDataSO == null)
                {
                    m_AdditionalCameraData = camera.gameObject.AddComponent<LWRPAdditionalCameraData>();
                    init(m_AdditionalCameraData);
                }
                m_AdditionalCameraDataRenderShadowsProp.intValue = selectedValueShadows;
                m_AdditionalCameraDataRenderDepthProp.intValue = (int)selectedDepthOption;
                m_AdditionalCameraDataRenderColorProp.intValue = (int)selectedOpaqueOption;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
        }
    }
}
