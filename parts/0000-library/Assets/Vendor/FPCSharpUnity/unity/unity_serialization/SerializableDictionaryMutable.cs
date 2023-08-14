using System;
using System.Collections.Immutable;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.reactive;
using GenerationAttributes;
using Sirenix.OdinInspector;
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
    RxRef.a(_keyValuePairs.ToImmutableDictionary(_ => _.key, _ => _.value));
    
  public SerializableDictionaryMutable(Pair[] keyValuePairs) : base(keyValuePairs) { }

  public void OnBeforeSerialize() { }
  public void OnAfterDeserialize() => updateCachedValue();

  [Button, OnInspectorGUI] void updateCachedValue() => valueChanged();

  public override void valueChanged() => 
    _dict.value = _keyValuePairs.ToImmutableDictionary(_ => _.key, _ => _.value);

  /// <summary>
  /// Sets the <see cref="value"/> for specified <see cref="key"/>. If application is not playing, then it also sets the
  /// <see cref="value"/> in the serialized data.
  /// </summary>
  public void set(K key, V value) {
    _dict.value = _dict.value.SetItem(key, value);
    
    // Do not set the serialized field values if playing, because that operation may be expensive and we do not want to
    // do that at runtime anyway.
    if (!Application.isPlaying) {
      setValueOnSerializedField();
    }

    void setValueOnSerializedField() {
      for (var i = 0; i < _keyValuePairs.Length; i++) {
        if (_keyValuePairs[i].key.Equals(key)) {
          _keyValuePairs[i].setValue(value);
          return;
        }
      }
      
      // Key not found, create a new element.
      _keyValuePairs = _keyValuePairs.addOne(new Pair(key, value));
    }
  }
    
  public void remove(K key) {
    _dict.value = _dict.value.Remove(key);
      
    // Do not set the serialized field values if playing, because that operation may be expensive and we do not want to
    // do that at runtime anyway.
    if (!Application.isPlaying) {
      setValueOnSerializedField();
    }
      
    void setValueOnSerializedField() {
      for (var i = 0; i < _keyValuePairs.Length; i++) {
        if (_keyValuePairs[i].key.Equals(key)) {
          _keyValuePairs = _keyValuePairs.removeAt(i);
          return;
        }
      }
    }
  }
}