using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
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
    
    protected ImmutableDictionary<A, B> getValuesAsDictionary =>
      _keyValuePairs
        // Adding new values in editor will create new elements with null key. Dictionary doesn't let us to have null
        // keys. Filter them out until developer sets the correct key in inspector, because inspector is not being
        // drawn until then.
        .Where(kv => kv.key != null)
        .ToImmutableDictionary(_ => _.key, _ => _.value);

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

    public IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent) {
      if (_keyValuePairs.Select(_ => _.key).Distinct().ToArray().Length < _keyValuePairs.Length) {
        yield return new ErrorMsg($"Duplicate keys are not allowed in {nameof(SerializableDictionary<A, B>)}");
      }
    }

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
}