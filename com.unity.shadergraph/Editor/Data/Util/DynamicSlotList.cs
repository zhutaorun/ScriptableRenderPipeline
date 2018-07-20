using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditorInternal;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
	// ----------------------------------------------------------------------------------------------------
    // Class
    // ----------------------------------------------------------------------------------------------------

	[Serializable]
	public class DynamicSlotList
	{
		public enum SlotListType { Input, Output, All }

        [Serializable]
		public class Entry
		{
            public int slotId;
			public string name = "New Slot";
			public SlotValueType type = SlotValueType.Vector1;

            public Entry()
            {
                slotId = -1;
            }
		}

		public DynamicSlotList(AbstractMaterialNode node, SlotListType type)
		{
			m_Node = node;
			m_Type = type;
		}

		private AbstractMaterialNode m_Node;
		private SlotListType m_Type;
		public SlotListType type
		{
			get { return m_Type; }
		}

		public List<Entry> m_InputList = new List<Entry>();

		public List<Entry> inputList
		{
			get { return m_InputList; }
			set 
			{ 
				m_InputList = value;
				UpdateSlots();
				m_Node.Dirty(ModificationScope.Topological); 
			}
		}

		public List<Entry> m_OutputList = new List<Entry>();

		public List<Entry> outputList
		{
			get { return m_OutputList; }
			set 
			{ 
				m_OutputList = value;
				UpdateSlots();
				m_Node.Dirty(ModificationScope.Topological); 
			}
		}

        private List<int> m_ActiveInputSlots = new List<int>();

        public List<int> activeInputSlots
        {
            get { return m_ActiveInputSlots; }
            set { m_ActiveInputSlots = value; }
        }

        private List<int> m_ActiveOutputSlots = new List<int>();

        public List<int> activeOutputSlots
        {
            get { return m_ActiveOutputSlots; }
            set { m_ActiveOutputSlots = value; }
        }

        public void UpdateSlots()
        {
            List<int> validNames = new List<int>();

            for(int i = 0; i < inputList.Count; i++)
            {
                if(inputList[i].slotId == -1)
                    inputList[i].slotId = GetNewSlotID();
                
                MaterialSlot slot = MaterialSlot.CreateMaterialSlot(inputList[i].type, inputList[i].slotId, inputList[i].name, inputList[i].name, 
                    SlotType.Input, Vector4.zero, ShaderStageCapability.All);

                m_Node.AddSlot(slot);
                validNames.Add(inputList[i].slotId);
            }

            for(int i = 0; i < outputList.Count; i++)
            {
                if(outputList[i].slotId == -1)
                    outputList[i].slotId = GetNewSlotID();
                
                MaterialSlot slot = MaterialSlot.CreateMaterialSlot(outputList[i].type, outputList[i].slotId, outputList[i].name, outputList[i].name, 
                    SlotType.Output, Vector4.zero, ShaderStageCapability.All);
                    
                m_Node.AddSlot(slot);
                validNames.Add(outputList[i].slotId);
            }

            m_Node.RemoveSlotsNameNotMatching(validNames);
        }

        private int GetNewSlotID()
        {
            int ceiling = -1;

            foreach(Entry e in inputList)
                ceiling = e.slotId > ceiling ? e.slotId : ceiling;
            
            foreach(Entry e in outputList)
                ceiling = e.slotId > ceiling ? e.slotId : ceiling;

            return ceiling + 1;
        }

		public VisualElement CreateSettingsElement()
		{
			var container = new VisualElement();

            var inputListElement = new DynamicSlotListView(m_Node, this);
            if (inputListElement != null)
                container.Add(inputListElement);

            return container;
		}
	}

	// ----------------------------------------------------------------------------------------------------
    // Utils
    // ----------------------------------------------------------------------------------------------------

    public static class DynamicSlotUtils
    {
        public static ReorderableList CreateDynamicSlotList(List<DynamicSlotList.Entry> list, string label, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton) 
        {
            var reorderableList = new ReorderableList(list, typeof(DynamicSlotList.Entry), draggable, displayHeader, displayAddButton, displayRemoveButton);

            reorderableList.drawHeaderCallback = (Rect rect) => 
            {  
                var labelRect = new Rect(rect.x, rect.y, rect.width-10, rect.height);
                EditorGUI.LabelField(labelRect, label);
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
            {
                var element = list[index];
                rect.y += 2;
                CreateEntry(list, index, element, new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight));
            };

            reorderableList.elementHeightCallback = (int indexer) => 
            {
                return reorderableList.elementHeight;
            };

            reorderableList.onAddCallback += AddItem;
            reorderableList.onRemoveCallback += RemoveItem;
            return reorderableList;
        }

        private static void CreateEntry(List<DynamicSlotList.Entry> list, int index, DynamicSlotList.Entry entry, Rect rect)
        {
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            list[index].name = EditorGUI.TextField( new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.name, labelStyle);
            list[index].type = (SlotValueType)EditorGUI.EnumPopup( new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.type);
        }

        private static void AddItem(ReorderableList list)
        {
            list.list.Add(new DynamicSlotList.Entry());
        }

        private static void RemoveItem(ReorderableList list)
        {
            list.list.RemoveAt(list.index);
        }
    }
}
