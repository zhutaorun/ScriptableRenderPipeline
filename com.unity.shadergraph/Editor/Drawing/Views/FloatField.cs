using UnityEditor.Experimental.UIElements;
#if UNITY_2019_1_OR_NEWER
using DoubleInputField = UnityEditor.Experimental.UIElements.DoubleInput;
#else
using DoubleInputField = UnityEditor.Experimental.UIElements.DoubleField;
#endif

namespace UnityEditor.ShaderGraph.Drawing
{
    public class FloatField : DoubleInputField
    {
        protected override string ValueToString(double v)
        {
            return ((float)v).ToString();
        }
    }
}
