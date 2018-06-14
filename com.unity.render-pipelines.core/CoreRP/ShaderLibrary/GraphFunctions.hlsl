#ifndef UNITY_GRAPHFUNCTIONS_INCLUDED
#define UNITY_GRAPHFUNCTIONS_INCLUDED

// ----------------------------------------------------------------------------
// Included in generated graph shaders
// ----------------------------------------------------------------------------

struct Gradient
{
    int type;
    int colorsLength;
    int alphasLength;
    float4 colors[8];
    float2 alphas[8];
};

#ifndef SHADERGRAPH_SAMPLE_SCENE_DEPTH
    #define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_SampleSceneDepth(uv);
#endif

#ifndef SHADERGRAPH_SAMPLE_SCENE_COLOR
    #define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_SampleSceneColor(uv);
#endif

#ifndef SHADERGRAPH_SAMPLE_LIGHTMAP
    #define SHADERGRAPH_SAMPLE_LIGHTMAP(uv) shadergraph_SampleLightmap(uv);
#endif

#ifndef SHADERGRAPH_SAMPLE_SHADOWMASK
    #define SHADERGRAPH_SAMPLE_SHADOWMASK(uv) shadergraph_SampleShadowmask(uv);
#endif

float shadergraph_SampleSceneDepth(float2 uv)
{
    return 1;
}

float shadergraph_SampleSceneColor(float2 uv)
{
    return 0;
}

float shadergraph_SampleLightmap(float2 uv)
{
    return SAMPLE_TEXTURE2D(unity_Lightmap, samplerunity_Lightmap, uv);
}

float shadergraph_SampleShadowmask(float2 uv)
{
    return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_Lightmap, uv);
}

#endif // UNITY_GRAPHFUNCTIONS_INCLUDED
