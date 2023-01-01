// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

namespace LinkAndSync
{
    public static class Strings
    {
        /************************************************************************************************************************/

        public const string
            ProductName = "Link & Sync",
            LogPrefix = "<B>" + ProductName + "</B>: ",
            DeveloperWebsite = "https://kybernetik.com.au";

        /************************************************************************************************************************/

        public static class DirectoryLink
        {
            /************************************************************************************************************************/

            public const string Color =
                "The display color of this link.";

            public const string ExternalDirectories =
                "The directory containing this link will be synchronized with these other directories." +
                "\n• Drag and drop from File Explorer to set a path.";

            public const string Direction =
                "When should this link be synchronized?" +
                "\n• " + nameof(SyncDirection.Pull) + ": Overwrites local files with external changes." +
                "\n• " + nameof(SyncDirection.Push) + ": Overwrites external files with local changes." +
                "\n• " + nameof(SyncDirection.Sync) + ": Mirrors any changes to all other locations." +
                "\n• Use '" + MenuFunctions.SyncSelectedFunction + "' to execute " + nameof(SyncDirection.Sync) + " mode once.";

            public const string Trigger =
                "When should this link be synchronized?" +
                "\n• " + nameof(SyncTrigger.Manual) + ": Executes when you tell it to." +
                "\n• " + nameof(SyncTrigger.Notify) + ": Same as " + nameof(SyncTrigger.Manual) +
                ", but also informs you if there are any modified files to synchronize." +
                "\n• " + nameof(SyncTrigger.Automatic) + ": Immediately synchronizes whenever modified files are detected.";

            public const string Exclusions =
                "Paths relative to the root of this link which will not be synchronized." +
                "\n• File: Exclude a single file." +
                "\n• Directory: Exclude a directory and everything in it recursively." +
                "\n• Metadata files will be excluded with their parent." +
                "\n• Use '" + MenuFunctions.ExcludeSelectionFunction + "' to add to this list.";

            public const string SynchronizedPaths =
                "Paths of everything that was synchronized by this link last time it was executed." + Automatic;

            public const string LastExecuted =
                "The time when this link was last synchronized." + Automatic;

            private const string Automatic =
                "\n\nThis value is updated automatically during synchronization.";

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

#endif