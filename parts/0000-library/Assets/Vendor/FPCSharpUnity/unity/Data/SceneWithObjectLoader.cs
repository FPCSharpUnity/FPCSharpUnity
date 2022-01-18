using FPCSharpUnity.unity.Data.scenes;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.functional;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Data {
  public static class SceneWithObjectLoader {
    public static Future<Either<ErrorMsg, A>> load<A>(
      ScenePath scenePath, LoadSceneMode loadSceneMode = LoadSceneMode.Single
    ) where A : Component =>
      Future.successful(
        F.doTry(() => SceneManager.LoadSceneAsync(scenePath, loadSceneMode))
          .toEither().mapLeft(err => new ErrorMsg($"Error while loading scene '{scenePath}': {err}"))
      ).flatMapT(op => op.toFuture().map(_ =>
        SceneManager.GetSceneByPath(scenePath).findComponentOnRootGameObjects<A>()
      ));
  }
}