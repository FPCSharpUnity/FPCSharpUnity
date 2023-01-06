using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static class ASyncOperationExts {
    public static Future<AsyncOp> toFuture<AsyncOp>(this AsyncOp op) where AsyncOp : AsyncOperation {
      var f = Future.async<AsyncOp>(out var p);
      op.completed += _ => p.complete(op);
      return f;
    }

    public static Future<IAsyncOperation> toFuture(this IAsyncOperation op) => 
      FutureU.fromBusyLoop(() => op.isDone.opt(op));

    public static IAsyncOperation wrap(this AsyncOperation op) => 
      new WrappedAsyncOperation(op);
  }
}
