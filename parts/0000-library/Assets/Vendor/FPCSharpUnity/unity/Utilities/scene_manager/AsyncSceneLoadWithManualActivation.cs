using System;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities;

/// <summary>
/// Data about a scene which is loading asynchronously. When the scene is ready to <see cref="activate"/>
/// <see cref="percentCompleteUntilActivation"/> will start returning 1.
/// <para/>
/// The <see cref="future"/> will complete when the scene will be fully loaded and activated.
/// </summary>
[PublicAPI] public sealed class AsyncSceneLoadWithManualActivation : AsyncSceneLoad {
  /// <summary>
  /// When <see cref="AsyncOperation.allowSceneActivation"/> is false in scene load operation, then Unity stops
  /// loading at 0.9 until we activate the scene.
  /// <para/>
  /// https://docs.unity3d.com/ScriptReference/AsyncOperation-allowSceneActivation.html
  /// </summary>
  const float SCENE_LOAD_VALUE_BEFORE_ACTIVATION = .9f;

  public AsyncOperation loadOperation { get; }
  readonly Option<Action> onSceneActivation;
  public Scene sceneBeingLoaded { get; }
  public Future<Scene> future { get; }

  /// <summary>
  /// Did we finish loading and only activation is left?
  /// <para/>
  /// If so, you can call <see cref="activate"/> and be pretty sure that the Scene will be fully loaded pretty quickly.
  /// </summary>
  public bool isReadyToActivate => 
    // ReSharper disable once CompareOfFloatsByEqualityOperator
    loadOperation.progress == SCENE_LOAD_VALUE_BEFORE_ACTIVATION;

  /// <summary>
  /// Completes when <see cref="isReadyToActivate"/> becomes true.
  /// </summary>
  [LazyProperty] public Future<AsyncSceneLoadWithManualActivation> whenReadyToActivate =>
    FutureU.fromBusyLoop(() => isReadyToActivate).map(this, static (_, self) => self);

  public AsyncSceneLoadWithManualActivation(
    AsyncOperation loadOperation, Option<Action> onSceneActivation, Scene sceneBeingLoaded
  ) {
    this.loadOperation = loadOperation;
    this.sceneBeingLoaded = sceneBeingLoaded;
    this.onSceneActivation = onSceneActivation;
    
    loadOperation.allowSceneActivation = false;
    future = loadOperation.toFuture().map(sceneBeingLoaded, static (_, sceneBeingLoaded) => sceneBeingLoaded);
  }

  /// <summary>
  /// Activate the scene. The <see cref="loadOperation"/> (and <see cref="future"/>) will not complete until you call
  /// this function.
  /// </summary>
  public Future<Scene> activate() {
    loadOperation.allowSceneActivation = true;
    {if (onSceneActivation.valueOut(out var action)) action();}
    return future;
  }

  /// <summary>
  /// Scene load progress that reaches 1 when scene is loaded and waits for activation.
  /// </summary>
  public float percentCompleteUntilActivation => 
    Mathf.Clamp01(loadOperation.progress / SCENE_LOAD_VALUE_BEFORE_ACTIVATION);
}