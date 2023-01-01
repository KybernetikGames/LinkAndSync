// Link & Sync // Copyright 2023 Kybernetik //

using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public static class LinkColors
    {
        /************************************************************************************************************************/

        public static bool IsDarkTheme => EditorGUIUtility.isProSkin;

        /************************************************************************************************************************/

        public static Color GetPrimaryColor(float hue)
            => Color.HSVToRGB(hue, 1, 1);

        public static Color GetTextColor(float hue)
            => Color.Lerp(GetPrimaryColor(hue), EditorStyles.label.normal.textColor, 0.5f);

        /************************************************************************************************************************/

        public static Color GetPrimaryColor(this LinkAndSync link)
            => GetPrimaryColor(link.Color);

        public static Color GetTextColor(this LinkAndSync link)
            => GetTextColor(link.Color);

        /************************************************************************************************************************/

        public static float GenerateHue(object obj)
        {
            const int Steps = 100;
            var hash = obj != null ? obj.GetHashCode() : 0;
            hash *= (int)(int.MaxValue * 0.99f);// Scatter nearby values far apart using integer wraparound.
            hash %= Steps;
            if (hash < 0)
                hash += Steps;
            return hash / (float)Steps;
        }

        /************************************************************************************************************************/

        public class HueAttribute : PropertyAttribute
        {
            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            [CustomPropertyDrawer(typeof(HueAttribute), true)]
            public class Drawer : PropertyDrawer
            {
                /************************************************************************************************************************/

                public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
                {
                    var sliderArea = area;
                    area.width = EditorGUIUtility.labelWidth + area.height * 2;
                    sliderArea.xMin = area.xMax + EditorGUIUtility.standardVerticalSpacing;

                    var hue = property.floatValue;

                    EditorGUI.BeginChangeCheck();

                    var color = GetPrimaryColor(hue);
                    color = EditorGUI.ColorField(area, label, color, false, false, false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Color.RGBToHSV(color, out hue, out _, out _);
                        property.floatValue = hue;
                        EditorApplication.RepaintProjectWindow();
                    }

                    EditorGUI.BeginChangeCheck();

                    var indentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    hue = EditorGUI.Slider(sliderArea, hue, 0, 1);

                    EditorGUI.indentLevel = indentLevel;

                    if (EditorGUI.EndChangeCheck())
                    {
                        property.floatValue = hue;
                        EditorApplication.RepaintProjectWindow();
                    }

                    if (hue == 1)
                    {
                        property.floatValue = 0;
                    }
                    else if (float.IsNaN(hue))
                    {
                        property.floatValue = GenerateHue(property.serializedObject.targetObject);
                    }
                }

                /************************************************************************************************************************/
            }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}