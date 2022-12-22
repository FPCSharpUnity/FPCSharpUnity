using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  [PublicAPI] public sealed class Scope {
    readonly Dictionary<ScopeKey, Scope> _scopes = new();
    readonly List<IData> data = new();

    public void add(IData data) => this.data.Add(data);

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
    public void exposeStatic(string name, Func<IRenderableValue> get) => add(new StaticData(name, get));
    public void exposeStatic(string name, Func<string> get) => exposeStatic(name, () => new StringValue(get()));
    public void exposeStatic(string name, Func<float> get) => exposeStatic(name, () => new FloatValue(get()));
    public void exposeStatic(string name, Func<bool> get) => exposeStatic(name, () => new BoolValue(get()));
    public void exposeStatic(string name, Func<UnityEngine.Object> get) => exposeStatic(name, () => new ObjectValue(get()));
    public void exposeStatic(string name, Func<Action> onClick) => exposeStatic(name, () => new ActionValue(onClick()));
    /// <summary>Helper for exposing <see cref="Future{A}"/>.</summary>
    public void exposeStatic<A>(string name, Func<Future<A>> get) => exposeStatic(name, () => get().ToString());
      
    /// <summary>Exposes a named value that is available via an object instance.</summary>
    public void expose<A>(A reference, string name, Func<A, IRenderableValue> get) where A : class => 
      add(new InstanceData<A>(reference.weakRef(), name, get));
      
    /// <summary>
    /// Helper for exposing <see cref="Future{A}"/>. Does not do anything if the future is not async (because it's a
    /// struct then).
    /// </summary>
    public void expose<A>(Future<A> future, string name) {
      if (future.asHeapFuture.valueOut(out var heapFuture)) expose(heapFuture, name, _ => _.ToString());
    }
  }
}