using System.Collections.Immutable;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities {
  [PublicAPI] public static class SortingLayerUtils {
    public static ImmutableDictionary<int, SortingLayer> idToLayer() {
      var b = ImmutableDictionary.CreateBuilder<int, SortingLayer>();
      foreach (var sortingLayer in SortingLayer.layers) {
        b.Add(sortingLayer.id, sortingLayer);
      }

      return b.ToImmutable();
    }
  }
}