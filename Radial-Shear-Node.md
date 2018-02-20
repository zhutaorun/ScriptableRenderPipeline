## Description

Under Construction...

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Center      | Input | Vector 2 | None | Center reference point |
| Strength      | Input | Vector 1 | None | Strength of the effect |
| Offset      | Input | Vector 2 | None | Individual channel offsets |
| Out | Output      |    Vector 2 | None | Output UV value |

## Shader Function

```
float2 delta = UV - Center;
float delta2 = dot(delta.xy, delta.xy);
float2 delta_offset = delta2 * Strength;
Out = UV + float2(delta.y, -delta.x) * delta_offset + Offset;
```