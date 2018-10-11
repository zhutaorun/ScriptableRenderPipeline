using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    abstract class HDProbeEditor<TProvider> : Editor
            where TProvider : struct, HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
    {
        internal abstract HDProbe GetTarget(Object editorTarget);

        protected SerializedHDProbe m_SerializedHDProbe;
        internal HDProbeUI m_UIState;
        HDProbeUI[] m_UIHandleState;
        protected HDProbe[] m_TypedTargets;

        protected virtual void OnEnable()
        {
            if(m_UIState == null)
                m_UIState = HDProbeUI.CreateFor(this);

            m_TypedTargets = new HDProbe[targets.Length];
            m_UIHandleState = new HDProbeUI[m_TypedTargets.Length];
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_TypedTargets[i] = GetTarget(targets[i]);
                m_UIHandleState[i] = HDProbeUI.CreateFor(m_TypedTargets[i]);
            }
        }

        protected virtual void Draw(HDProbeUI s, SerializedHDProbe p, Editor o)
        {
            HDProbeUI.Drawer<TProvider>.DrawPrimarySettings(s, p, o);
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedProjection, "Projection Settings"))
                HDProbeUI.Drawer<TProvider>.DrawProjectionSettings(s, p, o);
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedInfluence, "Influence Volume"))
                HDProbeUI.Drawer<TProvider>.DrawInfluenceSettings(s, p, o);
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedCapture, "Capture Settings"))
                HDProbeUI.Drawer<TProvider>.DrawCaptureSettings(s, p, o);
            if (DrawAndSetSectionFoldout(s, HDProbeUI.Flag.SectionExpandedCustom, "Custom Settings"))
                HDProbeUI.Drawer<TProvider>.DrawCustomSettings(s, p, o);
        }

        protected virtual void OnSceneGUI()
        {
            //mandatory update as for strange reason the serialized rollback one update here
            m_UIState.Update(m_SerializedHDProbe);
            m_SerializedHDProbe.Update();

            HDProbeUI.DrawHandles(m_UIState, m_SerializedHDProbe, this);
            HDProbeUI.Drawer<TProvider>.DoToolbarShortcutKey(this);
        }

        // TODO: generalize this
        static bool DrawAndSetSectionFoldout(HDProbeUI s, HDProbeUI.Flag flag, string title)
            => s.SetFlag(flag, HDEditorUtils.DrawSectionFoldout(title, s.HasFlag(flag)));
    }
}
