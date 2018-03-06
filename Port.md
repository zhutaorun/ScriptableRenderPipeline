## Description

A **Port** defines an input or output on a [Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Node). Connecting [Edges](https://github.com/Unity-Technologies/ShaderGraph/wiki/Edge) to a port allows data to flow through the [Shader Graph](https://github.com/Unity-Technologies/ShaderGraph/wiki/Shader-Graph) node network.

Each port has a [Data Type](https://github.com/Unity-Technologies/ShaderGraph/wiki/Data-Types) which defines what edges can be connected to it. Each [Data Type](https://github.com/Unity-Technologies/ShaderGraph/wiki/Data-Types) has an associated color for identifying its type.

You can open a contextual [Create Node Menu](https://github.com/Unity-Technologies/ShaderGraph/wiki/Create-Node-Menu) by dragging an [Edge](https://github.com/Unity-Technologies/ShaderGraph/wiki/Edge) from a **Port** with left mouse button.

### Default Inputs

Each **Input Port**, a **Port** on the left side of a [Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Node) implying that it is for inputting data into the [Node](https://github.com/Unity-Technologies/ShaderGraph/wiki/Node), has a **Default Input**. This appears as a small field connected to the **Port** when there is no [Edge](https://github.com/Unity-Technologies/ShaderGraph/wiki/Edge) connected. This field will display an input for the ports [Data Type](https://github.com/Unity-Technologies/ShaderGraph/wiki/Data-Types) unless the **Port** has a [Port Binding](https://github.com/Unity-Technologies/ShaderGraph/wiki/Port-Bindings).