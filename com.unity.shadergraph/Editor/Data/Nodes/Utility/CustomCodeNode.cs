using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Title("Custom Code")]
    public class CustomCodeNode : AbstractMaterialNode, IGeneratesFunction, IGeneratesBodyCode, IHasSettings
    {
        [SerializeField]
		DynamicSlotList m_DynamicSlotList;

        [DynamicSlotListControl]
		public DynamicSlotList dynamicSlotList
		{
			get { return m_DynamicSlotList; }
			set { m_DynamicSlotList = value; }
		}

		[SerializeField]
		private string m_Code;

		[TextFieldControl("", true)]
		public string code
		{
			get { return m_Code; }
			set 
			{ 
				m_Code = value;
				Dirty(ModificationScope.Topological); 
			}
		}

        public override bool hasPreview { get { return true; } }

        public CustomCodeNode()
        {
            name = "Custom Code";
            m_DynamicSlotList = new DynamicSlotList(this, DynamicSlotList.SlotListType.All);
            UpdateNodeAfterDeserialization();
        }

		string GetFunctionName()
        {
            return string.Format("Unity_CustomCode_{0}", precision);
        }

		public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
			var arguments = new List<string>();

            for(int i = 0; i < dynamicSlotList.inputList.Count; i++)
            {
                //var slotIndex = 100 * 0 + i;
                //arguments.Add(GetSlotValue(slotIndex, generationMode));
                arguments.Add(GetSlotValue(dynamicSlotList.inputList[i].slotId, generationMode));
            }

			for(int i = 0; i < dynamicSlotList.outputList.Count; i++)
            {
				//var slotIndex = 100 * 1 + i;
                //arguments.Add(GetVariableNameForSlot(slotIndex));
                arguments.Add(GetVariableNameForSlot(dynamicSlotList.outputList[i].slotId));
            }

            if(arguments.Count == 0)
                return;

            for(int i = 0; i < dynamicSlotList.outputList.Count; i++)
            {
				//var slotIndex = 100 * 1 + i;
                //visitor.AddShaderChunk(string.Format("{0} {1};", 
                //    FindOutputSlot<MaterialSlot>(slotIndex).concreteValueType.ToString(precision), 
                //    GetVariableNameForSlot(slotIndex)), false);
                visitor.AddShaderChunk(string.Format("{0} {1};", 
                    FindOutputSlot<MaterialSlot>(dynamicSlotList.outputList[i].slotId).concreteValueType.ToString(precision), 
                    GetVariableNameForSlot(dynamicSlotList.outputList[i].slotId)), false);
            }

			visitor.AddShaderChunk(
                string.Format("{0}({1});"
                    , GetFunctionName()
                    , arguments.Aggregate((current, next) => string.Format("{0}, {1}", current, next)))
                , false);
        }

		public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
			var arguments = new List<string>();
            
			for(int i = 0; i < dynamicSlotList.inputList.Count; i++)
			{
				//var slotIndex = 100 * 0 + i;
                //MaterialSlot slot = FindInputSlot<MaterialSlot>(slotIndex);
                MaterialSlot slot = FindInputSlot<MaterialSlot>(dynamicSlotList.inputList[i].slotId);
				arguments.Add(string.Format("{0} {1}", slot.concreteValueType.ToString(precision), slot.shaderOutputName));
			}

			for(int i = 0; i < dynamicSlotList.outputList.Count; i++)
			{
				//var slotIndex = 100 * 1 + i;
                //MaterialSlot slot = FindOutputSlot<MaterialSlot>(slotIndex);
                MaterialSlot slot = FindOutputSlot<MaterialSlot>(dynamicSlotList.outputList[i].slotId);
				arguments.Add(string.Format("out {0} {1}", slot.concreteValueType.ToString(precision), slot.shaderOutputName));
			}

            if(arguments.Count == 0)
                return;

			var argumentString = string.Format("{0}({1})"
                    , GetFunctionName()
                    , arguments.Aggregate((current, next) => string.Format("{0}, {1}", current, next)));

            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}", argumentString);
                    using (s.BlockScope())
                    {
                        s.AppendLine(code);
                    }
                });
        }

        public VisualElement CreateSettingsElement()
        {
            return dynamicSlotList.CreateSettingsElement();
        }
    }
}

