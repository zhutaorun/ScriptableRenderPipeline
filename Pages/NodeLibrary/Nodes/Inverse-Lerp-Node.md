## Description

Returns the linear parameter that produces the interpolant specified by input *T* within the range of input *A* to input *B*.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| T      | Input | Dynamic Vector | Time value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = (T - A)/(B - A)`