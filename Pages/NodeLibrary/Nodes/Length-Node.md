![](https://raw.githubusercontent.com/Kink3d/WikiTest/master/Images/AddNodePage.png?token=AKnrSa7IGhNcOvd4623PyrU5FQuM5iNLks5aV3LnwA%3D%3D)

## Description

Returns the length of input In. This is also known as magnitude. A vector's length is calculated with Pythagorean Formula.

## Example

The length of a vector 2 can be calculated as:
`(x ^ 2) * (y ^ 2)

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Base      | Dropdown | BaseE, Base2 | Selects the exponential base |

## Shader Function

**Base E**

`Out = exp(In)`

**Base 2**

`Out = exp2(In)`