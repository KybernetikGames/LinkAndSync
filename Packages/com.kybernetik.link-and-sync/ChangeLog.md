Link & Sync v3.1
- Added Right Click context menu to Project Window Overlays.
- Fixed automatic link initialization in Unity 2021+ to use an AssetPostprocessor since [InitializeOnLoadMethod] should no longer be used for loading assets.

Link & Sync v3.0
- Completely reworked the entire system.
- Merged the Lite and Pro versions. All features are now free.
- Links are now individual ScriptableObjects instead of being managed in a central window and settings file.
- All operations are now displayed in a window before being executed so you can see which files will be copied and deleted.
- Added the ability to specify multiple external directories.
- Added the ability to push files out to the external directories instead of only pulling them in or synchronizing both ways.
- Added protection against infinite automatic execution loops.

Link & Sync v2.0
- Created Link & Sync Lite and marked some features as Pro Only: Auto and Notify mode, Two Way sync, Exclusions, and Source Code access.
- Added two way synchronisation if you want to apply local changes back to the source.
- Moved the Linker Window to Assets/Link and Sync/Linker Window and added some other useful menu items along with it so they are accessible via the project window's right click menu.
- Added support for linking a single file instead of a whole folder.
- Links now have a customisable colour which is used for their project window overlay and notification messages.
- Improved copying system and progress bar implementation - exceptions while copying cause only the offending file to be skipped instead of stopping everything else as well.
- Improved exclusion management - it now supports drag and drop from both the source and destination and you can add an exclusion by right clicking on an asset in the project window and selecting Link and Sync -> Exclude Selection from Link.
- Improved handling of metadata files.
- Added a warning if two links try to target the same destination.
- Added a warning if files already exist at the destination when creating a link.
- Add a dialog box that shows on startup, asking if you want to allow auto mode (in case you open an old project and don't want it to immediately sync all its links).
- Improved the performance of the project window overlays and implemented a visible darkening while you hold middle click on them (previously there was no indication that you had middle clicked on them, so you couldn't be certain that it had actually performed a sync).
- Added debug logging that can be enabled by recompiling the DLL with DEBUG defined.
- Many miscellaneous bug fixes and improvements.

Link & Sync v1.1
- Removed the automatic call to AssetDatabase.Refresh if the copy queue is executed while empty.

Link & Sync v1.0
- Initial Release.