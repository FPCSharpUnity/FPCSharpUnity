using ExhaustiveMatching;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Concurrent;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities {
  [
    PublicAPI, 
    Closed(
      typeof(AsyncSceneLoadWithAutomaticActivationSingle), 
      typeof(AsyncSceneLoadWithAutomaticActivationAdditive),
      typeof(AsyncSceneLoadWithManualActivation)
    )
  ] public interface AsyncSceneLoad {
    /// <summary>The underlying load operation from Unity.</summary>
    AsyncOperation loadOperation { get; }
  
    /// <summary>The Unity scene that is being loaded.</summary>
    Scene sceneBeingLoaded { get; }
  
    /// <summary>Completes when the scene is loaded and activated.</summary>
    Future<Scene> future { get; }
  }
}