// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

namespace LinkAndSync
{
    public enum SyncDirection
    {
        /// <summary>Overwrite local files with any external changes.</summary>
        Pull,

        /// <summary>Overwrite external files with any local changes.</summary>
        Push,

        /// <summary>Mirror any changes to all other locations.</summary>
        Sync,
    }
}

#endif