#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Android.Bindings.java.util;
using UnityEngine;
using FPCSharpUnity.unity.Extensions;

namespace FPCSharpUnity.unity.Android.Bindings.fp_csharp_unity.unity.util {
  public static class GCFreeHashMap {
    public static GCFreeHashMap<K, V> a<K, V>(IDictionary<K, V> source) =>
      new GCFreeHashMap<K, V>(source.Keys.ToArray(), source.Values.ToArray());

    public static GCFreeHashMap<K, VV> a<K, V, VV>(
      IDictionary<K, V> source, Func<V, VV> valueMapper
    ) =>
      new GCFreeHashMap<K, VV>(source.Keys.ToArray(), source.Values.ToArray(valueMapper));
  }

  public class GCFreeHashMap<K, V> : Binding {
    public readonly HashMap map;

    // ReSharper disable SuggestBaseTypeForParameter
    public GCFreeHashMap(K[] keys, V[] values) : base(
      new AndroidJavaObject(
        "fp_csharp_unity.unity.util.GCFreeHashMap", keys, values
    )) {
      map = new HashMap(java.Get<AndroidJavaObject>("map"));
    }
    // ReSharper restore SuggestBaseTypeForParameter
  }
}
#endif