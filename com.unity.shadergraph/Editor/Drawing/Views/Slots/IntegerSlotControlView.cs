using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class IntegerSlotControlView : VisualElement
    {
        readonly INode m_Node;
        readonly Func<float> m_Get;
        readonly Action<float> m_Set;
        int m_UndoGroup = -1;

        public IntegerSlotControlView(INode node, string label, Func<float> get, Action<float> set)
        {
            AddStyleSheetPath("Styles/Controls/IntegerSlotControlView");
            m_Node = node;
            m_Get = get;
            m_Set = set;
            var initialValue = get();
            AddField(initialValue, label);
        }

        void AddField(float initialValue, string subLabel)
        {
            var dummy = new VisualElement { name = "dummy" };
            var label = new Label(subLabel);
            dummy.Add(label);
            Add(dummy);
            var field = new IntegerField { value = (int)initialValue };
            var dragger = new FieldMouseDragger<int>(field);
            dragger.SetDragZone(label);
            field.OnValueChanged(evt =>
                {
                    var value = m_Get();
                    value = (float)evt.newValue;
                    m_Set(value);
                    m_Node.Dirty(ModificationScope.Node);
                    m_UndoGroup = -1;
                });
            field.RegisterCallback<InputEvent>(evt =>
                {
                    if (m_UndoGroup == -1)
                    {
                        m_UndoGroup = Undo.GetCurrentGroup();
                        m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    }
                    float newValue;
                    if (!float.TryParse(evt.newData, out newValue))
                        newValue = 0f;
                    var value = m_Get();
                    if (Math.Abs(value - newValue) > 1e-9)
                    {
                        value = newValue;
                        m_Set(value);
                        m_Node.Dirty(ModificationScope.Node);
                    }
                });
            field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                    {
                        Undo.RevertAllDownToGroup(m_UndoGroup);
                        m_UndoGroup = -1;
                        evt.StopPropagation();
                    }
                    this.MarkDirtyRepaint();
                });
            Add(field);
        }
    }
}
