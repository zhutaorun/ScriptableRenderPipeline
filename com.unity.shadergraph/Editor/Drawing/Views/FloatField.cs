using UnityEditor.Experimental.UIElements;
#if !UNITY_2019_1_OR_NEWER
using DoubleInput = UnityEditor.Experimental.UIElements.DoubleField;
#endif

namespace UnityEditor.ShaderGraph.Drawing
{
    public class FloatField : DoubleInput
    {
        protected override string ValueToString(double v)
        {
            return ((float)v).ToString();
        }
    }
}
