// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public partial class LinkAndSync
    {
        [CustomEditor(typeof(LinkAndSync), true)]
        public class Editor : UnityEditor.Editor
        {
            /************************************************************************************************************************/

            [NonSerialized] private Texture2D _Icon;

            public LinkAndSync Target => (LinkAndSync)target;

            /************************************************************************************************************************/

            protected virtual void OnEnable()
            {
                _Icon = AssetPreview.GetMiniThumbnail(target);
            }

            /************************************************************************************************************************/

            private static GUIStyle
                _TitleAreaStyle,
                _TitleStyle;

            protected override void OnHeaderGUI()
            {
                var target = Target;

                if (_TitleAreaStyle == null)
                    _TitleAreaStyle = "In BigTitle";
                if (_TitleStyle == null)
                    _TitleStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 26,
                    };

                GUILayout.BeginHorizontal(_TitleAreaStyle);
                {
                    var content = LasUtilities.TempContent(target.name);
                    var iconSize = _TitleStyle.CalcHeight(content, EditorGUIUtility.currentViewWidth);

                    var color = GUI.color;
                    GUI.color = target.GetPrimaryColor();

                    GUILayout.Label(_Icon, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                    GUI.color = color;

                    GUILayout.Label(content, _TitleStyle);
                }
                GUILayout.EndHorizontal();
            }

            /************************************************************************************************************************/

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                DoPrepareExecuteButton();
                DoNameMismatchGUI();
                DoCreateSettingsGUI();
                DoOtherAssetsGUI();
            }

            /************************************************************************************************************************/

            private void DoPrepareExecuteButton()
            {
                var target = Target;

                var label = !LasSettings.ShowConfirmationWindow || SyncOperationWindow.IsShowing(target)
                    ? "Execute "
                    : "Prepare ";
                label += target.Direction;
                var content = LasUtilities.TempContent(label,
                    "• Click again when prepared to execute." +
                    "\n• Right Click for more functions." +
                    "\n• Middle Click the Project window icon to execute.");

                if (GUILayout.Button(content))
                {
                    switch (Event.current.button)
                    {
                        case 0:
                        default:
                            Execute();
                            break;

                        case 1:
                            ShowSyncContextMenu();
                            break;
                    }
                }
            }

            /************************************************************************************************************************/

            private void ShowSyncContextMenu()
            {
                var target = Target;

                var menu = new GenericMenu();

                var direction = target.Direction.ToString();
                menu.AddItem(new GUIContent(direction), false, Execute);

                menu.AddItem(new GUIContent("Force " + direction), false,
                    () => SyncOperationWindow.Show(new SyncOperation(Target, true)));

                if (target.Direction != SyncDirection.Sync)
                    menu.AddItem(new GUIContent(nameof(SyncDirection.Sync)), false,
                        () => SyncOperationWindow.Show(new SyncOperation(Target, SyncDirection.Sync)));

                menu.ShowAsContext();
            }

            /************************************************************************************************************************/

            private void Execute()
                => SyncOperationWindow.Show(new SyncOperation(Target));

            /************************************************************************************************************************/

            private void DoNameMismatchGUI()
            {
                var target = Target;
                var directoryName = Path.GetFileName(target.DirectoryPath);
                if (target.name == directoryName)
                    return;

                GUILayout.BeginVertical(GUI.skin.box);

                var content = LasUtilities.TempContent(
                    "This link's name doesn't match its current directory.",
                    "The name doesn't actually matter, but it's often cleaner to give it the same name as its directory.");
                GUILayout.Label(content, EditorStyles.wordWrappedLabel);

                if (GUILayout.Button($"Rename to '{directoryName}'"))
                {
                    AssetDatabase.RenameAsset(target.AssetPath, directoryName);
                }

                if (GUILayout.Button($"Move into '{target.name}'"))
                {
                    var assetPath = target.AssetPath;
                    var directoryPath = Path.GetDirectoryName(assetPath);
                    var newDirectory = Path.Combine(directoryPath, target.name);
                    AssetDatabase.CreateFolder(directoryPath, target.name);

                    var fileName = Path.GetFileName(assetPath);
                    AssetDatabase.MoveAsset(assetPath, Path.Combine(newDirectory, fileName));
                }

                GUILayout.EndVertical();
            }

            /************************************************************************************************************************/

            private void DoCreateSettingsGUI()
            {
                if (LasSettings.Instance != null)
                    return;

                EditorGUILayout.HelpBox(
                    $"Unable to find a {nameof(LasSettings)} asset. Click here to create one.", MessageType.Error);

                if (LasUtilities.TryUseClickEventInLastRect())
                    LasSettings.Create(target);
            }

            /************************************************************************************************************************/

            private void DoOtherAssetsGUI()
            {
                EditorGUILayout.HelpBox(
                    $"If you find {Strings.ProductName} useful and would like to support the developer," +
                    $" click here to check out my other assets.", MessageType.Info);

                if (LasUtilities.TryUseClickEventInLastRect())
                    Application.OpenURL(Strings.DeveloperWebsite);
            }

            /************************************************************************************************************************/
        }
    }
}

#endif