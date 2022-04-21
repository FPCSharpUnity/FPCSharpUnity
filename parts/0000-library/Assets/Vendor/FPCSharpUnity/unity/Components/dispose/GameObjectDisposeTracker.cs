using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.dispose {
  public class GameObjectDisposeTracker : MonoBehaviour, IMB_OnDestroy, IDisposableTracker {
    [LazyProperty] static ILog log => Log.d.withScope(nameof(GameObjectDisposeTracker));

    readonly LazyVal<DisposableTracker> tracker;
    public int trackedCount => tracker.strict.trackedCount;
    public IEnumerable<TrackedDisposable> trackedDisposables => tracker.strict.trackedDisposables;

    public GameObjectDisposeTracker() {
      tracker = F.lazy(() => new DisposableTracker(
        log,
        // ReSharper disable ExplicitCallerInfoArgument
        callerFilePath: Log.d.isDebug() ? gameObject.transform.debugPath() : gameObject.name,
        callerMemberName: nameof(GameObjectDisposeTracker),
        callerLineNumber: -1
        // ReSharper restore ExplicitCallerInfoArgument
      ));
    }

    public void OnDestroy() => Dispose();

    public void Dispose() {
      if (tracker.value.valueOut(out var t))
        t.Dispose();
    }

    public void track(
      IDisposable a,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) => tracker.strict.track(
      a,
      // ReSharper disable ExplicitCallerInfoArgument
      callerMemberName: callerMemberName, callerFilePath: callerFilePath, callerLineNumber: callerLineNumber
      // ReSharper restore ExplicitCallerInfoArgument
    );

    public void untrack(IDisposable a) => tracker.strict.untrack(a);
  }

  public static class GameObjectDisposeTrackerOps {
    [Obsolete(
      "Unity does not invoke OnDestroy() if Awake() on the object was not previously invoked.\n" +
      "This can lead to a disposable tracker never being disposed of.\n" +
      "\n" +
      "Instead of using this please create a tracker manually and use/dispose of it yourself.\n" +
      "\n" +
      "https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDestroy.html"
    )]
    public static IDisposableTracker asDisposableTracker(this GameObject o) =>
      // If we're not in the play mode we don't want to instantiate any components.
      Application.isPlaying ? o.EnsureComponent<GameObjectDisposeTracker>() : NoOpDisposableTracker.instance;
    
    /// <summary>
    /// As <see cref="asDisposableTracker"/> but you are only supposed to invoke this from <see cref="IMB_Awake"/>.
    /// </summary>
    public static IDisposableTracker asDisposableTrackerFromAwake(this GameObject o) =>
      // If we're not in the play mode we don't want to instantiate any components.
      Application.isPlaying ? o.EnsureComponent<GameObjectDisposeTracker>() : NoOpDisposableTracker.instance;
    
    /// <summary>
    /// As <see cref="asDisposableTracker"/> but you are only supposed to invoke this from <see cref="IMB_Start"/>.
    /// </summary>
    public static IDisposableTracker asDisposableTrackerFromStart(this GameObject o) =>
      // If we're not in the play mode we don't want to instantiate any components.
      Application.isPlaying ? o.EnsureComponent<GameObjectDisposeTracker>() : NoOpDisposableTracker.instance;
  }
}