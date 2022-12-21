using System;
using FPCSharpUnity.core.exts;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.debug;

[PublicAPI] public static class StateExposerExts {
  public static void exposeToInspector<A>(
    this GameObject go, A reference, string name, Func<A, StateExposer.IRenderableValue> get
  ) where A : class {
    (StateExposer.instance / go).expose(reference, name, get);
  }

  public static void exposeAllToInspector<A>(
    this GameObject go, A reference
  ) where A : class {
    foreach (var field in typeof(A).getAllFields()) {
      var fieldType = field.FieldType;
      if (fieldType.IsSubclassOf(typeof(float)))
        exposeToInspector(go, reference, field.Name, a => (float) field.GetValue(a));
      else if (fieldType.IsSubclassOf(typeof(bool)))
        exposeToInspector(go, reference, field.Name, a => (bool) field.GetValue(a));
      else if (fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
        exposeToInspector(go, reference, field.Name, a => (UnityEngine.Object) field.GetValue(a));
      else
        exposeToInspector(go, reference, field.Name, a => {
          var obj = field.GetValue(a);
          return obj == null ? "null" : obj.ToString();
        });
    }
  }
}