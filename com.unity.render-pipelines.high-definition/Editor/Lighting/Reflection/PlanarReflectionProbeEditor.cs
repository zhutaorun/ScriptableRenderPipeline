using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    [CustomEditorForRenderPipeline(typeof(PlanarReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    sealed class PlanarReflectionProbeEditor : HDProbeEditor<PlanarReflectionProbeUISettingsProvider>
    {
        const float k_PreviewHeight = 128;
        List<Texture> m_PreviewedTextures = new List<Texture>();

        public override bool HasPreviewGUI()
        {
            foreach (PlanarReflectionProbe p in m_TypedTargets)
            {
                if (p.texture != null)
                    return true;
            }
            return false;
        }

        public override GUIContent GetPreviewTitle() => _.GetContent("Planar Reflection");

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            m_PreviewedTextures.Clear();
            foreach (PlanarReflectionProbe p in m_TypedTargets)
            {
                m_PreviewedTextures.Add(p.texture);
            }

            var space = Vector2.one;
            var rowSize = Mathf.CeilToInt(Mathf.Sqrt(m_PreviewedTextures.Count));
            var size = r.size / rowSize - space * (rowSize - 1);

            for (var i = 0; i < m_PreviewedTextures.Count; i++)
            {
                var row = i / rowSize;
                var col = i % rowSize;
                var itemRect = new Rect(
                        r.x + size.x * row + ((row > 0) ? (row - 1) * space.x : 0),
                        r.y + size.y * col + ((col > 0) ? (col - 1) * space.y : 0),
                        size.x,
                        size.y);

                if (m_PreviewedTextures[i] != null)
                    EditorGUI.DrawPreviewTexture(itemRect, m_PreviewedTextures[i], CameraEditorUtils.GUITextureBlit2SRGBMaterial, ScaleMode.ScaleToFit, 0, 1);
                else
                    EditorGUI.LabelField(itemRect, _.GetContent("Not Available"));
            }
        }

        internal override HDProbe GetTarget(Object editorTarget) => editorTarget as HDProbe;

        protected override void OnEnable()
        {
            m_SerializedHDProbe = new SerializedPlanarReflectionProbe(serializedObject);
            base.OnEnable();
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            SceneViewOverlay_Window(_.GetContent("Planar Probe"), OnOverlayGUI, -100, target);

            var ui = new PlanarReflectionProbeUI();
            ui.Update(m_SerializedHDProbe);
            PlanarReflectionProbeUI.DrawHandles(ui, (SerializedPlanarReflectionProbe)m_SerializedHDProbe, this);
        }

        protected override HDProbeUI NewUI() => new PlanarReflectionProbeUI();

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            var previewSize = new Rect();
            foreach(PlanarReflectionProbe p in m_TypedTargets)
            {
                if (p.texture == null)
                    continue;

                var factor = k_PreviewHeight / p.texture.height;

                previewSize.x += p.texture.width * factor;
                previewSize.y = k_PreviewHeight;
            }

            // Get and reserve rect
            Rect cameraRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y);

            if (Event.current.type == EventType.Repaint)
            {
                var c = new Rect(cameraRect);
                foreach(PlanarReflectionProbe p in m_TypedTargets)
                {
                    if (p.texture == null)
                        continue;

                    var factor = k_PreviewHeight / p.texture.height;

                    c.width = p.texture.width * factor;
                    c.height = k_PreviewHeight;
                    Graphics.DrawTexture(c, p.texture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, CameraEditorUtils.GUITextureBlit2SRGBMaterial);

                    c.x += c.width;
                }
            }
        }

        static Type k_SceneViewOverlay_WindowFunction = Type.GetType("UnityEditor.SceneViewOverlay+WindowFunction,UnityEditor");
        static Type k_SceneViewOverlay_WindowDisplayOption = Type.GetType("UnityEditor.SceneViewOverlay+WindowDisplayOption,UnityEditor");
        static MethodInfo k_SceneViewOverlay_Window = Type.GetType("UnityEditor.SceneViewOverlay,UnityEditor")
            .GetMethod(
                "Window",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                CallingConventions.Any,
                new[] { typeof(GUIContent), k_SceneViewOverlay_WindowFunction, typeof(int), typeof(Object), k_SceneViewOverlay_WindowDisplayOption },
                null);
        static void SceneViewOverlay_Window(GUIContent title, Action<Object, SceneView> sceneViewFunc, int order, Object target)
        {
            k_SceneViewOverlay_Window.Invoke(null, new[]
            {
                title, DelegateUtility.Cast(sceneViewFunc, k_SceneViewOverlay_WindowFunction),
                order,
                target,
                Enum.ToObject(k_SceneViewOverlay_WindowDisplayOption, 1)
            });
        }
    }

    struct PlanarReflectionProbeUISettingsProvider : HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
    {
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawOffset => false;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawNormal => false;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawFace => false;


        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedCaptureSettings => new ProbeSettingsOverride
        {
            probe = (ProbeSettingsFields)(-1),
            camera = new CameraSettingsOverride
            {
                camera = (CameraSettingsFields)(-1)
            }
        };
        public ProbeSettingsOverride overrideableCaptureSettings => new ProbeSettingsOverride();
        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedAdvancedSettings => new ProbeSettingsOverride
        {
            probe = (ProbeSettingsFields)(-1),
            camera = new CameraSettingsOverride
            {
                camera = (CameraSettingsFields)(-1)
            }
        };
        public ProbeSettingsOverride overrideableAdvancedSettings => new ProbeSettingsOverride();
        Type HDProbeUI.IProbeUISettingsProvider.customTextureType => typeof(Cubemap);
        static readonly HDProbeUI.ToolBar[] k_Toolbars = { HDProbeUI.ToolBar.InfluenceShape | HDProbeUI.ToolBar.Blend };
        HDProbeUI.ToolBar[] HDProbeUI.IProbeUISettingsProvider.toolbars => k_Toolbars;

    }
}
