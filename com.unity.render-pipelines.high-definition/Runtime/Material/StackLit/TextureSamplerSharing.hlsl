//-----------------------------------------------------------------------------
// Texture Mapping: Sampler Sharing
//
// -.shader properties and shader uniforms should have _SharedSamplerMap0... available
// -shader should have shader_feature _USE_SAMPLER_SHARING
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

#define SHAREDSAMPLER_BASENAME _SharedSamplerMap // dont change this, or if you do, search and replace below
#define SHAREDSAMPLER_TEXTURE_NAME(basename, num) (basename##num)
#define SHAREDSAMPLER_SAMPLER_NAME(basename, num) (sampler##basename##num)

#ifdef _USE_SAMPLER_SHARING // shader_feature

#define TRUE_IF_SHARED_SAMPLING(expression) (true) // otherwise the expression is passed through
#define SHARED_SAMPLING_ENABLED (true)

// This is only needed once using one float4 that is going to be used (touched)
// further using SHARED_SAMPLING: since the compiler can't know if lvalue will 
// be initialized or not, DUMMY_USE_SHARED_TEXTURE() will serve as the initializer,
// and Unity will bind the textures for the _SharedSamplerMap*
// Furthermore, the branch will never be taken at runtime since we know in that 
// precise case, _EnableSamplerSharing is not 0.0.
#define DUMMY_USE_OF_SHARED_TEXTURES(tmp)  \
    if (_EnableSamplerSharing == 0.0)  \
    {  \
        /* These don't work unfortunately, preprocessor not good enough for multiple macro expansion, */  \
        /* converting eg the string "_SharedSamplerMap0" further to sampler##_SharedSamplerMap0 for the second macro: */  \
        /* tmp += SAMPLE_TEXTURE2D_SCALE_BIAS(SHAREDSAMPLER_TEXTURE_NAME(SHAREDSAMPLER_BASENAME, 0));*/  \
        tmp += SAMPLE_TEXTURE2D_SCALE_BIAS(_SharedSamplerMap0);  \
        tmp += SAMPLE_TEXTURE2D_SCALE_BIAS(_SharedSamplerMap1);  \
        tmp += SAMPLE_TEXTURE2D_SCALE_BIAS(_SharedSamplerMap2);  \
        tmp += SAMPLE_TEXTURE2D_SCALE_BIAS(_SharedSamplerMap3);  \
        tmp += SAMPLE_TEXTURE2D_SCALE_BIAS(_SharedSamplerMap4);  \
    }  \

#define SHARED_SAMPLING(lvalue, swizzle, useMapProperty, name)  \
    uint useMap = (uint) useMapProperty;  \
    /* if (useMap >= SHAREDSAMPLERID_ZERO) */  \
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
        case SHAREDSAMPLERID_ZERO:  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 0)).##swizzle;  \
            break;  \
        case SHAREDSAMPLERID_ONE:  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 1)).##swizzle;  \
            break;  \
        case SHAREDSAMPLERID_TWO:  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 2)).##swizzle;  \
            break;  \
        case SHAREDSAMPLERID_THREE:  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 3)).##swizzle;  \
            break;  \
        case SHAREDSAMPLERID_FOUR:  \
            lvalue.##swizzle = SAMPLE_TEXTURE2D_SMP_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 4)).##swizzle;  \
            break;  \
        }  \
    }  \


#define SHARED_SAMPLING_NORMAL(lvalue, useMapProperty, name, scale, objSpace)  \
    uint useMap = (uint) useMapProperty;  \
    /* if (useMap >= SHAREDSAMPLERID_ZERO) */  \
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
        case SHAREDSAMPLERID_ZERO:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 0), scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_ONE:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 1), scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_TWO:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 2), scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_THREE:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 3), scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_FOUR:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 4), scale, objSpace);  \
            break;  \
        }  \
    }  \

#define SHARED_SAMPLING_NORMAL_PROPNAME(lvalue, useMapProperty, name, propname, scale, objSpace)  \
    uint useMap = (uint) useMapProperty;  \
    /* if (useMap >= SHAREDSAMPLERID_ZERO) */  \
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
        case SHAREDSAMPLERID_ZERO:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 0), propname, scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_ONE:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 1), propname, scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_TWO:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 2), propname, scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_THREE:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 3), propname, scale, objSpace);  \
            break;  \
        case SHAREDSAMPLERID_FOUR:  \
            lvalue = SAMPLE_TEXTURE2D_SMP_NORMAL_PROPNAME_SCALE_BIAS(name, SHAREDSAMPLER_SAMPLER_NAME(SHAREDSAMPLER_BASENAME, 4), propname, scale, objSpace);  \
            break;  \
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