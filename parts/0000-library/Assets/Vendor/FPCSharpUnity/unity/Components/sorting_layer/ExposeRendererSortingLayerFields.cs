using Sirenix.OdinInspector;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.sorting_layer {
  [
    RequireComponent(typeof(Renderer)),
    TypeInfoBox(
      "Allows you to change the sorting layer of all renderers that are attached to the same GameObject as this " +
      "component."
    )
  ]
  public class ExposeRendererSortingLayerFields : MonoBehaviour {
    // Functionality provided by editor script.
  }
}