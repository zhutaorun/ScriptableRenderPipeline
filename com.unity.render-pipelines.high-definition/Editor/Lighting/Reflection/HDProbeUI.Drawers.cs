using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
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
        [Flags]
        internal enum ToolBar
        {
            InfluenceShape = 1 << 0,
            Blend = 1 << 1,
            NormalBlend = 1 << 2,
            CapturePosition = 1 << 3
        }

        protected interface IProbeUISettingsProvider
        {
            ProbeSettingsOverride primarySettingsField { get; }
            Type customTextureType { get; }
            ToolBar[] toolbars { get; }
        }

        const EditMode.SceneViewEditMode EditBaseShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        const EditMode.SceneViewEditMode EditInfluenceShape = EditMode.SceneViewEditMode.GridBox;
        const EditMode.SceneViewEditMode EditInfluenceNormalShape = EditMode.SceneViewEditMode.Collider;
        const EditMode.SceneViewEditMode EditCenter = EditMode.SceneViewEditMode.GridMove;
        //Note: EditMode.SceneViewEditMode.ReflectionProbeOrigin is still used
        //by legacy reflection probe and have its own mecanism that we don't want

        static readonly Dictionary<ToolBar, EditMode.SceneViewEditMode> k_ToolbarMode = new Dictionary<ToolBar, EditMode.SceneViewEditMode>
        {
            { ToolBar.InfluenceShape,  EditBaseShape },
            { ToolBar.Blend,  EditInfluenceShape },
            { ToolBar.NormalBlend,  EditInfluenceNormalShape },
            { ToolBar.CapturePosition,  EditCenter }
        };

        protected struct Drawer<TProvider>
            where TProvider : struct, IProbeUISettingsProvider
        {
            static readonly EditMode.SceneViewEditMode[][] k_ListModes;
            static readonly GUIContent[][] k_ListContent;
            static readonly GUIContent[] k_ModeContents = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
            static readonly int[] k_ModeValues = { (int)ProbeSettings.Mode.Baked, (int)ProbeSettings.Mode.Custom, (int)ProbeSettings.Mode.Realtime };

            static Drawer()
            {
                var provider = new TProvider();

                // Build toolbar content
                var toolbars = provider.toolbars;
                k_ListContent = new GUIContent[toolbars.Length][];
                k_ListModes = new EditMode.SceneViewEditMode[toolbars.Length][];

                var listMode = new List<EditMode.SceneViewEditMode>();
                var listContent = new List<GUIContent>();
                for (int i = 0; i < toolbars.Length; ++i)
                {
                    listMode.Clear();
                    listContent.Clear();

                    var toolBar = toolbars[i];
                    if ((toolBar & ToolBar.InfluenceShape) > 0)
                    {
                        listMode.Add(k_ToolbarMode[ToolBar.InfluenceShape]);
                        listContent.Add(k_ToolbarContents[ToolBar.InfluenceShape]);
                    }
                    if ((toolBar & ToolBar.Blend) > 0)
                    {
                        listMode.Add(k_ToolbarMode[ToolBar.Blend]);
                        listContent.Add(k_ToolbarContents[ToolBar.Blend]);
                    }
                    if ((toolBar & ToolBar.NormalBlend) > 0)
                    {
                        listMode.Add(k_ToolbarMode[ToolBar.NormalBlend]);
                        listContent.Add(k_ToolbarContents[ToolBar.NormalBlend]);
                    }
                    if ((toolBar & ToolBar.CapturePosition) > 0)
                    {
                        listMode.Add(k_ToolbarMode[ToolBar.CapturePosition]);
                        listContent.Add(k_ToolbarContents[ToolBar.CapturePosition]);
                    }
                    k_ListContent[i] = listContent.ToArray();
                    k_ListModes[i] = listMode.ToArray();
                }
                
            }

            public static void Drawer_Toolbars(HDProbeUI s, SerializedHDProbe d, Editor o)
            {
                var provider = new TProvider();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.changed = false;

                for (int i = 0; i < k_ListModes.Length; ++i)
                    EditMode.DoInspectorToolbar(k_ListModes[i], k_ListContent[i], GetBoundsGetter(o), o);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            public static void DrawPrimarySettings(HDProbeUI s, SerializedHDProbe p, Editor o)
            {
                const string modeGUIContent = "Type|'Baked' uses the 'Auto Baking' mode from the Lighting window. " +
                    "If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n" +
                    "'Custom' can be used if a custom capture is wanted. \n" +
                    "'Realtime' can be used to dynamically re-render the capture during runtime (every frame).";

                var provider = new TProvider();

                // Probe Mode
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = p.probeSettings.mode.hasMultipleDifferentValues;
                EditorGUILayout.IntPopup(p.probeSettings.mode, k_ModeContents, k_ModeValues, _.GetContent(modeGUIContent));
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    s.SetModeTarget(p.probeSettings.mode.intValue);

                // Baked: nothing
                // Realtime: nothing
                // Custom: specific settings
                if (p.probeSettings.mode.intValue == (int)ProbeSettings.Mode.Custom)
                {
                    EditorGUI.showMixedValue = p.customTexture.hasMultipleDifferentValues;
                    EditorGUI.BeginChangeCheck();
                    var customTexture = EditorGUILayout.ObjectField(
                        _.GetContent("Texture"), p.customTexture.objectReferenceValue, provider.customTextureType, false
                    );
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                        p.customTexture.objectReferenceValue = customTexture;
                }
            }

            public void DoShortcutKey(Editor owner)
            {
                var evt = Event.current;
                if (evt.type != EventType.KeyDown || !evt.shift)
                    return;

                ToolBar toolbar;
                if (toolbar_ShortCutKey.TryGetValue(evt.keyCode, out toolbar))
                {
                    bool used = false;
                    foreach (ToolBar t in toolBars)
                    {
                        if ((t & toolbar) > 0)
                        {
                            used = true;
                            break;
                        }
                    }
                    if (!used)
                    {
                        return;
                    }

                    var targetMode = toolbar_Mode[toolbar];
                    var mode = EditMode.editMode == targetMode ? EditMode.SceneViewEditMode.None : targetMode;
                    EditMode.ChangeEditMode(mode, GetBoundsGetter(owner)(), owner);
                    evt.Use();
                }
            }
        }

        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);

        public static readonly CED.IDrawer SectionProxyVolumeSettings = CED.FoldoutGroup(
                proxySettingsHeader,
                (s, d, o) => s.isSectionExpandedProxyVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionProxySettings)
                );
        
        public static readonly CED.IDrawer SectionInfluenceVolume = CED.Select(
            (s, d, o) => s.probeSettings.influence,
            (s, d, o) => d.probeSettings.influence,
            InfluenceVolumeUI.SectionFoldoutShape
        );

        public static readonly CED.IDrawer SectionShapeCheck = CED.Action(Drawer_DifferentShapeError);

        public static readonly CED.IDrawer SectionFrameSettings = CED.FadeGroup(
            (s, d, o, i) => s.isFrameSettingsOverriden,
            FadeOption.None,
            CED.Select(
                (s, d, o) => s.probeSettings.cameraFrameSettings,
                (s, d, o) => d.probeSettings.cameraSettings.frameSettings,
                FrameSettingsUI.Inspector(withOverride: true, withXR: false))
            );

        public static readonly CED.IDrawer SectionFoldoutAdditionalSettings = CED.FoldoutGroup(
                additionnalSettingsHeader,
                (s, d, o) => s.isSectionExpandedAdditionalSettings,
                FoldoutOption.Indent,
            CED.Action(Drawer_SectionCustomSettings),
            CED.space
                );

        static HDProbeUI()
        {
            //Inspector = new[]
            //{
            //    SectionToolbar,
            //    SectionPrimarySettings,
            //    SectionProxyVolumeSettings,
            //    SectionInfluenceVolume,
            //    SectionShapeCheck,
            //    SectionFoldoutAdditionalSettings,
            //    SectionFrameSettings,
            //    SectionBakeButton
            //};
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

        static internal void Drawer_ToolBarButton(ToolBar button, Editor owner, params GUILayoutOption[] options)
        {
            bool enabled = k_ToolbarMode[button] == EditMode.editMode;
            EditorGUI.BeginChangeCheck();
            enabled = GUILayout.Toggle(enabled, k_ToolbarContents[button], EditorStyles.miniButton, options);
            if (EditorGUI.EndChangeCheck())
            {
                EditMode.SceneViewEditMode targetMode = EditMode.editMode == k_ToolbarMode[button] ? EditMode.SceneViewEditMode.None : k_ToolbarMode[button];
                EditMode.ChangeEditMode(targetMode, GetBoundsGetter(owner)(), owner);
            }
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
