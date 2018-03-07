## Description

Some input [Ports](https://github.com/Unity-Technologies/ShaderGraph/wiki/Port) might have **Port Bindings**. This means there is an expectation of the data that should be supplied to the [Port](https://github.com/Unity-Technologies/ShaderGraph/wiki/Port), such as a **Normal Vector** or **UV**. However, a **Port Binding** only affects a [Port](https://github.com/Unity-Technologies/ShaderGraph/wiki/Port) that does not have a connected [Edge](https://github.com/Unity-Technologies/ShaderGraph/wiki/Edge). These [Ports](https://github.com/Unity-Technologies/ShaderGraph/wiki/Port) still have a regular [Data Type](https://github.com/Unity-Technologies/ShaderGraph/wiki/Data-Types) that define what [Edges](https://github.com/Unity-Technologies/ShaderGraph/wiki/Edge) can be connected to them.

In practice this means that if no [Edge](https://github.com/Unity-Technologies/ShaderGraph/wiki/Edge) is connected to the [Port](https://github.com/Unity-Technologies/ShaderGraph/wiki/Port) the default data used in that port will be taken from its **Port Binding**. A full list of **Port Bindings** and their associated default options is found below.

## Port Bindings List

