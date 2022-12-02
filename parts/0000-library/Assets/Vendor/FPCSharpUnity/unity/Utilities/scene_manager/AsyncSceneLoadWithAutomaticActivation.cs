using ExhaustiveMatching;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities {
  /// <summary>
  /// Asynchronous scene load where the scene activation happens automatically.
  /// </summary>
  [
    PublicAPI, 
    Closed(typeof(AsyncSceneLoadWithAutomaticActivationSingle), typeof(AsyncSceneLoadWithAutomaticActivationAdditive))
  ] public interface AsyncSceneLoadWithAutomaticActivation : AsyncSceneLoad {}

  /// <summary>
  /// <see cref="SceneManagerBetter.loadSceneAsyncWithAutomaticActivation"/> when
  /// <see cref="LoadSceneMode"/> is <see cref="LoadSceneMode.Single"/>.
  /// </summary>
  [PublicAPI] sealed class AsyncSceneLoadWithAutomaticActivationSingle : AsyncSceneLoadWithAutomaticActivation {
    readonly AsyncSceneLoadWithManualActivation manual;
    public AsyncOperation loadOperation => manual.loadOperation;
    public Scene sceneBeingLoaded => manual.sceneBeingLoaded;
    public Future<Scene> future => manual.future;

    public AsyncSceneLoadWithAutomaticActivationSingle(AsyncSceneLoadWithManualActivation manual) {
      this.manual = manual;
      // We simulate the automatic activation ourselves because we need the appropriate callbacks to fire.
      manual.whenReadyToActivate.onComplete(static _ => _.activate());
    }
  }

  /// <summary>
  /// <see cref="SceneManagerBetter.loadSceneAsyncWithAutomaticActivation"/> when
  /// <see cref="LoadSceneMode"/> is <see cref="LoadSceneMode.Additive"/>.
  /// </summary>
  [PublicAPI] sealed class AsyncSceneLoadWithAutomaticActivationAdditive : AsyncSceneLoadWithAutomaticActivation {
    public AsyncOperation loadOperation { get; }
    public Scene sceneBeingLoaded { get; }
    public Future<Scene> future { get; }

    public AsyncSceneLoadWithAutomaticActivationAdditive(AsyncOperation loadOperation, Scene sceneBeingLoaded) {
      this.loadOperation = loadOperation;
      this.sceneBeingLoaded = sceneBeingLoaded;
    
      loadOperation.allowSceneActivation = true;
      future = loadOperation.toFuture().map(sceneBeingLoaded, static (_, sceneBeingLoaded) => sceneBeingLoaded);
    }
  }
}