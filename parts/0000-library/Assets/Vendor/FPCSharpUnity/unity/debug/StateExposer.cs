using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.inspection;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.unity.core.Utilities;
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
      disposableTrackerRegistry.exposeStatic("Tracker groups", static () => new EnumerableValue(
        DisposableTrackerRegistry.instance.registered
          .GroupBy(_ => _.Key.callerData)
          .OrderBySafe(_ => _.Key.ToString())
          .Select(grouping => new HeaderValue(
            new StringValue($"{grouping.Count()} - {grouping.Key.asShortString()}"),
            new StringValue(grouping.Key.asString())
          ))
          .ToArrayFast()
      ));
      disposableTrackerRegistry.exposeStatic("Trackers", static () => new EnumerableValue(
        DisposableTrackerRegistry.instance.registered
          .OrderBySafe(_ => _.Key.ToString())
          .Select(kv => {
            var (key, tracker) = kv;
            return new HeaderValue(
              new StringValue($"{s(tracker.trackedCount)} tracked @ {s(key.ToShortString())}"),
              new EnumerableValue(
                tracker.trackedDisposables
                  .OrderBySafe(_ => _.createdAt.asShortString())
                  .Select(tracked => {
                    var values = new List<RenderableValue>{
                      new StringValue(tracked.createdAt.asShortString()),
                      new StringValue($"{tracked.createdAt.filePath}:{tracked.createdAt.lineNumber}"),
                    };
                    {if (tracked.inspectable.valueOut(out var inspectable)) {
                      values.Add(new ActionValue("Copy reference tree to clipboard", () => {
                        var str = inspectable switch {
                          // A hardcoded cast for a known type.
                          IRxObservable observable => 
                            $"Tracked from: {tracked.createdAt.asString()}\n"
                            + $"  {observable.renderObservableInspectionData().indentLines("  ", indentFirst: false)}",
                          _ => 
                            $"Tracked from: {tracked.createdAt.asString()}\n"
                            + $"  Inspectable '{inspectable}' created from:\n"
                            + $"  {inspectable.renderInspectable().indentLines("  ", indentFirst: false)}"
                        };
                        Clipboard.value = str;
                      }));
                    }}
                    
                    return new EnumerableValue(showCount: false, values);
                  })
                  .ToArrayFast()
              )
            );
          })
          .ToArrayFast()
      ));
    }
  }
}