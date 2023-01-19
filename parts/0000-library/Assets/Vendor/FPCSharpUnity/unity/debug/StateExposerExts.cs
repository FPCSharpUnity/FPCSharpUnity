using FPCSharpUnity.core.exts;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.debug;

[PublicAPI] public static class StateExposerExts {
  /// <note><b>To avoid memory leaks the <see cref="get"/> function needs to be a static one!</b></note>
  public static void exposeToInspector<Data, A>(
    this GameObject go, A reference, string name, Data data, StateExposer.Render<A, Data> render
  ) where A : class {
    (StateExposer.instance / go).expose(reference, name, data, render);
  }

  public static void exposeAllToInspector<A>(
    this GameObject go, A reference
  ) where A : class {
    foreach (var field in typeof(A).getAllFields()) {
      var fieldType = field.FieldType;
      if (fieldType.IsSubclassOf(typeof(float)))
        exposeToInspector(go, reference, field.Name, field, static (a, field) => (float) field.GetValue(a));
      else if (fieldType.IsSubclassOf(typeof(bool)))
        exposeToInspector(go, reference, field.Name, field, static (a, field) => (bool) field.GetValue(a));
      else if (fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
        exposeToInspector(go, reference, field.Name, field, static (a, field) => (UnityEngine.Object) field.GetValue(a));
      else
        exposeToInspector(go, reference, field.Name, field, static (a, field) => {
          var obj = field.GetValue(a);
          return obj == null ? "null" : obj.ToString();
        });
    }
  }
}