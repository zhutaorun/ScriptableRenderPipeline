using System;
using UnityEngine;
using System.Reflection;

namespace UnityEditor.Experimental.Rendering
{
    /// <summary>
    /// Provide a gizmo representing a box where all face can be moved independently.
    /// Also add a contained sub gizmo box if contained is used at creation.
    /// </summary>
    /// <example>
    /// <code>
    /// class MyComponentEditor : Editor
    /// {
    ///     static HierarchicalBox box;
    ///     static HierarchicalBox containedBox;
    ///
    ///     static MyComponentEditor()
    ///     {
    ///         Color[] handleColors = new Color[]
    ///         {
    ///             Color.red,
    ///             Color.green,
    ///             Color.Blue,
    ///             new Color(0.5f, 0f, 0f, 1f),
    ///             new Color(0f, 0.5f, 0f, 1f),
    ///             new Color(0f, 0f, 0.5f, 1f)
    ///         };
    ///         box = new HierarchicalBox(new Color(1f, 1f, 1f, 0.25), handleColors);
    ///         containedBox = new HierarchicalBox(new Color(1f, 0f, 1f, 0.25), handleColors, container: box);
    ///     }
    ///
    ///     [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
    ///     void DrawGizmo(MyComponent comp, GizmoType gizmoType)
    ///     {
    ///         box.center = comp.transform.position;
    ///         box.size = comp.transform.scale;
    ///         box.DrawHull(gizmoType == GizmoType.Selected);
    ///         
    ///         box.center = comp.innerposition;
    ///         box.size = comp.innerScale;
    ///         box.DrawHull(gizmoType == GizmoType.Selected);
    ///     }
    ///
    ///     void OnSceneGUI()
    ///     {
    ///         box.DrawHandle();
    ///         containedBox.DrawHandle();
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HierarchicalBox
    {
        const float k_HandleSizeCoef = 0.05f;

        enum NamedFace { Right, Top, Front, Left, Bottom, Back, None }

        readonly Mesh m_Face;
        readonly Material m_Material;
        readonly Color[] m_PolychromeHandleColor;
        readonly Color m_MonochromeHandleColor;

        readonly HierarchicalBox m_container;

        private bool m_MonoHandle = true;

        /// <summary>
        /// Allow to switch between the mode where all axis are controlled together or not
        /// Note that if there is several handles, they will use the polychrom colors.
        /// </summary>
        public bool monoHandle { get { return m_MonoHandle; } set { m_MonoHandle = value; } }

        private int[] m_ControlIDs = new int[6] { 0, 0, 0, 0, 0, 0 };

        /// <summary>The position of the center of the box in Handle.matrix space.</summary>
        public Vector3 center { get; set; }

        /// <summary>The size of the box in Handle.matrix space.</summary>
        public Vector3 size { get; set; }

        //Note: Handles.Slider not allow to use a specific ControlID.
        //Thus Slider1D is used (with reflection)
        static PropertyInfo k_scale = Type.GetType("UnityEditor.SnapSettings, UnityEditor").GetProperty("scale");
        static Type k_Slider1D = Type.GetType("UnityEditorInternal.Slider1D, UnityEditor");
        static MethodInfo k_Slider1D_Do = k_Slider1D
                .GetMethod(
                    "Do",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    CallingConventions.Any,
                    new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(float), typeof(Handles.CapFunction), typeof(float) },
                    null);
        static void Slider1D(int controlID, ref Vector3 handlePosition, Vector3 handleOrientation, float snapScale, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                handlePosition = (Vector3)k_Slider1D_Do.Invoke(null, new object[]
                    {
                        controlID,
                        handlePosition,
                        handleOrientation,
                        HandleUtility.GetHandleSize(handlePosition) * k_HandleSizeCoef,
                        new Handles.CapFunction(Handles.DotHandleCap),
                        snapScale
                    });
            }
        }


        /// <summary>Constructor. Used to setup colors and also the container if any.</summary>
        /// <param name="faceColor">The color of each face of the box.</param>
        /// <param name="polychromeHandleColors">The color of handle when they are separated. When they are grouped, they use a variation of the faceColor instead.</param>
        /// <param name="container">The HierarchicalBox containing this box. If null, the box will not be limited in size.</param>
        public HierarchicalBox(Color faceColor, Color[] polychromeHandleColors = null, HierarchicalBox container = null)
        {
            m_container = container;
            m_Material = new Material(Shader.Find("Hidden/UnlitTransparentColored"));
            m_Material.color = faceColor.gamma;
            m_Face = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            if(polychromeHandleColors != null && polychromeHandleColors.Length != 6)
            {
                throw new System.ArgumentException("polychromeHandleColors must be null or have a size of 6.");
            }
            m_PolychromeHandleColor = polychromeHandleColors ?? new Color[]
            {
                new Color(1f, 0f, 0f, 1f),
                new Color(0f, 1f, 0f, 1f),
                new Color(0f, 0f, 1f, 1f),
                new Color(1f, 0f, 0f, 1f),
                new Color(0f, 1f, 0f, 1f),
                new Color(0f, 0f, 1f, 1f)
            };
            faceColor.a = 0.7f;
            m_MonochromeHandleColor = faceColor;
        }

        Color GetHandleColor(NamedFace name)
        {
            return monoHandle ? m_MonochromeHandleColor : m_PolychromeHandleColor[(int)name];
        }

        /// <summary>Draw the hull which means the boxes without the handles</summary>
        public void DrawHull(bool selected)
        {
            if (selected)
            {
                Vector3 xSize = new Vector3(size.z, size.y, 1f);
                m_Material.SetPass(0);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.left, Quaternion.FromToRotation(Vector3.forward, Vector3.left), xSize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.right, Quaternion.FromToRotation(Vector3.forward, Vector3.right), xSize));
                
                Vector3 ySize = new Vector3(size.x, size.z, 1f);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.up, Quaternion.FromToRotation(Vector3.forward, Vector3.up), ySize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.down, Quaternion.FromToRotation(Vector3.forward, Vector3.down), ySize));

                Vector3 zSize = new Vector3(size.x, size.y, 1f);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.forward, Quaternion.identity, zSize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.back, Quaternion.FromToRotation(Vector3.forward, Vector3.back), zSize));

                //if contained, also draw handle distance to container here
                if (m_container != null)
                {
                    Vector3 centerDiff = center - m_container.center;
                    Vector3 xRecal = centerDiff;
                    Vector3 yRecal = centerDiff;
                    Vector3 zRecal = centerDiff;
                    xRecal.x = 0;
                    yRecal.y = 0;
                    zRecal.z = 0;

                    Color previousColor = Handles.color;
                    Handles.color = m_container.GetHandleColor(NamedFace.Left);
                    Debug.Log(Handles.color);
                    Handles.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.left, center + size.x * .5f * Vector3.left);

                    Handles.color = m_container.GetHandleColor(NamedFace.Right);
                    Handles.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.right, center + size.x * .5f * Vector3.right);

                    Handles.color = m_container.GetHandleColor(NamedFace.Top);
                    Handles.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.up, center + size.y * .5f * Vector3.up);

                    Handles.color = m_container.GetHandleColor(NamedFace.Bottom);
                    Handles.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.down, center + size.y * .5f * Vector3.down);

                    Handles.color = m_container.GetHandleColor(NamedFace.Front);
                    Handles.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.forward, center + size.z * .5f * Vector3.forward);

                    Handles.color = m_container.GetHandleColor(NamedFace.Back);
                    Handles.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.back, center + size.z * .5f * Vector3.back);

                    Handles.color = previousColor;
                }
            }

            Handles.DrawWireCube(center, size);
        }

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                m_ControlIDs[i] = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

            EditorGUI.BeginChangeCheck();

            Vector3 leftPosition = center + size.x * .5f * Vector3.left;
            Vector3 rightPosition = center + size.x * .5f * Vector3.right;
            Vector3 topPosition = center + size.y * .5f * Vector3.up;
            Vector3 bottomPosition = center + size.y * .5f * Vector3.down;
            Vector3 frontPosition = center + size.z * .5f * Vector3.forward;
            Vector3 backPosition = center + size.z * .5f * Vector3.back;

            float snapScale = (float)k_scale.GetValue(null, null);
            NamedFace theChangedFace = NamedFace.None;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Left], ref leftPosition, Vector3.left, snapScale, GetHandleColor(NamedFace.Left));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Left;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Right], ref rightPosition, Vector3.right, snapScale, GetHandleColor(NamedFace.Right));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Right;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Top], ref topPosition, Vector3.up, snapScale, GetHandleColor(NamedFace.Top));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Top;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Bottom], ref bottomPosition, Vector3.down, snapScale, GetHandleColor(NamedFace.Bottom));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Bottom;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Front], ref frontPosition, Vector3.forward, snapScale, GetHandleColor(NamedFace.Front));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Front;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Back], ref backPosition, Vector3.back, snapScale, GetHandleColor(NamedFace.Back));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Back;

            if (EditorGUI.EndChangeCheck())
            {
                if (monoHandle)
                {
                    float decal = 0f;
                    switch (theChangedFace)
                    {
                        case NamedFace.Left:
                            decal = (leftPosition - center - size.x * .5f * Vector3.left).x;
                            break;
                        case NamedFace.Right:
                            decal = -(rightPosition - center - size.x * .5f * Vector3.right).x;
                            break;
                        case NamedFace.Top:
                            decal = -(topPosition - center - size.y * .5f * Vector3.up).y;
                            break;
                        case NamedFace.Bottom:
                            decal = (bottomPosition - center - size.y * .5f * Vector3.down).y;
                            break;
                        case NamedFace.Front:
                            decal = -(frontPosition - center - size.z * .5f * Vector3.forward).z;
                            break;
                        case NamedFace.Back:
                            decal = (backPosition - center - size.z * .5f * Vector3.back).z;
                            break;
                    }

                    Vector3 tempSize = size - Vector3.one * decal;
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (tempSize[axis] < 0)
                        {
                            decal += tempSize[axis];
                            tempSize = size - Vector3.one * decal;
                        }
                    }

                    size = tempSize;
                }
                else
                {
                    Vector3 max = new Vector3(rightPosition.x, topPosition.y, frontPosition.z);
                    Vector3 min = new Vector3(leftPosition.x, bottomPosition.y, backPosition.z);

                    //ensure that the box face are still facing outside
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (min[axis] > max[axis])
                        {
                            if (GUIUtility.hotControl == m_ControlIDs[axis])
                            {
                                max[axis] = min[axis];
                            }
                            else
                            {
                                min[axis] = max[axis];
                            }
                        }
                    }

                    center = (max + min) * .5f;
                    size = max - min;
                }
            }
        }
    }
}
