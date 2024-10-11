#if WWW_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Dispose;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using GenerationAttributes;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using UnityEngine;
using Object = UnityEngine.Object;

// obsolete WWW
#pragma warning disable 618

namespace FPCSharpUnity.unity.Net {
  public partial class ImageDownloader {
    [Record]
    partial struct InternalResult {
      public readonly Future<Either<WWWError, UsageCountedDisposable<Texture2D>>> future;
      
      public Result toResult() => new Result(future.mapT(_ => _.use()));
    } 
    
    [Record]
    public partial struct Result {
      public readonly Future<Either<WWWError, Disposable<Texture2D>>> future;
    }
    
    public static readonly ImageDownloader instance = new ImageDownloader();

    readonly Dictionary<Url, InternalResult> cache = new Dictionary<Url, InternalResult>();
    readonly ASyncNAtATimeQueue<Url, Either<WWWError, UsageCountedDisposable<Texture2D>>> queue;

    ImageDownloader() {
      queue = new ASyncNAtATimeQueue<
        Url,
        Either<WWWError, UsageCountedDisposable<Texture2D>>
      >(2, url => download(url).future);
    }

    InternalResult download(Url url) => new InternalResult(
      Future.async<Either<WWWError, UsageCountedDisposable<Texture2D>>>((promise, f) => {
        ASync.StartCoroutine(textureLoader(
          // TODO: change to UnityWebRequest.GetTexture from old WWW implementation
          // Here is sample code, but I don't remember why it is not used. Try it and see
          // for yourself.
//          var f = UnityWebRequest.GetTexture(staticAd.image.url).toFuture().flatMapT(req => {
//            var dlHandler = (DownloadHandlerTexture) req.downloadHandler;
//            var texture = dlHandler.texture;
//            if (texture) return new Either<ErrorMsg, Texture>(texture);
//            return new ErrorMsg($"Can't download texture from url{staticAd.image.url}");
//          });
          new WWW(url), promise,
          onDispose: t => {
            Object.Destroy(t);
            cache.Remove(url);
            if (Log.d.isDebug()) Log.d.debug($"{nameof(ImageDownloader)} disposed texture: {url}");
          })
        );

        f.onComplete(e => {
          // remove from cache if image was not downloaded
          if (e.isLeft) cache.Remove(url);
        });
      })
    );

    // TODO: make it possible to dispose image before it started downloading / while downloading
    public Result loadImage(Url url, bool ignoreQueue = false) =>
      cache
        .getOrUpdate(url, () => ignoreQueue ? download(url) : new InternalResult(queue.enqueue(url)))
        .toResult();

    static IEnumerator textureLoader(
      WWW www,
      Promise<Either<WWWError, UsageCountedDisposable<Texture2D>>> promise,
      Action<Texture2D> onDispose
    ) {
      yield return www;
      promise.complete(
        string.IsNullOrEmpty(www.error)
          ? WWWExts.asTexture(www).mapRightM(t => UsageCountedDisposable.a(t, onDispose))
          : new WWWError(www, www.error)
      );
    }
  }
}
#endif