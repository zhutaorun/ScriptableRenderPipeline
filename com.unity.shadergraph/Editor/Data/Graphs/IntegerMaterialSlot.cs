using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class IntegerMaterialSlot : Vector1MaterialSlot
    {
        public IntegerMaterialSlot()
        {
        }

        public IntegerMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = "",
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, value.x, stageCapability, label1, hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            return new IntegerSlotControlView(owner, labels[0], () => value, (newValue) => value = newValue);
        }
    }
}
