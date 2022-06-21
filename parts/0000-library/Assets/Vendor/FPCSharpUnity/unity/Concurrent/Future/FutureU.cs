using System;
using System.Collections;
using System.Runtime.CompilerServices;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Concurrent {
  [PublicAPI] public static class FutureU {
    public static Future<A> delayFrames<A>(int framesToSkip, Func<A> createValue) =>
      Future.a<A>(p => ASync.AfterXFrames(framesToSkip, () => p.complete(createValue())));

    public static Future<A> delayFrames<A>(int framesToSkip, A value) =>
      Future.a<A>(p => ASync.AfterXFrames(framesToSkip, () => p.complete(value)));
    
    public static Future<A> delayFrames<A>(ITracker tracker, int framesToSkip, Func<A> createValue) =>
      Future.a<A>(p => tracker.track(ASync.AfterXFrames(framesToSkip, () => p.complete(createValue()))));

    public static Future<A> delayFrames<A>(ITracker tracker, int framesToSkip, A value) =>
      Future.a<A>(p => tracker.track(ASync.AfterXFrames(framesToSkip, () => p.complete(value))));

    public static Future<Unit> delayFrames(ITracker tracker, int framesToSkip) =>
      Future.a<Unit>(p => tracker.track(ASync.AfterXFrames(framesToSkip, () => p.complete(Unit._))));

    public static Future<A> delayOneFrame<A>(A value) => delayFrames(1, value);
    public static Future<A> delayOneFrame<A>(Func<A> createValue) => delayFrames(1, createValue);
    public static Future<Unit> delayOneFrame() => delayOneFrame(Unit._);
    
    public static Future<A> delayOneFrame<A>(ITracker tracker, A value) => delayFrames(tracker, 1, value);
    public static Future<A> delayOneFrame<A>(ITracker tracker, Func<A> createValue) => delayFrames(tracker, 1, createValue);
    public static Future<Unit> delayOneFrame(ITracker tracker) => delayOneFrame(tracker, Unit._);
      
    public static Future<bool> fromCoroutine(IEnumerator enumerator) =>
      Future.fromCoroutine(ASync.StartCoroutine(enumerator));

    /// <summary>Complete when checker returns `Some`.</summary>
    public static Future<A> fromBusyLoop<A>(
      Func<Option<A>> checker, YieldInstruction delay=null
    ) => Future.async<A>(p => ASync.StartCoroutine(busyLoopEnum(delay, p, checker)));

    /// <summary>Complete when checker returns true.</summary>
    public static Future<Unit> fromBusyLoop(
      Func<bool> checker, YieldInstruction delay=null
    ) => Future.async<Unit>(p => ASync.StartCoroutine(busyLoopEnum(delay, p, checker)));

    /** Measures how much time has passed from call to timed to future completion. **/
    public static Future<Tpl<A, Duration>> timed<A>(this Future<A> future) {
      var startTime = Time.realtimeSinceStartup;
      return future.map(a => {
        var time = Time.realtimeSinceStartup - startTime;
        return Tpl.a(a, Duration.fromSeconds(time));
      });
    }

    static IEnumerator busyLoopEnum<A>(YieldInstruction delay, Promise<A> p, Func<Option<A>> checker) {
      var valOpt = checker();
      while (valOpt.isNone) {
        yield return delay;
        valOpt = checker();
      }
      p.complete(valOpt.get);
    }

    static IEnumerator busyLoopEnum(YieldInstruction delay, Promise<Unit> p, Func<bool> checker) {
      while (!checker()) {
        yield return delay;
      }
      p.complete(F.unit);
    }
    
    /// <summary>
    /// Emitted events are delayed by one frame, using <see cref="ASync"/> which is tracked by <see cref="tracker"/>.
    /// </summary>
    public static ISubscription subscribeWithXFrameDelayUnity<A>(
      this IRxObservable<A> observable,
      ITracker tracker,
      int framesToDelay,
      Action<A> onEvent,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0,
      [Implicit] ILog log=default
    ) => observable.subscribeWithSubTracker(
      tracker: tracker,
      onChange: (a, subTracker) => delayFrames(subTracker, framesToDelay).onComplete(_ => onEvent(a)),
      // ReSharper disable ExplicitCallerInfoArgument
      callerMemberName: callerMemberName, callerFilePath: callerFilePath,
      callerLineNumber: callerLineNumber
      // ReSharper restore ExplicitCallerInfoArgument
    );
  }
}
