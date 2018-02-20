## Description

Generates a rectangle shape based on input **UV** at the size specified by inputs **Width** and **Height**. The generated shape can be offset or tiled by connecting a [Tiling And Offset Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Tiling-And-Offset-Node). Note that in order to preserve the ability to offset the shape within the UV space the shape will not automatically repeat if tiled. To achieve a repeating rectangle effect first connect your input through a [Fraction Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Fraction-Node).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Width      | Input | Vector 1 | None | Rectangle width |
| Height      | Input | Vector 1 | None | Rectangle height |
| Out | Output      |    Vector 1 | None | Output value |

## Shader Function

```
float2 XMinAndMax = float2(0.5 - Width / 2, 0.5 + Width / 2);
float2 YMinAndMax = float2(0.5 - Height / 2, 0.5 + Height / 2);
float x = step( XMinAndMax.x, UV.x ) - step( XMinAndMax.y, UV.x );
float y = step( YMinAndMax.x, UV.y ) - step( YMinAndMax.y, UV.y );
Out = x * y;
```