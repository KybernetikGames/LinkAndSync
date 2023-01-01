// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;

namespace LinkAndSync
{
    /// <summary>Ensure that <see cref="LinkAndSync.OnEnable"/> has been called on all instances.</summary>
    internal class InitializeAllLinks : AssetPostprocessor
    {
        /************************************************************************************************************************/

#if UNITY_2021_1_OR_NEWER
        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] movedTo, string[] movedFrom, bool didDomainReload)
            => LasUtilities.FindAssetsOfType<LinkAndSync>();
#else
        [InitializeOnLoadMethod]
        private static void Initialize()
            => LasUtilities.FindAssetsOfType<LinkAndSync>();
#endif

        /************************************************************************************************************************/
    }
}

#endif