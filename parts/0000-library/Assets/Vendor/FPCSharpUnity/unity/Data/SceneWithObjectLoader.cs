using FPCSharpUnity.unity.Data.scenes;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Data {
  public static class SceneWithObjectLoader {
    /// <summary>
    /// Loads the specified scene and then tries to find to find the specified component of type <see cref="A"/> on the
    /// root game objects.
    /// </summary>
    /// <note>If the component is not found, the scene is not unloaded.</note>
    public static Future<Either<ErrorMsg, A>> load<A>(
      ScenePath scenePath, LoadSceneMode loadSceneMode = LoadSceneMode.Single
    ) where A : Component =>
      Future.successful(
        Try.a(() => SceneManagerBetter.instance.loadSceneAsyncWithAutomaticActivation(scenePath, loadSceneMode))
          .toEither().mapLeft(err => new ErrorMsg($"Error while loading scene '{scenePath}': {err}"))
      ).flatMapT(op => op.future.map(scene =>
        scene.findComponentOnRootGameObjects<A>()
      ));
  }
}