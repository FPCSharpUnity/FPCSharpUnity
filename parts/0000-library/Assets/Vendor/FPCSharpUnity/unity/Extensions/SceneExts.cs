using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Data.scenes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static class SceneExts {
    // includes inactive objects
    // may not work on scene awake
    // http://forum.unity3d.com/threads/bug-getrootgameobjects-is-not-working-in-awake.379317/
    public static IEnumerable<T> findObjectsOfTypeAll<T>(
      this in Scene scene, bool includeInactive = true
    ) where T : Component =>
      scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(includeInactive));

    public static IEnumerable<T> findObjectsOfTypeAll<T>(
      this IEnumerable<Scene> scenes, bool includeInactive = true
    ) where T : Component => 
      scenes.SelectMany(scene => scene.findObjectsOfTypeAll<T>(includeInactive));

    /// <summary>
    /// Retrieve first <see cref="A"/> attached to a root <see cref="GameObject"/> in the <see cref="Scene"/>.
    /// </summary>
    public static Either<ErrorMsg, A> findComponentOnRootGameObjects<A>(this in Scene scene) where A : Component =>
      scene.GetRootGameObjects()
      .collectFirst(static go => go.GetComponent<A>().opt())
      .toRight(scene.path, static path => new ErrorMsg($"Couldn't find {typeof(A)} in scene '{path}' root game objects"));

    public static SceneName sceneName(this in Scene scene) => new SceneName(scene.name);
    public static ScenePath scenePath(this in Scene scene) => new ScenePath(scene.path);
    
    public static Either<SceneBuildIndexError, SceneBuildIndex> sceneBuildIndex(this in Scene scene) {
      var idx = scene.buildIndex;
      
      // If the Scene is loaded through an AssetBundle, Scene.buildIndex returns -1.
      if (idx == -1) return SceneBuildIndexError.SceneLoadedThroughAssetBundle;
      // A Scene that is not added to the Scenes in Build window returns a buildIndex one more than the highest in the
      // list. For example, if you don’t add a Scene to a Scenes in Build window that already has 6 Scenes in it,
      // then Scene.buildIndex returns 6 as its index .
      else if (idx >= SceneManager.sceneCountInBuildSettings) return SceneBuildIndexError.SceneNotIncludedInBuildScenesList;
      else return new SceneBuildIndex(idx);
    }
  }
}
