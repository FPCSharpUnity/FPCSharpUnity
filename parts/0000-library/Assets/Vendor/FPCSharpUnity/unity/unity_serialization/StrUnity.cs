using FPCSharpUnity.core.typeclasses;
using UnityEngine;

namespace FPCSharpUnity.unity.unity_serialization {
  /// <summary>
  /// <see cref="Str"/> instances/functions for Unity types.
  /// </summary>
  public class StrUnity {
    public static string s(Hash128 v) => v.ToString();
  }
}