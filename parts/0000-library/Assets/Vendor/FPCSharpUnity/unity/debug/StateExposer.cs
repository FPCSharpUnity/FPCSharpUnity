using System.Linq;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.macros;
using UnityEngine;
using static FPCSharpUnity.core.typeclasses.Str;

namespace FPCSharpUnity.unity.debug {
  /// <summary>
  /// Exposes fields of objects to Unity window.
  /// <para/>
  /// <see cref="StateExposerExts"/> and <see cref="StateExposerExts.exposeAllToInspector{A}"/>
  /// </summary>
  [Singleton(generateConstructor: false), PublicAPI] public partial class StateExposer {
    public readonly Scope rootScope = new();
    
    StateExposer() {
      // Expose disposable trackers by default.
      exposeDisposableTrackers();
    }

    /// <summary>Cleans everything before starting the game.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void reset() => instance.rootScope.clearNonStatics(); 

    public Scope withScope(ScopeKey name) => rootScope.withScope(name);
    public static Scope operator /(StateExposer e, ScopeKey name) => e.withScope(name);
    
    /// <summary>
    /// Exposes trackers from <see cref="DisposableTrackerRegistry"/>.
    /// </summary>
    void exposeDisposableTrackers() {
      var disposableTrackerRegistry = this / nameof(DisposableTrackerRegistry);
      disposableTrackerRegistry.exposeStatic("Trackers", () => new EnumerableValue(
        DisposableTrackerRegistry.instance.registered
          .OrderBySafe(_ => _.Key.ToString())
          .Select(kv => {
            var (key, tracker) = kv;
            return new HeaderValue(
              new StringValue($"{s(tracker.trackedCount)} tracked @ {s(key.ToShortString())}"),
              new EnumerableValue(
                tracker.trackedDisposables
                  .OrderBySafe(_ => _.asString())
                  .Select(tracked => new StringValue(tracked.asString()))
                  .ToArrayFast()
              )
            );
          })
          .ToArrayFast()
      ));
    }
  }
}