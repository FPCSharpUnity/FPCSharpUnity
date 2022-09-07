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
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Dispose;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.dispose {
  /// <summary>
  /// <see cref="IDisposableTracker"/> that gets disposed when the <see cref="GameObject"/> is destroyed. It works
  /// correctly even if the <see cref="GameObject"/> was never enabled.
  /// </summary>
  public class GameObjectDisposeTracker : MonoBehaviour, IMB_OnDestroy, IDisposableTracker, IMB_Awake {
    [LazyProperty] static ILog log => Log.d.withScope(nameof(GameObjectDisposeTracker));

    /// <summary>
    /// List of <see cref="GameObjectDisposeTracker"/> which were created, but <see cref="Awake"/> was not called on
    /// them yet.
    /// </summary>
    static readonly List<GameObjectDisposeTracker> trackersWaitingForAwake = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void init() {
      ASync.onUpdate.subscribe(NeverDisposeDisposableTracker.instance, _ => {
        // OnDestroy only gets called on a Component only if Awake was called first. This code ensures that Dispose gets
        // called when the GameObject is destroyed, even if Awake was never called.
        for (var i = 0; i < trackersWaitingForAwake.Count; i++) {
          var current = trackersWaitingForAwake[i];
          if (!current) {
            // Tracker Component was destroyed before Awake was called on it. So we need to call dispose here manually.
            trackersWaitingForAwake.removeAtReplacingWithLast(i);
            current.Dispose();
          }
          else if (current.awakeCalled) {
            // Awake was called. That means OnDestroy will work on this tracker Component. Don't need to check it on
            // update anymore.
            trackersWaitingForAwake.removeAtReplacingWithLast(i);
          }
        }
      });
    }

    /// <summary>
    /// Tracks if <see cref="Awake"/> was called on this component.
    /// </summary>
    bool awakeCalled;

    readonly LazyVal<DisposableTracker> tracker;
    public int trackedCount => tracker.strict.trackedCount;
    public IEnumerable<TrackedDisposable> trackedDisposables => tracker.strict.trackedDisposables;

    public GameObjectDisposeTracker() {
      tracker = F.lazy(() => {
        if (!awakeCalled) trackersWaitingForAwake.Add(this);
        return new DisposableTracker(
          log,
          // ReSharper disable ExplicitCallerInfoArgument
          callerFilePath: Log.d.isDebug() ? gameObject.transform.debugPath() : gameObject.name,
          callerMemberName: nameof(GameObjectDisposeTracker),
          callerLineNumber: -1
          // ReSharper restore ExplicitCallerInfoArgument
        );
      });
    }
    
    public void Awake() {
      awakeCalled = true;
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
    /// <inheritdoc cref="GameObjectDisposeTracker"/>
    public static IDisposableTracker asDisposableTracker(this GameObject o) =>
      // If we're not in the play mode we don't want to instantiate any components.
      Application.isPlaying ? o.EnsureComponent<GameObjectDisposeTracker>() : NoOpDisposableTracker.instance;
  }
}