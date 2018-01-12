![](https://raw.githubusercontent.com/Kink3d/WikiTest/master/Images/AddNodePage.png?token=AKnrSa7IGhNcOvd4623PyrU5FQuM5iNLks5aV3LnwA%3D%3D)

## Description

Returns the length of input In. This is also known as magnitude. A vector's length is calculated with <a href=https://en.wikipedia.org/wiki/Pythagorean_theorem>Pythagorean Theorum</a>.

## Example

The length of a vector 2 can be calculated as:

![](https://github.com/Unity-Technologies/ShaderGraph/wiki/Images/NodeLibrary/Nodes/PageImages/LengthNodePage02.png)

Where `x` and `y` are the x and y components of the input vector. Similarly the length of a vector 3 can be calculated as:

![](https://github.com/Unity-Technologies/ShaderGraph/wiki/Images/NodeLibrary/Nodes/PageImages/LengthNodePage03.png)

And so on.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |   Vector 1 | Output value |

## Shader Function

`Out = length(In)`