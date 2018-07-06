using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    public class MaterialGraphGroup : Group
    {
        public MaterialGraphGroup()
        {
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is MaterialGraphGroup)
            {
                evt.menu.AppendAction("Convert To Sub-graph", PrintAllNodesInsideGroup, ContextualMenu.MenuAction.AlwaysEnabled);
            }
        }

        void PrintAllNodesInsideGroup(ContextualMenu.MenuAction obj)
        {
            foreach (GraphElement element in containedElements)
            {
                Debug.Log((element.userData as INode).name);
            }
        }
    }
}

