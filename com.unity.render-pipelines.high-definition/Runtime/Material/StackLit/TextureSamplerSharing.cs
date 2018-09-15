
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    // Make sure that if you add more to the enum, the shader declares more TextureSamplerSharing:k_SharedPropertyBaseName texture
    // properties (including in the uniform declaration file), and make sure the limit override SHARED_SAMPLER_USED_NUM,
    // (but this is meant to be manipulated by the shader generator) is enlarged.
    // (You might also need to add cases in TextureSamplerSharing.hlsl if more than 13 shared samplers are needed as
    // it automatically uses the HLSL generated SharedSamplerID.First to SharedSamplerID.Last to deduce and use the number of cases
    // up to 13 samplers, which should be plenty enough)
    public enum SharedSamplerID
    {
        First = 10,
        //Zero = First+0,
        //One = Zero+1,
        //Two = One+1,
        //Three = Two+1,
        //Four = Three+1,
        //Last = 14,
        Last = First+13-1, // this is the maximum that our hardcoded macro system was written with (see TextureSamplerSharing.hlsl)
    };

    [GenerateHLSL]
    public enum ExternalExistingSampler
    {
        LinearClamp = 1,
        LinearRepeat = 2,
        // to add others, edit TextureSamplerSharing:EncodeSamplerState(ExternalExistingSampler extSamplerUsed), and edit shader code
        // to add them to the switch
    }

}
