using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    using SlotListType = DynamicSlotList.SlotListType;

    [AttributeUsage(AttributeTargets.Property)]
    public class DynamicSlotListControlAttribute : Attribute, IControlAttribute
    {
        public DynamicSlotListControlAttribute()
        {
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new DynamicSlotListControlView(node, propertyInfo);
        }
    }

    public class DynamicSlotListControlView : VisualElement, INodeModificationListener
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        DynamicSlotList m_DynamicSlotList;

        IMGUIContainer m_InputContainer;
        ReorderableList m_InputList;

        IMGUIContainer m_OutputContainer;
        ReorderableList m_OutputList;

        public DynamicSlotListControlView(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            AddStyleSheetPath("Styles/Controls/DynamicSlotListControlView");
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_DynamicSlotList = (DynamicSlotList)m_PropertyInfo.GetValue(m_Node, null);

            if (propertyInfo.PropertyType != typeof(DynamicSlotList))
                throw new ArgumentException("Property must be an DynamicSlotList.", "propertyInfo");

            if(m_DynamicSlotList.type != DynamicSlotList.SlotListType.Output)
            {   
                m_InputContainer = new IMGUIContainer(() => CreateReorderableList (SlotListType.Input)) { name = "ListContainer" };
                Add(m_InputContainer);
            }

            if(m_DynamicSlotList.type != DynamicSlotList.SlotListType.Input)
            {
                m_OutputContainer = new IMGUIContainer(() => CreateReorderableList (SlotListType.Output)) { name = "ListContainer" };
                Add(m_OutputContainer);
            }
        }

        void CreateReorderableList(SlotListType type)
        {
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                m_DynamicSlotList = (DynamicSlotList)m_PropertyInfo.GetValue(m_Node, null);

                switch(type)
                {
                    case SlotListType.Input:
                        {
                            var list = m_DynamicSlotList.inputList;
                            m_InputList = DynamicSlotUtils.CreateDynamicSlotList(list, "Input Slots", true, true, true, true);
                            
                            m_InputList.onAddCallback += Redraw;
                            m_InputList.onRemoveCallback += Redraw;
                            m_InputList.onSelectCallback += SelectItem;// Redraw;
                            m_InputList.onReorderCallback += SelectItem;

                            m_InputList.DoLayoutList();

                            if (changeCheckScope.changed)
                            {
                                m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                                m_DynamicSlotList.inputList = list;
                                m_PropertyInfo.SetValue(m_Node, m_DynamicSlotList, null);
                            }
                        }
                        break;
                    case SlotListType.Output:
                        {
                            var list = m_DynamicSlotList.outputList;
                            m_OutputList = DynamicSlotUtils.CreateDynamicSlotList(list, "Output Slots", true, true, true, true);

                            m_OutputList.onAddCallback += Redraw;
                            m_OutputList.onRemoveCallback += Redraw;
                            m_OutputList.onSelectCallback += Redraw;
                            m_OutputList.onReorderCallback += Redraw;

                            m_OutputList.DoLayoutList();

                            if (changeCheckScope.changed)
                            {
                                m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                                m_DynamicSlotList.outputList = list;
                                m_PropertyInfo.SetValue(m_Node, m_DynamicSlotList, null);
                            }
                        }
                        break;
                }
            }
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Node)
                Redraw(null);
        }

        private void SelectItem(ReorderableList list)
        {
            Repaint(list);
            m_InputContainer.Focus();
        }

        void Repaint(ReorderableList list)
        {
            if(m_DynamicSlotList.type != DynamicSlotList.SlotListType.Output)
                m_InputContainer.MarkDirtyRepaint();

            if(m_DynamicSlotList.type != DynamicSlotList.SlotListType.Input)
                m_OutputContainer.MarkDirtyRepaint();
        }

        void Redraw(ReorderableList list)
        {
            if(m_DynamicSlotList.type != DynamicSlotList.SlotListType.Output)
            {   
                Remove(m_InputContainer);
                m_InputContainer = new IMGUIContainer(() => CreateReorderableList (SlotListType.Input)) { name = "ListContainer" };
                Add(m_InputContainer);
            }

            if(m_DynamicSlotList.type != DynamicSlotList.SlotListType.Input)
            {
                Remove(m_OutputContainer);
                m_OutputContainer = new IMGUIContainer(() => CreateReorderableList (SlotListType.Output)) { name = "ListContainer" };
                Add(m_OutputContainer);
            }
        }
    }
}
