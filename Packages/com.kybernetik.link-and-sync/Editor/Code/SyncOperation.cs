// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

//#define LINK_AND_SYNC_LOG

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    [Serializable]
    public class SyncOperation
    {
        /************************************************************************************************************************/

        [SerializeField]
        private LinkAndSync _Link;

#if UNITY_2020_3_OR_NEWER
        [NonReorderable]
#endif
        [SerializeField]
        private List<string> _Delete, _CopyFrom, _CopyTo, _SynchronizedPaths;

        [NonSerialized] private string _LinkPath;
        [NonSerialized] private string _LinkMetaPath;
        [NonSerialized] private string _LinkDirectory;
        [NonSerialized] private SyncDirection _SyncDirection;

        // If a Copy From is empty, that means the corresponding Copy To is a directory to create.

        public const string
            DeleteField = nameof(_Delete),
            CopyFromField = nameof(_CopyFrom),
            CopyToField = nameof(_CopyTo);

        /************************************************************************************************************************/

        public LinkAndSync Link => _Link;

        /************************************************************************************************************************/

        public bool IsEmpty =>
            _Delete.GetSafeCount() == 0 &&
            _CopyTo.GetSafeCount() == 0 &&
            LasUtilities.Equals(_SynchronizedPaths, _Link.SynchronizedPaths);

        /************************************************************************************************************************/

        public SyncOperation(LinkAndSync link, bool force = false)
            : this(link, link.Direction, force)
        {
        }

        public SyncOperation(LinkAndSync link, SyncDirection direction, bool force = false)
        {
            AssetDatabase.SaveAssets();

            try
            {
                _Link = link;
                _LinkPath = _Link.AssetPath;
                _LinkMetaPath = _LinkPath + ".meta";
                _LinkDirectory = Path.GetDirectoryName(_LinkPath);
                _SyncDirection = direction;

                if (_Link.ExternalDirectories == null ||
                    _Link.ExternalDirectories.Count == 0)
                    return;

                GatherPathsToDelete();
                GatherPathsToCopy(force);
            }
            catch
            {
                _Link.EncounteredError = true;
                throw;
            }
        }

        /************************************************************************************************************************/

        public bool IsTargetLink(string path)
            => path == _LinkPath || path == _LinkMetaPath;

        /************************************************************************************************************************/

        public bool Equals(SyncOperation operation) =>
            operation != null &&
            _Link == operation._Link &&
            _LinkPath == operation._LinkPath &&
            _LinkMetaPath == operation._LinkMetaPath &&
            _LinkDirectory == operation._LinkDirectory &&
            LasUtilities.Equals(_Delete, operation._Delete) &&
            LasUtilities.Equals(_CopyFrom, operation._CopyFrom) &&
            LasUtilities.Equals(_CopyTo, operation._CopyTo) &&
            LasUtilities.Equals(_SynchronizedPaths, operation._SynchronizedPaths);

        /************************************************************************************************************************/
        #region Delete
        /************************************************************************************************************************/

        private void GatherPathsToDelete()
        {
            _Delete = new List<string>();

            if (_Link.SynchronizedPaths.GetSafeCount() == 0)
                return;

            switch (_SyncDirection)
            {
                case SyncDirection.Pull:
                    GatherPathsToDelete_Pull();
                    break;

                case SyncDirection.Push:
                    GatherPathsToDelete_Push();
                    break;

                case SyncDirection.Sync:
                    GatherPathsToDelete_Sync();
                    break;

                default:
                    throw new ArgumentException($"Unhandled {nameof(SyncDirection)}: {_SyncDirection}");
            }

            LasUtilities.SortLongestFirst(_Delete);
        }

        /************************************************************************************************************************/

        /// <summary>Delete any internal files that no longer exist externally.</summary>
        private void GatherPathsToDelete_Pull()
        {
            foreach (var path in _Link.SynchronizedPaths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var relativePath = path.NormalizeSlashes();

                var exists = false;
                if (!_Link.IsExcluded(relativePath))
                {
                    foreach (var externalDirectory in _Link.ExternalDirectories)
                    {
                        var externalPath = Path.Combine(externalDirectory, relativePath);
                        if (LasUtilities.Exists(externalPath))
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                if (!exists)
                {
                    var internalFile = Path.Combine(_LinkDirectory, relativePath)
                        .NormalizeSlashes();

                    if (!IsTargetLink(internalFile))
                        _Delete.Add(internalFile);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Delete any external paths that no longer exist internally.</summary>
        private void GatherPathsToDelete_Push()
        {
            foreach (var path in _Link.SynchronizedPaths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var relativePath = path.NormalizeSlashes();

                if (!_Link.IsExcluded(relativePath))
                {
                    var internalPath = Path.Combine(_LinkDirectory, relativePath)
                        .NormalizeSlashes();

                    if (IsTargetLink(internalPath) ||
                        LasUtilities.Exists(internalPath))
                        continue;
                }

                foreach (var externalDirectory in _Link.ExternalDirectories)
                {
                    _Delete.Add(Path.Combine(externalDirectory, relativePath));
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Delete paths in all locations if one of them no longer exists and none were modified since last sync.
        /// </summary>
        private void GatherPathsToDelete_Sync()
        {
            var absolutePaths = new List<string>();

            foreach (var path in _Link.SynchronizedPaths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var relativePath = path.NormalizeSlashes();
                var internalPath = Path.Combine(_LinkDirectory, relativePath)
                    .NormalizeSlashes();

                if (IsTargetLink(internalPath))
                    continue;

                var isExcluded = _Link.IsExcluded(relativePath);

                absolutePaths.Clear();

                absolutePaths.Add(internalPath);
                foreach (var externalDirectory in _Link.ExternalDirectories)
                    absolutePaths.Add(Path.Combine(externalDirectory, relativePath));

                var modifiedSinceLastSync = false;
                var anyDeleted = false;

                foreach (var absolutePath in absolutePaths)
                {
                    if (!isExcluded && LasUtilities.Exists(absolutePath))
                    {
                        if (File.GetLastWriteTimeUtc(absolutePath).Ticks > _Link.LastExecuted)
                        {
                            modifiedSinceLastSync = true;
                            break;
                        }
                    }
                    else
                    {
                        anyDeleted = true;
                    }
                }

                // If any one doesn't exist and none were modified, delete them all.

                if (!modifiedSinceLastSync && anyDeleted)
                    foreach (var absolutePath in absolutePaths)
                        if (LasUtilities.Exists(absolutePath))
                            _Delete.Add(absolutePath.NormalizeSlashes());
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Copy
        /************************************************************************************************************************/

        private void GatherPathsToCopy(bool force)
        {
            var destinationToSource = new Dictionary<string, string>();
            var synchronizedPaths = new HashSet<string>();

            switch (_SyncDirection)
            {
                case SyncDirection.Pull:
                    GatherPathsToCopy_Pull(force, destinationToSource, synchronizedPaths);
                    break;

                case SyncDirection.Push:
                    GatherPathsToCopy_Push(force, destinationToSource, synchronizedPaths);
                    break;

                case SyncDirection.Sync:
                    GatherPathsToCopy_Sync(force, destinationToSource, synchronizedPaths);
                    break;

                default:
                    throw new ArgumentException($"Unhandled {nameof(SyncDirection)}: {_SyncDirection}");
            }

            var count = destinationToSource.GetSafeCount();
            var copyFrom = new string[count];
            var copyTo = new string[count];

            destinationToSource.Keys.CopyTo(copyTo, 0);
            destinationToSource.Values.CopyTo(copyFrom, 0);

            for (int i = 0; i < count; i++)
            {
                copyFrom[i] = copyFrom[i].NormalizeSlashes();
                copyTo[i] = copyTo[i].NormalizeSlashes();
            }

            Array.Sort(copyTo, copyFrom);

            _CopyFrom = new List<string>(copyFrom);
            _CopyTo = new List<string>(copyTo);

            _SynchronizedPaths = new List<string>(synchronizedPaths);
            _SynchronizedPaths.Sort();
        }

        /************************************************************************************************************************/

        private void GatherPathsToCopy_Pull(
            bool force,
            Dictionary<string, string> destinationToSource,
            HashSet<string> synchronizedPaths)
        {
            var relativePathToDateModified = new Dictionary<string, DateTime>();

            foreach (var externalRoot in _Link.ExternalDirectories)
            {
                if (!Directory.Exists(externalRoot))
                    continue;

                var externalRootUnslashed = externalRoot.RemoveTrailingSlashes();

                GatherFilesToCopy_Pull(
                    force,
                    destinationToSource,
                    relativePathToDateModified,
                    synchronizedPaths,
                    externalRootUnslashed,
                    externalRootUnslashed);

                var externalDirectories = Directory.GetDirectories(externalRootUnslashed, "*", SearchOption.AllDirectories);
                foreach (var externalDirectory in externalDirectories)
                {
                    var relativeDirectory = externalDirectory
                        .Substring(externalRootUnslashed.Length + 1)
                        .NormalizeSlashes();

                    if (_Link.IsExcluded(relativeDirectory))
                        continue;

                    synchronizedPaths.Add(relativeDirectory);

                    var internalDirectory = Path.Combine(_LinkDirectory, relativeDirectory);
                    if (!Directory.Exists(internalDirectory))
                        destinationToSource[internalDirectory] = "";

                    GatherFilesToCopy_Pull(
                        force,
                        destinationToSource,
                        relativePathToDateModified,
                        synchronizedPaths,
                        externalDirectory,
                        externalRootUnslashed);
                }
            }
        }

        private void GatherFilesToCopy_Pull(
            bool force,
            Dictionary<string, string> destinationToSource,
            Dictionary<string, DateTime> relativePathToDateModified,
            HashSet<string> synchronizedPaths,
            string externalDirectory,
            string externalRoot)
        {
            if (!Directory.Exists(externalDirectory))
                return;

            var externalFiles = Directory.GetFiles(externalDirectory);
            foreach (var externalFile in externalFiles)
            {
                var relativeFile = externalFile
                    .Substring(externalRoot.Length + 1)
                    .NormalizeSlashes();

                if (_Link.IsExcluded(relativeFile))
                    continue;

                synchronizedPaths.Add(relativeFile);

                var externalModified = File.GetLastWriteTimeUtc(externalFile);

                if (relativePathToDateModified.TryGetValue(relativeFile, out var relativeModified) &&
                    relativeModified >= externalModified)
                    continue;

                var internalFile = Path.Combine(_LinkDirectory, relativeFile);
                if (IsTargetLink(internalFile))
                    continue;

                var internalModified = File.GetLastWriteTimeUtc(internalFile);

                if (force || externalModified > internalModified)
                {
                    destinationToSource[internalFile] = externalFile;

                    relativePathToDateModified[relativeFile] = externalModified;
                }
                else
                {
                    relativePathToDateModified[relativeFile] = internalModified;
                }
            }
        }

        /************************************************************************************************************************/

        private void GatherPathsToCopy_Push(
            bool force,
            Dictionary<string, string> destinationToSource,
            HashSet<string> synchronizedPaths)
        {
            if (!Directory.Exists(_LinkDirectory))
                return;

            GatherFilesToCopy_Push(
                force,
                destinationToSource,
                synchronizedPaths,
                _LinkDirectory);

            var internalDirectories = Directory.GetDirectories(_LinkDirectory, "*", SearchOption.AllDirectories);
            foreach (var internalDirectory in internalDirectories)
            {
                var relativeDirectory = internalDirectory
                    .Substring(_LinkDirectory.Length + 1)
                    .NormalizeSlashes();

                if (_Link.IsExcluded(relativeDirectory))
                    continue;

                synchronizedPaths.Add(relativeDirectory);

                foreach (var externalRoot in _Link.ExternalDirectories)
                {
                    var externalDirectory = Path.Combine(externalRoot, relativeDirectory).NormalizeSlashes();
                    if (!Directory.Exists(externalDirectory))
                        destinationToSource[externalDirectory] = "";
                }

                GatherFilesToCopy_Push(
                    force,
                    destinationToSource,
                    synchronizedPaths,
                    internalDirectory);
            }
        }

        private void GatherFilesToCopy_Push(
            bool force,
            Dictionary<string, string> destinationToSource,
            HashSet<string> synchronizedPaths,
            string internalDirectory)
        {
            if (!Directory.Exists(internalDirectory))
                return;

            var internalFiles = Directory.GetFiles(internalDirectory);
            foreach (var internalFile in internalFiles)
            {
                var internalFileNormalized = internalFile
                    .NormalizeSlashes();

                if (IsTargetLink(internalFileNormalized))
                    continue;

                var relativeFile = internalFileNormalized
                    .Substring(_LinkDirectory.Length + 1)
                    .NormalizeSlashes();

                if (_Link.IsExcluded(relativeFile))
                    continue;

                synchronizedPaths.Add(relativeFile);

                var internalModified = File.GetLastWriteTimeUtc(internalFileNormalized);

                foreach (var externalRoot in _Link.ExternalDirectories)
                {
                    var externalFile = Path.Combine(externalRoot, relativeFile);
                    var externalModified = File.GetLastWriteTimeUtc(externalFile);
                    if (force || internalModified > externalModified)
                        destinationToSource[externalFile] = internalFileNormalized;
                }
            }
        }

        /************************************************************************************************************************/

        private void GatherPathsToCopy_Sync(
            bool force,
            Dictionary<string, string> destinationToSource,
            HashSet<string> synchronizedPaths)
        {
            var roots = new List<string>(_Link.ExternalDirectories.GetSafeCount() + 1)
            {
                _LinkDirectory
            };
            roots.AddRange(_Link.ExternalDirectories);

            for (int i = 0; i < roots.Count; i++)
                roots[i] = roots[i].NormalizeSlashes();

            var deleting = new HashSet<string>(_Delete);

            GatherDirectoriesToCopy_Sync(
                destinationToSource,
                synchronizedPaths,
                roots,
                deleting,
                out var relativeDirectories);

            GatherFilesToCopy_Sync(
                force,
                destinationToSource,
                synchronizedPaths,
                roots,
                deleting,
                relativeDirectories);
        }

        private void GatherDirectoriesToCopy_Sync(
            Dictionary<string, string> destinationToSource,
            HashSet<string> synchronizedPaths,
            List<string> roots,
            HashSet<string> deleting,
            out HashSet<string> relativeDirectories)
        {
            // Gather all directories.
            relativeDirectories = new HashSet<string>
            {
                "",
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                    continue;

                var directories = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
                foreach (var directory in directories)
                {
                    var relativeDirectory = directory
                        .Substring(root.Length + 1)
                        .NormalizeSlashes();

                    if (_Link.IsExcluded(relativeDirectory))
                        continue;

                    relativeDirectories.Add(relativeDirectory);
                }
            }

            var absoluteDirectories = new List<string>();

            foreach (var relativeDirectory in relativeDirectories)
            {
                absoluteDirectories.Clear();

                // Ignore any that are being deleted.
                var isDeleting = false;
                foreach (var root in roots)
                {
                    var directory = Path.Combine(root, relativeDirectory)
                        .NormalizeSlashes();

                    if (deleting.Contains(directory))
                    {
                        isDeleting = true;
                        break;
                    }

                    absoluteDirectories.Add(directory);
                }

                if (isDeleting)
                    continue;

                // Otherwise, create any that don't exist.
                foreach (var directory in absoluteDirectories)
                {
                    if (!Directory.Exists(directory))
                        destinationToSource[directory] = "";
                }

                if (!string.IsNullOrEmpty(relativeDirectory))
                    synchronizedPaths.Add(relativeDirectory);

            }
        }

        private void GatherFilesToCopy_Sync(
            bool force,
            Dictionary<string, string> destinationToSource,
            HashSet<string> synchronizedPaths,
            List<string> roots,
            HashSet<string> deleting,
            HashSet<string> relativeDirectories)
        {
            var rootedFiles = new List<string>();

            var relativeFiles = new HashSet<string>();
            foreach (var relativeDirectory in relativeDirectories)
            {
                // Gather files.
                foreach (var root in roots)
                {
                    var directory = Path.Combine(root, relativeDirectory);
                    if (!Directory.Exists(directory))
                        continue;

                    var files = Directory.GetFiles(directory);
                    foreach (var file in files)
                    {
                        var fileNormalized = file
                            .NormalizeSlashes();

                        if (IsTargetLink(fileNormalized))
                            continue;

                        var relativeFile = fileNormalized
                            .Substring(root.Length + 1);

                        if (deleting.Contains(relativeFile) ||
                            _Link.IsExcluded(relativeFile))
                            continue;

                        relativeFiles.Add(relativeFile);
                    }
                }

                // Copy the most recently modified file over all others.
                foreach (var relativeFile in relativeFiles)
                {
                    // Find the newest.
                    string newest = null;
                    var newestModified = DateTime.MinValue;

                    var isDeleting = false;

                    foreach (var root in roots)
                    {
                        var rootedFile = Path.Combine(root, relativeFile)
                            .NormalizeSlashes();

                        if (deleting.Contains(rootedFile))
                        {
                            isDeleting = true;
                            break;
                        }

                        rootedFiles.Add(rootedFile);

                        var modified = File.GetLastWriteTimeUtc(rootedFile);
                        if (newestModified < modified)
                        {
                            newestModified = modified;
                            newest = rootedFile;
                        }
                    }

                    // Copy it over all others.
                    if (!isDeleting)
                    {
                        synchronizedPaths.Add(relativeFile);

                        foreach (var rootedFile in rootedFiles)
                        {
                            var modified = File.GetLastWriteTimeUtc(rootedFile);
                            if (force || newestModified > modified)
                                destinationToSource[rootedFile] = newest;
                        }
                    }

                    rootedFiles.Clear();
                }

                relativeFiles.Clear();
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Execute
        /************************************************************************************************************************/

        public static readonly AutoPrefs.EditorLong
            LastExecutedTicks = $"{nameof(LinkAndSync)}.{nameof(LastExecutedTicks)}";

        /************************************************************************************************************************/

        public void Execute()
        {
            if (IsEmpty)
                return;

            try
            {
                var syncTime = DateTime.UtcNow;

                var copyCount = Math.Min(_CopyFrom.GetSafeCount(), _CopyTo.GetSafeCount());
                var operationCount = _Delete.GetSafeCount() + copyCount;
                var executedOperations = 0;

                ExecuteDelete(ref executedOperations, operationCount);
                ExecuteCopy(copyCount, ref executedOperations, operationCount);

                LastExecutedTicks.Value = syncTime.Ticks;
                _Link.LastExecuted = syncTime.Ticks;
                _Link.SynchronizedPaths = new List<string>(_SynchronizedPaths);
                _Link.IsOutOfDate = false;

                EditorUtility.SetDirty(_Link);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                _Link.EncounteredError = false;
            }
            catch
            {
                _Link.EncounteredError = true;
                throw;
            }
            finally
            {
                LasUtilities.ClearProgress();
            }
        }

        /************************************************************************************************************************/

        private void ExecuteDelete(
            ref int executedOperations,
            float totalOperations)
        {
            if (_Delete == null)
                return;

            foreach (var deletePath in _Delete)
            {
                UpdateProgress("Deleting: " + deletePath, ref executedOperations, totalOperations);

                if (string.IsNullOrEmpty(deletePath))
                    continue;

                if (!deletePath.EndsWith(".meta") &&
                    LasUtilities.IsInsideThisProject(deletePath) &&
                    AssetDatabase.DeleteAsset(deletePath))
                    continue;

                if (File.Exists(deletePath))
                    File.Delete(deletePath);
                else if (Directory.Exists(deletePath))
                    Directory.Delete(deletePath);
            }
        }

        /************************************************************************************************************************/

        private void ExecuteCopy(
            int copyCount,
            ref int executedOperations,
            float totalOperations)
        {
            if (copyCount == 0)
                return;

            for (int i = 0; i < copyCount; i++)
            {
                var from = _CopyFrom[i];
                var to = _CopyTo[i];

                if (string.IsNullOrEmpty(to))
                    continue;

                if (string.IsNullOrEmpty(from))
                {
                    UpdateProgress("Creating Directory: " + to, ref executedOperations, totalOperations);

                    Directory.CreateDirectory(to);
                }
                else if (File.Exists(from))
                {
                    UpdateProgress($"Copying from '{from}' to '{to}'", ref executedOperations, totalOperations);

                    var dateModified = File.GetLastWriteTimeUtc(from);
                    File.Copy(from, to, true);
                    File.SetLastWriteTimeUtc(to, dateModified);
                }
            }
        }

        /************************************************************************************************************************/

        private void UpdateProgress(string message, ref int executedOperations, float totalOperations)
        {
#if LINK_AND_SYNC_LOG
            Debug.Log($"{Strings.LogPrefix}{_Link.name}: {message}", _Link);
#endif

            if (LasUtilities.IsReadyForProgress())
                LasUtilities.DisplayProgressBar(message, executedOperations / totalOperations);

            executedOperations++;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif