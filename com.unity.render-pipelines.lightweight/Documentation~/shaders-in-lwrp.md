# Shaders in LWRP

The Lightweight Render Pipeline provides the following shaders for the most common use case scenarios:

- [Lit](Lit-shader.md)
- [Simple Lit](Simple-Lit-shader.md)
- [Unlit](Unlit-shader.md)
- Particles Lit
- Particles Simple Lit
- Particles Unlit
- Autodesk Interactive 
- Autodesk Interactive Transparent 
- Autodesk Interactive Masked 

**Upgrade advice:** The Lightweight Render Pipeline uses a different shading approach than the Unity built-in Render Pipeline. As a result, built-in Lit and custom Lit shaders do not work with the LWRP. Instead, LWRP has a new set of standard shaders. If you upgrade your current Project to LWRP, you can [upgrade](upgrading-your-shaders.md) built-in shaders to the new ones. Unlit shaders from the built-in render pipeline still work with LWRP.

## Choosing a shader 

With the Lightweight Render Pipeline, you can chose between Physically Based Rendering (PBR) and non-PBR rendering. 

For PBR, use the [[Lit shader | lit_shader#lit_shader]]. You can use it on all platforms. The shader quality scales, depending on the platform, but keeps physically based rendering on all platforms. This gives you realistic graphics across hardware. The Unity [Standard Shader](<https://docs.unity3d.com/Manual/shader-StandardShader.html>) and the [Standard (Specular setup)](https://docs.unity3d.com/Manual/StandardShaderMetallicVsSpecular.html) shaders both map to the Lit shader in LWRP. For a list of shader mappings, see [shader mappings](upgrading-your-shaders.md#shaderMappings)

If you’re targeting less powerful devices, or just would like simpler shading, use the [[Simple lit shader | simple_lit_shader#simple_lit_shader]], which is non-PBR. 

If you don’t need light sources, or would rather use baked lighting and sample global illumination, choose an unlit shader.