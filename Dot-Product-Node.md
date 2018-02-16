## Description

Returns the dot product, or scalar product, of the values of the inputs **A** and **B**. This is the product of the magnitudes, or lengths, of the two vectors and the cosine of the angle between them. This is useful for calculating the angle between two vectors and is commonly used in lighting calculations. 

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Vector 1 | Output value |

## Shader Function

`Out = dot(A, B)`