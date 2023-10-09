using System;
using System.Collections;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.unity.Functional;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.ResourceReference {
  public static class ResourceLoader {
    static ErrorMsg notFound<A>(string path) => new ErrorMsg(
      $"Resource of type {typeof(A).FullName} not found at: {path}"
    );
    
    [PublicAPI]
    public static Either<ErrorMsg, A> load<A>(PathStr loadPath) where A : Object {
      var path = loadPath.unityPath();
      var a = Resources.Load<A>(path);
      if (a) return a;
      return notFound<A>(path);
    }

    public static Tpl<IAsyncOperation, Future<Either<ErrorMsg, A>>> loadAsync<A>(
      PathStr loadPath
    ) where A : Object {
      var path = loadPath.unityPath();
      IResourceRequest request = new WrappedResourceRequest(Resources.LoadAsync<A>(path));
      return Tpl.a(
        request.upcast(default(IAsyncOperation)), 
        Future.async<Either<ErrorMsg, A>>(
          p => ASync.StartCoroutine(waitForLoadCoroutine<A>(request, p.complete, path))
        )
      );
    }

    [PublicAPI]
    public static Tpl<IAsyncOperation, Future<A>> loadAsyncIgnoreErrors<A>(
      PathStr loadPath, [Implicit] ILog log=default, LogLevel logLevel=LogLevel.ERROR
    ) where A : Object =>
      loadAsync<A>(loadPath).map2(future => future.dropErrorAndLog(log, logLevel));

    static IEnumerator waitForLoadCoroutine<A>(
      IResourceRequest request, Action<Either<ErrorMsg, A>> whenDone, string path
    ) where A : Object {
      yield return request.yieldInstruction;
      if (request.asset is A a) {
        whenDone(a);
      }
      else {
        whenDone(notFound<A>(path));
      }
    }
  }
}