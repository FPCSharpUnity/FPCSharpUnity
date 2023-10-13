using FPCSharpUnity.unity.Concurrent;
using JetBrains.Annotations;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.reactive;
using UnityEngine;

namespace FPCSharpUnity.unity.Reactive {
  [PublicAPI] public static class ObservableOpsU {
    /// <summary>
    /// <see cref="ObservableOps.oncePerTick{A,B}"/> that dispatches on late update.
    /// </summary>
    public static IRxObservable<A> oncePerFrame<A>(
      this IRxObservable<A> o
    ) =>
      o.oncePerTick(ASync.onLateUpdate);
    
    /// <summary>
    /// Only emit an event if it's the first event in this frame.
    /// </summary>
    /// <param name="o"></param>
    /// <param name="frameNoRx">Reference to last frame number. Can be shared between several observables.</param>
    public static IRxObservable<A> oncePerFrameShared<A>(
      this IRxObservable<A> o, Ref<int> frameNoRx
    ) =>
      new Observable<A>((onEvent, target) => o.subscribe(
        NoOpDisposableTracker.instance, 
        a => {
          var frameNo = Time.frameCount;
          if (frameNoRx.value != frameNo) {
            frameNoRx.value = frameNo;
            onEvent(a);
          }
        },
        targetInspectable: target
      ));
    
    /// <summary> Delays each event by given number of frames. </summary>
    public static IRxObservable<A> delayed<A>(
      this IRxObservable<A> o, int frames
    ) =>
      new Observable<A>(subscribeFn: (onEvent, self) => o.subscribe(
        NoOpDisposableTracker.instance,
        onEvent: v => ASync.AfterXFrames(frames, () => onEvent(v)),
        targetInspectable: self
      ));
  }
}