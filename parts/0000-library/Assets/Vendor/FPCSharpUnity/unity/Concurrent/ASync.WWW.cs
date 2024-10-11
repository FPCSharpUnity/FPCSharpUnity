#if WWW_ENABLED
using System.Collections;
using FPCSharpUnity.unity.Extensions;
using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;

// obsolete WWW
#pragma warning disable 618

namespace FPCSharpUnity.unity.Concurrent {
  public static partial class ASync {
    /* Do async cancellable WWW request. */
    public static Cancellable<Future<Either<Cancelled, Either<WWWError, WWW>>>> toFuture(this WWW www) {
      var f = Future.async<Either<Cancelled, Either<WWWError, WWW>>>(out var promise);

      var wwwCoroutine = StartCoroutine(WWWEnumerator(www, promise));

      return Cancellable.a(f, () => {
        if (www.isDone) return false;

        wwwCoroutine.stop();
        www.Dispose();
        promise.complete(new Either<Cancelled, Either<WWWError, WWW>>(Cancelled.instance));
        return true;
      });
    }  
    
    [PublicAPI]
    public static Cancellable<Future<Either<Cancelled, Either<WWWError, Texture2D>>>> asTexture(
      this Cancellable<Future<Either<Cancelled, Either<WWWError, WWW>>>> cancellable
    ) => cancellable.map(f => f.map(e => e.mapRightM(_ => _.asTexture())));

    public static IEnumerator WWWEnumerator(WWW www) { yield return www; }

    public static IEnumerator WWWEnumerator(WWW www, Promise<Either<Cancelled, Either<WWWError, WWW>>> promise) =>
      WWWEnumerator(www).afterThis(() => promise.complete(
        Either<Cancelled, Either<WWWError, WWW>>.Right(www.toEither())
      ));
  }
}
#endif