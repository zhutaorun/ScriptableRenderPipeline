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
        public static readonly CED.IDrawer SectionProbeModeSettings;
        public static readonly CED.IDrawer ProxyVolumeSettings = CED.FoldoutGroup(
                proxySettingsHeader,
                (s, d, o) => s.isSectionExpendedProxyVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionProxySettings)
                );
        public static readonly CED.IDrawer SectionProbeModeBakedSettings = CED.noop;
        public static readonly CED.IDrawer SectionProbeModeRealtimeSettings = CED.noop;
        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);

        public static readonly CED.IDrawer SectionFoldoutAdditionalSettings = CED.FoldoutGroup(
                additionnalSettingsHeader,
                (s, d, o) => s.isSectionExpendedAdditionalSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionCustomSettings)
                );

        static HDProbeUI()
        {
            SectionProbeModeSettings = CED.Group(
                    CED.Action(Drawer_FieldCaptureType),
                    CED.FadeGroup(
                        (s, d, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                        FadeOption.Indent,
                        SectionProbeModeBakedSettings,
                        SectionProbeModeRealtimeSettings
                        )
                    );
        }

        protected static void Drawer_DifferentShapeError(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var proxy = d.proxyVolumeReference.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null
                && (int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
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
            EditorGUILayout.PropertyField(d.proxyVolumeReference, proxyVolumeContent);
            
            if (d.target.proxyVolume == null)
            {
                EditorGUI.BeginChangeCheck();
                d.infiniteProjection.boolValue = !EditorGUILayout.Toggle(useInfiniteProjectionContent, !d.infiniteProjection.boolValue);
                if(EditorGUI.EndChangeCheck())
                {
                    d.Apply();
                }
            }

            if (d.proxyVolumeReference.objectReferenceValue != null)
            {
                var proxy = (ReflectionProxyVolumeComponent)d.proxyVolumeReference.objectReferenceValue;
                if ((int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
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
                        d.infiniteProjection.boolValue ? noProxyInfiniteHelpBoxText : noProxyHelpBoxText,
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
                d.lightLayers.intValue = Convert.ToInt32(EditorGUILayout.EnumFlagsField(lightLayersContent, (LightLayerEnum)d.lightLayers.intValue));
            }

            EditorGUILayout.PropertyField(d.weight, weightContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.multiplier, multiplierContent);
            if (EditorGUI.EndChangeCheck())
                d.multiplier.floatValue = Mathf.Max(0.0f, d.multiplier.floatValue);
        }

        protected static void Drawer_FieldCaptureType(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            //GUI.enabled = false;
            EditorGUILayout.PropertyField(d.mode, fieldCaptureTypeContent);
            GUI.enabled = true;
        }


        [Flags]
        internal enum ToolBar
        {
            InfluenceShape = 1<<0,
            Blend = 1<<1,
            NormalBlend = 1<<2,
            CapturePosition = 1<<3
        }
        protected ToolBar[] toolBars = null;

        protected const EditMode.SceneViewEditMode EditBaseShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        protected const EditMode.SceneViewEditMode EditInfluenceShape = EditMode.SceneViewEditMode.GridBox;
        protected const EditMode.SceneViewEditMode EditInfluenceNormalShape = EditMode.SceneViewEditMode.Collider;
        protected const EditMode.SceneViewEditMode EditCenter = EditMode.SceneViewEditMode.GridMove;
        //Note: EditMode.SceneViewEditMode.ReflectionProbeOrigin is still used
        //by legacy reflection probe and have its own mecanism that we don't want
        
        internal static bool IsProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditBaseShape
                || editMode == EditInfluenceShape
                || editMode == EditInfluenceNormalShape
                || editMode == EditCenter;
        }

        static Dictionary<ToolBar, EditMode.SceneViewEditMode> s_Toolbar_Mode = null;
        protected static Dictionary<ToolBar, EditMode.SceneViewEditMode> toolbar_Mode
        {
            get
            {
                return s_Toolbar_Mode ?? (s_Toolbar_Mode = new Dictionary<ToolBar, EditMode.SceneViewEditMode>
                {
                    { ToolBar.InfluenceShape,  EditBaseShape },
                    { ToolBar.Blend,  EditInfluenceShape },
                    { ToolBar.NormalBlend,  EditInfluenceNormalShape },
                    { ToolBar.CapturePosition,  EditCenter }
                });
            }
        }

        //[TODO] change this to be modifiable shortcuts
        static Dictionary<KeyCode, ToolBar> s_Toolbar_ShortCutKey = null;
        protected static Dictionary<KeyCode, ToolBar> toolbar_ShortCutKey
        {
            get
            {
                return s_Toolbar_ShortCutKey ?? (s_Toolbar_ShortCutKey = new Dictionary<KeyCode, ToolBar>
                {
                    { KeyCode.Alpha1, ToolBar.InfluenceShape },
                    { KeyCode.Alpha2, ToolBar.Blend },
                    { KeyCode.Alpha3, ToolBar.NormalBlend },
                    { KeyCode.Alpha4, ToolBar.CapturePosition }
                });
            }
        }
        
        protected static void Drawer_Toolbars(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            foreach(ToolBar toolBar in s.toolBars)
            {
                List<EditMode.SceneViewEditMode> listMode = new List<EditMode.SceneViewEditMode>();
                List<GUIContent> listContent = new List<GUIContent>(); 
                if ((toolBar & ToolBar.InfluenceShape) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.InfluenceShape]);
                    listContent.Add(toolbar_Contents[ToolBar.InfluenceShape]);
                }
                if ((toolBar & ToolBar.Blend) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.Blend]);
                    listContent.Add(toolbar_Contents[ToolBar.Blend]);
                }
                if ((toolBar & ToolBar.NormalBlend) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.NormalBlend]);
                    listContent.Add(toolbar_Contents[ToolBar.NormalBlend]);
                }
                if ((toolBar & ToolBar.CapturePosition) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.CapturePosition]);
                    listContent.Add(toolbar_Contents[ToolBar.CapturePosition]);
                }
                EditMode.DoInspectorToolbar(listMode.ToArray(), listContent.ToArray(), GetBoundsGetter(o), o);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        static internal void Drawer_ToolBarButton(ToolBar button, Editor owner, params GUILayoutOption[] options)
        {
            bool enabled = toolbar_Mode[button] == EditMode.editMode;
            EditorGUI.BeginChangeCheck();
            enabled = GUILayout.Toggle(enabled, toolbar_Contents[button], EditorStyles.miniButton, options);
            if (EditorGUI.EndChangeCheck())
            {
                EditMode.SceneViewEditMode targetMode = EditMode.editMode == toolbar_Mode[button] ? EditMode.SceneViewEditMode.None : toolbar_Mode[button];
                EditMode.ChangeEditMode(targetMode, GetBoundsGetter(owner)(), owner);
            }
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in o.targets)
                    {
                        var rp = targetObject.transform;
                        var b = rp.position;
                        bounds.Encapsulate(b);
                    }
                    return bounds;
                };
        }

        public void DoShortcutKey(Editor owner)
        {
            var evt = Event.current;
            if (evt.type != EventType.KeyDown || !evt.shift)
                return;

            ToolBar toolbar;
            if(toolbar_ShortCutKey.TryGetValue(evt.keyCode, out toolbar))
            {
                bool used = false;
                foreach(ToolBar t in toolBars)
                {
                    if((t&toolbar)>0)
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
