using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
#if UNITY_2018_3_OR_NEWER
using ContextualMenu = UnityEngine.Experimental.UIElements.DropdownMenu;
#endif

namespace UnityEditor.ShaderGraph
{
    sealed class MaterialGraphGroup : Group
    {
        public MaterialGraphGroup()
        {
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is MaterialGraphGroup)
            {
                evt.menu.AppendAction("Ungroup All Nodes", RemoveNodesInsideGroup, ContextualMenu.MenuAction.AlwaysEnabled);
            }
        }

        void RemoveNodesInsideGroup(ContextualMenu.MenuAction obj)
        {
            var elements = containedElements.ToList();
            foreach (GraphElement element in elements)
            {
                var node = element.userData as INode;
                if (node == null)
                    continue;

                RemoveElement(element);
            }
        }
    }
}

