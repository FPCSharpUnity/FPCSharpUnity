using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.reactive;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  [PublicAPI] public sealed class Scope {
    readonly Dictionary<ScopeKey, Scope> _scopes = new();
    readonly List<IData> data = new();

    public ISubscription add(IData data) {
      this.data.Add(data);
      return new Subscription(() => this.data.Remove(data));
    }

    /// <summary>Clear all non statically accessible registered values.</summary>
    public void clearNonStatics() {
      lock (this) {
        var invalidKeys = new HashSet<ScopeKey>();
        foreach (var (key, scope) in _scopes) {
          if (key.isValid) scope.clearNonStatics();
          else invalidKeys.Add(key);
        }
        foreach (var invalidKey in invalidKeys) {
          _scopes.Remove(invalidKey);
        }
        data.removeWhere(_ => !_.isStatic);
      }
    }

    public KeyValuePair<ScopeKey, Scope>[] scopes {
      get {
        // Copy the scopes on access to ensure the locking.
        lock (this) return _scopes.ToArray();
      }
    }
      
    public IEnumerable<IGrouping<Option<object>, ForRepresentation>> groupedData =>
      data.collect(_ => _.representation).GroupBy(_ => _.objectReference);
      
    public Scope withScope(ScopeKey name) {
      // Lock to support accessing from all threads.
      lock (this) { return _scopes.getOrUpdate(name, () => new()); }
    }

    public static Scope operator /(Scope e, ScopeKey name) => e.withScope(name);

    /// <summary>Exposes a named value that is available statically (not via an object instance).</summary>
    /// <note><b>To avoid memory leaks the <see cref="get"/> function needs to be a static one!</b></note>
    /// <returns>Subscription that can be disposed to remove the exposed value.</returns>
    public ISubscription exposeStatic(string name, Func<RenderableValue> get) => add(new StaticData(name, get));
    
    /// <summary>
    /// Exposes a named value that is available via an object instance.
    /// </summary>
    /// <note><b>To avoid memory leaks the <see cref="render"/> function needs to be a static one!</b></note>
    /// <returns>Subscription that can be disposed to remove the exposed value.</returns>
    public ISubscription expose<A, Data>(A reference, string name, Data data, Render<A, Data> render) where A : class => 
      add(new InstanceData<A, Data>(reference.weakRef(), name, data, render));
      
    /// <summary>
    /// Helper for exposing <see cref="Future{A}"/>. Does not do anything if the future is not async (because it's a
    /// struct then).
    /// </summary>
    public void expose<A>(Future<A> future, string name) {
      if (future.asHeapFuture.valueOut(out var heapFuture)) expose(heapFuture, name, Unit._, static (f, _) => f.ToString());
    }
  }
}