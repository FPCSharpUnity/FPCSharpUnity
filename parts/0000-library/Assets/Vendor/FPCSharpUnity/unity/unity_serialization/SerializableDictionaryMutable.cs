using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.pools;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.utils;
using GenerationAttributes;
using Sirenix.OdinInspector;
using Unity.Profiling;
using UnityEngine;

namespace FPCSharpUnity.unity.unity_serialization; 

/// <summary>
/// Mutable version of <see cref="SerializableDictionary{K,V}"/>
/// </summary>
[Serializable]
public partial class SerializableDictionaryMutable<K, V> 
  : SerializableDictionaryBase<K, V>, ISerializationCallbackReceiver 
{
  [LazyProperty, PublicReadOnlyAccessor] IRxRef<ImmutableDictionary<K, V>> _dict =>
    RxRef.a(getValuesAsDictionary);
    
  public SerializableDictionaryMutable(Pair[] keyValuePairs) : base(keyValuePairs) { }

  public void OnBeforeSerialize() { }
  public void OnAfterDeserialize() => updateCachedValue();

  [Button, OnInspectorGUI] void updateCachedValue() => valueChanged();

  public override void valueChanged() {
    using var _ = SerializableDictionaryMutable.markerValueChanged.Auto();
    _dict.value = getValuesAsDictionary;
  }

  /// <summary>
  /// Sets values for specified keys. If application is not playing, then it also sets the values in the serialized data.
  /// Use this to batch values into a single update call, because updating `<see cref="_dict"/>` can be performance
  /// expensive.
  /// </summary>
  public void set(params KeyValuePair<K, V>[] values) {
    _dict.value = _dict.value.SetItems(values);
    
    // Do not set the serialized field values if playing, because that operation may be expensive and we do not want to
    // do that at runtime anyway.
    if (!Application.isPlaying) {
      values.matchWith(
        _keyValuePairs,
        extractKeyA: kv => kv.Key,
        extractKeyB: pair => pair.key,
        onMatched: static (kv, pair) => pair.setValue(kv.Value),
        onANotMatched: kv => _keyValuePairs = _keyValuePairs.addOne(new Pair(kv.Key, kv.Value))
      );
    }
  }
  
  /// <summary>
  /// Sets the <see cref="value"/> for specified <see cref="key"/>. If application is not playing, then it also sets the
  /// <see cref="value"/> in the serialized data. For updating multiple values at once, use <see cref="set"/>.
  /// </summary>
  public void setOne(K key, V value) {
    using var _ = ArrayPool<KeyValuePair<K, V>>.instance[1].borrowDisposable(out var array);
    array[0] = KV.a(key, value);
    set(array);
  }
    
  /// <summary>
  /// Removes specified keys. If application is not playing, then it also removes the keys in the serialized data.
  /// Use this to batch values into a single update call, because updating `<see cref="_dict"/>` can be performance
  /// expensive.
  /// </summary>
  public void remove(params K[] keys) {
    _dict.value = _dict.value.RemoveRange(keys);
      
    // Do not set the serialized field values if playing, because that operation may be expensive and we do not want to
    // do that at runtime anyway.
    if (!Application.isPlaying) {
      var set = keys.toHashSet();
      for (var i = 0; i < _keyValuePairs.Length; i++) {
        var key = _keyValuePairs[i].key;
        if (set.Contains(key)) {
          _keyValuePairs = _keyValuePairs.removeAt(i--);
          set.Remove(key);
        }
      }
    }
  }
  
  /// <summary>
  /// Removes specified key. If application is not playing, then it also removes the key in the serialized data.
  /// For removing multiple values at once, use <see cref="remove"/>.
  /// </summary>
  public void removeOne(K key) {
    using var _ = ArrayPool<K>.instance[1].borrowDisposable(out var array);
    array[0] = key;
    remove(array);
  }
}

static class SerializableDictionaryMutable {
  public static readonly ProfilerMarker markerValueChanged = new ("valueChanged");
}