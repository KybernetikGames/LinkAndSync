// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public static class MenuFunctions
    {
        /************************************************************************************************************************/

        public const string
            MenuPrefix = "Assets/Link and Sync/",
            ExecuteAllFunction = MenuPrefix + "Execute All Links",
            ExecuteSelectedFunction = MenuPrefix + "Execute Selected Link",
            SyncSelectedFunction = MenuPrefix + "Sync Selected Link",
            ExcludeSelectionFunction = MenuPrefix + "Exclude Selection from Link";

        public const int
            MenuPriority = 100,
            ExecuteAllPriority = MenuPriority + 1,
            ExecuteSelectedPriority = MenuPriority + 2,
            SyncSelectedPriority = MenuPriority + 3,
            ExcludeSelectionPriority = MenuPriority + 4;

        /************************************************************************************************************************/

        [MenuItem(ExecuteAllFunction, priority = ExecuteAllPriority)]
        private static void ExecuteAll()
        {
            AssetDatabase.SaveAssets();

            foreach (var link in LinkAndSync.GetAllLinks())
                SyncOperationWindow.Show(new SyncOperation(link), false);
        }

        [MenuItem(ExecuteAllFunction, priority = ExecuteAllPriority, validate = true)]
        private static bool ValidateExecuteAll()
        {
            foreach (var _ in LinkAndSync.GetAllLinks())
                return true;

            return false;
        }

        /************************************************************************************************************************/

        [MenuItem(ExecuteSelectedFunction, priority = ExecuteSelectedPriority)]
        private static void ExecuteSelected()
        {
            AssetDatabase.SaveAssets();

            foreach (var link in LasUtilities.GatherSelectedLinks())
                SyncOperationWindow.Show(new SyncOperation(link), false);
        }

        [MenuItem(ExecuteSelectedFunction, priority = ExecuteSelectedPriority, validate = true)]
        [MenuItem(SyncSelectedFunction, priority = SyncSelectedPriority, validate = true)]
        private static bool ValidateExecuteSelected()
        {
            foreach (var _ in LasUtilities.GatherSelectedLinks())
                return true;

            return false;
        }

        /************************************************************************************************************************/

        [MenuItem(SyncSelectedFunction, priority = SyncSelectedPriority)]
        private static void SyncSelected()
        {
            AssetDatabase.SaveAssets();

            foreach (var link in LasUtilities.GatherSelectedLinks())
                SyncOperationWindow.Show(new SyncOperation(link, SyncDirection.Sync), false);
        }

        /************************************************************************************************************************/

        [MenuItem(ExcludeSelectionFunction, priority = ExcludeSelectionPriority)]
        private static void ExcludeSelection()
        {
            var links = new List<LinkAndSync>();

            foreach (var selected in Selection.objects)
            {
                links.Clear();
                LinkAndSync.GetContainingLinks(selected, out var assetPath, links);

                foreach (var link in links)
                {
                    if (selected == link)
                        continue;

                    var linkRoot = link.DirectoryPath;
                    var relativePath = assetPath.Substring(linkRoot.Length + 1);
                    if (!link.Exclusions.Contains(relativePath))
                        link.Exclusions.Add(relativePath);
                }
            }

            ProjectWindowOverlay.ClearCache();
        }

        [MenuItem(ExcludeSelectionFunction, priority = ExcludeSelectionPriority, validate = true)]
        private static bool ValidateExcludeSelection()
        {
            var links = new List<LinkAndSync>();

            foreach (var selected in Selection.objects)
            {
                LinkAndSync.GetContainingLinks(selected, out _, links);

                foreach (var link in links)
                {
                    if (selected == link)
                        continue;

                    return true;
                }
            }

            return false;
        }

        /************************************************************************************************************************/

        public static void AddFunctions(GenericMenu menu, IEnumerable<LinkAndSync> links)
        {
            AddFunction(menu, ValidateExecuteAll(), ExecuteAllFunction, ExecuteAll);

            foreach (var link in links)
            {
                menu.AddItem(new GUIContent($"{link.Direction} '{link.name}' (Middle Click)"), false, () =>
                {
                    AssetDatabase.SaveAssets();
                    SyncOperationWindow.Show(new SyncOperation(link), false);
                });
            }

            foreach (var link in links)
            {
                if (link.Direction == SyncDirection.Sync)
                    continue;

                menu.AddItem(new GUIContent($"Sync '{link.name}'"), false, () =>
                {
                    AssetDatabase.SaveAssets();
                    SyncOperationWindow.Show(new SyncOperation(link, SyncDirection.Sync), false);
                });
            }

            AddFunction(menu, ValidateExcludeSelection(), ExcludeSelectionFunction, ExcludeSelection);
        }

        private static void AddFunction(GenericMenu menu, bool enabled, string assetsMenuPath, GenericMenu.MenuFunction function)
        {
            var label = new GUIContent(assetsMenuPath.Substring(MenuPrefix.Length));
            if (enabled)
                menu.AddItem(label, false, function);
            else
                menu.AddDisabledItem(label);
        }

        /************************************************************************************************************************/
    }
}

#endif