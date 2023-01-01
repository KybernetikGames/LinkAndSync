// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public class SyncOperationWindow : EditorWindow
    {
        /************************************************************************************************************************/

        public static readonly AutoPrefs.EditorBool
            IsMaximized = new AutoPrefs.EditorBool($"{nameof(LinkAndSync)}.{nameof(IsMaximized)}", true);

        /************************************************************************************************************************/

        public static void Show(SyncOperation operation, bool executeIfAlreadyShowing = true)
        {
            if (executeIfAlreadyShowing && !LasSettings.ShowConfirmationWindow)
            {
                operation.Execute();
                return;
            }

            var window = GetWindow<SyncOperationWindow>(typeof(SceneView));

            if (window._Operations.Count == 0)
            {
                window.maximized = IsMaximized;
            }
            else
            {
                foreach (var existingOperation in window._Operations)
                {
                    if (operation.Link == existingOperation.Link)
                    {
                        if (executeIfAlreadyShowing && operation.Equals(existingOperation))
                        {
                            operation.Execute();
                            window.Close();
                            return;
                        }
                        else
                        {
                            window._Operations.Remove(existingOperation);
                            break;
                        }
                    }
                }
            }

            window._Operations.Add(operation);
            window._HasDrawnGUI = false;
            window.Focus();
        }

        /************************************************************************************************************************/

        public static bool IsShowing(LinkAndSync link)
        {
            if (Instance == null)
                return false;

            foreach (var operation in Instance._Operations)
                if (operation.Link == link)
                    return true;

            return false;
        }

        /************************************************************************************************************************/

        private static readonly GUILayoutOption[]
            DontExpandWidth = { GUILayout.ExpandWidth(false) };

        public static SyncOperationWindow Instance { get; private set; }

        [SerializeField] private List<SyncOperation> _Operations;
        [SerializeField] private Vector2 _Scroll;
        [SerializeField] private bool _HasDrawnGUI;

        [NonSerialized] private SerializedProperty _OperationsProperty;

        /************************************************************************************************************************/

        private void OnEnable()
        {
            Instance = this;

            var icon = LinkColors.IsDarkTheme
                ? LasSettings.WindowIconLight
                : LasSettings.WindowIconDark;

            titleContent = new GUIContent(Strings.ProductName, icon);

            if (_Operations == null)
                _Operations = new List<SyncOperation>();

            var serializedObject = new SerializedObject(this);
            _OperationsProperty = serializedObject.FindProperty(nameof(_Operations));
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;

            if (_OperationsProperty != null)
            {
                _OperationsProperty.serializedObject.Dispose();
                _OperationsProperty = null;
            }
        }

        /************************************************************************************************************************/

        private void OnGUI()
        {
            EditorGUIUtility.wideMode = EditorGUIUtility.currentViewWidth > 300;

            DoSyncOperationGUI();
            DoFinalButtonGUI();
        }

        /************************************************************************************************************************/

        private void DoSyncOperationGUI()
        {
            if (_OperationsProperty == null)
                return;

            _Scroll = EditorGUILayout.BeginScrollView(_Scroll);

            _OperationsProperty.serializedObject.Update();

            var property = _OperationsProperty.Copy();

            property.Next(true);
            property.Next(true);
            property.Next(true);

            var operationsDepth = property.depth;

            // Array Size.

            var color = GUI.color;

            while (property.depth >= operationsDepth && property.Next(true))
            {
                // Link.
                DoLinkGUI(property, color);

                property.Next(false);

                // Delete.
                if (!_HasDrawnGUI)
                    property.isExpanded = true;
                EditorGUILayout.PropertyField(property);

                property.Next(false);

                // Copy.
                if (!_HasDrawnGUI)
                    property.isExpanded = true;
                DoCopyOperationGUI(property);

                // Other Properties.
                var linkDepth = property.depth;
                while (property.Next(false) && property.depth == linkDepth)
                {
                    EditorGUILayout.PropertyField(property);
                }
            }

            GUI.color = color;
            _HasDrawnGUI = true;

            _OperationsProperty.serializedObject.ApplyModifiedProperties();

            EditorGUILayout.EndScrollView();
        }

        /************************************************************************************************************************/

        private void DoLinkGUI(SerializedProperty property, Color guiColor)
        {
            GUILayout.BeginHorizontal();

            GUI.color = property.objectReferenceValue is LinkAndSync link
                ? Color.Lerp(guiColor, link.GetPrimaryColor(), 0.5f)
                : guiColor;

            EditorGUILayout.PropertyField(property);

            var isMaximized = GUILayout.Toggle(maximized, "Maximize", EditorStyles.miniButton, DontExpandWidth);
            if (IsMaximized.Value != isMaximized)
            {
                IsMaximized.Value = maximized = isMaximized;
            }

            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        private void DoCopyOperationGUI(SerializedProperty property)
        {
            var copyFrom = property.Copy();
            if (!property.Next(false))
                return;

            var copyTo = property;

            var isExpanded = copyFrom.isExpanded || copyTo.isExpanded;
            copyFrom.isExpanded = copyTo.isExpanded = isExpanded;

            var arraySize = Mathf.Max(copyFrom.arraySize, copyTo.arraySize);

            EditorGUI.BeginChangeCheck();

            var labelWidth = EditorGUIUtility.labelWidth;
            try
            {
                EditorGUIUtility.labelWidth = float.Epsilon;

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                EditorGUILayout.PropertyField(copyFrom);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                EditorGUILayout.PropertyField(copyTo);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUIUtility.labelWidth = labelWidth;
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (copyFrom.isExpanded != isExpanded)
                    copyTo.isExpanded = copyFrom.isExpanded;
                if (copyTo.isExpanded != isExpanded)
                    copyFrom.isExpanded = copyTo.isExpanded;

                if (copyFrom.arraySize != arraySize)
                    copyTo.arraySize = copyFrom.arraySize;
                if (copyTo.arraySize != arraySize)
                    copyFrom.arraySize = copyTo.arraySize;
            }
        }

        /************************************************************************************************************************/

        private static readonly GUILayoutOption[]
            ExpandHeightNotWidth = { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true) };

        private static GUIStyle _LargeButtonStyle;

        private void DoFinalButtonGUI()
        {
            if (_LargeButtonStyle == null)
            {
                _LargeButtonStyle = new GUIStyle(GUI.skin.button);
                _LargeButtonStyle.fontSize *= 2;
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            EditorGUILayout.HelpBox(
                "You must regularly backup your files to avoid data loss (which is recommended even without this plugin)." +
                "\nKybernetik cannot be held responsible for any loss of data.",
                MessageType.Info);

            GUILayout.FlexibleSpace();

            var wasEnabled = GUI.enabled;
            var isEnabled = false;
            foreach (var operation in _Operations)
            {
                if (!operation.IsEmpty)
                {
                    isEnabled = true;
                    break;
                }
            }
            GUI.enabled = isEnabled;

            if (GUILayout.Button("Execute", _LargeButtonStyle, ExpandHeightNotWidth))
            {
                if (isEnabled)
                {
                    Event.current.Use();
                    foreach (var operation in _Operations)
                        operation.Execute();
                    Close();
                }
            }

            GUI.enabled = wasEnabled;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", _LargeButtonStyle, ExpandHeightNotWidth))
            {
                Close();
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        protected virtual void OnDestroy()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/
    }
}

#endif