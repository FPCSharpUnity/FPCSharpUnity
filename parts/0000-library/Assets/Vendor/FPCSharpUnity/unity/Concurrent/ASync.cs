using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using FPCSharpUnity.unity.Concurrent.unity_web_request;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Utilities;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Logger;
using UnityEngine;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Concurrent; 

[PublicAPI, HasLogger(standalone = true)] public static partial class ASync {
  static ASyncHelperBehaviourEmpty coroutineHelper(GameObject go) =>
    go.EnsureComponent<ASyncHelperBehaviourEmpty>();
    
  /// <summary>
  /// A weak reference to the behaviour, so that the managed shell could be garbage collected in case the underlying
  /// GameObject is destroyed.
  /// </summary>
  static Option<WeakReference<ASyncHelperBehaviour>> _behaviour;

  public static ASyncHelperBehaviour behaviour { get {
    if (
      !_behaviour.valueOut(out var behaviourWr) || !behaviourWr.TryGetTarget(out var behaviour) || !behaviour
    ) {
      const string name = "ASync Helper";
      try {
        var go = new GameObject(name);
        // Notice that DontDestroyOnLoad can only be used in play mode and, as such, cannot
        // be part of an editor script.
        if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
        behaviour = go.EnsureComponent<ASyncHelperBehaviour>();
        _behaviour = Some.a(behaviour.weakRef());
        return behaviour;
      }
      catch (Exception e) {
        throw new Exception(
          $"Failed to create {name}! Make sure {nameof(ASync)} is initialized from main thread!",
          e
        );
      }
    }
    else {
      return behaviour;
    }
  } }

  static ASync() { behaviour.forSideEffects(); }

  public static Future<A> StartCoroutine<A>(
    Func<Promise<A>, IEnumerator> coroutine
  ) => Future.async<A>(p => behaviour.StartCoroutine(coroutine(p)));

  public static ICoroutine StartCoroutine(IEnumerator coroutine) =>
    new UnityCoroutine(behaviour, coroutine);

  public static ICoroutine WithDelay(
    float seconds, Action action,
    MonoBehaviour behaviour = null, TimeScale timeScale = TimeScale.Unity
  ) => WithDelay(Duration.fromSeconds(seconds), action, behaviour, timeScale);

  public static ICoroutine WithDelay(
    Duration duration, Action action,
    MonoBehaviour behaviour = null, TimeScale timeScale = TimeScale.Unity
  ) => WithDelay(duration, action, timeScale.asContext(), behaviour);

  public static ICoroutine WithDelay(
    Duration duration, Action action, ITimeContextUnity timeContext,
    MonoBehaviour behaviour = null
  ) {
    behaviour = behaviour ? behaviour : ASync.behaviour;
    var enumerator = WithDelayEnumerator(duration, action, timeContext);
    return new UnityCoroutine(behaviour, enumerator);
  }

  public static Future<Unit> WithDelayFuture(
    float seconds, MonoBehaviour behaviour = null, TimeScale timeScale = TimeScale.Unity
  ) => WithDelay(seconds, () => { }, behaviour: behaviour, timeScale: timeScale)
    .toFuture().discardValue();

  public static ICoroutine NextFrame(Action action) => NextFrame(behaviour, action);

  public static ICoroutine NextFrame(GameObject gameObject, Action action) =>
    NextFrame(coroutineHelper(gameObject), action);

  public static ICoroutine NextFrame(MonoBehaviour behaviour, Action action) {
    var enumerator = NextFrameEnumerator(action);
    return new UnityCoroutine(behaviour, enumerator);
  }

  public static ICoroutine AfterXFrames(
    int framesToSkip, Action action
  ) => AfterXFrames(behaviour, framesToSkip, action);

  public static ICoroutine AfterXFrames(
    MonoBehaviour behaviour, int framesToSkip, Action action
  ) {
    return EveryFrame(behaviour, () => {
      if (framesToSkip <= 0) {
        action();
        return false;
      }
      else {
        framesToSkip--;
        return true;
      }
    });
  }

  public static void NextPostRender(Camera camera, Action action) => NextPostRender(camera, 1, action);

  public static void NextPostRender(Camera camera, int afterFrames, Action action) {
    var pr = camera.gameObject.AddComponent<NextPostRenderBehaviour>();
    pr.init(action, afterFrames);
  }

  /// <summary>
  /// Allows you to turn the 'EveryX*' functions into an <see cref="IRxObservable{A}"/>.
  /// </summary>
  /// <param name="tracker">lifetime for the coroutine.</param>
  /// <param name="createCoroutine">function that needs to be provided to 'EveryX*' functions.</param>
  /// <example><code><![CDATA[
  /// var every5SecondsRx = ASync.EveryXObservable(tracker, f => ASync.EveryXSeconds(5f, f));
  /// ]]></code></example>
  public static (IRxObservable<Unit> observable, ICoroutine coroutine) EveryXObservable(
    ITracker tracker, Func<Func<bool>, ICoroutine> createCoroutine
  ) {
    var subject = new Subject<Unit>();
    var coroutine = createCoroutine(() => {
      subject.push(Unit._);
      return true;
    });
    tracker.track(coroutine);
    return (subject, coroutine);
  }

  /// <summary>Do a thing every frame until <see cref="f"/> returns false.</summary>
  public static ICoroutine EveryFrame(Func<bool> f) => EveryFrame(behaviour, f);

  /// <inheritdoc cref="EveryFrame(System.Func{bool})"/>
  public static ICoroutine EveryFrame(GameObject go, Func<bool> f) => EveryFrame(coroutineHelper(go), f);

  /// <inheritdoc cref="EveryFrame(System.Func{bool})"/>
  public static ICoroutine EveryFrame(MonoBehaviour behaviour, Func<bool> f) {
    var enumerator = EveryWaitEnumerator(null, f);
    return new UnityCoroutine(behaviour, enumerator);
  }

  /// <summary>
  /// Same as <see cref="EveryXSeconds(float,System.Func{bool})"/>,
  /// but the first invocation will be delayed for <paramref name="seconds"/> seconds
  /// </summary>
  public static ICoroutine EveryXSecondsWithDelay(float seconds, Func<bool> f) {
    var enumerator = EveryWaitEnumeratorWithInitialDelay(new WaitForSecondsRealtimeReusable(seconds), f);
    return new UnityCoroutine(behaviour, enumerator);
  }

  /// <summary>
  /// Do thing every X seconds until f returns false.
  /// Note: <paramref name="f"/> will be triggered immediately.
  /// </summary>
  public static ICoroutine EveryXSeconds(float seconds, Func<bool> f) => EveryXSeconds(seconds, behaviour, f);

  /// <summary>Do thing every X seconds until f returns false.</summary>
  public static ICoroutine EveryXSeconds(float seconds, GameObject go, Func<bool> f) =>
    EveryXSeconds(seconds, coroutineHelper(go), f);

  /// <summary>Do thing every X seconds until f returns false.</summary>
  public static ICoroutine EveryXSeconds(float seconds, MonoBehaviour behaviour, Func<bool> f) {
    var enumerator = EveryWaitEnumerator(new WaitForSecondsRealtimeReusable(seconds), f);
    return new UnityCoroutine(behaviour, enumerator);
  }

  /* Returns action that cancels our delayed call. */
  public static Action WithDelayFixedUpdate(GameObject go, float delay, Action act) {
    // TODO: probably this needs to be rewritten to use only one global component for fixed update
    if (delay < 1e-6) {
      // if delay is 0 call immediately
      // this is because we don't want to wait a single fixed update
      act();
      return () => { };
    }
    else {
      var component = go.AddComponent<ASyncFixedUpdateHelperBehaviour>();
      component.init(delay, act);
      return () => {
        if (component) UnityEngine.Object.Destroy(component);
      };
    }
  }

  /// <summary>
  /// Turn this request to <see cref="ICancellableFuture{A}"/>. Automatically cleans up the request.
  /// </summary>
  public static ICancellableFuture<Either<WebRequestError, A>> toFutureCancellable<A>(
    this UnityWebRequest req, AcceptedResponseCodes acceptedResponseCodes, 
    Func<UnityWebRequest, A> onSuccess
  ) {
    var f = Future.async<Either<WebRequestError, A>>(out var promise);
    var op = req.SendWebRequest();
    // Was `req.Dispose()` invoked?
    var reqDisposed = false;
    op.completed += _ => {
      var responseCode = req.responseCode;
      if (req.result.toNonSuccessfulResult().valueOut(out var nonSuccessfulResult)) {
        // Capture data before disposing the request.
        var url = new Url(req.url);
        var error =
          req.error == "User Aborted" ? new WebRequestError(new UserAborted(url))
          : responseCode == 0 && req.error == "Unknown Error" ? new WebRequestError(new NoInternetError(url))
          : new WebRequestError(new NonSuccessResult(
            url, nonSuccessfulResult, responseCode: responseCode, error: req.error,
            bodyText: Try.a(() => Option.a(req.downloadHandler).flatMapM(_ => Option.a(_.text))).toOption().flatten(),
            // When a UnityWebRequest ends with the result, DataProcessingError, the message describing the error is in
            // the download handler.
            //
            // https://docs.unity3d.com/ScriptReference/Networking.DownloadHandler-error.html
            dataProcessingError:
              req.result == UnityWebRequest.Result.DataProcessingError && req.downloadHandler != null
                ? Some.a(req.downloadHandler.error)
                : None._
          ));
        disposeReq("non-successful result");
        promise.complete(error);
      }
      else if (!acceptedResponseCodes.contains(responseCode)) {
        // Capture data before disposing the request.
        var error = new WebRequestError(new UnacceptableResponseCode(
          new Url(req.url), responseCode: responseCode, headers: req.GetResponseHeaders().ToImmutableDictionary(),
          acceptedResponseCodes: acceptedResponseCodes,
          // DownloadHandlerAssetBundle does not support text access.
          responseText: req.downloadHandler != null && req.downloadHandler is not DownloadHandlerAssetBundle
            ? Some.a(req.downloadHandler.text) : None._
        ));
        disposeReq("response code not accepted");
        promise.complete(error);
      }
      else {
        Either<WebRequestError, A> result;
        try {
          result = onSuccess(req);
          disposeReq("finished processing");
        }
        catch (Exception e) {
          result = new WebRequestError(new SuccessHandlerFailed(new Url(req.url), responseCode: responseCode, e));
          disposeReq("processing failed with exception", e);
        }
        promise.complete(result);
      }
    };
    return CancellableFuture.a(f, cancel);

    void cancel() {
      // Only do something if the request is not already disposed of.
      if (!reqDisposed) {
        // Aborted UnityWebRequests are considered to have encountered a system error. Either the isNetworkError or
        // the isHttpError property will return true and the error property will be "User Aborted".
        //
        // https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.Abort.html
        op.webRequest.Abort();
      }
    };
      
    void disposeReq(string reason, [CanBeNull] Exception e = null) {
      if (reqDisposed) return;
      log.mVerbose(
        $"Disposing of request to {req.url.echoNameOfDs()} for {reason.echoDs()}"
        + (e == null ? "" : $"\nException: {e}")
      );
      req.Dispose();
      reqDisposed = true;
    }
  }

  /// <summary>Turn this request to future. Automatically cleans up the request.</summary>
  public static Future<Either<WebRequestError, A>> toFuture<A>(
    this UnityWebRequest req, AcceptedResponseCodes acceptedResponseCodes,
    Func<UnityWebRequest, A> onSuccess
  ) => req.toFutureCancellable(acceptedResponseCodes, onSuccess).future;

  public static Future<Either<LogEntry, A>> toFutureSimple<A>(
    this UnityWebRequest req, AcceptedResponseCodes acceptedResponseCodes, Func<UnityWebRequest, A> onSuccess
  ) => req.toFuture(acceptedResponseCodes, onSuccess).map(_ => _.mapLeftM(err => err.simplify));

  /* Wait until enumerator is completed and then do action */
  public static IEnumerator afterThis(this IEnumerator enumerator, Action action) {
    while (enumerator.MoveNext()) yield return enumerator.Current;
    action();
  }

  public static IEnumerator WithDelayEnumerator(
    Duration duration, Action action, ITimeContextUnity timeContext
  ) {
    if (timeContext == TimeContextU.playMode) {
      // WaitForSeconds is optimized Unity in native code
      // waiters that extend CustomYieldInstruction (eg. WaitForSecondsRealtime) call C# code every frame,
      // so we don't need special handling for them
      yield return new WaitForSeconds(duration.seconds);
    }
    else {
      var waiter = timeContext == TimeContextU.fixedTime ? CoroutineHelpers.waitFixed : null;
      var waitTime = timeContext.passedSinceStartup + duration.toTimeSpan;
      while (waitTime > timeContext.passedSinceStartup) yield return waiter;
    }
    action();
  }

  /// <summary>Runs action forever every frame.</summary>
  public static IEnumerator everyFrameEnumerator(Action action) {
    while (true) {
      action();
      yield return null;
    }
    // ReSharper disable once IteratorNeverReturns
  }
    
  public static IEnumerator NextFrameEnumerator(Action action) {
    yield return null;
    action();
  }

  public static IEnumerator EveryWaitEnumerator(IEnumerator wait, Func<bool> f) {
    // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
    while (f()) yield return wait;
  }
    
  public static IEnumerator EveryWaitEnumeratorWithInitialDelay(IEnumerator wait, Func<bool> f) {
    do {
      yield return wait;
    } while (f());
  }

  public static IRxObservable<bool> onAppPause => behaviour.onPause;

  public static IRxObservable<Unit> onAppQuit => behaviour.onQuit;

  public static IRxObservable<Unit> onLateUpdate => behaviour.onLateUpdate;
    
  public static IRxObservable<Unit> onUpdate => behaviour.onUpdate;
    
  public static IRxVal<bool> hasFocus => behaviour.hasFocus;

  /**
     * Takes a function that transforms an element into a future and
     * applies it to all elements in given sequence.
     *
     * However instead of applying all elements concurrently it waits
     * for the future from previous element to complete before applying
     * the next element.
     *
     * Returns reactive value that can be used to observe current stage
     * of the application.
     **/
  public static IRxVal<Option<B>> inAsyncSeq<A, B>(
    this IEnumerable<A> enumerable, Func<A, Future<B>> asyncAction
  ) {
    var rxRef = RxRef.a(F.none<B>());
    inAsyncSeq(enumerable.GetEnumerator(), rxRef, asyncAction);
    return rxRef;
  }

  static void inAsyncSeq<A, B>(
    IEnumerator<A> e, IRxRef<Option<B>> rxRef,
    Func<A, Future<B>> asyncAction
  ) {
    if (! e.MoveNext()) return;
    asyncAction(e.Current).onComplete(b => {
      rxRef.value = Some.a(b);
      inAsyncSeq(e, rxRef, asyncAction);
    });
  }

  /// <summary>
  /// Split running action over collection over N chunks separated by a given yield instruction.
  /// </summary>
  public static IEnumerator overNYieldInstructions<A>(
    ICollection<A> collection, int n, Action<A, int> onA, YieldInstruction instruction = null
  ) {
    var chunkSize = collection.Count / n;
    var idx = 0;
    foreach (var a in collection) {
      onA(a, idx);
      if (idx % chunkSize == 0) yield return instruction;
      idx++;
    }
  }
}

public class WaitForSecondsUnscaled : ReusableYieldInstruction {
  readonly float time;
  float waitTime;

  public WaitForSecondsUnscaled(float time) { this.time = time; }

  protected override void init() => waitTime = Time.unscaledTime + time;

  public override bool keepWaiting => Time.unscaledTime < waitTime;
}

/** WaitForSecondsRealtime from Unity is not reusable. */
public class WaitForSecondsRealtimeReusable : ReusableYieldInstruction {
  readonly float time;
  float finishTime;

  public WaitForSecondsRealtimeReusable(float time) { this.time = time; }

  protected override void init() => finishTime = Time.realtimeSinceStartup + time;

  public override bool keepWaiting => Time.realtimeSinceStartup < finishTime;
}

// If we extend YieldInstruction we can not reuse its instances
// because it inits end condition only in constructor.
// We can reuse instances of ReusableYieldInstruction but we can't
// use the same instance in multiple places at once
public abstract class ReusableYieldInstruction : IEnumerator {
  bool inited;

  protected abstract void init();

  public abstract bool keepWaiting { get; }

  public bool MoveNext() {
    if (!inited) {
      init();
      inited = true;
    }
    var result = keepWaiting;
    if (!result) inited = false;
    return result;
  }

  // Never gets called
  public void Reset() {}

  // https://docs.unity3d.com/ScriptReference/CustomYieldInstruction.html
  public object Current => null;
}