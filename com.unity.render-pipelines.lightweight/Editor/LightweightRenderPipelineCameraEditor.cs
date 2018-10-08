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
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Rendering Shadows", "Muppets in da House.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Require Depth Texture", "Muppets in da House.");
            public static GUIContent requireColorTexture = EditorGUIUtility.TrTextContent("Require Color Texture", "Muppets in da House.");
            public readonly GUIContent[] renderingPathOptions = { EditorGUIUtility.TrTextContent("Forward") };
            public readonly string hdrDisabledWarning = "HDR rendering is disabled in the Lightweight Render Pipeline asset.";
            public readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Lightweight Render Pipeline asset.";
            public static GUIContent[] displayedOptions = new GUIContent[2]
            {
                new GUIContent("Off"),
                new GUIContent("Use Asset Pipeline Settings")
            };
            public static int[] optionValues = new int[2]{ 0, 1 };
        };



        public Camera camera { get { return target as Camera; } }

        // Animation Properties
        public bool isSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        public bool isSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

        static readonly int[] s_RenderingPathValues = {0};
        static Styles s_Styles;
        LightweightRenderPipelineAsset m_LightweightRenderPipeline;
        AdditionalCameraData m_AdditionalCameraData;

        readonly AnimBool m_ShowBGColorAnim = new AnimBool();
        readonly AnimBool m_ShowOrthoAnim = new AnimBool();
        readonly AnimBool m_ShowTargetEyeAnim = new AnimBool();

        SerializedProperty m_AdditionalCameraDataRenderShadowsProp;

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

            m_AdditionalCameraData = camera.GetComponent(typeof(AdditionalCameraData)) as AdditionalCameraData;
            //m_AdditionalCameraDataRenderShadowsProp = m_AdditionalCameraData.FindProperty("m_AdditionalLightsRenderingMode");

            settings.OnEnable();
            UpdateAnimationValues(true);
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
            settings.DrawVR();
            settings.DrawMultiDisplay();

            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowTargetEyeAnim.faded))
                if (group.visible) settings.DrawTargetEye();

            if(m_AdditionalCameraData != null)
                DrawOverrideData();

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

        void DrawHDR()
        {
            bool disabled = settings.HDR.boolValue && !m_LightweightRenderPipeline.supportsHDR;
            settings.DrawHDR();

            if (disabled)
                EditorGUILayout.HelpBox(s_Styles.hdrDisabledWarning, MessageType.Info);
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

        void DrawMSAA()
        {
            bool disabled = settings.allowMSAA.boolValue && m_LightweightRenderPipeline.msaaSampleCount <= 1;
            settings.DrawMSAA();

            if (disabled)
                EditorGUILayout.HelpBox(s_Styles.mssaDisabledWarning, MessageType.Info);
        }

        void DrawOverrideData()
        {
            Rect controlRectShadows = EditorGUILayout.GetControlRect(true);
            //EditorGUI.BeginProperty(controlRect, Styles.renderingShadows, m_AdditionalCameraData.renderShadows);
            int selectedValueShadows = !m_AdditionalCameraData.renderShadows ? 0 : 1;
            m_AdditionalCameraData.renderShadows = EditorGUI.IntPopup(controlRectShadows, Styles.renderingShadows, selectedValueShadows, Styles.displayedOptions, Styles.optionValues) == 1;
            //EditorGUI.EndProperty();


            Rect controlRectDepth = EditorGUILayout.GetControlRect(true);
            int selectedValueDepth = !m_AdditionalCameraData.requiresDepthTexture ? 0 : 1;
            m_AdditionalCameraData.requiresDepthTexture = EditorGUI.IntPopup(controlRectDepth, Styles.requireDepthTexture, selectedValueDepth, Styles.displayedOptions, Styles.optionValues) == 1;

            Rect controlRectColor = EditorGUILayout.GetControlRect(true);
            int selectedValueColor = !m_AdditionalCameraData.requiresColorTexture ? 0 : 1;
            m_AdditionalCameraData.requiresColorTexture = EditorGUI.IntPopup(controlRectColor, Styles.requireColorTexture, selectedValueColor, Styles.displayedOptions, Styles.optionValues) == 1;
        }
    }
}
