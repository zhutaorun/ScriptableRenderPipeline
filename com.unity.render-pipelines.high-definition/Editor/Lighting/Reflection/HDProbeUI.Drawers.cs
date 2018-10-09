using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDProbeUI, SerializedHDProbe>;

    partial class HDProbeUI
    {
        public static readonly CED.IDrawer[] Inspector;
        
        static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
            CED.Action(Drawer_ReflectionProbeMode),
            CED.FadeGroup((s, p, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                FadeOption.Indent,
                CED.space,                                              // Baked
                CED.noop,                                               // Realtime
                CED.Action(Drawer_ModeSettingsCustom)                   // Custom
                )
            );

        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);
        
        public static readonly CED.IDrawer SectionToolbar = CED.Group(
            CED.Action(Drawer_Toolbars),
            CED.space
            );

        public static readonly CED.IDrawer SectionProxyVolumeSettings = CED.FoldoutGroup(
                proxySettingsHeader,
                (s, d, o) => s.isSectionExpendedProxyVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionProxySettings)
                );
        
        public static readonly CED.IDrawer SectionInfluenceVolume = CED.Select(
            (s, d, o) => s.influenceVolume,
            (s, d, o) => d.probeSettings.influence,
            InfluenceVolumeUI.SectionFoldoutShape
            );

        public static readonly CED.IDrawer SectionShapeCheck = CED.Action(Drawer_DifferentShapeError);

        public static readonly CED.IDrawer SectionFrameSettings = CED.FadeGroup(
            (s, d, o, i) => s.isFrameSettingsOverriden,
            FadeOption.None,
            CED.Select(
                (s, d, o) => s.frameSettings,
                (s, d, o) => d.probeSettings.cameraSettings.frameSettings,
                FrameSettingsUI.Inspector(withOverride: true, withXR: false))
            );

        public static readonly CED.IDrawer SectionFoldoutAdditionalSettings = CED.FoldoutGroup(
                additionnalSettingsHeader,
                (s, d, o) => s.isSectionExpendedAdditionalSettings,
                FoldoutOption.Indent,
            CED.Action(Drawer_SectionCustomSettings),
            CED.space
                );

        static HDProbeUI()
        {
            Inspector = new[]
            {
                SectionToolbar,
                SectionPrimarySettings,
                SectionProxyVolumeSettings,
                SectionInfluenceVolume,
                SectionShapeCheck,
                SectionFoldoutAdditionalSettings,
                SectionFrameSettings,
                SectionBakeButton
            };
        }

        protected static void Drawer_DifferentShapeError(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var proxy = d.proxyVolume.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null
                && (int)proxy.proxyVolume.shape != d.probeSettings.influence.shape.enumValueIndex
                && proxy.proxyVolume.shape != ProxyShape.Infinite)
            {
                EditorGUILayout.HelpBox(
                    proxyInfluenceShapeMismatchHelpBoxText,
                    MessageType.Error,
                    true
                    );
            }
        }
        
        static GUIStyle disabled;
        static void PropertyField(SerializedProperty prop, GUIContent content)
        {
            if(prop != null)
            {
                EditorGUILayout.PropertyField(prop, content);
            }
            else
            {
                if(disabled == null)
                {
                    disabled = new GUIStyle(GUI.skin.label);
                    disabled.onNormal.textColor *= 0.5f;
                }
                EditorGUILayout.LabelField(content, disabled);
            }
        }

        protected static void Drawer_SectionProxySettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.proxyVolume, proxyVolumeContent);
            
            if (d.target.proxyVolume == null)
            {
                EditorGUI.BeginChangeCheck();
                d.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue = !EditorGUILayout.Toggle(useInfiniteProjectionContent, !d.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue);
                if(EditorGUI.EndChangeCheck())
                {
                    d.Apply();
                }
            }

            if (d.proxyVolume.objectReferenceValue != null)
            {
                var proxy = (ReflectionProxyVolumeComponent)d.proxyVolume.objectReferenceValue;
                if ((int)proxy.proxyVolume.shape != d.probeSettings.influence.shape.enumValueIndex
                    && proxy.proxyVolume.shape != ProxyShape.Infinite)
                    EditorGUILayout.HelpBox(
                        proxyInfluenceShapeMismatchHelpBoxText,
                        MessageType.Error,
                        true
                        );
            }
            else
            {
                EditorGUILayout.HelpBox(
                        d.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue ? noProxyInfiniteHelpBoxText : noProxyHelpBoxText,
                        MessageType.Info,
                        true
                        );
            }
        }

        protected static void Drawer_SectionCustomSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            using (new EditorGUI.DisabledScope(!hdPipeline.asset.renderPipelineSettings.supportLightLayers))
            {
                d.probeSettings.lightingLightLayer.intValue = Convert.ToInt32(EditorGUILayout.EnumFlagsField(lightLayersContent, (LightLayerEnum)d.probeSettings.lightingLightLayer.intValue));
            }

            EditorGUILayout.PropertyField(d.probeSettings.lightingWeight, weightContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.probeSettings.lightingMultiplier, multiplierContent);
            if (EditorGUI.EndChangeCheck())
                d.probeSettings.lightingMultiplier.floatValue = Mathf.Max(0.0f, d.probeSettings.lightingMultiplier.floatValue);
        }

        static readonly GUIContent[] k_ModeContents = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_ModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        protected static void Drawer_ReflectionProbeMode(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.probeSettings.mode.hasMultipleDifferentValues;
            EditorGUILayout.IntPopup(p.probeSettings.mode, k_ModeContents, k_ModeValues, CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting)."));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
        {
                s.SetModeTarget(p.probeSettings.mode.intValue);
                p.Apply();
        }
        }

        protected static void Drawer_ModeSettingsCustom(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            EditorGUI.showMixedValue = p.customTexture.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var customTexture = EditorGUILayout.ObjectField(_.GetContent("Cubemap"), p.customTexture.objectReferenceValue, typeof(Cubemap), false);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                p.customTexture.objectReferenceValue = customTexture;
        }

        #region Bake Button
        static readonly string[] k_BakeCustomOptionText = { "Bake as new Cubemap..." };
        static readonly string[] k_BakeButtonsText = { "Bake All Reflection Probes" };
        protected static void Drawer_SectionBakeButton(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var so = d.serializedObject;
            if (so.isEditingMultipleObjects)
            {

            }
            else
            {
                var settings = d.target.settings;
                switch (settings.mode)
                {
                    case ProbeSettings.Mode.Custom:
                        {
                            if (UnityEditor.Lightmapping.giWorkflowMode
                                != UnityEditor.Lightmapping.GIWorkflowMode.OnDemand)
                            {
                                EditorGUILayout.HelpBox("Baking of this probe is automatic because this probe's " +
                                    "type is 'Baked' and the Lighting window is using 'Auto Baking'. " +
                                    "The texture created is stored in the GI cache.", MessageType.Info);
                                break;
                            }
                            if (ButtonWithDropdownList(
                                _.GetContent(
                                    "Bake|Bakes Probe's texture, overwriting the existing texture asset " +
                                    "(if any)."
                                ),
                                k_BakeCustomOptionText,
                                data =>
                                {
                                    var mode = (int)data;
                                    switch ((int)data)
                                    {
                                        case 0:
                                            throw new NotImplementedException();
                                            // TODO: Create a new custom texture asset
                                            HDProbeSystem.RenderAndUpdateRenderData(
                                                d.target, null, ProbeSettings.Mode.Custom
                                            );
                                            break;
                                    }
                                }))
                            {
                                HDProbeSystem.RenderAndUpdateRenderData(
                                    d.target, null, ProbeSettings.Mode.Custom
                                );
                            }
                            break;
                        }
                    case ProbeSettings.Mode.Baked:
                        {
                            GUI.enabled = d.target.enabled;

                            // Bake button in non-continous mode
                            if (ButtonWithDropdownList(
                                    _.GetContent("Bake"),
                                    k_BakeButtonsText,
                                    data =>
                                    {
                                        var mode = (int)data;
                                        if (mode == 0)
                                        {
                                            var system = ScriptableBakedReflectionSystemSettings.system;
                                            system.BakeAllReflectionProbes();
                                        }
                                    },
                                    GUILayout.ExpandWidth(true)))
                            {
                                HDBakedReflectionSystem.BakeProbes(new HDProbe[] { d.target });
                                GUIUtility.ExitGUI();
                            }

                            GUI.enabled = true;
                            break;
                        }
                    case ProbeSettings.Mode.Realtime:
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
        static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
        }
        #endregion
    }
}
