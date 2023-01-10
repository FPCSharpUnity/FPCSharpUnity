#if UNITY_EDITOR
using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout {
  public static partial class ViewProvider {
    /// <summary> Items are only created/destroyed inside editor. Do not use this for playmode. </summary>
    [GenConstructor] public partial class InstantiateAndDestroyEditor<Obj> : IViewProvider<Obj> where Obj : Component {
      readonly Obj template;

      public IViewProvider<Obj>.ViewInstance createItem(RectTransform parent) {
        var v = template.clone(parent: parent);
        return new(v, (RectTransform) v.transform);
      }

      public void destroyItem(Obj obj) => DestroyImmediate(obj.gameObject);
    }
  }
}
#endif