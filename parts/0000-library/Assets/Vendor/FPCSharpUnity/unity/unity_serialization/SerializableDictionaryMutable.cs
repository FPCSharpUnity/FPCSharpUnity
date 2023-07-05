using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FPCSharpUnity.unity.unity_serialization {
  /// <summary>
  /// Mutable version of <see cref="SerializableDictionary{K,V}"/>
  /// </summary>
  [Serializable]
  public class SerializableDictionaryMutable<K, V> : SerializableDictionaryBase<K, V>, ISerializationCallbackReceiver {
    // Exposed in editor for debugging purposes.
    [ShowInInspector, ReadOnly] Option<Dictionary<K, V>> cachedValue;
  
    Dictionary<K, V> dictMutable {
      get {
        if (cachedValue.isNone) updateCachedValue();
        return cachedValue.get;
      }
    }

    public SerializableDictionaryMutable(Pair[] keyValuePairs) : base(keyValuePairs) { }

    public IReadOnlyDictionary<K, V> a => dictMutable;

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() => updateCachedValue();

    void updateCachedValue() {
      cachedValue = Some.a(_keyValuePairs.ToDictionary(_ => _.key, _ => _.value));
    }

    protected override void valueChanged() => cachedValue = None._;

    /// <summary>
    /// Sets the <see cref="value"/> for specified <see cref="key"/>. If application is not playing, then it also sets the
    /// <see cref="value"/> in the serialized data.
    /// </summary>
    public void set(K key, V value) {
      dictMutable[key] = value;
    
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
      dictMutable.Remove(key);
      
      // Do not set the serialized field values if playing, because that operation may be expensive and we do not want to
      // do that at runtime anyway.
      if (!Application.isPlaying) {
        setValueOnSerializedField();
      }
      
      void setValueOnSerializedField() {
        for (var i = 0; i < _keyValuePairs.Length; i++) {
          if (_keyValuePairs[i].key.Equals(key)) {
            _keyValuePairs.removeAt(i);
            return;
          }
        }
      }
    }
    
    public static implicit operator ImmutableDictionary<K, V>(SerializableDictionaryMutable<K, V> a) => 
      a.a.ToImmutableDictionary();
  }
}