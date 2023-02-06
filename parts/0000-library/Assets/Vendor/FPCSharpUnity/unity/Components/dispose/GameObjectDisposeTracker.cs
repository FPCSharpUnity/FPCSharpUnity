using System;
using System.Collections.Generic;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.inspection;
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
        trackersWaitingForAwake.removeWhere(
          replaceRemovedElementWithLast: true,
          predicate: static current => {
            if (!current) {
              // Tracker Component was destroyed before Awake was called on it, thus `Dispose()` was not called, because
              // `OnDestroy()` callback was not invoked. So we need to call `Dispose()` here manually.
              current.Dispose();
              return true;
            }
            else if (current.awakeCalled) {
              // Awake was called. That means `OnDestroy()` will work on this tracker Component. Don't need to check it on
              // update anymore.
              return true;
            }
            else {
              return false;
            }
          }
        );
      });
    }

    /// <summary>
    /// Tracks if <see cref="Awake"/> was called on this component.
    /// </summary>
    bool awakeCalled;

    readonly LazyVal<DisposableTracker> tracker;
    public int trackedCount => tracker.value.fold(0, static _ => _.trackedCount);
    public IEnumerable<TrackedDisposable> trackedDisposables => 
      tracker.value.fold(ImmutableArrayC<TrackedDisposable>.empty, _ => _.trackedDisposables);

    public GameObjectDisposeTracker() {
      tracker = Lazy.a(() => {
        if (!awakeCalled) trackersWaitingForAwake.Add(this);
        return new DisposableTracker(
          log,
          new CallerData(
            filePath: Log.d.isDebug() ? gameObject.transform.debugPath() : gameObject.name,
            memberName: nameof(GameObjectDisposeTracker),
            lineNumber: -1
          )
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
      IDisposable a, [Implicit] CallerData callerData = default
    ) => tracker.strict.track(a, callerData);

    public void untrack(IDisposable a) {
      if (tracker.value.valueOut(out var t)) t.untrack(a);
    }

    public CallerData createdAt => new CallerData(memberName: gameObject.name, filePath: "GameObject", lineNumber: -1);

    public void copyLinksTo(List<IInspectable> copyTo) {
      if (tracker.valueOut(out var t)) t.copyLinksTo(copyTo);
    }
  }

  public static class GameObjectDisposeTrackerOps {
    /// <inheritdoc cref="GameObjectDisposeTracker"/>
    public static IDisposableTracker asDisposableTracker(this GameObject o) =>
      // If we're not in the play mode we don't want to instantiate any components.
      Application.isPlaying ? o.EnsureComponent<GameObjectDisposeTracker>() : NoOpDisposableTracker.instance;
  }
}