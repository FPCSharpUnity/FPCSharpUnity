using FPCSharpUnity.unity.Utilities;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.sorting_layer {
  [RequireComponent(typeof(Renderer))]
  [DisallowMultipleComponent]
  public sealed class RendererSortingLayer : SortingLayerSetter {
    new Renderer renderer => GetComponent<Renderer>();

    protected override void recordEditorChanges() =>
      renderer.recordEditorChanges("Renderer sorting layer changed");

    protected override void apply(ISortingLayerReference sortingLayer) =>
      sortingLayer.applyTo(renderer);

    protected override SortingLayerAndOrder extract() {
      var renderer = this.renderer;
      return new SortingLayerAndOrder(renderer.sortingLayerID, renderer.sortingOrder);
    }
  }
}
