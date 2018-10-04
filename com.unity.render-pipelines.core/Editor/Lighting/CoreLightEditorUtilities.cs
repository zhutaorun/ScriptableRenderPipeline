using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreLightEditorUtilities
    {
        public static void DrawSpotlightWireFrameWithZTest(Light spotlight, Color? drawColorOuter = null, Color? drawColorInner = null, bool drawHandlesAndLabels = true)
        {
            // Saving the default colors
            var defColor = Handles.color;
            var defZTest = Handles.zTest;

            // Default Color for outer cone will be Yellow if nothing has been provided.
            Color outerColor = GetLightAboveObjectWireframeColor(drawColorOuter ?? Color.yellow);

            // The default z-test outer color will be 20% opacity of the outer color
            Color outerColorZTest = GetLightBehindObjectWireframeColor(outerColor);

            // Default Color for inner cone will be Yellow-ish if nothing has been provided.
            Color innerColor = GetLightAboveObjectWireframeColor(drawColorInner ?? new Color32(255, 100, 100, 150));

            // The default z-test outer color will be 20% opacity of the inner color
            Color innerColorZTest = GetLightBehindObjectWireframeColor(innerColor);

            // Drawing before objects
            Handles.zTest = CompareFunction.LessEqual;
            DrawSpotlightWireframe(spotlight, outerColor, innerColor);

            // Drawing behind objects
            Handles.zTest = CompareFunction.Greater;
            DrawSpotlightWireframe(spotlight, outerColorZTest, innerColorZTest);

            // Resets the compare function to always
            Handles.zTest = CompareFunction.Always;

            if(drawHandlesAndLabels)
                DrawHandlesAndLabels(spotlight);

            // Resets the handle colors
            Handles.color = defColor;
            Handles.zTest = defZTest;
        }

        // These are for the Labels, so we know which one to show
        static int m_HandleHotControl = 0;
        static bool m_ShowOuterLabel = true;
        static bool m_ShowRange = false;

        public static void DrawHandlesAndLabels(Light spotlight)
        {
            // Draw the handles ///////////////////////////////
            Handles.color = GetLightHandleColor(Color.white);

            // Draw Center Handle
            float range = spotlight.range;
            EditorGUI.BeginChangeCheck();
            range = SliderLineHandle(Vector3.zero, Vector3.forward, range);
            if (EditorGUI.EndChangeCheck())
            {
                m_HandleHotControl = GUIUtility.hotControl;
                m_ShowRange = true;
            }

            // Draw outer handles
            EditorGUI.BeginChangeCheck();
            float outerAngle = DrawConeHandles(spotlight.transform.position, spotlight.spotAngle, range);
            if (EditorGUI.EndChangeCheck())
            {
                m_HandleHotControl = GUIUtility.hotControl;
                m_ShowOuterLabel = true;
            }

            float innerAngle = 0;
            // Draw inner handles
            if (spotlight.innerSpotAngle > 0f)
            {
                EditorGUI.BeginChangeCheck();
                innerAngle = DrawConeHandles(spotlight.transform.position, spotlight.innerSpotAngle, range);
                if (EditorGUI.EndChangeCheck())
                {
                    m_HandleHotControl = GUIUtility.hotControl;
                    m_ShowOuterLabel = false;
                }
            }
            /////////////////////////////////////////////////////

            // Adding label /////////////////////////////////////
            Vector3 labelPosition = (Vector3.forward * spotlight.range);

            if (GUIUtility.hotControl != 0 && GUIUtility.hotControl == m_HandleHotControl)
            {
                string labelText = "";
                if (m_ShowRange)
                    labelText = (spotlight.range).ToString("0.00");
                else if (m_ShowOuterLabel)
                    labelText = (spotlight.spotAngle).ToString("0.00");
                else
                    labelText = (spotlight.innerSpotAngle).ToString("0.00");

                var style = new GUIStyle(GUI.skin.label);
                var offsetFromHandle = 10;
                style.contentOffset = new Vector2(0, -(style.font.lineHeight + HandleUtility.GetHandleSize(labelPosition) * 0.03f + offsetFromHandle));
                Handles.Label(labelPosition, labelText, style);
            }
            /////////////////////////////////////////////////////

            // If changes has been made we update the corresponding property
            if (GUI.changed)
            {
                spotlight.spotAngle = outerAngle;
                spotlight.innerSpotAngle = innerAngle;
                spotlight.range = Math.Max(range, 0.01f);
            }

            // Resets the member variables
            if (EditorGUIUtility.hotControl == 0 && EditorGUIUtility.hotControl != m_HandleHotControl)
            {
                m_HandleHotControl = 0;
                m_ShowOuterLabel = true;
                m_ShowRange = false;
            }
        }

        public static void DrawSpotlightWireframe(Light spotlight, Color outerColor, Color innerColor)
        {
            float outerAngle = spotlight.spotAngle;
            float innerAngle = spotlight.innerSpotAngle;
            float range = spotlight.range;

            var outerDiscRadius = range * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
            var outerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle * 0.5f) * range;
            var vectorLineUp = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.up * outerDiscRadius);
            var vectorLineLeft = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.left * outerDiscRadius);

            // Need to check if we need to draw inner angle
            if(innerAngle>0f)
            {
                var innerDiscRadius = range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * range;

                // Drawing the inner Cone and also z-testing it to draw another color if behind
                Handles.color = innerColor;
                DrawConeWireframe(innerDiscRadius, innerDiscDistance);
            }

            // Drawing the outer Cone and also z-testing it to draw another color if behind
            Handles.color = outerColor;
            DrawConeWireframe(outerDiscRadius, outerDiscDistance);

            // Bottom arcs, making a nice rounded shape
            Handles.DrawWireArc(Vector3.zero, Vector3.right, vectorLineUp, outerAngle, range);
            Handles.DrawWireArc(Vector3.zero, Vector3.up, vectorLineLeft, outerAngle, range);

            if(spotlight.shadows != LightShadows.None)
            {
                var shadowDiscRadius = spotlight.shadowNearPlane * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
                var shadowDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle / 2) * spotlight.shadowNearPlane ;
                Handles.DrawWireDisc(Vector3.forward * shadowDiscDistance, Vector3.forward, shadowDiscRadius);
            }
        }

        public static void DrawSpotlightWireframe(Vector3 outerAngleInnerAngleRange, float shadowPlaneDistance = -1f)
        {
            float outerAngle = outerAngleInnerAngleRange.x;
            float innerAngle = outerAngleInnerAngleRange.y;
            float range = outerAngleInnerAngleRange.z;

            var outerDiscRadius = range * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
            var outerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle * 0.5f) * range;
            var vectorLineUp = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.up * outerDiscRadius);
            var vectorLineLeft = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.left * outerDiscRadius);

            if(innerAngle>0f)
            {
                var innerDiscRadius = range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * range;
                DrawConeWireframe(innerDiscRadius, innerDiscDistance);
            }

            DrawConeWireframe(outerDiscRadius, outerDiscDistance);
            Handles.DrawWireArc(Vector3.zero, Vector3.right, vectorLineUp, outerAngle, range);
            Handles.DrawWireArc(Vector3.zero, Vector3.up, vectorLineLeft, outerAngle, range);

            if (shadowPlaneDistance > 0)
            {
                var shadowDiscRadius = shadowPlaneDistance * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
                var shadowDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle / 2) * shadowPlaneDistance;
                Handles.DrawWireDisc(Vector3.forward * shadowDiscDistance, Vector3.forward, shadowDiscRadius);
            }
        }

        static void DrawConeWireframe(float radius, float height)
        {
            var rangeCenter = Vector3.forward * height;
            var rangeUp = rangeCenter + Vector3.up * radius;
            var rangeDown = rangeCenter - Vector3.up * radius;
            var rangeRight = rangeCenter + Vector3.right * radius;
            var rangeLeft = rangeCenter - Vector3.right * radius;

            //Draw Lines
            Handles.DrawLine(Vector3.zero, rangeUp);
            Handles.DrawLine(Vector3.zero, rangeDown);
            Handles.DrawLine(Vector3.zero, rangeRight);
            Handles.DrawLine(Vector3.zero, rangeLeft);

            Handles.DrawWireDisc(rangeCenter, Vector3.forward, radius);
        }


        public static float DrawConeHandles(Vector3 position, float angle, float range)
        {
            angle = SizeSliderSpotAngle(position, Vector3.forward, Vector3.right, range, angle);
            angle = SizeSliderSpotAngle(position, Vector3.forward, -Vector3.right, range, angle);
            angle = SizeSliderSpotAngle(position, Vector3.forward, Vector3.up, range, angle);
            angle = SizeSliderSpotAngle(position, Vector3.forward, -Vector3.up, range, angle);

            return angle;
        }

        public static Vector3 DrawSpotlightHandle(Vector3 outerAngleInnerAngleRange)
        {
            float outerAngle = outerAngleInnerAngleRange.x;
            float innerAngle = outerAngleInnerAngleRange.y;
            float range = outerAngleInnerAngleRange.z;

            return new Vector3(outerAngle, innerAngle, range);
        }

        // Don't use Handles.Disc as it break the highlight of the gizmo axis, use our own draw disc function instead for gizmo
        public static void DrawWireDisc(Quaternion q, Vector3 position, Vector3 axis, float radius)
        {
            Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

            Gizmos.color = Color.white;
            float theta = 0.0f;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = rotation * new Vector3(x, y, 0);
            pos += position;
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < 2.0f * Mathf.PI; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);

                newPos = rotation * new Vector3(x, y, 0);
                newPos += position;
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }

        [Obsolete("DrawSpotlightGizmo is out of date. Should use the DrawSpotlightWireframe/Handle instead", true)]
        public static void DrawSpotlightGizmo(Light spotlight, float innerSpotPercent, bool selected)
        {
            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange - spotlight.gameObject.transform.position);

            var rangeDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.range;
            var rangeDiscRadius = spotlight.range * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            var nearDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.shadowNearPlane;
            var nearDiscRadius = spotlight.shadowNearPlane * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            //Draw Range disc
            DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * rangeDiscDistance, spotlight.gameObject.transform.forward, rangeDiscRadius);

            //Draw Lines
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);

            if (selected)
            {
                //Draw Range Arcs
                Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.right, vectorLineUp, spotlight.spotAngle, spotlight.range);
                Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.up, vectorLineLeft, spotlight.spotAngle, spotlight.range);
                //Draw Near Plane Disc
                if (spotlight.shadows != LightShadows.None)
                    DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * nearDiscDistance, spotlight.gameObject.transform.forward, nearDiscRadius);

                //Inner Cone
                DrawInnerCone(spotlight, innerSpotPercent);
            }
        }

        static float SizeSlider(Vector3 p, Vector3 d, float t)
        {
            Vector3 position = p + d * t;
            float size = HandleUtility.GetHandleSize(position);
            bool temp = GUI.changed;
            GUI.changed = false;
            position = Handles.Slider(position, d, size * 0.04f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
                t = Vector3.Dot(position - p, d);

            GUI.changed |= temp;
            return t;
        }

        static int s_SliderSpotAngleId;

        static float SizeSliderSpotAngle(Vector3 position, Vector3 forward, Vector3 axis, float range, float spotAngle)
        {
            if (Math.Abs(spotAngle) <= 0.05f && GUIUtility.hotControl != s_SliderSpotAngleId)
                return spotAngle;
            var angledForward = Quaternion.AngleAxis(Mathf.Max(spotAngle, 0.05f) * 0.5f, axis) * forward;
            var centerToLeftOnSphere = (angledForward * range + position) - (position + forward * range);
            bool temp = GUI.changed;
            GUI.changed = false;

            var newMagnitude = Mathf.Max(0f, SliderLineHandle(forward * range, centerToLeftOnSphere.normalized, centerToLeftOnSphere.magnitude));
            if (GUI.changed)
            {
                s_SliderSpotAngleId = GUIUtility.hotControl;
                centerToLeftOnSphere = centerToLeftOnSphere.normalized * newMagnitude;
                angledForward = (centerToLeftOnSphere + (position + forward * range) - position).normalized;
                spotAngle = Mathf.Clamp(Mathf.Acos(Vector3.Dot(forward, angledForward)) * Mathf.Rad2Deg * 2, 0f, 179f);
                if (spotAngle <= 0.05f || float.IsNaN(spotAngle))
                    spotAngle = 0f;
            }
            GUI.changed |= temp;
            return spotAngle;
        }

        // innerSpotPercent - 0 to 1 value (percentage 0 - 100%)
        public static void DrawInnerCone(Light spotlight, float innerSpotPercent)
        {
            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * innerSpotPercent * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange - spotlight.gameObject.transform.position);

            //Draw Lines
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);

            var innerAngle = spotlight.spotAngle * innerSpotPercent;
            if (innerAngle > 0)
            {
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * spotlight.range;
                var innerDiscRadius = spotlight.range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                //Draw Range disc
                DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * innerDiscDistance, spotlight.gameObject.transform.forward, innerDiscRadius);
            }
        }

        public static void DrawArealightGizmo(Light arealight)
        {
            var RectangleSize = new Vector3(arealight.areaSize.x, arealight.areaSize.y, 0);
            // Remove scale for light, not take into account
            var localToWorldMatrix = Matrix4x4.TRS(arealight.transform.position, arealight.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, RectangleSize);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(arealight.transform.position, arealight.range);
        }

        [Obsolete("Should use the legacy gizmo draw")]
        public static void DrawPointlightGizmo(Light pointlight, bool selected)
        {
            if (pointlight.shadows != LightShadows.None && selected) Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.shadowNearPlane);
            Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.range);
        }

        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        public static void DrawLightPyramidFrustum(Vector3 center, float fov, float maxRange, float minRange, float aspect)
        {
            fov = Mathf.Deg2Rad * fov * 0.5f;
            float tanfov = Mathf.Tan(fov);
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX;
            Vector3 endSizeY;

            if (aspect >= 1.0f)
            {
                endSizeX = new Vector3(maxRange * tanfov * aspect, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov, 0);
            }
            else
            {
                endSizeX = new Vector3(maxRange * tanfov, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov / aspect, 0);
            }

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX;
                Vector3 startSizeY;
                if (aspect >= 1.0f)
                {
                    startSizeX = new Vector3(minRange * tanfov * aspect, 0, 0);
                    startSizeY = new Vector3(0, minRange * tanfov, 0);
                }
                else
                {
                    startSizeY = new Vector3(minRange * tanfov / aspect, 0, 0);
                    startSizeX = new Vector3(0, minRange * tanfov, 0);
                }
                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        public static void DrawLightOrthoFrustum(Vector3 center, float width, float height, float maxRange, float minRange)
        {
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX = new Vector3(width, 0, 0);
            Vector3 endSizeY = new Vector3(0, height, 0);

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX = new Vector3(width, 0, 0);
                Vector3 startSizeY = new Vector3(0, height, 0);

                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        [Obsolete("Should use the legacy gizmo draw")]
        public static void DrawDirectionalLightGizmo(Light directionalLight)
        {
            var gizmoSize = 0.2f;
            DrawWireDisc(directionalLight.transform.rotation, directionalLight.transform.position, directionalLight.gameObject.transform.forward, gizmoSize);
            Gizmos.DrawLine(directionalLight.transform.position, directionalLight.transform.position + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * gizmoSize, directionalLight.transform.position + directionalLight.transform.up * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * -gizmoSize, directionalLight.transform.position + directionalLight.transform.up * -gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * gizmoSize, directionalLight.transform.position + directionalLight.transform.right * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * -gizmoSize, directionalLight.transform.position + directionalLight.transform.right * -gizmoSize + directionalLight.transform.forward);
        }

        static Vector2 SliderPlaneHandle(Vector3 origin, Vector3 axis1, Vector3 axis2, Vector2 position)
        {
            Vector3 pos = origin + position.x * axis1 + position.y * axis2;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            pos = Handles.Slider2D(pos, Vector3.forward, axis1, axis2, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                position = new Vector2(Vector3.Dot(pos, axis1), Vector3.Dot(pos, axis2));
            }
            GUI.changed |= temp;
            return position;
        }

        static float SliderLineHandle(Vector3 position, Vector3 direction, float value)
        {
            Vector3 pos = position + direction * value;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            pos = Handles.Slider(pos, direction, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                value = Vector3.Dot(pos - position, direction);
            }
            GUI.changed |= temp;
            return value;
        }

        static float SliderCircleHandle(Vector3 position, Vector3 normal, Vector3 zeroValueDirection, float angleValue, float radius)
        {
            zeroValueDirection.Normalize();
            normal.Normalize();
            Quaternion rot = Quaternion.AngleAxis(angleValue, normal);
            Vector3 pos = position + rot * zeroValueDirection * radius;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            Vector3 tangeant = Vector3.Cross(normal, (pos - position).normalized);
            pos = Handles.Slider(pos, tangeant, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                Vector3 dir = (pos - position).normalized;
                Vector3 cross = Vector3.Cross(zeroValueDirection, dir);
                int sign = ((cross - normal).sqrMagnitude < (-cross - normal).sqrMagnitude) ? 1 : -1;
                angleValue = Mathf.Acos(Vector3.Dot(zeroValueDirection, dir)) * Mathf.Rad2Deg * sign;
            }
            GUI.changed |= temp;
            return angleValue;
        }

        public static Color GetLightHandleColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = Mathf.Clamp01(color.a * 2);
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        public static Color GetLightAboveObjectWireframeColor(Color wireframeColor)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? wireframeColor.linear : wireframeColor;
        }

        public static Color GetLightBehindObjectWireframeColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = 0.2f;
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        public static void DrawPyramidFrustumWireframe(Vector4 aspectFovMaxRangeMinRange)
        {
            float aspect = aspectFovMaxRangeMinRange.x;
            float fov = aspectFovMaxRangeMinRange.y;
            float maxRange = aspectFovMaxRangeMinRange.z;
            float minRange = aspectFovMaxRangeMinRange.w;
            float tanfov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
            float minXYFarEndSize = maxRange * tanfov;
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX;
            Vector3 endSizeY;

            if (aspect >= 1.0f)
            {
                endSizeX = new Vector3(minXYFarEndSize * aspect, 0, 0);
                endSizeY = new Vector3(0, minXYFarEndSize, 0);
            }
            else
            {
                endSizeX = new Vector3(minXYFarEndSize, 0, 0);
                endSizeY = new Vector3(0, minXYFarEndSize / aspect, 0);
            }

            Vector3 s1 = Vector3.zero;
            Vector3 s2 = Vector3.zero;
            Vector3 s3 = Vector3.zero;
            Vector3 s4 = Vector3.zero;

            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;

            if (minRange > 0.0f)
            {
                Vector3 nearEnd = new Vector3(0, 0, minRange);

                Vector3 startSizeX;
                Vector3 startSizeY;
                float minXYStartSize = minRange * tanfov;
                if (aspect >= 1.0f)
                {
                    startSizeX = new Vector3(minXYStartSize * aspect, 0, 0);
                    startSizeY = new Vector3(0, minXYStartSize, 0);
                }
                else
                {
                    startSizeX = new Vector3(minXYStartSize, 0, 0);
                    startSizeY = new Vector3(0, minXYStartSize / aspect, 0);
                }
                Vector3 startPoint = nearEnd;
                s1 = startPoint + startSizeX + startSizeY;
                s2 = startPoint - startSizeX + startSizeY;
                s3 = startPoint - startSizeX - startSizeY;
                s4 = startPoint + startSizeX - startSizeY;

                Handles.DrawLine(s1, s2);
                Handles.DrawLine(s2, s3);
                Handles.DrawLine(s3, s4);
                Handles.DrawLine(s4, s1);
            }

            Handles.DrawLine(s1, e1);
            Handles.DrawLine(s2, e2);
            Handles.DrawLine(s3, e3);
            Handles.DrawLine(s4, e4);

            Handles.DrawLine(e1, e2);
            Handles.DrawLine(e2, e3);
            Handles.DrawLine(e3, e4);
            Handles.DrawLine(e4, e1);
        }

        public static Vector4 DrawPyramidFrustumHandle(Vector4 aspectFovMaxRangeMinRange, bool useNearPlane, float minAspect = 0.05f, float maxAspect = 20f, float minFov = 1f)
        {
            float aspect = aspectFovMaxRangeMinRange.x;
            float fov = aspectFovMaxRangeMinRange.y;
            float maxRange = aspectFovMaxRangeMinRange.z;
            float minRange = aspectFovMaxRangeMinRange.w;
            float tanfov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
            float minXYFarEndSize = maxRange * tanfov;
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX;
            Vector3 endSizeY;

            if (aspect >= 1.0f)
            {
                endSizeX = new Vector3(minXYFarEndSize * aspect, 0, 0);
                endSizeY = new Vector3(0, minXYFarEndSize, 0);
            }
            else
            {
                endSizeX = new Vector3(minXYFarEndSize, 0, 0);
                endSizeY = new Vector3(0, minXYFarEndSize / aspect, 0);
            }

            Vector3[] e = new Vector3[]
            {
                farEnd + endSizeX + endSizeY,
                farEnd - endSizeX + endSizeY,
                farEnd - endSizeX - endSizeY,
                farEnd + endSizeX - endSizeY
            };

            if (useNearPlane)
            {
                minRange = SliderLineHandle(Vector3.zero, Vector3.forward, minRange);
            }

            maxRange = SliderLineHandle(Vector3.zero, Vector3.forward, maxRange);

            //find the righttop corner in screen
            Vector2[] screenPositions = new Vector2[]
            {
                Camera.current.WorldToScreenPoint(Handles.matrix * e[0]),
                Camera.current.WorldToScreenPoint(Handles.matrix * e[1]),
                Camera.current.WorldToScreenPoint(Handles.matrix * e[2]),
                Camera.current.WorldToScreenPoint(Handles.matrix * e[3])
            };
            float maxWeight = float.MinValue;
            int maxIndex = 0;
            Vector2 support = new Vector2(Camera.current.pixelWidth, Camera.current.pixelHeight);
            Vector2 supportOrtho = new Vector2(support.y, -support.x);
            for (int i = 0; i < 4; ++i)
            {
                float weight = Vector3.Dot(screenPositions[i], support) - 0.5f * Mathf.Abs(Vector3.Dot(screenPositions[i], supportOrtho));
                if (weight > maxWeight)
                {
                    maxWeight = weight;
                    maxIndex = i;
                }
            }

            Vector2 send = e[maxIndex];
            EditorGUI.BeginChangeCheck();
            Vector2 received = SliderPlaneHandle(farEnd, Vector3.right, Vector3.up, send);
            if (EditorGUI.EndChangeCheck())
            {
                bool fixedFov = Event.current.control && !Event.current.shift;
                bool fixedAspect = Event.current.shift && !Event.current.control;

                //work on positive quadrant
                int xSign = send.x < 0f ? -1 : 1;
                int ySign = send.y < 0f ? -1 : 1;
                Vector2 corrected = new Vector2(received.x * xSign, received.y * ySign);

                //fixed aspect correction
                if (fixedAspect)
                {
                    corrected.x = corrected.y * aspect;
                }

                //remove aspect deadzone
                if (corrected.x > maxAspect * corrected.y)
                {
                    corrected.y = corrected.x * minAspect;
                }
                if (corrected.x < minAspect * corrected.y)
                {
                    corrected.x = corrected.y / maxAspect;
                }

                //remove fov deadzone
                float deadThresholdFoV = Mathf.Tan(Mathf.Deg2Rad * minFov * 0.5f) * maxRange;
                corrected.x = Mathf.Max(corrected.x, deadThresholdFoV);
                corrected.y = Mathf.Max(corrected.y, deadThresholdFoV, Mathf.Epsilon * 100); //prevent any division by zero

                if (!fixedAspect)
                {
                    aspect = corrected.x / corrected.y;
                }
                float min = Mathf.Min(corrected.x, corrected.y);
                if (!fixedFov && maxRange > Mathf.Epsilon * 100)
                {
                    fov = Mathf.Atan(min / maxRange) * 2f * Mathf.Rad2Deg;
                }
            }

            return new Vector4(aspect, fov, maxRange, minRange);
        }

        public static void DrawOrthoFrustumWireframe(Vector4 widthHeightMaxRangeMinRange)
        {
            float halfWidth = widthHeightMaxRangeMinRange.x * 0.5f;
            float halfHeight = widthHeightMaxRangeMinRange.y * 0.5f;
            float maxRange = widthHeightMaxRangeMinRange.z;
            float minRange = widthHeightMaxRangeMinRange.w;

            Vector3 sizeX = new Vector3(halfWidth, 0, 0);
            Vector3 sizeY = new Vector3(0, halfHeight, 0);
            Vector3 nearEnd = new Vector3(0, 0, minRange);
            Vector3 farEnd = new Vector3(0, 0, maxRange);

            Vector3 s1 = nearEnd + sizeX + sizeY;
            Vector3 s2 = nearEnd - sizeX + sizeY;
            Vector3 s3 = nearEnd - sizeX - sizeY;
            Vector3 s4 = nearEnd + sizeX - sizeY;

            Vector3 e1 = farEnd + sizeX + sizeY;
            Vector3 e2 = farEnd - sizeX + sizeY;
            Vector3 e3 = farEnd - sizeX - sizeY;
            Vector3 e4 = farEnd + sizeX - sizeY;

            Handles.DrawLine(s1, s2);
            Handles.DrawLine(s2, s3);
            Handles.DrawLine(s3, s4);
            Handles.DrawLine(s4, s1);

            Handles.DrawLine(e1, e2);
            Handles.DrawLine(e2, e3);
            Handles.DrawLine(e3, e4);
            Handles.DrawLine(e4, e1);

            Handles.DrawLine(s1, e1);
            Handles.DrawLine(s2, e2);
            Handles.DrawLine(s3, e3);
            Handles.DrawLine(s4, e4);
        }

        public static Vector4 DrawOrthoFrustumHandle(Vector4 widthHeightMaxRangeMinRange, bool useNearHandle)
        {
            float halfWidth = widthHeightMaxRangeMinRange.x * 0.5f;
            float halfHeight = widthHeightMaxRangeMinRange.y * 0.5f;
            float maxRange = widthHeightMaxRangeMinRange.z;
            float minRange = widthHeightMaxRangeMinRange.w;
            Vector3 farEnd = new Vector3(0, 0, maxRange);

            if (useNearHandle)
            {
                minRange = SliderLineHandle(Vector3.zero, Vector3.forward, minRange);
            }

            maxRange = SliderLineHandle(Vector3.zero, Vector3.forward, maxRange);

            EditorGUI.BeginChangeCheck();
            halfWidth = SliderLineHandle(farEnd, Vector3.right, halfWidth);
            halfWidth = SliderLineHandle(farEnd, Vector3.left, halfWidth);
            if (EditorGUI.EndChangeCheck())
            {
                halfWidth = Mathf.Max(0f, halfWidth);
            }

            EditorGUI.BeginChangeCheck();
            halfHeight = SliderLineHandle(farEnd, Vector3.up, halfHeight);
            halfHeight = SliderLineHandle(farEnd, Vector3.down, halfHeight);
            if (EditorGUI.EndChangeCheck())
            {
                halfHeight = Mathf.Max(0f, halfHeight);
            }

            return new Vector4(halfWidth * 2f, halfHeight * 2f, maxRange, minRange);
        }
    }
}
