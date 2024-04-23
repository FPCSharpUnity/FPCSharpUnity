using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.unity_serialization {
  [Serializable]
  public abstract partial class SerializableDictionaryBase<A, B> : OnObjectValidate {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [
      SerializeField, NotNull, TableList, OnValueChanged(nameof(valueChanged)),
      ListDrawerSettings(CustomAddFunction = nameof(selector))
    ] protected Pair[] _keyValuePairs = {};
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

    public Pair[] getPairs() => _keyValuePairs;
    
    protected ImmutableDictionary<A, B> getValuesAsDictionary { get {
      using var _ = SerializableDictionaryBase.markerGetValuesAsDictionary.Auto();
      return _keyValuePairs
        // Adding new values in editor will create new elements with null key. Dictionary doesn't let us to have null
        // keys. Filter them out until developer sets the correct key in inspector, because inspector is not being
        // drawn until then.
        .GroupBy(kv => kv.key)
        .Select(gr => {
          Pair pair = null;
          foreach (var kv in gr) {
            if (pair != null) {
              // Log.d.error(); causes errors, because it is accessing unity stuff while it can't after compilation.
              Debug.LogError(
                $"More than one value for key {kv.key} in {nameof(SerializableDictionary<A, B>)}!"
              );
            }
            pair = kv;
          }
          return pair;
        })
        .Where(kv => kv.key != null)
#if UNITY_EDITOR // - Don't throw exceptions here, because it will break the inspector editor and we can't fix it easily.
        .GroupBy(kv => kv.key)
        .Select(gr => {
          var array = gr.toImmutableArrayC();
          if (array.Count > 1) {
            // Our `Log.d` throws `UnityException: get_inBatchMode is not allowed to be called during serialization`
            Debug.LogError($"Duplicate keys are not allowed in {nameof(SerializableDictionary<A, B>)}: `{gr.Key}`!");
          }
          return array[0];
        })
#endif
        .ToImmutableDictionary(_ => _.key, _ => _.value);
    } }

    /// <summary> This doesn't work if inspector draws array as table. So we show it above table as extra button. </summary>
    [Button, ShowIf(nameof(showAddButton)), PropertyOrder(-1)] void addKey() => selector();
    bool showAddButton => typeof(A).IsEnum;

    void selector() {
      if (typeof(A).IsEnum) {
#if UNITY_EDITOR
        // Only available in Sirenix.OdinInspector.Editor
        Vendor.FPCSharpUnity.unity.Utilities.GenericOdinSelector.showPopup(
          ((A[])Enum.GetValues(typeof(A))).Where(e => !_keyValuePairs.Any(kvp => kvp.key.Equals(e))),
          onSelect: e => _keyValuePairs = _keyValuePairs.addOne(new Pair(e, default))
        );
#endif
      } 
      else {
        _keyValuePairs = _keyValuePairs.addOne(new Pair(default, default));
      }
    }

    protected SerializableDictionaryBase(Pair[] keyValuePairs) {
      _keyValuePairs = keyValuePairs;
    }

    public abstract void valueChanged();

    public bool onObjectValidateIsThreadSafe => false;

    public IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent) =>
      _keyValuePairs
        .GroupBy(_ => _.key)
        .collect(gr => {
          var arr = gr.ToArray();
          return (arr.Length > 1).optM(() => new ErrorMsg(
            $"Duplicate keys are not allowed in {nameof(SerializableDictionary<A, B>)}! "
            + $"Values: {s(arr.Select(a => a.key.ToString()).mkStringEnumNewLines())}"
          ));
        });

    [Serializable, Record] public partial class Pair {
#pragma warning disable 649
      // ReSharper disable NotNullMemberIsNotInitialized
      [SerializeField, NotNull, PublicAccessor, TableColumnWidth(50)] A _key;
      [SerializeField, NotNull, PublicAccessor] B _value;
      // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

      public void setValue(B newValue) => _value = newValue;
    }
  }

  static class SerializableDictionaryBase {
    public static readonly ProfilerMarker markerGetValuesAsDictionary = new ("getValuesAsDictionary");
  }
}