using System;
using System.Runtime.CompilerServices;
using FPCSharpUnity.unity.Components.dispose;
using FPCSharpUnity.unity.Concurrent;
using JetBrains.Annotations;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Reactive {
  [PublicAPI] public static class ObservableOpsU {
    public static ISubscription subscribe<A>(
      this IRxObservable<A> observable, GameObject tracker, Action<A> onEvent,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) => observable.subscribe(
      tracker: tracker.asDisposableTracker(), onEvent: onEvent,
      // ReSharper disable ExplicitCallerInfoArgument
      callerMemberName: callerMemberName, callerFilePath: callerFilePath,
      callerLineNumber: callerLineNumber
      // ReSharper restore ExplicitCallerInfoArgument
    );

    /// <summary>
    /// <see cref="ObservableOps.oncePerTick{A,B}"/> that dispatches on late update.
    /// </summary>
    public static IRxObservable<A> oncePerFrame<A>(this IRxObservable<A> o) =>
      o.oncePerTick(ASync.onLateUpdate);
    
    /// <summary>
    /// Only emit an event if it's the first event in this frame.
    /// </summary>
    /// <param name="o"></param>
    /// <param name="frameNoRx">Reference to last frame number. Can be shared between several observables.</param>
    public static IRxObservable<A> oncePerFrameShared<A>(this IRxObservable<A> o, Ref<int> frameNoRx) =>
      new Observable<A>(onEvent => o.subscribe(NoOpDisposableTracker.instance, a => {
        var frameNo = Time.frameCount;
        if (frameNoRx.value != frameNo) {
          frameNoRx.value = frameNo;
          onEvent(a);
        }
      }));
  }
}