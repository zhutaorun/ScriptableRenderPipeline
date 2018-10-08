# Fog Node

## Description

Provides access to the Scene's **Fog** parameters.

Note: The behaviour of this [Node](Node.md) is undefined globally. The executed HLSL code for this [Node](Node.md) is defined per **Render Pipeline**, and different **Render Pipelines** may produce different results. Custom **Render Pipelines** that wish to support this [Node](Node.md) will also need to explicitly define the behaviour for it. If undefined this [Node](Node.md) will return 0 (black).

#### Unity Pipelines Supported
- Lightweight Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position      | Output | Vector 3 | Position (object space) | Mesh vertex/fragment's position |
| Color      | Output | Vector 4 | None | Fog color |
| Density       | Output | Vector 1 | None | Fog density at the vertex or fragment's clip space depth |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Fog_float(float3 Position, out float4 Color, out float Density)
{
    SHADERGRAPH_FOG(Position, Color, Density);
}
```
