using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static class ASyncOperationExts {
    public static Future<AsyncOperation> toFuture(this AsyncOperation op) {
      var f = Future.async<AsyncOperation>(out var p);
      op.completed += p.complete;
      return f;
    }

    public static Future<IAsyncOperation> toFuture(this IAsyncOperation op) => 
      FutureU.fromBusyLoop(() => op.isDone.opt(op));

    public static IAsyncOperation wrap(this AsyncOperation op) => 
      new WrappedAsyncOperation(op);
  }
}
