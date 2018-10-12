using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

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

        internal interface IProbeUISettingsProvider
        {
            ProbeSettingsOverride displayedCaptureSettings { get; }
            ProbeSettingsOverride displayedAdvancedSettings { get; }
            Type customTextureType { get; }
            ToolBar[] toolbars { get; }
        }

        // Constants
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

        //[TODO] change this to be modifiable shortcuts
        static Dictionary<KeyCode, ToolBar> k_ToolbarShortCutKey = new Dictionary<KeyCode, ToolBar>
        {
            { KeyCode.Alpha1, ToolBar.InfluenceShape },
            { KeyCode.Alpha2, ToolBar.Blend },
            { KeyCode.Alpha3, ToolBar.NormalBlend },
            { KeyCode.Alpha4, ToolBar.CapturePosition }
        };

        // Probe Setting Mode cache
        static readonly GUIContent[] k_ModeContents = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_ModeValues = { (int)ProbeSettings.Mode.Baked, (int)ProbeSettings.Mode.Custom, (int)ProbeSettings.Mode.Realtime };

        protected internal struct Drawer<TProvider>
            where TProvider : struct, IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
        {
            // Toolbar content cache
            static readonly EditMode.SceneViewEditMode[][] k_ListModes;
            static readonly GUIContent[][] k_ListContent;

            static Drawer()
            {
                var provider = new TProvider();

                // Build toolbar content cache
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

            // Tool bars
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

            public static void DoToolbarShortcutKey(Editor owner)
            {
                var provider = new TProvider();
                var toolbars = provider.toolbars;

                var evt = Event.current;
                if (evt.type != EventType.KeyDown || !evt.shift)
                    return;

                if (k_ToolbarShortCutKey.TryGetValue(evt.keyCode, out ToolBar toolbar))
                {
                    bool used = false;
                    foreach (ToolBar t in toolbars)
                    {
                        if ((t & toolbar) > 0)
                        {
                            used = true;
                            break;
                        }
                    }
                    if (!used)
                        return;

                    var targetMode = k_ToolbarMode[toolbar];
                    var mode = EditMode.editMode == targetMode ? EditMode.SceneViewEditMode.None : targetMode;
                    EditMode.ChangeEditMode(mode, GetBoundsGetter(owner)(), owner);
                    evt.Use();
                }
            }

            // Drawers
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

            public static void DrawCaptureSettings(HDProbeUI s, SerializedHDProbe p, Editor o)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(
                    s.probeSettings, p.probeSettings, o,
                    p.probeSettingsOverride, provider.displayedCaptureSettings
                );
            }

            public static void DrawCustomSettings(HDProbeUI s, SerializedHDProbe p, Editor o)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(
                    s.probeSettings, p.probeSettings, o,
                    p.probeSettingsOverride, provider.displayedAdvancedSettings
                );
            }

            public static void DrawInfluenceSettings(HDProbeUI s, SerializedHDProbe p, Editor o)
            {
                var provider = new TProvider();
                InfluenceVolumeUI.Draw<TProvider>(s.probeSettings.influence, p.probeSettings.influence, o);
            }

            public static void DrawProjectionSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
            {
                EditorGUILayout.PropertyField(d.proxyVolume, proxyVolumeContent);

                if (d.target.proxyVolume == null)
                {
                    EditorGUI.BeginChangeCheck();
                    d.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue = !EditorGUILayout.Toggle(useInfiniteProjectionContent, !d.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue);
                    if (EditorGUI.EndChangeCheck())
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
