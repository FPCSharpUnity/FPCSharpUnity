using System;
using System.Collections.Immutable;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.unity_serialization;

[Serializable]
public class SerializableDictionary<K, V> : SerializableDictionaryBase<K, V>, ISerializationCallbackReceiver {
  Option<ImmutableDictionary<K, V>> cachedValue;

  public SerializableDictionary(Pair[] keyValuePairs) : base(keyValuePairs) { }

  public ImmutableDictionary<K, V> a {
    get {
      if (!Application.isPlaying) return _keyValuePairs.ToImmutableDictionary(_ => _.key, _ => _.value);
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