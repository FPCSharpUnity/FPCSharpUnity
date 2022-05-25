using System;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class TypeExts {
    // checks if type can be used in GetComponent and friends
    public static bool canBeUnityComponent(this Type type) =>
      type.IsInterface
      || typeof(MonoBehaviour).IsAssignableFrom(type)
      || typeof(Component).IsAssignableFrom(type);
  }
}
