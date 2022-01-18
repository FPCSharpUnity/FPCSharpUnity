using System.Linq;
using FPCSharpUnity.core.exts;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.sorting_layer.Editor {
  [CustomEditor(typeof(ExposeRendererSortingLayerFields), true)]
  [CanEditMultipleObjects]
  public class ExposeRendererSortingLayerFieldsEditor : UnityEditor.Editor {
    Renderer[] renderers;

    public void OnEnable() {
      renderers =
        targets
        .collect(t => ((MonoBehaviour) t).GetComponent<Renderer>().opt())
        .ToArray();
    }

    public override void OnInspectorGUI() {
      SortingLayerHelper.drawSortingLayers(renderers);
    }
  }
}
