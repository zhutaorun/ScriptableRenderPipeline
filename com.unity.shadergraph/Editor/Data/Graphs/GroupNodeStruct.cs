using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public struct GroupNodeStruct
    {
        public Guid nodeGuid;
        public Guid oldGroupGuid;
        public Guid newGroupGuid;
    }
}

