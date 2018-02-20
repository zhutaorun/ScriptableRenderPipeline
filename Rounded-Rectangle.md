## Description

Generates a rounded rectangle shape based on input **UV** at the size specified by inputs **Width** and **Height**. The radius of each corner is defined by input **Radius**. The generated shape can be offset or tiled by connecting a [Tiling And Offset Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Tiling-And-Offset-Node). Note that in order to preserve the ability to offset the shape within the UV space the shape will not automatically repeat if tiled. To achieve a repeating rounded rectangle effect first connect your input through a [Fraction Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Fraction-Node).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Width      | Input | Vector 1 | None | Rounded Rectangle width |
| Height      | Input | Vector 1 | None | Rounded Rectangle height |
| Radius      | Input | Vector 1 | None | Corner radius |
| Out | Output      |    Vector 1 | None | Output value |

## Shader Function

```
Radius = min(abs(Radius), 0.5 * min(abs(Width), abs(Height)));
float2 XMinAndMax = {precision}2(0.5 - abs(Width) / 2, 0.5 + abs(Width) / 2);
float2 YMinAndMax = {precision}2(0.5 - abs(Height) / 2, 0.5 + abs(Height) / 2);
float wide = (step( XMinAndMax.x, UV.x ) - step( XMinAndMax.y, UV.x )) * (step( YMinAndMax.x + Radius, UV.y ) - step( YMinAndMax.y - Radius, UV.y ));
float tall = (step( XMinAndMax.x + Radius, UV.x ) - step( XMinAndMax.y - Radius, UV.x )) * (step( YMinAndMax.x, UV.y ) - step( YMinAndMax.y, UV.y ));
float sw = step(length(UV - {precision}2(XMinAndMax.x + Radius, YMinAndMax.x + Radius)), Radius);
float se = step(length(UV - {precision}2(XMinAndMax.y - Radius, YMinAndMax.x + Radius)), Radius);
float nw = step(length(UV - {precision}2(XMinAndMax.x + Radius, YMinAndMax.y - Radius)), Radius);
float ne = step(length(UV - {precision}2(XMinAndMax.y - Radius, YMinAndMax.y - Radius)), Radius);
Out = saturate(wide + tall + sw + se + nw + ne);
```