## Description

Returns the cross product of the values of the inputs **A** and **B**. This is a vector that is perpendicular, or orthogonal, to both input vectors, with a direction given by the right-hand rule and a magnitude equal to the area of the parallelogram that the two input vectors span.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Vector 3 | First input value |
| B      | Input | Vector 3 | Second input value |
| Out | Output      |    Vector 3 | Output value |

## Shader Function

`Out = cross(A, B)`