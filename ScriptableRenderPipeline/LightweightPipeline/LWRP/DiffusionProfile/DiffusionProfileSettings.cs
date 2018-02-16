using System;

using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    
	//A minimal implementation of HDRP Diffusion Profile, to support out preintegrated subsurface scattering model.

	[GenerateHLSL]
	public class DiffusionProfileConstants
	{
		public const int DIFFUSION_PROFILE_COUNT 	  =  5;
		public const int DIFFUSION_PROFILE_NEUTRAL_ID =  0;
		public const int SSS_N_SAMPLES     	 	      = 11;
		public const int SSS_DISTANCE_SCALE 	 	  =  3;
	}

    public static class DiffusionProfileShaderIDs
    {
        public static readonly int _StdDev1                   = Shader.PropertyToID("_StdDev1");
        public static readonly int _StdDev2                   = Shader.PropertyToID("_StdDev2");
        public static readonly int _LerpWeight                = Shader.PropertyToID("_LerpWeight");
        public static readonly int _HalfRcpVarianceAndWeight1 = Shader.PropertyToID("_HalfRcpVarianceAndWeight1");
        public static readonly int _HalfRcpVarianceAndWeight2 = Shader.PropertyToID("_HalfRcpVarianceAndWeight2");
        public static readonly int _TransmissionTint          = Shader.PropertyToID("_TransmissionTint");
        public static readonly int _ThicknessRemap            = Shader.PropertyToID("_ThicknessRemap");

        //Addition Runtime Constants
        public static readonly int _PreintegratedDiffuseScatteringTextures = Shader.PropertyToID("_PreintegratedDiffuseScatteringTextures");
        public static readonly int _HalfRcpVariancesAndWeights             = Shader.PropertyToID("_HalfRcpVariancesAndWeights");
    }

	[Serializable]
	public sealed class DiffusionProfile
	{
		public string name;

		[ColorUsage(false, true)] public Color scatterDistance1;
		[ColorUsage(false, true)] public Color scatterDistance2;
		[Range(0f, 1f)] public float lerpWeight;
		[ColorUsage(false)] public Color transmissionTint;
		public Vector2 thicknessRemap;

		public Vector4[] filterKernel { get; private set; }
		public Vector4	 halfRcpWeightedVariances { get; private set; }

		public DiffusionProfile(string name)
		{
			this.name = name;

			scatterDistance1 = new Color(0.3f, 0.3f, 0.3f, 0f);
			scatterDistance2 = new Color(0.5f, 0.5f, 0.5f, 0f);
			lerpWeight = 1f;

			transmissionTint = Color.white;
			thicknessRemap = new Vector2(0f, 5f);

		}

		public void Validate()
		{
			thicknessRemap.y = Mathf.Max(thicknessRemap.y, 0f);
			thicknessRemap.x = Mathf.Clamp(thicknessRemap.x, 0f, thicknessRemap.y);

			scatterDistance1 = new Color
			{
				r = Mathf.Max(0.05f, scatterDistance1.r),
				g = Mathf.Max(0.05f, scatterDistance1.g),
				b = Mathf.Max(0.05f, scatterDistance1.b),
				a = 0.0f
			};
						
			scatterDistance2 = new Color
			{
				r = Mathf.Max(0.05f, scatterDistance2.r),
				g = Mathf.Max(0.05f, scatterDistance2.g),
				b = Mathf.Max(0.05f, scatterDistance2.b),
				a = 0.0f
			};

			UpdateKernelAndVarianceData();
		}

		public void UpdateKernelAndVarianceData()
		{
			const int kNumSamples 	 = DiffusionProfileConstants.SSS_N_SAMPLES;
			const int kDistanceScale = DiffusionProfileConstants.SSS_DISTANCE_SCALE;

			if (filterKernel == null || filterKernel.Length != kNumSamples)
                filterKernel = new Vector4[kNumSamples];

            // Apply the three-sigma rule, and rescale.
            var stdDev1 = ((1f / 3f) * kDistanceScale) * scatterDistance1;
            var stdDev2 = ((1f / 3f) * kDistanceScale) * scatterDistance2;

            // Our goal is to blur the image using a filter which is represented
            // as a product of a linear combination of two normalized 1D Gaussians
            // as suggested by Jimenez et al. in "Separable Subsurface Scattering".
            // A normalized (i.e. energy-preserving) 1D Gaussian with the mean of 0
            // is defined as follows: G1(x, v) = exp(-x * x / (2 * v)) / sqrt(2 * Pi * v),
            // where 'v' is variance and 'x' is the radial distance from the origin.
            // Using the weight 'w', our 1D and the resulting 2D filters are given as:
            // A1(v1, v2, w, x)    = G1(x, v1) * (1 - w) + G1(r, v2) * w,
            // A2(v1, v2, w, x, y) = A1(v1, v2, w, x) * A1(v1, v2, w, y).
            // The resulting filter function is a non-Gaussian PDF.
            // It is separable by design, but generally not radially symmetric.

            // N.b.: our scattering distance is rather limited. Therefore, in order to allow
            // for a greater range of standard deviation values for flatter profiles,
            // we rescale the world using 'distanceScale', effectively reducing the SSS
            // distance units from centimeters to (1 / distanceScale).

            // Find the widest Gaussian across 3 color channels.
            float maxStdDev1 = Mathf.Max(stdDev1.r, stdDev1.g, stdDev1.b);
            float maxStdDev2 = Mathf.Max(stdDev2.r, stdDev2.g, stdDev2.b);

            var weightSum = Vector3.zero;

            float step = 1f / (kNumSamples - 1);

            // Importance sample the linear combination of two Gaussians.
            for (int i = 0; i < kNumSamples; i++)
            {
                // Generate 'u' on (0, 0.5] and (0.5, 1).
                float u = (i <= kNumSamples / 2) ? 0.5f - i * step // The center and to the left
                                                 : i * step;       // From the center to the right

                u = Mathf.Clamp(u, 0.001f, 0.999f);

                float pos = GaussianCombinationCdfInverse(u, maxStdDev1, maxStdDev2, lerpWeight);
                float pdf = GaussianCombination(pos, maxStdDev1, maxStdDev2, lerpWeight);

                Vector3 val;
                val.x = GaussianCombination(pos, stdDev1.r, stdDev2.r, lerpWeight);
                val.y = GaussianCombination(pos, stdDev1.g, stdDev2.g, lerpWeight);
                val.z = GaussianCombination(pos, stdDev1.b, stdDev2.b, lerpWeight);

                // We do not divide by 'numSamples' since we will renormalize, anyway.
                filterKernel[i].x = val.x * (1 / pdf);
                filterKernel[i].y = val.y * (1 / pdf);
                filterKernel[i].z = val.z * (1 / pdf);
                filterKernel[i].w = pos;

                weightSum.x += filterKernel[i].x;
                weightSum.y += filterKernel[i].y;
                weightSum.z += filterKernel[i].z;
            }

            // Renormalize the weights to conserve energy.
            for (int i = 0; i < kNumSamples; i++)
            {
                filterKernel[i].x *= 1 / weightSum.x;
                filterKernel[i].y *= 1 / weightSum.y;
                filterKernel[i].z *= 1 / weightSum.z;
            }

            Vector4 weightedStdDev;
            weightedStdDev.x = Mathf.Lerp(stdDev1.r,  stdDev2.r,  lerpWeight);
            weightedStdDev.y = Mathf.Lerp(stdDev1.g,  stdDev2.g,  lerpWeight);
            weightedStdDev.z = Mathf.Lerp(stdDev1.b,  stdDev2.b,  lerpWeight);
            weightedStdDev.w = Mathf.Lerp(maxStdDev1, maxStdDev2, lerpWeight);

            // Store (1 / (2 * WeightedVariance)) per color channel.
            // Warning: do not use halfRcpWeightedVariances.Set(). It will not work.
            halfRcpWeightedVariances = new Vector4(0.5f / (weightedStdDev.x * weightedStdDev.x),
                                                   0.5f / (weightedStdDev.y * weightedStdDev.y),
                                                   0.5f / (weightedStdDev.z * weightedStdDev.z),
                                                   0.5f / (weightedStdDev.w * weightedStdDev.w));
		}

		static float Gaussian(float x, float stdDev)
        {
            float variance = stdDev * stdDev;
            return Mathf.Exp(-x * x / (2 * variance)) / Mathf.Sqrt(2 * Mathf.PI * variance);
        }

        static float GaussianCombination(float x, float stdDev1, float stdDev2, float lerpWeight)
        {
            return Mathf.Lerp(Gaussian(x, stdDev1), Gaussian(x, stdDev2), lerpWeight);
        }

		        static float RationalApproximation(float t)
        {
            // Abramowitz and Stegun formula 26.2.23.
            // The absolute value of the error should be less than 4.5 e-4.
            float[] c = { 2.515517f, 0.802853f, 0.010328f };
            float[] d = { 1.432788f, 0.189269f, 0.001308f };
            return t - ((c[2] * t + c[1]) * t + c[0]) / (((d[2] * t + d[1]) * t + d[0]) * t + 1.0f);
        }

        // Ref: https://www.johndcook.com/blog/csharp_phi_inverse/
        static float NormalCdfInverse(float p, float stdDev)
        {
            float x;

            if (p < 0.5)
            {
                // F^-1(p) = - G^-1(p)
                x = -RationalApproximation(Mathf.Sqrt(-2f * Mathf.Log(p)));
            }
            else
            {
                // F^-1(p) = G^-1(1-p)
                x = RationalApproximation(Mathf.Sqrt(-2f * Mathf.Log(1f - p)));
            }

            return x * stdDev;
        }
		
		static float GaussianCombinationCdfInverse(float p, float stdDev1, float stdDev2, float lerpWeight)
        {
            return Mathf.Lerp(NormalCdfInverse(p, stdDev1), NormalCdfInverse(p, stdDev2), lerpWeight);
        }
	}

	public sealed class DiffusionProfileSettings : ScriptableObject
	{
		public DiffusionProfile[] profiles;

		[NonSerialized] public Vector4[] worldScales;
		[NonSerialized] public Vector4[] thicknessRemaps;
		[NonSerialized] public Vector4[] transmissionTints;

		//TODO: Preintegration turns this into a texture
		[NonSerialized] public Vector4[] halfRcpWeightedVariances;
		[NonSerialized] public Vector4[] halfRcpVariancesAndWeights;
		[NonSerialized] public Vector4[] filterKernels;

        [NonSerialized] private Material      preintegration;
        [NonSerialized] public TextureCache2D preintegratedScatterLUTs;

		public DiffusionProfile this[int index]
        {
            get
            {
                if (index >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
                    throw new IndexOutOfRangeException("index");

                return profiles[index];
            }
        } 
		        
		static void ValidateArray<T>(ref T[] array, int len)
        {
            if (array == null || array.Length != len)
                array = new T[len];
        }

		void OnEnable()
		{
			// The neutral profile is not a part of the array.
            int profileArraySize = DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1;

            if (profiles != null && profiles.Length != profileArraySize)
                Array.Resize(ref profiles, profileArraySize);

            if (profiles == null)
                profiles = new DiffusionProfile[profileArraySize];

            for (int i = 0; i < profileArraySize; i++)
            {
                if (profiles[i] == null)
                    profiles[i] = new DiffusionProfile("Profile " + (i + 1));

                profiles[i].Validate();
            }


            ValidateArray(ref thicknessRemaps,            DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref worldScales,       		  DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref transmissionTints, 		  DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref halfRcpWeightedVariances,   DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref halfRcpVariancesAndWeights, DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 2);
            ValidateArray(ref filterKernels,              DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * DiffusionProfileConstants.SSS_N_SAMPLES);

            Debug.Assert(DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID <= 32, "Transmission and Texture flags (32-bit integer) cannot support more than 32 profiles.");

            //NOTE: We can't get reference to the render pipeline assets from scriptable object, but we add it to the assets anyways so they get included in build.
            //      Searching here is safe because we linked already in the LW resources, forcing it to be included in build.
            preintegration = CoreUtils.CreateEngineMaterial("Hidden/LightweightPipeline/PreintegratedScatter");
            
            preintegratedScatterLUTs = new TextureCache2D();
            preintegratedScatterLUTs.AllocTextureArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, 128, 128, TextureFormat.ARGB32, false);

            UpdateCache();
		}

		public void UpdateCache()
        {
            for (int i = 0; i < DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1; i++)
            {
                UpdateCache(i);
            }

            // Fill the neutral profile.
            int neutralId = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;
            halfRcpWeightedVariances[neutralId] = Vector4.one;
            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES; j < n; j++)
            {
                filterKernels[n * neutralId + j]   = Vector4.one;
                filterKernels[n * neutralId + j].w = 0f;
            }
        }

		public void UpdateCache(int p)
        {
            // 'p' is the profile array index. 'i' is the index in the shader (accounting for the neutral profile).
            int i = p + 1;

            // Erase previous value (This need to be done here individually as in the SSS editor we edit individual component)
            thicknessRemaps[i]   = new Vector4(profiles[p].thicknessRemap.x, profiles[p].thicknessRemap.y - profiles[p].thicknessRemap.x, 0f, 0f);
            // Convert ior to fresnel0
            transmissionTints[i] = new Vector4(profiles[p].transmissionTint.r * 0.25f, profiles[p].transmissionTint.g * 0.25f, profiles[p].transmissionTint.b * 0.25f, 0f); // Premultiplied
            //disabledTransmissionTintsAndFresnel0[i] = new Vector4(0.0f, 0.0f, 0.0f, fresnel0);

            halfRcpWeightedVariances[i] = profiles[p].halfRcpWeightedVariances;

            var stdDev1 = ((1f / 3f) * DiffusionProfileConstants.SSS_DISTANCE_SCALE) * (Vector4)profiles[p].scatterDistance1;
            var stdDev2 = ((1f / 3f) * DiffusionProfileConstants.SSS_DISTANCE_SCALE) * (Vector4)profiles[p].scatterDistance2;

            // Multiply by 0.1 to convert from millimeters to centimeters. Apply the distance scale.
            // Rescale by 4 to counter rescaling of transmission tints.
            float a = 0.1f * DiffusionProfileConstants.SSS_DISTANCE_SCALE;
            halfRcpVariancesAndWeights[2 * i + 0] = new Vector4(a * a * 0.5f / (stdDev1.x * stdDev1.x), a * a * 0.5f / (stdDev1.y * stdDev1.y), a * a * 0.5f / (stdDev1.z * stdDev1.z), 4f * (1f - profiles[p].lerpWeight));
            halfRcpVariancesAndWeights[2 * i + 1] = new Vector4(a * a * 0.5f / (stdDev2.x * stdDev2.x), a * a * 0.5f / (stdDev2.y * stdDev2.y), a * a * 0.5f / (stdDev2.z * stdDev2.z), 4f * profiles[p].lerpWeight);

            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES; j < n; j++)
            {
                filterKernels[n * i + j] = profiles[p].filterKernel[j];
            }

            RTHandle preintegratedScatterRT = RTHandle.Alloc(128, 128, 1, DepthBits.None, RenderTextureFormat.ARGB32, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, false); 


            preintegration.SetVector(DiffusionProfileShaderIDs._StdDev1,    stdDev1);
            preintegration.SetVector(DiffusionProfileShaderIDs._StdDev2,    stdDev2);
            preintegration.SetFloat (DiffusionProfileShaderIDs._LerpWeight, profiles[p].lerpWeight);

            CommandBuffer cmd = new CommandBuffer() { name = "BuildLUT_" + p };
            cmd.Blit(null, preintegratedScatterRT, preintegration);
            preintegratedScatterLUTs.TransferToSlice(cmd, i, preintegratedScatterRT);
            Graphics.ExecuteCommandBuffer(cmd);
        }
	}
}