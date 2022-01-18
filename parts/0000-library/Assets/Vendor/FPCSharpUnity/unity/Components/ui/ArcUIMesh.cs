using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.pools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  public class ArcUIMesh : BaseMeshEffect {
    [InfoBox("You should use this component together with Subdivide UI Mesh component for best results.")]
    public float widthToAngleRatio = 1;
    public float radius = 1;

    public override void ModifyMesh(VertexHelper vh) {
      if (!IsActive()) return;
      if (widthToAngleRatio.approx0()) return;

      using var vertsDisposable = ListPool<UIVertex>.instance.BorrowDisposable();
      var verts = vertsDisposable.value;
      vh.GetUIVertexStream(verts);
      for (var i = 0; i < verts.Count; i++) {
        var vert = verts[i];
        var pos = vert.position;
        var distance = pos.y + radius;
        var angle = widthToAngleRatio * pos.x - Mathf.PI / 2;
        var newPos = distance * angle.radiansToVector();
        vert.position = new Vector3(newPos.x, newPos.y, pos.z);
        verts[i] = vert;
      }
      vh.Clear();
      vh.AddUIVertexTriangleStream(verts);
    }
  }
}
