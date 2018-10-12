# Shaders in LWRP

The Lightweight Render Pipeline comes with a set of shaders, listed below. You can use these shaders for most common use case scenarios. To read more about each shader, click the name of it.

- [Lit](Lit-shader.md)
- [Simple Lit](Simple-Lit-shader.md)
- [Unlit](Unlit-shader.md)
- Particles Lit
- Particles Simple Lit
- Particles Unlit
- Autodesk Interactive 
- Autodesk Interactive Transparent 
- Autodesk Interactive Masked 

**Note:** The Lightweight Render Pipeline has a different shading approach than the Unity Built-in Render Pipeline. As a result, Built-in Lit and custom Lit shaders do not work with the LWRP. Instead, LWRP has a new set of standard shaders. If you upgrade a current Project to LWRP, you can [upgrade](upgrading-your-shaders.md) built-in shaders to the new ones. Unlit shaders from the Built-in render pipeline still work with LWRP.

## Choosing the shader for your needs

With the Lightweight Render Pipeline, you can chose between Physically Based Rendering (PBR) and non-PBR rendering. 

For PBR, use the [[Lit shader | lit_shader#lit_shader]] is PBR based. You can use it on all platforms. The shader quality scales depending on the platform. Because the LWRP has a set calculation for all tiers, the Lit shader keeps physically based rendering on mobile platforms as well. This gives you realistic graphics across hardware. The Unity [Standard Shader](<https://docs.unity3d.com/Manual/shader-StandardShader.html>) and the Standard (Specular setup) shaders both map to the [Lit] shader in LWRP.

If you’re targeting less powerful devices, or just would like simpler shading, use the [[Simple lit shader | simple_lit_shader#simple_lit_shader]], which is non-PBR. 

If you don’t need light sources, br would rather use baked lighting and sample global illumination, choose an unlit shader.