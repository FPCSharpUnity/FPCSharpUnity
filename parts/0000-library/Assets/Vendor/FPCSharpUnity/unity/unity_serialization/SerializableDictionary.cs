﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.typeclasses;
using UnityEngine;

namespace FPCSharpUnity.unity.unity_serialization; 

/// <summary>
/// A Unity-serializable dictionary which can only be changed in the Unity inspector. To be able to change this in
/// runtime use <see cref="SerializableDictionaryMutable{K,V}"/>.
/// </summary>
[Serializable]
public class SerializableDictionary<K, V> 
  : SerializableDictionaryBase<K, V>, ISerializationCallbackReceiver, IEquatable<SerializableDictionary<K, V>>,
    IDebugStr
{
  Option<ImmutableDictionary<K, V>> cachedValue;

  public SerializableDictionary(Pair[] keyValuePairs) : base(keyValuePairs) { }

  public SerializableDictionary(IEnumerable<KeyValuePair<K, V>> e) : base(
    e.Select(kvp => new Pair(kvp.Key, kvp.Value)).ToArray()
  ) {}

  public SerializableDictionary() : base(Array.Empty<Pair>()) {}

  public ImmutableDictionary<K, V> a {
    get {
      if (!Application.isPlaying) return getValuesAsDictionary;
      if (cachedValue.isNone) updateCachedValue();
      return cachedValue.get;
    }
  }

  public void OnBeforeSerialize() { }
  public void OnAfterDeserialize() => updateCachedValue();

  void updateCachedValue() {
    cachedValue = Some.a(getValuesAsDictionary);
  }

  public override void valueChanged() => cachedValue = None._;
    
  public static implicit operator ImmutableDictionary<K, V>(SerializableDictionary<K, V> a) => a.a;

  public bool Equals(SerializableDictionary<K, V> other) => 
    other != null
    && a.matchWithAndCollect(
      other.a, 
      extractKeyA: kvp => kvp.Key,
      extractKeyB: kvp => kvp.Key,
      onMatched: (p1, p2) => Some.a(p1.Key.Equals(p2.Key)),
      onANotMatched: _ => Some.a(false),
      onBNotMatched: _ => Some.a(false)
    ).All(_ => _);

  public override bool Equals(object obj) {
    if (ReferenceEquals(null, obj)) return false;
    if (ReferenceEquals(this, obj)) return true;
    if (obj.GetType() != GetType()) return false;
    return Equals((SerializableDictionary<K, V>)obj);
  }

  public override int GetHashCode() => StructuralEquals.GetHashCode(a);
    
  public string asDebugString() => a.Select(a => $"{a.Key}->{a.Value}").mkStringEnum();
}