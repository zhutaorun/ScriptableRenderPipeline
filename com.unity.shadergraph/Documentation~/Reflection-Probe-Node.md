# Reflection Probe Node

## Description

Provides access to the nearest **Reflection Probe** to the object. Requires **Normal** and **View Direction** to sample the probe. You can achieve a blurring effect by sampling at a different Level of Detail using the **LOD** input.

Note: The behaviour of this [Node](Node.md) is undefined globally. The executed HLSL code for this [Node](Node.md) is defined per **Render Pipeline**, and different **Render Pipelines** may produce different results. Custom **Render Pipelines** that wish to support this [Node](Node.md) will also need to explicitly define the behaviour for it. If undefined this [Node](Node.md) will return 0 (black).

#### Unity Pipelines Supported
- Lightweight Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| View Dir      | Input | Vector 3 | View Direction (object space) | Mesh's view direction |
| Normal | Input      |    Vector 3 | Normal (object space) | Mesh's normal vector |
| LOD | Input      |    Vector 1 | None | Level of detail for sampling |
| Out | Output      |    Vector 3 | None | Output color value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
{
    Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
}
```
