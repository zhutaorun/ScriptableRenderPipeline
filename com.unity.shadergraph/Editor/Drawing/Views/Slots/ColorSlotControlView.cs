using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
#if UNITY_2019_1_OR_NEWER
using ColorInputField = UnityEditor.Experimental.UIElements.ColorInput;
#else
using ColorInputField = UnityEditor.Experimental.UIElements.ColorField;
#endif

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class ColorRGBASlotControlView : VisualElement
    {
        ColorRGBAMaterialSlot m_Slot;

        public ColorRGBASlotControlView(ColorRGBAMaterialSlot slot)
        {
            AddStyleSheetPath("Styles/Controls/ColorRGBASlotControlView");
            m_Slot = slot;
            var colorField = new ColorInputField { value = slot.value, showEyeDropper = false };
            colorField.OnValueChanged(OnValueChanged);
            Add(colorField);
        }

        void OnValueChanged(ChangeEvent<Color> evt)
        {
            m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Color Change");
            m_Slot.value = evt.newValue;
            m_Slot.owner.Dirty(ModificationScope.Node);
        }
    }
}
