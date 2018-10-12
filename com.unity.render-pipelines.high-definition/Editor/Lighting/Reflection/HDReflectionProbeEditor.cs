using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    sealed partial class HDReflectionProbeEditor : HDProbeEditor<HDProbeSettingsProvider>
    {
        #region Context Menu
        [MenuItem("CONTEXT/ReflectionProbe/Remove Component", false, 0)]
        static void RemoveReflectionProbe(MenuCommand menuCommand)
        {
            GameObject go = ((ReflectionProbe)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Undo.SetCurrentGroupName("Remove HD Reflection Probe");
            Undo.DestroyObjectImmediate(go.GetComponent<ReflectionProbe>());
            Undo.DestroyObjectImmediate(go.GetComponent<HDAdditionalReflectionData>());
        }

        [MenuItem("CONTEXT/ReflectionProbe/Reset", false, 0)]
        static void ResetReflectionProbe(MenuCommand menuCommand)
        {
            GameObject go = ((ReflectionProbe)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            ReflectionProbe reflectionProbe = go.GetComponent<ReflectionProbe>();
            HDAdditionalReflectionData reflectionProbeAdditionalData = go.GetComponent<HDAdditionalReflectionData>();

            Assert.IsNotNull(reflectionProbe);
            Assert.IsNotNull(reflectionProbeAdditionalData);

            Undo.SetCurrentGroupName("Reset HD Reflection Probe");
            Undo.RecordObjects(new UnityEngine.Object[] { reflectionProbe, reflectionProbeAdditionalData }, "Reset HD Reflection Probe");
            reflectionProbe.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalReflectionData, thus why we don't wrap it in Reset() function
            EditorUtility.CopySerialized(HDUtils.s_DefaultHDAdditionalReflectionData, reflectionProbeAdditionalData);
        }
        #endregion

        static Dictionary<ReflectionProbe, HDReflectionProbeEditor> s_ReflectionProbeEditors = new Dictionary<ReflectionProbe, HDReflectionProbeEditor>();
        
        SerializedObject m_AdditionalDataSerializedObject;

        protected override void OnEnable()
        {
            var additionalData = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            m_AdditionalDataSerializedObject = new SerializedObject(additionalData);
            m_SerializedHDProbe = new SerializedHDReflectionProbe(serializedObject, m_AdditionalDataSerializedObject);

            foreach (var t in targets)
            {
                var p = (ReflectionProbe)t;
                s_ReflectionProbeEditors[p] = this;
            }

            base.OnEnable();
            
            InitializeTargetProbe();
        }

        protected override HDProbeUI NewUI() => new HDReflectionProbeUI();

        internal override HDProbe GetTarget(UnityEngine.Object editorTarget)
            => ((ReflectionProbe)editorTarget).GetComponent<HDAdditionalReflectionData>();
        static HDReflectionProbeEditor GetEditorFor(ReflectionProbe p)
            => s_ReflectionProbeEditors.TryGetValue(p, out HDReflectionProbeEditor e)
                ? e : null;
    }

    struct HDProbeSettingsProvider : HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
    {
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawOffset => true;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawNormal => true;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawFace => true;

        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedCaptureSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.proxyCapturePositionProxySpace
                | ProbeSettingsFields.proxyCaptureRotationProxySpace,
            camera = new CameraSettingsOverride
            {
                camera = (CameraSettingsFields)(-1) & ~(
                    CameraSettingsFields.frustumFieldOfView
                    | CameraSettingsFields.flipYMode
                    | CameraSettingsFields.cullingInvertCulling
                    | CameraSettingsFields.frustumMode
                    | CameraSettingsFields.frustumProjectionMatrix
                )
            }
        };
        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.overrideableCaptureSettings => new ProbeSettingsOverride();
        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedAdvancedSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.lightingLightLayer
                | ProbeSettingsFields.lightingMultiplier
                | ProbeSettingsFields.lightingWeight,
            camera = new CameraSettingsOverride
            {
                camera = CameraSettingsFields.none
            }
        };
        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.overrideableAdvancedSettings => new ProbeSettingsOverride();

        Type HDProbeUI.IProbeUISettingsProvider.customTextureType => typeof(Cubemap);
        static readonly HDProbeUI.ToolBar[] k_ToolBars
        = { HDProbeUI.ToolBar.InfluenceShape | HDProbeUI.ToolBar.NormalBlend | HDProbeUI.ToolBar.Blend, HDProbeUI.ToolBar.CapturePosition };
        HDProbeUI.ToolBar[] HDProbeUI.IProbeUISettingsProvider.toolbars => k_ToolBars;

        
    }
}
