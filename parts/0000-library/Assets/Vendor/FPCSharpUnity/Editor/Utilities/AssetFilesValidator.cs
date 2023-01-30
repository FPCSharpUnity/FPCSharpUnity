using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Editor.Utils;
using JetBrains.Annotations;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities.Editor {
  /// <summary>
  /// Sometimes Unity fails to import scenes or prefabs. This tries to detect that.
  /// </summary>
  [PublicAPI] public static class AssetFilesValidator {
    /// <summary>
    /// Checks whether Unity has correctly imported all scenes and prefabs.
    /// </summary>
    /// <param name="validateScenes">Should we check scenes?</param>
    /// <param name="validatePrefabs">Should we check prefabs?</param>
    /// <param name="validateMaterials">Should we check materials?</param>
    /// <param name="showProgress">Should editor progress be shown?</param>
    public static IEnumerable<ObjectValidator.Error> validateAll(
      bool validateScenes, bool validatePrefabs, bool validateMaterials, bool showProgress
    ) =>
      validate(
        scenePaths: validateScenes ? AssetDatabaseUtils.allScenes.ToArray() : EmptyArray<AssetPath>._,
        prefabPaths: validatePrefabs ? AssetDatabaseUtils.allPrefabs.ToArray() : EmptyArray<AssetPath>._,
        materialPaths: validatePrefabs ? AssetDatabaseUtils.allMaterials.ToArray() : EmptyArray<AssetPath>._,
        showProgress: showProgress
      );

    /// <summary>
    /// Checks whether Unity has correctly imported scenes and prefabs at given paths. 
    /// </summary>
    /// <param name="scenePaths">Paths to the scene files (ending in .unity)</param>
    /// <param name="prefabPaths">Paths to the prefab files (ending in .prefab)</param>
    /// <param name="materialPaths">Paths to the prefab files (ending in .mat)</param>
    /// <param name="showProgress">Should editor progress be shown?</param>
    public static IEnumerable<ObjectValidator.Error> validate(
      ICollection<AssetPath> scenePaths, ICollection<AssetPath> prefabPaths, ICollection<AssetPath> materialPaths, 
      bool showProgress
    ) {
      (AssetPath path, Object obj)[] badScenes, badPrefabs, badMaterials;
      var materialsMissingShaders = new List<Material>();
      {
        var maybeProgress = showProgress ? new EditorProgress("Asset files validator") : null;
        try {
          maybeProgress?.start("Checking scene assets");
          badScenes = scenePaths.collect((path, idx) => {
            maybeProgress?.progress(idx, scenePaths.Count);
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            return obj is SceneAsset ? None._ : Some.a((path, obj));
          }).ToArray();
          
          maybeProgress?.start("Checking prefab assets");
          badPrefabs = prefabPaths.collect((path, idx) => {
            maybeProgress?.progress(idx, prefabPaths.Count);
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            return obj is GameObject ? None._ : Some.a((path, obj));
          }).ToArray();
          
          maybeProgress?.start("Checking material assets");
          badMaterials = materialPaths.collect((path, idx) => {
            maybeProgress?.progress(idx, prefabPaths.Count);
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj is Material mat && !mat.shader) {
              materialsMissingShaders.Add(mat);
            }
            return obj is Material ? None._ : Some.a((path, obj));
          }).ToArray();
        }
        finally {
          maybeProgress?.Dispose();
        }
      }

      foreach (var error in createErrors(badScenes, "scene")) yield return error;
      foreach (var error in createErrors(badPrefabs, "prefab")) yield return error;
      foreach (var error in createErrors(badMaterials, "material")) yield return error;

      foreach (var mat in materialsMissingShaders) {
        // If we have materials missing a shader, then we will get an exception in 
        // UnityEditor.Rendering.Universal.MaterialPostprocessor.OnPostprocessAllAssets
        yield return new ObjectValidator.Error(
          ObjectValidator.Error.Type.NullReference, 
          "Material is missing a shader",
          mat
        );
      }

      IEnumerable<ObjectValidator.Error> createErrors(
        IEnumerable<(AssetPath path, Object obj)> src, string name
      ) {
        foreach (var (path, obj) in src) {
          var objStr = obj ? obj.GetType().FullName : "null";
          yield return new ObjectValidator.Error(
            ObjectValidator.Error.Type.AssetCorrupted,
            $"Expected file to be a {name}, but it was {objStr}",
            obj,
            objFullPath: $"{name} asset import failed",
            location: new AssetPath(path)
          );
        }
      }
    }
  }
}