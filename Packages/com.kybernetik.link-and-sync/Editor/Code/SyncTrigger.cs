// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

namespace LinkAndSync
{
    /// <summary>Determines when synchronizaiton operations are executed.</summary>
    public enum SyncTrigger
    {
        /// <summary>Specifically initiated by the user.</summary>
        Manual,

        /// <summary>Same as <see cref="Manual"/>, but also inform the user if there are any modified files to synchronize.</summary>
        Notify,

        /// <summary>Immediately synchronize whenever modified files are detected.</summary>
        Automatic,
    }
}

#endif