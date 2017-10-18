using System;
using System.Linq;
using System.Reflection;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class SubGraphOutputControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!(node is SubGraphOutputNode))
                throw new ArgumentException("Node must inherit from AbstractSubGraphIONode.", "node");
            return new SubGraphOutputControlView((SubGraphOutputNode)node);
        }
    }

    public class SubGraphOutputControlView : VisualElement
    {
        SubGraphOutputNode m_Node;

        public SubGraphOutputControlView(SubGraphOutputNode node)
        {
            m_Node = node;
            Add(new Button(OnAdd) { text = "Add Slot" });
            Add(new Button(OnRemove) { text = "Remove Slot" });
        }

        void OnAdd()
        {
            m_Node.AddSlot();
        }

        void OnRemove()
        {
            m_Node.RemoveSlot();
        }
    }

    public class SubGraphOutputNode : AbstractMaterialNode
    {
        [SubGraphOutputControl]
        int controlDummy { get; set; }

        public SubGraphOutputNode()
        {
            name = "SubGraphOutputs";
        }

        public virtual int AddSlot()
        {
            var index = GetInputSlots<ISlot>().Count() + 1;
            AddSlot(new Vector4MaterialSlot(index, "Output " + index, "Output" + index, SlotType.Input, Vector4.zero));
            return index;
        }

        public virtual void RemoveSlot()
        {
            var index = GetInputSlots<ISlot>().Count();
            if (index == 0)
                return;

            RemoveSlot(index);
        }

        public override bool allowedInRemapGraph { get { return false; } }
    }
}
