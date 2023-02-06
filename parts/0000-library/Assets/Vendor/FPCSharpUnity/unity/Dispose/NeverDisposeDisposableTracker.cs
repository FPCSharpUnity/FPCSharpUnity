using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.debug;
using GenerationAttributes;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.inspection;
using UnityEngine;

namespace FPCSharpUnity.unity.Dispose {
  /// <summary>
  /// Tracker that allows you to register a subscription to be kept forever.
  /// <para/>
  /// This should only be used for things that should never go out of scope.
  /// </summary>
#if UNITY_EDITOR
  [UnityEditor.InitializeOnLoad]
#endif
  public class NeverDisposeDisposableTracker : IInspectableTracker {
    [LazyProperty] public static IInspectableTracker instance => new NeverDisposeDisposableTracker();

    readonly DisposableTracker tracker = DisposableTracker.withoutExceptionHandling();
    public int trackedCount => tracker.trackedCount;
    public IEnumerable<TrackedDisposable> trackedDisposables => tracker.trackedDisposables;

    // needed for InitializeOnLoad to work
    // ReSharper disable once EmptyConstructor
    static NeverDisposeDisposableTracker() {}

    NeverDisposeDisposableTracker() {
#if UNITY_EDITOR
      if (Application.isPlaying) {
        var exposer = StateExposer.instance / nameof(NeverDisposeDisposableTracker);
        exposer.expose(
          this, nameof(tracker.trackedDisposables), Unit._,
          static (v, _) => new StateExposer.EnumerableValue( 
            v.trackedDisposables.Select(d => new StateExposer.StringValue(d.asString())).ToArrayFast()
          )
        );
      }
#endif
    }

    public void track(
      IDisposable a, [Implicit] CallerData callerData = default
    ) => tracker.track(a, callerData);

    public void untrack(IDisposable a) => tracker.untrack(a);

    public CallerData createdAt => tracker.createdAt;
    public void copyLinksTo(List<IInspectable> copyTo) => tracker.copyLinksTo(copyTo);
  }
}