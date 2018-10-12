# Upgrading your shaders

You cannot use shaders from the built-in render pipeline in the Lightweight Render Pipeline. Instead, you can upgrade the built-in shaders to the ones supplied with LWRP. For an overview of the correspondence between built-in shaders and LWRP shaders, see [Shader mapping](##Shader mapping).

To upgrade built-in shaders:

1. Open your Project in Unity. Go to __Edit__ > __Render Pipeline__. 
2. According to your needs, select either __Upgrade Project Materials to Lightweight RP Materials__ or __Upgrade Scene Materials to Lightweight RP Materials__.

**Note:** These changes cannot be unmade. Perform a backup of your Project before you upgrade them.

## Shader mapping

In the table below, you can see which LWRP shaders the Unity built-in shaders convert to.

| Unity built-in shader                             | Lightweight Render Pipeline shader |
| ------------------------------------------------- | ---------------------------------- |
| Standard                                          | Lit                                |
| Standard (Specular Setup)                         | Lit                                |
| Standard Terrain                                  | TerrainLit                         |
| Particles/Standard Surface                        | ParticlesLit                       |
| Particles/Standard Unlit                          | ParticlesUnlit                     |
| Mobile/Diffuse                                    | Simple Lit                         |
| Mobile/Bumped Specular                            | Simple Lit                         |
| Mobile/Bumped Specular(1 Directional Light)       | Simple Lit                         |
| Mobile/Unlit (Supports Lightmap)                  | Simple Lit                         |
| Mobile/VertexLit                                  | Simple Lit                         |
| Legacy Shaders/Diffuse                            | Simple Lit                         |
| Legacy Shaders/Specular                           | Simple Lit                         |
| Legacy Shaders/Bumped Diffuse                     | Simple Lit                         |
| Legacy Shaders/Bumped Specular                    | Simple Lit                         |
| Legacy Shaders/Self-Illumin/Diffuse               | Simple Lit                         |
| Legacy Shaders/Self-Illumin/Bumped Diffuse        | Simple Lit                         |
| Legacy Shaders/Self-Illumin/Specular              | Simple Lit                         |
| Legacy Shaders/Self-Illumin/Bumped Specular       | Simple Lit                         |
| Legacy Shaders/Transparent/Diffuse                | Simple Lit                         |
| Legacy Shaders/Transparent/Specular               | Simple Lit                         |
| Legacy Shaders/Transparent/Bumped Diffuse         | Simple Lit                         |
| Legacy Shaders/Transparent/Bumped Specular        | Simple Lit                         |
| Legacy Shaders/Transparent/Cutout/Diffuse         | Simple Lit                         |
| Legacy Shaders/Transparent/Cutout/Specular        | Simple Lit                         |
| Legacy Shaders/Transparent/Cutout/Bumped Diffuse  | Simple Lit                         |
| Legacy Shaders/Transparent/Cutout/Bumped Specular | Simple Lit                         |