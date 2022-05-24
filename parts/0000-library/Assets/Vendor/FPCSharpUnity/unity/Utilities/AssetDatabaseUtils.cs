#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.unity.Logger;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Utilities {
  /// <summary>
  /// Safe versions of functions in <see cref="AssetDatabase"/> and extra utilities.
  /// </summary>
  [PublicAPI] public static class AssetDatabaseUtils {
    /// <summary>Safe version of <see cref="AssetDatabase.GetAllAssetPaths"/>.</summary>
    public static AssetPath[] GetAllAssetPaths() => AssetDatabase.GetAllAssetPaths().map(p => new AssetPath(p));
    
    /// <summary>Safe version of <see cref="AssetDatabase.GUIDToAssetPath(string)"/>.</summary>
    public static Either<string, AssetPath> GUIDToAssetPath(AssetGuid guid) =>
      Option.a(AssetDatabase.GUIDToAssetPath(guid)).flatMap(_ => _.nonEmptyOpt()).map(path => new AssetPath(path))
        .toRight(guid, static guid => $"Can't turn {guid} to asset path: asset not found");
    
    /// <summary>Safe version of <see cref="AssetDatabase.GetAssetPath(Object)"/>.</summary>
    public static Either<string, AssetPath> GetAssetPath(Object obj) =>
      Option.a(AssetDatabase.GetAssetPath(obj)).flatMap(_ => _.nonEmptyOpt()).map(path => new AssetPath(path))
        .toRight(obj, static obj => $"Can't turn {obj} to asset path: asset not found");
    
    /// <summary>Safe version of <see cref="AssetDatabase.GUIDFromAssetPath"/>.</summary>
    public static Either<string, AssetGuid> AssetPathToGUID(AssetPath path) =>
      Option.a(AssetDatabase.AssetPathToGUID(path)).flatMap(_ => _.nonEmptyOpt()).map(guid => new AssetGuid(guid))
        .toRight(path, static path => $"Can't turn {path} to asset guid: asset not found");

    /// <summary>Safe version of <see cref="AssetDatabase.GUIDFromAssetPath"/>.</summary>
    public static Either<string, AssetGuid> GUIDFromAssetPath(AssetPath path) =>
      // Just reuse the same function, not sure why Unity has 2 functions to do the same thing. 
      AssetPathToGUID(path);

    /// <summary>Safe version of <see cref="AssetDatabase.LoadMainAssetAtPath(string)"/>.</summary>
    public static Either<string, Object> LoadMainAssetAtPath(AssetPath path) {
      try {
        var asset = AssetDatabase.LoadMainAssetAtPath(path);
        return !asset ? (Either<string, Object>) $"Loading main asset at {path} returned null!" : asset;
      }
      catch (Exception e) {
        return $"Error while loading main asset at {path}: {e}";
      }
    }
    
    public static IEnumerable<A> getPrefabsOfType<A>() {
      var prefabGuids = AssetDatabase.FindAssets("t:prefab");

      var prefabs = prefabGuids
        .Select(loadMainAssetByGuid)
        .OfType<GameObject>();

      var components = new List<A>();

      foreach (var go in prefabs) {
        go.GetComponentsInChildren(includeInactive: true, results: components);
        foreach (var c in components) yield return c;
      }
    }

    /// <summary>
    /// Helper function that is useful when we want to migrate all prefabs.
    ///
    /// Note: this does not migrate components on scenes, only prefabs.
    /// </summary>
    public static void editPrefabsOfType<A>(Action<A> editSinglePrefab) where A : Component {
      foreach (var prefab in getPrefabsOfType<A>()) {
        prefab.editPrefab(editSinglePrefab);
      }
    }

    /// <summary>
    /// Sometimes Unity fails to find scriptable objects using the t: selector.
    /// 
    /// Our known workaround:
    /// 1. Open asset references window.
    /// 2. Find all instances of your scriptable object.
    /// 3. Show Actions > Set Dirty
    /// 4. Save project.
    /// 5. Profit!
    /// </summary>
    /// <typeparam name="A"></typeparam>
    /// <returns></returns>
    public static IEnumerable<A> getScriptableObjectsOfType<A>() where A : ScriptableObject =>
      AssetDatabase.FindAssets($"t:{typeof(A).Name}")
      .Select(loadMainAssetByGuid)
      .OfType<A>();

    public static Object loadMainAssetByGuid(string guid) =>
      AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
    
    public static string copyAssetAndGetPath<T>(T obj, PathStr path) where T: Object {
      var originalPath = AssetDatabase.GetAssetPath(obj);
      var newPath = path.unityPath + "/" + obj.name + Path.GetExtension(originalPath);
      if (Log.d.isVerbose())
        Log.d.verbose($"{nameof(AssetDatabaseUtils)}#{nameof(copyAssetAndGetPath)}: " +
          $"copying asset from {originalPath} to {newPath}");
      if (!AssetDatabase.CopyAsset(originalPath, newPath))
        throw new Exception($"Couldn't copy asset from {originalPath} to {newPath}");
      return newPath;
    }

    public static IEnumerable<AssetPath> allWithExtension(string extension) =>
      Directory.EnumerateFiles("Assets", $"*.{extension}", SearchOption.AllDirectories).Select(s => new AssetPath(s));

    public static IEnumerable<AssetPath> allScriptableObjects => allWithExtension("asset");
    public static IEnumerable<AssetPath> allScenes => allWithExtension("unity");
    public static IEnumerable<AssetPath> allPrefabs => allWithExtension("prefab");
    public static IEnumerable<AssetPath> allMaterials => allWithExtension("mat");

    // Calling stopAssetEditing without starting or stopping multiple times causes exceptions.
    // These are used to track how many start calls there been to properly stop editing at the last stop call.
    static int _assetsAreBeingEditedCount;

    public static void startAssetEditing() {
      if (_assetsAreBeingEditedCount == 0)
        AssetDatabase.StartAssetEditing();
      _assetsAreBeingEditedCount++;
      if (Log.d.isVerbose()) 
        Log.d.verbose($"{nameof(AssetDatabaseUtils)}#{nameof(startAssetEditing)}: count: {_assetsAreBeingEditedCount}");
    }
    
    public static void stopAssetEditing() {
      _assetsAreBeingEditedCount--;
      if (Log.d.isVerbose()) 
        Log.d.verbose($"{nameof(AssetDatabaseUtils)}#{nameof(stopAssetEditing)}: count: {_assetsAreBeingEditedCount}");
      if (_assetsAreBeingEditedCount == 0)
        AssetDatabase.StopAssetEditing();
      else if (_assetsAreBeingEditedCount < 0)
        throw new Exception($"{nameof(stopAssetEditing)} was called more times than {nameof(startAssetEditing)}!");
    }

    /// <summary>
    /// Allows calling <see cref="startAssetEditing"/> and <see cref="stopAssetEditing"/> in a "using" like fashion.
    /// </summary>
    public static ActionOnDispose doAssetEditing() {
      startAssetEditing();
      return new ActionOnDispose(stopAssetEditing);
    }

    /// <summary>
    /// Returns all files from the <see cref="Selection.objects"/>. If a directory (or multiple directories) are
    /// selected, returns all files in the subdirectories.
    /// <para/>
    /// Will return `Left` if given array contains Unity objects which can not be resolved to paths.
    /// </summary>
    public static Either<string, ImmutableHashSet<AssetPath>> assetsFromSelectionRecursive(Object[] selection) {
      var paths = selection.Select(GetAssetPath).sequence().rightOr_RETURN();
      var (directories, files) = paths.partition(path => Directory.Exists(path));
      if (directories.isEmpty()) return paths.ToImmutableHashSet();
      else {
        var allAssetPaths = GetAllAssetPaths();
        var filesInDirectories = allAssetPaths.Where(path =>
          // Is a file
          !Directory.Exists(path)
          // is contained in a directory
          && directories.Any(dir => path.path.StartsWithFast(dir.path))
        );
        return filesInDirectories.Concat(files).ToImmutableHashSet();
      }
    }
  }
}
#endif
