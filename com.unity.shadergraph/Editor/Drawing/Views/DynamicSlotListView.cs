using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing
{
    using SlotListType = DynamicSlotList.SlotListType;

    public class DynamicSlotListView : VisualElement
    {
        AbstractMaterialNode m_Node;
        DynamicSlotList m_DynamicSlotList;

		IMGUIContainer m_InputContainer;
        ReorderableList m_InputList;

        IMGUIContainer m_OutputContainer;
        ReorderableList m_OutputList;

        public DynamicSlotListView(AbstractMaterialNode node, DynamicSlotList slotList)
        {
            AddStyleSheetPath("Styles/DynamicSlotListView");
            m_Node = node;
            m_DynamicSlotList = slotList;

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

        void CreateReorderableList(DynamicSlotList.SlotListType type)
        {
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                switch(type)
                {
                    case SlotListType.Input:
                        {
                            var list = m_DynamicSlotList.inputList;
                            m_InputList = DynamicSlotUtils.CreateDynamicSlotList(list, "Input Slots", true, true, true, true);
                            m_InputList.onAddCallback += Redraw;
                            m_InputList.DoLayoutList();

                            if (changeCheckScope.changed)
                            {
                                m_DynamicSlotList.inputList = list;
                                m_Node.Dirty(ModificationScope.Node);
                            }
                        }
                        break;
                    case SlotListType.Output:
                        {
                            var list = m_DynamicSlotList.outputList;
                            m_OutputList = DynamicSlotUtils.CreateDynamicSlotList(list, "Output Slots", true, true, true, true);
                            m_OutputList.onAddCallback += Redraw;
                            m_OutputList.DoLayoutList();

                            if (changeCheckScope.changed)
                            {
                                m_DynamicSlotList.outputList = list;
                                m_Node.Dirty(ModificationScope.Node);
                            }
                        }
                        break;
                }
            }
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
