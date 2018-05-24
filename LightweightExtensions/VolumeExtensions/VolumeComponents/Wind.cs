using System;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEngine.Experimental.Rendering
{
	public enum WindMode
    {
        Directional,
        Omni
    }

	[Serializable]
    public sealed class WindModeParameter : VolumeParameter<WindMode> {}

    [Serializable]
    public class Wind : VolumeComponent, IRendererEffect
    {
      	public BoolParameter                	enable = new BoolParameter(true);
		public WindModeParameter				mode = new WindModeParameter();
		public FloatParameter					strength = new FloatParameter(1);
		public FloatParameter					turbulence = new FloatParameter(1);
      	
		public string GetKeywordName () { return "WIND"; }

		public EffectData[] GetValue()
		{
			Vector3 direction = new Vector3(1,0,0);

			EffectData directionData = new EffectData("Direction", direction, typeof(Vector3));
			EffectData strengthData = new EffectData("Strength", enable ? strength.GetValue<float>() : 0, typeof(float));
			EffectData turbulenceData = new EffectData("Turbulence", turbulence.GetValue<float>(), typeof(float));
			return new EffectData[] { directionData, strengthData, turbulenceData };
		}
    }
}
