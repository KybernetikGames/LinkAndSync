// Link & Sync // Copyright 2023 Kybernetik //

using System;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public class DateTimeTicksAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(DateTimeTicksAttribute), true)]
        public class Drawer : PropertyDrawer
        {
            /************************************************************************************************************************/

            public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
            {
                label = EditorGUI.BeginProperty(area, label, property);

                EditorGUI.BeginChangeCheck();

                var dateTime = new DateTime(property.longValue, DateTimeKind.Utc).ToLocalTime();
                var text = EditorGUI.DelayedTextField(area, label, dateTime.ToString());

                if (EditorGUI.EndChangeCheck())
                {
                    if (DateTime.TryParse(text, out dateTime))
                        property.longValue = dateTime.ToUniversalTime().Ticks;
                    else if (long.TryParse(text, out var ticks))
                        property.longValue = ticks;
                }

                EditorGUI.EndProperty();
            }

            /************************************************************************************************************************/
        }
#endif
    }
}