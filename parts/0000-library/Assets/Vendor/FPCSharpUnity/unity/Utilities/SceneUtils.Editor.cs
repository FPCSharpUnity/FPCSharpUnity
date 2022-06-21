#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.unity.Data.scenes;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities {
  public static partial class SceneUtils {
    /// <param name="modifyScene">
    /// (Scene -> bool) You need to return true if scene was modified and needs to be saved
    /// </param>
    /// <returns>true - if operation was not canceled by user</returns>
    public static bool modifyAllScenesInProject(Func<Scene, bool> modifyScene) {
      var result = openScenesAndDo(
        AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Select(_ => new ScenePath(_)),
        scene => { if (modifyScene(scene)) EditorSceneManager.SaveScene(scene); }
      );

      if (result) {
        if (Log.d.isInfo()) Log.d.info($"{nameof(modifyAllScenesInProject)} completed successfully");
      }
      else {
        EditorUtils.userInfo(nameof(modifyAllScenesInProject), "Operation canceled by user", LogLevel.WARN);
      }
      return result;
    }

    /// <returns>Some(results) if the operation completed successfully, None if it was cancelled.</returns>
    /// <param name="afterScenesProcessed">
    /// Runs this callback after all scenes were processed, before returning to the original scene.
    /// It is guaranteed to run this exactly once in case of any failure.
    /// </param>
    public static Option<ImmutableDictionary<ScenePath, A>> openScenesAndDo<A>(
      IEnumerable<ScenePath> scenes, Func<Scene, A> doWithLoadedScene, bool askToSave = true, bool allAtOnce = false,
      string progressWindowTitle = nameof(openScenesAndDo),
      Action afterScenesProcessed = null
    ) {
      var ranAfterScenesProcessed = false;
      var sceneManager = SceneManagerBetter.instance;
      try {
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
          return None._;
        }
        var builder = ImmutableDictionary.CreateBuilder<ScenePath, A>();
        var scenesToUse = scenes.toImmutableArrayC();
        var sceneCount = scenesToUse.Count;
        var initialScene = sceneManager.getActiveScene().scenePath();
        try {
          var loadedScenes = new List<Scene>(sceneCount);
          for (var i = 0; i < sceneCount; i++) {
            var currentPath = scenesToUse[i];
            if (progress(i, currentPath, secondLoop: false)) {
              return None._;
            }
            var mode = (!allAtOnce || i == 0) ? OpenSceneMode.Single : OpenSceneMode.Additive;
            var loadedScene = sceneManager.__EDITOR.openScene(currentPath, mode);
            if (allAtOnce) {
              loadedScenes.Add(loadedScene);
            }
            else {
              builder[currentPath] = doWithLoadedScene(loadedScene);
            }
          }

          if (allAtOnce) {
            for (var i = 0; i < loadedScenes.Count; i++) {
              var scene = loadedScenes[i];
              var scenePath = scene.scenePath();
              if (progress(i, scenePath, secondLoop: true)) {
                return None._;
              }
              builder[scenePath] = doWithLoadedScene(scene);
            }
          }
        }
        finally {
          ranAfterScenesProcessed = true;
          afterScenesProcessed?.Invoke();
          // unload all scenes and load previously opened scene
          if (initialScene.path.isNullOrEmpty()) {
            // if path is empty, that means empty scene was loaded before
            sceneManager.__EDITOR.newScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
          }
          else {
            sceneManager.__EDITOR.openScene(initialScene, OpenSceneMode.Single);
          }
        }

        bool progress(int sceneNumber, ScenePath path, bool secondLoop) {
          var progressValue = sceneNumber / (float) sceneCount;
          if (allAtOnce) {
            progressValue /= 2;
            if (secondLoop) progressValue += .5f;
          }
          return EditorUtility.DisplayCancelableProgressBar(
            progressWindowTitle,
            $"({sceneNumber}/{sceneCount}) ...{path.path.trimToRight(40)}",
            progressValue
          );
        }
        return Some.a(builder.ToImmutable());
      }
      catch (Exception e) {
        if (!ranAfterScenesProcessed) afterScenesProcessed?.Invoke();
        EditorUtils.userInfo($"Error in {nameof(openScenesAndDo)}", e.ToString(), LogLevel.ERROR);
        throw;
      }
      finally {
        EditorUtility.ClearProgressBar();
      }
    }

    /// <returns>true if the operation completed successfully, false if it was cancelled</returns>
    public static bool openScenesAndDo(
      IEnumerable<ScenePath> scenes, Action<Scene> doWithLoadedScene, bool askToSave = true, bool allAtOnce = false,
      string progressWindowTitle = nameof(openScenesAndDo),
      Action afterScenesProcessed = null
    ) => openScenesAndDo(
      scenes, scene => {
        doWithLoadedScene(scene);
        return Unit._;
      }, 
      askToSave: askToSave, allAtOnce: allAtOnce, progressWindowTitle: progressWindowTitle, 
      afterScenesProcessed: afterScenesProcessed
    ).isSome;
  }
}
#endif
