# Baked GI Node

## Description

Provides access to the **Baked GI** values at the vertex or fragment's position. Requires **Position** and **Normal** input for light probe sampling, and lightmap coordinates **Static UV** and **Dynamic UV** for all potential lightmap sampling cases.

Note: The behaviour of this [Node](Node.md) is undefined globally. The executed HLSL code for this [Node](Node.md) is defined per **Render Pipeline**, and different **Render Pipelines** may produce different results. Custom **Render Pipelines** that wish to support this [Node](Node.md) will also need to explicitly define the behaviour for it. If undefined this [Node](Node.md) will return 0 (black).

#### Unity Pipelines Supported
- HD Render Pipeline
- Lightweight Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position    | Input | Vector 3 | Position (world space) | Mesh vertex/fragment's **Position** |
| Normal      | Input | Vector 3 | Normal (world space) | Mesh vertex/fragment's **Normal** |
| Static UV   | Input | Vector 2 | UV1 | Lightmap coordinates for the static lightmap |
| Dynamic UV  | Input | Vector 2 | UV2 | Lightmap coordinates for the dynamic lightmap |
| Out       | Output | Vector 3 | None | Output color value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Apply Lightmap Scaling     | Toggle | True, False | If enabled lightmaps are automatically scaled and offset. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_BakedGI_float(float3 Position, float3 Normal, float2 StaticUV, float2 DynamicUV, out float Out)
{
    Out = SHADERGRAPH_BAKED_GI(Position, Normal, StaticUV, DynamicUV, false);
}
```