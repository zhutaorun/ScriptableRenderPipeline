//-----------------------------------------------------------------------------
// Texture Mapping: Sampler Sharing
//
// -.shader properties and shader uniforms should have _SharedSamplerMap0..4 available
//  (or up to the number of cases here and in the enum in TextureSamplerSharing.cs)
//
// -shader should have shader_feature _USE_SAMPLER_SHARING
//
// -for each texture "name" used, a #define should be available as "name_USES_OWN_SAMPLER" set to
//  0 or 1 to hardcode an override of the shared sampling (when enabled) and make the texture
//  use its own sampler with the normal sampling macros.
//  (This is meant to be used by the shader generator)
//
// -you can hardcode a limit of shared samplers to use with #define SHARED_SAMPLER_USED_NUM num
//  but make sure num is between 1 to whatever number of cases we have here (5).
//  (This is meant to be used by the shader generator)
//
// -make sure these three sampling macros are defined:
//
//  SAMPLE_TEXTURE2D_SCALE_BIAS(name)
//  SAMPLE_TEXTURE2D_NORMAL_SCALE_BIAS(name, scale, objSpace)
//  SAMPLE_TEXTURE2D_NORMAL_PROPNAME_SCALE_BIAS(name, propname, scale, objSpace)
//
//  along with their samplername-specified versions
//
//  SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, samplerName)
//  SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, samplerName, scale, objSpace)
//  SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, samplerName, propname, scale, objSpace)
//
// -generic sampling macros are then provided that will use or not
//  (depending on _USE_SAMPLER_SHARING) sampler sharing: 
//
//  SHARED_SAMPLING(lvalue, swizzle, useMapProperty, name)
//  SHARED_SAMPLING_NORMAL(lvalue, useMapProperty, name, scale, objSpace)
//  SHARED_SAMPLING_NORMAL_PROPNAME(lvalue, useMapProperty, name, propname, scale, objSpace)
//
//  When sampler sharing is disabled, the above three macros map to the three macros 
//  mentionned on top (eg SHARED_SAMPLING behaves like SAMPLE_TEXTURE2D_SCALE_BIAS)
//-----------------------------------------------------------------------------
#ifndef TEXTURESAMPLERSHARING_HLSL
#define TEXTURESAMPLERSHARING_HLSL

#include "HDRP/Material/StackLit/TextureSamplerSharing.cs.hlsl"

#define SHAREDSAMPLER_BASENAME _SharedSamplerMap // dont change this, or if you do, match it with the UI and TextureSamplerSharing classes
#define SHAREDSAMPLER_TEXTURE_NAME(num) MERGE_NAME(SHAREDSAMPLER_BASENAME, num)
#define SHAREDSAMPLER_SAMPLER_NAME(num) MERGE_NAME(sampler, MERGE_NAME(SHAREDSAMPLER_BASENAME, num))

#define MAP_USES_OWN_SAMPLER(name) MERGE_NAME(name, _USES_OWN_SAMPLER)


// We won't use directly 
//
// #ifdef _USE_SAMPLER_SHARING // shader_feature
//
// to limit to SHADERPASS_FORWARD as it simplifies the dummy texture-touching macro usage and we don't really
// need sharing for these other passes, we could add back more if we want.
//
// In short, this just avoids injecting DUMMY_USE_OF_SHARED_TEXTURES() here and there on a single property
// that we know a pass that needs our samplers will use.
// It just adds an untaken branch that we could ifdef using SHADERPASS but just excluding other passes makes
// everything simpler.
//
#if ( defined(_USE_SAMPLER_SHARING) && (SHADERPASS == SHADERPASS_FORWARD) )

#define TRUE_IF_SHARED_SAMPLING(expression) (true) // otherwise the expression is passed through
#define SHARED_SAMPLING_ENABLED (true)

#define SHARED_SAMPLER_ENUM_NUM (SHAREDSAMPLERID_LAST-SHAREDSAMPLERID_FIRST+1)

#if ( !defined(SHARED_SAMPLER_USED_NUM) || (SHARED_SAMPLER_USED_NUM < 0) || (SHARED_SAMPLER_USED_NUM > SHARED_SAMPLER_ENUM_NUM) )
#undef SHARED_SAMPLER_USED_NUM
//Unfound or invalid SHARED_SAMPLER_USED_NUM definition, will use the max, 13
#define SHARED_SAMPLER_USED_NUM SHARED_SAMPLER_ENUM_NUM
#endif

//-----------------------------------------------------------------------------
// Number limited shared sampler touching:
//-----------------------------------------------------------------------------
#define DUMMY_USE_OF_SHARED_TEXTURE(num, tmp)  \
        tmp += SAMPLE_TEXTURE2D_SCALE_BIAS(SHAREDSAMPLER_TEXTURE_NAME(num))

#if (SHARED_SAMPLER_USED_NUM > 0)
    #define DUMMY_USE_OF_SHARED_TEXTURE_ZERO(tmp) DUMMY_USE_OF_SHARED_TEXTURE(0, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_ZERO(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 1)
    #define DUMMY_USE_OF_SHARED_TEXTURE_ONE(tmp) DUMMY_USE_OF_SHARED_TEXTURE(1, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_ONE(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 2)
    #define DUMMY_USE_OF_SHARED_TEXTURE_TWO(tmp) DUMMY_USE_OF_SHARED_TEXTURE(2, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_TWO(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 3)
    #define DUMMY_USE_OF_SHARED_TEXTURE_THREE(tmp) DUMMY_USE_OF_SHARED_TEXTURE(3, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_THREE(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 4)
    #define DUMMY_USE_OF_SHARED_TEXTURE_FOUR(tmp) DUMMY_USE_OF_SHARED_TEXTURE(4, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_FOUR(tmp)
#endif
//
// Not used, but could be by changing TextureSamplerSharing.cs' enum and thus SHARED_SAMPLER_ENUM_NUM :
//
#if (SHARED_SAMPLER_USED_NUM > 5)
    #define DUMMY_USE_OF_SHARED_TEXTURE_FIVE(tmp) DUMMY_USE_OF_SHARED_TEXTURE(5, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_FIVE(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 6)
    #define DUMMY_USE_OF_SHARED_TEXTURE_SIX(tmp) DUMMY_USE_OF_SHARED_TEXTURE(6, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_SIX(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 7)
    #define DUMMY_USE_OF_SHARED_TEXTURE_SEVEN(tmp) DUMMY_USE_OF_SHARED_TEXTURE(7, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_SEVEN(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 8)
    #define DUMMY_USE_OF_SHARED_TEXTURE_EIGHT(tmp) DUMMY_USE_OF_SHARED_TEXTURE(8, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_EIGHT(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 9)
    #define DUMMY_USE_OF_SHARED_TEXTURE_NINE(tmp) DUMMY_USE_OF_SHARED_TEXTURE(9, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_NINE(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 10)
    #define DUMMY_USE_OF_SHARED_TEXTURE_TEN(tmp) DUMMY_USE_OF_SHARED_TEXTURE(10, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_TEN(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 11)
    #define DUMMY_USE_OF_SHARED_TEXTURE_ELEVEN(tmp) DUMMY_USE_OF_SHARED_TEXTURE(11, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_ELEVEN(tmp)
#endif
#if (SHARED_SAMPLER_USED_NUM > 12)
    #define DUMMY_USE_OF_SHARED_TEXTURE_TWELVE(tmp) DUMMY_USE_OF_SHARED_TEXTURE(12, tmp)
#else
    #define DUMMY_USE_OF_SHARED_TEXTURE_TWELVE(tmp)
#endif


// This is only needed once using one float4 that is going to be used (touched)
// further using SHARED_SAMPLING: since the compiler can't know if lvalue will 
// be initialized or not, DUMMY_USE_SHARED_TEXTURE() will serve as the initializer,
// and Unity will bind the textures for the _SharedSamplerMap*
// Furthermore, the branch will never be taken at runtime since we know in that 
// precise case, _EnableSamplerSharing is not 0.0.
#define DUMMY_USE_OF_SHARED_TEXTURES(tmp)  \
    if (_EnableSamplerSharing == 0.0)  \
    {  \
        DUMMY_USE_OF_SHARED_TEXTURE_ZERO(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_ONE(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_TWO(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_THREE(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_FOUR(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_FIVE(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_SIX(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_SEVEN(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_EIGHT(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_NINE(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_TEN(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_ELEVEN(tmp);  \
        DUMMY_USE_OF_SHARED_TEXTURE_TWELVE(tmp);  \
    }  \


//-----------------------------------------------------------------------------
// Number limited SHARED_SAMPLING cases / switches :
//-----------------------------------------------------------------------------

// Generic cases:
#define SHARED_SAMPLING_CASE(num, lvalue, swizzle, useMapProperty, name)  \
        case (SHAREDSAMPLERID_FIRST+num):  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(num)).##swizzle;  \
            break

#define SHARED_SAMPLING_NORMAL_CASE(num, lvalue, useMapProperty, name, scale, objSpace)  \
        case (SHAREDSAMPLERID_FIRST+num):  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(num), scale, objSpace);  \
            break

#define SHARED_SAMPLING_NORMAL_PROPNAME_CASE(num, lvalue, useMapProperty, name, propname, scale, objSpace)  \
        case (SHAREDSAMPLERID_FIRST+num):  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(num), propname, scale, objSpace);  \
            break


// Specific cases:

#if (SHARED_SAMPLER_USED_NUM > 0)
    #define SHARED_SAMPLING_CASE_ZERO(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(0, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_ZERO(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(0, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ZERO(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(0, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_ZERO(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_ZERO(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ZERO(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 1)
    #define SHARED_SAMPLING_CASE_ONE(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(1, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_ONE(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(1, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ONE(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(1, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_ONE(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_ONE(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ONE(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 2)
    #define SHARED_SAMPLING_CASE_TWO(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(2, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_TWO(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(2, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TWO(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(2, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_TWO(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_TWO(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TWO(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 3)
    #define SHARED_SAMPLING_CASE_THREE(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(3, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_THREE(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(3, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_THREE(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(3, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_THREE(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_THREE(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_THREE(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 4)
    #define SHARED_SAMPLING_CASE_FOUR(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(4, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_FOUR(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(4, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_FOUR(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(4, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_FOUR(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_FOUR(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_FOUR(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
//
// Not used, but could be by changing TextureSamplerSharing.cs' enum and thus SHARED_SAMPLER_ENUM_NUM :
//
#if (SHARED_SAMPLER_USED_NUM > 5)
    #define SHARED_SAMPLING_CASE_FIVE(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(5, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_FIVE(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(5, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_FIVE(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(5, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_FIVE(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_FIVE(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_FIVE(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 6)
    #define SHARED_SAMPLING_CASE_SIX(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(6, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_SIX(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(6, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_SIX(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(6, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_SIX(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_SIX(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_SIX(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 7)
    #define SHARED_SAMPLING_CASE_SEVEN(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(7, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_SEVEN(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(7, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_SEVEN(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(7, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_SEVEN(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_SEVEN(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_SEVEN(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 8)
    #define SHARED_SAMPLING_CASE_EIGHT(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(8, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_EIGHT(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(8, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_EIGHT(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(8, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_EIGHT(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_EIGHT(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_EIGHT(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 9)
    #define SHARED_SAMPLING_CASE_NINE(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(9, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_NINE(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(9, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_NINE(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(9, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_NINE(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_NINE(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_NINE(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 10)
    #define SHARED_SAMPLING_CASE_TEN(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(10, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_TEN(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(10, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TEN(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(10, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_TEN(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_TEN(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TEN(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 11)
    #define SHARED_SAMPLING_CASE_ELEVEN(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(11, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_ELEVEN(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(11, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ELEVEN(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(11, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_ELEVEN(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_ELEVEN(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ELEVEN(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif
#if (SHARED_SAMPLER_USED_NUM > 12)
    #define SHARED_SAMPLING_CASE_TWELVE(lvalue, swizzle, useMapProperty, name) SHARED_SAMPLING_CASE(12, lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_TWELVE(lvalue, useMapProperty, name, scale, objSpace) SHARED_SAMPLING_NORMAL_CASE(12, lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TWELVE(lvalue, useMapProperty, name, propname, scale, objSpace) SHARED_SAMPLING_NORMAL_PROPNAME_CASE(12, lvalue, useMapProperty, name, propname, scale, objSpace)
#else
    #define SHARED_SAMPLING_CASE_TWELVE(lvalue, swizzle, useMapProperty, name)
    #define SHARED_SAMPLING_NORMAL_CASE_TWELVE(lvalue, useMapProperty, name, scale, objSpace)
    #define SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TWELVE(lvalue, useMapProperty, name, propname, scale, objSpace)
#endif


#define SHARED_SAMPLING(lvalue, swizzle, useMapProperty, name)  \
    uint useMap = (uint) useMapProperty;  \
    if (MAP_USES_OWN_SAMPLER(name))  \
    /* make sure this condition is a define, always known, so compiler can prune this on a */  \
    /* case by case basis or else we defeat the purpose of the sampler sharing system!     */  \
    {  \
        lvalue.##swizzle = SAMPLE_TEXTURE2D_SCALE_BIAS(name).##swizzle;  \
    }  \
    else  \
    {  \
        /* [forcecase] */  \
        switch (useMap)  \
        {  \
        case EXTERNALEXISTINGSAMPLER_LINEAR_CLAMP:  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, s_linear_clamp_sampler).##swizzle;  \
            break;  \
        case EXTERNALEXISTINGSAMPLER_LINEAR_REPEAT:  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, s_linear_repeat_sampler).##swizzle;  \
            break;  \
        SHARED_SAMPLING_CASE_ZERO(lvalue,  swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_ONE(lvalue,   swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_TWO(lvalue,   swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_THREE(lvalue, swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_FOUR(lvalue,  swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_FIVE(lvalue,  swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_SIX(lvalue,   swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_SEVEN(lvalue, swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_EIGHT(lvalue, swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_NINE(lvalue,  swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_TEN(lvalue,   swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_ELEVEN(lvalue, swizzle, useMapProperty, name);  \
        SHARED_SAMPLING_CASE_TWELVE(lvalue, swizzle, useMapProperty, name);  \
        }  \
    }  \


#define SHARED_SAMPLING_NORMAL(lvalue, useMapProperty, name, scale, objSpace)  \
    uint useMap = (uint) useMapProperty;  \
    if (MAP_USES_OWN_SAMPLER(name))  \
    /* make sure this condition is a define, always known, so compiler can prune this on a */  \
    /* case by case basis or else we defeat the purpose of the sampler sharing system!     */  \
    {  \
        lvalue = SAMPLE_TEXTURE2D_NORMAL_SCALE_BIAS(name, scale, objSpace);  \
    }  \
    else  \
    {  \
        /* [forcecase] */  \
        switch (useMap)  \
        {  \
        case EXTERNALEXISTINGSAMPLER_LINEAR_CLAMP:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, s_linear_clamp_sampler, scale, objSpace);  \
            break;  \
        case EXTERNALEXISTINGSAMPLER_LINEAR_REPEAT:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, s_linear_repeat_sampler, scale, objSpace);  \
            break;  \
        SHARED_SAMPLING_NORMAL_CASE_ZERO(lvalue,   useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_ONE(lvalue,    useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_TWO(lvalue,    useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_THREE(lvalue,  useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_FOUR(lvalue,   useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_FIVE(lvalue,   useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_SIX(lvalue,    useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_SEVEN(lvalue,  useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_EIGHT(lvalue,  useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_NINE(lvalue,   useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_TEN(lvalue,    useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_ELEVEN(lvalue, useMapProperty, name, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_CASE_TWELVE(lvalue, useMapProperty, name, scale, objSpace);  \
        }  \
    }  \

#define SHARED_SAMPLING_NORMAL_PROPNAME(lvalue, useMapProperty, name, propname, scale, objSpace)  \
    uint useMap = (uint) useMapProperty;  \
    if (MAP_USES_OWN_SAMPLER(name))  \
    /* make sure this condition is a define, always known, so compiler can prune this on a */  \
    /* case by case basis or else we defeat the purpose of the sampler sharing system!     */  \
    {  \
        lvalue = SAMPLE_TEXTURE2D_NORMAL_PROPNAME_SCALE_BIAS(name, propname, scale, objSpace);  \
    }  \
    else  \
    {  \
        /* [forcecase] */  \
        switch (useMap)  \
        {  \
        case EXTERNALEXISTINGSAMPLER_LINEAR_CLAMP:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, s_linear_clamp_sampler, propname, scale, objSpace);  \
            break;  \
        case EXTERNALEXISTINGSAMPLER_LINEAR_REPEAT:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, s_linear_repeat_sampler, propname, scale, objSpace);  \
            break;  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ZERO(lvalue,    useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ONE(lvalue,     useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TWO(lvalue,     useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_THREE(lvalue,   useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_FOUR(lvalue,    useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_FIVE(lvalue,    useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_SIX(lvalue,     useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_SEVEN(lvalue,   useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_EIGHT(lvalue,   useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_NINE(lvalue,    useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TEN(lvalue,     useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_ELEVEN(lvalue,  useMapProperty, name, propname, scale, objSpace);  \
        SHARED_SAMPLING_NORMAL_PROPNAME_CASE_TWELVE(lvalue,  useMapProperty, name, propname, scale, objSpace);  \
        }  \
    }  \

#else // _USE_SAMPLER_SHARING

#define TRUE_IF_SHARED_SAMPLING(expression) (expression)
#define SHARED_SAMPLING_ENABLED (false)

#define DUMMY_USE_OF_SHARED_TEXTURES(tmp)


#define SHARED_SAMPLING(lvalue, swizzle, useMapProperty, name)  \
    if (useMapProperty)  \
    {  \
        lvalue.##swizzle = SAMPLE_TEXTURE2D_SCALE_BIAS(name).##swizzle;  \
    }  \

#define SHARED_SAMPLING_NORMAL(lvalue, useMapProperty, name, scale, objSpace)  \
    if (useMapProperty)  \
    {  \
        lvalue = SAMPLE_TEXTURE2D_NORMAL_SCALE_BIAS(name, scale, objSpace);  \
    }  \

#define SHARED_SAMPLING_NORMAL_PROPNAME(lvalue, useMapProperty, name, propname, scale, objSpace)  \
    if (useMapProperty)  \
    {  \
        lvalue = SAMPLE_TEXTURE2D_NORMAL_PROPNAME_SCALE_BIAS(name, propname, scale, objSpace);  \
    }  \

#endif // _USE_SAMPLER_SHARING

#endif // TEXTURESAMPLERSHARING_HLSL