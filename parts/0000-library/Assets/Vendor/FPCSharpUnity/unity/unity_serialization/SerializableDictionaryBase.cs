using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.unity_serialization;

[Serializable]
public abstract partial class SerializableDictionaryBase<A, B> : OnObjectValidate {
#pragma warning disable 649
  // ReSharper disable NotNullMemberIsNotInitialized
  [SerializeField, NotNull, TableList, OnValueChanged(nameof(valueChanged))] protected Pair[] _keyValuePairs = {};
  // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

  protected abstract void valueChanged();

  public bool onObjectValidateIsThreadSafe => false;

  public IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent) {
    if (_keyValuePairs.Select(_ => _.key).Distinct().ToArray().Length < _keyValuePairs.Length) {
      yield return new ErrorMsg($"Duplicate keys are not allowed in {nameof(SerializableDictionary<A, B>)}");
    }
  }

  [Serializable, Record] protected partial class Pair {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [SerializeField, NotNull, PublicAccessor, TableColumnWidth(50)] A _key;
    [SerializeField, NotNull, PublicAccessor] B _value;
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

    public void setValue(B newValue) => _value = newValue;
  }
}