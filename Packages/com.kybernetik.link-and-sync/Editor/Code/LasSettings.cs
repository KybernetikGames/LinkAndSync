// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    public class LasSettings : ScriptableObject
    {
        /************************************************************************************************************************/
        #region Instance
        /************************************************************************************************************************/

        private static LasSettings _Instance;
        private static bool _HasInitializedInstance;
        private static bool _HasSearchedForInstance;

        public static LasSettings Instance
        {
            get
            {
                if (!_HasSearchedForInstance && _Instance == null)
                {
                    if (!_HasInitializedInstance)
                    {
                        _HasInitializedInstance = true;
                        AssetDatabaseWatcher.OnPostprocessAssets += (imported, deleted, movedTo, movedFrom) =>
                        {
                            if (imported.Length > 0)
                                _HasSearchedForInstance = false;
                        };
                    }

                    _HasSearchedForInstance = true;
                    _Instance = LasUtilities.FindAssetOfType<LasSettings>();
                }

                return _Instance;
            }
        }

        /************************************************************************************************************************/

        public static void Create(Object nextTo)
        {
            _Instance = CreateInstance<LasSettings>();

            var path = AssetDatabase.GetAssetPath(nextTo);
            if (string.IsNullOrEmpty(path))
            {
                var script = MonoScript.FromScriptableObject(_Instance);
                path = AssetDatabase.GetAssetPath(script);
                path = Path.GetDirectoryName(path);
                if (path != "Assets")
                    path = Path.GetDirectoryName(path);
            }
            else
            {
                path = Path.GetDirectoryName(path);
            }

            path += $"/{nameof(LinkAndSync)}Settings.asset";

            AssetDatabase.CreateAsset(_Instance, path);

            Selection.activeObject = _Instance;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        [SerializeField]
        private bool _EnableOverlay = true;
        public static bool EnableOverlay
        {
            get
            {
                var instance = Instance;
                return instance != null && instance._EnableOverlay;
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private Texture2D _OverlayIcon;
        public static Texture2D OverlayIcon
        {
            get
            {
                var instance = Instance;
                return instance != null ? instance._OverlayIcon : null;
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private Texture2D _OverlayAttentionIcon;
        public static Texture2D OverlayAttentionIcon
        {
            get
            {
                var instance = Instance;
                return instance != null ? instance._OverlayAttentionIcon : null;
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private Texture2D _WindowIconDark;
        public static Texture2D WindowIconDark
        {
            get
            {
                var instance = Instance;
                return instance != null ? instance._WindowIconDark : null;
            }
        }

        [SerializeField]
        private Texture2D _WindowIconLight;
        public static Texture2D WindowIconLight
        {
            get
            {
                var instance = Instance;
                return instance != null ? instance._WindowIconLight : null;
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("Should it show a warning dialog box the first time an Automatic sync would be executed?")]
        private bool _EnableAutomaticWarning = true;
        public static bool EnableAutomaticWarning
        {
            get
            {
                var instance = Instance;
                return instance != null && instance._EnableAutomaticWarning;
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip(nameof(SyncTrigger.Notify) + " mode will display a different overlay icon when changes have been detected." +
            " Should it also log a message in the Console?")]
        private bool _NotifyViaLog = true;
        public static bool NotifyViaLog
        {
            get
            {
                var instance = Instance;
                return instance != null && instance._NotifyViaLog;
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private bool _ShowConfirmationWindow = true;
        public static bool ShowConfirmationWindow
        {
            get
            {
                var instance = Instance;
                return instance != null && instance._ShowConfirmationWindow;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif