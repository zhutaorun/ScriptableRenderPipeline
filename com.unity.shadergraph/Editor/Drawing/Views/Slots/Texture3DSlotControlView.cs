using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;
#if UNITY_2019_1_OR_NEWER
using ObjectInputField = UnityEditor.Experimental.UIElements.ObjectInput;
#else
using ObjectInputField = UnityEditor.Experimental.UIElements.ObjectField;
#endif
namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class Texture3DSlotControlView : VisualElement
    {
        Texture3DInputMaterialSlot m_Slot;

        public Texture3DSlotControlView(Texture3DInputMaterialSlot slot)
        {
            m_Slot = slot;
            AddStyleSheetPath("Styles/Controls/Texture3DSlotControlView");
            var objectField = new ObjectInputField { objectType = typeof(Texture3D), value = m_Slot.texture };
            objectField.OnValueChanged(OnValueChanged);
            Add(objectField);
        }

        void OnValueChanged(ChangeEvent<Object> evt)
        {
            var texture = evt.newValue as Texture3D;
            if (texture != m_Slot.texture)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Texture");
                m_Slot.texture = texture;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
