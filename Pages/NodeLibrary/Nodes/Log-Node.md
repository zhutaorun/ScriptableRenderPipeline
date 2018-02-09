## Description

Returns the logarithm of input **In**. Log is the inverse operation to Exponential. 

For example, the base-2 exponential value of 3 is 8.

![](https://github.com/Unity-Technologies/ShaderGraph/wiki/Images/NodeLibrary/Nodes/PageImages/LogNodePage02.png)

Therefore the base-2 logarithmic value of 8 is 3.

The logarithmic base can be switched between base-e, base-2 and base-10 from the **Base** dropdown on the node. 

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Base      | Dropdown | BaseE, Base2, Base10 | Selects the logarithmic base |

## Shader Function

**Base E**

`Out = log(In)`

**Base 2**

`Out = log2(In)`

**Base 10**

`Out = log10(In)`