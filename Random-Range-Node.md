## Description

Returns a pseudo-random number value based on input *Seed* that is between the minimum and maximum values defined by inputs *Min* and *Max* respectively.

Whilst the same value in input *Seed* will always result in the same output value, the output value itself will appear random. Input *Seed* is a Vector 2 value for the convenience of generating a random number based on a UV input, however for most cases a Vector 1 input will suffice.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Seed      | Input | Dynamic Vector | Seed value used for generation |
| Min      | Input | Dynamic Vector | Minimum value |
| Max      | Input | Dynamic Vector | Maximum value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

```
float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453)
Out = lerp(Min, Max, randomno)
```