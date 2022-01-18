using System;
using System.IO;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using UnityEditor;

/**
 * Git identifies file changes by file size and date modified
 * If you swap the names of 2 files git doesn't see the changes in meta files
 * This code updates 'Date modified' so git detects the changes
*/
public class DateModifiedUpdater : AssetPostprocessor {
  [UsedImplicitly]
  static void OnPostprocessAllAssets(
    string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths
  ) {
    if (movedAssets.Length > 0 && Log.d.isDebug()) Log.d.debug(
      $"{nameof(DateModifiedUpdater)}.{nameof(OnPostprocessAllAssets)}[\n" +
      $"  {nameof(movedAssets)}: {movedAssets.mkStringEnum()}\n" +
      $"]"
    );
    foreach (var relativePath in movedAssets) {
      // Can't change write time of the asset file. Because Unity will try to load that file when you focus/unfocus 
      // Unity window and automatically discard the changes you made after the rename.
      // File.SetLastWriteTime(relativePath, DateTime.Now);
      
      File.SetLastWriteTime($"{relativePath}.meta", DateTime.Now);
    }
  }
}