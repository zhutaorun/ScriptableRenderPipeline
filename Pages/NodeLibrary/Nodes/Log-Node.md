![](https://raw.githubusercontent.com/Kink3d/WikiTest/master/Images/AddNodePage.png?token=AKnrSa7IGhNcOvd4623PyrU5FQuM5iNLks5aV3LnwA%3D%3D)

## Description

Returns the logarithm of input In. Log is the inverse operation to Exponential. The logarithmic base can be switched between base-e, base-2 and base-10 from a dropdown on the node. 

## Example

The base-2 exponential value of 3 is 8

`2^3 = 2 * 2 * 2 = 8`

Therefore the base-2 logarithmic value of 8 is 3

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