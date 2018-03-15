using System.Collections.Generic;


using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{

    //LW class name and namespace are the same. Gotta do this for now.
    using LWPipeline = UnityEngine.Experimental.Rendering.LightweightPipeline.LightweightPipeline;
	public class LWBaseEditor<T> : Editor where T : UnityEngine.Object
	{
		internal PropertyFetcher<T> properties { get; private set; }

		protected T m_Target
        {
            get { return target as T; }
        }

        protected T[] m_Targets
        {   
            get { return targets as T[]; }
        }

        protected LWPipeline m_LWPipeline
        {
            get { return RenderPipelineManager.currentPipeline as LWPipeline; }
        }

        protected virtual void OnEnable()
        {  
            properties = new PropertyFetcher<T>(serializedObject);
        }
	}

	[CustomEditor(typeof(DiffusionProfileSettings))]
	public sealed partial class DiffusionProfileSettingsEditor : LWBaseEditor<DiffusionProfileSettings>
	{
        sealed class Profile
        {
            internal SerializedProperty self;
            internal DiffusionProfile objReference;

            internal SerializedProperty name;
            internal SerializedProperty transmissionTint;
            internal SerializedProperty thicknessRemap;
            internal SerializedProperty scatterDistance1;
            internal SerializedProperty scatterDistance2;
            internal SerializedProperty lerpWeight;

            // Render preview
            internal RenderTexture profileRT;
            internal RenderTexture transmittanceRT;

            internal Profile()
            {
                profileRT       = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
                transmittanceRT = new RenderTexture( 16, 256, 0, RenderTextureFormat.DefaultHDR);
            }

            internal void Release()
            {
                CoreUtils.Destroy(profileRT);
                CoreUtils.Destroy(transmittanceRT);
            }
        }

        List<Profile> m_Profiles;

        Material m_ProfileMaterial;
        Material m_TransmittanceMaterial;

        protected override void OnEnable()
        {
            base.OnEnable();

            // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime
            m_ProfileMaterial       = CoreUtils.CreateEngineMaterial("Hidden/LightweightPipeline/PreintegratedScatter");
            m_TransmittanceMaterial = CoreUtils.CreateEngineMaterial("Hidden/LightweightPipeline/DrawTransmittanceGraph");

            int count = DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1;
            m_Profiles = new List<Profile>();

            var serializedProfiles = properties.Find(x => x.profiles);

            for (int i = 0; i < count; i++)
            {
                var serializedProfile = serializedProfiles.GetArrayElementAtIndex(i);
                var rp = new RelativePropertyFetcher<DiffusionProfile>(serializedProfile);

                var profile = new Profile
                {
                    self = serializedProfile,
                    objReference = m_Target.profiles[i],

                    name = rp.Find(x => x.name),

                    transmissionTint    = rp.Find(x => x.transmissionTint),
                    thicknessRemap      = rp.Find(x => x.thicknessRemap),
                    scatterDistance1    = rp.Find(x => x.scatterDistance1),
                    scatterDistance2    = rp.Find(x => x.scatterDistance2),
                    lerpWeight          = rp.Find(x => x.lerpWeight)
                };

                m_Profiles.Add(profile);
            }
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_ProfileMaterial);
            CoreUtils.Destroy(m_TransmittanceMaterial);

            foreach (var profile in m_Profiles)
                profile.Release();

            m_Profiles = null;
        }

        public override void OnInspectorGUI()
        {
            CheckStyles();

            // Display a warning if this settings asset is not currently in use
            if (m_LWPipeline == null || m_LWPipeline.DiffusionProfileSettings != m_Target) 
                EditorGUILayout.HelpBox("These profiles aren't currently in use, assign this asset to the LW render pipeline asset to use them.", MessageType.Warning);

            serializedObject.Update();

            EditorGUILayout.Space();

            if (m_Profiles == null || m_Profiles.Count == 0)
                return;

            for (int i = 0; i < m_Profiles.Count; i++)
            {
                var profile = m_Profiles[i];

                CoreEditorUtils.DrawSplitter();

                bool state = profile.self.isExpanded;
                state = CoreEditorUtils.DrawHeaderFoldout(profile.name.stringValue, state);

                if (state)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(profile.name);

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(profile.scatterDistance1, s_Styles.profileScatterDistance1);
                        EditorGUILayout.PropertyField(profile.scatterDistance2, s_Styles.profileScatterDistance2);
                        EditorGUILayout.PropertyField(profile.lerpWeight, s_Styles.profileLerpWeight);

                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(s_Styles.TransmissionLabel, EditorStyles.boldLabel);

                        EditorGUILayout.PropertyField(profile.transmissionTint, s_Styles.profileTransmissionTint);
                        EditorGUILayout.PropertyField(profile.thicknessRemap, s_Styles.profileMinMaxThickness);
                        var thicknessRemap = profile.thicknessRemap.vector2Value;
                        EditorGUILayout.MinMaxSlider(s_Styles.profileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0f, 50f);
                        profile.thicknessRemap.vector2Value = thicknessRemap;

                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(s_Styles.profilePreview0, s_Styles.centeredMiniBoldLabel);
                        EditorGUILayout.LabelField(s_Styles.profilePreview1, EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.LabelField(s_Styles.profilePreview2, EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.LabelField(s_Styles.profilePreview3, EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.Space();

                        serializedObject.ApplyModifiedProperties();

                        if (scope.changed)
                        {
                            // Validate and update the cache for this profile only
                            profile.objReference.Validate();
                            m_Target.UpdateCache(i);
                        }
                    }

                    RenderPreview(profile);

                    EditorGUILayout.Space();
                    EditorGUI.indentLevel--;
                }

                profile.self.isExpanded = state;
            }

            CoreEditorUtils.DrawSplitter();
            
            serializedObject.ApplyModifiedProperties();
        }

        void RenderPreview(Profile profile)
        {
            var T   = (Vector4)profile.transmissionTint.colorValue;
            var R   = profile.thicknessRemap.vector2Value;

            // Apply the three-sigma rule, and rescale.
            float s = (1f / 3f) * DiffusionProfileConstants.SSS_DISTANCE_SCALE;
            var scatterDist1 = profile.scatterDistance1.colorValue;
            var scatterDist2 = profile.scatterDistance2.colorValue;
            //float rMax = Mathf.Max(scatterDist1.r, scatterDist1.g, scatterDist1.b,
            //                       scatterDist2.r, scatterDist2.g, scatterDist2.b);
            var stdDev1 = s * (Vector4)scatterDist1;
            var stdDev2 = s * (Vector4)scatterDist2;
            m_ProfileMaterial.SetVector(DiffusionProfileShaderIDs._StdDev1, stdDev1);
            m_ProfileMaterial.SetVector(DiffusionProfileShaderIDs._StdDev2, stdDev2);
            m_ProfileMaterial.SetFloat (DiffusionProfileShaderIDs._LerpWeight, profile.lerpWeight.floatValue);

            // Draw the profile.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(128f, 128f), profile.profileRT, m_ProfileMaterial, ScaleMode.ScaleToFit, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.transmittancePreview0, s_Styles.centeredMiniBoldLabel);
            EditorGUILayout.LabelField(s_Styles.transmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(s_Styles.transmittancePreview2, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Multiply by 0.1 to convert from millimeters to centimeters. Apply the distance scale.
            float a = 0.1f * DiffusionProfileConstants.SSS_DISTANCE_SCALE;
            var halfRcpVarianceAndWeight1 = new Vector4(a * a * 0.5f / (stdDev1.x * stdDev1.x), a * a * 0.5f / (stdDev1.y * stdDev1.y), a * a * 0.5f / (stdDev1.z * stdDev1.z), 4f * (1f - profile.lerpWeight.floatValue));
            var halfRcpVarianceAndWeight2 = new Vector4(a * a * 0.5f / (stdDev2.x * stdDev2.x), a * a * 0.5f / (stdDev2.y * stdDev2.y), a * a * 0.5f / (stdDev2.z * stdDev2.z), 4f * profile.lerpWeight.floatValue);
            m_TransmittanceMaterial.SetVector(DiffusionProfileShaderIDs._HalfRcpVarianceAndWeight1, halfRcpVarianceAndWeight1);
            m_TransmittanceMaterial.SetVector(DiffusionProfileShaderIDs._HalfRcpVarianceAndWeight2, halfRcpVarianceAndWeight2);

            m_TransmittanceMaterial.SetVector(DiffusionProfileShaderIDs._TransmissionTint, T);
            m_TransmittanceMaterial.SetVector(DiffusionProfileShaderIDs._ThicknessRemap, R);

            // Draw the transmittance graph.
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16f, 16f), profile.transmittanceRT, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16f);
        }
	}
}