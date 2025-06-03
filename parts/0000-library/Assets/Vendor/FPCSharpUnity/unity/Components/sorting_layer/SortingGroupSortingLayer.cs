using FPCSharpUnity.unity.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace FPCSharpUnity.unity.Components.sorting_layer {
  [RequireComponent(typeof(SortingGroup))]
  [DisallowMultipleComponent]
  public sealed class SortingGroupSortingLayer : SortingLayerSetter {
    SortingGroup group => GetComponent<SortingGroup>();

    protected override void recordEditorChanges() =>
      group.recordEditorChanges("Renderer sorting layer changed");

    protected override void apply(ISortingLayerReference sortingLayer) =>
      sortingLayer.applyTo(group);

    protected override SortingLayerAndOrder extract() {
      var group = this.group;
      return new SortingLayerAndOrder(group.sortingLayerID, group.sortingOrder);
    }
  }
}
