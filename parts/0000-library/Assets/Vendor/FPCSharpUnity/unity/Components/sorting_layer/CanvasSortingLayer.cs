﻿using FPCSharpUnity.unity.Utilities;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.sorting_layer {
  [RequireComponent(typeof(Canvas))]
  [DisallowMultipleComponent]
  public sealed class CanvasSortingLayer : SortingLayerSetter {
    Canvas canvas => GetComponent<Canvas>();

    protected override void recordEditorChanges() =>
      canvas.recordEditorChanges("Canvas sorting layer changed");

    protected override void apply(SortingLayerReference sortingLayer) {
      canvas.overrideSorting = true;
      sortingLayer.applyTo(canvas);
    }

    protected override SortingLayerAndOrder extract() {
      var canvas = this.canvas;
      return new SortingLayerAndOrder(canvas.sortingLayerID, canvas.sortingOrder);
    }
  }
}
