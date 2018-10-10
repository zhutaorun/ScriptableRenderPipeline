using System;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    abstract partial class HDProbeUI : BaseUI<SerializedHDProbe>
    {
        const int k_AnimBoolSingleFieldCount = 6;
        static readonly int k_ReflectionProbeModeCount = Enum.GetValues(typeof(ProbeSettings.Mode)).Length;
        static readonly int k_ReflectionInfluenceShapeCount = Enum.GetValues(typeof(InfluenceShape)).Length;
        static readonly int k_AnimBoolTotal = k_ReflectionProbeModeCount + k_AnimBoolSingleFieldCount + k_ReflectionInfluenceShapeCount;

        public ReflectionProxyVolumeComponentUI reflectionProxyVolume = new ReflectionProxyVolumeComponentUI();
        public ProbeSettingsUI probeSettings = new ProbeSettingsUI();

        public AnimBool isSectionExpandedInfluenceSettings { get { return m_AnimBools[k_ReflectionProbeModeCount]; } }
        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[k_ReflectionProbeModeCount + 1]; } }
        public AnimBool isFrameSettingsOverriden { get { return m_AnimBools[k_ReflectionProbeModeCount + 2]; } }
        public AnimBool isSectionExpandedProxyVolume { get { return m_AnimBools[k_ReflectionProbeModeCount + 4]; } }
        public AnimBool isSectionExpandedAdditionalSettings { get { return m_AnimBools[k_ReflectionProbeModeCount + 5]; } }

        public bool showCaptureHandles { get; set; }

        internal static HDProbeUI CreateFor(HDProbeEditor o)
        {
            if (o is PlanarReflectionProbeEditor)
                return new PlanarReflectionProbeUI();
            else
                return new HDReflectionProbeUI();
        }
        internal static HDProbeUI CreateFor(HDProbe p)
        {
            if (p is PlanarReflectionProbe)
                return new PlanarReflectionProbeUI();
            else
                return new HDReflectionProbeUI();
        }

        public HDProbeUI()
            : base(k_AnimBoolTotal)
        {
            isSectionExpandedInfluenceSettings.value = true;
            isSectionExpandedCaptureSettings.value = true;
            isSectionExpandedProxyVolume.value = true;
            isSectionExpandedAdditionalSettings.value = false;
        }

        public AnimBool IsSectionExpandedReflectionProbeMode(ProbeSettings.Mode mode)
        {
            return m_AnimBools[(int)mode];
        }

        public void SetModeTarget(int value)
        {
            for (var i = 0; i < k_ReflectionProbeModeCount; i++)
                GetReflectionProbeModeBool(i).target = i == value;
        }

        AnimBool GetReflectionProbeModeBool(int i)
        {
            return m_AnimBools[i];
        }

        public override void Reset(SerializedHDProbe data, UnityAction repaint)
        {
            probeSettings.Reset(data.probeSettings, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            for (var i = 0; i < k_ReflectionProbeModeCount; i++)
                m_AnimBools[i].target = i == data.probeSettings.mode.intValue;

            probeSettings.Update();
            
            SetModeTarget(data.probeSettings.mode.hasMultipleDifferentValues ? -1 : data.probeSettings.mode.intValue);
            base.Update();
        }
    }
}
