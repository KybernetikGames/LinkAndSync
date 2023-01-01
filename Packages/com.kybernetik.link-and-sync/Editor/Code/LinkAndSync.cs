// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LinkAndSync
{
    [CreateAssetMenu]
    public partial class LinkAndSync : ScriptableObject
    {
        /************************************************************************************************************************/
        #region All Links
        /************************************************************************************************************************/

        private static readonly HashSet<LinkAndSync> AllLinks = new HashSet<LinkAndSync>();

        public static IEnumerable<LinkAndSync> GetAllLinks()
        {
            foreach (var link in AllLinks)
                if (AssetDatabase.Contains(link))
                    yield return link;
        }

        /************************************************************************************************************************/

        protected virtual void OnEnable()
        {
            AllLinks.Add(this);

            ProjectWindowOverlay.ClearCache();

            InitialiseWatcher();
        }

        /************************************************************************************************************************/

        protected virtual void Reset()
            => OnValidate();

        protected virtual void OnValidate()
        {
            GenerateColor();
            InitialiseWatcher();

            if (_ExternalDirectories.GetSafeCount() < 1)
            {
                _ExternalDirectories = new List<string>()
                {
                    "",
                };
            }
        }

        /************************************************************************************************************************/

        protected virtual void OnDisable()
        {
            AllLinks.Remove(this);
            LasUtilities.Dispose(ref _ExternalDirectoryWatcher);
            EditorApplication.delayCall -= OnFileChangeDetected;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        [SerializeField, LinkColors.Hue, Tooltip(Strings.DirectoryLink.Color)]
        private float _Color = 1;
        public ref float Color
        {
            get
            {
                GenerateColor();
                return ref _Color;
            }
        }

        [SerializeField, ExternalPath, Tooltip(Strings.DirectoryLink.ExternalDirectories)]
        private List<string> _ExternalDirectories;
        public ref List<string> ExternalDirectories => ref _ExternalDirectories;

        [SerializeField, RelativePath, Tooltip(Strings.DirectoryLink.Exclusions)]
        private List<string> _Exclusions;
        public ref List<string> Exclusions => ref _Exclusions;

        [SerializeField, Tooltip(Strings.DirectoryLink.Direction)]
        private SyncDirection _Direction;
        public ref SyncDirection Direction => ref _Direction;

        [SerializeField, Tooltip(Strings.DirectoryLink.Trigger)]
        private SyncTrigger _Trigger;
        public ref SyncTrigger Trigger => ref _Trigger;

        [SerializeField, DateTimeTicks, Tooltip(Strings.DirectoryLink.LastExecuted)]
        private long _LastExecuted = DateTime.MinValue.Ticks;
        public ref long LastExecuted => ref _LastExecuted;

        [SerializeField, RelativePath, Tooltip(Strings.DirectoryLink.SynchronizedPaths)]
        private List<string> _SynchronizedPaths;

        public ref List<string> SynchronizedPaths => ref _SynchronizedPaths;

        /************************************************************************************************************************/

        /// <summary>An error during execution will disable <see cref="SyncTrigger.Automatic"/>.</summary>
        [field: NonSerialized] public bool EncounteredError { get; set; }

        /// <summary>Only set in <see cref="SyncTrigger.Notify"/>.</summary>
        [field: NonSerialized] public bool IsOutOfDate { get; set; }

        private FileSystemWatcherGroup _ExternalDirectoryWatcher;

        /************************************************************************************************************************/

        public bool IsExcluded(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) ||
                Exclusions == null || Exclusions.Count == 0)
                return false;

            relativePath = relativePath.NormalizeSlashes();

            for (int i = 0; i < Exclusions.Count; i++)
            {
                var exclusion = Exclusions[i] = Exclusions[i].NormalizeSlashes();

                if (relativePath.Length >= exclusion.Length &&
                    string.Compare(relativePath, 0, exclusion, 0, exclusion.Length) == 0)
                {
                    // The exact path is excluded.
                    if (relativePath.Length == exclusion.Length)
                        return true;

                    // The target of a meta file is excluded.
                    if (relativePath.Length == exclusion.Length + 5 && relativePath.EndsWith(".meta"))
                        return true;

                    // The exclusion is a folder which contains the path.
                    if (relativePath[exclusion.Length] == LasUtilities.Slash)
                        return true;
                }
            }

            return false;
        }

        /************************************************************************************************************************/

        private void InitialiseWatcher()
        {
            if (Trigger != SyncTrigger.Manual &&
                ArePathsValid(false) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (_ExternalDirectoryWatcher == null)
                    _ExternalDirectoryWatcher = new FileSystemWatcherGroup();

                _ExternalDirectoryWatcher.Initialize(this);

                EditorApplication.delayCall -= OnFileChangeDetected;
                EditorApplication.delayCall += OnFileChangeDetected;
            }
            else
            {
                _ExternalDirectoryWatcher?.Dispose();
            }
        }

        /************************************************************************************************************************/

        public void OnFileChangeDetected()
            => SyncTriggers.OnFileChangeDetected(this);

        /************************************************************************************************************************/

        public string AssetPath
            => AssetDatabase.GetAssetPath(this).NormalizeSlashes();

        public string DirectoryPath
        {
            get
            {
                var path = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(path))
                    return path;
                else
                    return Path.GetDirectoryName(path).NormalizeSlashes();
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Utilities
        /************************************************************************************************************************/

        private void GenerateColor()
        {
            if (_Color >= 0 && _Color < 1)
                return;

            _Color = LinkColors.GenerateHue(this);
        }

        /************************************************************************************************************************/

        public bool ArePathsValid(bool logWarnings)
            => LasUtilities.ValidateExternalDirectories(ExternalDirectories, logWarnings);

        /************************************************************************************************************************/

        public static void GetContainingLinks(
            ref string assetPath,
            ICollection<LinkAndSync> links)
        {
            assetPath = assetPath
                .NormalizeSlashes()
                .RemoveTrailingSlashes();

            if (string.IsNullOrEmpty(assetPath))
                return;

            foreach (var link in GetAllLinks())
                if (link.Contains(assetPath))
                    links.Add(link);
        }

        public static void GetContainingLinks(Object asset, out string assetPath, ICollection<LinkAndSync> links)
        {
            assetPath = AssetDatabase.GetAssetPath(asset);
            GetContainingLinks(ref assetPath, links);
        }

        /************************************************************************************************************************/

        private bool Contains(string path)
        {
            var internalDirectory = LasUtilities.RelativeToAbsolute(DirectoryPath)
                .NormalizeSlashes();

            if (string.IsNullOrEmpty(internalDirectory))
                return false;

            path = LasUtilities.RelativeToAbsolute(path)
                .NormalizeSlashes();

            if (path.StartsWith(internalDirectory))
                return Contains(path, internalDirectory);

            if (ExternalDirectories == null)
                return false;

            foreach (var directory in ExternalDirectories)
            {
                if (string.IsNullOrEmpty(directory))
                    continue;

                var absoluteDirectory = LasUtilities.RelativeToAbsolute(directory)
                    .NormalizeSlashes();

                if (path.StartsWith(absoluteDirectory))
                    return Contains(path, absoluteDirectory);
            }

            return false;
        }

        private bool Contains(string absolutePath, string root)
        {
            if (absolutePath.Length == root.Length)
                return true;

            if (absolutePath.Length <= root.Length)
                return false;

            var relativePath = absolutePath.Substring(root.Length + 1);
            return !IsExcluded(relativePath);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif