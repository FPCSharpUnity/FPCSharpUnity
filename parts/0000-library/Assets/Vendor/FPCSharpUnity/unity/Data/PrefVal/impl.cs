﻿using System;
using System.Collections.Generic;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.caching;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.inspection;
using FPCSharpUnity.core.serialization;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Data {
  // Should be class (not struct) because .write mutates object.
  class PrefValImpl<A> : PrefVal<A> {
    readonly string key;

    readonly IPrefValueBackend backend;
    readonly IPrefValueWriter<A> writer;
    readonly IRxRef<A> rxRef;

    // ReSharper disable once NotAccessedField.Local
    // To keep the subscription alive.
    readonly ISubscription persistSubscription;

    public CallerData createdAt { get; }

    public A value {
      get => rxRef.value;
      set => rxRef.value = value;
    }

    public RxVersion version => rxRef.version;

    public object valueUntyped {
      get => value;
      set => this.trySetUntyped(value);
    }

    void persist(A value) => writer.write(backend, key, value);

    public PrefValImpl(
      string key, IPrefValueRW<A> rw, A defaultVal,
      IPrefValueBackend backend, [Implicit] CallerData createdAt = default
    ) {
      this.key = key;
      writer = rw;
      this.backend = backend;
      rxRef = RxRef.a(rw.read(backend, key, defaultVal));
      persistSubscription = rxRef.subscribe(NoOpDisposableTracker.instance, persist);
      this.createdAt = createdAt;
    }

    public void save() => backend.save();

    public override string ToString() => $"{nameof(PrefVal<A>)}({value})";

    #region ICachedBlob

    public bool cached => true;
    Option<Try<A>> ICachedBlob<A>.read() => Some.a(F.scs(value));

    Try<Unit> ICachedBlob<A>.store(A data) {
      value = data;
      return F.scs(F.unit);
    }

    public Try<Unit> clear() {
      backend.delete(key);
      return F.scs(F.unit);
    }

    #endregion

    #region IRxRef

    public int subscriberCount => rxRef.subscriberCount;

    public void copyLinksTo(List<IInspectable> copyTo) => rxRef.copyLinksTo(copyTo);

    public Option<Delegate> maybeSubscribeToSource => rxRef.maybeSubscribeToSource;

    public void subscribe(
      ITracker tracker, Action<A> onEvent, out ISubscription subscription, CallerData callerData,
      IInspectable targetInspectable = null
    ) =>
      rxRef.subscribe(tracker: tracker, onEvent: onEvent, subscription: out subscription, callerData, targetInspectable);

    public ISubscription subscribeWithoutEmit(
      ITracker tracker, Action<A> onEvent, CallerData callerData,
      IInspectable targetInspectable = null
    ) =>
      rxRef.subscribeWithoutEmit(tracker: tracker, onEvent: onEvent, callerData, targetInspectable);

    #endregion
  }

  class PrefValDictImpl<K, V> : PrefValDictionary<K, V> {
    readonly Dictionary<K, PrefVal<V>> cache = new Dictionary<K, PrefVal<V>>();
    readonly string keyPrefix;
    readonly Func<K, string> keyToString;
    readonly ISerializedRW<V> vRw;
    readonly PrefValStorage storage;
    readonly V defaultValue;
    readonly PrefVal.OnDeserializeFailure onDeserializeFailure;
    readonly ILog log;

    public PrefValDictImpl(
      string keyPrefix, Func<K, string> keyToString, ISerializedRW<V> vRw, PrefValStorage storage, V defaultValue, 
      PrefVal.OnDeserializeFailure onDeserializeFailure, ILog log = null
    ) {
      this.keyPrefix = keyPrefix;
      this.keyToString = keyToString;
      this.vRw = vRw;
      this.storage = storage;
      this.defaultValue = defaultValue;
      this.onDeserializeFailure = onDeserializeFailure;
      this.log = log;
    }

    string stringKey(K key) => $"{keyPrefix}:{keyToString(key)}";

    public bool hasKey(K key) => cache.ContainsKey(key) || storage.hasKey(stringKey(key));

    public PrefVal<V> this[K key] {
      get {
        if (cache.TryGetValue(key, out var prefVal)) {
          return prefVal;
        }
        else {
          prefVal = storage.custom(stringKey(key), defaultValue, vRw, onDeserializeFailure, log);
          cache.Add(key, prefVal);
          return prefVal;
        }
      }
    }
  }
}