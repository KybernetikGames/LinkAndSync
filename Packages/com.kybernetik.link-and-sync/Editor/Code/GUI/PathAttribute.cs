// Link & Sync // Copyright 2023 Kybernetik //

using System.IO;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public class ExternalPathAttribute : PathAttribute
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        protected override bool Validate(SerializedProperty property, ref string path)
        {
            if (LasUtilities.IsInsideThisProject(path))
                return false;

            path = LasUtilities.AbsoluteToRelative(path);
            return true;
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }

    public class InternalPathAttribute : PathAttribute
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        protected override bool IsInternal => true;

        /************************************************************************************************************************/

        protected override bool Validate(SerializedProperty property, ref string path)
        {
            if (Path.IsPathRooted(path))
            {
                if (!LasUtilities.IsInsideThisProject(path))
                    return false;
            }
            else if (path.StartsWith(".."))
            {
                return false;
            }

            path = LasUtilities.AbsoluteToRelative(path);
            return true;
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }

    public class RelativePathAttribute : InternalPathAttribute
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        protected override bool Validate(SerializedProperty property, ref string path)
        {
            if (path.StartsWith(".."))
                return false;

            path = path.NormalizeSlashes();

            var targetObjects = property.serializedObject.targetObjects;
            if (targetObjects.Length != 1)
                return false;

            var assetPath = AssetDatabase.GetAssetPath(targetObjects[0]);
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var directoryPath = Path.GetDirectoryName(assetPath).NormalizeSlashes();
            if (string.IsNullOrEmpty(directoryPath))
                return false;

            if (Path.IsPathRooted(path))
                directoryPath = LasUtilities.RelativeToAbsolute(directoryPath);

            if (!path.StartsWith(directoryPath) ||
                path.Length <= directoryPath.Length + 1)
                return false;

            path = path.Substring(directoryPath.Length + 1);
            return true;
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }

    public class PathAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        public bool IsFile { get; set; }

        /************************************************************************************************************************/

        protected virtual bool IsInternal => false;

        protected virtual bool Validate(SerializedProperty property, ref string path) => true;

        /************************************************************************************************************************/

        [CustomPropertyDrawer(typeof(PathAttribute), true)]
        public class Drawer : PropertyDrawer
        {
            /************************************************************************************************************************/

            private static readonly GUIContent BrowseContent = new GUIContent("…",
                "• Right Click to show the target in your File Explorer." +
                "\n• Drag and drop from File Explorer to set a path.");

            private static float _BrowseButtonWidth;

            /************************************************************************************************************************/

            private PathAttribute Attribute
                => (PathAttribute)attribute;

            /************************************************************************************************************************/

            public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
            {
                if (_BrowseButtonWidth <= 0)
                    GUI.skin.button.CalcMinMaxWidth(BrowseContent, out _, out _BrowseButtonWidth);

                area.height = EditorGUIUtility.singleLineHeight;
                var buttonArea = new Rect(area.xMax - _BrowseButtonWidth, area.y, _BrowseButtonWidth, area.height);
                area.width -= _BrowseButtonWidth + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.BeginChangeCheck();

                var value = CheckDragAndDrop(area, property);
                if (value == null)
                    value = property.stringValue;

                value = EditorGUI.TextField(area, value);

                if (EditorGUI.EndChangeCheck())
                {
                    if (Attribute.Validate(property, ref value))
                        property.stringValue = value;
                }

                if (GUI.Button(buttonArea, BrowseContent))
                {
                    switch (Event.current.button)
                    {
                        default:
                        case 0:
                            property = property.Copy();
                            EditorApplication.delayCall += () =>// Browsing causes errors if done inside the Inspector GUI.
                            {
                                var attribute = Attribute;

                                var target = attribute.IsInternal
                                    ? "Internal"
                                    : "External";

                                var found = attribute.IsFile
                                    ? LasUtilities.BrowseForRelativeFile($"Select {target} File", ref value)
                                    : LasUtilities.BrowseForRelativeDirectory($"Select {target} Directory", ref value);

                                if (found && attribute.Validate(property, ref value))
                                {
                                    property.stringValue = value;
                                    property.serializedObject.ApplyModifiedProperties();
                                }
                            };
                            break;

                        case 1:
                            if (LasUtilities.Exists(value))
                                EditorUtility.RevealInFinder(value);
                            break;
                    }
                }
            }

            /************************************************************************************************************************/

            private string CheckDragAndDrop(Rect area, SerializedProperty property)
            {
                var currentEvent = Event.current;
                switch (currentEvent.type)
                {
                    default:
                        return null;

                    case EventType.DragUpdated:
                    case EventType.MouseDrag:
                    case EventType.DragPerform:
                        break;
                }

                if (DragAndDrop.paths.Length != 1 ||
                    !area.Contains(currentEvent.mousePosition))
                    return null;

                var draggedPath = DragAndDrop.paths[0];
                var originalDraggedPath = draggedPath;

                if (!Attribute.Validate(property, ref draggedPath))
                    return null;

                if (currentEvent.type != EventType.DragPerform)// Drag.
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    return null;
                }
                else// Drop.
                {
                    GUI.changed = true;
                    return LasUtilities.AbsoluteToRelative(originalDraggedPath);
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}