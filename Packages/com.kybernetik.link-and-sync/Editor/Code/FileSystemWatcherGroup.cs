// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public class FileSystemWatcherGroup : IDisposable
    {
        /************************************************************************************************************************/

        private readonly List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

        private LinkAndSync _Link;

        /************************************************************************************************************************/

        public void Initialize(LinkAndSync link)
        {
            _Link = link;

            var count = 0;

            for (int i = 0; i < link.ExternalDirectories.Count; i++)
            {
                var path = link.ExternalDirectories[i];
                if (!LasUtilities.ValidateExternalDirectory(path, false))
                    continue;

                if (Watchers.Count > count)
                {
                    Watchers[count].Path = path;
                }
                else
                {
                    var watcher = new FileSystemWatcher(path);
                    watcher.Changed += DelayOnFileModified;
                    watcher.Created += DelayOnFileModified;
                    watcher.Deleted += DelayOnFileModified;
                    watcher.Renamed += DelayOnFileModified;
                    watcher.Error += (sender, error) =>
                    {
                        Debug.LogError(
                            $"{Strings.LogPrefix}{LasUtilities.TagAsBoldColored(path, link.GetTextColor())}:" +
                            $" {nameof(FileSystemWatcher)} error: {error.GetException()}");
                    };
                    watcher.IncludeSubdirectories = true;
                    watcher.EnableRaisingEvents = true;
                    Watchers.Add(watcher);
                }

                count++;
            }

            DisposeSpares(count);
        }

        /************************************************************************************************************************/

        private void DelayOnFileModified(object sender, FileSystemEventArgs e)
        {
            EditorApplication.delayCall -= _Link.OnFileChangeDetected;
            EditorApplication.delayCall += _Link.OnFileChangeDetected;
        }

        /************************************************************************************************************************/

        public void Dispose()
        {
            DisposeSpares(0);
        }

        /************************************************************************************************************************/

        private void DisposeSpares(int count)
        {
            if (count >= Watchers.Count)
                return;

            for (int i = count; i < Watchers.Count; i++)
                Watchers[i].Dispose();

            Watchers.RemoveRange(count, Watchers.Count - count);
        }

        /************************************************************************************************************************/
    }
}

#endif