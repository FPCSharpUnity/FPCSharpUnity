using System;
using System.Collections.Immutable;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.unity_serialization;

[Serializable]
public class SerializableDictionary<K, V> : SerializableDictionaryBase<K, V>, ISerializationCallbackReceiver {
  Option<ImmutableDictionary<K, V>> cachedValue;

  public ImmutableDictionary<K, V> a {
    get {
      if (cachedValue.isNone) updateCachedValue();
      return cachedValue.get;
    }
  }

  public void OnBeforeSerialize() { }
  public void OnAfterDeserialize() => updateCachedValue();

  void updateCachedValue() {
    cachedValue = Some.a(_keyValuePairs.ToImmutableDictionary(_ => _.key, _ => _.value));
  }

  protected override void valueChanged() => cachedValue = None._;
}