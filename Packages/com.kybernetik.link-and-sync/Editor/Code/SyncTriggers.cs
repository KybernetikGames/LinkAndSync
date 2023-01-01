// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public static class SyncTriggers
    {
        /************************************************************************************************************************/

        private static IEnumerable<LinkAndSync> AllLinks
            => LinkAndSync.GetAllLinks();

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.EnteredEditMode)
                    CheckSyncTriggers();
            };

            AssetDatabaseWatcher.OnPostprocessAssets += delegate
            {
                EditorApplication.delayCall -= CheckSyncTriggers;
                EditorApplication.delayCall += CheckSyncTriggers;
            };
        }

        /************************************************************************************************************************/

        public static void CheckSyncTriggers()
        {
            foreach (var link in AllLinks)
                link.OnFileChangeDetected();
        }

        /************************************************************************************************************************/

        public static void OnFileChangeDetected(LinkAndSync link)
        {
            switch (link.Trigger)
            {
                case SyncTrigger.Automatic:
                    if (link.EncounteredError ||
                        EditorApplication.isCompiling)
                        break;

                    var operation = new SyncOperation(link);
                    if (!operation.IsEmpty &&
                        AskIfAutomaticSyncIsAllowed(operation) &&
                        !HasAutomaticallySyncedTooMuch(link))
                        operation.Execute();

                    break;

                case SyncTrigger.Notify:
                    if (!new SyncOperation(link).IsEmpty)
                    {
                        if (!link.IsOutOfDate)
                        {
                            link.IsOutOfDate = true;
                            EditorApplication.RepaintProjectWindow();

                            if (LasSettings.NotifyViaLog)
                            {
                                var text = new StringBuilder();
                                text.Append(Strings.LogPrefix);
                                LasUtilities.AppendBoldColored(text, link.GetTextColor(), link.name);
                                text.Append(" is out of date and needs to be synchronized.");
                                Debug.LogWarning(text, link);
                            }
                        }
                    }
                    else
                    {
                        link.IsOutOfDate = false;
                    }
                    break;

                case SyncTrigger.Manual:
                default:
                    break;
            }
        }

        /************************************************************************************************************************/

        public static readonly AutoPrefs.SessionBool
            HasAskedToSyncAutomatically = $"{nameof(LinkAndSync)}.{nameof(HasAskedToSyncAutomatically)}";

        private static bool AskIfAutomaticSyncIsAllowed(SyncOperation operation)
        {
            if (SyncOperationWindow.IsShowing(operation.Link))
            {
                SyncOperationWindow.Show(operation, false);
                return false;
            }

            if (!LasSettings.EnableAutomaticWarning)
                return true;

            foreach (var link in AllLinks)
            {
                if (link.Trigger != SyncTrigger.Automatic)
                    continue;

                if (HasAskedToSyncAutomatically)
                    return true;

                var message = BuildAutomaticSyncMessage();
                var result = EditorUtility.DisplayDialogComplex(
                    Strings.ProductName,
                    message,
                    "Allow Automatic Sync",
                    "Disable Automatic Sync",
                    "Examine Sync Operation");

                switch (result)
                {
                    case 0:
                        HasAskedToSyncAutomatically.Value = true;
                        return true;

                    default:
                    case 1:
                        DisableAutomaticSync();
                        return false;

                    case 2:
                        SyncOperationWindow.Show(operation, false);
                        return false;
                }
            }

            return true;
        }

        /************************************************************************************************************************/

        private static string BuildAutomaticSyncMessage()
        {
            var text = new StringBuilder();

            text.AppendLine($"The following links are using {nameof(SyncTrigger.Automatic)} Triggers:");

            foreach (var link in AllLinks)
                if (link.Trigger == SyncTrigger.Automatic)
                    text.Append(" - ").AppendLine(link.name);

            text.AppendLine()
                .Append("This warning will be shown every time you open Unity unless disabled in ");

            if (LasSettings.Instance != null)
                text.Append(AssetDatabase.GetAssetPath(LasSettings.Instance).NormalizeSlashes())
                    .Append('.');
            else
                text.Append(" the " + nameof(LasSettings) + " asset which can be created via the Inspector of any link.");

            return text.ToString();
        }

        /************************************************************************************************************************/

        public static readonly AutoPrefs.SessionInt
            ConsecutiveAutomaticSyncCount = $"{nameof(LinkAndSync)}.{nameof(ConsecutiveAutomaticSyncCount)}";
        private static readonly TimeSpan
            ConsecutiveSyncPeriod = TimeSpan.FromSeconds(1);

        private static bool HasAutomaticallySyncedTooMuch(LinkAndSync link)
        {
            if (ConsecutiveAutomaticSyncCount == int.MinValue)
                return false;

            var lastSync = new DateTime(SyncOperation.LastExecutedTicks, DateTimeKind.Utc);
            var timeOut = lastSync.AddSeconds(1);

            var now = DateTime.UtcNow;
            if (now > timeOut)// If enough time has passed, reset the counter.
            {
                ConsecutiveAutomaticSyncCount.Value = 0;
                return false;
            }

            ConsecutiveAutomaticSyncCount.Value++;

            if (ConsecutiveAutomaticSyncCount < 7)
                return false;

            var continueAllowing = EditorUtility.DisplayDialog(
                Strings.ProductName,
                "Repeated automatic synchronizations have been executed in a short period of time" +
                " and could potentially be stuck in an infinite loop." +
                "\n\nContinue allowing automatic synchronization?",
                "Continue",
                "Disable Automatic Sync");

            if (continueAllowing)
            {
                ConsecutiveAutomaticSyncCount.Value = int.MinValue;// Don't ask again.
                return false;
            }
            else
            {
                DisableAutomaticSync();
                return true;
            }
        }

        /************************************************************************************************************************/

        private static void DisableAutomaticSync()
        {
            foreach (var link in AllLinks)
                if (link.Trigger == SyncTrigger.Automatic)
                    link.Trigger = SyncTrigger.Notify;
        }

        /************************************************************************************************************************/
    }
}

#endif