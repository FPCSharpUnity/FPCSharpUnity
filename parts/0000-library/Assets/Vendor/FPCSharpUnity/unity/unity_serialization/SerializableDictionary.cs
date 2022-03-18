using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Utilities;
using FPCSharpUnity.unity.validations;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Components {
  [Serializable]
  public partial class SerializableDictionary<A, B> : OnObjectValidate {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [SerializeField, NonEmpty] Pair[] _keyValuePairs = {};
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

    public ImmutableDictionary<A, B> a { get; private set; } = ImmutableDictionary<A, B>.Empty;

    public bool onObjectValidateIsThreadSafe => false;
    public void OnAfterDeserialize() {
      var builder = new ImmutableDictionaryBuilder<A, B>();
      foreach (var pair in _keyValuePairs) {
        builder.add(new KeyValuePair<A, B>(pair.key, pair.value));
      }

      a = builder.build();
    }

    public IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent) {
      if (_keyValuePairs.Select(_ => _.key).Distinct().ToArray().Length < _keyValuePairs.Length) {
        yield return new ErrorMsg($"Duplicate keys are not allowed in {nameof(SerializableDictionary<A, B>)}");
      }
    }

    [Serializable] partial class Pair {
#pragma warning disable 649
      // ReSharper disable NotNullMemberIsNotInitialized
      [SerializeField, NotNull, PublicAccessor] A _key;
      [SerializeField, NotNull, PublicAccessor] B _value;
      // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
    }
  }
}