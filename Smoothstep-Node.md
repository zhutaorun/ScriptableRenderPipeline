## Description

Returns the result of interpolating between input *A* and input *B* by input *T*. The value of input *T* is clamped to the range of 0 to 1. 

This node is similar to the [Lerp Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Lerp-Node) except it uses Smooth Hermite Interpolation instead of Linear Interpolation. This means the interpolation will gradually speed up from the start and slow down toward the end. This is useful for creating natural-looking animation, fading and other transitions.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| T      | Input | Dynamic Vector | Time value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = smoothstep(A, B, T)`