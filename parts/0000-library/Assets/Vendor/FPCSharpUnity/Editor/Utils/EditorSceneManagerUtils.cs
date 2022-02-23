using System;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Data.scenes;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Editor.Utils {
  public static class EditorSceneManagerUtils {
    public static A withScene<A>(ScenePath scenePath, Func<Scene, A> f) {
      var isLoaded = SceneManagerUtils.loadedScenes.Any(s => s.path == scenePath);
      var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
      try {
        return f(scene);
      }
      finally {
        if (!isLoaded) SceneManager.UnloadSceneAsync(scene);
      }
    }

    public static B withSceneObject<A, B>(
      this RuntimeSceneRefWithComponent<A> sceneRef, Func<A, B> f
    ) where A : Component =>
      withScene(sceneRef.scenePath, scene => f(scene.findComponentOnRootGameObjects<A>().rightOrThrow));
  }
}