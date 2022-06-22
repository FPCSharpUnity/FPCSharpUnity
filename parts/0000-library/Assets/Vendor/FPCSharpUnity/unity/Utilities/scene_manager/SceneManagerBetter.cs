using ExhaustiveMatching;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.unity.Data.scenes;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities;

/// <inheritdoc cref="SceneManager.sceneCount"/>
[Record(ConstructorFlags.Constructor), NewTypeImplicitTo]
public readonly partial struct LoadedSceneCount { public readonly int count; }

/// <inheritdoc cref="SceneManager.sceneCountInBuildSettings"/>
[Record(ConstructorFlags.Constructor), NewTypeImplicitTo]
public readonly partial struct SceneCountInBuildSettings { public readonly int count; }

/// <summary>Emitted when a scene is loaded.</summary>
[Record(ConstructorFlags.Constructor)]
public readonly partial struct SceneLoadedData {
  public readonly Scene scene;
  public readonly LoadSceneMode loadSceneMode;
}

/// <summary>
/// A better version of <see cref="SceneManager"/>. You should always use this, instead of the Unity's manager, as this
/// has richer API. 
/// </summary>
[Singleton, HasLogger(standalone = true), PublicAPI] public sealed partial class SceneManagerBetter {
  
#region Public API

  /// <summary>
  /// Provides a custom way to start the scene loading operation.
  /// </summary>
  public delegate (AsyncOperation op, Scene scene) StartLoading(
    ScenePath scenePath, LoadSceneParametersBetter loadSceneParams
  );

  public static readonly StartLoading startLoadingUsingSceneManager =
    static (scenePath, loadSceneParams) => {
      var op = SceneManager.LoadSceneAsync(scenePath, loadSceneParams);
      // When we do a `LoadSceneAsync` that scene becomes the last scene in the `SceneManager` list.
      var scene = instance.getLastLoadedScene();
      return (op, scene);
    };
  
  /// <summary>
  /// Dispatches an event just before the Scene is unloaded.
  /// </summary>
  /// <note>
  /// Unity does not have an API for this, thus we rely on the fact that all of the Scene management operations is done
  /// via this class, which then handles dispatching this callback itself.
  /// </note>
  [PublicReadOnlyAccessor] readonly Subject<Scene> _beforeSceneUnload = new();

  /// <summary>Dispatches an event after a Scene is loaded.</summary>
  public readonly IRxObservable<SceneLoadedData> afterSceneLoaded =
    Observable.fromEvent2<SceneLoadedData, UnityAction<Scene, LoadSceneMode>>(
      registerCallback: push => {
        var callback = new UnityAction<Scene, LoadSceneMode>((scene, mode) => push(new SceneLoadedData(scene, mode)));
        SceneManager.sceneLoaded += callback;
        return callback;
      },
      unregisterCallback: callback => SceneManager.sceneLoaded -= callback
    );

  public LoadedSceneCount loadedSceneCount => new(SceneManager.sceneCount);
  public SceneCountInBuildSettings sceneCountInBuildSettings => new(SceneManager.sceneCountInBuildSettings);
  
  /// <inheritdoc cref="SceneManagerLoadedScenes"/>
  public SceneManagerLoadedScenes loadedScenes => new(loadedSceneCount);

  /// <summary>Gets the loaded Scene at specified index. Will return `None` if there is no scene at this index.</summary>
  public Option<Scene> getLoadedSceneAt(int index) {
    var scene = SceneManager.GetSceneAt(index);
    return scene.IsValid() ? Some.a(scene) : None._;
  }
  
  /// <summary>Gets the loaded Scene at specified index. Will return `Left` if there is no scene at this index.</summary>
  public Either<DeveloperError, Scene> getLoadedSceneAtE(int index) {
    var scene = SceneManager.GetSceneAt(index);
    return scene.IsValid() ? scene : new DeveloperError($"Can't find a loaded scene with index {index}!");
  }
  
  /// <summary>Searches all Scenes loaded for a Scene that has the given name.</summary>
  public Either<DeveloperError, Scene> getLoadedScene(SceneName sceneName) {
    var scene = SceneManager.GetSceneByName(sceneName);
    return scene.IsValid() ? scene : new DeveloperError($"Can't load scene by {sceneName}!");
  }
  
  /// <summary>Searches all Scenes loaded for a Scene that has the given asset path.</summary>
  public Either<DeveloperError, Scene> getLoadedScene(ScenePath scenePath) {
    var scene = SceneManager.GetSceneByPath(scenePath);
    return scene.IsValid() ? scene : new DeveloperError($"Can't load scene by {scenePath}!");
  }

  /// <summary>
  /// Returns the first loaded scene. As Unity has to have at least 1 scene loaded at all times, this does not return
  /// an <see cref="Option{A}"/>.
  /// </summary>
  public Scene getFirstLoadedScene() => 
    getLoadedSceneAt(0).getOrThrow("Unity does not allow having no scenes loaded, this should never happen!");

  /// <summary>
  /// Returns the last loaded scene. As Unity has to have at least 1 scene loaded at all times, this does not return
  /// an <see cref="Option{A}"/>.
  /// </summary>
  public Scene getLastLoadedScene() => 
    getLoadedSceneAt(loadedSceneCount.count - 1)
      .getOrThrow("Unity does not allow having no scenes loaded, this should never happen!");
  
  /// <summary>Synchronously loads the scene specified by it's build index.</summary>
  public Scene loadScene(SceneBuildIndex sceneBuildIndex, LoadSceneParametersBetter? loadSceneParams=null) {
    var loadSceneParams_ = loadSceneParams ?? LoadSceneMode.Single;
    if (loadSceneParams_.loadSceneMode == LoadSceneMode.Single) {
      notifyAllScenesAboutUnloading(
        maybeExceptThisScene: None._,
        debugMethodName: nameof(loadScene), debugData: sceneBuildIndex
      );
    }

    return SceneManager.LoadScene(sceneBuildIndex, loadSceneParams_);
  }

  /// <summary>Synchronously loads the scene specified by it's name.</summary>
  public Scene loadScene(SceneName sceneName, LoadSceneParametersBetter? loadSceneParams=null) {
    var loadSceneParams_ = loadSceneParams ?? LoadSceneMode.Single;
    if (loadSceneParams_.loadSceneMode == LoadSceneMode.Single) {
      notifyAllScenesAboutUnloading(
        maybeExceptThisScene: None._,
        debugMethodName: nameof(loadScene), debugData: sceneName
      );
    }

    return SceneManager.LoadScene(sceneName, loadSceneParams_);
  }

  public AsyncSceneLoad loadSceneAsync(
    ScenePath scenePath, LoadSceneParametersBetter loadSceneParams, bool automaticActivation,
    StartLoading startLoading = null
  ) =>
    automaticActivation
      ? loadSceneAsyncWithAutomaticActivation(scenePath, loadSceneParams, startLoading)
      : loadSceneAsyncWithManualActivation(scenePath, loadSceneParams, startLoading);

  /// <summary>
  /// Loads the scene and activates it automatically when the load is done. 
  /// </summary>
  public AsyncSceneLoadWithAutomaticActivation loadSceneAsyncWithAutomaticActivation(
    ScenePath scenePath, LoadSceneParametersBetter loadSceneParams, StartLoading startLoading = null
  ) {
    startLoading ??= startLoadingUsingSceneManager;
    
    switch (loadSceneParams.loadSceneMode) {
      case LoadSceneMode.Single:
        return new AsyncSceneLoadWithAutomaticActivationSingle(
          loadSceneAsyncWithManualActivation(scenePath, loadSceneParams, startLoading)
        );
      case LoadSceneMode.Additive:
        var tpl = startLoading(scenePath, loadSceneParams);
        return new AsyncSceneLoadWithAutomaticActivationAdditive(tpl.op, tpl.scene);
      default:
        throw ExhaustiveMatch.Failed(loadSceneParams.loadSceneMode);
    }
  }

  /// <summary>
  /// Loads the scene, but doesn't activate it automatically, you have to do that yourself. 
  /// </summary>
  public AsyncSceneLoadWithManualActivation loadSceneAsyncWithManualActivation(
    ScenePath scenePath, LoadSceneParametersBetter loadSceneParams, StartLoading startLoading = null
  ) {
    startLoading ??= startLoadingUsingSceneManager;
    
    var tpl = startLoading(scenePath, loadSceneParams);
    var onSceneActivation = loadSceneParams.loadSceneMode switch {
      LoadSceneMode.Single => Some.a(() => notifyAllScenesAboutUnloading(
        maybeExceptThisScene: Some.a(tpl.scene),
        debugMethodName: nameof(loadSceneAsyncWithManualActivation),
        debugData: scenePath
      )),
      LoadSceneMode.Additive => None._,
      _ => throw ExhaustiveMatch.Failed(loadSceneParams.loadSceneMode)
    };
    return new AsyncSceneLoadWithManualActivation(tpl.op, onSceneActivation, tpl.scene);
  }

  /// <inheritdoc cref="SceneManager.UnloadSceneAsync(Scene)"/>
  public AsyncOperation unloadSceneAsync(Scene scene) {
    log.mDebug($"{nameof(unloadSceneAsync)}({scene.scenePath()})");
    _beforeSceneUnload.push(scene);
    return SceneManager.UnloadSceneAsync(scene);
  }

  /// <inheritdoc cref="SceneManager.UnloadSceneAsync(string)"/>
  public Either<DeveloperError, AsyncOperation> unloadSceneAsync(SceneName sceneName) {
    log.mDebug($"{nameof(unloadSceneAsync)}({sceneName})");
    if (getLoadedScene(sceneName).valueOut(out var err, out var scene)) {
      _beforeSceneUnload.push(scene);
      return SceneManager.UnloadSceneAsync(sceneName);
    }
    else return err;
  }

  /// <inheritdoc cref="SceneManager.GetActiveScene"/>
  public Scene getActiveScene() => SceneManager.GetActiveScene();
  
  /// <inheritdoc cref="SceneManager.MoveGameObjectToScene"/>
  public void moveGameObjectToScene(GameObject gameObject, Scene scene) => 
    SceneManager.MoveGameObjectToScene(gameObject, scene);

  /// <inheritdoc cref="SceneManager.SetActiveScene"/>
  public Either<DeveloperError, Unit> setActiveSceneE(Scene scene) => 
    SceneManager.SetActiveScene(scene)
    ? Unit._
    : new DeveloperError($"Scene '{scene.name}' @ '{scene.path}' is not loaded yet, thus we can't set it as active!");

  #endregion
  
  /// <summary>
  /// Notifies all scenes, except the given `<see cref="maybeExceptThisScene"/>` that they are about to get unloaded via
  /// <see cref="_beforeSceneUnload"/>.
  /// </summary>
  void notifyAllScenesAboutUnloading<A>(
    Option<Scene> maybeExceptThisScene,
    string debugMethodName, in A debugData
  ) {
    foreach (var scene in loadedScenes) {
      if (!maybeExceptThisScene.valueOut(out var exceptThisScene) || scene != exceptThisScene) {
        log.mDebug($"{debugMethodName}({debugData}): unloading {scene.scenePath()}");
        _beforeSceneUnload.push(scene);
      }
    }
  }
}