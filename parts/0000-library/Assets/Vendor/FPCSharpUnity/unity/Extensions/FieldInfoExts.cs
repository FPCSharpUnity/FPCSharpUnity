using System;
using System.Reflection;
using FPCSharpUnity.unity.Utilities;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class FieldInfoExts {
    public static bool isSerializable(this FieldInfo fi) =>
      (fi.IsPublic && !fi.hasAttribute<NonSerializedAttribute>())
      || ((fi.IsPrivate || fi.IsFamily) && (fi.hasAttribute<SerializeField>() || fi.hasAttribute<SerializeReference>()));
  }
}