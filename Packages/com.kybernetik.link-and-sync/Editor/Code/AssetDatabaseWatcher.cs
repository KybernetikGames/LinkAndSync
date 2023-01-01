// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;

namespace LinkAndSync
{
    internal class AssetDatabaseWatcher : AssetPostprocessor
    {
        /************************************************************************************************************************/

        public delegate void PostprocessAssetsDelegate(
            string[] imported,
            string[] deleted,
            string[] movedTo,
            string[] movedFrom);

        public static event PostprocessAssetsDelegate OnPostprocessAssets;

        /************************************************************************************************************************/

        private static void OnPostprocessAllAssets(
            string[] imported,
            string[] deleted,
            string[] movedTo,
            string[] movedFrom)
            => OnPostprocessAssets?.Invoke(imported, deleted, movedTo, movedFrom);

        /************************************************************************************************************************/
    }
}

#endif