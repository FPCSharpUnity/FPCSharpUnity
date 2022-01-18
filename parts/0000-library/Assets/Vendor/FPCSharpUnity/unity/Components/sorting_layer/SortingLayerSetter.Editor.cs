#if UNITY_EDITOR
using FPCSharpUnity.unity.Components.Interfaces;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.sorting_layer {
  [ExecuteInEditMode]
  public partial class SortingLayerSetter : IMB_Update {
    public void Update() {
      if (Application.isPlaying || !sortingLayer) return;

      var extracted = extract();
      var sortingLayerMatches =
        extracted.layerId == sortingLayer.sortingLayer
        && extracted.order == sortingLayer.orderInLayer;

      if (!sortingLayerMatches) {
        recordEditorChanges();
        apply(sortingLayer);
      }
    }
  }
}
#endif
