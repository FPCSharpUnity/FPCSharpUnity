#if UNITY_EDITOR
using FPCSharpUnity.unity.Utilities;
using UnityEngine;

namespace FPCSharpUnity.unity.Components {
  public partial class LineRendererTrailDrawer {
    public void setSerializedData_onlyInEditor(float duration, float minVertexDistance, LineRenderer lineRenderer) {
      this.recordEditorChanges("Set time and distance");
      this.duration = duration;
      this.minVertexDistance = minVertexDistance;
      this.lineRenderer = lineRenderer;
    }
  }
}
#endif